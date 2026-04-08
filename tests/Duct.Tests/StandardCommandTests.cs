using Duct.Core;
using Windows.System;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Tests for StandardCommand factory — verifies correct label, icon, accelerator,
/// and sync/async overload behavior for all 16 standard commands.
/// </summary>
public class StandardCommandTests
{
    // ════════════════════════════════════════════════════════════════
    //  Sync overload sets Execute, not ExecuteAsync
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Sync_Overload_Sets_Execute()
    {
        bool invoked = false;
        var cmd = StandardCommand.Cut(() => invoked = true);

        Assert.NotNull(cmd.Execute);
        Assert.Null(cmd.ExecuteAsync);
        cmd.Execute!();
        Assert.True(invoked);
    }

    // ════════════════════════════════════════════════════════════════
    //  Async overload sets ExecuteAsync, not Execute
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Async_Overload_Sets_ExecuteAsync()
    {
        var cmd = StandardCommand.Cut(() => Task.CompletedTask);

        Assert.Null(cmd.Execute);
        Assert.NotNull(cmd.ExecuteAsync);
    }

    // ════════════════════════════════════════════════════════════════
    //  canExecute parameter flows through
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void CanExecute_Flows_Through()
    {
        var cmd = StandardCommand.Save(() => { }, canExecute: false);
        Assert.False(cmd.CanExecute);
        Assert.False(cmd.IsEnabled);
    }

    [Fact]
    public void Default_CanExecute_Is_True()
    {
        var cmd = StandardCommand.Save(() => { });
        Assert.True(cmd.CanExecute);
    }

    // ════════════════════════════════════════════════════════════════
    //  Each command has correct metadata
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [MemberData(nameof(AllSyncCommands))]
    public void Standard_Command_Has_Correct_Metadata(
        DuctCommand cmd, string expectedLabel, string? expectedIcon, VirtualKey? expectedKey, VirtualKeyModifiers? expectedModifiers)
    {
        Assert.Equal(expectedLabel, cmd.Label);

        if (expectedIcon is not null)
        {
            Assert.IsType<SymbolIconData>(cmd.Icon);
            Assert.Equal(expectedIcon, ((SymbolIconData)cmd.Icon!).Symbol);
        }
        else
        {
            Assert.Null(cmd.Icon);
        }

        if (expectedKey is not null)
        {
            Assert.NotNull(cmd.Accelerator);
            Assert.Equal(expectedKey.Value, cmd.Accelerator!.Key);
            Assert.Equal(expectedModifiers ?? VirtualKeyModifiers.None, cmd.Accelerator.Modifiers);
        }
        else
        {
            Assert.Null(cmd.Accelerator);
        }
    }

    public static TheoryData<DuctCommand, string, string?, VirtualKey?, VirtualKeyModifiers?> AllSyncCommands()
    {
        Action noop = () => { };
        var data = new TheoryData<DuctCommand, string, string?, VirtualKey?, VirtualKeyModifiers?>();

        data.Add(StandardCommand.Cut(noop),       "Cut",        "Cut",      VirtualKey.X, VirtualKeyModifiers.Control);
        data.Add(StandardCommand.Copy(noop),      "Copy",       "Copy",     VirtualKey.C, VirtualKeyModifiers.Control);
        data.Add(StandardCommand.Paste(noop),     "Paste",      "Paste",    VirtualKey.V, VirtualKeyModifiers.Control);
        data.Add(StandardCommand.Undo(noop),      "Undo",       "Undo",     VirtualKey.Z, VirtualKeyModifiers.Control);
        data.Add(StandardCommand.Redo(noop),      "Redo",       "Redo",     VirtualKey.Y, VirtualKeyModifiers.Control);
        data.Add(StandardCommand.Delete(noop),    "Delete",     "Delete",   VirtualKey.Delete, VirtualKeyModifiers.None);
        data.Add(StandardCommand.SelectAll(noop), "Select all", null,       VirtualKey.A, VirtualKeyModifiers.Control);
        data.Add(StandardCommand.Save(noop),      "Save",       "Save",     VirtualKey.S, VirtualKeyModifiers.Control);
        data.Add(StandardCommand.Open(noop),      "Open",       "OpenFile", VirtualKey.O, VirtualKeyModifiers.Control);
        data.Add(StandardCommand.Close(noop),     "Close",      null,       VirtualKey.W, VirtualKeyModifiers.Control);
        data.Add(StandardCommand.Share(noop),     "Share",      "Share",    null,          null);
        data.Add(StandardCommand.Play(noop),      "Play",       "Play",     null,          null);
        data.Add(StandardCommand.Pause(noop),     "Pause",      "Pause",    null,          null);
        data.Add(StandardCommand.Stop(noop),      "Stop",       "Stop",     null,          null);
        data.Add(StandardCommand.Forward(noop),   "Forward",    "Forward",  null,          null);
        data.Add(StandardCommand.Backward(noop),  "Backward",   "Back",     null,          null);

        return data;
    }
}
