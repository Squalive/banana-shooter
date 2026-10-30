/* ==========================================================================
 * UDPLIB core - platform independent implementation.
 * Compiled into both the Windows DLL (udplib.dll) and the Linux .so
 * (libudplib.so). Only udplib_platform.{c,h} differ per OS.
 * ========================================================================== */
#include "udplib.h"
#include "udplib_platform.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#if !defined(UDPLIB_VERSION_STR)
#  define UDPLIB_VERSION_STR "1.0"
#endif
#if !defined(UDPLIB_BUILD_NUM)
#  define UDPLIB_BUILD_NUM 1
#endif
#if !defined(UDPLIB_BUILD_DATE)
#  define UDPLIB_BUILD_DATE "unknown"
#endif

#define UDPLIB_VERSION_INT ((int)(atoi(UDPLIB_VERSION_STR) * 100))

/* ---------------------------------------------------------------- client table */
typedef struct {
    int      used;
    char     ip[64];
    uint16_t port;
    uint64_t last_seen_ms;
} client_t;

static client_t          g_clients[UDPLIB_MAX_CLIENTS];
static udplib_mutex_t    g_clients_mtx;
static udplib_thread_t   g_listener_thread;
static udplib_socket_t   g_sock = UDPLIB_INVALID_SOCKET;
static volatile int      g_running = 0;
static uint16_t          g_port = 0;
static udplib_data_cb    g_data_cb = 0;

/* ---------------------------------------------------------------- metadata */
const char* udplib_info(char* buf, int buflen)
{
    if (!buf || buflen <= 0) return "";
    snprintf(buf, (size_t)buflen, "UDPLIB V%s Build %d from %s",
             UDPLIB_VERSION_STR, (int)UDPLIB_BUILD_NUM, UDPLIB_BUILD_DATE);
    return buf;
}

int udplib_version(void)       { return UDPLIB_VERSION_INT; }
int udplib_build(void)         { return (int)UDPLIB_BUILD_NUM; }
const char* udplib_build_date(void) { return UDPLIB_BUILD_DATE; }

void udplib_set_data_callback(udplib_data_cb cb)
{
    g_data_cb = cb;
}

/* ---------------------------------------------------------------- clients */
static client_t* client_find_or_add(const char* ip, uint16_t port, uint64_t now)
{
    client_t* free_slot = 0;
    for (uint32_t i = 0; i < UDPLIB_MAX_CLIENTS; ++i) {
        client_t* c = &g_clients[i];
        if (c->used && 0 == strcmp(c->ip, ip) && c->port == port) {
            c->last_seen_ms = now;
            return c;
        }
        if (!c->used && !free_slot) free_slot = c;
    }
    if (!free_slot) {
        /* evict the stalest client so the table never overflows */
        uint64_t oldest = ~0ull;
        for (uint32_t i = 0; i < UDPLIB_MAX_CLIENTS; ++i) {
            if (g_clients[i].last_seen_ms < oldest) {
                oldest = g_clients[i].last_seen_ms;
                free_slot = &g_clients[i];
            }
        }
    }
    memset(free_slot, 0, sizeof(*free_slot));
    free_slot->used = 1;
    strncpy(free_slot->ip, ip, sizeof(free_slot->ip) - 1);
    free_slot->port = port;
    free_slot->last_seen_ms = now;
    return free_slot;
}

static void client_remove(const char* ip, uint16_t port)
{
    udplib_mutex_lock(&g_clients_mtx);
    for (uint32_t i = 0; i < UDPLIB_MAX_CLIENTS; ++i) {
        client_t* c = &g_clients[i];
        if (c->used && 0 == strcmp(c->ip, ip) && c->port == port) {
            memset(c, 0, sizeof(*c));
            break;
        }
    }
    udplib_mutex_unlock(&g_clients_mtx);
}

int udplib_client_count(void)
{
    int n = 0;
    uint64_t now = udplib_now_ms();
    udplib_mutex_lock(&g_clients_mtx);
    for (uint32_t i = 0; i < UDPLIB_MAX_CLIENTS; ++i) {
        if (g_clients[i].used && (now - g_clients[i].last_seen_ms) < 30000ull)
            ++n;
    }
    udplib_mutex_unlock(&g_clients_mtx);
    return n;
}

/* ---------------------------------------------------------------- send path */
static int envelope_write(uint8_t* out, size_t outcap, uint8_t type, uint8_t seq,
                          const uint8_t* payload, size_t plen)
{
    if (outcap < sizeof(udplib_header_t) + plen) return -1;
    udplib_header_t* h = (udplib_header_t*)out;
    h->magic   = UDPLIB_MAGIC;
    h->version = UDPLIB_PROTO_VER;
    h->type    = type;
    h->flags   = 0;
    h->seq     = seq;
    if (plen) memcpy(out + sizeof(*h), payload, plen);
    return (int)(sizeof(*h) + plen);
}

