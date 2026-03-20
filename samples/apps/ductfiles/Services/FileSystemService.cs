using System.Runtime.InteropServices;
using DuctFiles.Models;
using DuctFiles.Native;

namespace DuctFiles.Services;

/// <summary>
/// Enumerates directories using the Rust FFI crate when available,
/// falling back to managed C# enumeration if the DLL is missing.
/// </summary>
internal static class FileSystemService
{
    private static readonly bool _nativeAvailable = CheckNativeAvailable();

    private static bool CheckNativeAvailable()
    {
        try
        {
            // Try loading the DLL to see if it's present
            NativeLibrary.TryLoad("ductfs", typeof(NativeFs).Assembly, null, out var handle);
            return handle != 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Enumerate all immediate children of the given directory path.
    /// </summary>
    public static Task<FileEntry[]> EnumerateDirectoryAsync(string path) =>
        Task.Run(() => _nativeAvailable ? EnumerateNative(path) : EnumerateManaged(path));

    /// <summary>
    /// Enumerate only subdirectories (for tree lazy-loading).
    /// </summary>
    public static Task<FileEntry[]> EnumerateSubdirsAsync(string path) =>
        Task.Run(() => _nativeAvailable ? EnumerateSubdirsNative(path) : EnumerateSubdirsManaged(path));

    // ── Native (Rust) path ──────────────────────────────────────────

    private static unsafe FileEntry[] EnumerateNative(string path)
    {
        FsResult result;
        fixed (char* pathPtr = path)
        {
            result = NativeFs.ductfs_enumerate(pathPtr, (uint)path.Length);
        }

        try
        {
            return ConvertResult(result, path);
        }
        finally
        {
            NativeFs.ductfs_free_result(result);
        }
    }

    private static unsafe FileEntry[] EnumerateSubdirsNative(string path)
    {
        FsResult result;
        fixed (char* pathPtr = path)
        {
            result = NativeFs.ductfs_enumerate_subdirs(pathPtr, (uint)path.Length);
        }

        try
        {
            return ConvertResult(result, path);
        }
        finally
        {
            NativeFs.ductfs_free_result(result);
        }
    }

    private static unsafe FileEntry[] ConvertResult(FsResult result, string parentPath)
    {
        if (result.Entries == 0 || result.Count == 0)
            return [];

        var entries = new FileEntry[(int)result.Count];
        var ptr = (Native.FsEntry*)result.Entries;

        for (int i = 0; i < (int)result.Count; i++)
        {
            ref var src = ref ptr[i];
            var name = new string((char*)src.NamePtr, 0, (int)src.NameLen);
            var fullPath = Path.Combine(parentPath, name);
            var modified = DateTime.FromFileTimeUtc((long)src.ModifiedTicks);

            entries[i] = new FileEntry(
                Name: name,
                FullPath: fullPath,
                IsDirectory: src.IsDirectory != 0,
                Size: (long)src.Size,
                Modified: modified,
                HasChildren: src.HasChildren != 0
            );
        }

        return entries;
    }

    // ── Managed (C#) fallback ───────────────────────────────────────

    private static FileEntry[] EnumerateManaged(string path)
    {
        var dir = new DirectoryInfo(path);
        if (!dir.Exists) return [];

        var results = new List<FileEntry>();
        try
        {
            foreach (var fsi in dir.EnumerateFileSystemInfos())
            {
                try
                {
                    bool isDir = fsi is DirectoryInfo;
                    long size = isDir ? 0 : ((FileInfo)fsi).Length;
                    bool hasChildren = isDir; // assume children; avoids extra I/O per dir

                    results.Add(new FileEntry(
                        Name: fsi.Name,
                        FullPath: fsi.FullName,
                        IsDirectory: isDir,
                        Size: size,
                        Modified: fsi.LastWriteTimeUtc,
                        HasChildren: hasChildren
                    ));
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }

        return results.ToArray();
    }

    private static FileEntry[] EnumerateSubdirsManaged(string path)
    {
        var dir = new DirectoryInfo(path);
        if (!dir.Exists) return [];

        var results = new List<FileEntry>();
        try
        {
            foreach (var sub in dir.EnumerateDirectories())
            {
                try
                {
                    results.Add(new FileEntry(
                        Name: sub.Name,
                        FullPath: sub.FullName,
                        IsDirectory: true,
                        Size: 0,
                        Modified: sub.LastWriteTimeUtc,
                        HasChildren: true // assume children; avoids extra I/O
                    ));
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }

        return results.ToArray();
    }

}
