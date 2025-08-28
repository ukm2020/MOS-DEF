#!/usr/bin/env pwsh
#
# Test script for MOS-DEF
# Runs all unit tests and generates coverage reports
#

param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "./test-results",
    [switch]$Coverage,
    [switch]$Verbose,
    [switch]$Watch
)

$ErrorActionPreference = "Stop"

# Script configuration
$SolutionFile = "MosDef.sln"
$TestProject = "tests/MosDef.Tests/MosDef.Tests.csproj"

Write-Host "MOS-DEF Test Script" -ForegroundColor Cyan
Write-Host "==================" -ForegroundColor Cyan
Write-Host ""

# Validate that we're in the project root
if (-not (Test-Path $SolutionFile)) {
    Write-Error "Solution file not found. Please run this script from the project root directory."
    exit 1
}

if (-not (Test-Path $TestProject)) {
    Write-Error "Test project not found: $TestProject"
    exit 1
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
    dotnet build $SolutionFile --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE"
    }

    # Prepare test arguments
    $testArgs = @(
        "test",
        $SolutionFile,
        "--configuration", $Configuration,
        "--no-build",
        "--results-directory", $OutputDir,
        "--logger", "trx",
        "--logger", "console;verbosity=normal"
    )

    if ($Coverage) {
        Write-Host "Enabling code coverage..." -ForegroundColor Yellow
        $testArgs += "--collect", "XPlat Code Coverage"
        $testArgs += "--settings", "coverlet.runsettings"
    }

    if ($Verbose) {
        $testArgs += "--verbosity", "detailed"
    }

    if ($Watch) {
        Write-Host "Running tests in watch mode..." -ForegroundColor Yellow
        $testArgs = @(
            "watch",
            "test",
            $TestProject,
            "--configuration", $Configuration
        )
    }

    # Run tests
    Write-Host "Running tests..." -ForegroundColor Green
    & dotnet @testArgs
    $testExitCode = $LASTEXITCODE

    # Process results
    if ($testExitCode -eq 0) {
        Write-Host ""
        Write-Host "✓ All tests passed!" -ForegroundColor Green
        
        # Display test result files
        $testResults = Get-ChildItem $OutputDir -Filter "*.trx" -Recurse
        if ($testResults) {
            Write-Host "✓ Test results: $($testResults.Count) file(s)" -ForegroundColor Green
            $testResults | ForEach-Object {
                Write-Host "  - $($_.FullName)" -ForegroundColor Gray
            }
        }

        # Display coverage results
        if ($Coverage) {
            $coverageFiles = Get-ChildItem $OutputDir -Filter "coverage.cobertura.xml" -Recurse
            if ($coverageFiles) {
                Write-Host "✓ Coverage reports: $($coverageFiles.Count) file(s)" -ForegroundColor Green
                $coverageFiles | ForEach-Object {
                    Write-Host "  - $($_.FullName)" -ForegroundColor Gray
                }
            }
        }
    } else {
        Write-Host ""
        Write-Host "✗ Some tests failed!" -ForegroundColor Red
        
        # Try to display test summary
        $testResults = Get-ChildItem $OutputDir -Filter "*.trx" -Recurse | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($testResults) {
            Write-Host "Latest test results: $($testResults.FullName)" -ForegroundColor Yellow
        }
    }

    Write-Host ""
    Write-Host "Test artifacts available in: $OutputDir" -ForegroundColor Cyan
    
    # Exit with the same code as the tests
    exit $testExitCode

} catch {
    Write-Host ""
    Write-Host "✗ Test execution failed: $_" -ForegroundColor Red
    exit 1
}
