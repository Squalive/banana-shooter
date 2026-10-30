/* ==========================================================================
 * UDPLIB GNU/Linux injector ("prepatcher" for Linux/UNIX).
 *
 * "Подкидывание" библиотеки перед запуском игры:
 *   1. РЕЖИМ ПО УМОЛЧАНИЮ (безопасный, работает всегда): prepatcher ставит
 *      LD_PRELOAD=libudplib.so и exec'ит игру. Динамический линковщик
 *      инициализирует нашу .so ДО main() игры; конструктор из
 *      libudplib_entry.c поднимает UDP-слушатель до старта Unity. Исходный
 *      бинарник игры не мутируется.
 *   2. РЕЖИМ --attach (для уже запущенного экземпляра, только x86_64):
 *      ptrace(PTRACE_ATTACH) -> удалённые syscall mmap/write через gadget
 *      "syscall; int3", найденный в libc цели -> удалённый вызов dlopen по
 *      адресу, вычисленному как base_target(libdl)+offset_local(dlopen) ->
 *      munmap буфера -> полное восстановление регистров. Требует прав
 *      (тот же пользователь + ptrace_scope).
 *
 * Сборка: gcc linux_prepatch.c -o prepatcher -ldl
 * ========================================================================== */
#ifndef _WIN32

#define _GNU_SOURCE
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <ctype.h>
#include <unistd.h>
#include <errno.h>
#include <dlfcn.h>
#include <dirent.h>
#include <signal.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <sys/wait.h>
#include <sys/ptrace.h>
#include <sys/mman.h>
#include <sys/user.h>
#include <sys/syscall.h>
#include <stdint.h>

#define LIB_NAME        "libudplib.so"
#define GAME_DEFAULT    "Banana Shooter.x86_64"

static char g_self_dir[4096];

static void find_self_dir(void)
{
    ssize_t n = readlink("/proc/self/exe", g_self_dir, sizeof(g_self_dir) - 1);
    if (n <= 0) { strcpy(g_self_dir, "."); return; }
    g_self_dir[n] = 0;
    char* slash = strrchr(g_self_dir, '/');
    if (slash) *slash = 0; else strcpy(g_self_dir, ".");
}

/* resolve absolute path of libudplib.so: $UDPLIB_LIB -> next to game ->
 * next to prepatcher -> cwd */
static int resolve_lib(const char* game_path, char* out, size_t cap)
{
    struct stat st;
    char buf[4096];
    const char* env = getenv("UDPLIB_LIB");
    if (env && stat(env, &st) == 0) { snprintf(out, cap, "%s", env); return 0; }

    char tmp[4096];
    strncpy(tmp, game_path, sizeof(tmp) - 1);
    tmp[sizeof(tmp) - 1] = 0;
    const char* dir = ".";
    char* sl = strrchr(tmp, '/');
    if (sl) { *sl = 0; dir = tmp; }

    snprintf(buf, sizeof(buf), "%s/%s", dir, LIB_NAME);
    if (stat(buf, &st) == 0) {
        if (!realpath(buf, out)) snprintf(out, cap, "%s", buf);
        return 0;
    }
    snprintf(buf, sizeof(buf), "%s/%s", g_self_dir, LIB_NAME);
    if (stat(buf, &st) == 0) { snprintf(out, cap, "%s", buf); return 0; }
    snprintf(buf, sizeof(buf), "./%s", LIB_NAME);
    if (stat(buf, &st) == 0) { snprintf(out, cap, "%s", buf); return 0; }
    return -1;
}

/* ------------------------------------------------------------------ attach */
#ifdef __x86_64__

static long pt_ok(int req, pid_t pid, void* addr, void* data)
{
    errno = 0;
    long r = ptrace(req, pid, addr, data);
    if (r == -1 && errno != 0) return -errno;
    return r;
}

static int cont_and_wait(pid_t pid)
{
    int status;
    if (ptrace(PTRACE_CONT, pid, 0, 0) == -1) return -errno;
    if (waitpid(pid, &status, __WALL) == -1) return -errno;
    if (!WIFSTOPPED(status)) return -ECHILD;
    return WSTOPSIG(status);
}

