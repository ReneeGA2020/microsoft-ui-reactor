using System.Windows.Input;
using Duct.Core;
using Windows.System;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Tests for CommandInterop — bridging ICommand to DuctCommand.
/// </summary>
public class CommandInteropTests
{
    private class TestCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecuteResult { get; set; } = true;
        public object? LastParameter { get; private set; }
        public int ExecuteCount { get; private set; }

        public bool CanExecute(object? parameter)
        {
            LastParameter = parameter;
            return CanExecuteResult;
        }

        public void Execute(object? parameter)
        {
            LastParameter = parameter;
            ExecuteCount++;
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    [Fact]
    public void FromCommand_Maps_Execute()
    {
        var iCmd = new TestCommand();
        var cmd = CommandInterop.FromCommand(iCmd, "Save");

        cmd.Execute!();

        Assert.Equal(1, iCmd.ExecuteCount);
    }

    [Fact]
    public void FromCommand_Evaluates_CanExecute()
    {
        var iCmd = new TestCommand { CanExecuteResult = false };
        var cmd = CommandInterop.FromCommand(iCmd, "Save");

        Assert.False(cmd.CanExecute);
        Assert.False(cmd.IsEnabled);
    }

    [Fact]
    public void FromCommand_Passes_Parameter()
    {
        var iCmd = new TestCommand();
        var cmd = CommandInterop.FromCommand(iCmd, "Open", parameter: "file.txt");

        cmd.Execute!();

        Assert.Equal("file.txt", iCmd.LastParameter);
    }

    [Fact]
    public void FromCommand_Metadata_Flows_Through()
    {
        var iCmd = new TestCommand();
        var icon = new SymbolIconData("Save");
        var accel = new KeyboardAcceleratorData(VirtualKey.S, VirtualKeyModifiers.Control);

        var cmd = CommandInterop.FromCommand(iCmd, "Save",
            icon: icon, description: "Save file", accelerator: accel);

        Assert.Equal("Save", cmd.Label);
        Assert.Same(icon, cmd.Icon);
        Assert.Equal("Save file", cmd.Description);
        Assert.Same(accel, cmd.Accelerator);
    }

    [Fact]
    public void FromCommandT_Maps_Execute_With_Parameter()
    {
        var iCmd = new TestCommand();
        var cmd = CommandInterop.FromCommand<string>(iCmd, "Delete");

        cmd.Execute!("item-1");

        Assert.Equal(1, iCmd.ExecuteCount);
        Assert.Equal("item-1", iCmd.LastParameter);
    }
}
