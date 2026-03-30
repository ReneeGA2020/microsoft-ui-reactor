// DuctCpp Test App — A single-file WinUI 3 application using the DuctCpp framework.
// No XAML. No data binding. No resources. No templates. Just C++.
// Port of the C# Duct.TestApp/App.cs

#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <windows.h>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Microsoft.UI.Dispatching.h>
#include <format>
#include <algorithm>
#include <numeric>
#include <random>
#include <chrono>
#include <duct/duct.h>

using namespace duct;

// ─── Forward declarations for demos ────────────────────────────────────────────

class CounterDemo;
class TodoDemo;
class ConditionalDemo;
class FormDemo;
class DynamicListDemo;
class PerfStressDemo;
class VirtualizationDemo;
class FlyoutDemo;
class DataTemplateDemo;
class TransitionsDemo;

// ─── Helper: tab button ────────────────────────────────────────────────────────

static Element tab_button(const std::string& label, const std::string& current,
                           std::function<void(std::string)> set_current) {
    return button(label, [=] { set_current(label); })
        .disabled(label == current);
}

// ─── Root application component ────────────────────────────────────────────────

class DemoApp : public Component {
public:
    Element render() override {
        auto [current_tab, set_tab] = use_state<std::string>("Counter");

        auto content = [&]() -> Element {
            if (current_tab == "Counter") return component<CounterDemo>();
            if (current_tab == "Todo List") return component<TodoDemo>();
            if (current_tab == "Conditional UI") return component<ConditionalDemo>();
            if (current_tab == "Form") return component<FormDemo>();
            if (current_tab == "Dynamic List") return component<DynamicListDemo>();
            if (current_tab == "Perf Stress") return component<PerfStressDemo>();
            if (current_tab == "Virtualization") return component<VirtualizationDemo>();
            if (current_tab == "Flyout") return component<FlyoutDemo>();
            if (current_tab == "DataTemplate") return component<DataTemplateDemo>();
            if (current_tab == "FlexPanel") return text("FlexPanel not ported — see C# version");
            if (current_tab == "Transitions") return component<TransitionsDemo>();
            return text("Select a tab");
        }();

        return vstack(12, {
            // Tab bar
            hstack(8, {
                tab_button("Counter", current_tab, set_tab),
                tab_button("Todo List", current_tab, set_tab),
                tab_button("Conditional UI", current_tab, set_tab),
                tab_button("Form", current_tab, set_tab),
                tab_button("Dynamic List", current_tab, set_tab),
                tab_button("Perf Stress", current_tab, set_tab),
                tab_button("Virtualization", current_tab, set_tab),
                tab_button("Flyout", current_tab, set_tab),
                tab_button("DataTemplate", current_tab, set_tab),
                tab_button("FlexPanel", current_tab, set_tab),
                tab_button("Transitions", current_tab, set_tab)
            }).margin(16, 16, 16, 0),

            // Content area with padding
            border(content).padding(24).margin(16)
        });
    }
};

// ─── Counter demo ──────────────────────────────────────────────────────────────

class CounterDemo : public Component {
public:
    Element render() override {
        auto [count, set_count] = use_state(0);
        auto [step, set_step] = use_state(1);

        auto count_message = [&]() -> Element {
            if (count == 0) return text("Try clicking the buttons!").opacity(0.6);
            if (count > 0 && count < 10) return text("Going up...");
            if (count >= 10 && count < 50) return text("Getting bigger!").semi_bold();
            if (count >= 50) return text("That's a LOT!").bold().font_size(20);
            if (count < 0 && count > -10) return text("Going negative...");
            return text("Way down there!").bold();
        }();

        return vstack(12, {
            heading("Counter"),
            text(std::format("Current count: {}", count)).font_size(24).semi_bold(),

            hstack(8, {
                button(std::format("- {}", step), [=] { set_count(count - step); }),
                button("Reset", [=] { set_count(0); }).disabled(count == 0),
                button(std::format("+ {}", step), [=] { set_count(count + step); })
            }),

            hstack(8, {
                text("Step size:"),
                slider(step, 1, 10, set_step).width(200),
                text(std::format("{}", step))
            }),

            count_message
        });
    }
};

// ─── Todo list demo ────────────────────────────────────────────────────────────

struct TodoItem {
    std::string text;
    bool done;
};