static int get_base_from_maps(pid_t pid, const char* soname, unsigned long* base)
{
    char path[64];
    snprintf(path, sizeof(path), "/proc/%d/maps", pid);
    FILE* f = fopen(path, "r");
    if (!f) return -1;
    char line[1024];
    int rc = -1;
    while (fgets(line, sizeof(line), f)) {
        unsigned long s, e; char perms[8], fn[512] = {0};
        if (sscanf(line, "%lx-%lx %7s %*s %*s %*s %511[^\n]",
                   &s, &e, perms, fn) == 4) {
            if (strstr(fn, soname) && strchr(perms, 'x')) { *base = s; rc = 0; break; }
        }
    }
    fclose(f);
    return rc;
}

static pid_t find_game_pid(const char* name_sub)
{
    char base[256];
    strncpy(base, name_sub, sizeof(base) - 1);
    base[sizeof(base) - 1] = 0;
    for (char* p = base; *p; ++p) *p = (char)tolower((unsigned char)*p);

    DIR* dir = opendir("/proc");
    if (!dir) return -1;
    struct dirent* de;
    pid_t found = -1;
    while ((de = readdir(dir)) != 0) {
        if (!(de->d_name[0] >= '0' && de->d_name[0] <= '9')) continue;
        pid_t pid = (pid_t)atoi(de->d_name);
        if (pid == getpid()) continue;
        char link[64], exe[1024];
        snprintf(link, sizeof(link), "/proc/%d/exe", pid);
        ssize_t n = readlink(link, exe, sizeof(exe) - 1);
        if (n <= 0) continue;
        exe[n] = 0;
        char* bn = strrchr(exe, '/');
        bn = bn ? bn + 1 : exe;
        char low[256];
        strncpy(low, bn, sizeof(low) - 1);
        low[sizeof(low) - 1] = 0;
        for (char* p = low; *p; ++p) *p = (char)tolower((unsigned char)*p);
        if (strstr(low, base)) { found = pid; break; }
    }
    closedir(dir);
    return found;
}

/* perform one remote syscall in the stopped tracee; returns its rax
 * (kernel already returns -errno for failures) */
static long remote_syscall(pid_t pid, const struct user_regs_struct* scratch,
                           long nr, long a1, long a2, long a3, long a4, long a5, long a6,
                           unsigned long gadget)
{
    struct user_regs_struct regs = *scratch;
    regs.rip = gadget;
    regs.rax = (unsigned long)nr;
    regs.rdi = (unsigned long)a1;
    regs.rsi = (unsigned long)a2;
    regs.rdx = (unsigned long)a3;
    regs.r10 = (unsigned long)a4;
    regs.r8  = (unsigned long)a5;
    regs.r9  = (unsigned long)a6;
    if (pt_ok(PTRACE_SETREGS, pid, 0, &regs) < 0) return -EIO;
    int sig = cont_and_wait(pid);
    if (sig < 0) return sig;
    if (pt_ok(PTRACE_GETREGS, pid, 0, &regs) < 0) return -EIO;
    /* after `syscall` the int3 stops us exactly 2 bytes past it */
    if (regs.rip != gadget + 2) {
        fprintf(stderr, "[linux_prepatch] unexpected rip after remote syscall %ld "
                        "(sig=%d) - aborting injection\n", nr, sig);
        return -EPROTO;
    }
    return (long)regs.rax;
}

