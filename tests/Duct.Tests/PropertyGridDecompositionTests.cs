using Duct.PropertyGrid;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Tests for decomposition, immutable edit propagation, and Compose chains.
/// </summary>
public class PropertyGridDecompositionTests
{
    // ── Test models ───────────────────────────────────────────────

    private record ImmutablePoint(int X, int Y);

    private record ImmutableTheme(string Name, ImmutablePoint Origin);

    private record ImmutableConfig(string Label, ImmutableTheme Theme);

    private class MutableParent
    {
        public string Name { get; set; } = "";
        public ImmutablePoint Position { get; set; } = new(0, 0);
    }

    private class CompositeModel
    {
        public string Title { get; set; } = "";
        public NestedModel Details { get; set; } = new();
    }

    private class NestedModel
    {
        public string Description { get; set; } = "";
        public int Priority { get; set; }
    }

    // ── Composite type decomposition ──────────────────────────────

    [Fact]
    public void Composite_Type_Has_Decompose()
    {
        var registry = new TypeRegistry();
        var meta = registry.Resolve(typeof(NestedModel));

        Assert.NotNull(meta.Decompose);

        var model = new NestedModel { Description = "test", Priority = 5 };
        var descriptors = meta.Decompose!(model);

        Assert.Equal(2, descriptors.Count);
        Assert.Contains(descriptors, d => d.Name == "Description");
        Assert.Contains(descriptors, d => d.Name == "Priority");
    }

    [Fact]
    public void Editing_Decomposed_SubProperty_On_Mutable_Parent_Works()
    {
        var registry = new TypeRegistry();
        var parent = new CompositeModel
        {
            Title = "test",
            Details = new NestedModel { Description = "old", Priority = 1 }
        };

        var parentMeta = registry.Resolve(typeof(CompositeModel));
        var parentDescriptors = parentMeta.Decompose!(parent);
        var detailsDesc = parentDescriptors.First(d => d.Name == "Details");

        // The Details property is mutable (has a setter)
        var detailsValue = (NestedModel)detailsDesc.GetValue()!;
        var nestedMeta = registry.Resolve(typeof(NestedModel));
        var nestedDescriptors = nestedMeta.Decompose!(detailsValue);

        var descProp = nestedDescriptors.First(d => d.Name == "Description");
        Assert.NotNull(descProp.SetValue);
        descProp.SetValue!("new");
        Assert.Equal("new", parent.Details.Description);
    }

    // ── Immutable edit propagation via Compose ────────────────────

    [Fact]
    public void Immutable_Nested_Edit_Propagates_Via_Compose()
    {
        var registry = new TypeRegistry();

        // MutableParent.Position is an immutable ImmutablePoint
        var parent = new MutableParent { Name = "test", Position = new ImmutablePoint(10, 20) };

        var parentMeta = registry.Resolve(typeof(MutableParent));
        var parentDescriptors = parentMeta.Decompose!(parent);
        var positionDesc = parentDescriptors.First(d => d.Name == "Position");

        // Position is a record — Compose should be generated
        var pointMeta = registry.Resolve(typeof(ImmutablePoint));
        Assert.NotNull(pointMeta.Compose);

        // Simulate editing X on the immutable ImmutablePoint
        var currentPoint = (ImmutablePoint)positionDesc.GetValue()!;
        var newPoint = (ImmutablePoint)pointMeta.Compose!(currentPoint,
            new Dictionary<string, object> { { "X", 99 } });

        Assert.Equal(99, newPoint.X);
        Assert.Equal(20, newPoint.Y);

        // Write back to mutable parent
        positionDesc.SetValue!(newPoint);
        Assert.Equal(new ImmutablePoint(99, 20), parent.Position);
    }

    [Fact]
    public void Fully_Immutable_Root_Fires_OnRootChanged()
    {
        var registry = new TypeRegistry();
        var original = new ImmutablePoint(10, 20);

        var meta = registry.Resolve(typeof(ImmutablePoint));
        Assert.NotNull(meta.Compose);

        object? receivedRoot = null;
        var onRootChanged = (object newRoot) => { receivedRoot = newRoot; };

        // Simulate the edit chain
        var editChain = new EditChain(original, meta, onRootChanged);
        editChain.PropagateImmutableEdit("X", 99);

        Assert.NotNull(receivedRoot);
        var result = (ImmutablePoint)receivedRoot!;
        Assert.Equal(99, result.X);
        Assert.Equal(20, result.Y);
    }

