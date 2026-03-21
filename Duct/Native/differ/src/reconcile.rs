//! Keyed list reconciliation.
//!
//! Given old and new key arrays, computes minimal insert/remove/move ops.
//! Uses LIS (Longest Increasing Subsequence) for move minimization,
//! similar to Vue 3's algorithm.

use crate::context::DiffContext;
use crate::types::DifferPatch;
use std::collections::HashMap;

/// Compute the Longest Increasing Subsequence of `arr`.
/// Returns indices into `arr` that form the LIS.
/// Items NOT in the LIS need to be moved.
fn compute_lis(arr: &[u32]) -> Vec<usize> {
    if arr.is_empty() {
        return Vec::new();
    }

    let n = arr.len();
    let mut tails: Vec<u32> = Vec::with_capacity(n);
    let mut tail_indices: Vec<usize> = Vec::with_capacity(n);
    let mut predecessors: Vec<Option<usize>> = vec![None; n];

    for i in 0..n {
        let val = arr[i];
        if val == u32::MAX {
            continue; // unmapped entry
        }

        // Binary search for insertion position
        let pos = tails.partition_point(|&t| t < val);

        if pos == tails.len() {
            tails.push(val);
            tail_indices.push(i);
        } else {
            tails[pos] = val;
            tail_indices[pos] = i;
        }

        predecessors[i] = if pos > 0 {
            Some(tail_indices[pos - 1])
        } else {
            None
        };
    }

    // Backtrack to collect LIS indices
    if tails.is_empty() {
        return Vec::new();
    }
    let mut result = vec![0usize; tails.len()];
    let mut k = tail_indices[tails.len() - 1];
    for i in (0..tails.len()).rev() {
        result[i] = k;
        if i > 0 {
            k = predecessors[k].expect("LIS predecessor chain broken");
        }
    }

    result
}

/// Reconcile two keyed lists, emitting minimal patch operations.
pub fn reconcile_keys(ctx: &mut DiffContext, old_keys: &[i64], new_keys: &[i64]) {
    if old_keys.is_empty() && new_keys.is_empty() {
        return;
    }

    // All new
    if old_keys.is_empty() {
        for i in 0..new_keys.len() {
            ctx.emit(DifferPatch::insert(i as u32, 0));
        }
        return;
    }

    // All removed
    if new_keys.is_empty() {
        for i in 0..old_keys.len() {
            ctx.emit(DifferPatch::remove(i as u32, 0));
        }
        return;
    }

    // Build old key → index map, detecting duplicates
    let mut old_map: HashMap<i64, u32> = HashMap::with_capacity(old_keys.len());
    for (i, &k) in old_keys.iter().enumerate() {
        if let Some(existing) = old_map.insert(k, i as u32) {
            log::warn!(
                "Duplicate key {} in old list at indices {} and {} — earlier entry will be ignored",
                k, existing, i
            );
            debug_assert!(
                false,
                "Duplicate key {k} in old list at indices {existing} and {i}"
            );
        }
    }

    // Map new keys to old indices
    let mut new_to_old = vec![u32::MAX; new_keys.len()];
    let mut old_matched = vec![false; old_keys.len()];

    for (ni, &new_key) in new_keys.iter().enumerate() {
        if let Some(&old_idx) = old_map.get(&new_key) {
            new_to_old[ni] = old_idx;
            old_matched[old_idx as usize] = true;
        }
    }

    // Emit REMOVE for unmatched old keys
    for (i, &matched) in old_matched.iter().enumerate() {
        if !matched {
            ctx.emit(DifferPatch::remove(i as u32, 0));
        }
    }

    // Compute LIS on new_to_old for move minimization
    let lis_indices = compute_lis(&new_to_old);
    let mut in_lis = vec![false; new_keys.len()];
    for &idx in &lis_indices {
        in_lis[idx] = true;
    }

    // Emit INSERT for new keys, MOVE for existing keys not in LIS
    for i in 0..new_keys.len() {
        if new_to_old[i] == u32::MAX {
            ctx.emit(DifferPatch::insert(i as u32, 0));
        } else if !in_lis[i] {
            ctx.emit(DifferPatch::move_node(new_to_old[i], i as u32));
        }
        // else: in LIS — stays in place, no op needed
    }
}

#[cfg(test)]
mod lis_tests {
    use super::*;

    #[test]
    fn lis_empty() {
        assert!(compute_lis(&[]).is_empty());
    }

    #[test]
    fn lis_sorted() {
        let result = compute_lis(&[0, 1, 2, 3]);
        assert_eq!(result.len(), 4);
        assert_eq!(result, vec![0, 1, 2, 3]);
    }

    #[test]
    fn lis_reversed() {
        let result = compute_lis(&[3, 2, 1, 0]);
        assert_eq!(result.len(), 1);
    }

    #[test]
    fn lis_with_unmapped() {
        let result = compute_lis(&[0, u32::MAX, 1, 2]);
        assert_eq!(result.len(), 3);
        assert_eq!(result, vec![0, 2, 3]);
    }

    #[test]
    fn lis_correctness_mixed() {
        // Input: [2, 0, 3, 1, 4] → LIS values could be [0, 1, 4] or [0, 3, 4] etc.
        // We verify the result is a valid increasing subsequence of the input.
        let input = [2u32, 0, 3, 1, 4];
        let result = compute_lis(&input);
        // Verify it's actually increasing
        for w in result.windows(2) {
            assert!(input[w[0]] < input[w[1]],
                "LIS not increasing: input[{}]={} >= input[{}]={}",
                w[0], input[w[0]], w[1], input[w[1]]);
        }
        // LIS of [2,0,3,1,4] has length 3 (e.g., [0,1,4] or [0,3,4])
        assert_eq!(result.len(), 3);
    }

    #[test]
    fn lis_single_element() {
        let result = compute_lis(&[5]);
        assert_eq!(result, vec![0]);
    }

    #[test]
    fn lis_all_same() {
        // All same value — LIS should be length 1
        let result = compute_lis(&[3, 3, 3]);
        assert_eq!(result.len(), 1);
    }

    #[test]
    fn lis_all_unmapped() {
        let result = compute_lis(&[u32::MAX, u32::MAX, u32::MAX]);
        assert!(result.is_empty());
    }
}
