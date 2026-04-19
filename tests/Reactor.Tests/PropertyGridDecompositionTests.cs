using Microsoft.UI.Reactor.Data;
using Microsoft.UI.Reactor.Controls;
using Xunit;
using static Microsoft.UI.Reactor.Factories;

namespace Microsoft.UI.Reactor.Tests;

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
        var detailsValue = (NestedModel)detailsDesc.GetValue(parent)!;
        var nestedMeta = registry.Resolve(typeof(NestedModel));
        var nestedDescriptors = nestedMeta.Decompose!(detailsValue);

        var descProp = nestedDescriptors.First(d => d.Name == "Description");
        Assert.NotNull(descProp.SetValue);
        var result = descProp.SetValue!(detailsValue, "new");
        Assert.Same(detailsValue, result); // mutable — same reference
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
        var currentPoint = (ImmutablePoint)positionDesc.GetValue(parent)!;
        var newPoint = (ImmutablePoint)pointMeta.Compose!(currentPoint,
            new Dictionary<string, object> { { "X", 99 } });

        Assert.Equal(99, newPoint.X);
        Assert.Equal(20, newPoint.Y);

        // Write back to mutable parent via SetValue (return-new-owner)
        var result = positionDesc.SetValue!(parent, newPoint);
        Assert.Same(parent, result); // mutable parent — same reference
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
                return TextBlock($"({p.X}, {p.Y})");
            },
            Decompose = val =>
            {
                var p = (ImmutablePoint)val;
                return new List<FieldDescriptor>
                {
                    new() { Name = "X", FieldType = typeof(int),
                            GetValue = _ => p.X, Order = 0 },
                    new() { Name = "Y", FieldType = typeof(int),
                            GetValue = _ => p.Y, Order = 1 },
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

    // ── New tests for return-new-owner pattern ───────────────────

    [Fact]
    public void SetValue_Mutable_Path_Stops_At_Mutated_Object()
    {
        var registry = new TypeRegistry();
        var parent = new MutableParent { Name = "test", Position = new ImmutablePoint(10, 20) };
        var meta = registry.Resolve(typeof(MutableParent));
        var descriptors = meta.Decompose!(parent);

        var nameProp = descriptors.First(d => d.Name == "Name");
        Assert.NotNull(nameProp.SetValue);

        var result = nameProp.SetValue!(parent, "updated");
        Assert.Same(parent, result); // mutable — early termination, same reference
        Assert.Equal("updated", parent.Name);
    }

    [Fact]
    public void SetValue_Immutable_Path_Returns_New_Object()
    {
        var registry = new TypeRegistry();
        var point = new ImmutablePoint(10, 20);
        var meta = registry.Resolve(typeof(ImmutablePoint));
        var descriptors = meta.Decompose!(point);

        var xProp = descriptors.First(d => d.Name == "X");
        Assert.NotNull(xProp.SetValue);

        var newPoint = (ImmutablePoint)xProp.SetValue!(point, 99);
        Assert.NotSame(point, newPoint); // immutable — new object
        Assert.Equal(99, newPoint.X);
        Assert.Equal(20, newPoint.Y);
    }

    [Fact]
    public void SetValue_Mixed_Path_Immutable_Nested_In_Mutable()
    {
        var registry = new TypeRegistry();
        var parent = new MutableParent { Name = "test", Position = new ImmutablePoint(10, 20) };
        var meta = registry.Resolve(typeof(MutableParent));
        var descriptors = meta.Decompose!(parent);

        var positionDesc = descriptors.First(d => d.Name == "Position");
        Assert.NotNull(positionDesc.SetValue);

        // Setting a new immutable value on a mutable parent
        var newPoint = new ImmutablePoint(99, 88);
        var result = positionDesc.SetValue!(parent, newPoint);
        Assert.Same(parent, result); // mutable parent absorbed change
        Assert.Equal(new ImmutablePoint(99, 88), parent.Position);
    }

    [Fact]
    public void ThreePlus_Level_Nesting_With_Mixed_Mutability()
    {
        var registry = new TypeRegistry();

        // 3-level: ImmutableConfig → ImmutableTheme → ImmutablePoint (all immutable)
        var original = new ImmutableConfig(
            "MyConfig",
            new ImmutableTheme("dark", new ImmutablePoint(5, 10)));

        object? receivedRoot = null;
        var onRootChanged = (object newRoot) => { receivedRoot = newRoot; };

        var configMeta = registry.Resolve(typeof(ImmutableConfig));
        var themeMeta = registry.Resolve(typeof(ImmutableTheme));
        var pointMeta = registry.Resolve(typeof(ImmutablePoint));

        // Verify all descriptors have SetValue (via compose-based init-only setters)
        var configDescriptors = configMeta.Decompose!(original);
        var themeDesc = configDescriptors.First(d => d.Name == "Theme");
        Assert.NotNull(themeDesc.SetValue);

        var themeDescriptors = themeMeta.Decompose!(original.Theme);
        var originDesc = themeDescriptors.First(d => d.Name == "Origin");
        Assert.NotNull(originDesc.SetValue);

        var pointDescriptors = pointMeta.Decompose!(original.Theme.Origin);
        var xDesc = pointDescriptors.First(d => d.Name == "X");
        Assert.NotNull(xDesc.SetValue);

        // Edit at the deepest level
        var newPoint = (ImmutablePoint)xDesc.SetValue!(original.Theme.Origin, 42);
        Assert.Equal(42, newPoint.X);
        Assert.Equal(10, newPoint.Y);
        Assert.NotSame(original.Theme.Origin, newPoint);
    }
}
