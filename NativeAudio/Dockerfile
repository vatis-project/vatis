ARG UBUNTU_VERSION=24.04
FROM ubuntu:${UBUNTU_VERSION}
SHELL ["/bin/bash", "-o", "pipefail", "-c"]

ARG ZIG_VERSION=0.14.0
ARG CMAKE_VERSION=3.28.6
ARG USERNAME=app

RUN apt-get update \
  && apt-get install -y --no-install-recommends \
  ninja-build sudo bash coreutils ca-certificates curl xz-utils \
  llvm-18 \
  && apt-get clean \
  && rm -rf /var/lib/apt/lists/*

RUN curl -sSL https://cmake.org/files/v${CMAKE_VERSION%.*}/cmake-${CMAKE_VERSION}-linux-x86_64.tar.gz \
  | tar -xz -C /usr/local/ --strip-components=1

RUN curl -sSL https://ziglang.org/download/${ZIG_VERSION}/zig-linux-x86_64-${ZIG_VERSION}.tar.xz \
  | tar -xJ -C /opt/ \
  && ln -s /opt/zig-linux-x86_64-${ZIG_VERSION} /opt/zig \
  && ln -s /opt/zig/zig /usr/local/bin/zig

RUN set -e \
  && for pair in \
  "zig-cc-windows:x86_64-windows-gnu" \
  "zig-cxx-windows:x86_64-windows-gnu"; \
  do \
  name="${pair%%:*}"; \
  target="${pair#*:}"; \
  case "$name" in \
  *cxx*) mode="c++" ;; \
  *) mode="cc" ;; \
  esac; \
  printf '#!/bin/sh\nexec zig %s -target %s "$@"\n' "$mode" "$target" > /usr/local/bin/"$name"; \
  chmod +x /usr/local/bin/"$name"; \
  done

RUN userdel -r ubuntu 2>/dev/null || true \
  && useradd -m -u 1000 ${USERNAME} \
  && usermod -aG sudo ${USERNAME} \
  && echo '%sudo ALL=(ALL) NOPASSWD:ALL' >> /etc/sudoers

VOLUME /src
USER ${USERNAME}
WORKDIR /src
