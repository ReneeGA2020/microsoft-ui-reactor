//! Wire types shared between Rust internals and C FFI.
//! These are `#[repr(C)]` so they match the C# P/Invoke structs exactly.

/// A node in the flat view tree representation.
#[repr(C)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct DifferNode {
    /// Hash of the view type (e.g. FNV-1a of "TextBlock").
    pub type_id: u32,
    /// Stable identity for keyed list reconciliation. 0 = no key.
    pub key: i64,
    /// Index of parent node in the array. -1 = root.
    pub parent_index: i32,
    /// Number of properties on this node.
    pub prop_count: u16,
    /// Number of direct children.
    pub child_count: u16,
    /// Index of first child in the node array.
    pub first_child: u32,
    /// Index of first property in the prop array.
    pub first_prop: u32,
}

/// A property on a node, represented as (id, value_hash).
/// The C# side maps dp_id → DependencyProperty and value_hash → actual value.
#[repr(C)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct DifferProp {
    /// Hash of the DependencyProperty identifier.
    pub dp_id: u32,
    /// Hash of the property value. Only used for equality comparison.
    pub value_hash: u64,
}

/// Patch operation type.
#[repr(i32)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum PatchOp {
    None = 0,
    Insert = 1,
    Remove = 2,
    Move = 3,
    UpdateProp = 4,
    Replace = 5,
}

/// A single patch operation emitted by the differ.
#[repr(C)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct DifferPatch {
    pub op: PatchOp,
    /// Index in NEW tree (Insert/Replace) or OLD tree (Remove).
    pub node_index: u32,
    /// For Move: new position. For UpdateProp: prop index in new tree.
    pub target_index: u32,
    /// For UpdateProp: which property changed.
    pub dp_id: u32,
    /// For UpdateProp: new value hash.
    pub new_value_hash: u64,
}

impl DifferPatch {
    pub fn insert(node_index: u32, parent_index: u32) -> Self {
        Self { op: PatchOp::Insert, node_index, target_index: parent_index, dp_id: 0, new_value_hash: 0 }
    }

    pub fn remove(node_index: u32, parent_index: u32) -> Self {
        Self { op: PatchOp::Remove, node_index, target_index: parent_index, dp_id: 0, new_value_hash: 0 }
    }

    pub fn move_node(old_index: u32, new_position: u32) -> Self {
        Self { op: PatchOp::Move, node_index: old_index, target_index: new_position, dp_id: 0, new_value_hash: 0 }
    }

    pub fn update_prop(node_index: u32, prop_index: u32, dp_id: u32, new_value_hash: u64) -> Self {
        Self { op: PatchOp::UpdateProp, node_index, target_index: prop_index, dp_id, new_value_hash }
    }

    pub fn replace(new_index: u32, old_index: u32) -> Self {
        Self { op: PatchOp::Replace, node_index: new_index, target_index: old_index, dp_id: 0, new_value_hash: 0 }
    }
}

// Compile-time struct layout assertions.
// These must match the C# StructLayout(LayoutKind.Sequential) definitions in ViewDiffer.cs.
const _: () = {
    assert!(std::mem::size_of::<DifferNode>() == 32);
    assert!(std::mem::align_of::<DifferNode>() == 8);
    assert!(std::mem::size_of::<DifferProp>() == 16);
    assert!(std::mem::align_of::<DifferProp>() == 8);
    assert!(std::mem::size_of::<DifferPatch>() == 24);
    assert!(std::mem::align_of::<DifferPatch>() == 8);
    assert!(std::mem::size_of::<PatchOp>() == 4);
};
