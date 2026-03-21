using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Duct.Core;

/// <summary>
/// C# interop wrapper for the native Rust diffing engine.
/// Provides zero-copy tree diffing and keyed list reconciliation via P/Invoke.
///
/// Usage:
///   using var differ = new ViewDiffer();
///   var patches = differ.DiffTrees(oldNodes, oldProps, newNodes, newProps);
///   foreach (var patch in patches) { /* apply patch */ }
///
/// The context is reused across diffs — after warm-up, allocation is amortized on the hot path.
///
/// NOT thread-safe. Must be called from the same thread (typically the UI dispatcher thread).
/// The underlying Rust context has no internal synchronization.
/// </summary>
public sealed class ViewDiffer : IDisposable
{
    private IntPtr _ctx;
    private bool _disposed;

    public ViewDiffer()
    {
        _ctx = NativeMethods.differ_create_context();
        if (_ctx == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create native diff context");

        // Verify struct layouts match between C# and Rust at runtime
        Debug.Assert(Marshal.SizeOf<ViewNode>() == NativeMethods.differ_node_size(),
            $"ViewNode size mismatch: C#={Marshal.SizeOf<ViewNode>()} Rust={NativeMethods.differ_node_size()}");
        Debug.Assert(Marshal.SizeOf<ViewProp>() == NativeMethods.differ_prop_size(),
            $"ViewProp size mismatch: C#={Marshal.SizeOf<ViewProp>()} Rust={NativeMethods.differ_prop_size()}");
        Debug.Assert(Marshal.SizeOf<ViewPatch>() == NativeMethods.differ_patch_size(),
            $"ViewPatch size mismatch: C#={Marshal.SizeOf<ViewPatch>()} Rust={NativeMethods.differ_patch_size()}");
    }

    /// <summary>
    /// Diff two flat view trees. Returns a span of patches pointing into Rust's heap.
    ///
    /// WARNING: The returned span is only valid until the next call to DiffTrees, ReconcileKeys,
    /// or Dispose. The span points directly into the Rust context's internal buffer, which is
    /// cleared on the next operation. Copy the data if you need to keep it longer.
    /// </summary>
    public ReadOnlySpan<ViewPatch> DiffTrees(
        ReadOnlySpan<ViewNode> oldNodes, ReadOnlySpan<ViewProp> oldProps,
        ReadOnlySpan<ViewNode> newNodes, ReadOnlySpan<ViewProp> newProps)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        unsafe
        {
            fixed (ViewNode* oldNodesPtr = oldNodes)
            fixed (ViewProp* oldPropsPtr = oldProps)
            fixed (ViewNode* newNodesPtr = newNodes)
            fixed (ViewProp* newPropsPtr = newProps)
            {
                int result = NativeMethods.differ_trees_ffi(
                    _ctx,
                    (IntPtr)oldNodesPtr, (uint)oldNodes.Length,
                    (IntPtr)oldPropsPtr, (uint)oldProps.Length,
                    (IntPtr)newNodesPtr, (uint)newNodes.Length,
                    (IntPtr)newPropsPtr, (uint)newProps.Length,
                    out IntPtr patchesPtr, out uint patchCount);

                if (result != 0)
                    throw new InvalidOperationException($"Native differ failed with code {result}");

                if (patchCount == 0 || patchesPtr == IntPtr.Zero)
                    return ReadOnlySpan<ViewPatch>.Empty;

                return new ReadOnlySpan<ViewPatch>((void*)patchesPtr, (int)patchCount);
            }
        }
    }

    /// <summary>
    /// Reconcile two keyed lists. Returns minimal insert/remove/move operations.
    ///
    /// WARNING: Same lifetime constraint as DiffTrees — span is valid until the next call.
    /// </summary>
    public ReadOnlySpan<ViewPatch> ReconcileKeys(ReadOnlySpan<long> oldKeys, ReadOnlySpan<long> newKeys)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        unsafe
        {
            fixed (long* oldKeysPtr = oldKeys)
            fixed (long* newKeysPtr = newKeys)
            {
                int result = NativeMethods.differ_reconcile_keys_ffi(
                    _ctx,
                    (IntPtr)oldKeysPtr, (uint)oldKeys.Length,
                    (IntPtr)newKeysPtr, (uint)newKeys.Length,
                    out IntPtr patchesPtr, out uint patchCount);

                if (result != 0)
                    throw new InvalidOperationException($"Native reconcile failed with code {result}");

                if (patchCount == 0 || patchesPtr == IntPtr.Zero)
                    return ReadOnlySpan<ViewPatch>.Empty;

                return new ReadOnlySpan<ViewPatch>((void*)patchesPtr, (int)patchCount);
            }
        }
    }

    /// <summary>
    /// FNV-1a hash matching the Rust side. Use to create type IDs and property IDs.
    ///
    /// NOTE: This is a C# copy of the FNV-1a algorithm also implemented in Rust
    /// (ffi::fnv1a_hash). Both implementations must be kept in sync. The Rust version
    /// is authoritative — if you change the algorithm, update both sides and run
    /// cross-validation tests.
    /// </summary>
    public static uint HashString(string s)
    {
        uint hash = 0x811c_9dc5u;
        foreach (byte b in Encoding.UTF8.GetBytes(s))
        {
            hash ^= b;
            hash *= 0x0100_0193u;
        }
        return hash;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    ~ViewDiffer()
    {
        Dispose(disposing: false);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed && _ctx != IntPtr.Zero)
        {
            NativeMethods.differ_destroy_context(_ctx);
            _ctx = IntPtr.Zero;
            _disposed = true;
        }
    }

    private static class NativeMethods
    {
        private const string DllName = "viewdiffer";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr differ_create_context();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void differ_destroy_context(IntPtr ctx);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int differ_trees_ffi(
            IntPtr ctx,
            IntPtr oldNodes, uint oldCount,
            IntPtr oldProps, uint oldPropCount,
            IntPtr newNodes, uint newCount,
            IntPtr newProps, uint newPropCount,
            out IntPtr outPatches, out uint outPatchCount);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int differ_reconcile_keys_ffi(
            IntPtr ctx,
            IntPtr oldKeys, uint oldCount,
            IntPtr newKeys, uint newCount,
            out IntPtr outPatches, out uint outPatchCount);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe uint differ_hash_string(IntPtr s, uint len);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint differ_node_size();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint differ_prop_size();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint differ_patch_size();
    }
}

// ════════════════════════════════════════════════════════════════════════
//  Wire types (must match Rust #[repr(C)] structs byte-for-byte)
// ════════════════════════════════════════════════════════════════════════

/// A node in the flat view tree representation.
[StructLayout(LayoutKind.Sequential)]
public struct ViewNode
{
    public uint TypeId;
    public long Key;
    public int ParentIndex;
    public ushort PropCount;
    public ushort ChildCount;
    public uint FirstChild;
    public uint FirstProp;
}

/// A property on a node (id + value hash).
[StructLayout(LayoutKind.Sequential)]
public struct ViewProp
{
    public uint DpId;
    public ulong ValueHash;
}

/// Patch operation type.
public enum ViewPatchOp : int
{
    None = 0,
    Insert = 1,
    Remove = 2,
    Move = 3,
    UpdateProp = 4,
    Replace = 5,
}

/// A single patch operation from the differ.
[StructLayout(LayoutKind.Sequential)]
public struct ViewPatch
{
    public ViewPatchOp Op;
    public uint NodeIndex;
    public uint TargetIndex;
    public uint DpId;
    public ulong NewValueHash;
}
