#!/usr/bin/env bash
# Builds vATIS for linux-x64, win-x64 and macOS (universal) from a Linux dev machine and packs
# them with Velopack.
#
# Usage: scripts/build.sh <version> [--platform linux|win|macos]... [--native] [--skip-codesign]
#
#   --platform  Limit to one or more platforms (default: all three).
#   --native    Rebuild NativeAudio first: Linux .so with cmake, Windows .dll and macOS .dylib
#               via Docker (clang-cl + xwin, osxcross).
#   --skip-codesign  Don't sign or notarize anything.
#
# Output goes to publish/release/{linux,windows,macos}. Those directories accumulate releases:
# Velopack builds delta packages against the previous release found there. If a directory is empty,
# the previous release is downloaded from VATIS_RELEASES_URL (default https://vatis.app/downloads,
# with linux/, windows/, macos/ below it). Copy the new files to your server yourself, with the
# releases.*.json feed last.
#
# Requirements: dotnet SDK, docker (macOS universal binary via lipo), curl, python3, zip/unzip,
#               cmake (--native), java (Windows signing).
# vpk is installed on demand into publish/tools, pinned to the Velopack version the app uses.
# Linux and macOS packages are built with a patched vpk (scripts/velopack/*.patch): stock vpk only packs for
# macOS on a Mac, and the Linux AppImage is built with pkgforge's appimagetool (no libfuse.so.2 dependency).
#
# Code signing (skipped with --skip-codesign, or per platform when credentials are unset):
#   macOS    rcodesign (auto-downloaded to publish/tools). Signs, notarizes and staples the .app.
#              APPLE_CERT_P12        Developer ID Application .p12 file
#              APPLE_CERT_PASSWORD   its password
#              APPLE_NOTARY_API_KEY  App Store Connect API key JSON (issuer_id, key_id, private_key);
#                                    without it the app is signed but not notarized
#   Windows  jsign + Azure Trusted Signing (jar auto-downloaded to publish/tools; needs java, curl).
#              AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET,
#              AZURE_TRUSTED_SIGNING_ENDPOINT, AZURE_TRUSTED_SIGNING_ACCOUNT,
#              AZURE_TRUSTED_SIGNING_CERT_PROFILE
#   Copy scripts/env-setup.sh.example to scripts/env-setup.sh, fill it in and source it first.
#
# macOS output is a signed, notarized vATIS-<version>-macos.zip plus the Velopack update packages. There is no DMG.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

PROJECT="./vATIS.Desktop/vATIS.Desktop.csproj"
PACK_ID="org.vatsim.vatis"
APP_NAME="vATIS"
AUTHORS="Justin Shannon"
LIPO_IMAGE="ghcr.io/shepherdjerred/macos-cross-compiler:latest"
VELOPACK_VERSION="1.2.161" # keep in sync with the Velopack package referenced by vATIS.Desktop.csproj

info() { printf '\e[33m==> %s\e[0m\n' "$*"; }
err() { printf '\e[31m%s\e[0m\n' "$*" >&2; }
die() { err "$*"; exit 1; }

usage() { sed -n '2,/^set -euo/p' "${BASH_SOURCE[0]}" | sed '$d' | sed 's/^# \{0,1\}//'; exit "${1:-1}"; }

[ $# -ge 1 ] || usage
case "$1" in -h|--help) usage 0 ;; esac
case "$1" in -*) die "Expected a version first, got '$1' (usage: $0 <version> [options])" ;; esac
VERSION="$1"; shift
VATIS_RELEASES_URL="${VATIS_RELEASES_URL:-https://vatis.app/downloads}"

PLATFORMS=()
BUILD_NATIVE=false
SKIP_CODESIGN=false
while [ $# -gt 0 ]; do
    case "$1" in
        --platform) [ $# -ge 2 ] || die "--platform needs a value"; PLATFORMS+=("$2"); shift 2 ;;
        --native) BUILD_NATIVE=true; shift ;;
        --skip-codesign) SKIP_CODESIGN=true; shift ;;
        -h|--help) usage 0 ;;
        *) die "Unknown option: $1" ;;
    esac
done
[ ${#PLATFORMS[@]} -gt 0 ] || PLATFORMS=(linux win macos)
for p in "${PLATFORMS[@]}"; do
    case "$p" in linux|win|macos) ;; *) die "Unknown platform: $p" ;; esac
done

