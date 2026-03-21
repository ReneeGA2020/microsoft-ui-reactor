# ViewDiffer (Rust Native Differ) — Code Review

**Reviewer:** Senior Platform Engineer (code review agent)
**Date:** 2026-03-18
**Scope:** All files in `Duct/Native/differ/` — Rust source, tests, configuration. Also `Duct/Native/ViewDiffer.cs` (C# interop).

---

## Status Field Instructions

Each feedback item has a **Status** field. The workflow is:

| Status | Meaning | Who sets it |
|--------|---------|-------------|
| `draft` | Initial reviewer feedback, pending manager triage | Reviewer |
| `approved` | Manager agrees this should be completed | Manager |
| `optional` | Manager thinks this is borderline — complete if time allows | Manager |
| `skip` | Manager thinks this feedback is not useful or not worth the cost | Manager |

Engineers: work through `approved` items first, then `optional` if time permits. Mark items complete by checking the box.

---

## Table of Contents

1. [Critical: FFI Safety](#1-critical-ffi-safety)
2. [Critical: Struct Layout Verification](#2-critical-struct-layout-verification)
3. [High: No Logging](#3-high-no-logging)
4. [High: Thread Safety](#4-high-thread-safety)
5. [High: Duplicate Key Handling](#5-high-duplicate-key-handling)
6. [High: Unbounded Recursion in diff_subtree](#6-high-unbounded-recursion-in-diff_subtree)
7. [High: Bounds Checking on Array Accesses](#7-high-bounds-checking-on-array-accesses)
8. [Medium: PatchOp Enum Representation](#8-medium-patchop-enum-representation)
9. [Medium: Dead Code — error Field](#9-medium-dead-code--error-field)
10. [Medium: differ_get_error Returns Non-Null-Terminated String](#10-medium-differ_get_error-returns-non-null-terminated-string)
11. [Medium: Module Naming — arena.rs](#11-medium-module-naming--arenars)
12. [Medium: Hash Function Duplication](#12-medium-hash-function-duplication)
13. [Medium: Allocation in reconcile_keyed_children](#13-medium-allocation-in-reconcile_keyed_children)
14. [Medium: LIS Backtrack Edge Case](#14-medium-lis-backtrack-edge-case)
15. [Low: Cargo.toml — Release Profile](#15-low-cargotoml--release-profile)
16. [Low: Cargo.toml — Lints Configuration](#16-low-cargotoml--lints-configuration)
17. [Low: lib.rs Module Doc Inconsistency](#17-low-librs-module-doc-inconsistency)
18. [Low: Module Visibility](#18-low-module-visibility)
19. [Low: Missing Default Impl](#19-low-missing-default-impl)
20. [Low: Missing PartialEq on DifferPatch](#20-low-missing-partialeq-on-differpatch)
21. [Test Coverage Gaps](#21-test-coverage-gaps)
22. [C# Interop File Review](#22-c-interop-file-review)
23. [.gitignore Review](#23-gitignore-review)

---

## 1. Critical: FFI Safety

- [x] **Status:** `approved`
- **File:** `src/ffi.rs` — all `extern "C"` functions
- **Lines:** 16–137

**Issue:** None of the `extern "C"` functions wrap their bodies in `std::panic::catch_unwind`. If any Rust code panics (e.g., an index-out-of-bounds in `diff_trees`, or any `.unwrap()` path), the panic will hit the `extern "C"` boundary. In modern Rust (2021 edition), this results in a **safe abort** rather than true undefined behavior — but the entire C# host process still terminates immediately with no useful error message and no opportunity for the host to recover or log the failure.

This is the single most important issue in this review. The diff engine does direct array indexing in multiple places (e.g., `old_nodes[old_index as usize]` in `diff.rs:75`), and if the C# side ever passes malformed data (incorrect `child_count`, out-of-range `first_child`), the Rust code will panic and kill the host process.

**Recommendation:**

Wrap every `extern "C"` function body in `catch_unwind` and convert panics to error codes:

```rust
use std::panic::{catch_unwind, AssertUnwindSafe};

#[no_mangle]
pub unsafe extern "C" fn differ_trees_ffi(
    ctx: DifferContextPtr,
    // ... params ...
) -> i32 {
    if ctx.is_null() || out_patches.is_null() || out_patch_count.is_null() {
        return -1;
    }

    let result = catch_unwind(AssertUnwindSafe(|| {
        let ctx = &mut *ctx;
        // ... existing logic ...
        0i32
    }));

    match result {
        Ok(code) => code,
        Err(e) => {
            // Store panic message in ctx.error for retrieval
            let ctx = &mut *ctx;
            ctx.error = format!("panic: {:?}", e.downcast_ref::<&str>());
            -2 // distinct error code for panics
        }
    }
}
```

Also consider adding `panic = "abort"` to the release profile in `Cargo.toml` as a defense-in-depth measure — but `catch_unwind` is still needed for debug builds and for capturing the error message.

**Question for engineer:** Are you aware of the `catch_unwind` pattern? Research the Rustonomicon section on FFI and panic safety. This is foundational knowledge for anyone writing `extern "C"` Rust code.

---

## 2. Critical: Struct Layout Verification

- [x] **Status:** `approved`
- **File:** `src/types.rs` + `Duct/Native/ViewDiffer.cs`
- **Lines:** types.rs:7–22 (DifferNode), ViewDiffer.cs:154–164 (ViewNode)

**Issue:** The Rust structs use `#[repr(C)]` and the C# structs use `StructLayout(LayoutKind.Sequential)`. The layouts *appear* to match, but there are no compile-time or runtime assertions verifying this. If anyone adds, removes, or reorders a field on either side, the result will be silent memory corruption — reading garbage values with no error.

Specific alignment concerns:
- `DifferNode.key` is `i64` at offset 4 from a `u32`. Repr(C) inserts 4 bytes of padding. C# Sequential layout does the same — but this is an implicit assumption.
- `DifferProp.value_hash` is `u64` after a `u32` — same padding concern.
- `DifferPatch.new_value_hash` is `u64` at offset 16 — happens to be naturally aligned, but fragile.

**Recommendation:**

Add compile-time size and alignment assertions in Rust:

```rust
// In types.rs or a dedicated layout_checks.rs
const _: () = {
    assert!(std::mem::size_of::<DifferNode>() == 32);
    assert!(std::mem::align_of::<DifferNode>() == 8);
    assert!(std::mem::size_of::<DifferProp>() == 16);
    assert!(std::mem::align_of::<DifferProp>() == 8);
    assert!(std::mem::size_of::<DifferPatch>() == 32);
    assert!(std::mem::align_of::<DifferPatch>() == 8);
    assert!(std::mem::size_of::<PatchOp>() == 4);
};
```

Also add an FFI function that returns struct sizes so C# can verify at startup:

```rust
#[no_mangle]
pub extern "C" fn differ_node_size() -> u32 {
    std::mem::size_of::<DifferNode>() as u32
}
```

And on the C# side:
```csharp
Debug.Assert(Marshal.SizeOf<ViewNode>() == NativeMethods.differ_node_size());
```

---

## 3. High: No Logging

- [x] **Status:** `approved`
- **File:** All source files
- **Lines:** N/A (nothing exists)

**Issue:** The entire crate has zero logging — no `log`, no `tracing`, no `println!`, no `eprintln!`. For a native library that's called via FFI from a managed host, this makes debugging extremely difficult. When something goes wrong, the only signal is a crash or an opaque error code.

This is especially concerning given the lack of `catch_unwind` (issue #1) — right now, failures are completely silent.

**Recommendation:**

Add the `log` crate as a dependency. The `log` crate is the standard Rust logging facade — it defines macros (`error!`, `warn!`, `info!`, `debug!`, `trace!`) but does not include a logging backend. The consumer (your C# host, via an FFI initialization function) chooses the backend.

For your use case (OSS library that needs to work with both public and Microsoft-internal logging):

1. Use `log` crate in Rust code — this is the abstraction layer
2. Provide an FFI function to register a C callback for log output
3. The C# side implements the callback, routing to whatever logging framework is in use (ILogger, ETW, etc.)

```toml
# Cargo.toml
[dependencies]
log = "0.4"
```

```rust
// src/logging.rs
use std::sync::OnceLock;

type LogCallback = extern "C" fn(level: i32, msg: *const u8, len: u32);
static LOG_CALLBACK: OnceLock<LogCallback> = OnceLock::new();

struct FfiLogger;
impl log::Log for FfiLogger {
    fn enabled(&self, _: &log::Metadata) -> bool { true }
    fn log(&self, record: &log::Record) {
        if let Some(cb) = LOG_CALLBACK.get() {
            let msg = format!("{}", record.args());
            cb(record.level() as i32, msg.as_ptr(), msg.len() as u32);
        }
    }
    fn flush(&self) {}
}

#[no_mangle]
pub extern "C" fn differ_set_log_callback(cb: LogCallback) {
    LOG_CALLBACK.set(cb).ok();
    log::set_logger(&FfiLogger).ok();
    log::set_max_level(log::LevelFilter::Trace);
}
```

This approach:
- Uses the standard `log` facade (the Rust ecosystem standard, not a custom solution)
- Allows the C# host to receive structured log messages via callback
- Can be switched to ETW/Microsoft internal logging by changing only the C# callback implementation
- Zero cost when no callback is registered (log macros compile to nothing when no logger is set)

Then add logging to critical paths:
- `diff_trees` entry/exit with node counts
- Error conditions in FFI functions
- Panic messages captured by `catch_unwind`

**Question for engineer:** Research the `log` vs `tracing` crate trade-off. `tracing` is more powerful (structured, span-based) but heavier. For a leaf library like this, `log` is likely sufficient. If the broader Duct platform standardizes on `tracing`, it's easy to switch later since `tracing` has a compatibility layer with `log`.

---

## 4. High: Thread Safety

- [x] **Status:** `approved`
- **File:** `src/ffi.rs`, `Duct/Native/ViewDiffer.cs`
- **Lines:** ffi.rs:16–24, ViewDiffer.cs:17–19

**Issue:** The `DiffContext` is a mutable struct accessed via raw pointer. If the C# side calls any FFI function from multiple threads using the same context pointer, that's a data race and undefined behavior. The C# `ViewDiffer` class has no synchronization — `_ctx` is accessed without locking.

Currently this is likely single-threaded (UI thread only), but:
1. There's no documentation of this requirement
2. Nothing prevents a future developer from using it off-thread
3. The `ViewDiffer` class doesn't implement any thread-safety warning

**Recommendation:**

Choose one approach:

**Option A (document + assert):** Add a comment to the FFI functions and C# wrapper stating that the context is not thread-safe. Consider a debug-mode thread ID check:

```rust
pub struct DiffContext {
    pub patches: Vec<DifferPatch>,
    pub error: String,
    #[cfg(debug_assertions)]
    owner_thread: std::thread::ThreadId,
}
```

**Option B (make C# wrapper safe):** Since this is a UI framework that runs on the dispatcher thread, the simplest approach is to document the threading requirement in the C# wrapper's XML doc:

```csharp
/// <summary>
/// NOT thread-safe. Must be called from the same thread (typically the UI dispatcher thread).
/// </summary>
```

---

## 5. High: Duplicate Key Handling

- [x] **Status:** `approved`
- **File:** `src/reconcile.rs`
- **Lines:** 85–89

**Issue:** The `reconcile_keys` function builds a `HashMap<i64, u32>` from old keys. If there are duplicate keys in the old array, the `HashMap` silently keeps only the last occurrence. This means earlier items with the same key will never be matched, producing incorrect patches — they'll be spuriously removed and then inserted as new.

Similarly in `diff.rs:276–280`, `old_key_map` has the same problem.

This is a correctness bug that will manifest if a developer accidentally assigns the same key to two sibling elements (which is a user error, but the differ should handle it gracefully rather than silently producing wrong results).

**Recommendation:**

At minimum, detect and report duplicate keys. Options:

1. **Debug assertion:** `debug_assert!(old_map.len() == old_keys.len(), "duplicate keys in old list");`
2. **Log a warning** (once logging is added): `warn!("Duplicate key {} in old list at indices {} and {}", key, existing, i);`
3. **Return an error:** Set `ctx.error` and return early

Duplicate keys in a keyed list are always a bug in the calling code, so a loud warning or error is the right call. React emits a console warning for this exact scenario.

---

## 6. High: Unbounded Recursion in diff_subtree

- [x] **Status:** `approved`
- **File:** `src/diff.rs`
- **Lines:** 66–144

**Issue:** `diff_subtree` recurses once per tree level. For a flat view tree, the depth is bounded by the UI component tree depth, which is typically manageable (10–50 levels). However:

1. There is no depth limit or guard
2. A malformed input (e.g., circular parent references, or an extremely deep tree) could cause a stack overflow
3. Stack overflow in Rust = immediate abort (not a catchable panic by default)

**Recommendation:**

Add a maximum depth parameter. A reasonable limit for a UI tree is 256 or 512:

```rust
const MAX_DEPTH: u32 = 256;

fn diff_subtree(
    ctx: &mut DiffContext,
    // ... existing params ...
    depth: u32,
) {
    if depth > MAX_DEPTH {
        ctx.error = format!("Tree depth exceeds maximum of {}", MAX_DEPTH);
        return;
    }
    // ... rest of function, passing depth + 1 to recursive calls ...
}
```

**Question for engineer:** What's the expected maximum depth of UI trees in Duct? If it's always shallow, this may be lower priority, but the guard is cheap and protects against pathological inputs.

---

## 7. High: Bounds Checking on Array Accesses

- [x] **Status:** `approved`
- **File:** `src/diff.rs`
- **Lines:** 23, 27, 56–57, 75–76, 104–107, 164–165, 181–182, and others

**Issue:** Throughout `diff.rs`, array accesses use direct indexing like `old_props[(old_first + oi as u32) as usize]` and `old_nodes[old_index as usize]`. If the input data is malformed (e.g., `first_prop + prop_count` exceeds the props array length, or `first_child + child_count` exceeds the nodes array length), these will panic.

Combined with issue #1 (no `catch_unwind`), this means malformed input from C# will crash the entire process.

**Recommendation:**

Add validation at the entry point (`diff_trees`) that checks invariants:

```rust
fn validate_tree(nodes: &[DifferNode], props: &[DifferProp]) -> Result<(), String> {
    for (i, node) in nodes.iter().enumerate() {
        let prop_end = node.first_prop as usize + node.prop_count as usize;
        if prop_end > props.len() {
            return Err(format!("Node {} props out of bounds: {}..{} but props len is {}",
                i, node.first_prop, prop_end, props.len()));
        }
        let child_end = node.first_child as usize + node.child_count as usize;
        if child_end > nodes.len() {
            return Err(format!("Node {} children out of bounds: {}..{} but nodes len is {}",
                i, node.first_child, child_end, nodes.len()));
        }
    }
    Ok(())
}
```

Call this at the top of `diff_trees` and return an error code through FFI if validation fails. This converts index-out-of-bounds panics into clean error paths.

---

## 8. Medium: PatchOp Enum Representation

- [x] **Status:** `approved`
- **File:** `src/types.rs`
- **Lines:** 38–45

**Issue:** `PatchOp` is `#[repr(C)]` but not `#[repr(i32)]`. The `#[repr(C)]` representation for enums uses the platform's C `int` type, which is *almost always* 32-bit on modern platforms, but this is technically platform-dependent. The C# side uses `ViewPatchOp : int` (always 32-bit).

This works on Windows x86_64 today but is a latent portability issue.

**Recommendation:**

Change to explicit representation:

```rust
#[repr(i32)]  // Explicit 32-bit, matches C# int
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum PatchOp {
    None = 0,
    Insert = 1,
    // ...
}
```

This makes the ABI contract explicit rather than relying on platform defaults.

---

## 9. Medium: Dead Code — error Field

- [x] **Status:** `approved`
- **File:** `src/arena.rs`, `src/ffi.rs`
- **Lines:** arena.rs:10, ffi.rs:140–150

**Issue:** `DiffContext.error` is a `String` that is:
- Declared in `arena.rs:10`
- Cleared in `arena.rs:23` (`reset()`)
- Read in `ffi.rs:145–148` (`differ_get_error`)
- **Never written to anywhere in the codebase**

This is dead code. The `differ_get_error` FFI function will always return an empty string.

**Recommendation:**

Either:
1. **Remove it** until it's actually needed (YAGNI)
2. **Wire it up** — use it for the error paths recommended in issues #1, #6, and #7

If you implement the `catch_unwind` and validation recommendations, this field becomes the natural place to store error messages. In that case, keep it but document its purpose.

---

## 10. Medium: differ_get_error Returns Non-Null-Terminated String

- [x] **Status:** `approved`
- **File:** `src/ffi.rs`
- **Lines:** 140–150

**Issue:** `differ_get_error` returns `ctx.error.as_ptr()` — a pointer to UTF-8 bytes. Rust strings are **not** null-terminated. If the C# side reads this as a C string (null-terminated), it will read past the buffer until it hits a zero byte — a buffer over-read vulnerability.

The empty case returns `b"\0".as_ptr()` (a single null byte), which is correct. But the non-empty case at line 148 has no null terminator.

Also: the C# side doesn't call `differ_get_error` at all (it's not in the `NativeMethods` class), so this is currently unused. But if someone adds it later without fixing this, it will be a security issue.

**Recommendation:**

If you keep this function, ensure the error string is null-terminated:

```rust
// When setting the error:
ctx.error = format!("some error\0");

// Or, better, use CString:
use std::ffi::CString;
// Store as CString instead of String, which guarantees null termination
```

Or simply remove the function until it's needed, and when you re-add it, design the API to also return the string length.

---

## 11. Medium: Module Naming — arena.rs

- [x] **Status:** `optional`
- **File:** `src/arena.rs`

**Issue:** The file is named `arena.rs` and the doc comment says "Reusable result buffer for zero-allocation diffing." The lib.rs doc comment says "All hot-path allocations use a reusable arena that resets between diffs — zero GC pressure."

This is misleading. An arena allocator (bump allocator) is a specific data structure. What you have is a `Vec<DifferPatch>` that gets cleared and reused. That's a **reusable buffer**, not an arena. The "zero-allocation" claim is also incorrect — `reconcile_keyed_children` in `diff.rs:230` creates a brand new `DiffContext`, and the reconciliation code allocates `HashMap`s and `Vec`s.

Using incorrect terminology in a crate that other engineers will maintain leads to confusion.

**Recommendation:**

1. Rename `arena.rs` to `context.rs` (since the struct is `DiffContext`)
2. Update the doc comments to accurately describe what's happening: "Reusable result buffer that amortizes allocation across diffs"
3. Drop the "zero-allocation" claim from lib.rs, or qualify it: "amortized allocation on the hot path"

---

## 12. Medium: Hash Function Duplication

- [x] **Status:** `approved`
- **File:** `src/ffi.rs`
- **Lines:** 126–137 (unsafe FFI version), 153–160 (safe version)

**Issue:** The FNV-1a hash function is implemented twice — once as an unsafe `extern "C"` function and once as a safe wrapper. The implementations are identical but maintained separately. If one is updated and the other isn't, hashes will diverge between FFI and test usage.

**Recommendation:**

Have the unsafe FFI version delegate to the safe version:

```rust
pub fn fnv1a_hash(bytes: &[u8]) -> u32 {
    let mut hash: u32 = 0x811c_9dc5;
    for &b in bytes {
        hash ^= b as u32;
        hash = hash.wrapping_mul(0x0100_0193);
    }
    hash
}

#[no_mangle]
pub unsafe extern "C" fn differ_hash_string(s: *const u8, len: u32) -> u32 {
    if s.is_null() {
        return 0;
    }
    let bytes = slice::from_raw_parts(s, len as usize);
    fnv1a_hash(bytes)
}
```

This also gives the hash function a proper name (`fnv1a_hash`) instead of `differ_hash_string_safe`.

Additionally: the C# side (`ViewDiffer.cs:97–106`) has its **own** implementation of FNV-1a. That's three copies of the same algorithm. Consider whether the C# side should always call the Rust version, or document clearly that these must be kept in sync.

---

## 13. Medium: Allocation in reconcile_keyed_children

- [x] **Status:** `optional`
- **File:** `src/diff.rs`
- **Lines:** 223–231, 263–280

**Issue:** `reconcile_keyed_children` allocates:
1. Two `Vec<i64>` for mid-section keys (lines 223–228)
2. A new `DiffContext` (line 230)
3. A `HashSet<u32>` for moved/inserted tracking (lines 263–268)
4. A `HashSet<u32>` for removed tracking (lines 269–274)
5. A `HashMap<i64, u32>` for old key lookup (lines 276–280)

For a library claiming "zero GC pressure" and arena-based allocation, this is a lot of heap allocation in the hot path. For typical UI trees (small child lists), this is fine. For large lists (virtualized lists with hundreds of items), this could be measurable.

**Recommendation:**

This is not urgent — measure before optimizing. But if performance becomes a concern:

1. The two `Vec<i64>` for keys could use `SmallVec` (stack-allocated for small lists)
2. The `HashSet`s could be replaced with bit vectors when the index range is small
3. The `mid_ctx` allocation could be avoided by adding a "sub-range" mode to the main context

For now, update the doc comments to be honest about allocation behavior. Developers reading "zero-allocation" will be misled.

---

## 14. Medium: LIS Backtrack Edge Case

- [x] **Status:** `approved`
- **File:** `src/reconcile.rs`
- **Lines:** 52–58

**Issue:** In the LIS backtrack phase:

```rust
let mut k = tail_indices[tails.len() - 1];
for i in (0..tails.len()).rev() {
    result[i] = k;
    k = predecessors[k].unwrap_or(0);
}
```

The `unwrap_or(0)` is suspicious. When `i == 0`, we're at the start of the LIS, and `predecessors[k]` should be `None` (the first element has no predecessor). Using `0` as the fallback means `k` becomes `0`, which is only correct if `0` happens to be the first element of the LIS. If the LIS doesn't start at index 0, this produces an incorrect result.

However, since `k` is only used for `result[i]` and `i` is already 0 at that point, the assignment `result[0] = k` is correct (the value of `k` was set in the previous iteration). The `unwrap_or(0)` value is never actually used because the loop ends.

**Recommendation:**

Even though the current code is technically correct, the `unwrap_or(0)` hides the intent and makes the code fragile. Replace with:

```rust
for i in (0..tails.len()).rev() {
    result[i] = k;
    if i > 0 {
        k = predecessors[k].expect("LIS predecessor chain broken");
    }
}
```

This makes the invariant explicit: every element except the first must have a predecessor.

---

## 15. Low: Cargo.toml — Release Profile

- [x] **Status:** `approved`
- **File:** `Cargo.toml`

**Issue:** No `[profile.release]` section. The default release profile is reasonable, but for a native library called from a UI framework, you should explicitly configure optimization:

**Recommendation:**

```toml
[profile.release]
opt-level = 3
lto = "thin"        # Link-time optimization for smaller, faster binary
strip = "symbols"   # Strip debug symbols from release binary
panic = "abort"     # No unwinding in release — smaller binary, defense-in-depth for FFI
```

`lto = "thin"` is particularly valuable for cdylib targets — it enables cross-module optimization. `panic = "abort"` prevents unwinding across FFI in release (but you still need `catch_unwind` for debug builds and for capturing error messages).

---

## 16. Low: Cargo.toml — Lints Configuration

- [x] **Status:** `optional`
- **File:** `Cargo.toml`

**Issue:** No `[lints]` section. Rust has excellent built-in and clippy lints that catch common mistakes. For a crate with unsafe FFI code, extra strictness pays off.

**Recommendation:**

```toml
[lints.rust]
unsafe_op_in_unsafe_fn = "warn"

[lints.clippy]
all = { level = "warn", priority = -1 }
pedantic = { level = "warn", priority = -1 }
# Allow specific pedantic lints that are too noisy:
cast_possible_truncation = "allow"   # We do deliberate u32-to-usize casts
cast_sign_loss = "allow"             # We use i32 with -1 sentinel
```

At minimum, enable `unsafe_op_in_unsafe_fn` — it forces you to wrap each unsafe operation in its own `unsafe` block within unsafe functions, making the safety reasoning more granular. This is on track to become a future Rust edition default.

---

## 17. Low: lib.rs Module Doc Inconsistency

- [x] **Status:** `approved`
- **File:** `src/lib.rs`
- **Lines:** 1

**Issue:** The module doc says `//! # patch_differ` but the crate is named `viewdiffer` (renamed specifically to avoid Windows PCA heuristic, as noted in Cargo.toml). The doc should match the actual crate name.

**Recommendation:**

Change line 1 to `//! # viewdiffer`.

---

## 18. Low: Module Visibility

- [x] **Status:** `optional`
- **File:** `src/lib.rs`
- **Lines:** 9–13

**Issue:** All modules are `pub`:
```rust
pub mod types;
pub mod arena;
pub mod diff;
pub mod reconcile;
pub mod ffi;
```

For a `cdylib` (C dynamic library), the public Rust API surface doesn't matter for the DLL — only `#[no_mangle] extern "C"` functions are exported. However, the crate also builds as `rlib` (line 11 in Cargo.toml: `crate-type = ["cdylib", "rlib"]`), meaning these modules are accessible to Rust consumers.

**Question for engineer:** Is the `rlib` target needed? If this crate is only consumed via FFI from C#, you can remove `rlib` and the module visibility question goes away. If Rust consumers need access (e.g., for testing from another crate), then `pub` is fine, but consider which modules should be part of the public API vs. implementation details.

---

## 19. Low: Missing Default Impl

- [x] **Status:** `optional`
- **File:** `src/arena.rs`
- **Lines:** 14–19

**Issue:** `DiffContext::new()` could also be `impl Default for DiffContext`. This is idiomatic Rust — clippy's `new_without_default` lint flags this. Not a bug, just a convention that other Rust engineers will expect.

**Recommendation:**

```rust
impl Default for DiffContext {
    fn default() -> Self {
        Self::new()
    }
}
```

---

## 20. Low: Missing PartialEq on DifferPatch

- [x] **Status:** `optional`
- **File:** `src/types.rs`
- **Lines:** 49

**Issue:** `DifferPatch` derives `Debug, Clone, Copy` but not `PartialEq`. This makes tests harder to write — you can't `assert_eq!` on patches directly and must inspect fields individually. `DifferNode` and `DifferProp` also lack `PartialEq`.

**Recommendation:**

Add `PartialEq, Eq` to all `#[derive]` lists:

```rust
#[repr(C)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct DifferPatch { ... }
```

This has no runtime cost and dramatically improves test ergonomics.

---

## 21. Test Coverage Gaps

- [x] **Status:** `approved`
- **File:** `src/tests.rs`, `src/reconcile.rs` (lis_tests)

**Current state:** 22 tests, all passing. The tests cover basic scenarios well. Here are the gaps:

### 21a. No FFI layer tests

The `unsafe extern "C"` functions are the most critical code to test — they're where memory safety bugs live. Currently, zero tests exercise the FFI functions directly.

**Recommendation:** Add tests that call `differ_create_context`, `differ_trees_ffi`, `differ_get_error`, and `differ_destroy_context`. Test null pointer handling, empty inputs via FFI, and the full create-use-destroy lifecycle.

```rust
#[test]
fn ffi_create_destroy_context() {
    unsafe {
        let ctx = differ_create_context();
        assert!(!ctx.is_null());
        differ_destroy_context(ctx);
    }
}

#[test]
fn ffi_null_context_returns_error() {
    unsafe {
        let mut patches: *const DifferPatch = std::ptr::null();
        let mut count: u32 = 0;
        let result = differ_trees_ffi(
            std::ptr::null_mut(),
            std::ptr::null(), 0, std::ptr::null(), 0,
            std::ptr::null(), 0, std::ptr::null(), 0,
            &mut patches, &mut count,
        );
        assert_eq!(result, -1);
    }
}
```

### 21b. No deep tree tests

All tree tests use 1–3 levels of depth. There's no test that exercises recursive diffing at meaningful depth (e.g., 10+ levels with mixed property and child changes).

### 21c. No keyed children in tree diff tests

The `keyed_reorder`, `keyed_insert_new`, etc. tests exercise `reconcile_keys` directly, but no test exercises the full `diff_trees` → `reconcile_keyed_children` → `reconcile_keys` code path. The index remapping logic in `reconcile_keyed_children` (diff.rs:234–260) is complex and untested through integration.

**Recommendation:** Add a test with a tree where a parent has keyed children, and verify the patches are correct with absolute (not relative) indices.

### 21d. No property removal detection test via diff_trees

`property_removed` test exists but doesn't verify the convention that removed props get `new_value_hash == 0` and `target_index == 0`. Actually — it does check `new_value_hash == 0` on line 96. But `target_index` is `0` for removed props, which is the same as a valid prop index. This convention should be documented and tested explicitly.

### 21e. No duplicate key tests

No test verifies behavior when duplicate keys are present in old or new lists. This is especially important given issue #5.

### 21f. No large input / stress tests

No test with more than ~6 elements. For a diffing engine, it's valuable to test with hundreds or thousands of nodes to catch:
- Performance regressions
- Integer overflow in index calculations
- Memory usage patterns

```rust
#[test]
fn stress_large_flat_list() {
    let mut ctx = DiffContext::new();
    let n = 1000;
    let old: Vec<DifferNode> = (0..n).map(|i| DifferNode {
        type_id: 1, key: i as i64, parent_index: -1,
        prop_count: 0, child_count: 0, first_child: 0, first_prop: 0,
    }).collect();
    // ... test with reversed, shuffled, etc.
}
```

### 21g. LIS tests don't verify actual subsequence correctness

The LIS tests only check the length of the result, not whether it's actually a valid longest increasing subsequence. For example, `lis_sorted` checks `result.len() == 4` but doesn't verify `result == [0, 1, 2, 3]`.

**Recommendation:** Verify both the length AND that the result is a valid increasing subsequence of the input.

### 21h. No tests for `differ_hash_string` FFI function

The unsafe FFI hash function is tested only through the safe wrapper. Add a test that calls the actual `extern "C"` function with a valid pointer and verifies it matches the safe version. Also test the null-pointer case (should return 0).

---

## 22. C# Interop File Review

- [x] **Status:** `approved`
- **File:** `Duct/Native/ViewDiffer.cs`

This file is C# rather than Rust, but it's tightly coupled to the Rust code and worth reviewing in context.

### 22a. ReadOnlySpan lifetime escaping fixed block (Lines 33–63)

The `DiffTrees` method returns a `ReadOnlySpan<ViewPatch>` that points into the Rust context's internal buffer. The span is valid until the next FFI call or dispose. The `fixed` blocks pin the *input* arrays, but the *output* span points to Rust heap memory (the `ctx.patches` Vec), which is not pinned.

This is actually correct — Rust's Vec buffer won't move until the next `diff_trees` call (which calls `reset()`/`clear()`). But the comment on line 31 ("valid until the next call to DiffTrees or Dispose") is the critical safety contract and should be MORE prominent. Consider returning a copied array instead of a span for safety, or adding a large warning comment.

### 22b. HashString duplication (Lines 97–106)

The C# `HashString` method is a third copy of the FNV-1a algorithm (alongside the two in ffi.rs). If someone changes the hash constant or algorithm, all three must be updated.

**Recommendation:** Either always call through to Rust (`differ_hash_string`), or add a cross-validation test that computes the same hash on both sides and compares.

### 22c. No finalizer (Lines 108–116)

`ViewDiffer` implements `IDisposable` but has no finalizer (`~ViewDiffer()`). If a consumer forgets to call `Dispose()`, the Rust context leaks. For a class wrapping an unmanaged resource, the standard pattern is `IDisposable` + finalizer.

**Question for engineer:** Is this intentional? If `ViewDiffer` is always used in a `using` block, a finalizer isn't strictly necessary, but it's a safety net for the common "forgot to dispose" mistake.

---

## 23. .gitignore Review

- [x] **Status:** `approved`
- **File:** `.gitignore`
- **Lines:** 205–206

**Issue:** The Rust `target/` directory is correctly ignored on line 206:
```
Duct/Native/differ/target/
```

However, the broad `[Dd]ebug/` and `[Rr]elease/` patterns on lines 15–16 would also match `Duct/Native/differ/target/debug/` and `Duct/Native/differ/target/release/`, so the explicit rule is redundant but not harmful.

**Minor concern:** There's no `Cargo.lock` in the gitignore. For a **library crate** (which this is — it's consumed via FFI, not run as a standalone binary), the Rust convention is to NOT commit `Cargo.lock`. However, since this library is pinned to a specific application and has zero dependencies, it doesn't matter. If external dependencies are added in the future, consider following the Rust convention of not committing `Cargo.lock` for libraries.

**Question for engineer:** Actually, since this is a `cdylib` that's part of a larger application build, committing `Cargo.lock` is arguably correct (it ensures reproducible builds). The Rust convention about libraries mainly applies to crates published on crates.io. Your call — either way is defensible.

---

## Summary

### Critical (fix before shipping)
1. **FFI panic safety** — `catch_unwind` on all extern functions
2. **Struct layout verification** — compile-time size/alignment assertions

### High (fix soon)
3. **Logging** — add `log` crate with FFI callback for C# host
4. **Thread safety** — document or enforce single-thread requirement
5. **Duplicate key handling** — detect and warn
6. **Unbounded recursion** — add depth limit
7. **Bounds checking** — validate input at FFI entry point

### Medium (improve quality)
8. PatchOp enum should be `#[repr(i32)]`
9. Dead `error` field — wire up or remove
10. `differ_get_error` null termination
11. Rename arena.rs to context.rs
12. Hash function deduplication
13. Document allocation behavior honestly
14. LIS backtrack clarity

### Low (polish)
15–20. Cargo.toml profiles/lints, doc fixes, derive improvements

### Test gaps
21. FFI tests, deep tree tests, keyed children in tree diff, large input stress tests, LIS correctness verification

### C# interop
22. Span lifetime safety, hash duplication, finalizer pattern

---

**Overall assessment:** The core algorithms (tree diff, keyed reconciliation with LIS) are well-implemented and demonstrate good understanding of the problem space. The code is clean, well-organized, and appropriately commented. The main gaps are in the **operational robustness** layer — FFI safety, logging, input validation, and test coverage. These are common blind spots for engineers new to Rust's FFI story, and fixing them will make this code production-ready.
