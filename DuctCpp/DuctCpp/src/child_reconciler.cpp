#include "pch.h"
#include "child_reconciler.h"
#include "reconciler.h"
#include <unordered_map>
#include <algorithm>
#include <sstream>

namespace duct {

// --- Helpers ---

std::vector<const Element*> ChildReconciler::filter(const std::vector<Element>& elements) {
    std::vector<const Element*> result;
    result.reserve(elements.size());
    for (const auto& el : elements) {
        if (!std::holds_alternative<EmptyElement>(el.data)) {
            result.push_back(&el);
        }
    }
    return result;
}

bool ChildReconciler::has_any_keys(const std::vector<const Element*>& elements) {
    for (const auto* el : elements) {
        if (el->key.has_value()) return true;
    }
    return false;
}

bool ChildReconciler::key_match(const Element& a, const Element& b) {
    // Both must have the same type AND same key (or both no key)
    if (a.data.index() != b.data.index()) return false;
    return a.key == b.key;
}

std::string ChildReconciler::get_key(const Element& element, int positional_index) {
    if (element.key.has_value()) return *element.key;
    return "__pos_" + std::to_string(positional_index) + "_" + std::to_string(element.data.index());
}

// --- Main Entry Point ---

void ChildReconciler::reconcile(
    const std::vector<Element>& old_children,
    const std::vector<Element>& new_children,
    IChildCollection& children,
    Reconciler& reconciler,
    std::function<void()> request_rerender)
{
    auto old_filtered = filter(old_children);
    auto new_filtered = filter(new_children);

    bool has_keys = has_any_keys(old_filtered) || has_any_keys(new_filtered);

    if (has_keys)
        reconcile_keyed(old_filtered, new_filtered, children, reconciler, request_rerender);
    else
        reconcile_positional(old_filtered, new_filtered, children, reconciler, request_rerender);
}

// --- Positional Reconciliation ---

void ChildReconciler::reconcile_positional(
    const std::vector<const Element*>& old_children,
    const std::vector<const Element*>& new_children,
    IChildCollection& children,
    Reconciler& reconciler,
    std::function<void()> request_rerender)
{
    int old_count = static_cast<int>(old_children.size());
    int new_count = static_cast<int>(new_children.size());
    int common = std::min(old_count, new_count);

    // Update common children in place
    for (int i = 0; i < common; i++) {
        if (i >= children.count()) break;

        if (Reconciler::can_update(*old_children[i], *new_children[i])) {
            auto replacement = reconciler.update(*old_children[i], *new_children[i],
                                                  children.get(i), request_rerender);
            if (replacement) {
                // update() returned a replacement control
                reconciler.unmount(children.get(i));
                children.replace(i, replacement);
            }
        } else {
            // Type mismatch — unmount old, mount new
            reconciler.unmount(children.get(i));
            auto new_control = reconciler.mount(*new_children[i], request_rerender);
            if (new_control)
                children.replace(i, new_control);
        }
    }

    // Remove excess old children (from end to start for stable indices)
    for (int i = children.count() - 1; i >= common; i--) {
        auto old_ctrl = children.get(i);
        children.remove_at(i);
        reconciler.unmount(old_ctrl);
    }

    // Insert new children beyond old count
    for (int i = common; i < new_count; i++) {
        auto ctrl = reconciler.mount(*new_children[i], request_rerender);
        if (ctrl)
            children.insert(children.count(), ctrl);
    }
}

// --- Keyed Reconciliation ---

void ChildReconciler::reconcile_keyed(
    const std::vector<const Element*>& old_children,
    const std::vector<const Element*>& new_children,
    IChildCollection& children,
    Reconciler& reconciler,
    std::function<void()> request_rerender)
{
    int old_len = static_cast<int>(old_children.size());
    int new_len = static_cast<int>(new_children.size());

    // Phase 1: Common prefix
    int prefix_len = 0;
    while (prefix_len < old_len && prefix_len < new_len &&
           key_match(*old_children[prefix_len], *new_children[prefix_len]) &&
           Reconciler::can_update(*old_children[prefix_len], *new_children[prefix_len]))
    {
        if (prefix_len < children.count()) {
            auto replacement = reconciler.update(*old_children[prefix_len], *new_children[prefix_len],
                                                  children.get(prefix_len), request_rerender);
            if (replacement) {
                reconciler.unmount(children.get(prefix_len));
                children.replace(prefix_len, replacement);
            }
        }
        prefix_len++;
    }

    // Phase 2: Common suffix
    int suffix_len = 0;
    while (suffix_len < (old_len - prefix_len) && suffix_len < (new_len - prefix_len) &&
           key_match(*old_children[old_len - 1 - suffix_len], *new_children[new_len - 1 - suffix_len]) &&
           Reconciler::can_update(*old_children[old_len - 1 - suffix_len], *new_children[new_len - 1 - suffix_len]))
    {
        int old_idx = old_len - 1 - suffix_len;
        int panel_idx = children.count() - 1 - suffix_len;
        if (panel_idx >= 0 && panel_idx < children.count()) {
            auto replacement = reconciler.update(*old_children[old_idx], *new_children[new_len - 1 - suffix_len],
                                                  children.get(panel_idx), request_rerender);
            if (replacement) {
                reconciler.unmount(children.get(panel_idx));
                children.replace(panel_idx, replacement);
            }
        }
        suffix_len++;
    }

    // Phase 3: Middle section
    int old_start = prefix_len;
    int old_end = old_len - suffix_len;
    int new_start = prefix_len;
    int new_end = new_len - suffix_len;

    int old_mid_len = old_end - old_start;
    int new_mid_len = new_end - new_start;

    if (old_mid_len == 0 && new_mid_len == 0)
        return; // Prefix + suffix covered everything

    if (old_mid_len == 0) {
        // Only insertions
        for (int i = 0; i < new_mid_len; i++) {
            auto ctrl = reconciler.mount(*new_children[new_start + i], request_rerender);
            if (ctrl)
                children.insert(prefix_len + i, ctrl);
        }
        return;
    }

    if (new_mid_len == 0) {
        // Only removals (from end to start)
        for (int i = old_mid_len - 1; i >= 0; i--) {
            int panel_idx = prefix_len + i;
            if (panel_idx < children.count()) {
                auto old_ctrl = children.get(panel_idx);
                children.remove_at(panel_idx);
                reconciler.unmount(old_ctrl);
            }
        }
        return;
    }

    // Middle section requires key mapping + LIS
    reconcile_keyed_middle(old_children, new_children, old_start, old_mid_len, new_start, new_mid_len,
                           prefix_len, suffix_len, children, reconciler, request_rerender);
}

// --- Keyed Middle Section (LIS) ---

void ChildReconciler::reconcile_keyed_middle(
    const std::vector<const Element*>& old_children,
    const std::vector<const Element*>& new_children,
    int old_start, int old_mid_len,
    int new_start, int new_mid_len,
    int prefix_len, int suffix_len,
    IChildCollection& children,
    Reconciler& reconciler,
    std::function<void()> request_rerender)
{
    // Build old key → index map
    std::unordered_map<std::string, int> old_key_map;
    old_key_map.reserve(old_mid_len);
    for (int i = 0; i < old_mid_len; i++) {
        auto key = get_key(*old_children[old_start + i], old_start + i);
        old_key_map[key] = i;
    }

    // Map new keys to old indices
    std::vector<int> new_to_old(new_mid_len);
    std::vector<bool> matched(old_mid_len, false);
    for (int i = 0; i < new_mid_len; i++) {
        auto key = get_key(*new_children[new_start + i], new_start + i);
        auto it = old_key_map.find(key);
        if (it != old_key_map.end() &&
            Reconciler::can_update(*old_children[old_start + it->second], *new_children[new_start + i]))
        {
            new_to_old[i] = it->second;
            matched[it->second] = true;
        } else {
            new_to_old[i] = -1;
        }
    }

    // Compute LIS on new_to_old
    auto lis_indices = compute_lis(new_to_old);

    // Step 1: Remove unmatched old items (reverse order for stable indices)
    for (int i = old_mid_len - 1; i >= 0; i--) {
        if (!matched[i]) {
            int panel_idx = prefix_len + i;
            if (panel_idx < children.count()) {
                auto old_ctrl = children.get(panel_idx);
                children.remove_at(panel_idx);
                reconciler.unmount(old_ctrl);
            }
        }
    }

    // Step 2: Process new items - insert new, move existing not in LIS
    for (int i = 0; i < new_mid_len; i++) {
        int target_panel_idx = prefix_len + i;

        if (new_to_old[i] == -1) {
            // New item — mount and insert
            auto ctrl = reconciler.mount(*new_children[new_start + i], request_rerender);
            if (ctrl)
                children.insert(target_panel_idx, ctrl);
        }
        else if (lis_indices.count(i)) {
            // In LIS — update in place (no move needed)
            if (target_panel_idx < children.count()) {
                auto replacement = reconciler.update(
                    *old_children[old_start + new_to_old[i]],
                    *new_children[new_start + i],
                    children.get(target_panel_idx),
                    request_rerender);
                if (replacement) {
                    reconciler.unmount(children.get(target_panel_idx));
                    children.replace(target_panel_idx, replacement);
                }
            }
        }
        else {
            // Not in LIS — needs to be moved
            int old_rel_idx = new_to_old[i];
            int current_pos = find_item_by_old_index(
                children, old_children, old_start + old_rel_idx,
                prefix_len, children.count() - suffix_len, reconciler);
            if (current_pos >= 0 && current_pos != target_panel_idx) {
                children.move(current_pos, target_panel_idx);
            }
            if (target_panel_idx < children.count()) {
                auto replacement = reconciler.update(
                    *old_children[old_start + old_rel_idx],
                    *new_children[new_start + i],
                    children.get(target_panel_idx),
                    request_rerender);
                if (replacement) {
                    reconciler.unmount(children.get(target_panel_idx));
                    children.replace(target_panel_idx, replacement);
                }
            }
        }
    }
}

// --- Find Item by Old Index ---

int ChildReconciler::find_item_by_old_index(
    IChildCollection& children,
    const std::vector<const Element*>& old_elements,
    int old_index,
    int search_start, int search_end,
    Reconciler& /*reconciler*/)
{
    for (int i = search_start; i < search_end && i < children.count(); i++) {
        auto child = children.get(i);
        if (auto fe = child.try_as<xaml::FrameworkElement>()) {
            auto tag = fe.Tag();
            if (tag) {
                // Tag stores a pointer to the Element as a uint64
                auto ptr_val = winrt::unbox_value_or<uint64_t>(tag, 0);
                if (ptr_val != 0) {
                    auto* tag_el = reinterpret_cast<const Element*>(ptr_val);
                    if (old_index < static_cast<int>(old_elements.size()) &&
                        get_key(*tag_el, -1) == get_key(*old_elements[old_index], old_index))
                        return i;
                }
            }
        }
    }
    return -1;
}

// --- Longest Increasing Subsequence ---

std::unordered_set<int> ChildReconciler::compute_lis(const std::vector<int>& arr) {
    if (arr.empty()) return {};

    std::vector<int> tails;        // Smallest tail values
    std::vector<int> tail_indices; // Indices in arr corresponding to tails
    std::vector<int> predecessors(arr.size(), -1);

    for (int i = 0; i < static_cast<int>(arr.size()); i++) {
        if (arr[i] == -1) continue; // Skip unmapped

        int val = arr[i];

        // Binary search for insertion position
        int lo = 0, hi = static_cast<int>(tails.size());
        while (lo < hi) {
            int mid = (lo + hi) / 2;
            if (tails[mid] < val) lo = mid + 1;
            else hi = mid;
        }

        if (lo == static_cast<int>(tails.size())) {
            tails.push_back(val);
            tail_indices.push_back(i);
        } else {
            tails[lo] = val;
            tail_indices[lo] = i;
        }

        if (lo > 0)
            predecessors[i] = tail_indices[lo - 1];
    }

    // Backtrack to find actual LIS indices
    std::unordered_set<int> result;
    if (tail_indices.empty()) return result;

    int idx = tail_indices.back();
    while (idx != -1) {
        result.insert(idx);
        idx = predecessors[idx];
    }

    return result;
}

} // namespace duct
