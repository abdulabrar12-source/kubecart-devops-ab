#!/usr/bin/env bash
# Start all 3 KubeCart microservices locally.
# Usage: ./start-local.sh
# Logs: /tmp/identity.log  /tmp/catalog.log  /tmp/order.log

ROOT="$(cd "$(dirname "$0")" && pwd)"

# Helper: load a .env file into the environment without sourcing it
# Works around spaces in values (e.g. "User Id=sa")
load_env() {
  local file="$1"
  while IFS= read -r line || [[ -n "$line" ]]; do
    # Skip blank lines and comments
    [[ -z "$line" || "$line" == \#* ]] && continue
    # Split only on the FIRST '='
    local key="${line%%=*}"
    local val="${line#*=}"
    # Strip surrounding double-quotes from the value
    val="${val%\"}"
    val="${val#\"}"
    export "$key=$val"
  done < "$file"
}

start_service() {
  local name="$1"
  local dir="$2"
  local log="$3"

  echo "▶  Starting $name …"
  (
    cd "$dir"
    load_env "$dir/.env"
    dotnet run > "$log" 2>&1
  ) &
  echo "   PID $!  →  $log"
}

# Kill any previously started dotnet services on our ports
for port in 5001 5002 5003; do
  pid=$(lsof -ti tcp:"$port" 2>/dev/null)
  if [[ -n "$pid" ]]; then
    echo "⚠  Port $port in use (PID $pid) — killing…"
    kill "$pid" 2>/dev/null
  fi
done

start_service "identity-service" "$ROOT/identity-service" "/tmp/identity.log"
start_service "catalog-service"  "$ROOT/catalog-service"  "/tmp/catalog.log"
start_service "order-service"    "$ROOT/order-service"    "/tmp/order.log"

echo ""
echo "Services starting — waiting 5 s for initial output…"
sleep 5

echo ""
echo "── identity-service (last 8 lines) ──────────────────"
tail -8 /tmp/identity.log
echo ""
echo "── catalog-service (last 8 lines) ───────────────────"
tail -8 /tmp/catalog.log
echo ""
echo "── order-service (last 8 lines) ─────────────────────"
tail -8 /tmp/order.log
echo ""
echo "Done.  Vite dev server: cd ui && npm run dev"
