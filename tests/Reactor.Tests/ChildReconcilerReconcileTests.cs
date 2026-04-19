#if CHILD_RECONCILER_RECONCILE_TESTS
// ⚠️ Disabled — blocked by the selftest wall.
//
// This file attempted to exercise ChildReconciler.Reconcile's full surface
// (ReconcilePositional, ReconcileKeyed prefix/suffix, ReconcileKeyedMiddle + LIS)
// using a custom-registered element type whose mount handler returned a real Border.
// That plan fails in CI: `new Border()` throws COMException without a full XAML
// Application host, so registered-type Mount can't produce a UIElement here.
//
// Conclusion: the ~310 uncovered lines in ReconcilePositional/ReconcileKeyed/
// ReconcileKeyedMiddle genuinely need selftest (real StackPanel + dispatcher).
// Only the helpers (ComputeLIS, Filter, HasAnyKeys, KeyMatch, GetKey) are
// unit-testable in CI — ComputeLIS/HasAnyKeys are already covered by
// ChildReconcilerTests + ChildReconcilerIntegrationTests. Filter/KeyMatch/GetKey
// are the only remaining CI-reachable surface and are small.

using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Xunit;

namespace Microsoft.UI.Reactor.Tests;

public class ChildReconcilerReconcileTests
{
    private static readonly Action NoOp = () => { };

    private record Item(string Label) : Element;

    private static Reconciler MakeReconciler()
    {
        var r = new Reconciler();
        r.RegisterType<Item, Border>(
            mount: (_, el, _) =>
            {
                var b = new Border();
                b.Tag = el;
                return b;
            },
            update: (_, _, newEl, ctrl, _) =>
            {
                ctrl.Tag = newEl;
                return null;
            });
        return r;
    }

    private sealed class Recorder : IChildCollection
    {
        private readonly List<UIElement> _items = new();
        public List<string> Ops { get; } = new();
        public int Count => _items.Count;
        public UIElement Get(int index) => _items[index];
        public void Insert(int index, UIElement element) { _items.Insert(index, element); Ops.Add($"Insert({index})"); }
        public void RemoveAt(int index) { _items.RemoveAt(index); Ops.Add($"RemoveAt({index})"); }
        public void Move(int oldIndex, int newIndex) { var it = _items[oldIndex]; _items.RemoveAt(oldIndex); _items.Insert(newIndex, it); Ops.Add($"Move({oldIndex},{newIndex})"); }
        public void Replace(int index, UIElement element) { _items[index] = element; Ops.Add($"Replace({index})"); }
    }
}
#endif