int udplib_send_to_client(const char* ip, uint16_t port,
                          const uint8_t* data, size_t len)
{
    if (!g_running || !ip) return -1;
    uint8_t  pkt[UDPLIB_MAX_PACKET];
    uint8_t  seq = (uint8_t)(udplib_now_ms() & 0xFFu);
    int total = envelope_write(pkt, sizeof(pkt), UDPLIB_TYPE_RELAY, seq, data, len);
    if (total < 0) return -1;
    return udplib_udp_sendto(g_sock, ip, port, pkt, (size_t)total);
}

int udplib_broadcast_relay(const uint8_t* data, size_t len)
{
    if (!g_running) return 0;
    client_t snapshot[UDPLIB_MAX_CLIENTS];
    int n = 0;
    uint64_t now = udplib_now_ms();
    udplib_mutex_lock(&g_clients_mtx);
    for (uint32_t i = 0; i < UDPLIB_MAX_CLIENTS; ++i) {
        if (g_clients[i].used && (now - g_clients[i].last_seen_ms) < 30000ull)
            snapshot[n++] = g_clients[i];
    }
    udplib_mutex_unlock(&g_clients_mtx);

    int sent = 0;
    for (int i = 0; i < n; ++i) {
        if (udplib_send_to_client(snapshot[i].ip, snapshot[i].port, data, len) > 0)
            ++sent;
    }
    return sent;
}

/* ---------------------------------------------------------------- receive loop */
static void handle_packet(const uint8_t* buf, size_t len, const char* ip, uint16_t port)
{
    if (len < sizeof(udplib_header_t)) return;
    const udplib_header_t* h = (const udplib_header_t*)buf;
    if (h->magic != UDPLIB_MAGIC || h->version != UDPLIB_PROTO_VER) return;

    const uint8_t* payload = buf + sizeof(udplib_header_t);
    size_t plen = len - sizeof(udplib_header_t);
    uint64_t now = udplib_now_ms();

    switch (h->type) {
        case UDPLIB_TYPE_PING: {
            uint8_t pkt[64];
            int total = envelope_write(pkt, sizeof(pkt), UDPLIB_TYPE_PONG, h->seq, 0, 0);
            if (total > 0) udplib_udp_sendto(g_sock, ip, port, pkt, (size_t)total);
            udplib_mutex_lock(&g_clients_mtx);
            client_find_or_add(ip, port, now);
            udplib_mutex_unlock(&g_clients_mtx);
            break;
        }
        case UDPLIB_TYPE_CLOSE:
            client_remove(ip, port);
            break;
        case UDPLIB_TYPE_DATA: {
            udplib_mutex_lock(&g_clients_mtx);
            client_find_or_add(ip, port, now);
            udplib_mutex_unlock(&g_clients_mtx);
            if (g_data_cb && plen > 0) {
                /* copy so the callback may keep/queue it freely */
                uint8_t tmp[UDPLIB_MAX_PACKET];
                if (plen <= sizeof(tmp)) {
                    memcpy(tmp, payload, plen);
                    g_data_cb(tmp, (int)plen);
                }
            }
            break;
        }
        default:
            break;
    }
}

static void* listener_main(void* arg)
{
    (void)arg;
    uint8_t buf[UDPLIB_MAX_PACKET];
    while (g_running) {
        char raddr[64];
        uint16_t rport = 0;
        int r = udplib_udp_recvfrom(g_sock, buf, sizeof(buf), raddr, sizeof(raddr), &rport);
        if (r < 0) {
            if (!g_running) break;
            udplib_sleep_ms(5);
            continue;
        }
        if (r == 0) continue;
        handle_packet(buf, (size_t)r, raddr, rport);
    }
    return 0;
}

/* ---------------------------------------------------------------- start/stop */
int udplib_start(uint16_t port)
{
    if (g_running) return 0;
    if (port == 0) port = UDPLIB_DEFAULT_PORT;

    udplib_platform_init();
    udplib_mutex_init(&g_clients_mtx);
    memset(g_clients, 0, sizeof(g_clients));

    g_sock = udplib_udp_bind(port);
    if (g_sock == UDPLIB_INVALID_SOCKET) return -1;
    g_port = port;
    g_running = 1;
    if (udplib_thread_start(&g_listener_thread, listener_main, 0) != 0) {
        g_running = 0;
        udplib_closesocket(g_sock);
        g_sock = UDPLIB_INVALID_SOCKET;
        return -2;
    }
    fprintf(stderr, "[udplib] listening on udp/0.0.0.0:%u\n", (unsigned)port);
    fflush(stderr);
    return 0;
}

void udplib_stop(void)
{
    if (!g_running) return;
    g_running = 0;
    udplib_closesocket(g_sock);
    g_sock = UDPLIB_INVALID_SOCKET;
    udplib_thread_join(&g_listener_thread);
    udplib_mutex_destroy(&g_clients_mtx);
}

int udplib_is_running(void)
{
    return g_running ? 1 : 0;
}