    [Fact]
    public void Multi_Level_Immutable_Propagation()
    {
        var registry = new TypeRegistry();

        // ImmutableConfig → ImmutableTheme → ImmutablePoint
        var original = new ImmutableConfig(
            "MyConfig",
            new ImmutableTheme("dark", new ImmutablePoint(5, 10)));

        var configMeta = registry.Resolve(typeof(ImmutableConfig));
        var themeMeta = registry.Resolve(typeof(ImmutableTheme));
        var pointMeta = registry.Resolve(typeof(ImmutablePoint));

        Assert.NotNull(configMeta.Compose);
        Assert.NotNull(themeMeta.Compose);
        Assert.NotNull(pointMeta.Compose);

        // Compose the point with a new X
        var newPoint = (ImmutablePoint)pointMeta.Compose!(
            original.Theme.Origin,
            new Dictionary<string, object> { { "X", 42 } });
        Assert.Equal(42, newPoint.X);

        // Compose the theme with the new point
        var newTheme = (ImmutableTheme)themeMeta.Compose!(
            original.Theme,
            new Dictionary<string, object> { { "Origin", newPoint } });
        Assert.Equal("dark", newTheme.Name);
        Assert.Equal(42, newTheme.Origin.X);

        // Compose the config with the new theme
        var newConfig = (ImmutableConfig)configMeta.Compose!(
            original,
            new Dictionary<string, object> { { "Theme", newTheme } });
        Assert.Equal("MyConfig", newConfig.Label);
        Assert.Equal(42, newConfig.Theme.Origin.X);
        Assert.Equal(10, newConfig.Theme.Origin.Y);
    }

    [Fact]
    public void EditChain_Propagates_Through_Multiple_Immutable_Levels()
    {
        var registry = new TypeRegistry();

        var original = new ImmutableConfig(
            "MyConfig",
            new ImmutableTheme("dark", new ImmutablePoint(5, 10)));

        object? receivedRoot = null;
        var onRootChanged = (object newRoot) => { receivedRoot = newRoot; };

        var configMeta = registry.Resolve(typeof(ImmutableConfig));
        var themeMeta = registry.Resolve(typeof(ImmutableTheme));
        var pointMeta = registry.Resolve(typeof(ImmutablePoint));

        // Build the edit chain: Config → Theme → Point
        var configDescriptors = configMeta.Decompose!(original);
        var themeDesc = configDescriptors.First(d => d.Name == "Theme");

        var themeDescriptors = themeMeta.Decompose!(original.Theme);
        var originDesc = themeDescriptors.First(d => d.Name == "Origin");

        // Create the full chain
        var chain = new EditChain(original, configMeta, onRootChanged);
        var themeChain = chain.Push(themeDesc, themeMeta, original.Theme);
        var pointChain = themeChain.Push(originDesc, pointMeta, original.Theme.Origin);

        // Edit X at the deepest level
        pointChain.PropagateImmutableEdit("X", 42);

        Assert.NotNull(receivedRoot);
        var result = (ImmutableConfig)receivedRoot!;
        Assert.Equal("MyConfig", result.Label);
        Assert.Equal("dark", result.Theme.Name);
        Assert.Equal(42, result.Theme.Origin.X);
        Assert.Equal(10, result.Theme.Origin.Y);
    }

    [Fact]
    public void Type_With_Both_Editor_And_Decompose()
    {
        var registry = new TypeRegistry();

        // Register a type that has both an editor and decompose
        registry.Register<ImmutablePoint>(new TypeMetadata
        {
            Editor = (val, onChange) =>
            {
                var p = (ImmutablePoint)val;
                return Duct.UI.Text($"({p.X}, {p.Y})");
            },
            Decompose = val =>
            {
                var p = (ImmutablePoint)val;
                return new List<PropertyDescriptor>
                {
                    new() { Name = "X", PropertyType = typeof(int),
                            GetValue = () => p.X, Order = 0 },
                    new() { Name = "Y", PropertyType = typeof(int),
                            GetValue = () => p.Y, Order = 1 },
                };
            },
            Compose = (val, updates) =>
            {
                var p = (ImmutablePoint)val;
                var x = updates.TryGetValue("X", out var ux) ? (int)ux : p.X;
                var y = updates.TryGetValue("Y", out var uy) ? (int)uy : p.Y;
                return new ImmutablePoint(x, y);
            }
        });

        var meta = registry.Resolve(typeof(ImmutablePoint));
        Assert.NotNull(meta.Editor);
        Assert.NotNull(meta.Decompose);
        Assert.NotNull(meta.Compose);
    }
}
