//! # viewdiffer
//!
//! Native view diffing engine for Duct.
//!
//! Performs tree diffing and keyed list reconciliation, exposing a C ABI
//! for consumption from C# via P/Invoke. The result buffer is reused across
//! diffs to amortize allocation on the hot path.

pub mod types;
pub mod context;
pub mod diff;
pub mod reconcile;
pub mod ffi;
pub mod logging;

#[cfg(test)]
mod tests;