class TodoDemo : public Component {
public:
    Element render() override {
        auto [items, update_items] = use_reducer<std::vector<TodoItem>>(
            std::vector<TodoItem>{
                {"Build Duct library", true},
                {"Write test app", true},
                {"Add more features", false},
            });
        auto [new_text, set_new_text] = use_state<std::string>("");

        int done_count = 0;
        for (const auto& item : items) {
            if (item.done) done_count++;
        }
        int total_count = static_cast<int>(items.size());

        // Build the todo list items
        std::vector<Element> item_elements;
        for (int i = 0; i < static_cast<int>(items.size()); i++) {
            const auto& item = items[i];
            item_elements.push_back(
                hstack(8, {
                    check_box(item.done,
                        [=](bool done) {
                            update_items([=](std::vector<TodoItem> list) {
                                list[i].done = done;
                                return list;
                            });
                        },
                        {.label = item.text}),
                    button("x", [=] {
                        update_items([=](std::vector<TodoItem> list) {
                            list.erase(list.begin() + i);
                            return list;
                        });
                    })
                }).with_key(std::format("todo-{}", i))
            );
        }

        bool new_text_empty = new_text.empty() ||
            new_text.find_first_not_of(' ') == std::string::npos;

        return vstack(12, {
            heading("Todo List"),
            text(std::format("{}/{} completed", done_count, total_count)),

            // Add new item
            hstack(8, {
                text_field(new_text, set_new_text, {.placeholder = "What needs to be done?"}).width(300),
                button("Add", [=] {
                    if (!new_text_empty) {
                        // Trim whitespace
                        auto trimmed = new_text;
                        auto start = trimmed.find_first_not_of(' ');
                        auto end = trimmed.find_last_not_of(' ');
                        if (start != std::string::npos)
                            trimmed = trimmed.substr(start, end - start + 1);
                        update_items([=](std::vector<TodoItem> list) {
                            list.push_back({trimmed, false});
                            return list;
                        });
                        set_new_text("");
                    }
                }).disabled(new_text_empty)
            }),

            // List of items
            vstack(4, std::move(item_elements)),

            // Conditional: show "All done!" when everything is checked
            when(total_count > 0 && done_count == total_count,
                [] { return text("All done!").bold().font_size(18); })
        });
    }
};

// ─── Conditional UI demo ───────────────────────────────────────────────────────

class ConditionalDemo : public Component {
    enum class ViewMode { Simple, Detailed, Custom };

public:
    Element render() override {
        auto [show_advanced, set_show_advanced] = use_state(false);
        auto [enable_feature_a, set_feature_a] = use_state(false);
        auto [enable_feature_b, set_feature_b] = use_state(false);
        auto [view_mode, set_view_mode] = use_reducer(ViewMode::Simple);
        auto [item_count, set_item_count] = use_state(3);

        // Section 1: Advanced options
        Element advanced_section = show_advanced
            ? border(
                vstack(8, {
                    text("Advanced Settings").semi_bold(),
                    check_box(enable_feature_a, set_feature_a, {.label = "Enable Feature A"}),
                    check_box(enable_feature_b, set_feature_b, {.label = "Enable Feature B"}),

                    enable_feature_a
                        ? border(
                            vstack(4, {
                                text("Feature A Configuration").semi_bold(),
                                text("This sub-tree only exists when Feature A is checked."),
                                slider(50, 0, 100).width(200)
                            })
                          ).corner_radius(4).background("#e8f5e9").padding(12)
                        : empty(),

                    enable_feature_b
                        ? border(
                            vstack(4, {
                                text("Feature B Configuration").semi_bold(),
                                text("This sub-tree only exists when Feature B is checked."),
                                toggle_switch(false, nullptr, {.on_content = "On", .off_content = "Off"})
                            })
                          ).corner_radius(4).background("#e3f2fd").padding(12)
                        : empty()
                })
              ).corner_radius(8).background("#f5f5f5").padding(16)
            : text("Check the box above to reveal advanced options.").opacity(0.6);

        // Section 2: View mode content
        Element view_content;
        switch (view_mode) {
        case ViewMode::Simple:
            view_content = vstack(4, {
                text("Simple view - just a summary."),
                text(std::format("{} items in the list.", item_count))
            });
            break;
        case ViewMode::Detailed: {
            std::vector<Element> detail_items;
            for (int i = 1; i <= item_count; i++) {
                detail_items.push_back(
                    hstack(4, {
                        text(std::format("Item {}", i)).width(80),
                        progress(static_cast<double>(i) / item_count).width(150)
                    })
                );
            }
            view_content = vstack(4, std::move(detail_items));
            view_content = vstack(4, {
                text("Detailed view - shows every item:").semi_bold(),
                std::move(view_content)
            });
            break;
        }
        case ViewMode::Custom: {
            std::vector<Element> custom_items;
            for (int i = 1; i <= item_count; i++) {
                custom_items.push_back(
                    border(text(std::format("Custom item {}", i)))
                        .corner_radius(4).background("#fff3e0").padding(8, 4)
                );
            }
            view_content = vstack(8, {
                text("Custom view - configure the list:").semi_bold(),
                hstack(8, {
                    text("Item count:"),
                    slider(item_count, 1, 10, set_item_count).width(200),
                    text(std::format("{}", item_count))
                }),
                vstack(4, std::move(custom_items))
            });
            break;
        }
        }

        // Computed summary
        std::string features_str;
        if (enable_feature_a) features_str += "A";
        if (enable_feature_b) features_str += "B";
        if (features_str.empty()) features_str = "none";

        std::string view_mode_str = view_mode == ViewMode::Simple ? "Simple"
            : view_mode == ViewMode::Detailed ? "Detailed" : "Custom";

        return scroll_view(vstack(16, {
            heading("Conditional UI"),
            text("Every piece of UI below is driven by plain C++ expressions."),
            text("Check the boxes and watch entire sub-trees appear and disappear."),

            sub_heading("1. Checkbox toggles a sub-tree"),
            check_box(show_advanced, set_show_advanced, {.label = "Show advanced options"}),
            advanced_section,

            sub_heading("2. Switch expression picks a sub-tree"),
            hstack(8, {
                button("Simple", [=] { set_view_mode([](auto) { return ViewMode::Simple; }); })
                    .disabled(view_mode == ViewMode::Simple),
                button("Detailed", [=] { set_view_mode([](auto) { return ViewMode::Detailed; }); })
                    .disabled(view_mode == ViewMode::Detailed),
                button("Custom", [=] { set_view_mode([](auto) { return ViewMode::Custom; }); })
                    .disabled(view_mode == ViewMode::Custom)
            }),
            view_content,

            sub_heading("3. Computed UI from expressions"),
            text("The UI below is generated by a C++ expression - no templates needed:"),
            vstack(4, {
                text(std::format("Advanced: {}, Features: {}, View: {}",
                    show_advanced ? "ON" : "OFF", features_str, view_mode_str))
                    .opacity(0.7),
                when(show_advanced && enable_feature_a && enable_feature_b,
                    [] { return border(
                        text("Warning: Both features enabled simultaneously may cause conflicts.")
                    ).corner_radius(4).background("#fff9c4").padding(12); })
            })
        }));
    }
};

