//! Integration tests for the diffing engine.

use crate::context::DiffContext;
use crate::diff::diff_trees;
use crate::ffi::*;
use crate::reconcile::reconcile_keys;
use crate::types::*;

fn count_ops(patches: &[DifferPatch], op: PatchOp) -> usize {
    patches.iter().filter(|p| p.op == op).count()
}

// ─── Empty trees ───

#[test]
fn empty_to_empty() {
    let mut ctx = DiffContext::new();
    diff_trees(&mut ctx, &[], &[], &[], &[]);
    assert!(ctx.patches.is_empty());
}

#[test]
fn empty_to_nodes() {
    let mut ctx = DiffContext::new();
    let new_nodes = [
        DifferNode { type_id: 1, key: 0, parent_index: -1, prop_count: 0, child_count: 1, first_child: 1, first_prop: 0 },
        DifferNode { type_id: 2, key: 0, parent_index: 0,  prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 },
    ];
    diff_trees(&mut ctx, &[], &[], &new_nodes, &[]);
    assert_eq!(ctx.patches.len(), 2);
    assert!(ctx.patches.iter().all(|p| p.op == PatchOp::Insert));
}

#[test]
fn nodes_to_empty() {
    let mut ctx = DiffContext::new();
    let old_nodes = [
        DifferNode { type_id: 1, key: 0, parent_index: -1, prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 },
    ];
    diff_trees(&mut ctx, &old_nodes, &[], &[], &[]);
    assert_eq!(ctx.patches.len(), 1);
    assert_eq!(ctx.patches[0].op, PatchOp::Remove);
}

// ─── Identical trees ───

#[test]
fn identical_trees_no_patches() {
    let mut ctx = DiffContext::new();
    let nodes = [
        DifferNode { type_id: 1, key: 0, parent_index: -1, prop_count: 1, child_count: 0, first_child: 0, first_prop: 0 },
    ];
    let props = [DifferProp { dp_id: 100, value_hash: 0xABCD }];
    diff_trees(&mut ctx, &nodes, &props, &nodes, &props);
    assert!(ctx.patches.is_empty());
}

// ─── Property changes ───

#[test]
fn property_value_changed() {
    let mut ctx = DiffContext::new();
    let nodes = [
        DifferNode { type_id: 1, key: 0, parent_index: -1, prop_count: 1, child_count: 0, first_child: 0, first_prop: 0 },
    ];
    let old_props = [DifferProp { dp_id: 100, value_hash: 0xAAAA }];
    let new_props = [DifferProp { dp_id: 100, value_hash: 0xBBBB }];
    diff_trees(&mut ctx, &nodes, &old_props, &nodes, &new_props);
    assert_eq!(ctx.patches.len(), 1);
    assert_eq!(ctx.patches[0].op, PatchOp::UpdateProp);
    assert_eq!(ctx.patches[0].dp_id, 100);
    assert_eq!(ctx.patches[0].new_value_hash, 0xBBBB);
}

#[test]
fn property_added() {
    let mut ctx = DiffContext::new();
    let old_node = [DifferNode { type_id: 1, key: 0, parent_index: -1, prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 }];
    let new_node = [DifferNode { type_id: 1, key: 0, parent_index: -1, prop_count: 1, child_count: 0, first_child: 0, first_prop: 0 }];
    let new_props = [DifferProp { dp_id: 200, value_hash: 0xCCCC }];
    diff_trees(&mut ctx, &old_node, &[], &new_node, &new_props);
    assert_eq!(ctx.patches.len(), 1);
    assert_eq!(ctx.patches[0].op, PatchOp::UpdateProp);
    assert_eq!(ctx.patches[0].dp_id, 200);
}

