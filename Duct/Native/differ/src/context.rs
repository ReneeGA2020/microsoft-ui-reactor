//! Reusable result buffer that amortizes allocation across diffs.
//! Patches are accumulated into a Vec that is cleared and reused between diffs.

use crate::types::DifferPatch;

/// Diff context that owns the result buffer and error state.
/// Reused across multiple diffs to amortize allocation.
///
/// NOT thread-safe. Must be used from a single thread (typically the UI dispatcher thread).
/// In debug builds, a thread ID check is performed to detect misuse.
pub struct DiffContext {
    pub patches: Vec<DifferPatch>,
    /// Error message from the last operation. Empty if no error.
    /// Set by catch_unwind panic handlers and input validation.
    pub error: String,
    #[cfg(debug_assertions)]
    owner_thread: std::thread::ThreadId,
}

impl DiffContext {
    pub fn new() -> Self {
        Self {
            patches: Vec::with_capacity(64),
            error: String::new(),
            #[cfg(debug_assertions)]
            owner_thread: std::thread::current().id(),
        }
    }

    pub fn reset(&mut self) {
        #[cfg(debug_assertions)]
        debug_assert_eq!(
            std::thread::current().id(),
            self.owner_thread,
            "DiffContext accessed from a different thread than the one that created it"
        );
        self.patches.clear();
        self.error.clear();
    }

    #[inline]
    pub fn emit(&mut self, patch: DifferPatch) {
        self.patches.push(patch);
    }
}

impl Default for DiffContext {
    fn default() -> Self {
        Self::new()
    }
}
