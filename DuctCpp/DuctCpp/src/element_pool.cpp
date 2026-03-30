#include "pch.h"
#include "element_pool.h"
#include <winrt/Microsoft.UI.Xaml.Media.h>
#include <winrt/Windows.UI.Text.h>

namespace duct {

void ElementPool::release(xaml::UIElement control) {
    if (!control) return;

    // Get the runtime class name as pool key
    auto type_name = winrt::get_class_name(control);
    std::string key = winrt::to_string(type_name);

    auto& pool = pools_[key];
    if (pool.size() >= kMaxPoolSizePerType) return; // Pool is full

    reset_control(control);
    pool.push_back(control);
}

void ElementPool::clear() {
    pools_.clear();
}

size_t ElementPool::total_pooled() const {
    size_t total = 0;
    for (const auto& [_, pool] : pools_) {
        total += pool.size();
    }
    return total;
}

void ElementPool::reset_control(xaml::UIElement control) {
    // Reset common FrameworkElement properties
    if (auto fe = control.try_as<xaml::FrameworkElement>()) {
        fe.Width(std::numeric_limits<double>::quiet_NaN());  // Auto
        fe.Height(std::numeric_limits<double>::quiet_NaN()); // Auto
        fe.MinWidth(0);
        fe.MinHeight(0);
        fe.MaxWidth(std::numeric_limits<double>::infinity());
        fe.MaxHeight(std::numeric_limits<double>::infinity());
        fe.Margin({ 0, 0, 0, 0 });
        fe.HorizontalAlignment(xaml::HorizontalAlignment::Stretch);
        fe.VerticalAlignment(xaml::VerticalAlignment::Stretch);
        fe.Tag(nullptr);
    }

    control.Opacity(1.0);
    control.Visibility(xaml::Visibility::Visible);

    // Reset type-specific properties
    if (auto tb = control.try_as<controls::TextBlock>()) {
        tb.Text(L"");
        tb.FontSize(14); // WinUI default
        tb.FontWeight(winrt::Windows::UI::Text::FontWeights::Normal());
        tb.Foreground(nullptr);
    }
    else if (auto sp = control.try_as<controls::StackPanel>()) {
        sp.Children().Clear();
        sp.Spacing(0);
        sp.Orientation(controls::Orientation::Vertical);
        sp.Background(nullptr);
    }
    else if (auto grid = control.try_as<controls::Grid>()) {
        grid.Children().Clear();
        grid.ColumnDefinitions().Clear();
        grid.RowDefinitions().Clear();
        grid.Background(nullptr);
    }
    else if (auto border = control.try_as<controls::Border>()) {
        border.Child(nullptr);
        border.Background(nullptr);
        border.Padding({ 0, 0, 0, 0 });
        border.CornerRadius({ 0, 0, 0, 0 });
    }
    else if (auto sv = control.try_as<controls::ScrollViewer>()) {
        sv.Content(nullptr);
    }
    else if (auto btn = control.try_as<controls::Button>()) {
        btn.Content(nullptr);
        btn.Background(nullptr);
        btn.Foreground(nullptr);
        btn.IsEnabled(true);
    }
}

} // namespace duct
