using System.Windows.Input;

namespace Duct.Core;

/// <summary>
/// Bridges ICommand (MVVM/CommunityToolkit) to DuctCommand, enabling migration from
/// ViewModel-based patterns to Duct's declarative commanding.
///   var ductCmd = CommandInterop.FromCommand(viewModel.SaveCommand, "Save",
///       icon: new SymbolIconData("Save"), accelerator: Accelerator(VirtualKey.S, Control));
/// </summary>
public static class CommandInterop
{
    /// <summary>
    /// Creates a DuctCommand from an ICommand. CanExecute is evaluated at creation time.
    /// For CanExecute to update on each render, call this within a component's Render method.
    /// </summary>
    public static DuctCommand FromCommand(
        ICommand command,
        string label,
        IconData? icon = null,
        string? description = null,
        KeyboardAcceleratorData? accelerator = null,
        object? parameter = null)
    {
        return new DuctCommand
        {
            Label = label,
            Execute = () => command.Execute(parameter),
            CanExecute = command.CanExecute(parameter),
            Icon = icon,
            Description = description,
            Accelerator = accelerator,
        };
    }

    /// <summary>
    /// Creates a parameterized DuctCommand from an ICommand. The ICommand receives the
    /// DuctCommand's parameter when Execute and CanExecute are called.
    /// </summary>
    public static DuctCommand<T> FromCommand<T>(
        ICommand command,
        string label,
        IconData? icon = null,
        string? description = null,
        KeyboardAcceleratorData? accelerator = null)
    {
        return new DuctCommand<T>
        {
            Label = label,
            Execute = arg => command.Execute(arg),
            CanExecute = true, // evaluated per-call since parameter varies
            Icon = icon,
            Description = description,
            Accelerator = accelerator,
        };
    }
}
