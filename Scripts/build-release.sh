#!/usr/bin/env bash
# build-release.sh - Build and package vATIS for Windows, macOS, and Linux.
#
# Usage:
#   ./build-release.sh [OPTIONS]
#
# Options:
#   --version <semver>          Override version. Defaults to the Version value in
#                               vATIS.Desktop/vATIS.Desktop.csproj.
#   --configuration <name>      Build configuration. Default: Release
#   --output-dir <path>         Output directory. Default: ./artifacts
#   --platform <platform>       Platform(s) to build: windows, macos, linux
#                               Can be specified multiple times or as a comma-separated
#                               list (e.g. --platform windows,macos). Default: all three.
#   --skip-codesign             Skip code signing and notarization.
#   --skip-notarize             Skip notarization for macOS builds.
#   --build-nativeaudio         Rebuild NativeAudio for each selected platform before publish.
#   --help                      Show this help text.
#
# This script follows the same env-setup workflow as xPilot's release script.
# If ./env-setup.sh exists, it is sourced automatically.
#
# macOS signing / notarization:
#   APPLE_CERT_P12
#   APPLE_CERT_PASSWORD
#   APPLE_NOTARY_API_KEY
#
# Windows signing with Azure Trusted Signing:
#   AZURE_TENANT_ID
#   AZURE_CLIENT_ID
#   AZURE_CLIENT_SECRET
#   AZURE_TRUSTED_SIGNING_ENDPOINT
#   AZURE_TRUSTED_SIGNING_ACCOUNT
#   AZURE_TRUSTED_SIGNING_CERT_PROFILE

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_SETUP_FILE="$SCRIPT_DIR/env-setup.sh"
DESKTOP_CSPROJ="$SCRIPT_DIR/vATIS.Desktop/vATIS.Desktop.csproj"
ENTITLEMENTS_FILE="$SCRIPT_DIR/Scripts/app.entitlements"
ICON_ICO="$SCRIPT_DIR/vATIS.Desktop/Assets/MainIcon.ico"
ICON_PNG="$SCRIPT_DIR/vATIS.Desktop/Assets/MainIcon.png"
ICON_ICNS="$SCRIPT_DIR/vATIS.Desktop/Assets/MainIcon.icns"
APP_NAME="vATIS"
APP_BUNDLE_ID="org.vatsim.vatis"

VERSION=""
CONFIGURATION="Release"
OUTPUT_DIR="$SCRIPT_DIR/artifacts"
SKIP_CODESIGN="false"
SKIP_NOTARIZE="false"
BUILD_NATIVEAUDIO="false"
BUILD_PLATFORMS=()

JSIGN_VERSION="7.4"
JSIGN_JAR=""
AZURE_ACCESS_TOKEN=""
VPK_BIN=""
HOST_OS=""
RCODESIGN_BIN=""
LIPO_BIN=""

print_info() {
    printf '\033[33m%s\033[0m\n' "$*"
}

print_error() {
    printf '\033[31m%s\033[0m\n' "$*" >&2
}

show_help() {
    awk 'NR>1 && /^[^#]/{exit} /^#/{print}' "$0" | sed 's/^# \{0,1\}//'
}

if [[ -f "$ENV_SETUP_FILE" ]]; then
    # shellcheck disable=SC1090
    source "$ENV_SETUP_FILE"
fi

while [[ $# -gt 0 ]]; do
    case "$1" in
        --version)
            VERSION="$2"
            shift 2
            ;;
        --configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        --output-dir)
            OUTPUT_DIR="$2"
            shift 2
            ;;
        --platform)
            IFS=',' read -ra _plats <<< "$2"
            BUILD_PLATFORMS+=("${_plats[@]}")
            shift 2
            ;;
        --skip-codesign)
            SKIP_CODESIGN="true"
            shift
            ;;
        --skip-notarize)
            SKIP_NOTARIZE="true"
            shift
            ;;
        --build-nativeaudio)
            BUILD_NATIVEAUDIO="true"
            shift
            ;;
        --help)
            show_help
            exit 0
            ;;
        *)
            print_error "Unknown argument: $1"
            exit 1
            ;;
    esac
