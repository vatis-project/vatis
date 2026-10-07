#!/usr/bin/env bash
# Cross-compiles NativeAudio.dll (win-x64, static CRT) with clang-cl + xwin inside Docker.
set -euo pipefail
cd "$(dirname "$0")"

IMAGE="${VATIS_WIN_BUILD_IMAGE:-vatis-win-build}"

command -v docker >/dev/null || { echo "ERROR: Docker is required for the Windows build." >&2; exit 1; }

if ! docker image inspect "$IMAGE" >/dev/null 2>&1; then
    echo "==> Building Windows toolchain image: $IMAGE"
    docker build -t "$IMAGE" -f Dockerfile.windows .
fi

docker run --rm \
  --user "$(id -u):$(id -g)" \
  -v "$(pwd)":/src \
  -w /src \
  "$IMAGE" \
  bash -c '
    set -euo pipefail
    echo "==> Building Windows x64..."
    rm -rf build-windows-x64
    cmake -S . -B build-windows-x64 -G Ninja \
      -DCMAKE_TOOLCHAIN_FILE=toolchains/windows-x64.cmake \
      -DCMAKE_BUILD_TYPE=Release \
      -DNATIVE_DLL_OUTPUT_DIR=/src/build-windows-x64/out
    cmake --build build-windows-x64 -- -j$(nproc)
  '

mkdir -p ../vATIS.Desktop/Voice/Audio/Native/win
cp build-windows-x64/out/win/NativeAudio.dll ../vATIS.Desktop/Voice/Audio/Native/win/
echo "==> Copied to vATIS.Desktop/Voice/Audio/Native/win/NativeAudio.dll"
