/* ==========================================================================
 * UDPLIB GNU/Linux platform backend (POSIX sockets + pthreads).
 * Built into libudplib.so by build.sh / CMakeLists.txt when targeting Linux.
 * ========================================================================== */
#ifndef _WIN32

#include "../common/udplib_platform.h"
#include <errno.h>
#include <poll.h>
#include <stdio.h>
#include <string.h>
#include <time.h>

void udplib_platform_init(void)
{
    /* nothing to do on POSIX */
}

udplib_socket_t udplib_udp_bind(uint16_t port)
{
    int s = socket(AF_INET, SOCK_DGRAM, 0);
    if (s < 0) return UDPLIB_INVALID_SOCKET;

    int on = 1;
    setsockopt(s, SOL_SOCKET, SO_REUSEADDR, &on, sizeof(on));

    struct sockaddr_in addr;
    memset(&addr, 0, sizeof(addr));
    addr.sin_family      = AF_INET;
    addr.sin_addr.s_addr = htonl(INADDR_ANY);
    addr.sin_port        = htons(port);

    if (bind(s, (struct sockaddr*)&addr, sizeof(addr)) < 0) {
        close(s);
        return UDPLIB_INVALID_SOCKET;
    }
    return (udplib_socket_t)s;
}

int udplib_udp_sendto(udplib_socket_t s, const char* ip, uint16_t port,
                      const void* data, size_t len)
{
    struct sockaddr_in addr;
    memset(&addr, 0, sizeof(addr));
    addr.sin_family = AF_INET;
    addr.sin_port   = htons(port);
    if (inet_pton(AF_INET, ip, &addr.sin_addr) != 1) return -1;
    ssize_t r = sendto((int)s, data, len, 0, (struct sockaddr*)&addr, sizeof(addr));
    return (int)r;
}

int udplib_udp_recvfrom(udplib_socket_t s, void* buf, size_t cap,
                        char* raddr, size_t raddrcap, uint16_t* rport)
{
    struct pollfd pfd;
    pfd.fd = (int)s;
    pfd.events = POLLIN;
    pfd.revents = 0;
    int pr = poll(&pfd, 1, 50 /*ms*/);
    if (pr < 0) {
        if (errno == EINTR) return 0;
        return -1;
    }
    if (pr == 0) return 0; /* timeout -> loop checks g_running */

    struct sockaddr_in from;
    socklen_t fromlen = sizeof(from);
    ssize_t r = recvfrom((int)s, buf, cap, 0, (struct sockaddr*)&from, &fromlen);
    if (r < 0) {
        if (errno == EINTR || errno == EAGAIN || errno == EWOULDBLOCK) return 0;
        return -1; /* socket closed => caller stops the loop */
    }
    char tmp[INET_ADDRSTRLEN];
    inet_ntop(AF_INET, &from.sin_addr, tmp, sizeof(tmp));
    size_t n = raddrcap - 1;
    if (strlen(tmp) < n) n = strlen(tmp);
    memcpy(raddr, tmp, n);
    raddr[n] = 0;
    *rport = ntohs(from.sin_port);
    return (int)r;
}

void udplib_closesocket(udplib_socket_t s)
{
    if (s != UDPLIB_INVALID_SOCKET) close((int)s);
}

void udplib_mutex_init(udplib_mutex_t* m)    { pthread_mutex_init(m, 0); }
void udplib_mutex_lock(udplib_mutex_t* m)    { pthread_mutex_lock(m); }
void udplib_mutex_unlock(udplib_mutex_t* m)  { pthread_mutex_unlock(m); }
void udplib_mutex_destroy(udplib_mutex_t* m) { pthread_mutex_destroy(m); }

int udplib_thread_start(udplib_thread_t* t, void* (*fn)(void*), void* arg)
{
    return pthread_create(t, 0, fn, arg);
}

void udplib_thread_join(udplib_thread_t* t)
{
    pthread_join(*t, 0);
}

uint64_t udplib_now_ms(void)
{
    struct timespec ts;
    clock_gettime(CLOCK_MONOTONIC, &ts);
    return (uint64_t)ts.tv_sec * 1000ull + (uint64_t)(ts.tv_nsec / 1000000);
}

void udplib_sleep_ms(int ms)
{
    struct timespec ts;
    ts.tv_sec  = ms / 1000;
    ts.tv_nsec = (long)(ms % 1000) * 1000000L;
    nanosleep(&ts, 0);
}

#endif /* !_WIN32 */
