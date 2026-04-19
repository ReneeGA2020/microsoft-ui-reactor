using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Data;
using Microsoft.UI.Reactor.Data.Providers;
using Microsoft.UI.Reactor.Controls;
using Xunit;
using static Microsoft.UI.Reactor.Factories;

namespace Microsoft.UI.Reactor.Tests;

/// <summary>
/// Tests for DataGridElement construction and context records.
/// </summary>
public class DataGridElementTests
{
    private record TestItem(int Id, string Name);

    private static ListDataSource<TestItem> CreateSource() =>
        new(new[] { new TestItem(1, "Test") }, t => (RowKey)t.Id);

    // ── Element construction ─────────────────────────────────────

    [Fact]
    public void Element_Construction_With_Required_Source()
    {
        var el = new DataGridElement<TestItem>
        {
            Source = CreateSource(),
        };

        Assert.NotNull(el.Source);
        Assert.Null(el.Columns);
        Assert.Null(el.Registry);
        Assert.Equal(SelectionMode.None, el.SelectionMode);
        Assert.Equal(40.0, el.RowHeight);
        Assert.Equal(40.0, el.EstimatedRowHeight);
        Assert.Equal(EditMode.Cell, el.EditMode);
        Assert.True(el.ShowHeaders);
        Assert.False(el.Editable);
        Assert.True(el.AllowColumnReorder);
        Assert.True(el.AllowColumnResize);
    }

    [Fact]
    public void Element_With_All_Properties()
    {
        var source = CreateSource();
        var columns = new FieldDescriptor[]
        {
            new() { Name = "Id", FieldType = typeof(int), GetValue = _ => 0 },
        };
        Func<RowKey, TestItem, Task> onRowChanged = (_, _) => Task.CompletedTask;
        Action<IReadOnlySet<RowKey>> onSelection = _ => { };

        var el = new DataGridElement<TestItem>
        {
            Source = source,
            Columns = columns,
            SelectionMode = SelectionMode.Multiple,
            OnSelectionChanged = onSelection,
            OnRowChanged = onRowChanged,
            RowHeight = 32,
            EstimatedRowHeight = 36,
            EditMode = EditMode.Row,
            ShowHeaders = false,
            Editable = true,
            AllowColumnReorder = false,
            AllowColumnResize = false,
        };

        Assert.Equal(source, el.Source);
        Assert.Equal(columns, el.Columns);
        Assert.Equal(SelectionMode.Multiple, el.SelectionMode);
        Assert.Equal(onSelection, el.OnSelectionChanged);
        Assert.Equal(onRowChanged, el.OnRowChanged);
        Assert.Equal(32.0, el.RowHeight);
        Assert.Equal(36.0, el.EstimatedRowHeight);
        Assert.Equal(EditMode.Row, el.EditMode);
        Assert.False(el.ShowHeaders);
        Assert.True(el.Editable);
        Assert.False(el.AllowColumnReorder);
        Assert.False(el.AllowColumnResize);
    }

    // ── Context records ──────────────────────────────────────────

    [Fact]
    public void CellContext_Construction()
    {
        var item = new TestItem(1, "Test");
        var key = (RowKey)1;
        var col = new FieldDescriptor
        {
            Name = "Name",
            FieldType = typeof(string),
            GetValue = o => ((TestItem)o).Name,
        };
        object? setValue = null;

        var ctx = new CellContext<TestItem>(item, key, col, "Test", false, v => setValue = v);

        Assert.Equal(item, ctx.Row);
        Assert.Equal(key, ctx.Key);
        Assert.Equal(col, ctx.Column);
        Assert.Equal("Test", ctx.Value);
        Assert.False(ctx.IsEditing);

        ctx.SetValue("New");
        Assert.Equal("New", setValue);
    }

    [Fact]
    public void RowContext_Construction()
    {
        var item = new TestItem(1, "Test");
        var key = (RowKey)1;
        var cells = new Element[] { TextBlock("cell1") };

        var ctx = new RowContext<TestItem>(item, key, 0, true, false, cells);

        Assert.Equal(item, ctx.Row);
        Assert.Equal(key, ctx.Key);
        Assert.Equal(0, ctx.RowIndex);
        Assert.True(ctx.IsSelected);
        Assert.False(ctx.IsEditing);
        Assert.Single(ctx.Cells);
    }

    [Fact]
    public void HeaderContext_Construction()
    {
        var col = new FieldDescriptor
        {
            Name = "Name",
            FieldType = typeof(string),
            GetValue = _ => null,
        };
        bool toggled = false;
        double resized = 0;

        var ctx = new HeaderContext(col, SortDirection.Ascending, () => toggled = true, w => resized = w);

        Assert.Equal(col, ctx.Column);
        Assert.Equal(SortDirection.Ascending, ctx.CurrentSort);

        ctx.ToggleSort();
        Assert.True(toggled);

        ctx.Resize(200);
        Assert.Equal(200, resized);
    }

    // ── Enum values ──────────────────────────────────────────────

    [Fact]
    public void SelectionMode_Values()
    {
        Assert.Equal(0, (int)SelectionMode.None);
        Assert.Equal(1, (int)SelectionMode.Single);
        Assert.Equal(2, (int)SelectionMode.Multiple);
    }

    [Fact]
    public void EditMode_Values()
    {
        Assert.Equal(0, (int)EditMode.Cell);
        Assert.Equal(1, (int)EditMode.Row);
    }
}
