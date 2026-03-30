#pragma once

#include <duct/element.h>
#include <vector>
#include <functional>
#include <unordered_set>
#include <cassert>

// WinRT forward declarations
#include <winrt/Microsoft.UI.Xaml.h>
#include <winrt/Microsoft.UI.Xaml.Controls.h>

namespace duct {

namespace xaml = winrt::Microsoft::UI::Xaml;
namespace controls = winrt::Microsoft::UI::Xaml::Controls;

class Reconciler;

// --- IChildCollection abstraction ---
// Wraps Panel.Children() or similar, allowing the reconciler to work with any container.

class IChildCollection {
public:
    virtual ~IChildCollection() = default;
    virtual int count() const = 0;
    virtual xaml::UIElement get(int index) const = 0;
    virtual void insert(int index, xaml::UIElement element) = 0;
    virtual void remove_at(int index) = 0;
    virtual void move(int old_index, int new_index) = 0;
    virtual void replace(int index, xaml::UIElement element) = 0;
};

// Wraps Panel.Children (StackPanel, Grid, etc.)
class PanelChildCollection : public IChildCollection {
public:
    explicit PanelChildCollection(controls::Panel panel)
        : children_(panel.Children()) {}

    int count() const override {
        return static_cast<int>(children_.Size());
    }

    xaml::UIElement get(int index) const override {
        return children_.GetAt(static_cast<uint32_t>(index));
    }

    void insert(int index, xaml::UIElement element) override {
        children_.InsertAt(static_cast<uint32_t>(index), element);
    }

    void remove_at(int index) override {
        children_.RemoveAt(static_cast<uint32_t>(index));
    }

    void move(int old_index, int new_index) override {
        if (old_index == new_index) return;
        assert(old_index >= 0 && old_index < count());
        assert(new_index >= 0 && new_index < count());
        auto item = children_.GetAt(static_cast<uint32_t>(old_index));
        children_.RemoveAt(static_cast<uint32_t>(old_index));
        children_.InsertAt(static_cast<uint32_t>(new_index), item);
    }

    void replace(int index, xaml::UIElement element) override {
        children_.SetAt(static_cast<uint32_t>(index), element);
    }

private:
    winrt::Microsoft::UI::Xaml::Controls::UIElementCollection children_;
};

// --- ChildReconciler ---
// Keyed child reconciliation using Longest Increasing Subsequence (LIS).
//
// Strategies:
//   1. Unkeyed children: positional reconciliation (match by index)
//   2. Keyed children: prefix/suffix stripping + LIS for minimal moves

class ChildReconciler {
public:
    // Main entry point: reconcile old and new child element arrays against a child collection.
    static void reconcile(
        const std::vector<Element>& old_children,
        const std::vector<Element>& new_children,
        IChildCollection& children,
        Reconciler& reconciler,
        std::function<void()> request_rerender);

    // Compute Longest Increasing Subsequence.
    // Returns indices into the input array that form the LIS.
    // Skips entries with value -1 (unmapped items).
    // O(n log n) using patience sorting.
    static std::unordered_set<int> compute_lis(const std::vector<int>& arr);

private:
    // Positional reconciliation: match children by index.
    // O(max(old, new)) — no reorder detection, but simple and fast.
    static void reconcile_positional(
        const std::vector<const Element*>& old_children,
        const std::vector<const Element*>& new_children,
        IChildCollection& children,
        Reconciler& reconciler,
        std::function<void()> request_rerender);

    // Keyed reconciliation using prefix/suffix stripping + LIS.
    // Minimizes DOM operations for reordered lists.
    static void reconcile_keyed(
        const std::vector<const Element*>& old_children,
        const std::vector<const Element*>& new_children,
        IChildCollection& children,
        Reconciler& reconciler,
        std::function<void()> request_rerender);

    // Keyed middle section reconciliation using LIS for minimal moves.
    static void reconcile_keyed_middle(
        const std::vector<const Element*>& old_children,
        const std::vector<const Element*>& new_children,
        int old_start, int old_mid_len,
        int new_start, int new_mid_len,
        int prefix_len, int suffix_len,
        IChildCollection& children,
        Reconciler& reconciler,
        std::function<void()> request_rerender);

    // Filter out empty elements
    static std::vector<const Element*> filter(const std::vector<Element>& elements);

    // Check if any elements have keys
    static bool has_any_keys(const std::vector<const Element*>& elements);

    // Check if two elements match by key (and type)
    static bool key_match(const Element& a, const Element& b);

    // Get effective key for an element (explicit key or positional fallback)
    static std::string get_key(const Element& element, int positional_index);

    // Find the current position of an item by its old index (via tag matching)
    static int find_item_by_old_index(
        IChildCollection& children,
        const std::vector<const Element*>& old_elements,
        int old_index,
        int search_start, int search_end,
        Reconciler& reconciler);
};

} // namespace duct
