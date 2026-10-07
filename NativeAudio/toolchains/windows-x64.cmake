set(CMAKE_SYSTEM_NAME Windows)
set(CMAKE_SYSTEM_PROCESSOR AMD64)

set(CMAKE_C_COMPILER clang-cl-windows)
set(CMAKE_CXX_COMPILER clang-cl-windows)
set(CMAKE_C_COMPILER_TARGET x86_64-pc-windows-msvc)
set(CMAKE_CXX_COMPILER_TARGET x86_64-pc-windows-msvc)

set(CMAKE_RC_COMPILER llvm-rc)
set(CMAKE_MT llvm-mt)
set(CMAKE_LIB llvm-lib)
set(CMAKE_LINKER lld-link)

set(XWIN_ROOT /opt/winsysroot)
set(XWIN_ARCH x64)

# Keep try-compile checks from attempting to link runnable Windows binaries on
# the Linux host during configuration.
set(CMAKE_TRY_COMPILE_TARGET_TYPE STATIC_LIBRARY)

# Search libraries and headers in the xwin-provided sysroot instead of the
# Linux host.
set(CMAKE_FIND_ROOT_PATH /opt/winsysroot)
set(CMAKE_FIND_ROOT_PATH_MODE_PROGRAM NEVER)
set(CMAKE_FIND_ROOT_PATH_MODE_LIBRARY ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_INCLUDE ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_PACKAGE ONLY)

set(WINDOWS_SDK_LIBPATHS
    "/libpath:${XWIN_ROOT}/msvc/lib/${XWIN_ARCH} /libpath:${XWIN_ROOT}/sdk-lib/ucrt/${XWIN_ARCH} /libpath:${XWIN_ROOT}/sdk-lib/um/${XWIN_ARCH}")

set(CMAKE_EXE_LINKER_FLAGS_INIT "${WINDOWS_SDK_LIBPATHS}")
set(CMAKE_SHARED_LINKER_FLAGS_INIT "${WINDOWS_SDK_LIBPATHS}")
set(CMAKE_MODULE_LINKER_FLAGS_INIT "${WINDOWS_SDK_LIBPATHS}")

# When targeting the MSVC ABI from Linux, use the static runtime so the library
# does not require the VC++ redistributable DLLs.
set(CMAKE_MSVC_RUNTIME_LIBRARY
    "MultiThreaded$<$<CONFIG:Debug>:Debug>"
    CACHE STRING "MSVC runtime to use for Windows cross-builds" FORCE)

# Windows-specific defaults for shared libraries (.dll).
set(CMAKE_SHARED_LIBRARY_PREFIX "")
set(CMAKE_SHARED_LIBRARY_SUFFIX ".dll")
