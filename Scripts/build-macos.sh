#!/bin/bash

# Convert a semantic version to a CFBundleVersion format
convert_semver_to_cfbundleversion() {
    local semver="$1"
    local base_version beta_number

    # Extract base version (e.g., 1.0.0)
    base_version=$(echo "$semver" | sed -E 's/[-+].*//')

    # Extract beta number, default to 0 if not found
    beta_number=$(echo "$semver" | sed -E 's/.*-.*\.([0-9]+)/\1/')
    [[ "$beta_number" == "$semver" ]] && beta_number="0"

    print_info "$base_version.$beta_number"
}

cleanup() {
    print_info "Cleaning up..."
    security lock-keychain "$KEYCHAIN_PATH" || print_error "Failed to lock keychain"
    security delete-keychain "$KEYCHAIN_PATH" || print_error "Failed to delete keychain, it may not exist"
    rm -f "$CERT_PATH" || echo "Failed to delete certificate files"
}

print_info() {
    printf "\e[33m$*\e[0m\n"
}

print_error() {
    printf "\e[31m$*\e[0m\n"
}

trap cleanup EXIT

# Input validation
if [ $# -lt 1 ]; then
    print_info "Usage: $0 <version> [--codesign]"
    exit 1
fi

# Variables
VERSION="$1"
OUTPUT_DIR="./Publish"
TMP_DIR="$OUTPUT_DIR/tmp"
APP_NAME="vATIS"
ICON_SRC="./Desktop/Assets/MainIcon.icns"
APP_BUNDLE="$OUTPUT_DIR/$APP_NAME.app"
BIN_DIR="$OUTPUT_DIR/Bin"
CODESIGN=false
CERT_PATH=$RUNNER_TEMP/build_certificate.p12
KEYCHAIN_PATH=$RUNNER_TEMP/app-signing.keychain
KEYCHAIN_PASSWORD=`openssl rand -base64 12`

shift # skip the first argument
while [[ $# -gt 0 ]]; do
    case "$1" in
        --codesign)
            CODESIGN=true
            shift
            ;;
        *)
            print_error "Unknown option: $1"
            exit 1
            ;;
    esac
done

if $CODESIGN; then
    # Install certificates
    print_info "Installing certificates..."

    if [ -z "$APPLE_CERTIFICATE_BASE64" ] || [ -z "$APPLE_CERTIFICATE_PASSWORD" ] || [ -z "$APPLE_ID" ] || [ -z "$APPLE_TEAM_ID" ] || [ -z "$APPLE_PASSWORD" ]; then
        print_error "Required environment variables are not set."
        exit 1
    fi

    # import certificates
    echo -n "$APPLE_CERTIFICATE_BASE64" | base64 --decode -o $CERT_PATH

    # default again user login keychain
    security list-keychains -d user -s login.keychain

    # Create temp keychain
    security create-keychain -p "$KEYCHAIN_PASSWORD" "$KEYCHAIN_PATH"

    # Append temp keychain to the user domain
    security list-keychains -d user -s "$KEYCHAIN_PATH" $(security list-keychains -d user | sed s/\"//g)

    # Remove relock timeout
    security set-keychain-settings "$KEYCHAIN_PATH"

    # Unlock keychain
    security unlock-keychain -p "$KEYCHAIN_PASSWORD" "$KEYCHAIN_PATH"

    # Add certificate to keychain
    security import $CERT_PATH -k "$KEYCHAIN_PATH" -P "$APPLE_CERTIFICATE_PASSWORD" -A -T "/usr/bin/codesign" -T "/usr/bin/productsign"

    # Enable codesigning from a non user interactive shell
    security set-key-partition-list -S apple-tool:,apple:, -s -k $KEYCHAIN_PASSWORD -t private $KEYCHAIN_PATH

    # Create notarytool profile
    xcrun notarytool store-credentials "notary-profile" --apple-id "$APPLE_ID" --team-id "$APPLE_TEAM_ID" --password "$APPLE_PASSWORD" --keychain "$KEYCHAIN_PATH" 
fi

# Clean previous app bundle
[ -d "$APP_BUNDLE" ] && rm -r "$APP_BUNDLE"

# Create output directories
mkdir -p "$OUTPUT_DIR" "$APP_BUNDLE/Contents/MacOS" "$APP_BUNDLE/Contents/Resources"

# Build function
build_dotnet_project() {
    local runtime="$1"
    local output_path="$OUTPUT_DIR/$runtime"

    dotnet publish -c Release -r "$runtime" -o "$output_path" \
        -p:UseAppHost=true -p:Version=$VERSION ./Desktop/Desktop.csproj
}

# Build for x64 and arm64
build_dotnet_project "osx-x64"
build_dotnet_project "osx-arm64"

# Create a universal binary using lipo
lipo -create \
    "$OUTPUT_DIR/osx-x64/vATIS" \
    "$OUTPUT_DIR/osx-arm64/vATIS" \
    -output "$APP_BUNDLE/Contents/MacOS/vATIS"

# Copy required libraries
for lib in libAvaloniaNative.dylib libHarfBuzzSharp.dylib libNativeAudio.dylib libSkiaSharp.dylib; do
    cp "$OUTPUT_DIR/osx-arm64/$lib" "$APP_BUNDLE/Contents/MacOS/$lib"
done

# Copy app icon
cp "$ICON_SRC" "$APP_BUNDLE/Contents/Resources/app.icns"

# Generate CFBundleVersion
CFBUNDLE_VERSION=$(convert_semver_to_cfbundleversion "$VERSION")
print_info "CFBundleVersion: $CFBUNDLE_VERSION"

# Create Info.plist
cat << EOF > "$APP_BUNDLE/Contents/Info.plist"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>$APP_NAME</string>
    <key>CFBundleDisplayName</key>
    <string>$APP_NAME</string>
    <key>CFBundleIdentifier</key>
    <string>org.vatsim.vatis</string>
    <key>CFBundleShortVersionString</key>
    <string>$VERSION</string>
    <key>CFBundleVersion</key>
    <string>$CFBUNDLE_VERSION</string>
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

# Download necessary files
vpk download s3 --outputDir "$BIN_DIR" \
    --bucket vatis-releases --endpoint "$AWS_ENDPOINT" \
    --keyId "$AWS_ACCESS_KEY_ID" --secret "$AWS_SECRET_ACCESS_KEY" \
    --prefix "macos"

# Package the app
vpk pack \
    --packId "org.vatsim.vatis" \
    --packTitle "$APP_NAME" \
    --packVersion "$VERSION" \
    --packAuthors "Justin Shannon" \
    --packDir "$APP_BUNDLE" \
    --noInst \
    --mainExe "$APP_NAME" \
    --delta None \
    --icon "$ICON_SRC" \
    --outputDir "$BIN_DIR" \
    --verbose

unzip -o "$BIN_DIR/org.vatsim.vatis-osx-Portable.zip" -d $BIN_DIR
if [ -d "$BIN_DIR/__MACOSX" ]; then
    rm -rf "$BIN_DIR/__MACOSX"
fi

if [ -f "$BIN_DIR/vATIS-$VERSION.dmg" ]; then
    rm "$BIN_DIR/vATIS-$VERSION.dmg"
fi

if $CODESIGN; then
    # Codesign App Bundle
    print_info "Codesigning App Bundle..."
    codesign --deep --force --verify --verbose \
        --sign "$APPLE_SIGNING_IDENTITY" \
        --options runtime \
        --entitlements ./Scripts/app.entitlements \
        --keychain "$KEYCHAIN_PATH" \
        "$BIN_DIR/vATIS.app"

    print_info "Verifying App Bundle Signature..."
    codesign --verify --deep --strict --verbose=2 "$BIN_DIR/vATIS.app"
fi

# Create DMG
print_info Installing Create-DMG...
brew install create-dmg

print_info Creating DMG...
create-dmg \
    --background ./Scripts/dmg-background.tiff \
    --window-size 660 400 \
    --window-pos 200 120 \
    --app-drop-link 475 170 \
    --icon "$APP_NAME.app" 195 170 \
    --hdiutil-quiet \
    --hide-extension "$APP_NAME.app" \
    "$BIN_DIR/$APP_NAME-$VERSION.dmg" \
    "$BIN_DIR/vATIS.app"

if $CODESIGN; then
    # Codesign DMG
    print_info "Codesigning DMG..."
    codesign --force --sign "$APPLE_CERTIFICATE_NAME" --keychain "$KEYCHAIN_PATH" "$BIN_DIR/$APP_NAME-$VERSION.dmg"

    print_info "Verifying DMG codesign..."
    codesign --verify --verbose=2 "$BIN_DIR/$APP_NAME-$VERSION.dmg"

    # Notarize DMG
    print_info "Notarizing DMG..."
    xcrun notarytool submit "$BIN_DIR/$APP_NAME-$VERSION.dmg" --keychain-profile "notary-profile" --keychain "$KEYCHAIN_PATH" --wait

    # Staple Notarization Ticket
    print_info "Stapling notarization ticket to DMG..."
    xcrun stapler staple "$BIN_DIR/$APP_NAME-$VERSION.dmg"

    print_info "Validating notarization ticket..."
    xcrun stapler validate "$BIN_DIR/$APP_NAME-$VERSION.dmg"
fi

vpk upload s3 \
    --outputDir "$BIN_DIR" \
    --bucket vatis-releases \
    --endpoint "$AWS_ENDPOINT" \
    --keyId "$AWS_ACCESS_KEY_ID" \
    --secret "$AWS_SECRET_ACCESS_KEY" \
    --prefix "macos"

# Upload Debug Symbols
npm install -g @sentry/cli
sentry-cli login --auth-token $SENTRY_AUTH_TOKEN
sentry-cli debug-files upload -o clowd -p vatis $BIN_DIR

# Upload to R2
brew install awscli

aws configure set aws_access_key_id "$AWS_ACCESS_KEY_ID"
aws configure set aws_secret_access_key "$AWS_SECRET_ACCESS_KEY"
aws configure set region auto
aws configure set output "json"

aws s3 cp "$BIN_DIR/$APP_NAME-$VERSION.dmg" \
    "s3://vatis-releases/macos/$APP_NAME-$VERSION.dmg" \
    --endpoint-url "$AWS_ENDPOINT"

# Remove unused files
aws s3 rm "s3://vatis-releases/macos/RELEASES-osx" \
    --endpoint-url "$AWS_ENDPOINT"
aws s3 rm "s3://vatis-releases/macos/org.vatsim.vatis-osx-Portable.zip" \
    --endpoint-url "$AWS_ENDPOINT"