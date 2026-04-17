use crate::enumerate;
use crate::types::FsResult;
use std::path::PathBuf;
use std::slice;

/// Decode a UTF-16 path from C# into a Rust `PathBuf`.
unsafe fn decode_path(path_ptr: *const u16, path_len: u32) -> PathBuf {
    let slice = unsafe { slice::from_raw_parts(path_ptr, path_len as usize) };
    PathBuf::from(String::from_utf16_lossy(slice))
}

/// Enumerate all immediate children (files and directories) of the given path.
///
/// # Safety
/// `path_ptr` must point to a valid UTF-16 buffer of `path_len` code units.
/// The returned `FsResult` must be freed via `reactorfs_free_result`.
#[no_mangle]
pub unsafe extern "C" fn reactorfs_enumerate(path_ptr: *const u16, path_len: u32) -> FsResult {
    let path = unsafe { decode_path(path_ptr, path_len) };
    enumerate::enumerate_directory(&path)
}

/// Enumerate only subdirectories of the given path (for tree lazy-loading).
///
/// # Safety
/// Same contract as `reactorfs_enumerate`.
#[no_mangle]
pub unsafe extern "C" fn reactorfs_enumerate_subdirs(path_ptr: *const u16, path_len: u32) -> FsResult {
    let path = unsafe { decode_path(path_ptr, path_len) };
    enumerate::enumerate_subdirs(&path)
}

/// Free a result previously returned by `reactorfs_enumerate` or `reactorfs_enumerate_subdirs`.
///
/// # Safety
/// `result` must be a value previously returned by one of the enumerate functions
/// and must not have been freed already.
#[no_mangle]
pub unsafe extern "C" fn reactorfs_free_result(result: FsResult) {
    if result.entries.is_null() || result.count == 0 {
        return;
    }

    let entries = unsafe {
        Vec::from_raw_parts(result.entries, result.count as usize, result.count as usize)
    };

    // Free each name string
    for entry in &entries {
        if !entry.name_ptr.is_null() && entry.name_len > 0 {
            let _ = unsafe {
                Vec::from_raw_parts(
                    entry.name_ptr as *mut u16,
                    entry.name_len as usize,
                    entry.name_len as usize,
                )
            };
        }
    }

    drop(entries);
}
