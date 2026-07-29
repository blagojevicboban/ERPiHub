# Script for building and packaging ErpHub using Velopack (vpk)
$ErrorActionPreference = "Stop"

Write-Host "================================================="
Write-Host "ErpHub -- Build and Packaging (Velopack)"
Write-Host "================================================="

$version = (Get-Content "version.txt").Trim()
Write-Host "Version: $version"

Write-Host "1. Building self-contained binaries..."
dotnet publish ErpHub.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish_output

Write-Host "2. Packaging with Velopack..."
if (Test-Path "ReleasePackage") {
    Remove-Item -Recurse -Force "ReleasePackage"
}

vpk pack --packId "ErpHub" --packVersion "$version" --packDir "publish_output" --mainExe "ErpHub.exe" --outputDir "ReleasePackage" --packTitle "ErpHub" --packAuthors "Blagojevic Boban"

Write-Host "================================================="
Write-Host "SUCCESS! Installation package created in ReleasePackage\"
Write-Host "Installer: ReleasePackage\ErpHub-$version-win-x64-Setup.exe"
Write-Host "================================================="
