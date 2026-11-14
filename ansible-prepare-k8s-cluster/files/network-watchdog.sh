#!/bin/bash
set -euo pipefail

MAX_FAILURES=3
STATE_FILE="/run/network-watchdog-failures"

# Target hosts on local network to verify eth0 connectivity
TARGETS=("192.168.1.1" "192.168.1.120" "192.168.1.119" "192.168.1.121")

LOCAL_IP=$(ip -4 -o addr show dev eth0 2>/dev/null | grep -oP "inet \K[\d.]+" || true)

ONLINE=0
for target in "${TARGETS[@]}"; do
  if [ -n "$LOCAL_IP" ] && [ "$target" = "$LOCAL_IP" ]; then
    continue
  fi
  if ping -c 1 -W 2 -I eth0 "$target" >/dev/null 2>&1; then
    ONLINE=1
    break
  fi
done

if [ "$ONLINE" -eq 1 ]; then
  if [ -f "$STATE_FILE" ]; then
    PREV_FAILS=$(cat "$STATE_FILE" 2>/dev/null || echo 0)
    rm -f "$STATE_FILE"
    logger -t network-watchdog -p daemon.info "Network recovered after $PREV_FAILS failure(s)."
  fi
  exit 0
fi

FAILS=0
if [ -f "$STATE_FILE" ]; then
  FAILS=$(cat "$STATE_FILE" 2>/dev/null || echo 0)
fi
FAILS=$((FAILS + 1))
echo "$FAILS" > "$STATE_FILE"

logger -t network-watchdog -p daemon.warn "Network check failed ($FAILS/$MAX_FAILURES): all LAN targets unreachable on eth0."

if [ "$FAILS" -lt "$MAX_FAILURES" ]; then
  logger -t network-watchdog -p daemon.notice "Attempting eth0 link reset..."
  ip link set eth0 down || true
  sleep 2
  ip link set eth0 up || true
else
  logger -t network-watchdog -p daemon.crit "Network unreachable for $FAILS consecutive checks. Force rebooting node!"
  sync
  systemctl reboot || reboot -f
fi
