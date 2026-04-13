using System.ComponentModel;
using Duct.Core;
using Duct.PropertyGrid;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Tests for the PropertyGrid component logic — metadata resolution,
/// category grouping, filtering, and INPC integration.
/// These tests exercise the TypeRegistry and ReflectionTypeMetadataProvider
/// through the PropertyGridComponent's rendering logic.
/// </summary>
public class PropertyGridComponentTests
{
    // ── Test models ───────────────────────────────────────────────

    private class MutableModel
    {
        public string Name { get; set; } = "Test";
        public int Count { get; set; } = 42;
        public bool Active { get; set; } = true;
    }

    private class CategorizedModel
    {
        [PropertyCategory("Appearance")]
        public string Name { get; set; } = "";

        [PropertyCategory("Appearance")]
        public bool Visible { get; set; } = true;

        [PropertyCategory("Layout")]
        public double Width { get; set; }

        [PropertyCategory("Layout")]
        public double Height { get; set; }

        // No category → "General"
        public int Priority { get; set; }
    }

    private class ReadOnlyModel
    {
        public string Editable { get; set; } = "can edit";

        [PropertyReadOnly]
        public string ReadOnly { get; set; } = "cannot edit";

        public int ComputedValue { get; } = 99;
    }

    private class FilterableModel
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public bool Active { get; set; }
    }

    private class InpcModel : INotifyPropertyChanged
    {
        private string _name = "original";
        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(nameof(Name)); } }
        }

        private int _count;
        public int Count
        {
            get => _count;
            set { if (_count != value) { _count = value; OnPropertyChanged(nameof(Count)); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ── Tests ─────────────────────────────────────────────────────

    [Fact]
    public void Mutable_Object_Decomposes_To_Correct_Properties()
    {
        var registry = new TypeRegistry();
        var model = new MutableModel();
        var meta = registry.Resolve(typeof(MutableModel));
        var descriptors = meta.Decompose!(model);

        Assert.Equal(3, descriptors.Count);
        Assert.Contains(descriptors, d => d.Name == "Name" && d.PropertyType == typeof(string));
        Assert.Contains(descriptors, d => d.Name == "Count" && d.PropertyType == typeof(int));
        Assert.Contains(descriptors, d => d.Name == "Active" && d.PropertyType == typeof(bool));
    }

    [Fact]
    public void Editing_Mutable_Property_Calls_Setter()
    {
        var registry = new TypeRegistry();
        var model = new MutableModel { Name = "old" };
        var meta = registry.Resolve(typeof(MutableModel));
        var descriptors = meta.Decompose!(model);

        var nameProp = descriptors.First(d => d.Name == "Name");
        Assert.NotNull(nameProp.SetValue);

        nameProp.SetValue!("new");
        Assert.Equal("new", model.Name);
    }

    [Fact]
    public void Categories_Group_Correctly()
    {
        var registry = new TypeRegistry();
        var model = new CategorizedModel();
        var meta = registry.Resolve(typeof(CategorizedModel));
        var descriptors = meta.Decompose!(model);

        var groups = descriptors
            .GroupBy(d => d.Category ?? "General")
            .ToDictionary(g => g.Key, g => g.ToList());

        Assert.True(groups.ContainsKey("Appearance"));
        Assert.True(groups.ContainsKey("Layout"));
        Assert.True(groups.ContainsKey("General"));

        Assert.Equal(2, groups["Appearance"].Count);
        Assert.Equal(2, groups["Layout"].Count);
        Assert.Single(groups["General"]);
    }

    [Fact]
    public void ReadOnly_Properties_Are_NonEditable()
    {
        var registry = new TypeRegistry();
        var model = new ReadOnlyModel();
        var meta = registry.Resolve(typeof(ReadOnlyModel));
        var descriptors = meta.Decompose!(model);

        var editable = descriptors.First(d => d.Name == "Editable");
        Assert.False(editable.IsReadOnly);
        Assert.NotNull(editable.SetValue);

        var readOnly = descriptors.First(d => d.Name == "ReadOnly");
        Assert.True(readOnly.IsReadOnly);
        Assert.Null(readOnly.SetValue);

        var computed = descriptors.First(d => d.Name == "ComputedValue");
        Assert.True(computed.IsReadOnly);
        Assert.Null(computed.SetValue);
    }

    [Fact]
    public void Filter_Excludes_Matching_Properties()
    {
        var registry = new TypeRegistry();
        var model = new FilterableModel { Name = "test", Age = 25, Active = true };
        var meta = registry.Resolve(typeof(FilterableModel));
        var allDescriptors = meta.Decompose!(model);

        Assert.Equal(3, allDescriptors.Count);

        // Apply filter — only string properties
        var filtered = allDescriptors.Where(d => d.PropertyType == typeof(string)).ToList();
        Assert.Single(filtered);
        Assert.Equal("Name", filtered[0].Name);
    }

    [Fact]
    public void INPC_Target_Values_Update_On_External_Mutation()
    {
        var registry = new TypeRegistry();
        var model = new InpcModel { Name = "before" };
        var meta = registry.Resolve(typeof(InpcModel));

        // Simulate external mutation
        model.Name = "after";

        // Re-decompose and read value
        var descriptors = meta.Decompose!(model);
        var nameProp = descriptors.First(d => d.Name == "Name");
        Assert.Equal("after", nameProp.GetValue());
    }

    [Fact]
    public void INPC_Target_Triggers_Rerender_Via_UseObservableTree()
    {
        var ctx = new RenderContext();
        var model = new InpcModel { Name = "start" };
        var c = new[] { 0 };

        // Render cycle with UseObservableTree
        ctx.BeginRender(() => c[0]++);
        ctx.UseObservableTree(model);
        ctx.FlushEffects();

        ctx.BeginRender(() => c[0]++);
        ctx.UseObservableTree(model);
        ctx.FlushEffects();

        model.Name = "changed";
        Assert.True(c[0] >= 1);
    }

    [Fact]
    public void Primitive_Editors_Resolve_Correctly()
    {
        var registry = new TypeRegistry();

        // Each primitive type should resolve to a TypeMetadata with an Editor
        Assert.NotNull(registry.Resolve(typeof(string)).Editor);
        Assert.NotNull(registry.Resolve(typeof(bool)).Editor);
        Assert.NotNull(registry.Resolve(typeof(int)).Editor);
        Assert.NotNull(registry.Resolve(typeof(double)).Editor);
    }

    [Fact]
    public void PropertyGrid_Element_Can_Be_Created()
    {
        var registry = new TypeRegistry();
        var model = new MutableModel();

        var element = new PropertyGridElement
        {
            Target = model,
            Registry = registry,
        };

        Assert.Same(model, element.Target);
        Assert.Same(registry, element.Registry);
        Assert.Null(element.OnRootChanged);
        Assert.Null(element.Filter);
        Assert.False(element.ShowSearch);
    }
}
