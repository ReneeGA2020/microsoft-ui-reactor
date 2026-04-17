# Bug: Component memoization blocks child-initiated re-renders

**Date**: 2026-04-08
**Affected commits**: e955fe1 (context system / component memoization) through 7fea25f
**Fixed in**: Reconciler.Mount.cs, Reconciler.cs

## Symptom

In any component tree where a child component calls a state setter during render (or any child-initiated `setState`), the re-render never reaches the child because an ancestor's memo check short-circuits the subtree.

Concrete example from PIX docking:
- `DockPanelFrame` lazily creates content on first activation by calling `setContent(created)` during its initial render.
- The content never appears because `DockingHost` (an ancestor) has unchanged props and skips re-render.
- Tabs appear but switching them never shows content.

## Root cause

`ReconcileComponent` introduced a memo check that skips re-rendering a component when:
1. `SelfTriggered` is false, AND
2. Props/context haven't changed

`CreateComponentRerender(wrapper, requestRerender)` correctly marks the **immediate** component as `SelfTriggered`, then calls the parent `requestRerender`. But `MountComponent` passed the **raw** `requestRerender` (from its own parameter) to `Mount(childElement, ...)` for mounting child elements. This meant the callback chain was:

```
DockPanelFrame.setState()
  -> CreateComponentRerender(panelFrameWrapper, hostRerender)
       -> sets DockPanelFrame.SelfTriggered = true
       -> calls hostRerender (root ReactorHost callback, skipping all intermediate components)
```

When the root re-renders, it walks the tree top-down. `DockingHost` has `SelfTriggered = false` and unchanged props, so the memo check returns early. The entire subtree (including the dirty `DockPanelFrame`) is never visited.

## Fix

Pass the component's own wrapped rerender callback to child mounts, so the chain becomes:

```
DockPanelFrame.setState()
  -> CreateComponentRerender(panelFrameWrapper, tabGroupRerender)
       -> sets DockPanelFrame.SelfTriggered = true
       -> CreateComponentRerender(tabGroupWrapper, dockingHostRerender)
            -> sets DockTabGroup.SelfTriggered = true
            -> CreateComponentRerender(dockingHostWrapper, hostRerender)
                 -> sets DockingHost.SelfTriggered = true
                 -> calls hostRerender
```

Now every ancestor has `SelfTriggered = true`, so the memo check is bypassed for the dirty path. Sibling components with unchanged props are still correctly skipped.

### Files changed

| File | Change |
|------|--------|
| `Reconciler.Mount.cs` | `MountComponent`, `MountFuncComponent`, `MountMemoComponent`: pass `componentRerender` (not raw `requestRerender`) to `Mount(childElement, ...)` |
| `Reconciler.cs` | `ReconcileComponent`: pass `componentRerender` (not raw `requestRerender`) to `Reconcile(...)` |
| `Reconciler.cs` | Added `MemoElement` branch in `ReconcileComponent` render switch (was falling through to `else return;`) |

## Regression test specification

### Test 1: Child setState propagates through ancestor memo checks

Setup a three-level component tree: `Parent > Middle > Child`.

- `Parent` is a `Component<ParentProps>` that renders `Middle`.
- `Middle` is a `Component<MiddleProps>` that renders `Child`.
- `Child` is a `Component<ChildProps>` that calls `setState` on mount (during the first render).

After the initial mount + one re-render cycle:

- **Assert**: `Child`'s state-updated content is visible in the mounted control tree.
- **Assert**: `Parent` and `Middle` were re-entered by the reconciler (their `Render()` was called at least twice: once for mount, once for the child-triggered re-render).

Variant: use `Func()` and `Memo()` components instead of class components to cover all three paths.

### Test 2: Memo check still skips siblings

Setup: `Parent` renders two children, `DirtyChild` (calls setState) and `CleanChild` (pure, no state changes, stable props).

After `DirtyChild` triggers a re-render:

- **Assert**: `DirtyChild` re-renders (SelfTriggered propagation works).
- **Assert**: `CleanChild`'s `Render()` is NOT called again (memo check correctly skips it since props are unchanged and it's not on the dirty path).

### Test 3: MemoElement re-renders on update

Setup: a `MemoElement` with dependencies. Change the dependencies and trigger a reconcile.

- **Assert**: The `MemoElement` re-renders with the new dependencies (doesn't fall through to `else return`).

### Test 4: During-render setState with deep nesting (integration)

Reproduce the exact PIX pattern: a component that lazily creates content via `setState` during its first render, nested 3+ levels deep inside components with stable props.

- **Assert**: The lazily-created content appears in the visual tree after at most two render cycles.
- **Assert**: Ancestor components with unchanged props are correctly re-entered when the child's SelfTriggered propagates up.
