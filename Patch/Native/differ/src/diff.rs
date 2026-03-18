//! Tree diffing engine.
//!
//! Compares old and new view trees represented as flat node arrays.
//! Generates minimal patch operations: INSERT, REMOVE, MOVE, UPDATE_PROP, REPLACE.

use crate::arena::DiffContext;
use crate::reconcile::reconcile_keys;
use crate::types::*;

/// Diff properties between old and new nodes, emitting UPDATE_PROP patches.
fn diff_props(
    ctx: &mut DiffContext,
    new_node_index: u32,
    old_props: &[DifferProp],
    old_first: u32,
    old_count: u16,
    new_props: &[DifferProp],
    new_first: u32,
    new_count: u16,
) {
    // Check each new property against old
    for ni in 0..new_count {
        let np = &new_props[(new_first + ni as u32) as usize];
        let mut found = false;

        for oi in 0..old_count {
            let op = &old_props[(old_first + oi as u32) as usize];
            if op.dp_id == np.dp_id {
                found = true;
                if op.value_hash != np.value_hash {
                    ctx.emit(DifferPatch::update_prop(
                        new_node_index,
                        new_first + ni as u32,
                        np.dp_id,
                        np.value_hash,
                    ));
                }
                break;
            }
        }

        // New property that didn't exist before
        if !found {
            ctx.emit(DifferPatch::update_prop(
                new_node_index,
                new_first + ni as u32,
                np.dp_id,
                np.value_hash,
            ));
        }
    }

    // Properties removed (in old but not in new)
    for oi in 0..old_count {
        let op = &old_props[(old_first + oi as u32) as usize];
        let still_exists = (0..new_count).any(|ni| {
            new_props[(new_first + ni as u32) as usize].dp_id == op.dp_id
        });
        if !still_exists {
            ctx.emit(DifferPatch::update_prop(new_node_index, 0, op.dp_id, 0));
        }
    }
}

/// Recursive tree diff.
fn diff_subtree(
    ctx: &mut DiffContext,
    old_nodes: &[DifferNode],
    old_props: &[DifferProp],
    new_nodes: &[DifferNode],
    new_props: &[DifferProp],
    old_index: u32,
    new_index: u32,
) {
    let old_node = &old_nodes[old_index as usize];
    let new_node = &new_nodes[new_index as usize];

    // Same type + same key → update in place
    let can_update = old_node.type_id == new_node.type_id && old_node.key == new_node.key;

    if !can_update {
        ctx.emit(DifferPatch::replace(new_index, old_index));
        return;
    }

    // Diff properties
    diff_props(
        ctx,
        new_index,
        old_props,
        old_node.first_prop,
        old_node.prop_count,
        new_props,
        new_node.first_prop,
        new_node.prop_count,
    );

    // Leaf nodes — done
    if old_node.child_count == 0 && new_node.child_count == 0 {
        return;
    }

    // Check if any children have keys
    let has_keys = (0..new_node.child_count).any(|i| {
        new_nodes[(new_node.first_child + i as u32) as usize].key != 0
    }) || (0..old_node.child_count).any(|i| {
        old_nodes[(old_node.first_child + i as u32) as usize].key != 0
    });

    if has_keys {
        reconcile_keyed_children(ctx, old_nodes, old_props, new_nodes, new_props, old_node, new_node);
    } else {
        // Positional reconciliation
        let common = old_node.child_count.min(new_node.child_count);

        for i in 0..common {
            diff_subtree(
                ctx,
                old_nodes,
                old_props,
                new_nodes,
                new_props,
                old_node.first_child + i as u32,
                new_node.first_child + i as u32,
            );
        }

        // New children → INSERT
        for i in common..new_node.child_count {
            ctx.emit(DifferPatch::insert(
                new_node.first_child + i as u32,
                new_index,
            ));
        }

        // Removed children → REMOVE
        for i in common..old_node.child_count {
            ctx.emit(DifferPatch::remove(
                old_node.first_child + i as u32,
                new_index,
            ));
        }
    }
}