static int attach_inject(pid_t pid, const char* lib)
{
    if (ptrace(PTRACE_ATTACH, pid, 0, 0) == -1) {
        fprintf(stderr, "[linux_prepatch] PTRACE_ATTACH pid %d failed: %s\n"
                        "                  (check /proc/sys/kernel/yama/ptrace_scope)\n",
                pid, strerror(errno));
        return -1;
    }
    int status;
    if (waitpid(pid, &status, __WALL) == -1 || !WIFSTOPPED(status)) {
        ptrace(PTRACE_DETACH, pid, 0, 0);
        return -2;
    }

    struct user_regs_struct orig, scratch;
    if (pt_ok(PTRACE_GETREGS, pid, 0, &orig) < 0) {
        ptrace(PTRACE_DETACH, pid, 0, 0);
        return -3;
    }

    /* --- locate `syscall; int3` inside the target's libc text --- */
    unsigned long tgt_libc = 0;
    if (get_base_from_maps(pid, "libc.so.6", &tgt_libc) != 0) {
        fprintf(stderr, "[linux_prepatch] cannot find libc.so.6 mapping in target\n");
        ptrace(PTRACE_DETACH, pid, 0, 0);
        return -4;
    }
    unsigned long gadget = 0;
    for (unsigned long off = tgt_libc + 0x20000; off < tgt_libc + 0x200000; ++off) {
        errno = 0;
        long word = ptrace(PTRACE_PEEKTEXT, pid, (void*)off, 0);
        if (errno) break;
        if (((uint64_t)word & 0xFFFFFFull) == 0xCC050Full) { gadget = off; break; }
    }
    if (!gadget) {
        fprintf(stderr, "[linux_prepatch] no 'syscall;int3' gadget found in target libc\n");
        ptrace(PTRACE_DETACH, pid, 0, 0);
        return -5;
    }

    /* --- remote mmap a page to hold the path string --- */
    scratch = orig;
    long mem = remote_syscall(pid, &scratch, SYS_mmap, 0, 4096,
                              PROT_READ | PROT_WRITE,
                              MAP_PRIVATE | MAP_ANONYMOUS, -1, 0, gadget);
    if (mem < 0) {
        fprintf(stderr, "[linux_prepatch] remote mmap failed: %ld (%s)\n",
                mem, strerror((int)-mem));
        ptrace(PTRACE_DETACH, pid, 0, 0);
        return -6;
    }
    uintptr_t remote_mem = (uintptr_t)mem;

    size_t pathlen = strlen(lib) + 1;
    if (pathlen > 4096) {
        scratch = orig; scratch.rip = gadget + 2;
        remote_syscall(pid, &scratch, SYS_munmap, (long)remote_mem, 4096, 0,0,0,0, gadget);
        ptrace(PTRACE_DETACH, pid, 0, 0);
        return -7;
    }
    for (size_t off = 0; off < pathlen; off += 8) {
        uint64_t word = 0;
        size_t rem = pathlen - off;
        memcpy(&word, lib + off, rem < 8 ? rem : 8);
        errno = 0;
        ptrace(PTRACE_POKEDATA, pid, (void*)(remote_mem + off), (void*)word);
        if (errno) {
            scratch = orig; scratch.rip = gadget + 2;
        remote_syscall(pid, &scratch, SYS_munmap, (long)remote_mem, 4096, 0,0,0,0, gadget);
            ptrace(PTRACE_DETACH, pid, 0, 0);
            return -8;
        }
    }

    /* --- resolve dlopen's address inside the target: same machine means the
     * same libdl/libc build, so local file-offset transfers 1:1 onto the
     * target's mapping base read from /proc/<pid>/maps --- */
    void* local_dlopen = (void*)dlopen;
    Dl_info di;
    unsigned long tgt_dlopen = 0;
    if (dladdr(local_dlopen, &di) && di.dli_fname && di.dli_fbase) {
        const char* soname = strrchr(di.dli_fname, '/');
        soname = soname ? soname + 1 : di.dli_fname;
        unsigned long tb = 0;
        if (get_base_from_maps(pid, soname, &tb) == 0)
            tgt_dlopen = tb + ((unsigned long)local_dlopen - (unsigned long)di.dli_fbase);
    }
    if (!tgt_dlopen) {
        fprintf(stderr, "[linux_prepatch] cannot resolve target dlopen address\n");
        scratch = orig; scratch.rip = gadget + 2;
        remote_syscall(pid, &scratch, SYS_munmap, (long)remote_mem, 4096, 0,0,0,0, gadget);
        ptrace(PTRACE_DETACH, pid, 0, 0);
        return -9;
    }

    /* --- execute dlopen(remote_mem, RTLD_NOW) on the target's own stack:
     * push a NULL return address; when dlopen RETs the target faults with
     * SIGSEGV and rax still holds the handle --- */
    struct user_regs_struct regs = orig;
    regs.rip = tgt_dlopen;
    regs.rdi = remote_mem;
    regs.rsi = RTLD_NOW;
    regs.rsp -= 8;
    regs.rsp &= ~0xFull; /* keep 16-byte aligned ABI frame */
    errno = 0;
    ptrace(PTRACE_POKEDATA, pid, (void*)(uintptr_t)regs.rsp, (void*)(uintptr_t)0);
    if (errno || pt_ok(PTRACE_SETREGS, pid, 0, &regs) < 0) {
        scratch = orig; scratch.rip = gadget + 2;
        remote_syscall(pid, &scratch, SYS_munmap, (long)remote_mem, 4096, 0,0,0,0, gadget);
        ptrace(PTRACE_DETACH, pid, 0, 0);
        return -10;
    }
    int sig = cont_and_wait(pid);
    struct user_regs_struct after;
    if (pt_ok(PTRACE_GETREGS, pid, 0, &after) < 0) {
        ptrace(PTRACE_DETACH, pid, 0, 0);
        return -11;
    }
    uintptr_t handle = (uintptr_t)after.rax;
    if (sig != SIGSEGV) {
        fprintf(stderr, "[linux_prepatch] warning: expected SIGSEGV after dlopen,"
                        " got signal %d\n", sig);
    }

    /* --- cleanup + full register restore. The remote-syscall helper always
     * starts from the pristine `orig` snapshot and leaves regs.rip ==
     * gadget+2 afterwards, so we must re-read that scratch rip before the
     * next call (the tracee is still stopped; orig itself is never written
     * back until the final restore below). --- */
    scratch = orig;
    scratch.rip = gadget + 2;
    remote_syscall(pid, &scratch, SYS_munmap, (long)remote_mem, 4096, 0,0,0,0, gadget);
    if (pt_ok(PTRACE_SETREGS, pid, 0, &orig) < 0) {
        ptrace(PTRACE_DETACH, pid, 0, 0);
        return -12;
    }
    ptrace(PTRACE_DETACH, pid, 0, 0);

    if (handle == 0) {
        fprintf(stderr, "[linux_prepatch] remote dlopen(\"%s\") returned NULL\n", lib);
        return -13;
    }
    printf("[linux_prepatch] loaded %s into pid %d (handle 0x%lx)\n",
           lib, (int)pid, (unsigned long)handle);
    return 0;
}

