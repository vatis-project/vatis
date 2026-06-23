#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
IMAGE="nativeaudio-build-windows"

if ! docker image inspect "${IMAGE}" >/dev/null 2>&1; then
    docker build -t "${IMAGE}" -f "${SCRIPT_DIR}/Dockerfile" "${SCRIPT_DIR}"
fi

docker run --rm \
  --user "$(id -u):$(id -g)" \
  -v "${REPO_DIR}":/src \
  -w /src/NativeAudio \
  "${IMAGE}" \
  bash -c '
    set -euo pipefail

    rm -rf build-windows-x64
    cmake -S . -B build-windows-x64 -G Ninja \
      -DCMAKE_TOOLCHAIN_FILE=toolchains/windows-x64.cmake \
      -DCMAKE_BUILD_TYPE=Release

    cmake --build build-windows-x64 -- -j$(nproc)

    echo "Built Windows library at build-windows-x64/NativeAudio.dll"
  '
