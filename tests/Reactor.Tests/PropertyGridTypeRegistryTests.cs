using System.ComponentModel;
using Microsoft.UI.Reactor.Data;
using Microsoft.UI.Reactor.Controls;
using Xunit;
using static Microsoft.UI.Reactor.Factories;

namespace Microsoft.UI.Reactor.Tests;

/// <summary>
/// Tests for the PropertyGrid TypeRegistry, ReflectionTypeMetadataProvider,
/// attributes, and Compose generation.
/// </summary>
public class PropertyGridTypeRegistryTests
{
    // ── Test models ───────────────────────────────────────────────

    private enum TestColor { Red, Green, Blue }

    private class SimpleModel
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
        public bool Active { get; set; }
        public double Score { get; set; }
    }

    private class AttributeModel
    {
        [PropertyCategory("Appearance")]
        [PropertyDescription("The display name")]
        [PropertyDisplayName("Display Name")]
        public string Name { get; set; } = "";

        [PropertyHidden]
        public int InternalId { get; set; }

        [PropertyReadOnly]
        public string Status { get; set; } = "OK";

        [PropertyOrder(0)]
        public int Priority { get; set; }

        [PropertyOrder(1)]
        public int Secondary { get; set; }
    }

    private class SystemComponentModelAttributes
    {
        [Category("Layout")]
        [Description("The width in pixels")]
        [DisplayName("Width (px)")]
        public double Width { get; set; }

        [Browsable(false)]
        public int HiddenProp { get; set; }

        [ReadOnly(true)]
        public string ReadOnlyProp { get; set; } = "fixed";
    }

    private class MixedAttributeModel
    {
        // Reactor attribute should override global::System.ComponentModel
        [PropertyCategory("ReactorCategory")]
        [Category("SCCategory")]
        public string Name { get; set; } = "";

        [PropertyDescription("Reactor desc")]
        [Description("SC desc")]
        public int Value { get; set; }
    }

    // Immutable record for Compose testing
    private record ImmutablePoint(int X, int Y);

    private record ImmutableConfig(string Name, int Value, bool Enabled);

    // Mixed mutability
    private class MixedModel
    {
        public string MutableProp { get; set; } = "";
        public int ReadOnlyProp { get; }

        public MixedModel() { }
        public MixedModel(int readOnlyProp) { ReadOnlyProp = readOnlyProp; }
    }

    // Grid attribute test model
    private class GridAttributeModel
    {
        [ColumnWidth(200, MinWidth = 100, MaxWidth = 400)]
        public string Name { get; set; } = "";

        [ColumnPin(PinPosition.Left)]
        public int Id { get; set; }

        [NotSortable]
        public string Description { get; set; } = "";

        [NotFilterable]
        public double Score { get; set; }
    }

    // ── TypeRegistry tests ────────────────────────────────────────

    [Fact]
    public void Explicit_Registration_Is_Returned_By_Resolve()
    {
        var registry = new TypeRegistry();
        var custom = new TypeMetadata { DisplayName = "Custom" };
        registry.Register<SimpleModel>(custom);

        var result = registry.Resolve(typeof(SimpleModel));
        Assert.Equal("Custom", result.DisplayName);
    }

    [Fact]
    public void Primitive_Types_Resolve_To_Correct_Editors()
    {
        var registry = new TypeRegistry();

        var stringMeta = registry.Resolve(typeof(string));
        Assert.NotNull(stringMeta.Editor);

        var boolMeta = registry.Resolve(typeof(bool));
        Assert.NotNull(boolMeta.Editor);

        var intMeta = registry.Resolve(typeof(int));
        Assert.NotNull(intMeta.Editor);

        var doubleMeta = registry.Resolve(typeof(double));
        Assert.NotNull(doubleMeta.Editor);

        var floatMeta = registry.Resolve(typeof(float));
        Assert.NotNull(floatMeta.Editor);

        var longMeta = registry.Resolve(typeof(long));
        Assert.NotNull(longMeta.Editor);

        var shortMeta = registry.Resolve(typeof(short));
        Assert.NotNull(shortMeta.Editor);

        var byteMeta = registry.Resolve(typeof(byte));
        Assert.NotNull(byteMeta.Editor);

        var decimalMeta = registry.Resolve(typeof(decimal));
        Assert.NotNull(decimalMeta.Editor);
    }

    [Fact]
    public void Enum_Types_Resolve_To_ComboBox_Editor()
    {
        var registry = new TypeRegistry();
        var meta = registry.Resolve(typeof(TestColor));

        Assert.NotNull(meta.Editor);
        // Editor should exist — it creates a ComboBox with enum values
    }

    [Fact]
    public void Unregistered_Class_Falls_Back_To_Reflection_Decomposition()
    {
        var registry = new TypeRegistry();
        var meta = registry.Resolve(typeof(SimpleModel));

        Assert.NotNull(meta.Decompose);
        Assert.Null(meta.Editor); // class → decomposition, not atomic editor

        var model = new SimpleModel { Name = "Test", Count = 42, Active = true, Score = 3.14 };
        var descriptors = meta.Decompose!(model);

        Assert.Equal(4, descriptors.Count);
        Assert.Contains(descriptors, d => d.Name == "Name");
        Assert.Contains(descriptors, d => d.Name == "Count");
        Assert.Contains(descriptors, d => d.Name == "Active");
        Assert.Contains(descriptors, d => d.Name == "Score");
    }

    [Fact]
    public void Decomposition_GetValue_Works()
    {
        var registry = new TypeRegistry();
        var meta = registry.Resolve(typeof(SimpleModel));
        var model = new SimpleModel { Name = "Hello", Count = 7 };

        var descriptors = meta.Decompose!(model);
        var nameProp = descriptors.First(d => d.Name == "Name");
        var countProp = descriptors.First(d => d.Name == "Count");

        Assert.Equal("Hello", nameProp.GetValue(model));
        Assert.Equal(7, countProp.GetValue(model));
    }

    [Fact]
    public void Decomposition_SetValue_Works_For_Mutable_Properties()
    {
        var registry = new TypeRegistry();
        var meta = registry.Resolve(typeof(SimpleModel));
        var model = new SimpleModel { Name = "old" };

        var descriptors = meta.Decompose!(model);
        var nameProp = descriptors.First(d => d.Name == "Name");

        Assert.NotNull(nameProp.SetValue);
        var result = nameProp.SetValue!(model, "new");
        Assert.Same(model, result); // mutable — same reference
        Assert.Equal("new", model.Name);
    }

    // ── Attribute tests ───────────────────────────────────────────

    [Fact]
    public void PropertyHidden_Excludes_Property()
    {
        var meta = ReflectionTypeMetadataProvider.CreateMetadata(typeof(AttributeModel));
        var model = new AttributeModel();
        var descriptors = meta.Decompose!(model);

        Assert.DoesNotContain(descriptors, d => d.Name == "InternalId");
    }

    [Fact]
    public void Browsable_False_Excludes_Property()
    {
        var meta = ReflectionTypeMetadataProvider.CreateMetadata(typeof(SystemComponentModelAttributes));
        var model = new SystemComponentModelAttributes();
        var descriptors = meta.Decompose!(model);

        Assert.DoesNotContain(descriptors, d => d.Name == "HiddenProp");
    }

    [Fact]
    public void Reactor_Attributes_Read_Correctly()
    {
        var meta = ReflectionTypeMetadataProvider.CreateMetadata(typeof(AttributeModel));
        var model = new AttributeModel();
        var descriptors = meta.Decompose!(model);

        var nameProp = descriptors.First(d => d.Name == "Name");
        Assert.Equal("Appearance", nameProp.Category);
        Assert.Equal("The display name", nameProp.Description);
        Assert.Equal("Display Name", nameProp.DisplayName);
    }

    [Fact]
    public void PropertyReadOnly_Makes_Property_ReadOnly()
    {
        var meta = ReflectionTypeMetadataProvider.CreateMetadata(typeof(AttributeModel));
        var model = new AttributeModel();
        var descriptors = meta.Decompose!(model);

        var statusProp = descriptors.First(d => d.Name == "Status");
        Assert.True(statusProp.IsReadOnly);
        Assert.Null(statusProp.SetValue);
    }

    [Fact]
    public void PropertyOrder_Controls_Sorting()
    {
        var meta = ReflectionTypeMetadataProvider.CreateMetadata(typeof(AttributeModel));
        var model = new AttributeModel();
        var descriptors = meta.Decompose!(model);

        var priorityIdx = descriptors.ToList().FindIndex(d => d.Name == "Priority");
        var secondaryIdx = descriptors.ToList().FindIndex(d => d.Name == "Secondary");
        Assert.True(priorityIdx < secondaryIdx, "Priority (order 0) should come before Secondary (order 1)");
    }

    [Fact]
    public void SystemComponentModel_Attributes_Work_As_Fallback()
    {
        var meta = ReflectionTypeMetadataProvider.CreateMetadata(typeof(SystemComponentModelAttributes));
        var model = new SystemComponentModelAttributes();
        var descriptors = meta.Decompose!(model);

        var widthProp = descriptors.First(d => d.Name == "Width");
        Assert.Equal("Layout", widthProp.Category);
        Assert.Equal("The width in pixels", widthProp.Description);
        Assert.Equal("Width (px)", widthProp.DisplayName);
    }

    [Fact]
    public void SystemComponentModel_ReadOnly_Works()
    {
        var meta = ReflectionTypeMetadataProvider.CreateMetadata(typeof(SystemComponentModelAttributes));
        var model = new SystemComponentModelAttributes();
        var descriptors = meta.Decompose!(model);

        var roProps = descriptors.First(d => d.Name == "ReadOnlyProp");
        Assert.True(roProps.IsReadOnly);
        Assert.Null(roProps.SetValue);
    }

    [Fact]
    public void Reactor_Attributes_Override_SystemComponentModel()
    {
        var meta = ReflectionTypeMetadataProvider.CreateMetadata(typeof(MixedAttributeModel));
        var model = new MixedAttributeModel();
        var descriptors = meta.Decompose!(model);

        var nameProp = descriptors.First(d => d.Name == "Name");
        Assert.Equal("ReactorCategory", nameProp.Category);

        var valueProp = descriptors.First(d => d.Name == "Value");
        Assert.Equal("Reactor desc", valueProp.Description);
    }

    // ── Compose tests ─────────────────────────────────────────────

    [Fact]
    public void Compose_For_Immutable_Record()
    {
        var meta = ReflectionTypeMetadataProvider.CreateMetadata(typeof(ImmutablePoint));

        Assert.NotNull(meta.Compose);

        var original = new ImmutablePoint(10, 20);
        var updates = new Dictionary<string, object> { { "X", 99 } };
        var result = (ImmutablePoint)meta.Compose!(original, updates);

        Assert.Equal(99, result.X);
        Assert.Equal(20, result.Y); // unchanged
    }

    [Fact]
    public void Compose_For_Multi_Property_Record()
    {
        var meta = ReflectionTypeMetadataProvider.CreateMetadata(typeof(ImmutableConfig));
        Assert.NotNull(meta.Compose);

        var original = new ImmutableConfig("test", 42, true);
        var updates = new Dictionary<string, object> { { "Name", "updated" }, { "Enabled", false } };
        var result = (ImmutableConfig)meta.Compose!(original, updates);

        Assert.Equal("updated", result.Name);
        Assert.Equal(42, result.Value);
        Assert.False(result.Enabled);
    }

    [Fact]
    public void Mixed_Mutability_Mutable_Props_Have_SetValue()
    {
        var meta = ReflectionTypeMetadataProvider.CreateMetadata(typeof(MixedModel));
        var model = new MixedModel(42) { MutableProp = "hello" };
        var descriptors = meta.Decompose!(model);

        var mutable = descriptors.First(d => d.Name == "MutableProp");
        Assert.NotNull(mutable.SetValue);
        Assert.False(mutable.IsReadOnly);

        var readOnly = descriptors.First(d => d.Name == "ReadOnlyProp");
        Assert.Null(readOnly.SetValue);
        Assert.True(readOnly.IsReadOnly);
    }

    // ── Array resolution tests ────────────────────────────────────

    [Fact]
    public void Array_Type_Resolves_To_ArrayTypeMetadata()
    {
        var registry = new TypeRegistry();
        var meta = registry.Resolve(typeof(int[]));
        Assert.IsType<ArrayTypeMetadata>(meta);
    }

    [Fact]
    public void List_Type_Resolves_To_ArrayTypeMetadata()
    {
        var registry = new TypeRegistry();
        var meta = registry.Resolve(typeof(List<string>));
        Assert.IsType<ArrayTypeMetadata>(meta);
    }

    [Fact]
    public void Array_CreateElement_Null_For_No_Parameterless_Ctor()
    {
        var registry = new TypeRegistry();
        // ImmutablePoint has no parameterless ctor
        var meta = (ArrayTypeMetadata)registry.Resolve(typeof(ImmutablePoint[]));
        Assert.Null(meta.CreateElement);
    }

    [Fact]
    public void Array_CreateElement_Works_For_Parameterless_Ctor()
    {
        var registry = new TypeRegistry();
        var meta = (ArrayTypeMetadata)registry.Resolve(typeof(SimpleModel[]));
        Assert.NotNull(meta.CreateElement);
    }

    // ── TypeRegistry extension tests ──────────────────────────────

    [Fact]
    public void RegisterCellRenderer_Stores_And_Retrieves()
    {
        var registry = new TypeRegistry();
        Func<object, Microsoft.UI.Reactor.Core.Element> renderer = val => TextBlock(val?.ToString() ?? "");
        registry.RegisterCellRenderer<string>(renderer);

        var result = registry.GetCellRenderer(typeof(string));
        Assert.Same(renderer, result);
    }

    [Fact]
    public void RegisterFormatter_Stores_And_Retrieves()
    {
        var registry = new TypeRegistry();
        Func<object?, string> formatter = val => val?.ToString() ?? "(empty)";
        registry.RegisterFormatter<int>(formatter);

        var result = registry.GetFormatter(typeof(int));
        Assert.Same(formatter, result);
    }

    [Fact]
    public void Tiered_Resolution_CompactEditor_Falls_Back_To_Editor()
    {
        var registry = new TypeRegistry();
        var editorFunc = new Func<object, Action<object>, Microsoft.UI.Reactor.Core.Element>(
            (val, onChange) => TextBlock("standard"));
        registry.Register<SimpleModel>(new TypeMetadata { Editor = editorFunc });

        var compactEditor = registry.ResolveEditor(typeof(SimpleModel), EditorTier.Compact);
        Assert.Same(editorFunc, compactEditor);
    }

    [Fact]
    public void Tiered_Resolution_CompactEditor_Preferred_Over_Editor()
    {
        var registry = new TypeRegistry();
        var editorFunc = new Func<object, Action<object>, Microsoft.UI.Reactor.Core.Element>(
            (val, onChange) => TextBlock("standard"));
        var compactFunc = new Func<object, Action<object>, Microsoft.UI.Reactor.Core.Element>(
            (val, onChange) => TextBlock("compact"));
        registry.Register<SimpleModel>(new TypeMetadata { Editor = editorFunc, CompactEditor = compactFunc });

        var resolved = registry.ResolveEditor(typeof(SimpleModel), EditorTier.Compact);
        Assert.Same(compactFunc, resolved);
    }

    [Fact]
    public void Tiered_Resolution_FullEditor_Null_When_Not_Registered()
    {
        var registry = new TypeRegistry();
        registry.Register<SimpleModel>(new TypeMetadata
        {
            Editor = (val, onChange) => TextBlock("standard")
        });

        var fullEditor = registry.ResolveEditor(typeof(SimpleModel), EditorTier.Full);
        Assert.Null(fullEditor);
    }

    [Fact]
    public void Tiered_Resolution_FullEditor_Returned_When_Registered()
    {
        var registry = new TypeRegistry();
        var fullFunc = new Func<object, Action<object>, Microsoft.UI.Reactor.Core.Element>(
            (val, onChange) => TextBlock("full"));
        registry.Register<SimpleModel>(new TypeMetadata
        {
            Editor = (val, onChange) => TextBlock("standard"),
            FullEditor = fullFunc,
        });

        var resolved = registry.ResolveEditor(typeof(SimpleModel), EditorTier.Full);
        Assert.Same(fullFunc, resolved);
    }

    // ── Grid attribute tests ──────────────────────────────────────

    [Fact]
    public void Grid_Attributes_Populate_FieldDescriptor()
    {
        var meta = ReflectionTypeMetadataProvider.CreateMetadata(typeof(GridAttributeModel));
        var model = new GridAttributeModel();
        var descriptors = meta.Decompose!(model);

        var nameProp = descriptors.First(d => d.Name == "Name");
        Assert.Equal(200, nameProp.Width);
        Assert.Equal(100, nameProp.MinWidth);
        Assert.Equal(400, nameProp.MaxWidth);
        Assert.True(nameProp.Sortable);
        Assert.True(nameProp.Filterable);

        var idProp = descriptors.First(d => d.Name == "Id");
        Assert.Equal(PinPosition.Left, idProp.Pin);

        var descProp = descriptors.First(d => d.Name == "Description");
        Assert.False(descProp.Sortable);
        Assert.True(descProp.Filterable);

        var scoreProp = descriptors.First(d => d.Name == "Score");
        Assert.True(scoreProp.Sortable);
        Assert.False(scoreProp.Filterable);
    }

    // ── SetValue tests for mutable/immutable/readonly ─────────────

    [Fact]
    public void SetValue_Generated_For_Mutable_Class_Property()
    {
        var meta = ReflectionTypeMetadataProvider.CreateMetadata(typeof(SimpleModel));
        var model = new SimpleModel { Name = "old" };
        var descriptors = meta.Decompose!(model);

        var nameProp = descriptors.First(d => d.Name == "Name");
        Assert.NotNull(nameProp.SetValue);

        var result = nameProp.SetValue!(model, "new");
        Assert.Same(model, result); // mutable — same ref
        Assert.Equal("new", model.Name);
    }

    [Fact]
    public void SetValue_Generated_For_Immutable_Record_Property()
    {
        var meta = ReflectionTypeMetadataProvider.CreateMetadata(typeof(ImmutablePoint));
        var point = new ImmutablePoint(10, 20);
        var descriptors = meta.Decompose!(point);

        var xProp = descriptors.First(d => d.Name == "X");
        Assert.NotNull(xProp.SetValue);

        var newPoint = (ImmutablePoint)xProp.SetValue!(point, 99);
        Assert.NotSame(point, newPoint); // immutable — new object
        Assert.Equal(99, newPoint.X);
        Assert.Equal(20, newPoint.Y);
    }

    [Fact]
    public void SetValue_Null_For_Truly_ReadOnly_Property()
    {
        var meta = ReflectionTypeMetadataProvider.CreateMetadata(typeof(MixedModel));
        var model = new MixedModel(42);
        var descriptors = meta.Decompose!(model);

        var readOnly = descriptors.First(d => d.Name == "ReadOnlyProp");
        Assert.Null(readOnly.SetValue);
        Assert.True(readOnly.IsReadOnly);
    }

    [Fact]
    public void Mixed_Mutable_Immutable_Properties_On_Same_Type()
    {
        var meta = ReflectionTypeMetadataProvider.CreateMetadata(typeof(MutableParentForMixed));
        var model = new MutableParentForMixed { Name = "hello" };
        var descriptors = meta.Decompose!(model);

        var nameProp = descriptors.First(d => d.Name == "Name");
        Assert.NotNull(nameProp.SetValue);
        var result1 = nameProp.SetValue!(model, "world");
        Assert.Same(model, result1); // mutable

        var positionProp = descriptors.First(d => d.Name == "Position");
        Assert.NotNull(positionProp.SetValue);
        var result2 = positionProp.SetValue!(model, new ImmutablePoint(1, 2));
        Assert.Same(model, result2); // parent is mutable, setter is mutable
    }

    private class MutableParentForMixed
    {
        public string Name { get; set; } = "";
        public ImmutablePoint Position { get; set; } = new(0, 0);
    }
}
