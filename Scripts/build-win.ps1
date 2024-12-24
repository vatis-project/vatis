param (
    [string]$template = $(throw "Signing template required")
)

# Dotnet publish command with dynamically set version
Write-Host "Publishing project with version $env:VERSION..."
dotnet publish -c Release -r win-x64 -o .\Publish\win-x64 .\vATIS.Desktop\vATIS.Desktop.csproj -p:Version=$env:VERSION

# Upload debug symbols to Sentry
npm install -g @sentry/cli
sentry-cli login --auth-token $env:SENTRY_AUTH_TOKEN
sentry-cli debug-files upload -o clowd -p vatis .\Publish\win-x64

vpk download s3 --outputDir ".\Publish" `
    --bucket vatis-releases --endpoint "$env:AWS_ENDPOINT" `
    --keyId "$env:AWS_ACCESS_KEY_ID" --secret "$env:AWS_SECRET_ACCESS_KEY" `
    --prefix "windows"

# vpk pack command
Write-Host "Packing application with vpk..."
vpk pack `
    -x `
    -y `
    --verbose `
    --packId org.vatsim.vatis `
    --packTitle "vATIS" `
    --packVersion $env:VERSION `
    --packAuthors "Justin Shannon" `
    --packDir .\Publish\win-x64 `
    --mainExe vATIS.exe `
    --noPortable `
    --delta BestSize `
    --skipVeloAppCheck `
    --icon .\vATIS.Desktop\Assets\MainIcon.ico `
    --outputDir .\Publish `
    --signTemplate $template

Write-Host "Uploading package..."
vpk upload s3 `
    --outputDir ".\Publish" `
    --bucket vatis-releases `
    --endpoint "$env:AWS_ENDPOINT" `
    --keyId "$env:AWS_ACCESS_KEY_ID" `
    --secret "$env:AWS_SECRET_ACCESS_KEY" `
    --prefix "staging/windows"

#Rename resulting file with version
$setupFile = ".\Publish\org.vatsim.vatis-win-Setup.exe"
$newSetupFile = ".\Publish\vATIS-Setup-$env:VERSION.exe"

# Check if the new setup file already exists and delete it if so
if (Test-Path -Path $newSetupFile) {
    Write-Host "File $newSetupFile already exists. Deleting it..."
    Remove-Item -Path $newSetupFile -Force
}

Write-Host "Renaming output file to $newSetupFile..."
Rename-Item -Path $setupFile -NewName "vATIS-Setup-$env:VERSION.exe"

Write-Host "Done. The output file is $newSetupFile"

aws configure set aws_access_key_id "$env:AWS_ACCESS_KEY_ID"
aws configure set aws_secret_access_key "$env:AWS_SECRET_ACCESS_KEY"
aws configure set region auto
aws configure set output "json"

aws s3 cp "$newSetupFile" `
    "s3://vatis-releases/staging/windows/vATIS-Setup-$env:VERSION.exe" `
    --endpoint-url "$env:AWS_ENDPOINT"

# Remove unused files
aws s3 rm "s3://vatis-releases/staging/windows/RELEASES" `
    --endpoint-url "$env:AWS_ENDPOINT"
aws s3 rm "s3://vatis-releases/staging/windows/org.vatsim.vatis-win-Setup.exe" `
    --endpoint-url "$env:AWS_ENDPOINT"