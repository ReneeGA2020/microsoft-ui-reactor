use crate::types::{FsEntry, FsResult};
use std::path::Path;
use std::time::SystemTime;

/// Converts a `SystemTime` to Windows FILETIME ticks (100-ns intervals since 1601-01-01).
fn to_filetime_ticks(t: SystemTime) -> u64 {
    // Offset between Unix epoch (1970) and Windows epoch (1601) in 100-ns ticks.
    const EPOCH_OFFSET: u64 = 116_444_736_000_000_000;
    match t.duration_since(SystemTime::UNIX_EPOCH) {
        Ok(d) => d.as_nanos() as u64 / 100 + EPOCH_OFFSET,
        Err(_) => 0,
    }
}

/// Enumerate immediate children of `path` using jwalk for parallel metadata.
pub fn enumerate_directory(path: &Path) -> FsResult {
    let walker = jwalk::WalkDir::new(path)
        .max_depth(1)
        .skip_hidden(false);

    let mut entries: Vec<FsEntry> = Vec::new();

    for entry in walker {
        let Ok(entry) = entry else { continue };

        // Skip the root directory itself (depth 0)
        if entry.depth == 0 {
            continue;
        }

        let file_name = entry.file_name().to_string_lossy().to_string();
        let name_utf16: Vec<u16> = file_name.encode_utf16().collect();

        let is_dir = entry.file_type().is_dir();

        let (size, modified_ticks) = match std::fs::metadata(entry.path()) {
            Ok(meta) => {
                let size = if is_dir { 0 } else { meta.len() };
                let modified = meta
                    .modified()
                    .map(|t| to_filetime_ticks(t))
                    .unwrap_or(0);
                (size, modified)
            }
            Err(_) => (0, 0),
        };

        // Assume all directories might have children — avoids an extra read_dir per dir.
        // The tree shows an expand arrow; expanding an empty dir simply shows nothing.
        let has_children = is_dir;

        let name_ptr = name_utf16.as_ptr();
        let name_len = name_utf16.len() as u32;
        std::mem::forget(name_utf16); // Caller frees via ductfs_free_result

        entries.push(FsEntry {
            name_ptr,
            name_len,
            size,
            modified_ticks,
            is_directory: is_dir as u8,
            has_children: has_children as u8,
        });
    }

    let count = entries.len() as u32;
    let ptr = entries.as_mut_ptr();
    std::mem::forget(entries); // Caller frees via ductfs_free_result

    FsResult {
        entries: ptr,
        count,
    }
}

/// Enumerate only subdirectories of `path` (for tree lazy-loading).
pub fn enumerate_subdirs(path: &Path) -> FsResult {
    let walker = jwalk::WalkDir::new(path)
        .max_depth(1)
        .skip_hidden(false);

    let mut entries: Vec<FsEntry> = Vec::new();

    for entry in walker {
        let Ok(entry) = entry else { continue };
        if entry.depth == 0 {
            continue;
        }

        let is_dir = entry.file_type().is_dir();
        if !is_dir {
            continue;
        }

        let file_name = entry.file_name().to_string_lossy().to_string();
        let name_utf16: Vec<u16> = file_name.encode_utf16().collect();

        let has_children = true; // assume children; expand reveals actual content

        let name_ptr = name_utf16.as_ptr();
        let name_len = name_utf16.len() as u32;
        std::mem::forget(name_utf16);

        entries.push(FsEntry {
            name_ptr,
            name_len,
            size: 0,
            modified_ticks: 0,
            is_directory: 1,
            has_children: has_children as u8,
        });
    }

    let count = entries.len() as u32;
    let ptr = entries.as_mut_ptr();
    std::mem::forget(entries);

    FsResult {
        entries: ptr,
        count,
    }
}
