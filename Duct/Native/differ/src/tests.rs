//! Integration tests for the diffing engine.

use crate::arena::DiffContext;
use crate::diff::diff_trees;
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
    assert_eq!(ctx.patches[0].new_value_hash, 0);
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
    let h1 = crate::ffi::differ_hash_string_safe(b"TextBlock");
    let h2 = crate::ffi::differ_hash_string_safe(b"TextBlock");
    assert_eq!(h1, h2);
    assert_ne!(h1, 0);
}

#[test]
fn hash_string_different_inputs() {
    let h1 = crate::ffi::differ_hash_string_safe(b"Text");
    let h2 = crate::ffi::differ_hash_string_safe(b"Button");
    assert_ne!(h1, h2);
}
