using System.Collections.ObjectModel;
using System.ComponentModel;
using Duct.Core;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Tests for Feature 6: Observable/Binding Interop Hooks.
/// Tests the hook behavior via RenderContext directly (no UI thread needed).
/// </summary>
public class ObservableHookTests
{
    private class NotifyModel : INotifyPropertyChanged
    {
        private string _name = "";
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }

        private int _count;
        public int Count
        {
            get => _count;
            set
            {
                if (_count != value)
                {
                    _count = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    // ── UseObservable ──────────────────────────────────────────────

    [Fact]
    public void UseObservable_Returns_Source()
    {
        var ctx = new RenderContext();
        var model = new NotifyModel { Name = "Alice" };

        ctx.BeginRender(() => { });
        var result = ctx.UseObservable(model);
        ctx.FlushEffects();

        Assert.Same(model, result);
    }

    [Fact]
    public void UseObservable_Triggers_Rerender_On_PropertyChanged()
    {
        var ctx = new RenderContext();
        var model = new NotifyModel { Name = "Alice" };
        int rerenderCount = 0;

        // First render
        ctx.BeginRender(() => rerenderCount++);
        ctx.UseObservable(model);
        ctx.FlushEffects();

        Assert.Equal(0, rerenderCount);

        // Simulate property change
        // Need to begin a new render first so the rerender callback is fresh
        ctx.BeginRender(() => rerenderCount++);
        ctx.UseObservable(model);
        ctx.FlushEffects();

        model.Name = "Bob";
        Assert.Equal(1, rerenderCount);
    }

    [Fact]
    public void UseObservable_Cleanup_Unsubscribes()
    {
        var ctx = new RenderContext();
        var model = new NotifyModel { Name = "Alice" };
        int rerenderCount = 0;

        ctx.BeginRender(() => rerenderCount++);
        ctx.UseObservable(model);
        ctx.FlushEffects();

        // Run cleanups (simulating unmount)
        ctx.RunCleanups();

        // Changes should no longer trigger rerender
        model.Name = "Charlie";
        Assert.Equal(0, rerenderCount);
    }

    // ── UseObservableProperty ──────────────────────────────────────

    [Fact]
    public void UseObservableProperty_Returns_Property_Value()
    {
        var ctx = new RenderContext();
        var model = new NotifyModel { Name = "Alice" };

        ctx.BeginRender(() => { });
        var name = ctx.UseObservableProperty(model, m => m.Name, nameof(NotifyModel.Name));
        ctx.FlushEffects();

        Assert.Equal("Alice", name);
    }

    [Fact]
    public void UseObservableProperty_Only_Rerenders_For_Matching_Property()
    {
        var ctx = new RenderContext();
        var model = new NotifyModel { Name = "Alice", Count = 0 };
        int rerenderCount = 0;

        ctx.BeginRender(() => rerenderCount++);
        ctx.UseObservableProperty(model, m => m.Name, nameof(NotifyModel.Name));
        ctx.FlushEffects();

        ctx.BeginRender(() => rerenderCount++);
        ctx.UseObservableProperty(model, m => m.Name, nameof(NotifyModel.Name));
        ctx.FlushEffects();

        // Changing a different property should NOT trigger rerender
        model.Count = 42;
        Assert.Equal(0, rerenderCount);

        // Changing the watched property SHOULD trigger rerender
        model.Name = "Bob";
        Assert.Equal(1, rerenderCount);
    }

    // ── UseCollection ──────────────────────────────────────────────

    [Fact]
    public void UseCollection_Returns_Collection_As_ReadOnlyList()
    {
        var ctx = new RenderContext();
        var items = new ObservableCollection<string> { "A", "B", "C" };

        ctx.BeginRender(() => { });
        var result = ctx.UseCollection(items);
        ctx.FlushEffects();

        Assert.Equal(3, result.Count);
        Assert.Equal("A", result[0]);
        Assert.Equal("B", result[1]);
        Assert.Equal("C", result[2]);
    }

    [Fact]
    public void UseCollection_Triggers_Rerender_On_Add()
    {
        var ctx = new RenderContext();
        var items = new ObservableCollection<string> { "A" };
        int rerenderCount = 0;

        ctx.BeginRender(() => rerenderCount++);
        ctx.UseCollection(items);
        ctx.FlushEffects();

        ctx.BeginRender(() => rerenderCount++);
        ctx.UseCollection(items);
        ctx.FlushEffects();

        items.Add("B");
        Assert.Equal(1, rerenderCount);
    }

    [Fact]
    public void UseCollection_Triggers_Rerender_On_Remove()
    {
        var ctx = new RenderContext();
        var items = new ObservableCollection<string> { "A", "B" };
        int rerenderCount = 0;

        ctx.BeginRender(() => rerenderCount++);
        ctx.UseCollection(items);
        ctx.FlushEffects();

        ctx.BeginRender(() => rerenderCount++);
        ctx.UseCollection(items);
        ctx.FlushEffects();

        items.Remove("A");
        Assert.Equal(1, rerenderCount);
    }

    [Fact]
    public void UseCollection_Cleanup_Unsubscribes()
    {
        var ctx = new RenderContext();
        var items = new ObservableCollection<string> { "A" };
        int rerenderCount = 0;

        ctx.BeginRender(() => rerenderCount++);
        ctx.UseCollection(items);
        ctx.FlushEffects();

        ctx.RunCleanups();

        items.Add("B");
        Assert.Equal(0, rerenderCount);
    }
}
