/* ==========================================================================
 * UDPLIB - native helper library for Banana Shooter.
 *
 * Purpose:
 *   The compiled Banana Shooter game (Unity + Steamworks.NET) talks to its
 *   players through Steam Networking Sockets. A "pure" UDP client (a bot,
 *   a test harness, anything that is NOT running Steam) cannot connect to
 *   such a server.
 *
 *   UDPLIB is injected into the game process at startup (see prepatcher /
 *   win_prepatch.c and linux_prepatch.c). It opens a plain UDP listener on
 *   port 42420 ("BSUD" protocol, see PROTOCOL.md) and bridges every packet
 *   it receives from a pure-UDP client into the game by handing the payload
 *   to the managed bridge (UDPLIB.Managed.dll), which forwards it into the
 *   Riptide stream of the currently connected Steam connection. Replies
 *   travel back out over plain UDP.
 *
 *   The library also exposes udplib_info() so tooling (and the in-game
 *   console command `udplib`) can print:
 *       "UDPLIB V<version> Build <build> from <date>"
 * ========================================================================== */
#ifndef UDPLIB_H
#define UDPLIB_H

#include <stdint.h>
#include <stddef.h>

#if defined(_WIN32) || defined(__CYGWIN__)
#  ifdef UDPLIB_BUILD
#    define UDPLIB_API __declspec(dllexport)
#  else
#    define UDPLIB_API __declspec(dllimport)
#  endif
#  define UDPLIB_CALL __cdecl
#else
#  define UDPLIB_API __attribute__((visibility("default")))
#  define UDPLIB_CALL
#endif

#ifdef __cplusplus
extern "C" {
#endif

/* ---- wire protocol (see UDPLIB/PROTOCOL.md) -----------------------------
 * Client -> Server envelope:
 *   magic u32 = 'B','S','U','D' | version u8=1 | type u8 | flags u8 | seq u8
 *   then payload bytes (Riptide-format message body, or empty).
 * Server -> Client envelope: same header, payload = raw Riptide send-data.
 * ------------------------------------------------------------------------- */
#define UDPLIB_MAGIC        0x44555342u /* "BSUD" little endian */
#define UDPLIB_PROTO_VER    1u

#define UDPLIB_TYPE_PING    0u  /* answered with PONG, no game involvement */
#define UDPLIB_TYPE_PONG    1u
#define UDPLIB_TYPE_DATA    2u  /* payload -> injected into the game netcode */
#define UDPLIB_TYPE_RELAY   3u  /* payload <- captured game broadcast      */
#define UDPLIB_TYPE_CLOSE   4u  /* client disconnect notice                */

#define UDPLIB_DEFAULT_PORT 42420u
#define UDPLIB_MAX_CLIENTS  64u
#define UDPLIB_MAX_PACKET   1400u

typedef struct {
    uint32_t magic;
    uint8_t  version;
    uint8_t  type;
    uint8_t  flags;
    uint8_t  seq;
} udplib_header_t;

/* ---- exported C API ------------------------------------------------------ */

/* Fills buf with "UDPLIB V<version> Build <build> from <date>".
 * Returns buf. This exact string is what the in-game `udplib` console
 * command prints - both sides must agree, so the managed side calls this
 * function through P/Invoke instead of duplicating the format. */
UDPLIB_API const char* UDPLIB_CALL udplib_info(char* buf, int buflen);

/* Library version triplet / build metadata (compile-time constants). */
UDPLIB_API int  UDPLIB_CALL udplib_version(void);   /* e.g. 100 = v1.0      */
UDPLIB_API int  UDPLIB_CALL udplib_build(void);     /* .build counter value */
UDPLIB_API const char* UDPLIB_CALL udplib_build_date(void);

/* Start the UDP listener + relay thread. port==0 -> UDPLIB_DEFAULT_PORT.
 * Returns 0 on success, negative errno-like code on failure. Idempotent. */
UDPLIB_API int UDPLIB_CALL udplib_start(uint16_t port);

/* Stop listener and join thread. Safe to call multiple times. */
UDPLIB_API void UDPLIB_CALL udplib_stop(void);

/* 1 if udplib_start succeeded and the socket is alive. */
UDPLIB_API int UDPLIB_CALL udplib_is_running(void);

/* Number of currently known pure-UDP clients (seen within 30s). */
UDPLIB_API int UDPLIB_CALL udplib_client_count(void);

/* Send arbitrary bytes to one client over plain UDP (server->client path).
 * Called from the managed bridge when the game answers a UDP client. */
UDPLIB_API int UDPLIB_CALL udplib_send_to_client(const char* ip, uint16_t port,
                                                 const uint8_t* data, size_t len);

/* Broadcast raw game data (a Riptide "send-data" blob) to every UDP client
 * as a RELAY envelope. Called from the managed bridge's postfix hook on
 * Riptide SenderThread.SendData. */
UDPLIB_API int UDPLIB_CALL udplib_broadcast_relay(const uint8_t* data, size_t len);

/* Register the managed callback that receives inbound UDP DATA payloads.
 * Signature: void(uint8_t* payload, int len). The managed side registers it
 * with Marshal.GetFunctionPointerForDelegate; the pointer stays valid while
 * the delegate is kept alive on the managed side. */
typedef void (UDPLIB_CALL *udplib_data_cb)(uint8_t* payload, int len);
UDPLIB_API void UDPLIB_CALL udplib_set_data_callback(udplib_data_cb cb);

#ifdef __cplusplus
}
#endif

#endif /* UDPLIB_H */
