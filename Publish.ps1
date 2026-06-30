#!/usr/bin/env pwsh
# Publish OllamaDesktopChat as a standalone executable

param(
    [ValidateSet('x64', 'x86', 'all')]
    [string]$Architecture = 'x64'
)

Write-Host "Publishing OllamaDesktopChat..." -ForegroundColor Cyan

function Publish-Standalone {
    param([string]$Arch)
    
    $profile = if ($Arch -eq 'x64') { 'StandaloneWin64' } else { 'StandaloneWin86' }
    
    Write-Host "Building $Arch version using profile: $profile..." -ForegroundColor Yellow
    
    dotnet publish -c Release -p:PublishProfile=$profile
    
    if ($LASTEXITCODE -eq 0) {
        $publishDir = "bin\Release\net8.0-windows\win-$Arch\publish"
        Write-Host "✓ Published to: $publishDir" -ForegroundColor Green
        Write-Host "  Executable: $publishDir\OllamaDesktopChat.exe" -ForegroundColor Green
    } else {
        Write-Host "✗ Publish failed for $Arch" -ForegroundColor Red
    }
}

if ($Architecture -eq 'all') {
    Publish-Standalone 'x64'
    Publish-Standalone 'x86'
} else {
    Publish-Standalone $Architecture
}

Write-Host "`nPublish complete!" -ForegroundColor Cyan
