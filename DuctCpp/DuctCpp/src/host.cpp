#include "pch.h"
#include "host.h"
#include <winrt/Microsoft.UI.Xaml.Media.h>

// Render phase timing — sampled every second
static LARGE_INTEGER g_perf_freq{};
static double g_tree_build_sum = 0;
static double g_reconcile_sum = 0;
static double g_effects_sum = 0;
static int g_render_count = 0;
static double g_last_report_time = 0;

static double qpc_ms() {
    LARGE_INTEGER now;
    QueryPerformanceCounter(&now);
    return static_cast<double>(now.QuadPart) * 1000.0 / g_perf_freq.QuadPart;
}

namespace duct {

DuctHost::DuctHost(winrt::Microsoft::UI::Xaml::Window window)
    : window_(std::move(window))
{
    dispatcher_ = window_.DispatcherQueue();
    if (g_perf_freq.QuadPart == 0) QueryPerformanceFrequency(&g_perf_freq);
}

void DuctHost::mount(std::shared_ptr<Component> component) {
    root_component_ = std::move(component);
    root_func_ = nullptr;
    root_func_context_ = nullptr;
    request_render();
}

void DuctHost::mount(std::function<Element(RenderContext&)> render_func) {
    root_func_ = std::move(render_func);
    root_func_context_ = std::make_unique<RenderContext>();
    root_component_ = nullptr;
    request_render();
}

void DuctHost::request_render() {
    needs_rerender_ = true;
    if (render_scheduled_) {
#ifdef DUCT_DEBUG_LOG
        OutputDebugStringA("REQUEST_RENDER: already scheduled\n");
#endif
        return;
    }
    render_scheduled_ = true;

#ifdef DUCT_DEBUG_LOG
    OutputDebugStringA("REQUEST_RENDER: enqueueing\n");
#endif
    bool ok = dispatcher_.TryEnqueue([this]() {
#ifdef DUCT_DEBUG_LOG
        OutputDebugStringA("REQUEST_RENDER: dispatch callback fired\n");
#endif
        render_scheduled_ = false;
        render_loop();
    });
#ifdef DUCT_DEBUG_LOG
    OutputDebugStringA(ok ? "REQUEST_RENDER: enqueue OK\n" : "REQUEST_RENDER: enqueue FAILED\n");
#endif
}

void DuctHost::render_loop() {
    // Allow cascading re-renders (state set during render), but cap iterations
    for (int i = 0; i < 50 && needs_rerender_; ++i) {
        needs_rerender_ = false;
        render();
    }
}

void DuctHost::render() {
#ifdef DUCT_DEBUG_LOG
    OutputDebugStringA("RENDER: starting\n");
#endif
    try {
        Element new_tree;

        auto request_rerender = [this]() { request_render(); };

        double t0 = qpc_ms();

        if (root_component_) {
            root_component_->begin_render();
            root_component_->context().set_request_render(request_rerender);
            new_tree = root_component_->render();
        } else if (root_func_) {
            root_func_context_->begin_render();
            root_func_context_->set_request_render(request_rerender);
            new_tree = root_func_(*root_func_context_);
        } else {
            return;
        }

        double t1 = qpc_ms();

        // Reconcile
        auto old_tree_ptr = std::holds_alternative<EmptyElement>(current_tree_.data) ? nullptr : &current_tree_;
        auto new_control = reconciler_.reconcile(old_tree_ptr, new_tree, current_control_, request_rerender);

        if (new_control != current_control_) {
            window_.Content(new_control);
            current_control_ = new_control;
        }

        double t2 = qpc_ms();

        // Flush effects
        if (root_component_) {
            root_component_->flush_effects();
        } else if (root_func_) {
            root_func_context_->flush_effects();
        }

        double t3 = qpc_ms();

        // Keep previous tree alive for tag-based event dispatch
        previous_tree_ = std::move(current_tree_);
        current_tree_ = std::move(new_tree);

#ifdef DUCT_DEBUG_LOG
        // Accumulate timing and report every ~1 second
        g_tree_build_sum += (t1 - t0);
        g_reconcile_sum += (t2 - t1);
        g_effects_sum += (t3 - t2);
        g_render_count++;

        static double s_last_report = 0;
        double now_s = qpc_ms() / 1000.0;
        if (s_last_report == 0) s_last_report = now_s;
        if ((now_s - s_last_report) >= 1.0 && g_render_count > 0) {
            char buf[256];
            snprintf(buf, sizeof(buf),
                "PERF [%d renders]: tree=%.2fms  reconcile=%.2fms  effects=%.2fms  total=%.2fms\n",
                g_render_count,
                g_tree_build_sum / g_render_count,
                g_reconcile_sum / g_render_count,
                g_effects_sum / g_render_count,
                (g_tree_build_sum + g_reconcile_sum + g_effects_sum) / g_render_count);
            OutputDebugStringA(buf);
            fprintf(stderr, "%s", buf);
            if (FILE* f = _wfopen(L"C:\\temp\\ductcpp_perf_phases.log", L"a")) {
                fprintf(f, "%s", buf);
                fclose(f);
            }
            g_tree_build_sum = 0;
            g_reconcile_sum = 0;
            g_effects_sum = 0;
            g_render_count = 0;
            s_last_report = now_s;
        }
#endif

    } catch (const winrt::hresult_error& e) {
        show_error(e.message());
    } catch (const std::exception& e) {
        int size = MultiByteToWideChar(CP_UTF8, 0, e.what(), -1, nullptr, 0);
        std::wstring ws(size, 0);
        MultiByteToWideChar(CP_UTF8, 0, e.what(), -1, ws.data(), size);
        show_error(winrt::hstring(ws));
    } catch (...) {
        show_error(L"Unknown error during render");
    }
}

void DuctHost::show_error(winrt::hstring message) {
    try {
        // Use Color struct directly — avoids ColorHelper activation issues
        winrt::Windows::UI::Color red{ 255, 255, 0, 0 };
        winrt::Windows::UI::Color white{ 255, 255, 255, 255 };

        controls::Border error_border;
        error_border.Background(winrt::Microsoft::UI::Xaml::Media::SolidColorBrush(red));
        error_border.Padding({ 16, 16, 16, 16 });

        controls::TextBlock error_text;
        error_text.Text(message);
        error_text.Foreground(winrt::Microsoft::UI::Xaml::Media::SolidColorBrush(white));
        error_text.FontSize(16);
        error_text.TextWrapping(winrt::Microsoft::UI::Xaml::TextWrapping::Wrap);

        error_border.Child(error_text);
        window_.Content(error_border);
    } catch (...) {
        // If even the error display fails, log to debug output
        OutputDebugStringW(L"DuctHost: render error: ");
        OutputDebugStringW(message.c_str());
        OutputDebugStringW(L"\n");
    }
}

} // namespace duct
