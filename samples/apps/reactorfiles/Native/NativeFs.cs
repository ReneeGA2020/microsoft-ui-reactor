using System.Runtime.InteropServices;

namespace ReactorFiles.Native;

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
    [LibraryImport("reactorfs")]
    internal static unsafe partial FsResult reactorfs_enumerate(char* pathPtr, uint pathLen);

    [LibraryImport("reactorfs")]
    internal static unsafe partial FsResult reactorfs_enumerate_subdirs(char* pathPtr, uint pathLen);

    [LibraryImport("reactorfs")]
    internal static partial void reactorfs_free_result(FsResult result);
}