#else /* !x86_64 */
static int attach_inject(pid_t pid, const char* lib)
{
    (void)pid; (void)lib;
    fprintf(stderr, "[linux_prepatch] --attach ptrace injection is implemented for x86_64;\n"
                    "                  use the default LD_PRELOAD mode on this architecture.\n");
    return -1;
}
#endif /* __x86_64 */

/* ------------------------------------------------------------------ preload */
static int preload_launch(const char* game, char** args, int nargs, const char* lib)
{
    setenv("LD_PRELOAD", lib, 1);
    char* newargs[128];
    int n = 0;
    newargs[n++] = (char*)game;
    for (int i = 0; i < nargs && n < 126; ++i) newargs[n++] = args[i];
    newargs[n] = 0;
    fprintf(stderr, "[linux_prepatch] LD_PRELOAD=%s -> exec \"%s\"\n", lib, game);
    fflush(stderr);
    execv(game, newargs);
    fprintf(stderr, "[linux_prepatch] execv(%s) failed: %s\n", game, strerror(errno));
    return -1;
}

/* ------------------------------------------------------------------ main */
int main(int argc, char** argv)
{
    find_self_dir();

    int do_attach = 0;
    const char* game = 0;
    int argi = 1;
    if (argc > 1 && strcmp(argv[1], "--attach") == 0) {
        do_attach = 1;
        argi = 2;
    }
    if (argc > argi) game = argv[argi++];
    if (!game) game = GAME_DEFAULT;

    char lib[4096];
    if (resolve_lib(game, lib, sizeof(lib)) != 0) {
        fprintf(stderr, "[linux_prepatch] libudplib.so not found (looked next to game,\n"
                        "                  next to prepatcher, ./). Set UDPLIB_LIB=/abs/path.\n");
        return 2;
    }

    if (do_attach) {
        const char* bn = strrchr(game, '/');
        bn = bn ? bn + 1 : game;
        pid_t pid = find_game_pid(bn);
        if (pid < 0) pid = find_game_pid("banana");
        if (pid < 0) {
            fprintf(stderr, "[linux_prepatch] no running '%s' process found\n", bn);
            return 3;
        }
        return attach_inject(pid, lib) == 0 ? 0 : 4;
    }
    return preload_launch(game, argv + argi, argc - argi, lib);
}

#endif /* !_WIN32 */