BUILD_DIR="$ROOT/artifacts/$VERSION"   # dotnet publish output
OUT_DIR="$ROOT/publish/release"        # velopack output, one dir per platform
TOOLS_DIR="$ROOT/publish/tools"
NATIVE_DIR="$ROOT/vATIS.Desktop/Voice/Audio/Native"
mkdir -p "$BUILD_DIR" "$OUT_DIR" "$TOOLS_DIR"

command -v dotnet >/dev/null || die "dotnet not found"
has() { local p; for p in "${PLATFORMS[@]}"; do [ "$p" = "$1" ] && return 0; done; return 1; }

publish() { # <rid>
    local rid="$1"
    info "dotnet publish $rid"
    dotnet publish -c Release -r "$rid" -o "$BUILD_DIR/$rid" \
        -p:UseAppHost=true -p:NoWarn=IL3000 -p:Version="$VERSION" "$PROJECT"
}

# Extracts this version's section ("## v<version>") of CHANGELOG.md for the release notes. The heading must
# match exactly, so 4.1.0-beta.1 does not pick up 4.1.0-beta.19.
release_notes() {
    local f="$BUILD_DIR/RELEASE_NOTES.md"
    awk -v version="v$VERSION" '/^## /{if(p) exit; if($2 == version) p=1; if(p) print; next} p' CHANGELOG.md \
        | sed 's/^\s*-/*/' > "$f"
    [ -s "$f" ] || err "WARNING: no '## v$VERSION' section in CHANGELOG.md; this release will have no release notes"
    echo "$f"
}

# ---------------------------------------------------------------------- vpk
# Stock vpk, pinned. Also supplies the prebuilt helper binaries (Update, UpdateMac, ...) for the patched build.
ensure_vpk() {
    local dir="$TOOLS_DIR/vpk-stock-$VELOPACK_VERSION"
    VPK_STOCK="$dir/vpk"
    if [ ! -x "$VPK_STOCK" ]; then
        info "Installing vpk $VELOPACK_VERSION"
        dotnet tool install vpk --version "$VELOPACK_VERSION" --tool-path "$dir" >/dev/null \
            || die "Could not install vpk $VELOPACK_VERSION"
    fi
}