done

if [[ ${#BUILD_PLATFORMS[@]} -eq 0 ]]; then
    BUILD_PLATFORMS=(windows macos linux)
fi

for _p in "${BUILD_PLATFORMS[@]}"; do
    case "$_p" in
        windows|macos|linux) ;;
        *)
            print_error "Unknown platform: $_p"
            print_error "Valid values: windows, macos, linux"
            exit 1
            ;;
    esac
done
unset _p

if [[ -z "$VERSION" ]]; then
    VERSION="$(sed -n 's/.*<Version>\(.*\)<\/Version>.*/\1/p' "$DESKTOP_CSPROJ" | head -n 1)"
fi

if [[ -z "$VERSION" ]]; then
    print_error "Unable to determine version. Pass --version explicitly."
    exit 1
fi

if [[ "$SKIP_CODESIGN" == "true" ]]; then
    SKIP_NOTARIZE="true"
fi

if [[ "$OUTPUT_DIR" != /* ]]; then
    OUTPUT_DIR="$SCRIPT_DIR/$OUTPUT_DIR"
fi

case "$(uname -s)" in
    Linux) HOST_OS="linux" ;;
    Darwin) HOST_OS="macos" ;;
    MINGW*|MSYS*|CYGWIN*) HOST_OS="windows" ;;
    *) HOST_OS="unknown" ;;
esac

ARTIFACT_DIR="$OUTPUT_DIR/$VERSION"
PUBLISH_ROOT="$SCRIPT_DIR/publish/release/$VERSION"

build_platform() { [[ " ${BUILD_PLATFORMS[*]} " == *" $1 "* ]]; }

require_command() {
    if ! command -v "$1" >/dev/null 2>&1; then
        print_error "Required command not found: $1"
        exit 1
    fi
}

sha256_of() {
    local file="$1"
    if command -v sha256sum >/dev/null 2>&1; then
        sha256sum "$file" | awk '{print $1}'
    else
        shasum -a 256 "$file" | awk '{print $1}'
    fi
}

windows_sign_enabled() {
    [[ "$SKIP_CODESIGN" != "true" ]] \
        && [[ -n "${AZURE_TENANT_ID:-}" ]] \
        && [[ -n "${AZURE_CLIENT_ID:-}" ]] \
        && [[ -n "${AZURE_CLIENT_SECRET:-}" ]] \
        && [[ -n "${AZURE_TRUSTED_SIGNING_ENDPOINT:-}" ]] \
        && [[ -n "${AZURE_TRUSTED_SIGNING_ACCOUNT:-}" ]] \
        && [[ -n "${AZURE_TRUSTED_SIGNING_CERT_PROFILE:-}" ]]
}

ensure_jsign() {
    local tools_dir="$SCRIPT_DIR/publish/tools"
    JSIGN_JAR="$tools_dir/jsign-${JSIGN_VERSION}.jar"

    if [[ -f "$JSIGN_JAR" ]]; then
        return
    fi

    require_command java
    require_command curl

    print_info "jsign not found; downloading v${JSIGN_VERSION}..."
    mkdir -p "$tools_dir"
    curl -fsSL -o "$JSIGN_JAR" \
        "https://github.com/ebourg/jsign/releases/download/${JSIGN_VERSION}/jsign-${JSIGN_VERSION}.jar"
}

ensure_vpk() {
    local tools_dir="$SCRIPT_DIR/publish/tools"

    if command -v vpk >/dev/null 2>&1; then
        VPK_BIN="$(command -v vpk)"
        return
    fi

    VPK_BIN="$tools_dir/vpk"
    if [[ -x "$VPK_BIN" ]]; then
        return
    fi

    require_command dotnet
    mkdir -p "$tools_dir"

    print_info "vpk not found; installing local Velopack CLI..."
    dotnet tool install --tool-path "$tools_dir" vpk
}

convert_semver_to_cfbundleversion() {
    local semver="$1"
    local core prerelease build_number

    core="${semver%%[-+]*}"
    prerelease="${semver#"$core"}"
    build_number="0"

    if [[ "$prerelease" =~ \.([0-9]+)$ ]]; then
        build_number="${BASH_REMATCH[1]}"
    fi

    printf '%s.%s\n' "$core" "$build_number"
}

merge_deps_json() {
    local x64_file="$1"
    local arm64_file="$2"
    local out_file="$3"

    python3 - "$x64_file" "$arm64_file" "$out_file" <<'PY'
import json
import pathlib
import sys

x64_path = pathlib.Path(sys.argv[1])
arm64_path = pathlib.Path(sys.argv[2])
out_path = pathlib.Path(sys.argv[3])

x64 = json.loads(x64_path.read_text())
arm64 = json.loads(arm64_path.read_text())

x64_runtime = x64["runtimeTarget"]["name"]
arm64_runtime = arm64["runtimeTarget"]["name"]

if x64_runtime == arm64_runtime:
    out_path.write_text(arm64_path.read_text())
    raise SystemExit(0)

x64_targets = x64.get("targets", {})
arm64_targets = arm64.get("targets", {})

common_targets = sorted(set(x64_targets).intersection(arm64_targets))
for key in common_targets:
    if x64_targets[key] != arm64_targets[key]:
        raise SystemExit(f"Shared target '{key}' differs between x64 and arm64 deps files")

merged = arm64
merged["targets"][x64_runtime] = x64_targets[x64_runtime]
out_path.write_text(json.dumps(merged, indent=2) + "\n")
PY
}

ensure_lipo() {
    if command -v lipo >/dev/null 2>&1; then
        LIPO_BIN="$(command -v lipo)"
        return
    fi

    if command -v llvm-lipo >/dev/null 2>&1; then
        LIPO_BIN="$(command -v llvm-lipo)"
        return
    fi

    print_error "Neither lipo nor llvm-lipo was found."
    print_error "Install LLVM lipo support or run the script on a host that provides lipo."
    exit 1
}

ensure_rcodesign() {
    if command -v rcodesign >/dev/null 2>&1; then
        RCODESIGN_BIN="$(command -v rcodesign)"
        return
    fi

    local tools_dir="$SCRIPT_DIR/publish/tools"
    local rcodesign_version="0.29.0"
    local uname_s uname_m triple archive_name archive_url

    RCODESIGN_BIN="$tools_dir/rcodesign"
    if [[ -x "$RCODESIGN_BIN" ]]; then
        return
    fi

    require_command curl
    require_command tar

    uname_s="$(uname -s)"
    uname_m="$(uname -m)"

    if [[ "$uname_s" == "Linux" ]]; then
        triple="x86_64-unknown-linux-musl"
    elif [[ "$uname_s" == "Darwin" ]]; then
        if [[ "$uname_m" == "arm64" ]]; then
            triple="aarch64-apple-darwin"
        else
            triple="x86_64-apple-darwin"
        fi
    else
        print_error "Unsupported host platform for rcodesign auto-download: $uname_s"
        exit 1
    fi

    archive_name="apple-codesign-${rcodesign_version}-${triple}.tar.gz"
    archive_url="https://github.com/indygreg/apple-platform-rs/releases/download/apple-codesign%2F${rcodesign_version}/${archive_name}"

    print_info "rcodesign not found; downloading ${rcodesign_version}..."
    mkdir -p "$tools_dir"
    curl -fsSL "$archive_url" | tar -xz -C "$tools_dir" \
        "apple-codesign-${rcodesign_version}-${triple}/rcodesign" \
        --strip-components=1
    chmod +x "$RCODESIGN_BIN"
}

run_vpk() {
    DOTNET_ROLL_FORWARD=Major "$VPK_BIN" "$@"
}

vpk_supports_windows_pack() {
    local help_output
    help_output="$(run_vpk pack -H 2>/dev/null || true)"
    [[ "$help_output" == *"--noPortable"* ]] || [[ "$help_output" == *"Setup.exe"* ]] || [[ "$help_output" == *"windows"* ]]
}

zip_dir_contents() {
    local source_dir="$1"
    local dest_zip="$2"

    python3 - "$source_dir" "$dest_zip" <<'PY'
import pathlib
import sys
import zipfile

source = pathlib.Path(sys.argv[1])
dest = pathlib.Path(sys.argv[2])
dest.parent.mkdir(parents=True, exist_ok=True)

with zipfile.ZipFile(dest, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
    for path in sorted(source.rglob("*")):
        if path.is_file():
            zf.write(path, path.relative_to(source))
PY
}

notarize_with_retry() {
    local zip_path="$1"
    local max_attempts=3
    local delay=30
    local attempt=1

    while (( attempt <= max_attempts )); do
        print_info "Notarization attempt $attempt of $max_attempts..."
        if "$RCODESIGN_BIN" notary-submit \
            --api-key-file "$APPLE_NOTARY_API_KEY" \
            --wait \
            "$zip_path"; then
            return 0
        fi

        if (( attempt < max_attempts )); then
            print_info "Notarization attempt failed; retrying in ${delay}s..."
            sleep "$delay"
            delay=$(( delay * 2 ))
        fi

        attempt=$(( attempt + 1 ))
    done

    print_error "Notarization failed after $max_attempts attempts."
    exit 1
}

get_azure_codesign_token() {
    if [[ -n "$AZURE_ACCESS_TOKEN" ]]; then
        return
    fi

    print_info "Fetching Azure Trusted Signing access token..."
    AZURE_ACCESS_TOKEN="$(
        curl -fsSL -X POST \
            "https://login.microsoftonline.com/${AZURE_TENANT_ID}/oauth2/v2.0/token" \
            --data-urlencode "grant_type=client_credentials" \
            --data-urlencode "client_id=${AZURE_CLIENT_ID}" \
            --data-urlencode "client_secret=${AZURE_CLIENT_SECRET}" \
            --data-urlencode "scope=https://codesigning.azure.net/.default" |
            python3 -c "import sys,json; print(json.load(sys.stdin)['access_token'])"
    )"

    if [[ -z "$AZURE_ACCESS_TOKEN" ]]; then
        print_error "Failed to obtain Azure Trusted Signing access token."
        exit 1
    fi
}

win_sign() {
    local file="$1"
    local endpoint

    ensure_jsign
    get_azure_codesign_token

    endpoint="${AZURE_TRUSTED_SIGNING_ENDPOINT#https://}"
    endpoint="${endpoint#http://}"

    print_info "Signing (Windows): $file"
    java -jar "$JSIGN_JAR" \
        --storetype TRUSTEDSIGNING \
        --keystore "$endpoint" \
        --storepass "$AZURE_ACCESS_TOKEN" \
        --alias "${AZURE_TRUSTED_SIGNING_ACCOUNT}/${AZURE_TRUSTED_SIGNING_CERT_PROFILE}" \
        --tsaurl "http://timestamp.acs.microsoft.com" \
        --tsmode RFC3161 \
        "$file"
}

build_nativeaudio_for_platform() {
    local platform="$1"

    if [[ "$BUILD_NATIVEAUDIO" != "true" ]]; then
        return
    fi

    print_info "Building NativeAudio for $platform..."
    case "$platform" in
        linux)
            "$SCRIPT_DIR/NativeAudio/build-linux.sh"
            ;;
        macos)
            "$SCRIPT_DIR/NativeAudio/build-macos.sh"
            ;;
        windows)
            "$SCRIPT_DIR/NativeAudio/build-windows.sh"
            ;;
    esac
}

publish_desktop() {
    local rid="$1"
    local out_dir="$2"
    shift 2

    rm -rf "$out_dir"
    print_info "Publishing vATIS.Desktop ($rid)..."
    dotnet publish "$DESKTOP_CSPROJ" \
        -c "$CONFIGURATION" \
        -r "$rid" \
        -o "$out_dir" \
        -p:Version="$VERSION" \
        "$@" \
        --nologo -v quiet
}

package_windows() {
    local rid="win-x64"
    local publish_dir="$PUBLISH_ROOT/windows/$rid"
    local out_dir="$ARTIFACT_DIR/windows"
    local setup_src
    local setup_dst
    local portable_zip

    build_nativeaudio_for_platform windows
    publish_desktop "$rid" "$publish_dir"
    mkdir -p "$out_dir"

    if windows_sign_enabled; then
        win_sign "$publish_dir/vATIS.exe"
    elif [[ "$SKIP_CODESIGN" != "true" ]]; then
        print_info "Skipping Windows signing; Azure Trusted Signing credentials are not fully configured."
    fi

    ensure_vpk
    if vpk_supports_windows_pack; then
        print_info "Packaging Windows setup..."
        run_vpk pack \
            -x \
            -y \
            --verbose \
            --packId org.vatsim.vatis \
            --packTitle "vATIS" \
            --packVersion "$VERSION" \
            --packAuthors "Justin Shannon" \
            --packDir "$publish_dir" \
            --mainExe vATIS.exe \
            --icon "$ICON_ICO" \
            --outputDir "$out_dir"

        setup_src="$out_dir/org.vatsim.vatis-win-Setup.exe"
        setup_dst="$out_dir/vATIS-Setup-$VERSION.exe"
        if [[ -f "$setup_src" ]]; then
            if windows_sign_enabled; then
                win_sign "$setup_src"
            fi
            cp "$setup_src" "$setup_dst"
            print_info "Windows installer: $setup_dst"
            print_info "SHA-256: $(sha256_of "$setup_dst")"
            return
        fi
    fi

    portable_zip="$out_dir/vATIS-win-x64-$VERSION.zip"
    print_info "Windows installer packaging is not available on this host/toolchain; creating portable zip instead."
    zip_dir_contents "$publish_dir" "$portable_zip"
    print_info "Windows package: $portable_zip"
    print_info "SHA-256: $(sha256_of "$portable_zip")"
}

package_linux() {
    local rid="linux-x64"
    local publish_dir="$PUBLISH_ROOT/linux/$rid"
    local out_dir="$ARTIFACT_DIR/linux"
    local appimage_src
    local appimage_dst

    build_nativeaudio_for_platform linux
    publish_desktop "$rid" "$publish_dir"
    mkdir -p "$out_dir"

    ensure_vpk
    print_info "Packaging Linux AppImage..."
    run_vpk pack \
        --verbose \
        --packId org.vatsim.vatis \
        --packTitle "vATIS" \
        --packVersion "$VERSION" \
        --packAuthors "Justin Shannon" \
        --packDir "$publish_dir" \
        --mainExe vATIS \
        --delta BestSize \
        --icon "$ICON_PNG" \
        --outputDir "$out_dir"

    appimage_src="$out_dir/org.vatsim.vatis.AppImage"
    appimage_dst="$out_dir/vATIS-$VERSION.AppImage"
    if [[ -f "$appimage_src" ]]; then
        cp "$appimage_src" "$appimage_dst"
        chmod +x "$appimage_dst"
        print_info "Linux package: $appimage_dst"
        print_info "SHA-256: $(sha256_of "$appimage_dst")"
    fi
}

package_macos() {
    local out_dir="$ARTIFACT_DIR/macos"
    local staging_dir="$out_dir/staging"
    local archive_path="$out_dir/$APP_NAME-$VERSION-macos.zip"
    local app_bundle="$out_dir/$APP_NAME.app"
    local universal_publish_dir="$staging_dir/macos-universal"
    local x64_dir="$staging_dir/osx-x64"
    local arm64_dir="$staging_dir/osx-arm64"
    local bundle_version
    local macos_dir
    local resources_dir
    local item
    local name

    build_nativeaudio_for_platform macos
    mkdir -p "$out_dir"

    publish_desktop osx-x64 "$x64_dir" \
        -p:SelfContained=true \
        -p:PublishSingleFile=true \
        -p:PublishTrimmed=true \
        -p:DebugType=embedded \
        -p:UseAppHost=true
    publish_desktop osx-arm64 "$arm64_dir" \
        -p:SelfContained=true \
        -p:PublishSingleFile=true \
        -p:PublishTrimmed=true \
        -p:DebugType=embedded \
        -p:UseAppHost=true

    print_info "Creating universal macOS binaries..."
    rm -rf "$universal_publish_dir"
    mkdir -p "$universal_publish_dir"
    cp -r "$arm64_dir/." "$universal_publish_dir/"

    rm -f "$universal_publish_dir/$APP_NAME"
    "$LIPO_BIN" -create \
        -output "$universal_publish_dir/$APP_NAME" \
        "$x64_dir/$APP_NAME" \
        "$arm64_dir/$APP_NAME"
    chmod +x "$universal_publish_dir/$APP_NAME"

    while IFS= read -r rel; do
        local arm_file="$arm64_dir/$rel"
        local x64_file="$x64_dir/$rel"
        local out_file="$universal_publish_dir/$rel"

        [[ "$rel" == "$APP_NAME" ]] && continue
        [[ ! -f "$x64_file" ]] && continue
        cmp -s "$x64_file" "$arm_file" && continue

        case "$rel" in
            *.dylib)
                "$LIPO_BIN" -create -output "$out_file" "$x64_file" "$arm_file"
                ;;
            *.deps.json)
                merge_deps_json "$x64_file" "$arm_file" "$out_file"
                ;;
            *.pdb|*.dbg)
                cp "$arm_file" "$out_file"
                ;;
            *)
                print_error "macOS publish outputs differ for non-Mach-O file '$rel'."
                print_error "x64:   $x64_file"
                print_error "arm64: $arm_file"
                exit 1
                ;;
        esac
    done < <(cd "$arm64_dir" && find . -type f | sed 's#^\./##' | sort)

    print_info "Preparing app bundle structure..."
    rm -rf "$app_bundle" "$archive_path"
    mkdir -p "$app_bundle/Contents/MacOS" "$app_bundle/Contents/Resources"

    cp -r "$universal_publish_dir/." "$app_bundle/Contents/MacOS/"
    chmod +x "$app_bundle/Contents/MacOS/$APP_NAME"
    cp "$ICON_ICNS" "$app_bundle/Contents/Resources/app.icns"

    print_info "Relocating managed/runtime payload to Contents/Resources..."
    macos_dir="$app_bundle/Contents/MacOS"
    resources_dir="$app_bundle/Contents/Resources"

    shopt -s dotglob nullglob
    for item in "$macos_dir"/*; do
        name="$(basename "$item")"
        case "$name" in
            "$APP_NAME"|createdump|*.dylib)
                continue
                ;;
        esac

        mv "$item" "$resources_dir/$name"
        ln -s "../Resources/$name" "$item"
    done
    shopt -u dotglob nullglob

    for item in "$macos_dir"/createdump "$macos_dir"/*.dylib; do
        if [[ -e "$item" ]]; then
            name="$(basename "$item")"
            ln -sfn "../MacOS/$name" "$resources_dir/$name"
        fi
    done

    bundle_version="$(convert_semver_to_cfbundleversion "$VERSION")"
    print_info "Writing Info.plist..."
    cat > "$app_bundle/Contents/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>$APP_NAME</string>
    <key>CFBundleDisplayName</key>
    <string>$APP_NAME</string>
    <key>CFBundleIdentifier</key>
    <string>$APP_BUNDLE_ID</string>
    <key>CFBundleShortVersionString</key>
    <string>$VERSION</string>
    <key>CFBundleVersion</key>
    <string>$bundle_version</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleExecutable</key>
    <string>$APP_NAME</string>
    <key>CFBundleIconFile</key>
    <string>app.icns</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSMicrophoneUsageDescription</key>
    <string>vATIS requires access to your microphone.</string>
</dict>
</plist>
EOF

    if [[ "$SKIP_CODESIGN" != "true" ]]; then
        print_info "Code signing nested dylibs..."
        while IFS= read -r -d '' dylib; do
            "$RCODESIGN_BIN" sign \
                --p12-file "$APPLE_CERT_P12" \
                --p12-password "${APPLE_CERT_PASSWORD:-}" \
                --code-signature-flags runtime \
                "$dylib"
        done < <(find "$app_bundle/Contents" -name "*.dylib" -print0)

        print_info "Code signing app bundle..."
        "$RCODESIGN_BIN" sign \
            --p12-file "$APPLE_CERT_P12" \
            --p12-password "${APPLE_CERT_PASSWORD:-}" \
            --code-signature-flags runtime \
            --for-notarization \
            --entitlements-xml-path "$ENTITLEMENTS_FILE" \
            "$app_bundle"
    fi

    print_info "Creating macOS zip archive..."
    rm -f "$archive_path"
    if command -v ditto >/dev/null 2>&1; then
        ditto -c -k --sequesterRsrc --keepParent "$app_bundle" "$archive_path"
    else
        require_command zip
        (
            cd "$out_dir"
            zip -qr --symlinks "$archive_path" "$(basename "$app_bundle")"
        )
    fi

    if [[ "$SKIP_NOTARIZE" != "true" ]]; then
        notarize_with_retry "$archive_path"
        print_info "Stapling notarization ticket..."
        "$RCODESIGN_BIN" staple "$app_bundle"

        print_info "Recreating macOS zip archive..."
        rm -f "$archive_path"
        if command -v ditto >/dev/null 2>&1; then
            ditto -c -k --sequesterRsrc --keepParent "$app_bundle" "$archive_path"
        else
            (
                cd "$out_dir"
                zip -qr --symlinks "$archive_path" "$(basename "$app_bundle")"
            )
        fi
    fi

    print_info "macOS app bundle: $app_bundle"
    print_info "macOS archive:    $archive_path"
    print_info "SHA-256: $(sha256_of "$archive_path")"
}

require_command dotnet
require_command python3

if build_platform windows && [[ "$SKIP_CODESIGN" != "true" ]] && ! windows_sign_enabled; then
    print_info "Windows signing credentials are not fully configured; Windows artifacts will be unsigned."
fi

if build_platform macos && [[ "$SKIP_CODESIGN" != "true" && -z "${APPLE_CERT_P12:-}" ]]; then
    print_error "Building macOS with signing enabled but APPLE_CERT_P12 is not set."
    print_error "Run: source ./env-setup.sh  (or pass --skip-codesign to skip signing)"
    exit 1
fi

if build_platform macos && [[ "$SKIP_NOTARIZE" != "true" && -z "${APPLE_NOTARY_API_KEY:-}" ]]; then
    print_error "Building macOS with notarization enabled but APPLE_NOTARY_API_KEY is not set."
    print_error "Run: source ./env-setup.sh  (or pass --skip-notarize to skip notarization)"
    exit 1
fi

if build_platform macos; then
    if [[ ! -f "$ENTITLEMENTS_FILE" ]]; then
        print_error "Entitlements file not found: $ENTITLEMENTS_FILE"
        exit 1
    fi

    if [[ ! -f "$ICON_ICNS" ]]; then
        print_error "Icon file not found: $ICON_ICNS"
        exit 1
    fi

    ensure_lipo

    if [[ "$SKIP_CODESIGN" != "true" ]]; then
        ensure_rcodesign
    fi
fi

print_info "=========================================="
print_info " vATIS Release Build"
print_info " Version:        $VERSION"
print_info " Configuration:  $CONFIGURATION"
print_info " Platforms:      ${BUILD_PLATFORMS[*]}"
print_info " Output dir:     $ARTIFACT_DIR"
print_info " Code sign:      $([[ "$SKIP_CODESIGN" == "true" ]] && printf 'no' || printf 'yes')"
print_info " Notarize:       $([[ "$SKIP_NOTARIZE" == "true" ]] && printf 'no' || printf 'yes')"
print_info " NativeAudio:    $([[ "$BUILD_NATIVEAUDIO" == "true" ]] && printf 'rebuild' || printf 'reuse existing')"
print_info "=========================================="

mkdir -p "$ARTIFACT_DIR"

if build_platform windows; then
    package_windows
fi

if build_platform macos; then
    package_macos
fi

if build_platform linux; then
    package_linux
fi

print_info "Release packaging complete."
print_info "Artifacts: $ARTIFACT_DIR"
