#!/bin/bash
# Commanding Performance Benchmark — runs all 3 variants (Duct, XamlCmd, Wpf)
# across all scenarios (mount, toggle, bulk) and collects results.
#
# Usage:
#   bash tests/cmd_perf/run_cmd_benchmark.sh [--iterations N]
#
# Run from repo root.

set -e

ITERATIONS=${1:-50}
BASE="tests/cmd_perf"
CONFIG="Release"
RESULTS_FILE="$BASE/cmd_perf_results.txt"

# Platform detection
if [[ "$(uname -m)" == "aarch64" ]] || [[ "$PROCESSOR_ARCHITECTURE" == "ARM64" ]]; then
    PLATFORM="ARM64"
else
    PLATFORM="x64"
fi

echo "=== Commanding Perf Benchmark ==="
echo "Platform: $PLATFORM | Config: $CONFIG | Iterations: $ITERATIONS"
echo ""

# Build all variants
echo "Building all variants..."
dotnet build "$BASE/CmdPerf.Duct/CmdPerf.Duct.csproj" -c $CONFIG -p:Platform=$PLATFORM --verbosity quiet
dotnet build "$BASE/CmdPerf.XamlCmd/CmdPerf.XamlCmd.csproj" -c $CONFIG -p:Platform=$PLATFORM --verbosity quiet
dotnet build "$BASE/CmdPerf.Wpf/CmdPerf.Wpf.csproj" -c $CONFIG -p:Platform=$PLATFORM --verbosity quiet
echo "Build complete."
echo ""

# Clear previous results
> "$RESULTS_FILE"

TFM_WINUI="net9.0-windows10.0.22621.0"
TFM_WPF="net9.0-windows"

DUCT_EXE="$BASE/CmdPerf.Duct/bin/$PLATFORM/$CONFIG/$TFM_WINUI/CmdPerf.Duct.exe"
XAML_EXE="$BASE/CmdPerf.XamlCmd/bin/$PLATFORM/$CONFIG/$TFM_WINUI/CmdPerf.XamlCmd.exe"
WPF_EXE="$BASE/CmdPerf.Wpf/bin/$PLATFORM/$CONFIG/$TFM_WPF/CmdPerf.Wpf.exe"

run_variant() {
    local name=$1
    local exe=$2
    local scenario=$3

    echo "Running $name --scenario $scenario ..."
    "$exe" --headless --scenario "$scenario" --iterations "$ITERATIONS" 2>/dev/null || true

    # Find and append report
    local report_dir=$(dirname "$exe")
    local report_file="$report_dir/$name.$scenario.report.txt"
    if [ -f "$report_file" ]; then
        cat "$report_file" >> "$RESULTS_FILE"
        echo "" >> "$RESULTS_FILE"
    else
        echo "WARNING: No report file found at $report_file" >&2
    fi
}

for scenario in mount toggle bulk; do
    echo "─── Scenario: $scenario ───"
    run_variant "CmdPerf.Duct"    "$DUCT_EXE" "$scenario"
    run_variant "CmdPerf.XamlCmd" "$XAML_EXE"  "$scenario"
    run_variant "CmdPerf.Wpf"     "$WPF_EXE"   "$scenario"
    echo ""
done

echo "═══════════════════════════════════════"
echo "Results written to $RESULTS_FILE"
echo "═══════════════════════════════════════"
echo ""
cat "$RESULTS_FILE"
