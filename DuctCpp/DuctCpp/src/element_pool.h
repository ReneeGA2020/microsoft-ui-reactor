#pragma once

#include <winrt/Microsoft.UI.Xaml.h>
#include <winrt/Microsoft.UI.Xaml.Controls.h>
#include <unordered_map>
#include <vector>
#include <string>
#include <typeindex>

namespace duct {

namespace xaml = winrt::Microsoft::UI::Xaml;
namespace controls = winrt::Microsoft::UI::Xaml::Controls;

/// Recycles unmounted WinUI controls to reduce allocation pressure.
/// Pools controls by their WinRT runtime class name.
class ElementPool {
public:
    static constexpr size_t kMaxPoolSizePerType = 32;

    /// Try to acquire a control of type T from the pool.
    /// Returns a recycled control, or creates a new one if the pool is empty.
    template <typename T>
    T acquire() {
        auto type_name = winrt::name_of<T>();
        std::string key(type_name.begin(), type_name.end());

        auto it = pools_.find(key);
        if (it != pools_.end() && !it->second.empty()) {
            auto control = it->second.back();
            it->second.pop_back();
            return control.as<T>();
        }
        return T();
    }

    /// Return a control to the pool for future reuse.
    /// Resets common properties before pooling.
    void release(xaml::UIElement control);

    /// Clear all pooled controls.
    void clear();

    /// Get total number of pooled controls across all types.
    size_t total_pooled() const;

private:
    /// Reset a control's properties to a clean state before pooling.
    static void reset_control(xaml::UIElement control);

    /// Pool storage: runtime class name → vector of controls
    std::unordered_map<std::string, std::vector<xaml::UIElement>> pools_;
};

} // namespace duct