# Builds a patched copy of vpk (scripts/velopack/*.patch): macOS packing on Linux, and Linux AppImages built
# with pkgforge's appimagetool. The folder name includes a hash of the patches, so editing them rebuilds it.
# Sets VPK_PATCHED to its vpk.dll.
ensure_vpk_patched() {
    local hash root src
    hash="$(cat "$ROOT"/scripts/velopack/*.patch | sha256sum | cut -c1-8)"
    root="$TOOLS_DIR/vpk-patched-$VELOPACK_VERSION-$hash"
    VPK_PATCHED="$root/tools/net10.0/any/vpk.dll"
    [ -f "$VPK_PATCHED" ] && return

    ensure_vpk
    info "Building patched vpk $VELOPACK_VERSION"
    src="$(mktemp -d)"
    curl -fsSL "https://github.com/velopack/velopack/archive/refs/tags/$VELOPACK_VERSION.tar.gz" \
        | tar -xz -C "$src" --strip-components=1
    local patch
    for patch in "$ROOT"/scripts/velopack/*.patch; do
        (cd "$src" && git apply "$patch") || die "Velopack patch does not apply: $(basename "$patch")"
    done
    # PublicSign: the .snk key cannot be used to sign here (OpenSSL policies reject SHA-1), but internals
    # are shared between Velopack assemblies by public key, which public signing keeps intact.
    (cd "$src" && dotnet publish src/vpk/Velopack.Vpk/Velopack.Vpk.csproj -c Release -f net10.0 \
        -p:PublicSign=true -o "$root/tools/net10.0/any" -nologo -v q -clp:ErrorsOnly) || die "Building patched vpk failed"
    cp -r "$(ls -d "$TOOLS_DIR/vpk-stock-$VELOPACK_VERSION"/.store/vpk/"$VELOPACK_VERSION"/vpk/"$VELOPACK_VERSION"/vendor)" "$root/vendor"
    rm -rf "$src"
    find "$TOOLS_DIR" -maxdepth 1 -name "vpk-patched-$VELOPACK_VERSION-*" ! -name "vpk-patched-$VELOPACK_VERSION-$hash" -exec rm -rf {} +
}

# pkgforge-dev/appimagetool (the "full" build bundles uruntime and mkdwarfs, so no downloads at build time).
APPIMAGETOOL_VERSION="0.5.2"
APPIMAGETOOL_SHA256="49999e2ba854fd826aa00cb872a6b7294137d32346aec6e21be63cae49bdc854"

ensure_appimagetool() {
    APPIMAGETOOL="$TOOLS_DIR/appimagetool-full-$APPIMAGETOOL_VERSION-x86_64-linux"
    [ -x "$APPIMAGETOOL" ] && return
    info "Downloading appimagetool $APPIMAGETOOL_VERSION"
    curl -fsSL -o "$APPIMAGETOOL.part" \
        "https://github.com/pkgforge-dev/appimagetool/releases/download/$APPIMAGETOOL_VERSION/appimagetool-full-x86_64-linux"
    echo "$APPIMAGETOOL_SHA256  $APPIMAGETOOL.part" | sha256sum -c --quiet - \
        || { rm -f "$APPIMAGETOOL.part"; die "appimagetool checksum mismatch"; }
    chmod +x "$APPIMAGETOOL.part"
    mv -f "$APPIMAGETOOL.part" "$APPIMAGETOOL"
}

# Seeds an empty output dir with the previous release (for delta packages). The channel must be given:
# vpk otherwise assumes the host OS's channel (linux), whatever platform is being built.
vpk_download() { # <prefix> <channel> <outdir>
    if [ -n "$(ls -A "$3" 2>/dev/null)" ]; then return 0; fi
    local feed="${VATIS_RELEASES_URL%/}/$1/releases.$2.json"
    if ! curl -fsI "$feed" >/dev/null 2>&1; then
        err "No previous release at $feed; building without deltas"
        return 0
    fi
    ensure_vpk
    info "Fetching previous $1 release"
    "$VPK_STOCK" download http --url "${VATIS_RELEASES_URL%/}/$1" --channel "$2" --outputDir "$3" \
        || err "Download failed; building without deltas"
}

# -------------------------------------------------------------------- signing
RCODESIGN_VERSION="0.29.0"
JSIGN_VERSION="7.4"

mac_sign_enabled() { ! $SKIP_CODESIGN && [ -n "${APPLE_CERT_P12:-}" ]; }

win_sign_enabled() {
    $SKIP_CODESIGN && return 1
    local v
    for v in AZURE_TENANT_ID AZURE_CLIENT_ID AZURE_CLIENT_SECRET AZURE_TRUSTED_SIGNING_ENDPOINT \
             AZURE_TRUSTED_SIGNING_ACCOUNT AZURE_TRUSTED_SIGNING_CERT_PROFILE; do
        [ -n "${!v:-}" ] || return 1
    done
}

ensure_rcodesign() {
    if command -v rcodesign >/dev/null; then RCODESIGN="$(command -v rcodesign)"; return; fi
    RCODESIGN="$TOOLS_DIR/rcodesign"
    [ -x "$RCODESIGN" ] && return
    info "Downloading rcodesign $RCODESIGN_VERSION"
    local triple="x86_64-unknown-linux-musl"
    curl -fsSL "https://github.com/indygreg/apple-platform-rs/releases/download/apple-codesign%2F$RCODESIGN_VERSION/apple-codesign-$RCODESIGN_VERSION-$triple.tar.gz" \
        | tar -xz -C "$TOOLS_DIR" --strip-components=1 "apple-codesign-$RCODESIGN_VERSION-$triple/rcodesign"
    chmod +x "$RCODESIGN"
}

ensure_jsign() {
    JSIGN_JAR="$TOOLS_DIR/jsign-$JSIGN_VERSION.jar"
    [ -f "$JSIGN_JAR" ] && return
    command -v java >/dev/null || die "java is required by jsign"
    info "Downloading jsign $JSIGN_VERSION"
    curl -fsSL -o "$JSIGN_JAR" "https://github.com/ebourg/jsign/releases/download/$JSIGN_VERSION/jsign-$JSIGN_VERSION.jar"
}

# Prints a vpk --signTemplate for Azure Trusted Signing. The bearer token is short-lived.
win_sign_template() {
    ensure_jsign
    local token endpoint
    token="$(curl -fsSL -X POST "https://login.microsoftonline.com/$AZURE_TENANT_ID/oauth2/v2.0/token" \
        --data-urlencode "grant_type=client_credentials" \
        --data-urlencode "client_id=$AZURE_CLIENT_ID" \
        --data-urlencode "client_secret=$AZURE_CLIENT_SECRET" \
        --data-urlencode "scope=https://codesigning.azure.net/.default" \
        | python3 -I -c "import sys,json; print(json.load(sys.stdin)['access_token'])")" \
        || die "Failed to get Azure Trusted Signing token"
    endpoint="${AZURE_TRUSTED_SIGNING_ENDPOINT#https://}"; endpoint="${endpoint#http://}"
    echo "java -jar $JSIGN_JAR --storetype TRUSTEDSIGNING --keystore $endpoint --storepass $token" \
         "--alias $AZURE_TRUSTED_SIGNING_ACCOUNT/$AZURE_TRUSTED_SIGNING_CERT_PROFILE" \
         "--tsaurl http://timestamp.acs.microsoft.com --tsmode RFC3161 {{file}}"
}

# ---------------------------------------------------------------- native libs
build_native_linux() {
    info "NativeAudio (linux)"
    cmake -S ./NativeAudio -B ./NativeAudio/build -DCMAKE_BUILD_TYPE=Release
    cmake --build ./NativeAudio/build --config Release
    mkdir -p "$NATIVE_DIR/lin"
    cp ./NativeAudio/build/libNativeAudio.so "$NATIVE_DIR/lin/"
}

build_native_win() {
    info "NativeAudio (windows x64, docker)"
    ./NativeAudio/build-win.sh
}

build_native_macos() {
    info "NativeAudio (macos universal, docker)"
    ./NativeAudio/build-macos.sh
}

# ---------------------------------------------------------------------- linux
build_linux() {
    local out="$OUT_DIR/linux"
    mkdir -p "$out"
    [ -f "$NATIVE_DIR/lin/libNativeAudio.so" ] || die "Missing $NATIVE_DIR/lin/libNativeAudio.so (use --native)"
    publish linux-x64
    ensure_vpk_patched
    ensure_appimagetool
    rm -f "$out/$PACK_ID.AppImage"
    vpk_download linux linux "$out"

    info "vpk pack (linux)"
    VPK_APPIMAGETOOL="$APPIMAGETOOL" dotnet "$VPK_PATCHED" pack -y --skip-updates --packId "$PACK_ID" --packTitle "$APP_NAME" --packVersion "$VERSION" \
        --packAuthors "$AUTHORS" --packDir "$BUILD_DIR/linux-x64" --mainExe "$APP_NAME" \
        --delta BestSize --icon ./vATIS.Desktop/Assets/MainIcon.png --categories "AudioVideo;Audio" \
        --releaseNotes "$(release_notes)" --outputDir "$out" --verbose

    mv -f "$out/$PACK_ID.AppImage" "$out/$APP_NAME-$VERSION.AppImage"
}

# -------------------------------------------------------------------- windows
build_win() {
    local out="$OUT_DIR/windows"
    mkdir -p "$out"
    [ -f "$NATIVE_DIR/win/NativeAudio.dll" ] || die "Missing $NATIVE_DIR/win/NativeAudio.dll (use --native)"
    publish win-x64
    ensure_vpk
    vpk_download windows win "$out"

    local sign=()
    if win_sign_enabled; then
        sign=(--signTemplate "$(win_sign_template)")
    else
        err "WARNING: Windows build will NOT be code signed (--skip-codesign or AZURE_* not fully set)"
    fi

    info "vpk pack (windows)"
    "$VPK_STOCK" "[win]" pack -y --skip-updates --runtime win-x64 --packId "$PACK_ID" --packTitle "$APP_NAME" \
        --packVersion "$VERSION" --packAuthors "$AUTHORS" --packDir "$BUILD_DIR/win-x64" \
        --mainExe "$APP_NAME.exe" --noPortable --delta BestSize --skipVeloAppCheck \
        --icon ./vATIS.Desktop/Assets/MainIcon.ico --releaseNotes "$(release_notes)" \
        --outputDir "$out" "${sign[@]}" --verbose

    mv -f "$out/$PACK_ID-win-Setup.exe" "$out/$APP_NAME-Setup-$VERSION.exe"
}

# ---------------------------------------------------------------------- macOS
build_macos() {
    local out="$OUT_DIR/macos"
    local bundle="$BUILD_DIR/macos/$APP_NAME.app"
    mkdir -p "$out"
    [ -f "$NATIVE_DIR/macos/libNativeAudio.dylib" ] || die "Missing macOS libNativeAudio.dylib (use --native)"
    command -v docker >/dev/null || die "docker is required for lipo (macOS universal binary)"

    publish osx-x64
    publish osx-arm64

    info "Creating $APP_NAME.app"
    rm -rf "$BUILD_DIR/macos"
    mkdir -p "$bundle/Contents/MacOS" "$bundle/Contents/Resources"

    docker run --rm --user "$(id -u):$(id -g)" -v "$BUILD_DIR":/build -w /build "$LIPO_IMAGE" \
        /cctools/bin/arm64-apple-darwin24-lipo -create \
        "osx-x64/$APP_NAME" "osx-arm64/$APP_NAME" -output "macos/$APP_NAME.app/Contents/MacOS/$APP_NAME"
    chmod +x "$bundle/Contents/MacOS/$APP_NAME"

    local lib
    for lib in libAvaloniaNative.dylib libHarfBuzzSharp.dylib libNativeAudio.dylib libSkiaSharp.dylib; do
        cp "$BUILD_DIR/osx-arm64/$lib" "$bundle/Contents/MacOS/$lib"
    done
    # App icon: Assets.car (compiled from scripts/macos/AppIcon.icon, used by macOS 26 and later) plus the .icns
    # fallback that actool generated alongside it. See scripts/macos/README.md.
    cp ./scripts/macos/AppIcon.icns ./scripts/macos/Assets.car "$bundle/Contents/Resources/"

    # CFBundleVersion: <major.minor.patch>.<prerelease number or 0>
    local base beta cfbundle
    base="$(sed -E 's/[-+].*//' <<<"$VERSION")"
    beta="$(sed -E 's/.*-.*\.([0-9]+)/\1/' <<<"$VERSION")"
    [ "$beta" = "$VERSION" ] && beta=0
    cfbundle="$base.$beta"

    cat > "$bundle/Contents/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>$APP_NAME</string>
    <key>CFBundleDisplayName</key>
    <string>$APP_NAME</string>
    <key>CFBundleIdentifier</key>
    <string>$PACK_ID</string>
    <key>CFBundleShortVersionString</key>
    <string>$VERSION</string>
    <key>CFBundleVersion</key>
    <string>$cfbundle</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleExecutable</key>
    <string>$APP_NAME</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>CFBundleIconName</key>
    <string>AppIcon</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSMicrophoneUsageDescription</key>
    <string>vATIS requires access to your microphone.</string>
</dict>
</plist>
EOF

    ensure_vpk_patched
    vpk_download macos osx "$out"

    # The patched vpk adds UpdateMac and sq.version, signs and notarizes with rcodesign (configured through
    # APPLE_* variables), then writes the nupkg, feed and a portable zip of the signed app.
    local env_args=()
    if mac_sign_enabled; then
        ensure_rcodesign
        env_args=(VPK_RCODESIGN="$RCODESIGN")
    else
        err "WARNING: macOS app will NOT be signed/notarized (--skip-codesign or APPLE_CERT_P12 not set)"
        env_args=(-u APPLE_CERT_P12)
    fi

    info "vpk pack (macos)"
    env "${env_args[@]}" dotnet "$VPK_PATCHED" "[osx]" pack -y --skip-updates --packId "$PACK_ID" \
        --packTitle "$APP_NAME" --packVersion "$VERSION" --packAuthors "$AUTHORS" --packDir "$bundle" \
        --mainExe "$APP_NAME" --noInst --delta BestSize --icon ./scripts/macos/AppIcon.icns \
        --signEntitlements "$ROOT/scripts/app.entitlements" --releaseNotes "$(release_notes)" \
        --outputDir "$out" --verbose

    mv -f "$out/$PACK_ID-osx-Portable.zip" "$out/$APP_NAME-$VERSION-macos.zip"
}