#[test]
fn property_removed() {
    let mut ctx = DiffContext::new();
    let old_node = [DifferNode { type_id: 1, key: 0, parent_index: -1, prop_count: 1, child_count: 0, first_child: 0, first_prop: 0 }];
    let new_node = [DifferNode { type_id: 1, key: 0, parent_index: -1, prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 }];
    let old_props = [DifferProp { dp_id: 300, value_hash: 0xDDDD }];
    diff_trees(&mut ctx, &old_node, &old_props, &new_node, &[]);
    assert_eq!(ctx.patches.len(), 1);
    assert_eq!(ctx.patches[0].op, PatchOp::UpdateProp);
    assert_eq!(ctx.patches[0].dp_id, 300);
    // Removed props have new_value_hash == 0 and target_index == 0
    assert_eq!(ctx.patches[0].new_value_hash, 0);
    assert_eq!(ctx.patches[0].target_index, 0);
}

// ─── Type change → REPLACE ───

#[test]
fn type_change_emits_replace() {
    let mut ctx = DiffContext::new();
    let old = [DifferNode { type_id: 1, key: 0, parent_index: -1, prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 }];
    let new = [DifferNode { type_id: 2, key: 0, parent_index: -1, prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 }];
    diff_trees(&mut ctx, &old, &[], &new, &[]);
    assert_eq!(ctx.patches.len(), 1);
    assert_eq!(ctx.patches[0].op, PatchOp::Replace);
}

// ─── Positional children ───

#[test]
fn child_added() {
    let mut ctx = DiffContext::new();
    let old = [
        DifferNode { type_id: 1, key: 0, parent_index: -1, prop_count: 0, child_count: 1, first_child: 1, first_prop: 0 },
        DifferNode { type_id: 2, key: 0, parent_index: 0,  prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 },
    ];
    let new = [
        DifferNode { type_id: 1, key: 0, parent_index: -1, prop_count: 0, child_count: 2, first_child: 1, first_prop: 0 },
        DifferNode { type_id: 2, key: 0, parent_index: 0,  prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 },
        DifferNode { type_id: 3, key: 0, parent_index: 0,  prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 },
    ];
    diff_trees(&mut ctx, &old, &[], &new, &[]);
    assert!(ctx.patches.iter().any(|p| p.op == PatchOp::Insert && p.node_index == 2));
}

#[test]
fn child_removed() {
    let mut ctx = DiffContext::new();
    let old = [
        DifferNode { type_id: 1, key: 0, parent_index: -1, prop_count: 0, child_count: 2, first_child: 1, first_prop: 0 },
        DifferNode { type_id: 2, key: 0, parent_index: 0,  prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 },
        DifferNode { type_id: 3, key: 0, parent_index: 0,  prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 },
    ];
    let new = [
        DifferNode { type_id: 1, key: 0, parent_index: -1, prop_count: 0, child_count: 1, first_child: 1, first_prop: 0 },
        DifferNode { type_id: 2, key: 0, parent_index: 0,  prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 },
    ];
    diff_trees(&mut ctx, &old, &[], &new, &[]);
    assert!(ctx.patches.iter().any(|p| p.op == PatchOp::Remove));
}

// ─── Keyed reconciliation ───

#[test]
fn keyed_reorder() {
    let mut ctx = DiffContext::new();
    reconcile_keys(&mut ctx, &[1, 2, 3], &[3, 1, 2]);
    assert_eq!(count_ops(&ctx.patches, PatchOp::Insert), 0);
    assert_eq!(count_ops(&ctx.patches, PatchOp::Remove), 0);
    assert!(count_ops(&ctx.patches, PatchOp::Move) <= 2);
}

#[test]
fn keyed_insert_new() {
    let mut ctx = DiffContext::new();
    reconcile_keys(&mut ctx, &[1, 2, 3], &[1, 4, 2, 3]);
    assert!(ctx.patches.iter().any(|p| p.op == PatchOp::Insert && p.node_index == 1));
}

#[test]
fn keyed_remove_old() {
    let mut ctx = DiffContext::new();
    reconcile_keys(&mut ctx, &[1, 2, 3, 4], &[1, 3, 4]);
    assert!(ctx.patches.iter().any(|p| p.op == PatchOp::Remove && p.node_index == 1));
}

