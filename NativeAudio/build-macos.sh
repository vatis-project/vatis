#!/usr/bin/env bash
# Cross-compiles the universal macOS libNativeAudio.dylib inside the osxcross Docker image.
set -euo pipefail
cd "$(dirname "$0")"

IMAGE="ghcr.io/shepherdjerred/macos-cross-compiler:latest"

docker run --rm \
  --user "$(id -u):$(id -g)" \
  -e TMPDIR=/src/build-macos-tmp \
  -v "$(pwd)":/src \
  -w /src \
  "$IMAGE" \
  bash -c '
    set -euo pipefail
    mkdir -p "$TMPDIR"
    SDK_PATH="/osxcross/SDK/MacOSX15.0.sdk"

    for ARCH in x86_64 arm64; do
      echo "==> Building $ARCH..."
      cmake -S . -B build-macos-$ARCH \
        -DCMAKE_TOOLCHAIN_FILE=toolchains/macos-$ARCH.cmake \
        -DCMAKE_BUILD_TYPE=Release \
        -DCMAKE_OSX_ARCHITECTURES=$ARCH \
        -DCMAKE_FRAMEWORK_PATH="$SDK_PATH/System/Library/Frameworks" \
        -DCMAKE_LIBRARY_PATH="$SDK_PATH/usr/lib" \
        -DNATIVE_DLL_OUTPUT_DIR=/src/build-macos-$ARCH/out
      cmake --build build-macos-$ARCH -- -j$(nproc)
    done

    echo "==> Merging with lipo..."
    mkdir -p build-macos-universal
    /cctools/bin/arm64-apple-darwin24-lipo -create \
      build-macos-x86_64/libNativeAudio.dylib \
      build-macos-arm64/libNativeAudio.dylib \
      -output build-macos-universal/libNativeAudio.dylib
    /cctools/bin/arm64-apple-darwin24-lipo -info build-macos-universal/libNativeAudio.dylib
  '

mkdir -p ../vATIS.Desktop/Voice/Audio/Native/macos
cp build-macos-universal/libNativeAudio.dylib ../vATIS.Desktop/Voice/Audio/Native/macos/
rm -rf build-macos-tmp
echo "==> Copied to vATIS.Desktop/Voice/Audio/Native/macos/libNativeAudio.dylib"