// ─── Form demo ─────────────────────────────────────────────────────────────────

class FormDemo : public Component {
public:
    Element render() override {
        auto [name, set_name] = use_state<std::string>("");
        auto [email, set_email] = use_state<std::string>("");
        auto [agree_to_terms, set_agree] = use_state(false);
        auto [dark_mode, set_dark_mode] = use_state(false);
        auto [font_sz, set_font_sz] = use_state(14.0);
        auto [submitted, set_submitted] = use_state(false);

        if (submitted) {
            return vstack(12, {
                heading("Form Submitted!"),
                text(std::format("Name: {}", name)),
                text(std::format("Email: {}", email)),
                text(std::format("Dark mode: {}", dark_mode ? "Yes" : "No")),
                text(std::format("Font size: {:.0f}px", font_sz)),
                button("Back", [=] { set_submitted(false); })
            });
        }

        bool is_valid = !name.empty() && name.find_first_not_of(' ') != std::string::npos
            && !email.empty() && email.find_first_not_of(' ') != std::string::npos
            && agree_to_terms;

        return vstack(16, {
            heading("Registration Form"),

            vstack(8, {
                text("Name"),
                text_field(name, set_name, {.placeholder = "Enter your name"}).width(300)
            }),

            vstack(8, {
                text("Email"),
                text_field(email, set_email, {.placeholder = "you@example.com"}).width(300)
            }),

            toggle_switch(dark_mode, set_dark_mode, {.on_content = "Dark", .off_content = "Light"}),

            hstack(8, {
                text("Font size:"),
                slider(font_sz, 10, 30, set_font_sz).width(200),
                text(std::format("{:.0f}px", font_sz))
            }),

            check_box(agree_to_terms, set_agree, {.label = "I agree to the terms"}),

            when(!is_valid,
                [] { return text("Please fill all fields and agree to terms").opacity(0.6); }),

            button("Submit", [=] { set_submitted(true); }).disabled(!is_valid)
        });
    }
};

// ─── Dynamic list demo ─────────────────────────────────────────────────────────

class DynamicListDemo : public Component {
public:
    Element render() override {
        auto [count, set_count] = use_state(3);
        auto [show_indices, set_show_indices] = use_state(true);

        // Build dynamic list
        std::vector<Element> items;
        for (int i = 0; i < count; i++) {
            std::vector<Element> row;
            if (show_indices)
                row.push_back(text(std::format("#{}", i + 1)).semi_bold());
            row.push_back(text(std::format("Item {}", i + 1)));
            row.push_back(text("(created dynamically)").opacity(0.5));

            items.push_back(
                border(hstack(8, std::move(row)))
                    .corner_radius(4).background("#f0f0f0").padding(12, 8)
            );
        }

        return vstack(12, {
            heading("Dynamic List"),
            text("Demonstrates conditional and list rendering"),

            hstack(8, {
                button("Remove", [=] { set_count(std::max(0, count - 1)); }).disabled(count == 0),
                text(std::format("{} items", count)),
                button("Add", [=] { set_count(count + 1); })
            }),

            check_box(show_indices, set_show_indices, {.label = "Show indices"}),

            vstack(4, std::move(items)),

            when(count == 0,
                [] { return text("No items. Click Add to create some.").opacity(0.6); }),
            when(count >= 10,
                [] { return text("That's a lot of items!").bold(); })
        });
    }
};

// ─── Performance stress test ───────────────────────────────────────────────────

