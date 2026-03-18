//! # patch_differ
//!
//! Native view diffing engine for Duct.
//!
//! Performs tree diffing and keyed list reconciliation, exposing a C ABI
//! for consumption from C# via P/Invoke. All hot-path allocations use a
//! reusable arena that resets between diffs — zero GC pressure.

pub mod types;
pub mod arena;
pub mod diff;
pub mod reconcile;
pub mod ffi;

#[cfg(test)]
mod tests;