/// Keyed children reconciliation using prefix/suffix matching + LIS.
fn reconcile_keyed_children(
    ctx: &mut DiffContext,
    old_nodes: &[DifferNode],
    old_props: &[DifferProp],
    new_nodes: &[DifferNode],
    new_props: &[DifferProp],
    old_parent: &DifferNode,
    new_parent: &DifferNode,
) {
    let old_start = old_parent.first_child;
    let new_start = new_parent.first_child;
    let old_len = old_parent.child_count as u32;
    let new_len = new_parent.child_count as u32;

    // Step 1: Common prefix
    let mut prefix: u32 = 0;
    while prefix < old_len && prefix < new_len {
        let o = &old_nodes[(old_start + prefix) as usize];
        let n = &new_nodes[(new_start + prefix) as usize];
        if o.type_id != n.type_id || o.key != n.key {
            break;
        }
        diff_props(
            ctx,
            new_start + prefix,
            old_props, o.first_prop, o.prop_count,
            new_props, n.first_prop, n.prop_count,
        );
        prefix += 1;
    }

    // Step 2: Common suffix
    let mut suffix: u32 = 0;
    while suffix < (old_len - prefix) && suffix < (new_len - prefix) {
        let o = &old_nodes[(old_start + old_len - 1 - suffix) as usize];
        let n = &new_nodes[(new_start + new_len - 1 - suffix) as usize];
        if o.type_id != n.type_id || o.key != n.key {
            break;
        }
        diff_props(
            ctx,
            new_start + new_len - 1 - suffix,
            old_props, o.first_prop, o.prop_count,
            new_props, n.first_prop, n.prop_count,
        );
        suffix += 1;
    }

    let old_mid_start = prefix;
    let old_mid_end = old_len - suffix;
    let new_mid_start = prefix;
    let new_mid_end = new_len - suffix;
    let old_mid_len = old_mid_end - old_mid_start;
    let new_mid_len = new_mid_end - new_mid_start;

    if old_mid_len == 0 && new_mid_len == 0 {
        return;
    }

    // Only inserts
    if old_mid_len == 0 {
        for i in new_mid_start..new_mid_end {
            ctx.emit(DifferPatch::insert(new_start + i, new_parent.first_child));
        }
        return;
    }

    // Only removes
    if new_mid_len == 0 {
        for i in old_mid_start..old_mid_end {
            ctx.emit(DifferPatch::remove(old_start + i, new_parent.first_child));
        }
        return;
    }

    // Step 3: Middle section — delegate to key reconciliation
    let old_mid_keys: Vec<i64> = (old_mid_start..old_mid_end)
        .map(|i| old_nodes[(old_start + i) as usize].key)
        .collect();
    let new_mid_keys: Vec<i64> = (new_mid_start..new_mid_end)
        .map(|i| new_nodes[(new_start + i) as usize].key)
        .collect();

    let mut mid_ctx = DiffContext::new();
    reconcile_keys(&mut mid_ctx, &old_mid_keys, &new_mid_keys);

    // Remap indices from mid-relative to absolute and diff props for matched nodes
    for patch in &mid_ctx.patches {
        match patch.op {
            PatchOp::Insert => {
                ctx.emit(DifferPatch::insert(
                    new_start + new_mid_start + patch.node_index,
                    new_parent.first_child,
                ));
            }
            PatchOp::Remove => {
                ctx.emit(DifferPatch::remove(
                    old_start + old_mid_start + patch.node_index,
                    new_parent.first_child,
                ));
            }
            PatchOp::Move => {
                let abs_old = old_start + old_mid_start + patch.node_index;
                let abs_new = new_start + new_mid_start + patch.target_index;
                ctx.emit(DifferPatch::move_node(abs_old, abs_new));
                // Also diff props of moved node
                let o = &old_nodes[abs_old as usize];
                let n = &new_nodes[abs_new as usize];
                diff_props(ctx, abs_new, old_props, o.first_prop, o.prop_count,
                          new_props, n.first_prop, n.prop_count);
            }
            _ => {}
        }
    }

    // Diff props for nodes that stayed in place (LIS nodes — matched but not moved)
    let moved_or_inserted: std::collections::HashSet<u32> = mid_ctx
        .patches
        .iter()
        .filter(|p| p.op == PatchOp::Move || p.op == PatchOp::Insert)
        .map(|p| p.target_index)
        .collect();
    let removed: std::collections::HashSet<u32> = mid_ctx
        .patches
        .iter()
        .filter(|p| p.op == PatchOp::Remove)
        .map(|p| p.node_index)
        .collect();

    let old_key_map: std::collections::HashMap<i64, u32> = old_mid_keys
        .iter()
        .enumerate()
        .map(|(i, &k)| (k, i as u32))
        .collect();

    for ni in 0..new_mid_len {
        if moved_or_inserted.contains(&ni) {
            continue;
        }
        let abs_new = (new_start + new_mid_start + ni) as usize;
        let new_key = new_nodes[abs_new].key;
        if let Some(&oi) = old_key_map.get(&new_key) {
            if !removed.contains(&oi) {
                let abs_old = (old_start + old_mid_start + oi) as usize;
                diff_props(
                    ctx,
                    abs_new as u32,
                    old_props, old_nodes[abs_old].first_prop, old_nodes[abs_old].prop_count,
                    new_props, new_nodes[abs_new].first_prop, new_nodes[abs_new].prop_count,
                );
            }
        }
    }
}

/// Top-level tree diff entry point.
pub fn diff_trees(
    ctx: &mut DiffContext,
    old_nodes: &[DifferNode],
    old_props: &[DifferProp],
    new_nodes: &[DifferNode],
    new_props: &[DifferProp],
) {
    ctx.reset();

    if old_nodes.is_empty() && new_nodes.is_empty() {
        return;
    }

    if old_nodes.is_empty() {
        for i in 0..new_nodes.len() {
            ctx.emit(DifferPatch::insert(i as u32, 0));
        }
        return;
    }

    if new_nodes.is_empty() {
        for i in 0..old_nodes.len() {
            ctx.emit(DifferPatch::remove(i as u32, 0));
        }
        return;
    }

    diff_subtree(ctx, old_nodes, old_props, new_nodes, new_props, 0, 0);
}