// Sort state colors: 0=default, 1=pivot, 2=comparing, 3=swapped, 4=final
static const std::string bar_colors[] = {
    "#4fc3f7", "#81c784", "#fff176", "#ff8a65", "#ba68c8",
    "#4dd0e1", "#aed581", "#ffd54f", "#e57373", "#9575cd",
};

static Element legend_item(const std::string& color, const std::string& label) {
    return hstack(4, {
        border(empty()).background(color).corner_radius(2).size(12, 12),
        text(label).font_size(12).opacity(0.7)
    });
}

// Quicksort state machine for async stepping
struct SortState {
    std::vector<int> values;
    std::vector<int> colors; // 0=default,1=pivot,2=comparing,3=swapped,4=final
    struct Work { int lo; int hi; };
    std::vector<Work> stack;
    int total_swaps = 0;
    int step_count = 0;
    bool done = false;

    // Partition phase state (for stepping within a partition)
    bool in_partition = false;
    int part_lo = 0, part_hi = 0;
    int part_pivot_val = 0;
    int part_i = 0, part_j = 0;

    void init(int count) {
        std::mt19937 rng(42);
        values.resize(count);
        std::iota(values.begin(), values.end(), 1);
        for (int i = count - 1; i > 0; i--) {
            std::uniform_int_distribution<int> dist(0, i);
            int j = dist(rng);
            std::swap(values[i], values[j]);
        }
        colors.assign(count, 0);
        stack.clear();
        if (count > 1) stack.push_back({0, count - 1});
        total_swaps = 0;
        step_count = 0;
        done = false;
        in_partition = false;
    }

    // Do one step. Returns true if sort is complete.
    bool step() {
        if (done) return true;
        step_count++;

        if (in_partition) {
            // Clear previous comparing highlights
            for (int k = part_lo; k <= part_hi; k++) {
                if (colors[k] == 2 || colors[k] == 3) colors[k] = 0;
            }
            colors[part_hi] = 1; // pivot

            if (part_j < part_hi) {
                colors[part_j] = 2; // comparing
                if (values[part_j] <= part_pivot_val) {
                    std::swap(values[part_i], values[part_j]);
                    colors[part_i] = 3; // swapped
                    colors[part_j] = 3;
                    total_swaps++;
                    part_i++;
                }
                part_j++;
            } else {
                // Partition done: place pivot
                std::swap(values[part_i], values[part_hi]);
                if (part_i != part_hi) total_swaps++;
                colors[part_i] = 4; // final position

                // Push sub-ranges
                int pivot_idx = part_i;
                if (pivot_idx - 1 > part_lo) stack.push_back({part_lo, pivot_idx - 1});
                if (pivot_idx + 1 < part_hi) stack.push_back({pivot_idx + 1, part_hi});
                in_partition = false;

                // Check if done
                if (stack.empty()) {
                    // Mark all as final
                    for (auto& c : colors) c = 4;
                    done = true;
                    return true;
                }
            }
        } else {
            // Start next partition
            if (stack.empty()) {
                for (auto& c : colors) c = 4;
                done = true;
                return true;
            }
            auto [lo, hi] = stack.back();
            stack.pop_back();

            if (lo >= hi) {
                if (lo == hi) colors[lo] = 4;
                return false; // skip trivial ranges
            }

            in_partition = true;
            part_lo = lo;
            part_hi = hi;
            part_pivot_val = values[hi];
            part_i = lo;
            part_j = lo;
            colors[hi] = 1; // pivot
        }
        return false;
    }
};

// Render time tracker
struct RenderTimeTracker {
    std::vector<double> times_ms;
    LARGE_INTEGER freq{};

    RenderTimeTracker() {
        QueryPerformanceFrequency(&freq);
    }

    void record(double ms) {
        times_ms.push_back(ms);
        if (times_ms.size() > 1000) times_ms.erase(times_ms.begin());
    }

    double now_ms() const {
        LARGE_INTEGER t;
        QueryPerformanceCounter(&t);
        return static_cast<double>(t.QuadPart) * 1000.0 / freq.QuadPart;
    }

    double avg() const {
        if (times_ms.empty()) return 0;
        double sum = 0;
        for (auto t : times_ms) sum += t;
        return sum / times_ms.size();
    }

    double p95() const {
        if (times_ms.empty()) return 0;
        auto sorted = times_ms;
        std::sort(sorted.begin(), sorted.end());
        return sorted[(size_t)(sorted.size() * 0.95)];
    }

    double max_val() const {
        if (times_ms.empty()) return 0;
        return *std::max_element(times_ms.begin(), times_ms.end());
    }

    // Histogram: 10 buckets from 0 to max
    std::vector<int> histogram(int buckets = 10) const {
        std::vector<int> hist(buckets, 0);
        if (times_ms.empty()) return hist;
        double mx = max_val();
        if (mx <= 0) return hist;
        for (auto t : times_ms) {
            int b = std::min(buckets - 1, static_cast<int>(t / mx * buckets));
            hist[b]++;
        }
        return hist;
    }
};

