using System.Reflection;
using System.Runtime.Loader;

namespace Duct.Preview;

/// <summary>
/// A collectible AssemblyLoadContext that loads the user's assembly from memory
/// (no file locks) while sharing Duct/WinUI/runtime types from the default context.
/// </summary>
sealed class PreviewLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver? _resolver;

    public PreviewLoadContext(string mainAssemblyPath)
        : base(isCollectible: true)
    {
        try
        {
            _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
        }
        catch
        {
            // If .deps.json is missing, we'll just rely on default context fallthrough
            _resolver = null;
        }
    }

    protected override Assembly? Load(AssemblyName name)
    {
        // If the assembly is already loaded in the default context, share it.
        // This is critical for type identity — the user's Component subclass must
        // reference the same Duct.Core.Component type as the preview host.
        foreach (var asm in Default.Assemblies)
        {
            if (string.Equals(asm.GetName().Name, name.Name, StringComparison.OrdinalIgnoreCase))
                return null; // fall through to default context
        }

        // Resolve user-specific dependencies from their output directory
        string? path = _resolver?.ResolveAssemblyToPath(name);
        if (path != null && File.Exists(path))
        {
            // Load from memory to avoid file locks
            var bytes = File.ReadAllBytes(path);
            return LoadFromStream(new MemoryStream(bytes));
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string? path = _resolver?.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path != null ? LoadUnmanagedDllFromPath(path) : IntPtr.Zero;
    }
}