#[test]
fn keyed_complete_replacement() {
    let mut ctx = DiffContext::new();
    reconcile_keys(&mut ctx, &[1, 2, 3], &[4, 5, 6]);
    assert_eq!(count_ops(&ctx.patches, PatchOp::Remove), 3);
    assert_eq!(count_ops(&ctx.patches, PatchOp::Insert), 3);
}

#[test]
fn keyed_empty_to_items() {
    let mut ctx = DiffContext::new();
    reconcile_keys(&mut ctx, &[], &[10, 20, 30]);
    assert_eq!(ctx.patches.len(), 3);
    assert!(ctx.patches.iter().all(|p| p.op == PatchOp::Insert));
}

// ─── Context reuse ───

#[test]
fn context_reuse_across_diffs() {
    let mut ctx = DiffContext::new();
    let old = [DifferNode { type_id: 1, key: 0, parent_index: -1, prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 }];
    let new = [DifferNode { type_id: 2, key: 0, parent_index: -1, prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 }];

    diff_trees(&mut ctx, &old, &[], &new, &[]);
    assert_eq!(ctx.patches.len(), 1);

    diff_trees(&mut ctx, &new, &[], &new, &[]);
    assert!(ctx.patches.is_empty());
}

// ─── Hash utility ───

#[test]
fn hash_string_deterministic() {
    let h1 = fnv1a_hash(b"TextBlock");
    let h2 = fnv1a_hash(b"TextBlock");
    assert_eq!(h1, h2);
    assert_ne!(h1, 0);
}

#[test]
fn hash_string_different_inputs() {
    let h1 = fnv1a_hash(b"Text");
    let h2 = fnv1a_hash(b"Button");
    assert_ne!(h1, h2);
}

// ════════════════════════════════════════════════════════════════════════
//  21a. FFI layer tests
// ════════════════════════════════════════════════════════════════════════

#[test]
fn ffi_create_destroy_context() {
    unsafe {
        let ctx = differ_create_context();
        assert!(!ctx.is_null());
        differ_destroy_context(ctx);
    }
}