class PerfStressDemo : public Component {
public:
    Element render() override {
        auto [element_count, set_element_count] = use_state(100);
        auto [show_labels, set_show_labels] = use_state(false);
        auto [show_borders, set_show_borders] = use_state(true);
        auto [tick_ms, set_tick_ms] = use_state(16);
        auto [sort_values, set_sort_values] = use_reducer<std::vector<int>>({});
        auto [sort_colors, set_sort_colors] = use_reducer<std::vector<int>>({});
        auto [running, set_running] = use_state(false);
        auto [sorted, set_sorted] = use_state(false);
        auto [total_swaps, set_total_swaps] = use_state(0);
        auto [step_count, set_step_count] = use_state(0);
        auto [render_stats, set_render_stats] = use_state<std::string>("");
        auto [hist_bars, set_hist_bars] = use_reducer<std::vector<int>>(std::vector<int>(10, 0));
        auto [total_sort_ms, set_total_sort_ms] = use_state(0.0);

        // Refs for persistent state across renders
        auto sort_ref = use_ref<std::shared_ptr<SortState>>(nullptr);
        auto perf_ref = use_ref<std::shared_ptr<RenderTimeTracker>>(nullptr);
        auto sort_start_ref = use_ref<double>(0.0);

        // Lazily init perf tracker
        if (!perf_ref->current) perf_ref->current = std::make_shared<RenderTimeTracker>();
        auto perf = perf_ref->current;

        // Track render time
        auto render_start = perf->now_ms();

        // Timer effect: start/stop DispatcherQueue repeating timer
        use_effect([=]() -> std::function<void()> {
            if (!running || !sort_ref->current) return nullptr;

            auto state = sort_ref->current;
            auto tracker = perf_ref->current;

            auto queue = winrt::Microsoft::UI::Dispatching::DispatcherQueue::GetForCurrentThread();
            if (!queue) return nullptr;

            // Create a repeating timer
            auto timer = queue.CreateTimer();
            timer.Interval(std::chrono::milliseconds(std::max(1, tick_ms)));
            timer.IsRepeating(true);

            auto start_time = sort_start_ref->current;

            timer.Tick([=](auto sender, auto) {
                if (state->done) {
                    sender.as<winrt::Microsoft::UI::Dispatching::DispatcherQueueTimer>().Stop();
                    set_total_sort_ms(tracker->now_ms() - start_time);
                    set_sorted(true);
                    set_running(false);
                    return;
                }

                auto t0 = tracker->now_ms();

                // Do multiple steps per tick — match C# which emits every elementCount/20 steps
                int steps_per_tick = std::max(1, static_cast<int>(state->values.size()) / 20);
                for (int s = 0; s < steps_per_tick && !state->done; s++) {
                    state->step();
                }

                auto t1 = tracker->now_ms();
                tracker->record(t1 - t0);

                // Update UI state
                set_sort_values([vals = state->values](auto) { return vals; });
                set_sort_colors([cols = state->colors](auto) { return cols; });
                set_total_swaps(state->total_swaps);
                set_step_count(state->step_count);
                set_render_stats(std::format("Avg: {:.1f}ms  P95: {:.1f}ms  Max: {:.1f}ms",
                    tracker->avg(), tracker->p95(), tracker->max_val()));
                set_hist_bars([h = tracker->histogram()](auto) { return h; });

                if (state->done) {
                    sender.as<winrt::Microsoft::UI::Dispatching::DispatcherQueueTimer>().Stop();
                    set_total_sort_ms(tracker->now_ms() - start_time);
                    set_sorted(true);
                    set_running(false);
                }
            });

            timer.Start();

            // Cleanup: stop timer when effect is re-run or unmounted
            return [timer]() {
                timer.Stop();
            };
        }, deps(running, tick_ms));

        // Build the bar visualization
        Element bars;
        if (!sort_values.empty()) {
            std::vector<Element> bar_elements;
            double max_val = static_cast<double>(element_count);
            for (int i = 0; i < static_cast<int>(sort_values.size()); i++) {
                double height_pct = sort_values[i] / max_val * 200;
                int color_idx = sort_colors[i] % 5; // 0-4 for sort states

                static const std::string state_colors[] = {
                    "#4fc3f7", "#81c784", "#fff176", "#ff8a65", "#ba68c8"
                };
                auto& bar_color = state_colors[color_idx];

                static const std::string all_colors[] = {
                    "#4fc3f7", "#81c784", "#fff176", "#ff8a65", "#ba68c8",
                    "#4dd0e1", "#aed581", "#ffd54f", "#e57373", "#9575cd",
                };

                // Determine pivot/active state for opacity (matching C#)
                // We don't track pivot/left/right indices separately, so use color to infer:
                bool is_pivot = (sort_colors[i] == 1);
                bool is_active = (sort_colors[i] == 2 || sort_colors[i] == 3);
                double opacity = is_pivot ? 1.0 : is_active ? 0.9 : 0.7;

                double bar_width = std::max(2.0, 800.0 / element_count - (show_borders ? 1.0 : 0.0));
                double bar_height = std::max(4.0, height_pct);
                int val = sort_values[i];

                // Each bar contains child controls to stress the reconciler (matching C#):
                // a tiny pip + a value label + a progress-like fill
                Element bar_content = vstack(0, {
                    // Top: small colored indicator pip (changes with sort state)
                    border(empty())
                        .background(is_pivot ? "#ffffff" : is_active ? "#ffeb3b" : all_colors[(color_idx + 1) % 10])
                        .corner_radius(1)
                        .width(std::min(bar_width - 1, 6.0))
                        .height(2),
                    // Middle: value label (only when bars are wide enough)
                    bar_width >= 10
                        ? text(std::format("{}", val)).font_size(std::min(7.0, bar_width * 0.8))
                        : empty(),
                    // Bottom: progress-like fill showing relative position
                    border(empty())
                        .background(all_colors[(color_idx + 2) % 10])
                        .corner_radius(0)
                        .width(std::max(1.0, bar_width * 0.6))
                        .height(std::max(1.0, bar_height * 0.15))
                        .opacity(0.5)
                });

                Element bar = border(std::move(bar_content))
                    .background(bar_color)
                    .corner_radius(0)
                    .width(bar_width)
                    .height(bar_height)
                    .opacity(opacity)
                    .v_align(VerticalAlignment::Bottom);

                if (show_borders)
                    bar = bar.margin(0, 0, 1, 0);

                bar_elements.push_back(std::move(bar));
            }
            bars = hstack(0, std::move(bar_elements))
                .height(220).v_align(VerticalAlignment::Bottom);
        } else {
            bars = text("Click 'Start Sort' to begin").opacity(0.5).height(220);
        }

        // Start sort action
        auto start_sort = [=] {
            auto state = std::make_shared<SortState>();
            state->init(element_count);
            sort_ref->current = state;
            sort_start_ref->current = perf_ref->current->now_ms();

            set_sort_values([vals = state->values](auto) { return vals; });
            set_sort_colors([cols = state->colors](auto) { return cols; });
            set_total_swaps(0);
            set_step_count(0);
            set_total_sort_ms(0);
            set_sorted(false);
            set_running(true);
        };

        // Build mini histogram
        Element histogram_ui = empty();
        if (!hist_bars.empty() && !render_stats.empty()) {
            int hist_max = *std::max_element(hist_bars.begin(), hist_bars.end());
            if (hist_max > 0) {
                std::vector<Element> hbars;
                double max_time = perf->max_val();
                for (int i = 0; i < static_cast<int>(hist_bars.size()); i++) {
                    double h = static_cast<double>(hist_bars[i]) / hist_max * 40.0;
                    hbars.push_back(
                        vstack(0, {
                            border(empty())
                                .background(h > 0 ? "#64b5f6" : "#333")
                                .width(16).height(std::max(1.0, h))
                                .v_align(VerticalAlignment::Bottom),
                            text(std::format("{:.0f}", max_time * (i + 1) / hist_bars.size()))
                                .font_size(7).opacity(0.5)
                        }).v_align(VerticalAlignment::Bottom)
                    );
                }
                histogram_ui = vstack(4, {
                    text("Render time distribution (ms)").font_size(11).opacity(0.7),
                    hstack(2, std::move(hbars)).height(60).v_align(VerticalAlignment::Bottom)
                });
            }
        }

        // Record this render's time
        auto render_end = perf->now_ms();
        // (We can't easily record post-reconcile time here, but element construction time is useful)

        return scroll_view(vstack(12, {
            heading("Performance Stress Test"),
            text("Quicksort visualization - stresses tree diffing with many simultaneous changes."),

            hstack(12, {
                vstack(4, {
                    text("Elements:"),
                    hstack(8, {
                        button("10", [=] { set_element_count(10); }).disabled(running || element_count == 10),
                        button("50", [=] { set_element_count(50); }).disabled(running || element_count == 50),
                        button("100", [=] { set_element_count(100); }).disabled(running || element_count == 100),
                        button("250", [=] { set_element_count(250); }).disabled(running || element_count == 250),
                        button("500", [=] { set_element_count(500); }).disabled(running || element_count == 500),
                        button("1000", [=] { set_element_count(1000); }).disabled(running || element_count == 1000)
                    })
                }),
                vstack(4, {
                    text("Tick interval:"),
                    hstack(8, {
                        slider(tick_ms, 0, 100, set_tick_ms).width(150),
                        text(std::format("{}ms", tick_ms))
                    })
                })
            }),

            hstack(12, {
                check_box(show_labels, [=](bool v) { set_show_labels(v); }, {.label = "Show value labels"}),
                check_box(show_borders, [=](bool v) { set_show_borders(v); }, {.label = "Show bar gaps"})
            }),

            hstack(8, {
                button("Start Sort", start_sort).disabled(running),
                button("Reset", [=] {
                    sort_ref->current = nullptr;
                    set_sort_values([](auto) { return std::vector<int>{}; });
                    set_sort_colors([](auto) { return std::vector<int>{}; });
                    set_total_swaps(0);
                    set_step_count(0);
                    set_total_sort_ms(0);
                    set_sorted(false);
                    set_render_stats("");
                    set_hist_bars([](auto) { return std::vector<int>(10, 0); });
                }).disabled(running)
            }),

            // Render time stats
            !render_stats.empty()
                ? text(render_stats).font_size(13).foreground("#64b5f6")
                : empty(),

            sorted
                ? text(std::format("Sorted in {:.0f} ms  ({} swaps, {} steps)", total_sort_ms, total_swaps, step_count))
                    .bold().font_size(16)
                : (running
                    ? text(std::format("Sorting... step {}, {} swaps", step_count, total_swaps))
                        .font_size(14).opacity(0.8)
                    : empty()),

            border(bars).corner_radius(8).background("#1a1a2e").padding(8),

            // Mini histogram
            histogram_ui,

            hstack(16, {
                legend_item("#4fc3f7", "Default"),
                legend_item("#81c784", "Pivot"),
                legend_item("#fff176", "Comparing"),
                legend_item("#ff8a65", "Swapped"),
                legend_item("#ba68c8", "Final position")
            }).margin(0, 8, 0, 0)
        }));
    }
};

