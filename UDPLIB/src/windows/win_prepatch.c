/* ==========================================================================
 * UDPLIB Windows injector ("prepatcher" for Microsoft Windows).
 *
 * "Подкидывание" библиотеки перед запуском игры:
 *   1. Создаём процесс Banana Shooter в приостановленном состоянии
 *      (CREATE_SUSPENDED) - игра ещё не выполнила ни одной инструкции.
 *   2. Виртуализируем адрес функции LoadLibraryW из kernel32.dll текущего
 *      процесса (он совпадает с адресом в целевом процессе, т.к. kernel32
 *      маппится по одному базовому адресу в рамках одной загрузки системы).
 *   3. Пишем путь к udplib.dll в память цели и создаём удалённый поток,
 *      который вызывает LoadLibraryW(udplib.dll). ДLLp loader вызывает
 *      DllMain(UDPLIB), который стартует UDP-слушатель до main() игры.
 *   4. Возобновляем главный поток игры.
 *
 * Компиляция (MinGW-w64 на Linux или MSVC cl.exe на Windows):
 *   x86_64-w64-mingw32-gcc -shared -municode win_prepatch.c -o prepatcher.dll \
 *        -lkernel32                        # как DLL для GameOverlay
 *   x86_64-w64-mingw32-gcc win_prepatch.c -o prepatcher.exe -lkernel32 \
 *        -DSTANDALONE                       # как standalone-консоль
 * ========================================================================== */
#ifdef _WIN32

#ifndef STANDALONE
#  define UDPLIB_API __declspec(dllexport)
#else
#  define UDPLIB_API
#endif

#include <windows.h>
#include <tlhelp32.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <wctype.h>

#define UDPLIB_DLL_NAME   L"udplib.dll"

/* ---- locate our own module directory ------------------------------------- */
static void get_module_dir(HMODULE mod, wchar_t* out, size_t cap)
{
    wchar_t path[MAX_PATH];
    GetModuleFileNameW(mod, path, MAX_PATH);
    wchar_t* slash = wcsrchr(path, L'\\');
    if (slash) {
        size_t n = (size_t)(slash - path) + 1;
        if (n >= cap) n = cap - 1;
        wcsncpy(out, path, n);
        out[n] = 0;
    } else {
        wcscpy(out, L".\\");
    }
}

/* ---- is a game already running? ------------------------------------------ */
static HANDLE find_game_process(wchar_t* exeNameLower)
{
    HANDLE snap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (snap == INVALID_HANDLE_VALUE) return 0;
    PROCESSENTRY32W pe;
    pe.dwSize = sizeof(pe);
    HANDLE result = 0;
    if (Process32FirstW(snap, &pe)) {
        do {
            wchar_t lower[MAX_PATH];
            _snwprintf(lower, MAX_PATH, L"%s", pe.szExeFile);
            for (wchar_t* p = lower; *p; ++p) *p = (wchar_t)towlower(*p);
            if (wcsstr(lower, exeNameLower)) {
                result = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION | SYNCHRONIZE,
                                     FALSE, pe.th32ProcessID);
                break;
            }
        } while (Process32NextW(snap, &pe));
    }
    CloseHandle(snap);
    return result;
}

/* ---- core injection ------------------------------------------------------ */
static int inject_dll_into(DWORD pid, const wchar_t* dllPath, HANDLE hProcIn /*may be NULL*/)
{
    HANDLE hProc = hProcIn ? hProcIn : OpenProcess(
        PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION |
        PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ,
        FALSE, pid);
    if (!hProc) {
        fprintf(stderr, "[win_prepatch] OpenProcess(%lu) failed: %lu\n",
                (unsigned long)pid, GetLastError());
        return -1;
    }

    size_t pathBytes = (wcslen(dllPath) + 1) * sizeof(wchar_t);
    void* remote = VirtualAllocEx(hProc, 0, pathBytes, MEM_COMMIT | MEM_RESERVE,
                                  PAGE_READWRITE);
    if (!remote) {
        fprintf(stderr, "[win_prepatch] VirtualAllocEx failed: %lu\n", GetLastError());
        if (!hProcIn) CloseHandle(hProc);
        return -2;
    }
    if (!WriteProcessMemory(hProc, remote, dllPath, pathBytes, 0)) {
        fprintf(stderr, "[win_prepatch] WriteProcessMemory failed: %lu\n", GetLastError());
        VirtualFreeEx(hProc, remote, 0, MEM_RELEASE);
        if (!hProcIn) CloseHandle(hProc);
        return -3;
    }

    HMODULE k32 = GetModuleHandleW(L"kernel32.dll");
    FARPROC loadLib = GetProcAddress(k32, "LoadLibraryW");
    if (!loadLib) {
        VirtualFreeEx(hProc, remote, 0, MEM_RELEASE);
        if (!hProcIn) CloseHandle(hProc);
        return -4;
    }

    HANDLE hThread = CreateRemoteThread(hProc, 0, 0,
                                        (LPTHREAD_START_ROUTINE)loadLib,
                                        remote, 0, 0);
    if (!hThread) {
        fprintf(stderr, "[win_prepatch] CreateRemoteThread failed: %lu\n", GetLastError());
        VirtualFreeEx(hProc, remote, 0, MEM_RELEASE);
        if (!hProcIn) CloseHandle(hProc);
        return -5;
    }
    WaitForSingleObject(hThread, 60000);
    DWORD exitCode = 0;
    GetExitCodeThread(hThread, &exitCode); /* HMODULE of loaded dll (low 32 bit) */
    CloseHandle(hThread);
    VirtualFreeEx(hProc, remote, 0, MEM_RELEASE);
    if (!hProcIn) CloseHandle(hProc);

    if (exitCode == 0) {
        fprintf(stderr, "[win_prepatch] LoadLibraryW(\"%S\") inside target FAILED\n", dllPath);
        return -6;
    }
    fprintf(stderr, "[win_prepatch] injected %S into pid %lu\n", dllPath,
            (unsigned long)pid);
    return 0;
}

