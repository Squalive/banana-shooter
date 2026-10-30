/* ==========================================================================
 * libudplib.so entry point (GNU/Linux). Loaded via LD_PRELOAD by the
 * prepatcher BEFORE the game's main() runs; the ELF constructor below
 * starts the UDP listener immediately. The same object also exports the
 * udplib_* C API for P/Invoke from the managed bridge.
 * ========================================================================== */
#ifndef _WIN32

#include "../common/udplib.h"
#include <stdlib.h>
#include <stdio.h>

__attribute__((constructor))
static void udplib_ctor(void)
{
    const char* env = getenv("UDPLIB_PORT");
    uint16_t port = 0;
    if (env) {
        long v = atol(env);
        if (v > 0 && v < 65536) port = (uint16_t)v;
    }
    if (getenv("UDPLIB_DISABLE") == NULL) /* escape hatch for debugging */
        udplib_start(port);
}

__attribute__((destructor))
static void udplib_dtor(void)
{
    udplib_stop();
}

#endif /* !_WIN32 */
