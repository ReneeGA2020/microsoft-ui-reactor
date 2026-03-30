#include "pch.h"
#include <duct/app.h>
#include "host.h"

#include <winrt/Microsoft.UI.Xaml.h>
#include <winrt/Microsoft.UI.Xaml.Controls.h>
#include <winrt/Microsoft.UI.Xaml.XamlTypeInfo.h>
#include <winrt/Microsoft.UI.Xaml.Markup.h>
#include <winrt/Microsoft.UI.Dispatching.h>
#include <winrt/Microsoft.UI.Windowing.h>
#include <winrt/Windows.Graphics.h>

namespace duct {

namespace xaml = winrt::Microsoft::UI::Xaml;

// Global state for the app launch callback
static std::wstring g_title;
static int g_width, g_height;
static bool g_full_screen;
static std::shared_ptr<Component> g_component;
static std::function<Element(RenderContext&)> g_render_func;
static std::unique_ptr<DuctHost> g_host;

struct DuctApp : xaml::ApplicationT<DuctApp, winrt::Microsoft::UI::Xaml::Markup::IXamlMetadataProvider>
{
    // Delegate to XamlControlsXamlMetaDataProvider for theme resource type resolution.
    // Without this, XamlControlsResources{} fails with "Cannot find resource" errors.
    winrt::Microsoft::UI::Xaml::XamlTypeInfo::XamlControlsXamlMetaDataProvider provider_;

    winrt::Microsoft::UI::Xaml::Markup::IXamlType GetXamlType(winrt::Windows::UI::Xaml::Interop::TypeName const& type)
    { return provider_.GetXamlType(type); }

    winrt::Microsoft::UI::Xaml::Markup::IXamlType GetXamlType(winrt::hstring const& fullName)
    { return provider_.GetXamlType(fullName); }

    winrt::com_array<winrt::Microsoft::UI::Xaml::Markup::XmlnsDefinition> GetXmlnsDefinitions()
    { return provider_.GetXmlnsDefinitions(); }

    void OnLaunched(xaml::LaunchActivatedEventArgs const&)
    {
        Resources().MergedDictionaries().Append(xaml::Controls::XamlControlsResources{});

        auto window = xaml::Window{};
        window.Title(winrt::hstring(g_title));

        if (auto app_window = window.AppWindow()) {
            if (g_full_screen) {
                app_window.SetPresenter(winrt::Microsoft::UI::Windowing::AppWindowPresenterKind::FullScreen);
            } else {
                app_window.Resize({ g_width, g_height });
            }
        }

        g_host = std::make_unique<DuctHost>(window);

        if (g_component) {
            g_host->mount(g_component);
        } else if (g_render_func) {
            g_host->mount(g_render_func);
        }

        window.Activate();
    }
};

void run(const std::wstring& title,
         std::function<Element(RenderContext&)> render_func,
         int width, int height, bool full_screen) {
    run_impl(title, nullptr, std::move(render_func), width, height, full_screen);
}

void run_impl(const std::wstring& title,
              std::shared_ptr<Component> component,
              std::function<Element(RenderContext&)> render_func,
              int width, int height, bool full_screen) {
    g_title = title;
    g_width = width;
    g_height = height;
    g_full_screen = full_screen;
    g_component = std::move(component);
    g_render_func = std::move(render_func);

    winrt::init_apartment(winrt::apartment_type::single_threaded);
    xaml::Application::Start([](auto&&) { winrt::make<DuctApp>(); });
}

} // namespace duct
