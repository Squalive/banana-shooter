/* ==========================================================================
 * udplib.dll entry point (Windows). The injector drops this DLL into the
 * suspended game process; the loader calls DllMain BEFORE the game's own
 * code runs, so the UDP listener is up before Unity/Steam initialise.
 * ========================================================================== */
#ifdef _WIN32

#include "../common/udplib.h"
#include <windows.h>
#include <stdlib.h>

static void UDPLIB_CALL auto_init(void)
{
    /* port 0 -> UDPLIB_DEFAULT_PORT (42420); override with UDPLIB_PORT env */
    const char* env = getenv("UDPLIB_PORT");
    uint16_t port = 0;
    if (env) {
        long v = atol(env);
        if (v > 0 && v < 65536) port = (uint16_t)v;
    }
    udplib_start(port);
}

BOOL WINAPI DllMain(HINSTANCE hinst, DWORD reason, LPVOID reserved)
{
    (void)hinst; (void)reserved;
    switch (reason) {
        case DLL_PROCESS_ATTACH:
            DisableThreadLibraryCalls(hinst);
            auto_init();
            break;
        case DLL_PROCESS_DETACH:
            udplib_stop();
            break;
        default:
            break;
    }
    return TRUE;
}

#endif /* _WIN32 */
