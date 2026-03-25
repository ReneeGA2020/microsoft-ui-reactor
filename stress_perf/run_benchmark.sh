#!/bin/bash
# StressPerf Benchmark Suite
# Runs all 5 variants at 10%-100% update rates, 7s each, outputs CSV.

set -e

DURATION=7
OUTFILE="C:/Users/andersonch/Code/patch/stress_perf/benchmark_results.csv"

DIRECT_EXE="C:/Users/andersonch/Code/patch/stress_perf/StressPerf.Direct/bin/ARM64/Release/net8.0-windows10.0.22621.0/StressPerf.Direct.exe"
BOUND_EXE="C:/Users/andersonch/Code/patch/stress_perf/StressPerf.Bound/bin/ARM64/Release/net8.0-windows10.0.22621.0/StressPerf.Bound.exe"
DUCT_EXE="C:/Users/andersonch/Code/patch/stress_perf/StressPerf.Duct/bin/ARM64/Release/net8.0-windows10.0.22621.0/StressPerf.Duct.exe"
WPF_EXE="C:/Users/andersonch/Code/patch/stress_perf/StressPerf.Wpf/bin/ARM64/Release/net8.0-windows/StressPerf.Wpf.exe"
DIRECTX_EXE="C:/Users/andersonch/Code/patch/stress_perf/StressPerf.DirectX/bin/ARM64/Release/net8.0-windows10.0.22621.0/StressPerf.DirectX.exe"

# CSV header
echo "App,Percent,Duration_s,Avg_FPS,Min_FPS,Max_FPS,Avg_Update_ms,Max_Update_ms,Avg_Memory_MB,Peak_Memory_MB" > "$OUTFILE"

parse_report() {
    local file="$1"
    local app="$2"
    local pct="$3"

    if [ ! -f "$file" ]; then
        echo "$app,$pct,0,0,0,0,0,0,0,0" >> "$OUTFILE"
        return
    fi

    local duration=$(grep "Duration:" "$file" | awk '{print $NF}' | tr -d 's')
    local avg_fps=$(grep "Avg FPS:" "$file" | awk '{print $NF}')
    local min_fps=$(grep "Min FPS:" "$file" | awk '{print $NF}')
    local max_fps=$(grep "Max FPS:" "$file" | awk '{print $NF}')
    local avg_update=$(grep "Avg Update:" "$file" | awk '{print $(NF-1)}')
    local max_update=$(grep "Max Update:" "$file" | awk '{print $(NF-1)}')
    local avg_mem=$(grep "Avg Memory:" "$file" | awk '{print $(NF-1)}')
    local peak_mem=$(grep "Peak Memory:" "$file" | awk '{print $(NF-1)}')

    echo "$app,$pct,$duration,$avg_fps,$min_fps,$max_fps,$avg_update,$max_update,$avg_mem,$peak_mem" >> "$OUTFILE"
}

run_app() {
    local exe="$1"
    local name="$2"
    local pct="$3"
    local exe_dir
    exe_dir=$(dirname "$exe")

    # Delete old report files
    find "$exe_dir" -maxdepth 1 -iname "*.report.txt" -delete 2>/dev/null || true

    echo "  Running $name @ ${pct}%..."
    "$exe" --headless --percent "$pct" --duration "$DURATION" 2>/dev/null || true

    # Find report file (case-insensitive to handle ARM64/arm64 mismatch)
    local report_file
    report_file=$(find "$exe_dir" -maxdepth 1 -iname "*.report.txt" -type f 2>/dev/null | head -1)
    [ -z "$report_file" ] && report_file=$(find "$exe_dir/.." -iname "*.report.txt" -type f 2>/dev/null | head -1)

    parse_report "$report_file" "$name" "$pct"
}

echo "=== StressPerf Benchmark Suite ==="
echo "Duration per run: ${DURATION}s"
echo "Output: $OUTFILE"
echo ""

for pct in 10 20 30 40 50 60 70 80 90 100; do
    echo "--- ${pct}% update rate ---"
    run_app "$WPF_EXE"    "WPF.Direct"   "$pct"
    run_app "$DIRECT_EXE"  "WinUI.Direct" "$pct"
    run_app "$BOUND_EXE"   "WinUI.Bound"  "$pct"
    run_app "$DUCT_EXE"    "WinUI.Duct"   "$pct"
    run_app "$DIRECTX_EXE" "WinUI.DirectX" "$pct"
    echo ""
done

echo "=== Done ==="
echo "Results written to: $OUTFILE"
echo ""
cat "$OUTFILE"
