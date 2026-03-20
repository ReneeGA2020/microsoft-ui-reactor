using System.Runtime.InteropServices;

namespace DuctFiles.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct FsEntry
{
    public nint NamePtr;     // *const u16
    public uint NameLen;
    public ulong Size;
    public ulong ModifiedTicks;
    public byte IsDirectory;
    public byte HasChildren;
}

[StructLayout(LayoutKind.Sequential)]
internal struct FsResult
{
    public nint Entries;     // *mut FsEntry
    public uint Count;
}

internal static partial class NativeFs
{
    [LibraryImport("ductfs")]
    internal static unsafe partial FsResult ductfs_enumerate(char* pathPtr, uint pathLen);

    [LibraryImport("ductfs")]
    internal static unsafe partial FsResult ductfs_enumerate_subdirs(char* pathPtr, uint pathLen);

    [LibraryImport("ductfs")]
    internal static partial void ductfs_free_result(FsResult result);
}
