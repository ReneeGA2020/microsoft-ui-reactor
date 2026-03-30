#pragma once

// Prevent Windows.h from defining min/max macros (use std::min/std::max instead)
#ifndef NOMINMAX
#define NOMINMAX
#endif

// Windows SDK
#include <windows.h>
#include <unknwn.h>
#include <restrictederrorinfo.h>
#include <hstring.h>

// WinRT core
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>

// C++ standard library
#include <string>
#include <vector>
#include <variant>
#include <optional>
#include <memory>
#include <functional>
#include <any>
#include <format>
#include <unordered_map>
#include <algorithm>
#include <cassert>
