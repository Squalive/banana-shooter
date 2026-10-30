/* ==========================================================================
 * UDPLIB Windows platform backend (winsock2 + threads + CRITICAL_SECTION).
 * Built into udplib.dll by build.sh / CMakeLists.txt when targeting Windows.
 * ========================================================================== */
#ifdef _WIN32

#include "../common/udplib_platform.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

static int g_wsa_inited = 0;

void udplib_platform_init(void)
{
    if (!g_wsa_inited) {
        WSADATA wsa;
        if (WSAStartup(MAKEWORD(2, 2), &wsa) == 0)
            g_wsa_inited = 1;
    }
}

udplib_socket_t udplib_udp_bind(uint16_t port)
{
    SOCKET s = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
    if (s == INVALID_SOCKET) return INVALID_SOCKET;

    int on = 1;
    setsockopt(s, SOL_SOCKET, SO_REUSEADDR, (const char*)&on, sizeof(on));

    /* 50ms receive timeout so the listener loop can observe g_running flips */
    DWORD tv = 50;
    setsockopt(s, SOL_SOCKET, SO_RCVTIMEO, (const char*)&tv, sizeof(tv));

    struct sockaddr_in addr;
    memset(&addr, 0, sizeof(addr));
    addr.sin_family      = AF_INET;
    addr.sin_addr.s_addr = htonl(INADDR_ANY);
    addr.sin_port        = htons(port);

    if (bind(s, (struct sockaddr*)&addr, sizeof(addr)) == SOCKET_ERROR) {
        closesocket(s);
        return INVALID_SOCKET;
    }
    return s;
}

int udplib_udp_sendto(udplib_socket_t s, const char* ip, uint16_t port,
                      const void* data, size_t len)
{
    struct sockaddr_in addr;
    memset(&addr, 0, sizeof(addr));
    addr.sin_family = AF_INET;
    addr.sin_port   = htons(port);
    if (inet_pton(AF_INET, ip, &addr.sin_addr) != 1) return -1;
    int r = sendto(s, (const char*)data, (int)len, 0,
                   (struct sockaddr*)&addr, sizeof(addr));
    return r;
}

int udplib_udp_recvfrom(udplib_socket_t s, void* buf, size_t cap,
                        char* raddr, size_t raddrcap, uint16_t* rport)
{
    struct sockaddr_in from;
    int fromlen = (int)sizeof(from);
    int r = recvfrom(s, (char*)buf, (int)cap, 0, (struct sockaddr*)&from, &fromlen);
    if (r == SOCKET_ERROR) {
        int err = WSAGetLastError();
        if (err == WSAETIMEDOUT) return 0;
        return -1;
    }
    char tmp[46];
    inet_ntop(AF_INET, &from.sin_addr, tmp, sizeof(tmp));
    size_t n = raddrcap - 1;
    if (strlen(tmp) < n) n = strlen(tmp);
    memcpy(raddr, tmp, n);
    raddr[n] = 0;
    *rport = ntohs(from.sin_port);
    return r;
}

void udplib_closesocket(udplib_socket_t s)
{
    if (s != INVALID_SOCKET) closesocket(s);
}

void udplib_mutex_init(udplib_mutex_t* m)   { InitializeCriticalSection(m); }
void udplib_mutex_lock(udplib_mutex_t* m)   { EnterCriticalSection(m); }
void udplib_mutex_unlock(udplib_mutex_t* m) { LeaveCriticalSection(m); }
void udplib_mutex_destroy(udplib_mutex_t* m){ DeleteCriticalSection(m); }

typedef struct {
    void* (*fn)(void*);
    void* arg;
} thread_shim_t;

static DWORD WINAPI thread_trampoline(LPVOID p)
{
    thread_shim_t shim = *(thread_shim_t*)p;
    free(p);
    shim.fn(shim.arg);
    return 0;
}

int udplib_thread_start(udplib_thread_t* t, void* (*fn)(void*), void* arg)
{
    thread_shim_t* shim = (thread_shim_t*)malloc(sizeof(thread_shim_t));
    if (!shim) return -1;
    shim->fn = fn;
    shim->arg = arg;
    HANDLE h = CreateThread(0, 0, thread_trampoline, shim, 0, 0);
    if (!h) { free(shim); return -2; }
    *t = h;
    return 0;
}

void udplib_thread_join(udplib_thread_t* t)
{
    if (*t) {
        WaitForSingleObject(*t, INFINITE);
        CloseHandle(*t);
        *t = 0;
    }
}

uint64_t udplib_now_ms(void)
{
    ULONGLONG ticks = GetTickCount64();
    return (uint64_t)ticks;
}

void udplib_sleep_ms(int ms)
{
    Sleep((DWORD)ms);
}

#endif /* _WIN32 */
