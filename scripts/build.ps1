#!/usr/bin/env pwsh
#
# Build script for MOS-DEF
# Creates a single-file executable for Windows x64
#

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputDir = "./artifacts",
    [switch]$Clean,
    [switch]$Test,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

# Script configuration
$ProjectFile = "src/MosDef.Cli/MosDef.Cli.csproj"
$SolutionFile = "MosDef.sln"
$ExecutableName = "mos-def.exe"

Write-Host "MOS-DEF Build Script" -ForegroundColor Cyan
Write-Host "===================" -ForegroundColor Cyan
Write-Host ""

# Validate that we're in the project root
if (-not (Test-Path $SolutionFile)) {
    Write-Error "Solution file not found. Please run this script from the project root directory."
    exit 1
}

if (-not (Test-Path $ProjectFile)) {
    Write-Error "Project file not found: $ProjectFile"
    exit 1
}

# Clean output directory if requested
if ($Clean -and (Test-Path $OutputDir)) {
    Write-Host "Cleaning output directory: $OutputDir" -ForegroundColor Yellow
    Remove-Item $OutputDir -Recurse -Force
}

# Create output directory
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

try {
    # Restore dependencies
    Write-Host "Restoring NuGet packages..." -ForegroundColor Green
    dotnet restore $SolutionFile
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed with exit code $LASTEXITCODE"
    }

    # Build solution
    Write-Host "Building solution..." -ForegroundColor Green
    $buildArgs = @(
        "build",
        $SolutionFile,
        "--configuration", $Configuration,
        "--no-restore"
    )
    
    if ($Verbose) {
        $buildArgs += "--verbosity", "detailed"
    }
    
    & dotnet @buildArgs
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE"
    }

    # Run tests if requested
    if ($Test) {
        Write-Host "Running tests..." -ForegroundColor Green
        dotnet test $SolutionFile --configuration $Configuration --no-build --logger "console;verbosity=normal"
        if ($LASTEXITCODE -ne 0) {
            throw "Tests failed with exit code $LASTEXITCODE"
        }
    }

    # Publish single-file executable
    Write-Host "Publishing single-file executable..." -ForegroundColor Green
    $publishArgs = @(
        "publish",
        $ProjectFile,
        "--configuration", $Configuration,
        "--runtime", $Runtime,
        "--self-contained", "true",
        "--output", $OutputDir,
        "/p:PublishSingleFile=true",
        "/p:PublishTrimmed=true",
        "/p:DebugType=None",
        "/p:DebugSymbols=false"
    )
    
    if ($Verbose) {
        $publishArgs += "--verbosity", "detailed"
    }
    
    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }

    # Verify executable was created
    $executablePath = Join-Path $OutputDir $ExecutableName
    if (-not (Test-Path $executablePath)) {
        throw "Executable not found at expected location: $executablePath"
    }

    # Get file information
    $fileInfo = Get-Item $executablePath
    $fileSizeMB = [math]::Round($fileInfo.Length / 1MB, 2)

    Write-Host ""
    Write-Host "✓ Build completed successfully!" -ForegroundColor Green
    Write-Host "✓ Executable: $executablePath" -ForegroundColor Green
    Write-Host "✓ Size: $fileSizeMB MB" -ForegroundColor Green
    Write-Host "✓ Runtime: $Runtime" -ForegroundColor Green
    Write-Host "✓ Configuration: $Configuration" -ForegroundColor Green

    # Test the executable
    Write-Host ""
    Write-Host "Testing executable..." -ForegroundColor Yellow
    try {
        $versionOutput = & $executablePath --version 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Version check: $versionOutput" -ForegroundColor Green
        } else {
            Write-Warning "Version check failed with exit code $LASTEXITCODE"
        }
    } catch {
        Write-Warning "Could not test executable: $_"
    }

    Write-Host ""
    Write-Host "Build artifacts available in: $OutputDir" -ForegroundColor Cyan

} catch {
    Write-Host ""
    Write-Host "✗ Build failed: $_" -ForegroundColor Red
    exit 1
}
