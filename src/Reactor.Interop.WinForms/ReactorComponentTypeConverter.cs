using System.ComponentModel;
using System.Globalization;
using ReactorComponent = Microsoft.UI.Reactor.Core.Component;

namespace Microsoft.UI.Reactor.Interop.WinForms;

/// <summary>
/// TypeConverter for the <see cref="XamlIslandControl.ComponentType"/> property.
/// Enables the WinForms designer Properties grid to:
///   - Display the component type name as a readable string
///   - Show a dropdown of all concrete Reactor Component subclasses in the project
///   - Accept typed type names and resolve them to Type objects
/// </summary>
internal class ReactorComponentTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name == "(none)")
                return null;

            // Try exact match first (full name), then short name
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.IsDynamic) continue;
                try
                {
                    var match = asm.GetType(name);
                    if (match is not null && IsValidComponentType(match))
                        return match;
                }
                catch { }
            }

            // Short name search — check all types
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.IsDynamic) continue;
                try
                {
                    foreach (var t in asm.GetTypes())
                    {
                        if (t.Name == name && IsValidComponentType(t))
                            return t;
                    }
                }
                catch { }
            }
        }
        return base.ConvertFrom(context, culture, value);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string))
            return value is Type t ? t.FullName : "(none)";
        return base.ConvertTo(context, culture, value, destinationType);
    }

    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;

    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => false;

    public override StandardValuesCollection? GetStandardValues(ITypeDescriptorContext? context)
    {
        var types = new List<Type>();
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm.IsDynamic) continue;
            try
            {
                foreach (var t in asm.GetTypes())
                {
                    if (IsValidComponentType(t))
                        types.Add(t);
                }
            }
            catch { }
        }
        types.Sort((a, b) => string.Compare(a.FullName, b.FullName, StringComparison.Ordinal));
        return new StandardValuesCollection(types);
    }

    private static bool IsValidComponentType(Type t)
        => t.IsClass
        && !t.IsAbstract
        && typeof(ReactorComponent).IsAssignableFrom(t)
        && t.GetConstructor(Type.EmptyTypes) is not null;
}
