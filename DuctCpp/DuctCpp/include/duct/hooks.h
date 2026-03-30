#pragma once

#include <any>
#include <vector>
#include <functional>
#include <memory>
#include <cassert>
#include <typeindex>
#include <utility>

namespace duct {

// Hook state storage — type-erased via std::any
struct HookSlot {
    std::type_index type;
    std::any value;

    template<typename T>
    HookSlot(T&& v) : type(typeid(std::decay_t<T>)), value(std::forward<T>(v)) {}
};

// Effect state
struct EffectState {
    std::function<std::function<void()>()> effect;
    std::function<void()> cleanup;
    std::vector<std::any> deps;
    bool needs_run = true;
};

// Ref wrapper
template<typename T>
struct Ref {
    T current;
};

class RenderContext {
public:
    RenderContext() = default;

    // Set the re-render callback (called by the host)
    void set_request_render(std::function<void()> callback) {
        request_render_ = std::move(callback);
    }

    // Lifecycle
    void begin_render() { hook_index_ = 0; }

    void flush_effects() {
        for (auto& slot : hook_state_) {
            if (slot.type == typeid(EffectState)) {
                auto& effect = std::any_cast<EffectState&>(slot.value);
                if (effect.needs_run) {
                    if (effect.cleanup) effect.cleanup();
                    effect.cleanup = effect.effect();
                    effect.needs_run = false;
                }
            }
        }
    }

    void cleanup_all_effects() {
        for (auto& slot : hook_state_) {
            if (slot.type == typeid(EffectState)) {
                auto& effect = std::any_cast<EffectState&>(slot.value);
                if (effect.cleanup) {
                    effect.cleanup();
                    effect.cleanup = nullptr;
                }
            }
        }
    }

    // --- use_state ---
    template<typename T>
    std::pair<T, std::function<void(T)>> use_state(T initial) {
        size_t idx = hook_index_++;

        if (idx >= hook_state_.size()) {
            // First render — initialize
            hook_state_.emplace_back(std::make_shared<T>(std::move(initial)));
        }

        auto& slot = hook_state_[idx];
        assert(slot.type == typeid(std::shared_ptr<T>));
        auto state_ptr = std::any_cast<std::shared_ptr<T>>(slot.value);
        T current_value = *state_ptr;

        auto request_render = request_render_;
        auto setter = [state_ptr, request_render](T new_value) {
            if constexpr (requires { *state_ptr == new_value; }) {
                if (*state_ptr == new_value) {
#ifdef DUCT_DEBUG_LOG
                    OutputDebugStringA("STATE_SET: value unchanged, skipping\n");
#endif
                    return;
                }
            }
#ifdef DUCT_DEBUG_LOG
            OutputDebugStringA("STATE_SET: value changed, requesting render\n");
#endif
            *state_ptr = std::move(new_value);
            if (request_render) {
                request_render();
            } else {
#ifdef DUCT_DEBUG_LOG
                OutputDebugStringA("STATE_SET: WARNING no request_render callback!\n");
#endif
            }
        };

        return { current_value, std::move(setter) };
    }

    // --- use_reducer ---
    template<typename T>
    std::pair<T, std::function<void(std::function<T(T)>)>> use_reducer(T initial) {
        size_t idx = hook_index_++;

        if (idx >= hook_state_.size()) {
            hook_state_.emplace_back(std::make_shared<T>(std::move(initial)));
        }

        auto& slot = hook_state_[idx];
        assert(slot.type == typeid(std::shared_ptr<T>));
        auto state_ptr = std::any_cast<std::shared_ptr<T>>(slot.value);
        T current_value = *state_ptr;

        auto request_render = request_render_;
        auto dispatch = [state_ptr, request_render](std::function<T(T)> updater) {
            *state_ptr = updater(*state_ptr);
            if (request_render) request_render();
        };

        return { current_value, std::move(dispatch) };
    }

