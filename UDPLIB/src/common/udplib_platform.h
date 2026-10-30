/* ==========================================================================
 * UDPLIB platform abstraction - shared header for the per-OS backends.
 * Windows backend:  src/windows/udplib_platform.c  (winsock2)
 * Linux backend:    src/linux/udplib_platform.c    (POSIX sockets + pthreads)
 * ========================================================================== */
#ifndef UDPLIB_PLATFORM_H
#define UDPLIB_PLATFORM_H

#include <stdint.h>
#include <stddef.h>

#if defined(_WIN32)

#ifndef WIN32_LEAN_AND_MEAN
#  define WIN32_LEAN_AND_MEAN
#endif
#include <winsock2.h>
#include <windows.h>

typedef SOCKET        udplib_socket_t;
typedef HANDLE        udplib_thread_t;
typedef CRITICAL_SECTION udplib_mutex_t;
#define UDPLIB_INVALID_SOCKET INVALID_SOCKET

#else /* POSIX / GNU Linux */

#include <pthread.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <unistd.h>

typedef int           udplib_socket_t;
typedef pthread_t     udplib_thread_t;
typedef pthread_mutex_t udplib_mutex_t;
#define UDPLIB_INVALID_SOCKET (-1)

#endif

/* one-time process init (WSAStartup on windows, no-op elsewhere) */
void udplib_platform_init(void);

udplib_socket_t udplib_udp_bind(uint16_t port);
int  udplib_udp_sendto(udplib_socket_t s, const char* ip, uint16_t port,
                       const void* data, size_t len);
/* returns bytes read (>0), 0 on timeout, <0 on error/closed */
int  udplib_udp_recvfrom(udplib_socket_t s, void* buf, size_t cap,
                         char* raddr, size_t raddrcap, uint16_t* rport);
void udplib_closesocket(udplib_socket_t s);

void udplib_mutex_init(udplib_mutex_t* m);
void udplib_mutex_lock(udplib_mutex_t* m);
void udplib_mutex_unlock(udplib_mutex_t* m);
void udplib_mutex_destroy(udplib_mutex_t* m);

int  udplib_thread_start(udplib_thread_t* t, void* (*fn)(void*), void* arg);
void udplib_thread_join(udplib_thread_t* t);

uint64_t udplib_now_ms(void);
void     udplib_sleep_ms(int ms);

#endif /* UDPLIB_PLATFORM_H */