/* ---- exported entry points ------------------------------------------------ */

/* Called by Steam's GameOverlay loader from the ALREADY RUNNING game process.
 * We cannot re-create the process here, so we simply LoadLibrary ourselves:
 * udplib.dll sits next to this overlay copy, its DllMain starts the bridge. */
UDPLIB_API void UDPLIB_STEAMOVERLAY_INITIALIZED(void)
{
    wchar_t dir[MAX_PATH], dll[MAX_PATH];
    get_module_dir(GetModuleHandleW(0), dir, MAX_PATH);
    _snwprintf(dll, MAX_PATH, L"%s%s", dir, UDPLIB_DLL_NAME);
    if (!LoadLibraryW(dll)) {
        fprintf(stderr, "[win_prepatch] overlay bootstrap: LoadLibraryW(%S) err=%lu\n",
                dll, GetLastError());
    }
}

UDPLIB_API void UDPLIB_STEAMOVERLAY_SHUTDOWN(void) { }

/* Standalone: "prepatcher.exe [game.exe] [--args ...]"
 * Launches the game suspended, drops udplib.dll in, resumes it. */
#ifdef STANDALONE
int wmain(int argc, wchar_t** argv)
{
    wchar_t dir[MAX_PATH], dll[MAX_PATH];
    get_module_dir(GetModuleHandleW(0), dir, MAX_PATH);
    _snwprintf(dll, MAX_PATH, L"%s%s", dir, UDPLIB_DLL_NAME);

    wchar_t gameName[] = L"banana shooter";
    int argi = 1;
    if (argc > 1 && (_wcsicmp(argv[1], L"--attach") == 0)) {
        /* attach mode: inject into an already running instance */
        HANDLE h = find_game_process(gameName);
        if (!h) { fwprintf(stderr, L"[win_prepatch] no running game found\n"); return 1; }
        DWORD pid = GetProcessId(h);
        CloseHandle(h);
        return inject_dll_into(pid, dll, 0) == 0 ? 0 : 1;
    }
    if (argc > 1) { /* first arg = explicit game path */
        wcscpy(gameName, argv[1]);
        argi = 2;
    }

    /* build command line: "game.exe" <rest args> */
    wchar_t cmdline[MAX_PATH * 4] = L"\"";
    _snwprintf(cmdline + 1, MAX_PATH * 4 - 2, L"%s\"", argv[argi - 1]);
    for (int i = argi; i < argc; ++i) {
        wcscat(cmdline, L" \"");
        wcscat(cmdline, argv[i]);
        wcscat(cmdline, L"\"");
    }

    STARTUPINFOW si; memset(&si, 0, sizeof(si)); si.cb = sizeof(si);
    PROCESS_INFORMATION pi;
    if (!CreateProcessW(0, cmdline, 0, 0, FALSE, CREATE_SUSPENDED, 0, dir, &si, &pi)) {
        fwprintf(stderr, L"[win_prepatch] CreateProcessW failed: %lu\n", GetLastError());
        return 2;
    }
    fprintf(stderr, "[win_prepatch] game started suspended (pid %lu)\n",
            (unsigned long)pi.dwProcessId);

    if (inject_dll_into(pi.dwProcessId, dll, pi.hProcess) != 0) {
        TerminateProcess(pi.hProcess, 3);
        CloseHandle(pi.hThread); CloseHandle(pi.hProcess);
        return 3;
    }
    ResumeThread(pi.hThread);
    fprintf(stderr, "[win_prepatch] game resumed with udplib loaded\n");
    CloseHandle(pi.hThread);
    CloseHandle(pi.hProcess);
    return 0;
}
#endif /* STANDALONE */

#endif /* _WIN32 */