// ─── Virtualization demo ──────────────────────────────────────────────────────

class VirtualizationDemo : public Component {
public:
    Element render() override {
        auto [item_count, set_item_count] = use_state(100);
        auto [selected, set_selected] = use_state(-1);

        std::vector<Element> items;
        for (int i = 0; i < item_count; i++) {
            items.push_back(
                hstack(12, {
                    border(
                        text(std::format("{}", i)).font_size(12)
                    ).background("#e3f2fd").corner_radius(4).size(48, 32),
                    vstack(2, {
                        text(std::format("Item {}", i)).semi_bold(),
                        text(std::format("Description for item {} - this row tests scrolling", i))
                            .font_size(12).opacity(0.6)
                    })
                }).padding(4, 2)
            );
        }

        return vstack(12, {
            heading("Virtualization Test"),
            text("Uses ListView for virtualized item rendering."),

            hstack(8, {
                text("Items:"),
                button("100", [=] { set_item_count(100); }).disabled(item_count == 100),
                button("500", [=] { set_item_count(500); }).disabled(item_count == 500),
                button("1000", [=] { set_item_count(1000); }).disabled(item_count == 1000),
                button("5000", [=] { set_item_count(5000); }).disabled(item_count == 5000)
            }),

            selected >= 0
                ? text(std::format("Selected: item {}", selected)).foreground("#1976d2")
                : text("No selection").opacity(0.6),

            text(std::format("{} items", item_count)).opacity(0.6),

            list_view(std::move(items), {
                .selected_index = selected >= 0 ? std::optional(selected) : std::nullopt,
                .on_selection_changed = set_selected
            }).height(500)
        });
    }
};

