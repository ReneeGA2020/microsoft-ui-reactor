//! C FFI for P/Invoke from C#.
//!
//! All functions are `extern "C"` with `#[no_mangle]`.
//! The DiffContext is heap-allocated and returned as an opaque pointer.

use crate::arena::DiffContext;
use crate::diff::diff_trees;
use crate::reconcile::reconcile_keys;
use crate::types::*;
use std::slice;

/// Opaque context handle for FFI.
type DifferContextPtr = *mut DiffContext;

#[no_mangle]
pub extern "C" fn differ_create_context() -> DifferContextPtr {
    Box::into_raw(Box::new(DiffContext::new()))
}

#[no_mangle]
pub unsafe extern "C" fn differ_destroy_context(ctx: DifferContextPtr) {
    if !ctx.is_null() {
        drop(Box::from_raw(ctx));
    }
}

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

    let ctx = &mut *ctx;

    let old_nodes_slice = if old_count == 0 || old_nodes.is_null() {
        &[]
    } else {
        slice::from_raw_parts(old_nodes, old_count as usize)
    };

    let old_props_slice = if old_prop_count == 0 || old_props.is_null() {
        &[]
    } else {
        slice::from_raw_parts(old_props, old_prop_count as usize)
    };

    let new_nodes_slice = if new_count == 0 || new_nodes.is_null() {
        &[]
    } else {
        slice::from_raw_parts(new_nodes, new_count as usize)
    };

    let new_props_slice = if new_prop_count == 0 || new_props.is_null() {
        &[]
    } else {
        slice::from_raw_parts(new_props, new_prop_count as usize)
    };

    diff_trees(ctx, old_nodes_slice, old_props_slice, new_nodes_slice, new_props_slice);

    *out_patches = if ctx.patches.is_empty() {
        std::ptr::null()
    } else {
        ctx.patches.as_ptr()
    };
    *out_patch_count = ctx.patches.len() as u32;

    0
}

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

    let ctx = &mut *ctx;
    ctx.reset();

    let old_slice = if old_count == 0 || old_keys.is_null() {
        &[]
    } else {
        slice::from_raw_parts(old_keys, old_count as usize)
    };

    let new_slice = if new_count == 0 || new_keys.is_null() {
        &[]
    } else {
        slice::from_raw_parts(new_keys, new_count as usize)
    };

    reconcile_keys(ctx, old_slice, new_slice);

    *out_patches = if ctx.patches.is_empty() {
        std::ptr::null()
    } else {
        ctx.patches.as_ptr()
    };
    *out_patch_count = ctx.patches.len() as u32;

    0
}

/// FNV-1a hash for strings (type IDs, property names).
#[no_mangle]
pub unsafe extern "C" fn differ_hash_string(s: *const u8, len: u32) -> u32 {
    if s.is_null() {
        return 0;
    }
    let bytes = slice::from_raw_parts(s, len as usize);
    let mut hash: u32 = 0x811c_9dc5;
    for &b in bytes {
        hash ^= b as u32;
        hash = hash.wrapping_mul(0x0100_0193);
    }
    hash
}

#[no_mangle]
pub unsafe extern "C" fn differ_get_error(ctx: DifferContextPtr) -> *const u8 {
    if ctx.is_null() {
        return b"null context\0".as_ptr();
    }
    let ctx = &*ctx;
    if ctx.error.is_empty() {
        b"\0".as_ptr()
    } else {
        ctx.error.as_ptr()
    }
}

/// Safe wrapper for tests.
pub fn differ_hash_string_safe(bytes: &[u8]) -> u32 {
    let mut hash: u32 = 0x811c_9dc5;
    for &b in bytes {
        hash ^= b as u32;
        hash = hash.wrapping_mul(0x0100_0193);
    }
    hash
}
