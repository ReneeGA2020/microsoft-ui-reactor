#pragma once

#include "element.h"
#include "hooks.h"

namespace duct {

class Component {
public:
    virtual ~Component() = default;
    virtual Element render() = 0;

    // Lifecycle — called by the host
    RenderContext& context() { return *context_; }
    void set_context(std::unique_ptr<RenderContext> ctx) { context_ = std::move(ctx); }
    bool has_context() const { return context_ != nullptr; }

    void begin_render() {
        if (!context_) context_ = std::make_unique<RenderContext>();
        context_->begin_render();
    }

    void flush_effects() {
        if (context_) context_->flush_effects();
    }

    void cleanup() {
        if (context_) context_->cleanup_all_effects();
    }

protected:
    // --- Hook accessors (delegate to context) ---

    template<typename T>
    std::pair<T, std::function<void(T)>> use_state(T initial) {
        return context_->use_state<T>(std::move(initial));
    }

    template<typename T>
    std::pair<T, std::function<void(std::function<T(T)>)>> use_reducer(T initial) {
        return context_->use_reducer<T>(std::move(initial));
    }

    void use_effect(std::function<std::function<void()>()> effect,
                    std::vector<std::any> deps = {}) {
        context_->use_effect(std::move(effect), std::move(deps));
    }

    template<typename T>
    T use_memo(std::function<T()> factory, std::vector<std::any> deps) {
        return context_->use_memo<T>(std::move(factory), std::move(deps));
    }

    template<typename T>
    std::shared_ptr<Ref<T>> use_ref(T initial) {
        return context_->use_ref<T>(std::move(initial));
    }

private:
    std::unique_ptr<RenderContext> context_;
};

} // namespace duct