// ─── Flyout demo ──────────────────────────────────────────────────────────────

class FlyoutDemo : public Component {
public:
    Element render() override {
        auto [tick, set_tick] = use_state(0);
        auto [color, set_color] = use_state<std::string>("Red");
        auto [last_action, set_last_action] = use_state<std::string>("None");

        auto color_hex = [&]() -> std::string {
            if (color == "Red") return "#e57373";
            if (color == "Orange") return "#ffb74d";
            if (color == "Yellow") return "#fff176";
            if (color == "Green") return "#81c784";
            if (color == "Blue") return "#64b5f6";
            if (color == "Purple") return "#ba68c8";
            return "#e0e0e0";
        }();

        return scroll_view(vstack(16, {
            heading("Flyout Attachments"),
            text("Flyout and MenuFlyout element types."),
            text(std::format("Timer tick: {}", tick)).opacity(0.6),

            sub_heading("1. Content Flyout (Color Picker)"),
            flyout_button("Pick Color", {
                text("Choose a color:").semi_bold(),
                hstack(8, {
                    button("Red", [=] { set_color("Red"); }),
                    button("Orange", [=] { set_color("Orange"); }),
                    button("Yellow", [=] { set_color("Yellow"); })
                }),
                hstack(8, {
                    button("Green", [=] { set_color("Green"); }),
                    button("Blue", [=] { set_color("Blue"); }),
                    button("Purple", [=] { set_color("Purple"); })
                })
            }),

            hstack(8, {
                text(std::format("Selected: {}", color)),
                border(empty()).background(color_hex).corner_radius(4).size(24, 24)
            }),

            sub_heading("2. Menu Flyout"),
            menu_flyout_button("File Menu", {
                {"New",  [=] { set_last_action("New file"); }},
                {"Open", [=] { set_last_action("Open file"); }},
                {"Save", [=] { set_last_action("Save file"); }},
                {"Exit", [=] { set_last_action("Exit"); }}
            }),
            text(std::format("Last action: {}", last_action)).opacity(0.7),

            sub_heading("3. Context Menu (right-click)"),
            border(
                vstack(8, {
                    text("Right-click this area for context menu").semi_bold(),
                    text(std::format("Color: {} | Action: {}", color, last_action))
                })
            ).corner_radius(8).background("#f5f5f5").padding(24)
             .context_menu({
                {"Cut",   [=] { set_last_action("Cut"); }},
                {"Copy",  [=] { set_last_action("Copy"); }},
                {"Paste", [=] { set_last_action("Paste"); }}
             })
        }));
    }
};

