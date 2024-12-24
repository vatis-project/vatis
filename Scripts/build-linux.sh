#!/bin/bash

# Check if a version number is provided
if [ $# -ne 1 ]; then
    echo "Usage: $0 <version>"
    exit 1
fi

VERSION="$1"

dotnet publish -c Release -r linux-x64 -o "./build-linux" -p:UseAppHost=true -p:Version=$VERSION ./vATIS.Desktop/vATIS.Desktop.csproj

if [ $? -ne 0 ]; then
    echo "Build failed!"
    exit 1
fi

# Build NativeAudio
mkdir -p ./NativeAudio/build
cmake -S ./NativeAudio -B ./NativeAudio/build
cmake --build ./NativeAudio/build --config Release
mkdir -p ./vATIS.Desktop/Voice/Audio/Native/lin
cp ./NativeAudio/build/libNativeAudio.so ./vATIS.Desktop/Voice/Audio/Native/lin

if [ -f "./velopack/org.vatsim.vatis.AppImage" ]; then
    rm "./velopack/org.vatsim.vatis.AppImage"
fi

# Download necessary files
vpk download s3 --outputDir "./velopack" \
    --bucket vatis-releases --endpoint "$AWS_ENDPOINT" \
    --keyId "$AWS_ACCESS_KEY_ID" --secret "$AWS_SECRET_ACCESS_KEY" \
    --prefix "linux"

vpk pack \
    --packId org.vatsim.vatis \
    --packTitle "vATIS" \
    --packVersion $VERSION \
    --packAuthors "Justin Shannon" \
    --packDir ./build-linux \
    --mainExe vATIS \
    --delta BestSize \
    --icon ./vATIS.Desktop/Assets/MainIcon.png \
    --outputDir ./velopack \
    --verbose

vpk upload s3 \
    --outputDir "./velopack" \
    --bucket vatis-releases \
    --endpoint "$AWS_ENDPOINT" \
    --keyId "$AWS_ACCESS_KEY_ID" \
    --secret "$AWS_SECRET_ACCESS_KEY" \
    --prefix "staging/linux"

# Upload Debug Symbols
npm install -g @sentry/cli
sentry-cli login --auth-token $SENTRY_AUTH_TOKEN
sentry-cli debug-files upload -o clowd -p vatis $BIN_DIR

mv ./velopack/org.vatsim.vatis.AppImage ./velopack/vATIS-$VERSION.AppImage

aws configure set aws_access_key_id "$AWS_ACCESS_KEY_ID"
aws configure set aws_secret_access_key "$AWS_SECRET_ACCESS_KEY"
aws configure set region auto
aws configure set output "json"

aws s3 cp "./velopack/vATIS-$VERSION.AppImage" \
    "s3://vatis-releases/staging/linux/vATIS-$VERSION.AppImage" \
    --endpoint-url "$AWS_ENDPOINT"

# Remove unused files
aws s3 rm "s3://vatis-releases/staging/linux/RELEASES-linux" \
    --endpoint-url "$AWS_ENDPOINT"
aws s3 rm "s3://vatis-releases/staging/linux/org.vatsim.vatis.AppImage" \
    --endpoint-url "$AWS_ENDPOINT"