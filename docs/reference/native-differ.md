# Native Differ (ViewDiffer) Reference

The native differ is an **experimental** Rust-based tree diffing engine that can replace Duct's pure C# reconciliation path for better performance on large trees.

## Architecture

```
C# Element tree
    ↓ TreeSerializer (C#)
Flat arrays: ViewNode[], ViewProp[]
    ↓ P/Invoke (zero-copy)
Rust: diff_trees() → Vec<DifferPatch>
    ↓ P/Invoke (pointer to Rust heap)
C# reads patches as ReadOnlySpan<ViewPatch>
    ↓ Reconciler applies patches
WinUI controls updated
```

## Why Rust?

The tree diff is a CPU-bound algorithm operating on flat data structures — a good fit for a native language. The Rust differ:

- Operates on flat arrays (cache-friendly, no pointer chasing)
- Uses a reusable buffer (`DiffContext`) that amortizes allocation across diffs
- Implements keyed list reconciliation with LIS (Longest Increasing Subsequence) for minimal moves
- Returns patches as a pointer into Rust heap memory — C# reads them without copying

## File layout

```
Duct/Native/
  ViewDiffer.cs              C# P/Invoke wrapper, span-based API
  differ/
    Cargo.toml               Crate config (cdylib + rlib targets)
    src/
      lib.rs                 Module declarations
      types.rs               Wire types: DifferNode, DifferProp, DifferPatch, PatchOp
      diff.rs                Tree diff algorithm (diff_subtree, reconcile_keyed_children)
      reconcile.rs           Keyed list reconciliation with LIS
      ffi.rs                 extern "C" entry points
      arena.rs               Reusable DiffContext buffer
      tests.rs               Unit tests
```

## Wire types

Data crosses the FFI boundary as flat, `repr(C)` structs. Both Rust and C# define matching layouts.

### ViewNode / DifferNode (32 bytes)

```
┌──────────┬──────────┬──────────────┬────────────┬─────────────┬───────────┬───────────┐
│ type_id  │ (pad 4B) │     key      │parent_index│ child_count │first_child│ prop_count│
│   u32    │          │     i64      │    i32     │    u32      │   u32     │   u32     │
└──────────┴──────────┴──────────────┴────────────┴─────────────┴───────────┴───────────┘
```

- `type_id` — element type (TextElement = 1, ButtonElement = 2, etc.)
- `key` — element key for keyed reconciliation (-1 if unkeyed)
- `parent_index` — index of parent node in the flat array (-1 for root)
- `child_count`, `first_child` — children are a contiguous range in the array
- `prop_count`, `first_prop` — props are a contiguous range in the props array

### ViewProp / DifferProp (16 bytes)

```
┌──────────┬──────────┬──────────────────┐
│  dp_id   │ (pad 4B) │   value_hash     │
│   u32    │          │      u64         │
└──────────┴──────────┴──────────────────┘
```

- `dp_id` — DependencyProperty identifier
- `value_hash` — FNV-1a hash of the property value (the differ compares hashes, not values)

### ViewPatch / DifferPatch (32 bytes)

```
┌──────────┬──────────────┬──────────────┬──────────┬──────────────────┐
│    op    │ source_index │ target_index │  dp_id   │ new_value_hash   │
│   i32    │     i32      │     i32      │   u32    │     u64          │
└──────────┴──────────────┴──────────────┴──────────┴──────────────────┘
```

Patch operations:
| Op | Meaning |
|----|---------|
| Insert | Mount a new node at target_index |
| Remove | Unmount the node at source_index |
| Move | Move node from source_index to target_index |
| Replace | Replace node at source_index with new type at target_index |
| UpdateProp | Update property dp_id on source_index to new_value_hash |

## Tree serialization

`TreeSerializer.SerializeWithMapping()` does a BFS traversal of the Element tree and flattens it into `ViewNode[]` and `ViewProp[]` arrays. The mapping allows the reconciler to look up the original Element for a given array index.

**Gap nodes** (TabView, NavigationView, TreeView, etc.) are excluded from serialization because their children aren't in `Panel.Children`. They're reconciled in a separate imperative pass.

**Components** (`ComponentElement`, `FuncElement`) are opaque — the serializer doesn't traverse into them. They're reconciled imperatively after the flat diff.

## The diff algorithm

`diff_subtree` in `diff.rs` recursively walks old and new trees:

1. Compare root nodes — if types differ, emit `Replace`
2. Compare properties — emit `UpdateProp` for any hash changes
3. Compare children:
   - If all children are unkeyed: positional matching
   - If children have keys: keyed reconciliation via LIS

### Keyed reconciliation

`reconcile_keyed_children` handles reordering with minimal moves:

1. Match common prefix (elements at the start that didn't change)
2. Match common suffix (elements at the end that didn't change)
3. For the middle section:
   - Build a key → index map for old children
   - Match new children to old by key
   - Compute LIS of matched indices to find the longest subsequence already in order
   - Elements in the LIS stay in place; others are moved
   - Unmatched old elements are removed; unmatched new elements are inserted

This is the same algorithm used by Vue.js and Svelte.

## C# integration (ViewDiffer.cs)

The `ViewDiffer` class wraps the Rust FFI:

```csharp
using var differ = new ViewDiffer();

// Serialize trees
var (oldNodes, oldProps) = TreeSerializer.Serialize(oldTree);
var (newNodes, newProps) = TreeSerializer.Serialize(newTree);

// Diff (returns span pointing into Rust heap — valid until next call)
ReadOnlySpan<ViewPatch> patches = differ.DiffTrees(
    oldNodes, oldProps, newNodes, newProps);

// Apply patches to WinUI controls
foreach (var patch in patches) { ... }
```

**Important:** The returned `ReadOnlySpan<ViewPatch>` points directly into the Rust DiffContext's internal buffer. It is valid until the next call to `DiffTrees` or `Dispose`. Do not store or cache the span.

## Build integration

`Duct.csproj` has MSBuild targets that invoke Cargo:

1. `BuildRustDiffer` — runs `cargo build` with the correct target triple
2. `CopyRustDll` — copies `viewdiffer.dll` to the output directory

If Rust is not installed, these targets are skipped and the framework falls back to the pure C# reconciliation path.

## Experiment status: Rust vs. C# diff

The native differ is an experiment to answer: **is a native diffing engine worth the FFI complexity?**

### What works well
- Flat array representation is cache-friendly and fast to diff
- Keyed reconciliation with LIS produces minimal DOM operations
- Zero-copy patch reading avoids marshaling overhead
- Amortized allocation via reusable DiffContext

### Current limitations
- **Components are opaque** — the flat serializer can't represent component boundaries, so components fall back to imperative C# reconciliation after the flat diff
- **Full serialization every render** — the entire tree is re-serialized even if only a small subtree changed (incremental serialization is a planned optimization)
- **FFI safety gaps** — see the [code review](../viewdiffer-code-review.md) for details on panic safety, struct layout verification, and other issues being addressed
- **Three copies of FNV-1a** — the hash function is duplicated in Rust (2x) and C# (1x)

### When to use which path
- **Small to medium trees (< 200 elements):** The C# path is fine. FFI overhead may exceed the diff speedup.
- **Large trees or frequent updates:** The native differ shows gains on trees with many children or frequent keyed reordering.
- **Development:** Use the C# path (simpler debugging, no Rust dependency).

### Future directions
- Incremental tree serialization (dirty tracking)
- Native property application (Rust emits typed patches, bypassing C# dispatch)
- Shared memory ring buffer for pipelining serialization and diffing
- See [WinUI Integration Proposals](../winui3-integration-proposals.md) for the full roadmap
