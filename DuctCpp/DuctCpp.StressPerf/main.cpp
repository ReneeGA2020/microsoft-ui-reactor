// DuctCpp StressPerf — Stock ticker grid benchmark using the DuctCpp framework.
// C++ port of stress_perf/StressPerf.Duct/Program.cs
// Phase 9 of the DuctCpp implementation.

#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <windows.h>
#include <shellapi.h>
#include <psapi.h>

#pragma comment(lib, "Shell32.lib")
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Microsoft.UI.Dispatching.h>
#include <winrt/Microsoft.UI.Xaml.Media.h>
#include <format>
#include <algorithm>
#include <numeric>
#include <random>
#include <chrono>
#include <string>
#include <vector>
#include <fstream>
#include <cmath>
#include <duct/duct.h>

using namespace duct;

// ─── CLI Options ──────────────────────────────────────────────────────────────

struct CliOptions {
    double percent = 10.0;
    int duration_seconds = 10;
    bool headless = false;

    static CliOptions parse(int argc, wchar_t* argv[]) {
        CliOptions opts;
        for (int i = 1; i < argc; i++) {
            std::wstring arg = argv[i];
            if (arg == L"--percent" && i + 1 < argc) {
                opts.percent = std::wcstod(argv[++i], nullptr);
            } else if (arg == L"--duration" && i + 1 < argc) {
                opts.duration_seconds = std::wcstol(argv[++i], nullptr, 10);
            } else if (arg == L"--headless") {
                opts.headless = true;
            }
        }
        return opts;
    }
};

static CliOptions g_cli_options;

// ─── Stock Data Source ────────────────────────────────────────────────────────

struct StockItem {
    std::string symbol;
    double prev_price;
    double current_price;
    bool is_up;

    bool operator==(const StockItem& o) const {
        return symbol == o.symbol && current_price == o.current_price && is_up == o.is_up;
    }
};

class StockDataSource {
public:
    static constexpr int Columns = 70;
    static constexpr int Rows = 70;
    static constexpr int TotalItems = Columns * Rows;

    StockDataSource() : rng_(42) {
        items_.resize(TotalItems);
        std::uniform_real_distribution<double> price_dist(0.0, 1.0);

        for (int i = 0; i < TotalItems; i++) {
            int row = i / Columns;
            int col = i % Columns;
            char c1 = static_cast<char>('A' + (row % 26));
            char c2 = static_cast<char>('A' + (col / 3 % 26));
            char c3 = static_cast<char>('A' + (col % 26));

            std::string symbol = { c1, c2, c3 };
            double price = std::round((10.0 + price_dist(rng_) * 990.0) * 100.0) / 100.0;
            items_[i] = { symbol, price, price, true };
        }
    }

    // Mutate a percentage of items. Returns changed indices.
    std::vector<int> update(double percent) {
        int count = std::max(1, static_cast<int>(TotalItems * percent / 100.0));
        std::vector<int> changed;
        changed.reserve(count);

        std::uniform_int_distribution<int> idx_dist(0, TotalItems - 1);
        std::uniform_real_distribution<double> delta_dist(0.0, 1.0);

        for (int i = 0; i < count; i++) {
            int idx = idx_dist(rng_);
            auto& item = items_[idx];
            // +/- up to 2%, biased slightly upward (0.48 center)
            double delta = ((delta_dist(rng_) - 0.48) * 2.0) * item.current_price * 0.02;
            double new_price = std::max(0.01, std::round((item.current_price + delta) * 100.0) / 100.0);
            item.prev_price = item.current_price;
            item.current_price = new_price;
            item.is_up = new_price >= item.prev_price;
            changed.push_back(idx);
        }
        return changed;
    }

    // Snapshot for Duct state (copy of items)
    std::vector<StockItem> snapshot() const {
        return items_;
    }

    const std::vector<StockItem>& items() const { return items_; }

    static std::string format_cell(const StockItem& item) {
        return std::format("{} {:.2f}", item.symbol, item.current_price);
    }

private:
    std::vector<StockItem> items_;
    std::mt19937 rng_;
};