# ----------------------------------------------------------------------- main
if $BUILD_NATIVE; then
    has linux && build_native_linux
    has win && build_native_win
    has macos && build_native_macos
fi

for p in "${PLATFORMS[@]}"; do "build_$p"; done

# ------------------------------------------------------------------- summary
declare -A PLATFORM_DIR=([linux]=linux [win]=windows [macos]=macos)

print_copy_instructions() {
    local p dir f
    info "Done. Copy these files to your server:"
    for p in "${PLATFORMS[@]}"; do
        dir="${PLATFORM_DIR[$p]}"
        printf '\n  \e[1mFrom:\e[0m %s/%s/\n  \e[1mTo:\e[0m   %s/%s/\n' "$OUT_DIR" "$dir" "${VATIS_RELEASES_URL%/}" "$dir"
        # Packages and installers for this version first, the feed last.
        while IFS= read -r f; do printf '      %s\n' "$f"; done \
            < <(cd "$OUT_DIR/$dir" && find . -maxdepth 1 -type f -name "*$VERSION*" -printf '%P\n' | sort)
        while IFS= read -r f; do printf '      %s  (copy last)\n' "$f"; done \
            < <(cd "$OUT_DIR/$dir" && find . -maxdepth 1 -type f -name 'releases.*.json' -printf '%P\n' | sort)
    done
    printf '\n  Do not copy: assets.*.json or RELEASES*.\n'
}

print_copy_instructions
