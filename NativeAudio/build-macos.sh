#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
IMAGE="ghcr.io/shepherdjerred/macos-cross-compiler:latest"

docker run --rm \
  --user "$(id -u):$(id -g)" \
  -v "${REPO_DIR}":/src \
  -w /src/NativeAudio \
  "${IMAGE}" \
  bash -c '
    set -euo pipefail

    SDK_PATH="/osxcross/SDK/MacOSX15.0.sdk"

    rm -rf build-macos-x86_64 build-macos-arm64 build-macos-universal

    echo "==> Building macOS x86_64..."
    cmake -S . -B build-macos-x86_64 \
      -DCMAKE_TOOLCHAIN_FILE=toolchains/macos-x86_64.cmake \
      -DCMAKE_BUILD_TYPE=Release \
      -DCMAKE_FRAMEWORK_PATH="$SDK_PATH/System/Library/Frameworks" \
      -DCMAKE_LIBRARY_PATH="$SDK_PATH/usr/lib"
    cmake --build build-macos-x86_64 -- -j$(nproc)

    echo "==> Building macOS arm64..."
    cmake -S . -B build-macos-arm64 \
      -DCMAKE_TOOLCHAIN_FILE=toolchains/macos-arm64.cmake \
      -DCMAKE_BUILD_TYPE=Release \
      -DCMAKE_FRAMEWORK_PATH="$SDK_PATH/System/Library/Frameworks" \
      -DCMAKE_LIBRARY_PATH="$SDK_PATH/usr/lib"
    cmake --build build-macos-arm64 -- -j$(nproc)

    echo "==> Creating universal dylib..."
    mkdir -p build-macos-universal ../vATIS.Desktop/Voice/Audio/Native/macos

    /cctools/bin/arm64-apple-darwin24-lipo -create \
      build-macos-x86_64/libNativeAudio.dylib \
      build-macos-arm64/libNativeAudio.dylib \
      -output build-macos-universal/libNativeAudio.dylib

    cp build-macos-universal/libNativeAudio.dylib ../vATIS.Desktop/Voice/Audio/Native/macos/libNativeAudio.dylib

    echo "Built macOS universal library at build-macos-universal/libNativeAudio.dylib"
    /cctools/bin/arm64-apple-darwin24-lipo -info build-macos-universal/libNativeAudio.dylib
  '
