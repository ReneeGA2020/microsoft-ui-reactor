//! C FFI for P/Invoke from C#.
//!
//! All functions are `extern "C"` with `#[no_mangle]`.
//! The DiffContext is heap-allocated and returned as an opaque pointer.
//!
//! # Safety
//!
//! NOT thread-safe. All calls using the same context pointer must come from
//! the same thread (typically the UI dispatcher thread).
//!
//! All `extern "C"` functions are wrapped in `catch_unwind` so that a panic
//! in Rust code does not abort the C# host process. On panic, the error
//! message is stored in `ctx.error` and a `-2` error code is returned.

use crate::context::DiffContext;
use crate::diff::diff_trees;
use crate::reconcile::reconcile_keys;
use crate::types::*;
use std::panic::{catch_unwind, AssertUnwindSafe};
use std::slice;

/// Opaque context handle for FFI.
type DifferContextPtr = *mut DiffContext;

/// FNV-1a hash for byte slices. Single implementation used by both FFI and internal code.
pub fn fnv1a_hash(bytes: &[u8]) -> u32 {
    let mut hash: u32 = 0x811c_9dc5;
    for &b in bytes {
        hash ^= b as u32;
        hash = hash.wrapping_mul(0x0100_0193);
    }
    hash
}

// ──── Context lifecycle ─────────────────────────────────────────────────

#[no_mangle]
pub extern "C" fn differ_create_context() -> DifferContextPtr {
    match catch_unwind(|| Box::into_raw(Box::new(DiffContext::new()))) {
        Ok(ptr) => ptr,
        Err(_) => std::ptr::null_mut(),
    }
}

#[no_mangle]
pub unsafe extern "C" fn differ_destroy_context(ctx: DifferContextPtr) {
    if ctx.is_null() {
        return;
    }
    let _ = catch_unwind(AssertUnwindSafe(|| {
        // SAFETY: ctx was created by differ_create_context via Box::into_raw,
        // and the caller guarantees it is only destroyed once.
        unsafe { drop(Box::from_raw(ctx)) };
    }));
}

// ──── Tree diffing ──────────────────────────────────────────────────────

#[no_mangle]
pub unsafe extern "C" fn differ_trees_ffi(
    ctx: DifferContextPtr,
    old_nodes: *const DifferNode,
    old_count: u32,
    old_props: *const DifferProp,
    old_prop_count: u32,
    new_nodes: *const DifferNode,
    new_count: u32,
    new_props: *const DifferProp,
    new_prop_count: u32,
    out_patches: *mut *const DifferPatch,
    out_patch_count: *mut u32,
) -> i32 {
    if ctx.is_null() || out_patches.is_null() || out_patch_count.is_null() {
        return -1;
    }

    let result = catch_unwind(AssertUnwindSafe(|| {
        // SAFETY: caller guarantees ctx is valid and non-null (checked above).
        let ctx = unsafe { &mut *ctx };

        // SAFETY: caller guarantees pointers are valid for the given counts.
        let old_nodes_slice = if old_count == 0 || old_nodes.is_null() {
            &[]
        } else {
            unsafe { slice::from_raw_parts(old_nodes, old_count as usize) }
        };

        let old_props_slice = if old_prop_count == 0 || old_props.is_null() {
            &[]
        } else {
            unsafe { slice::from_raw_parts(old_props, old_prop_count as usize) }
        };

        let new_nodes_slice = if new_count == 0 || new_nodes.is_null() {
            &[]
        } else {
            unsafe { slice::from_raw_parts(new_nodes, new_count as usize) }
        };

        let new_props_slice = if new_prop_count == 0 || new_props.is_null() {
            &[]
        } else {
            unsafe { slice::from_raw_parts(new_props, new_prop_count as usize) }
        };

        diff_trees(ctx, old_nodes_slice, old_props_slice, new_nodes_slice, new_props_slice);

        // SAFETY: out_patches and out_patch_count are non-null (checked above).
        unsafe {
            *out_patches = if ctx.patches.is_empty() {
                std::ptr::null()
            } else {
                ctx.patches.as_ptr()
            };
            *out_patch_count = ctx.patches.len() as u32;
        }

        if ctx.error.is_empty() { 0i32 } else { -3 }
    }));

    match result {
        Ok(code) => code,
        Err(e) => {
            // SAFETY: ctx is non-null (checked above), and we're outside catch_unwind.
            let ctx = unsafe { &mut *ctx };
            let msg = if let Some(s) = e.downcast_ref::<&str>() {
                format!("panic: {s}\0")
            } else if let Some(s) = e.downcast_ref::<String>() {
                format!("panic: {s}\0")
            } else {
                "panic: <unknown>\0".to_string()
            };
            log::error!("{}", &msg[..msg.len() - 1]);
            ctx.error = msg;
            -2
        }
    }
}