    // --- use_effect ---
    void use_effect(std::function<std::function<void()>()> effect,
                    std::vector<std::any> deps = {}) {
        size_t idx = hook_index_++;

        if (idx >= hook_state_.size()) {
            // First render — schedule effect
            EffectState es;
            es.effect = std::move(effect);
            es.deps = std::move(deps);
            es.needs_run = true;
            hook_state_.emplace_back(std::move(es));
            return;
        }

        auto& slot = hook_state_[idx];
        assert(slot.type == typeid(EffectState));
        auto& es = std::any_cast<EffectState&>(slot.value);

        // Check if deps changed
        if (deps_changed(es.deps, deps)) {
            es.effect = std::move(effect);
            es.deps = std::move(deps);
            es.needs_run = true;
        }
    }

    // --- use_memo ---
    template<typename T>
    T use_memo(std::function<T()> factory, std::vector<std::any> deps) {
        size_t idx = hook_index_++;

        struct MemoState {
            T value;
            std::vector<std::any> deps;
        };

        if (idx >= hook_state_.size()) {
            MemoState ms{ factory(), std::move(deps) };
            T result = ms.value;
            hook_state_.emplace_back(std::move(ms));
            return result;
        }

        auto& slot = hook_state_[idx];
        assert(slot.type == typeid(MemoState));
        auto& ms = std::any_cast<MemoState&>(slot.value);

        if (deps_changed(ms.deps, deps)) {
            ms.value = factory();
            ms.deps = std::move(deps);
        }

        return ms.value;
    }

    // --- use_ref ---
    template<typename T>
    std::shared_ptr<Ref<T>> use_ref(T initial) {
        size_t idx = hook_index_++;

        if (idx >= hook_state_.size()) {
            auto ref = std::make_shared<Ref<T>>(Ref<T>{ std::move(initial) });
            hook_state_.emplace_back(ref);
            return ref;
        }

        auto& slot = hook_state_[idx];
        assert(slot.type == typeid(std::shared_ptr<Ref<T>>));
        return std::any_cast<std::shared_ptr<Ref<T>>>(slot.value);
    }

private:
    // Compare two dep arrays using std::any
    static bool deps_changed(const std::vector<std::any>& old_deps,
                             const std::vector<std::any>& new_deps) {
        if (old_deps.size() != new_deps.size()) return true;
        for (size_t i = 0; i < old_deps.size(); ++i) {
            if (old_deps[i].type() != new_deps[i].type()) return true;
            // Use type-erased comparison via hash — we compare by serialized representation
            // For simple types, this works. For complex types, always re-run.
            if (!any_equal(old_deps[i], new_deps[i])) return true;
        }
        return false;
    }

    // Type-erased equality comparison for std::any
    // Supports common types; defaults to "changed" for unknown types
    static bool any_equal(const std::any& a, const std::any& b) {
        if (a.type() != b.type()) return false;

        // Check common types
        if (a.type() == typeid(int)) return std::any_cast<int>(a) == std::any_cast<int>(b);
        if (a.type() == typeid(double)) return std::any_cast<double>(a) == std::any_cast<double>(b);
        if (a.type() == typeid(float)) return std::any_cast<float>(a) == std::any_cast<float>(b);
        if (a.type() == typeid(bool)) return std::any_cast<bool>(a) == std::any_cast<bool>(b);
        if (a.type() == typeid(std::string)) return std::any_cast<std::string>(a) == std::any_cast<std::string>(b);
        if (a.type() == typeid(size_t)) return std::any_cast<size_t>(a) == std::any_cast<size_t>(b);

        // Unknown type — assume changed
        return false;
    }

    size_t hook_index_ = 0;
    std::vector<HookSlot> hook_state_;
    std::function<void()> request_render_;
};

// --- Dependency array helper ---
// Instead of { std::any(a), std::any(b) }, write deps(a, b)
template<typename... Args>
std::vector<std::any> deps(Args&&... args) {
    return { std::any(std::forward<Args>(args))... };
}

} // namespace duct
