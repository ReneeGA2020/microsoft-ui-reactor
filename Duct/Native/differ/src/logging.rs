//! FFI-compatible logging via the `log` crate.
//!
//! The C# host registers a callback via `differ_set_log_callback`.
//! Rust code uses standard `log` macros (`error!`, `warn!`, etc.)
//! which are routed to the callback.

use std::sync::OnceLock;

type LogCallback = extern "C" fn(level: i32, msg: *const u8, len: u32);
static LOG_CALLBACK: OnceLock<LogCallback> = OnceLock::new();

struct FfiLogger;

impl log::Log for FfiLogger {
    fn enabled(&self, _: &log::Metadata) -> bool {
        true
    }

    fn log(&self, record: &log::Record) {
        if let Some(cb) = LOG_CALLBACK.get() {
            let msg = format!("{}", record.args());
            cb(record.level() as i32, msg.as_ptr(), msg.len() as u32);
        }
    }

    fn flush(&self) {}
}

static LOGGER: FfiLogger = FfiLogger;

/// Register a C callback to receive log messages from the Rust differ.
///
/// `level` values: 1=Error, 2=Warn, 3=Info, 4=Debug, 5=Trace.
/// `msg` is a UTF-8 byte pointer with `len` bytes (NOT null-terminated).
#[no_mangle]
pub extern "C" fn differ_set_log_callback(cb: LogCallback) {
    LOG_CALLBACK.set(cb).ok();
    log::set_logger(&LOGGER).ok();
    log::set_max_level(log::LevelFilter::Trace);
}