// ─── Perf Tracker ─────────────────────────────────────────────────────────────

class PerfTracker {
public:
    PerfTracker() {
        QueryPerformanceFrequency(&freq_);
        QueryPerformanceCounter(&wall_start_);
        last_sample_time_ = 0.0;
    }

    // Call from CompositionTarget.Rendering to count composed frames.
    void frame_rendered() {
        frame_count_++;
        double now = elapsed_seconds();
        double elapsed = now - last_sample_time_;
        if (elapsed >= 1.0) {
            current_fps_ = frame_count_ / elapsed;
            fps_samples_.push_back(current_fps_);

            PROCESS_MEMORY_COUNTERS pmc{};
            pmc.cb = sizeof(pmc);
            if (GetProcessMemoryInfo(GetCurrentProcess(), &pmc, sizeof(pmc))) {
                memory_samples_.push_back(static_cast<long long>(pmc.WorkingSetSize));
            }

            frame_count_ = 0;
            last_sample_time_ = now;
        }
    }

    // Call before updating data + UI.
    void begin_update() {
        QueryPerformanceCounter(&update_start_);
    }

    // Call after updating data + UI.
    void end_update() {
        LARGE_INTEGER now;
        QueryPerformanceCounter(&now);
        last_update_ms_ = static_cast<double>(now.QuadPart - update_start_.QuadPart)
                          * 1000.0 / freq_.QuadPart;
        update_time_samples_.push_back(last_update_ms_);
    }

    double current_fps() const { return current_fps_; }
    double last_update_ms() const { return last_update_ms_; }

    long long current_memory_mb() const {
        PROCESS_MEMORY_COUNTERS pmc{};
        pmc.cb = sizeof(pmc);
        if (GetProcessMemoryInfo(GetCurrentProcess(), &pmc, sizeof(pmc))) {
            return static_cast<long long>(pmc.WorkingSetSize) / (1024 * 1024);
        }
        return 0;
    }

    double elapsed_seconds() const {
        LARGE_INTEGER now;
        QueryPerformanceCounter(&now);
        return static_cast<double>(now.QuadPart - wall_start_.QuadPart) / freq_.QuadPart;
    }

    std::string get_report(const std::string& app_name, double percent) const {
        if (fps_samples_.empty()) return "No data collected.\n";

        double avg_fps = std::accumulate(fps_samples_.begin(), fps_samples_.end(), 0.0) / fps_samples_.size();
        double min_fps = *std::min_element(fps_samples_.begin(), fps_samples_.end());
        double max_fps = *std::max_element(fps_samples_.begin(), fps_samples_.end());

        std::string report;
        report += std::format("=== {} ===\n", app_name);
        report += std::format("Duration:    {:.1f}s\n", elapsed_seconds());
        report += std::format("Percent:     {:.0f}%\n", percent);
        report += std::format("Avg FPS:     {:.1f}\n", avg_fps);
        report += std::format("Min FPS:     {:.1f}\n", min_fps);
        report += std::format("Max FPS:     {:.1f}\n", max_fps);

        if (!update_time_samples_.empty()) {
            double avg_update = std::accumulate(update_time_samples_.begin(), update_time_samples_.end(), 0.0)
                                / update_time_samples_.size();
            double max_update = *std::max_element(update_time_samples_.begin(), update_time_samples_.end());
            report += std::format("Avg Update:  {:.1f} ms\n", avg_update);
            report += std::format("Max Update:  {:.1f} ms\n", max_update);
        }

        if (!memory_samples_.empty()) {
            double avg_mem = std::accumulate(memory_samples_.begin(), memory_samples_.end(), 0LL)
                             / static_cast<double>(memory_samples_.size()) / (1024.0 * 1024.0);
            double peak_mem = *std::max_element(memory_samples_.begin(), memory_samples_.end())
                              / (1024.0 * 1024.0);
            report += std::format("Avg Memory:  {:.1f} MB\n", avg_mem);
            report += std::format("Peak Memory: {:.1f} MB\n", peak_mem);
        }

        return report;
    }

