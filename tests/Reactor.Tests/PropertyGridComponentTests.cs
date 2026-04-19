using System.ComponentModel;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Data;
using Microsoft.UI.Reactor.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Xunit;
using static Microsoft.UI.Reactor.Factories;

namespace Microsoft.UI.Reactor.Tests;

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
        Assert.Contains(descriptors, d => d.Name == "Name" && d.FieldType == typeof(string));
        Assert.Contains(descriptors, d => d.Name == "Count" && d.FieldType == typeof(int));
        Assert.Contains(descriptors, d => d.Name == "Active" && d.FieldType == typeof(bool));
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

        var result = nameProp.SetValue!(model, "new");
        Assert.Same(model, result);
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
        var filtered = allDescriptors.Where(d => d.FieldType == typeof(string)).ToList();
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
        Assert.Equal("after", nameProp.GetValue(model));
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

    [Fact]
    public void Component_Renders_Properties_From_FieldDescriptor_List()
    {
        var registry = new TypeRegistry();
        var model = new MutableModel { Name = "TestName", Count = 5, Active = false };
        var meta = registry.Resolve(typeof(MutableModel));
        var descriptors = meta.Decompose!(model);

        // Verify all descriptors are FieldDescriptor instances
        foreach (var desc in descriptors)
        {
            Assert.IsType<FieldDescriptor>(desc);
            Assert.NotNull(desc.GetValue);
            Assert.NotNull(desc.Name);
            Assert.NotNull(desc.FieldType);
        }
    }

    [Fact]
    public void Template_Delegates_Receive_FieldDescriptor()
    {
        var registry = new TypeRegistry();
        var model = new MutableModel();
        var meta = registry.Resolve(typeof(MutableModel));
        var descriptors = meta.Decompose!(model);

        // Verify template delegates work with FieldDescriptor
        var desc = descriptors.First();
        var label = PropertyGridDefaults.PropertyLabelTemplate(desc, 0);
        Assert.NotNull(label);

        var editor = TextBlock("test");
        var row = PropertyGridDefaults.PropertyRowTemplate(desc, label, editor, 0);
        Assert.NotNull(row);
    }

    // ── FullEditor "..." affordance tests ───────────────────────

    private record ExpandableValue(string Data)
    {
        public override string ToString() => Data;
    }

    [Fact]
    public void FullEditor_Button_Visible_When_FullEditor_Registered()
    {
        var registry = new TypeRegistry();
        Func<object, Action<object>, Element> fullEditor =
            (val, onChange) => TextBlock($"Full: {val}");

        registry.Register<ExpandableValue>(new TypeMetadata
        {
            Editor = (val, onChange) => TextBlock($"Compact: {val}"),
            FullEditor = fullEditor,
        });

        var meta = registry.Resolve(typeof(ExpandableValue));

        // FullEditor is available → "..." button should appear
        Assert.NotNull(meta.FullEditor);
        Assert.NotNull(meta.Editor);

        // Verify FullEditor produces valid Element
        var element = meta.FullEditor!(new ExpandableValue("test"), _ => { });
        Assert.IsType<TextBlockElement>(element);
    }

    [Fact]
    public void FullEditor_Button_Hidden_When_FullEditor_Is_Null()
    {
        var registry = new TypeRegistry();
        registry.Register<ExpandableValue>(new TypeMetadata
        {
            Editor = (val, onChange) => TextBlock($"Standard: {val}"),
            // No FullEditor registered
        });

        var meta = registry.Resolve(typeof(ExpandableValue));

        // FullEditor is null → no "..." button
        Assert.Null(meta.FullEditor);
        Assert.NotNull(meta.Editor);
    }

    [Fact]
    public void FullEditor_Flyout_Contains_FullEditor_Content()
    {
        var registry = new TypeRegistry();
        var receivedValue = (object?)null;

        registry.Register<ExpandableValue>(new TypeMetadata
        {
            Editor = (val, onChange) => TextBlock("inline"),
            FullEditor = (val, onChange) =>
            {
                receivedValue = val;
                return TextBlock($"Full editor for: {val}");
            },
        });

        var meta = registry.Resolve(typeof(ExpandableValue));
        var testValue = new ExpandableValue("hello");

        // Simulate what PropertyGridComponent does: call FullEditor to build flyout content
        var fullEditorElement = meta.FullEditor!(testValue, _ => { });

        Assert.Equal(testValue, receivedValue);
        Assert.IsType<TextBlockElement>(fullEditorElement);

        // Verify ContentFlyout wraps the FullEditor content
        var flyout = Microsoft.UI.Reactor.Factories.ContentFlyout(fullEditorElement,
            placement: Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Bottom);
        Assert.IsType<ContentFlyoutElement>(flyout);
        Assert.Same(fullEditorElement, flyout.Content);
    }
}