// ─── DataTemplate demo ────────────────────────────────────────────────────────

struct Animal {
    int id;
    std::string name;
    std::string species;
    std::string emoji;
};

class DataTemplateDemo : public Component {
public:
    Element render() override {
        auto [filter, set_filter] = use_state<std::string>("");
        auto [selected_idx, set_selected_idx] = use_state(-1);

        static const std::vector<Animal> all_animals = {
            {1, "Luna", "Cat", "[cat]"},
            {2, "Max", "Dog", "[dog]"},
            {3, "Bella", "Cat", "[cat]"},
            {4, "Charlie", "Dog", "[dog]"},
            {5, "Oliver", "Rabbit", "[rabbit]"},
            {6, "Lucy", "Cat", "[cat]"},
            {7, "Buddy", "Dog", "[dog]"},
            {8, "Daisy", "Hamster", "[hamster]"},
            {9, "Rocky", "Dog", "[dog]"},
            {10, "Coco", "Parrot", "[parrot]"},
        };

        // Filter animals
        std::vector<const Animal*> filtered;
        for (const auto& a : all_animals) {
            if (filter.empty() ||
                a.name.find(filter) != std::string::npos ||
                a.species.find(filter) != std::string::npos) {
                filtered.push_back(&a);
            }
        }

        // Build ListView items with per-species colored template
        std::vector<Element> items;
        for (const auto* a : filtered) {
            auto bg = a->species == "Cat" ? "#fff3e0"
                : a->species == "Dog" ? "#e3f2fd"
                : a->species == "Rabbit" ? "#f3e5f5"
                : a->species == "Hamster" ? "#fff9c4"
                : "#e8f5e9";

            items.push_back(
                border(
                    hstack(12, {
                        text(a->emoji).font_size(24),
                        vstack(2, {
                            text(a->name).semi_bold(),
                            text(a->species).font_size(12).opacity(0.7)
                        }),
                        text(std::format("#{}", a->id)).opacity(0.3)
                    })
                ).corner_radius(8).background(bg).padding(12, 8)
            );
        }

        Element selected_info = selected_idx >= 0 && selected_idx < static_cast<int>(filtered.size())
            ? text(std::format("Selected: {} the {}", filtered[selected_idx]->name, filtered[selected_idx]->species))
                .foreground("#1976d2")
            : text("No selection").opacity(0.6);

        return vstack(16, {
            heading("DataTemplate Demo"),
            text("ListView with per-species colored item templates."),

            hstack(12, {
                text_field(filter, set_filter, {.placeholder = "Filter animals..."}).width(200)
            }),
            text(std::format("{} animals shown", filtered.size())).opacity(0.6),
            selected_info,

            sub_heading("Animal List"),
            list_view(std::move(items), {
                .selected_index = selected_idx >= 0 ? std::optional(selected_idx) : std::nullopt,
                .on_selection_changed = set_selected_idx
            }).height(400)
        });
    }
};

// ─── Transitions demo ─────────────────────────────────────────────────────────

class TransitionsDemo : public Component {
public:
    Element render() override {
        auto [opacity_val, set_opacity_val] = use_state(1.0);
        auto [show_items, set_show_items] = use_state(true);
        auto [item_count, set_item_count] = use_state(3);

        std::vector<Element> items;
        if (show_items) {
            for (int i = 0; i < item_count; i++) {
                items.push_back(
                    border(
                        text(std::format("Item {}", i + 1))
                    ).corner_radius(8).background("#e3f2fd").padding(16, 12)
                     .transition("Opacity", 300)
                );
            }
        }

        return scroll_view(vstack(16, {
            heading("Transitions"),
            text("Implicit transitions using ScalarTransition for smooth animations."),

            sub_heading("1. Opacity (animated)"),
            hstack(8, {
                text("Opacity:"),
                slider(opacity_val, 0, 1, set_opacity_val).width(200),
                text(std::format("{:.1f}", opacity_val))
            }),
            border(
                text("This text fades smoothly").font_size(20)
            ).corner_radius(8).background("#f5f5f5").padding(24)
             .opacity(opacity_val)
             .transition("Opacity", 500),

            sub_heading("2. Show/Hide Items"),
            hstack(8, {
                button(show_items ? "Hide Items" : "Show Items",
                    [=] { set_show_items(!show_items); }),
                button("Add", [=] { set_item_count(item_count + 1); }),
                button("Remove", [=] { set_item_count(std::max(0, item_count - 1)); })
                    .disabled(item_count == 0)
            }),

            vstack(8, std::move(items))
        }));
    }
};

// ─── Entry point ───────────────────────────────────────────────────────────────

int WINAPI wWinMain(HINSTANCE, HINSTANCE, LPWSTR, int)
{
    duct::run<DemoApp>(L"DuctCpp TestApp", 1200, 800);
    return 0;
}
