/// Wire-format entry passed across the FFI boundary.
/// All strings are UTF-16 so C# can read them with zero transcoding.
#[repr(C)]
pub struct FsEntry {
    /// Pointer to a UTF-16 encoded name (not null-terminated).
    pub name_ptr: *const u16,
    /// Length in u16 code units.
    pub name_len: u32,
    /// File size in bytes (0 for directories).
    pub size: u64,
    /// Last modified time as Windows FILETIME ticks (100-ns intervals since 1601-01-01).
    pub modified_ticks: u64,
    /// 1 if directory, 0 if file.
    pub is_directory: u8,
    /// 1 if directory has at least one child subdirectory.
    pub has_children: u8,
}

/// Result buffer returned to C#. Caller must free via `ductfs_free_result`.
#[repr(C)]
pub struct FsResult {
    pub entries: *mut FsEntry,
    pub count: u32,
}