    void write_report_file(const std::string& app_name, double percent) const {
        auto report = get_report(app_name, percent);

        // Write next to the executable
        wchar_t path_buf[MAX_PATH];
        GetModuleFileNameW(nullptr, path_buf, MAX_PATH);
        std::wstring exe_path(path_buf);
        auto last_slash = exe_path.find_last_of(L'\\');
        std::wstring dir = (last_slash != std::wstring::npos) ? exe_path.substr(0, last_slash + 1) : L"";

        // Convert app_name to wide for path
        std::wstring wide_name(app_name.begin(), app_name.end());
        std::wstring report_path = dir + wide_name + L".report.txt";

        std::ofstream out(report_path);
        out << report;
    }

private:
    LARGE_INTEGER freq_{};
    LARGE_INTEGER wall_start_{};
    LARGE_INTEGER update_start_{};
    int frame_count_ = 0;
    double last_sample_time_ = 0.0;
    double current_fps_ = 0.0;
    double last_update_ms_ = 0.0;

    std::vector<double> fps_samples_;
    std::vector<long long> memory_samples_;
    std::vector<double> update_time_samples_;
};

// ─── StockGrid Component ─────────────────────────────────────────────────────

static constexpr const char* AppName = "StressPerf.DuctCpp";

class StockGridApp : public Component {
public:
    Element render() override {
        // Data source stored in a ref so it survives across renders
        auto source_ref = use_ref<std::shared_ptr<StockDataSource>>(nullptr);
        if (!source_ref->current) {
            source_ref->current = std::make_shared<StockDataSource>();
        }
        auto source = source_ref->current;

        // Generation counter triggers re-render; render reads source directly
        auto [gen, bump_gen] = use_reducer(0);

        auto [percent, set_percent] = use_state(g_cli_options.percent);
        auto [running, set_running] = use_state(false);
        auto [fps, set_fps] = use_state<std::string>("FPS: --");
        auto [update_ms, set_update_ms] = use_state<std::string>("Update: -- ms");
        auto [mem, set_mem] = use_state<std::string>("Mem: -- MB");

        auto perf_ref = use_ref<std::shared_ptr<PerfTracker>>(nullptr);
        auto timer_ref = use_ref<winrt::Microsoft::UI::Dispatching::DispatcherQueueTimer>(nullptr);
        auto shutdown_ref = use_ref<winrt::Microsoft::UI::Dispatching::DispatcherQueueTimer>(nullptr);

        // Lazily create PerfTracker
        if (!perf_ref->current) {
            perf_ref->current = std::make_shared<PerfTracker>();
        }

        // Hook CompositionTarget.Rendering for FPS counting
        auto render_hooked = use_ref(false);
        if (!render_hooked->current) {
            render_hooked->current = true;
            auto perf = perf_ref->current;
            winrt::Microsoft::UI::Xaml::Media::CompositionTarget::Rendering(
                [perf](auto&&, auto&&) { perf->frame_rendered(); });
        }

        // Start/stop update timer when running or percent changes
        use_effect([=]() -> std::function<void()> {
            if (running) {
                auto src = source_ref->current;
                auto perf = perf_ref->current;

                auto queue = winrt::Microsoft::UI::Dispatching::DispatcherQueue::GetForCurrentThread();
                if (!queue) return nullptr;

                auto timer = queue.CreateTimer();
                timer.Interval(std::chrono::milliseconds(33));
                timer.IsRepeating(true);

                timer.Tick([=](auto&&, auto&&) {
                    perf->begin_update();

                    src->update(percent);
                    bump_gen([](int g) { return g + 1; });

                    perf->end_update();

                    set_fps(std::format("FPS: {:.0f}", perf->current_fps()));
                    set_update_ms(std::format("Update: {:.1f} ms", perf->last_update_ms()));
                    set_mem(std::format("Mem: {} MB", perf->current_memory_mb()));
                });

                timer.Start();
                timer_ref->current = timer;
            } else {
                if (timer_ref->current) {
                    timer_ref->current.Stop();
                    timer_ref->current = nullptr;
                }
            }

            return [timer_ref]() {
                if (timer_ref->current) {
                    timer_ref->current.Stop();
                    timer_ref->current = nullptr;
                }
            };
        }, { std::any(running), std::any(percent) });

        // Headless auto-start
        use_effect([=]() -> std::function<void()> {
            if (!g_cli_options.headless) return nullptr;

            set_percent(g_cli_options.percent);
            set_running(true);

            auto queue = winrt::Microsoft::UI::Dispatching::DispatcherQueue::GetForCurrentThread();
            if (!queue) return nullptr;

            auto shutdown_timer = queue.CreateTimer();
            shutdown_timer.Interval(std::chrono::seconds(g_cli_options.duration_seconds));
            shutdown_timer.IsRepeating(false);

            auto perf = perf_ref->current;
            shutdown_timer.Tick([=](auto&&, auto&&) {
                set_running(false);
                perf->write_report_file(AppName, g_cli_options.percent);

                // Print report to stdout if console is attached
                auto report = perf->get_report(AppName, g_cli_options.percent);
                OutputDebugStringA(report.c_str());

                // Exit the application
                winrt::Microsoft::UI::Xaml::Application::Current().Exit();
            });

            shutdown_timer.Start();
            shutdown_ref->current = shutdown_timer;

            return [shutdown_ref]() {
                if (shutdown_ref->current) {
                    shutdown_ref->current.Stop();
                    shutdown_ref->current = nullptr;
                }
            };
        }, {}); // empty deps = run once on mount

        // ─── Build element tree ──────────────────────────────────────────

        // Pre-compute grid definitions
        // 70 columns of 64px, 70 rows of 18px
        std::string col_defs;
        for (int i = 0; i < StockDataSource::Columns; i++) {
            if (i > 0) col_defs += ' ';
            col_defs += "64";
        }
        std::string row_defs;
        for (int i = 0; i < StockDataSource::Rows; i++) {
            if (i > 0) row_defs += ' ';
            row_defs += "18";
        }

        std::vector<Element> children;
        children.reserve(StockDataSource::TotalItems);

        const auto& items = source->items();
        for (int i = 0; i < StockDataSource::TotalItems; i++) {
            int r = i / StockDataSource::Columns;
            int c = i % StockDataSource::Columns;
            const auto& item = items[i];
            children.push_back(
                text(StockDataSource::format_cell(item))
                    .font_size(8)
                    .foreground(item.is_up ? "#32CD32" : "#FF0000")
                    .padding(2, 1, 2, 1)
                    .grid(r, c)
            );
        }

        return vstack({
            hstack(12, {
                button(running ? "Stop" : "Start", [=] { set_running(!running); }),
                text("Update %:").v_align(VerticalAlignment::Center),
                slider(percent, 0, 100, [=](double v) { set_percent(v); }).width(200),
                text(fps).v_align(VerticalAlignment::Center).width(100),
                text(update_ms).v_align(VerticalAlignment::Center).width(120),
                text(mem).v_align(VerticalAlignment::Center).width(120),
            }).padding(8),
            scroll_view(
                grid({ col_defs, row_defs }, std::move(children))
            ),
        });
    }
};

// ─── Entry Point ─────────────────────────────────────────────────────────────

int WINAPI wWinMain(HINSTANCE, HINSTANCE, LPWSTR, int)
{
    // Parse CLI args
    int argc = 0;
    auto argv = CommandLineToArgvW(GetCommandLineW(), &argc);
    if (argv) {
        g_cli_options = CliOptions::parse(argc, argv);
        LocalFree(argv);
    }

    // Attach parent console for headless output
    if (g_cli_options.headless) {
        AttachConsole(static_cast<DWORD>(-1)); // ATTACH_PARENT_PROCESS
    }

    duct::run<StockGridApp>(L"StressPerf.DuctCpp", 1920, 1080, true);
    return 0;
}
