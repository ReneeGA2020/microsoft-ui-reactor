//! Reusable result buffer for zero-allocation diffing.
//! Patches are accumulated into a Vec that is reused across diffs.

use crate::types::DifferPatch;

/// Diff context that owns the result buffer.
/// Reused across multiple diffs to amortize allocation.
pub struct DiffContext {
    pub patches: Vec<DifferPatch>,
    pub error: String,
}

impl DiffContext {
    pub fn new() -> Self {
        Self {
            patches: Vec::with_capacity(64),
            error: String::new(),
        }
    }

    pub fn reset(&mut self) {
        self.patches.clear();
        self.error.clear();
    }

    #[inline]
    pub fn emit(&mut self, patch: DifferPatch) {
        self.patches.push(patch);
    }
}
