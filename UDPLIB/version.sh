#!/usr/bin/env bash
# ============================================================================
# UDPLIB build versioning helper.
#
# Rules (as required):
#   * The version lives in UDPLIB/VERSION as a single line: "1.0"
#   * The build number lives in UDPLIB/.build .
#     If .build does not exist -> the build number is 1 and gets written there.
#     Every call to `bump` increases it by one.
#   * The build date lives in UDPLIB/.builddate (UTC ISO-8601, date only).
#     It is stamped at the moment a build is numbered.
#
# Usage:
#   ./version.sh get    -> prints "<version> <build> <date>" (creates them if missing)
#   ./version.sh bump   -> increments build number, re-stamps date, prints same
#   ./version.sh version|build|date -> print a single field
# ============================================================================
set -euo pipefail

UDPLIB_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VERSION_FILE="${UDPLIB_DIR}/VERSION"
BUILD_FILE="${UDPLIB_DIR}/.build"
DATE_FILE="${UDPLIB_DIR}/.builddate"

ensure_version() {
    if [ ! -f "${VERSION_FILE}" ]; then
        echo "1.0" > "${VERSION_FILE}"
    fi
}

ensure_build() {
    ensure_version
    # Requirement: if there is no .build file with a build number -> it is 1
    if [ ! -f "${BUILD_FILE}" ] || ! grep -qE '^[0-9]+$' "${BUILD_FILE}"; then
        echo "1" > "${BUILD_FILE}"
        date -u +"%Y-%m-%dT%H:%M:%SZ" > "${DATE_FILE}"
    fi
    if [ ! -f "${DATE_FILE}" ]; then
        date -u +"%Y-%m-%dT%H:%M:%SZ" > "${DATE_FILE}"
    fi
}

cmd="${1:-get}"
case "${cmd}" in
    get)
        ensure_build
        echo "$(cat "${VERSION_FILE}") $(cat "${BUILD_FILE}") $(cat "${DATE_FILE}")"
        ;;
    bump)
        ensure_build
        b="$(cat "${BUILD_FILE}")"
        echo "$(( b + 1 ))" > "${BUILD_FILE}"
        date -u +"%Y-%m-%dT%H:%M:%SZ" > "${DATE_FILE}"
        echo "$(cat "${VERSION_FILE}") $(cat "${BUILD_FILE}") $(cat "${DATE_FILE}")"
        ;;
    version)
        ensure_build; cat "${VERSION_FILE}" ;;
    build)
        ensure_build; cat "${BUILD_FILE}" ;;
    date)
        ensure_build; cat "${DATE_FILE}" ;;
    *)
        echo "usage: $0 {get|bump|version|build|date}" >&2
        exit 2
        ;;
esac