// ──── Key reconciliation ────────────────────────────────────────────────

#[no_mangle]
pub unsafe extern "C" fn differ_reconcile_keys_ffi(
    ctx: DifferContextPtr,
    old_keys: *const i64,
    old_count: u32,
    new_keys: *const i64,
    new_count: u32,
    out_patches: *mut *const DifferPatch,
    out_patch_count: *mut u32,
) -> i32 {
    if ctx.is_null() || out_patches.is_null() || out_patch_count.is_null() {
        return -1;
    }

    let result = catch_unwind(AssertUnwindSafe(|| {
        // SAFETY: caller guarantees ctx is valid and non-null (checked above).
        let ctx = unsafe { &mut *ctx };
        ctx.reset();

        // SAFETY: caller guarantees pointers are valid for the given counts.
        let old_slice = if old_count == 0 || old_keys.is_null() {
            &[]
        } else {
            unsafe { slice::from_raw_parts(old_keys, old_count as usize) }
        };

        let new_slice = if new_count == 0 || new_keys.is_null() {
            &[]
        } else {
            unsafe { slice::from_raw_parts(new_keys, new_count as usize) }
        };

        reconcile_keys(ctx, old_slice, new_slice);

        // SAFETY: out_patches and out_patch_count are non-null (checked above).
        unsafe {
            *out_patches = if ctx.patches.is_empty() {
                std::ptr::null()
            } else {
                ctx.patches.as_ptr()
            };
            *out_patch_count = ctx.patches.len() as u32;
        }

        0i32
    }));

    match result {
        Ok(code) => code,
        Err(e) => {
            // SAFETY: ctx is non-null (checked above).
            let ctx = unsafe { &mut *ctx };
            let msg = if let Some(s) = e.downcast_ref::<&str>() {
                format!("panic: {s}\0")
            } else if let Some(s) = e.downcast_ref::<String>() {
                format!("panic: {s}\0")
            } else {
                "panic: <unknown>\0".to_string()
            };
            log::error!("{}", &msg[..msg.len() - 1]);
            ctx.error = msg;
            -2
        }
    }
}

// ──── Hash ──────────────────────────────────────────────────────────────

/// FNV-1a hash for strings (type IDs, property names).
#[no_mangle]
pub unsafe extern "C" fn differ_hash_string(s: *const u8, len: u32) -> u32 {
    if s.is_null() {
        return 0;
    }
    // SAFETY: caller guarantees s points to at least len bytes.
    let bytes = unsafe { slice::from_raw_parts(s, len as usize) };
    fnv1a_hash(bytes)
}

// ──── Error retrieval ───────────────────────────────────────────────────

/// Returns a pointer to the null-terminated error string.
/// The pointer is valid until the next FFI call on this context.
#[no_mangle]
pub unsafe extern "C" fn differ_get_error(ctx: DifferContextPtr) -> *const u8 {
    if ctx.is_null() {
        return b"null context\0".as_ptr();
    }
    // SAFETY: caller guarantees ctx is valid and non-null (checked above).
    let ctx = unsafe { &*ctx };
    if ctx.error.is_empty() {
        b"\0".as_ptr()
    } else {
        // Error strings are stored with a trailing \0 by the catch_unwind handlers
        ctx.error.as_ptr()
    }
}

// ──── Struct size verification ──────────────────────────────────────────

/// Returns the size of DifferNode for cross-language layout verification.
#[no_mangle]
pub extern "C" fn differ_node_size() -> u32 {
    std::mem::size_of::<DifferNode>() as u32
}

/// Returns the size of DifferProp for cross-language layout verification.
#[no_mangle]
pub extern "C" fn differ_prop_size() -> u32 {
    std::mem::size_of::<DifferProp>() as u32
}

/// Returns the size of DifferPatch for cross-language layout verification.
#[no_mangle]
pub extern "C" fn differ_patch_size() -> u32 {
    std::mem::size_of::<DifferPatch>() as u32
}
