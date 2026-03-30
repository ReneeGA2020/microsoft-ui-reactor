#!/bin/bash
set -e
APP_EXE="$1"
APP_NAME="$2"
TRACE_DIR="C:/Users/andersonch/Code/patch/stress_perf/traces"

echo "=== Starting $APP_NAME ==="
# Launch app in background
"$APP_EXE" --headless --percent 10 --duration 8 &
APP_PID=$!
echo "PID: $APP_PID"

# Wait for app to initialize
sleep 2

echo "Attaching dotnet-trace for 5 seconds..."
dotnet-trace collect \
  --process-id $APP_PID \
  --profile cpu-sampling \
  --duration 00:00:05 \
  --output "$TRACE_DIR/${APP_NAME}.nettrace" \
  2>&1

echo "Waiting for app to exit..."
wait $APP_PID 2>/dev/null || true
echo "=== $APP_NAME done ==="