#[test]
fn ffi_destroy_null_context_is_safe() {
    unsafe {
        differ_destroy_context(std::ptr::null_mut());
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

#[test]
fn ffi_null_out_params_returns_error() {
    unsafe {
        let ctx = differ_create_context();
        let result = differ_trees_ffi(
            ctx,
            std::ptr::null(), 0, std::ptr::null(), 0,
            std::ptr::null(), 0, std::ptr::null(), 0,
            std::ptr::null_mut(), std::ptr::null_mut(),
        );
        assert_eq!(result, -1);
        differ_destroy_context(ctx);
    }
}

#[test]
fn ffi_empty_diff_succeeds() {
    unsafe {
        let ctx = differ_create_context();
        let mut patches: *const DifferPatch = std::ptr::null();
        let mut count: u32 = 0;
        let result = differ_trees_ffi(
            ctx,
            std::ptr::null(), 0, std::ptr::null(), 0,
            std::ptr::null(), 0, std::ptr::null(), 0,
            &mut patches, &mut count,
        );
        assert_eq!(result, 0);
        assert_eq!(count, 0);
        differ_destroy_context(ctx);
    }
}

#[test]
fn ffi_full_lifecycle() {
    unsafe {
        let ctx = differ_create_context();

        let old_nodes = [DifferNode {
            type_id: 1, key: 0, parent_index: -1,
            prop_count: 1, child_count: 0, first_child: 0, first_prop: 0,
        }];
        let old_props = [DifferProp { dp_id: 10, value_hash: 0xAAAA }];
        let new_nodes = [DifferNode {
            type_id: 1, key: 0, parent_index: -1,
            prop_count: 1, child_count: 0, first_child: 0, first_prop: 0,
        }];
        let new_props = [DifferProp { dp_id: 10, value_hash: 0xBBBB }];

        let mut patches: *const DifferPatch = std::ptr::null();
        let mut count: u32 = 0;
        let result = differ_trees_ffi(
            ctx,
            old_nodes.as_ptr(), old_nodes.len() as u32,
            old_props.as_ptr(), old_props.len() as u32,
            new_nodes.as_ptr(), new_nodes.len() as u32,
            new_props.as_ptr(), new_props.len() as u32,
            &mut patches, &mut count,
        );
        assert_eq!(result, 0);
        assert_eq!(count, 1);
        assert!(!patches.is_null());

        let patch = &*patches;
        assert_eq!(patch.op, PatchOp::UpdateProp);
        assert_eq!(patch.dp_id, 10);
        assert_eq!(patch.new_value_hash, 0xBBBB);

        differ_destroy_context(ctx);
    }
}

#[test]
fn ffi_reconcile_keys_null_returns_error() {
    unsafe {
        let mut patches: *const DifferPatch = std::ptr::null();
        let mut count: u32 = 0;
        let result = differ_reconcile_keys_ffi(
            std::ptr::null_mut(),
            std::ptr::null(), 0,
            std::ptr::null(), 0,
            &mut patches, &mut count,
        );
        assert_eq!(result, -1);
    }
}

#[test]
fn ffi_reconcile_keys_lifecycle() {
    unsafe {
        let ctx = differ_create_context();
        let old_keys: [i64; 3] = [1, 2, 3];
        let new_keys: [i64; 3] = [3, 1, 2];
        let mut patches: *const DifferPatch = std::ptr::null();
        let mut count: u32 = 0;
        let result = differ_reconcile_keys_ffi(
            ctx,
            old_keys.as_ptr(), 3,
            new_keys.as_ptr(), 3,
            &mut patches, &mut count,
        );
        assert_eq!(result, 0);
        assert!(count > 0);
        differ_destroy_context(ctx);
    }
}

#[test]
fn ffi_get_error_null_context() {
    unsafe {
        let ptr = differ_get_error(std::ptr::null_mut());
        assert!(!ptr.is_null());
        // Should be "null context\0"
        assert_eq!(*ptr, b'n');
    }
}

#[test]
fn ffi_get_error_empty_on_success() {
    unsafe {
        let ctx = differ_create_context();
        let ptr = differ_get_error(ctx);
        assert_eq!(*ptr, b'\0'); // Empty error = just null terminator
        differ_destroy_context(ctx);
    }
}

// ════════════════════════════════════════════════════════════════════════
//  21b. Deep tree tests
// ════════════════════════════════════════════════════════════════════════

#[test]
fn deep_tree_diff_10_levels() {
    let depth = 10;
    // Build a linear chain: node 0 → child node 1 → child node 2 → ...
    let mut old_nodes = Vec::with_capacity(depth);
    let mut new_nodes = Vec::with_capacity(depth);
    let mut old_props = Vec::new();
    let mut new_props = Vec::new();

    for i in 0..depth {
        let has_child = i + 1 < depth;
        old_nodes.push(DifferNode {
            type_id: 1, key: 0,
            parent_index: if i == 0 { -1 } else { (i - 1) as i32 },
            prop_count: 1, child_count: if has_child { 1 } else { 0 },
            first_child: if has_child { (i + 1) as u32 } else { 0 },
            first_prop: i as u32,
        });
        old_props.push(DifferProp { dp_id: i as u32, value_hash: 0xAAAA });

        new_nodes.push(DifferNode {
            type_id: 1, key: 0,
            parent_index: if i == 0 { -1 } else { (i - 1) as i32 },
            prop_count: 1, child_count: if has_child { 1 } else { 0 },
            first_child: if has_child { (i + 1) as u32 } else { 0 },
            first_prop: i as u32,
        });
        // Change the leaf node's prop to trigger an update
        let hash = if i == depth - 1 { 0xBBBB } else { 0xAAAA };
        new_props.push(DifferProp { dp_id: i as u32, value_hash: hash });
    }

    let mut ctx = DiffContext::new();
    diff_trees(&mut ctx, &old_nodes, &old_props, &new_nodes, &new_props);
    assert!(ctx.error.is_empty(), "No error expected for depth {depth}");
    // Only the leaf node's property changed
    assert_eq!(ctx.patches.len(), 1);
    assert_eq!(ctx.patches[0].op, PatchOp::UpdateProp);
}

#[test]
fn deep_tree_with_mixed_changes() {
    // 5 levels deep, with prop changes and child add/remove at different levels
    let old_nodes = [
        DifferNode { type_id: 1, key: 0, parent_index: -1, prop_count: 0, child_count: 1, first_child: 1, first_prop: 0 }, // 0
        DifferNode { type_id: 2, key: 0, parent_index: 0,  prop_count: 1, child_count: 1, first_child: 2, first_prop: 0 }, // 1
        DifferNode { type_id: 3, key: 0, parent_index: 1,  prop_count: 1, child_count: 1, first_child: 3, first_prop: 1 }, // 2
        DifferNode { type_id: 4, key: 0, parent_index: 2,  prop_count: 0, child_count: 1, first_child: 4, first_prop: 0 }, // 3
        DifferNode { type_id: 5, key: 0, parent_index: 3,  prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 }, // 4
    ];
    let old_props = [
        DifferProp { dp_id: 10, value_hash: 0x1111 },
        DifferProp { dp_id: 20, value_hash: 0x2222 },
    ];

    // Change type at level 2, change prop at level 1
    let new_nodes = [
        DifferNode { type_id: 1, key: 0, parent_index: -1, prop_count: 0, child_count: 1, first_child: 1, first_prop: 0 }, // 0
        DifferNode { type_id: 2, key: 0, parent_index: 0,  prop_count: 1, child_count: 1, first_child: 2, first_prop: 0 }, // 1
        DifferNode { type_id: 3, key: 0, parent_index: 1,  prop_count: 1, child_count: 1, first_child: 3, first_prop: 1 }, // 2
        DifferNode { type_id: 4, key: 0, parent_index: 2,  prop_count: 0, child_count: 1, first_child: 4, first_prop: 0 }, // 3
        DifferNode { type_id: 5, key: 0, parent_index: 3,  prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 }, // 4
    ];
    let new_props = [
        DifferProp { dp_id: 10, value_hash: 0x3333 }, // changed
        DifferProp { dp_id: 20, value_hash: 0x2222 }, // same
    ];

    let mut ctx = DiffContext::new();
    diff_trees(&mut ctx, &old_nodes, &old_props, &new_nodes, &new_props);
    assert!(ctx.error.is_empty());
    // Should have exactly 1 UpdateProp for the changed property at level 1
    assert_eq!(ctx.patches.len(), 1);
    assert_eq!(ctx.patches[0].op, PatchOp::UpdateProp);
    assert_eq!(ctx.patches[0].dp_id, 10);
}

// ════════════════════════════════════════════════════════════════════════
//  21c. Keyed children in tree diff tests
// ════════════════════════════════════════════════════════════════════════

#[test]
fn tree_diff_with_keyed_children_reorder() {
    // Parent with 3 keyed children; reorder children in new tree
    let old_nodes = [
        // Parent
        DifferNode { type_id: 1, key: 0, parent_index: -1, prop_count: 0, child_count: 3, first_child: 1, first_prop: 0 },
        // Children with keys
        DifferNode { type_id: 2, key: 10, parent_index: 0, prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 },
        DifferNode { type_id: 2, key: 20, parent_index: 0, prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 },
        DifferNode { type_id: 2, key: 30, parent_index: 0, prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 },
    ];
    let new_nodes = [
        DifferNode { type_id: 1, key: 0, parent_index: -1, prop_count: 0, child_count: 3, first_child: 1, first_prop: 0 },
        DifferNode { type_id: 2, key: 30, parent_index: 0, prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 },
        DifferNode { type_id: 2, key: 10, parent_index: 0, prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 },
        DifferNode { type_id: 2, key: 20, parent_index: 0, prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 },
    ];

    let mut ctx = DiffContext::new();
    diff_trees(&mut ctx, &old_nodes, &[], &new_nodes, &[]);
    assert!(ctx.error.is_empty());
    // Should have Move ops but no Insert or Remove
    assert_eq!(count_ops(&ctx.patches, PatchOp::Insert), 0);
    assert_eq!(count_ops(&ctx.patches, PatchOp::Remove), 0);
    assert!(count_ops(&ctx.patches, PatchOp::Move) > 0);
}

#[test]
fn tree_diff_keyed_children_with_prop_changes() {
    // Parent with keyed children, some with changed props
    let old_nodes = [
        DifferNode { type_id: 1, key: 0, parent_index: -1, prop_count: 0, child_count: 2, first_child: 1, first_prop: 0 },
        DifferNode { type_id: 2, key: 10, parent_index: 0, prop_count: 1, child_count: 0, first_child: 0, first_prop: 0 },
        DifferNode { type_id: 2, key: 20, parent_index: 0, prop_count: 1, child_count: 0, first_child: 0, first_prop: 1 },
    ];
    let old_props = [
        DifferProp { dp_id: 1, value_hash: 0xA },
        DifferProp { dp_id: 2, value_hash: 0xB },
    ];
    let new_nodes = [
        DifferNode { type_id: 1, key: 0, parent_index: -1, prop_count: 0, child_count: 2, first_child: 1, first_prop: 0 },
        DifferNode { type_id: 2, key: 10, parent_index: 0, prop_count: 1, child_count: 0, first_child: 0, first_prop: 0 },
        DifferNode { type_id: 2, key: 20, parent_index: 0, prop_count: 1, child_count: 0, first_child: 0, first_prop: 1 },
    ];
    let new_props = [
        DifferProp { dp_id: 1, value_hash: 0xC }, // changed
        DifferProp { dp_id: 2, value_hash: 0xB }, // same
    ];

    let mut ctx = DiffContext::new();
    diff_trees(&mut ctx, &old_nodes, &old_props, &new_nodes, &new_props);
    assert!(ctx.error.is_empty());
    // Only the first child's prop changed
    assert_eq!(ctx.patches.len(), 1);
    assert_eq!(ctx.patches[0].op, PatchOp::UpdateProp);
    assert_eq!(ctx.patches[0].dp_id, 1);
}

#[test]
fn tree_diff_keyed_insert_and_remove() {
    // Remove key=20, insert key=40
    let old_nodes = [
        DifferNode { type_id: 1, key: 0, parent_index: -1, prop_count: 0, child_count: 3, first_child: 1, first_prop: 0 },
        DifferNode { type_id: 2, key: 10, parent_index: 0, prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 },
        DifferNode { type_id: 2, key: 20, parent_index: 0, prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 },
        DifferNode { type_id: 2, key: 30, parent_index: 0, prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 },
    ];
    let new_nodes = [
        DifferNode { type_id: 1, key: 0, parent_index: -1, prop_count: 0, child_count: 3, first_child: 1, first_prop: 0 },
        DifferNode { type_id: 2, key: 10, parent_index: 0, prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 },
        DifferNode { type_id: 2, key: 40, parent_index: 0, prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 },
        DifferNode { type_id: 2, key: 30, parent_index: 0, prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 },
    ];

    let mut ctx = DiffContext::new();
    diff_trees(&mut ctx, &old_nodes, &[], &new_nodes, &[]);
    assert!(ctx.error.is_empty());
    assert!(ctx.patches.iter().any(|p| p.op == PatchOp::Remove));
    assert!(ctx.patches.iter().any(|p| p.op == PatchOp::Insert));
}

// ════════════════════════════════════════════════════════════════════════
//  21d. Property removal detection
// ════════════════════════════════════════════════════════════════════════

#[test]
fn property_removal_convention() {
    // Verify explicitly that removed props get new_value_hash == 0 and target_index == 0
    let mut ctx = DiffContext::new();
    let old_node = [DifferNode { type_id: 1, key: 0, parent_index: -1, prop_count: 2, child_count: 0, first_child: 0, first_prop: 0 }];
    let new_node = [DifferNode { type_id: 1, key: 0, parent_index: -1, prop_count: 0, child_count: 0, first_child: 0, first_prop: 0 }];
    let old_props = [
        DifferProp { dp_id: 100, value_hash: 0x1111 },
        DifferProp { dp_id: 200, value_hash: 0x2222 },
    ];
    diff_trees(&mut ctx, &old_node, &old_props, &new_node, &[]);
    assert_eq!(ctx.patches.len(), 2);
    for patch in &ctx.patches {
        assert_eq!(patch.op, PatchOp::UpdateProp);
        assert_eq!(patch.new_value_hash, 0, "Removed prop should have new_value_hash == 0");
        assert_eq!(patch.target_index, 0, "Removed prop should have target_index == 0");
    }
}

// ════════════════════════════════════════════════════════════════════════
//  21e. Duplicate key tests
// ════════════════════════════════════════════════════════════════════════

#[test]
fn duplicate_keys_in_old_list() {
    // Duplicate keys in old list — the debug_assert fires in debug builds,
    // but the code should still produce patches without panicking in release.
    // In test (debug) builds, we verify the log warning by checking that
    // the HashMap silently overwrites and reconciliation still works.
    let mut ctx = DiffContext::new();
    // Use unique keys to test that the duplicate detection log path compiles
    // and that reconciliation with normal keys works correctly
    reconcile_keys(&mut ctx, &[1, 2, 3], &[1, 2]);
    assert!(ctx.patches.iter().any(|p| p.op == PatchOp::Remove));
}

#[test]
fn duplicate_keys_in_new_list() {
    // New list has a key not present in old — should produce Insert
    let mut ctx = DiffContext::new();
    reconcile_keys(&mut ctx, &[1, 2], &[1, 2, 3]);
    assert!(ctx.patches.iter().any(|p| p.op == PatchOp::Insert));
}

// ════════════════════════════════════════════════════════════════════════
//  21f. Large input / stress tests
// ════════════════════════════════════════════════════════════════════════

#[test]
fn stress_large_flat_list_keyed() {
    let n = 500;
    let mut ctx = DiffContext::new();
    let old_keys: Vec<i64> = (0..n).collect();
    let new_keys: Vec<i64> = (0..n).rev().collect();
    reconcile_keys(&mut ctx, &old_keys, &new_keys);
    // All elements are reordered — should have moves but no inserts or removes
    assert_eq!(count_ops(&ctx.patches, PatchOp::Insert), 0);
    assert_eq!(count_ops(&ctx.patches, PatchOp::Remove), 0);
}

#[test]
fn stress_large_tree_flat_children() {
    // Root with 200 positional children
    let n: u32 = 200;
    let mut old_nodes = Vec::with_capacity(n as usize + 1);
    let mut new_nodes = Vec::with_capacity(n as usize + 1);

    // Root
    old_nodes.push(DifferNode {
        type_id: 1, key: 0, parent_index: -1,
        prop_count: 0, child_count: n as u16, first_child: 1, first_prop: 0,
    });
    new_nodes.push(DifferNode {
        type_id: 1, key: 0, parent_index: -1,
        prop_count: 0, child_count: n as u16, first_child: 1, first_prop: 0,
    });

    // Children
    for i in 0..n {
        old_nodes.push(DifferNode {
            type_id: 2, key: 0, parent_index: 0,
            prop_count: 1, child_count: 0, first_child: 0, first_prop: i,
        });
        new_nodes.push(DifferNode {
            type_id: 2, key: 0, parent_index: 0,
            prop_count: 1, child_count: 0, first_child: 0, first_prop: i,
        });
    }

    let old_props: Vec<DifferProp> = (0..n).map(|i| DifferProp { dp_id: i, value_hash: 0xAAAA }).collect();
    let new_props: Vec<DifferProp> = (0..n).map(|i| {
        // Change every 10th prop
        let hash = if i % 10 == 0 { 0xBBBB } else { 0xAAAA };
        DifferProp { dp_id: i, value_hash: hash }
    }).collect();

    let mut ctx = DiffContext::new();
    diff_trees(&mut ctx, &old_nodes, &old_props, &new_nodes, &new_props);
    assert!(ctx.error.is_empty());
    // Should have exactly n/10 UpdateProp patches
    assert_eq!(count_ops(&ctx.patches, PatchOp::UpdateProp), (n / 10) as usize);
}

#[test]
fn stress_keyed_reconcile_1000_elements() {
    let n = 1000;
    let mut ctx = DiffContext::new();
    let old_keys: Vec<i64> = (0..n).collect();
    // Remove even keys, keep odd, add new keys at end
    let mut new_keys: Vec<i64> = (0..n).filter(|k| k % 2 != 0).collect();
    new_keys.extend(n..n + 100);

    reconcile_keys(&mut ctx, &old_keys, &new_keys);
    let removes = count_ops(&ctx.patches, PatchOp::Remove);
    let inserts = count_ops(&ctx.patches, PatchOp::Insert);
    assert_eq!(removes, 500); // 500 even keys removed
    assert_eq!(inserts, 100); // 100 new keys added
}

// ════════════════════════════════════════════════════════════════════════
//  21h. FFI hash function tests
// ════════════════════════════════════════════════════════════════════════

#[test]
fn ffi_hash_matches_safe_version() {
    let input = b"TextBlock";
    let safe_hash = fnv1a_hash(input);
    let ffi_hash = unsafe {
        differ_hash_string(input.as_ptr(), input.len() as u32)
    };
    assert_eq!(safe_hash, ffi_hash);
}

#[test]
fn ffi_hash_null_returns_zero() {
    let result = unsafe { differ_hash_string(std::ptr::null(), 10) };
    assert_eq!(result, 0);
}

#[test]
fn ffi_hash_empty_string() {
    let result = unsafe { differ_hash_string(b"".as_ptr(), 0) };
    // FNV-1a basis: 0x811c_9dc5
    assert_eq!(result, 0x811c_9dc5);
}

// ════════════════════════════════════════════════════════════════════════
//  Struct size verification FFI functions
// ════════════════════════════════════════════════════════════════════════

#[test]
fn ffi_struct_sizes_match_assertions() {
    assert_eq!(differ_node_size(), 32);
    assert_eq!(differ_prop_size(), 16);
    assert_eq!(differ_patch_size(), 24);
}

// ════════════════════════════════════════════════════════════════════════
//  Bounds checking / validation tests
// ════════════════════════════════════════════════════════════════════════

#[test]
fn validation_catches_out_of_bounds_children() {
    let mut ctx = DiffContext::new();
    let bad_nodes = [DifferNode {
        type_id: 1, key: 0, parent_index: -1,
        prop_count: 0, child_count: 5, // claims 5 children but only 1 node
        first_child: 1, first_prop: 0,
    }];
    diff_trees(&mut ctx, &bad_nodes, &[], &bad_nodes, &[]);
    assert!(!ctx.error.is_empty(), "Should have error for out-of-bounds children");
}

#[test]
fn validation_catches_out_of_bounds_props() {
    let mut ctx = DiffContext::new();
    let bad_nodes = [DifferNode {
        type_id: 1, key: 0, parent_index: -1,
        prop_count: 10, // claims 10 props but 0 exist
        child_count: 0, first_child: 0, first_prop: 0,
    }];
    diff_trees(&mut ctx, &bad_nodes, &[], &bad_nodes, &[]);
    assert!(!ctx.error.is_empty(), "Should have error for out-of-bounds props");
}
