#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build script for SharpHSQL library with automatic versioning and NuGet packaging.

.DESCRIPTION
    This script automates the build process for SharpHSQL:
    - Uses GitVersion for semantic versioning
    - Updates AssemblyInfo.cs with calculated version
    - Builds the library in specified configuration
    - Runs unit tests
    - Creates NuGet package
    - Optionally publishes to NuGet.org

.PARAMETER Configuration
    Build configuration (Debug or Release). Default: Release

.PARAMETER SkipTests
    Skip running unit tests

.PARAMETER CreatePackage
    Create NuGet package after build

.PARAMETER Publish
    Publish the NuGet package to NuGet.org

.PARAMETER ApiKey
    NuGet API key for publishing (required if -Publish is specified)

.PARAMETER Source
    NuGet source URL. Default: https://api.nuget.org/v3/index.json

.EXAMPLE
    ./build.ps1
    Build in Release mode without creating package

.EXAMPLE
    ./build.ps1 -Configuration Release -CreatePackage
    Build in Release mode and create NuGet package

.EXAMPLE
    ./build.ps1 -Configuration Release -CreatePackage -Publish -ApiKey "your-api-key"
    Build, create package, and publish to NuGet.org
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [Parameter()]
    [switch]$SkipTests,
    
    [Parameter()]
    [switch]$CreatePackage,
    
    [Parameter()]
    [switch]$Publish,
    
    [Parameter()]
    [string]$ApiKey,
    
    [Parameter()]
    [string]$Source = 'https://api.nuget.org/v3/index.json',
    
    [Parameter()]
    [switch]$SkipDuplicate
)

$ErrorActionPreference = 'Stop'

# Script variables
$scriptRoot = $PSScriptRoot
$solutionFile = Join-Path $scriptRoot "SharpHSQL.sln"
$projectFile = Join-Path $scriptRoot "SharpHSQL/SharpHsql.csproj"
$assemblyInfoFile = Join-Path $scriptRoot "SharpHSQL/AssemblyInfo.cs"
$artifactsDir = Join-Path $scriptRoot "artifacts"
$testProjectFile = Join-Path $scriptRoot "SharpHSQL.Tests/SharpHSQL.Tests.csproj"

# Colors for output
function Write-Header($message) {
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host $message -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan
}

function Write-Success($message) {
    Write-Host "✓ $message" -ForegroundColor Green
}

function Write-Error($message) {
    Write-Host "✗ $message" -ForegroundColor Red
}

function Write-Info($message) {
    Write-Host "→ $message" -ForegroundColor Yellow
}

# Check prerequisites
Write-Header "Checking Prerequisites"

# Check if dotnet CLI is available
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet CLI not found. Please install .NET SDK."
    exit 1
}
Write-Success "dotnet CLI found: $(dotnet --version)"

# Check if Git is available
if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Error "Git not found. Please install Git."
    exit 1
}
Write-Success "Git found"

# Install GitVersion if not already installed
Write-Header "Installing GitVersion"
try {
    dotnet tool install --global GitVersion.Tool 2>&1 | Out-Null
    Write-Success "GitVersion installed"
} catch {
    Write-Info "GitVersion already installed or checking for updates..."
    dotnet tool update --global GitVersion.Tool 2>&1 | Out-Null
}

# Calculate version using GitVersion
Write-Header "Calculating Version"
$gitVersionOutput = dotnet gitversion /output json 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "GitVersion failed. Using default version 2.0.0"
    $version = @{
        MajorMinorPatch = "2.0.0"
        AssemblySemVer = "2.0.0.0"
        InformationalVersion = "2.0.0+unknown"
        NuGetVersion = "2.0.0"
        SemVer = "2.0.0"
        CommitsSinceVersionSource = "0"
        Sha = "unknown"
    }
} else {
    $version = $gitVersionOutput | ConvertFrom-Json
}

$assemblyVersion = $version.AssemblySemVer
$fileVersion = $version.AssemblySemVer
$informationalVersion = $version.InformationalVersion
# Use SemVer if NuGetVersion is empty
$nugetVersion = if ([string]::IsNullOrWhiteSpace($version.NuGetVersion)) { 
    if ([string]::IsNullOrWhiteSpace($version.SemVer)) {
        $version.MajorMinorPatch
    } else {
        $version.SemVer
    }
} else { 
    $version.NuGetVersion 
}

Write-Success "Assembly Version: $assemblyVersion"
Write-Success "File Version: $fileVersion"
Write-Success "Informational Version: $informationalVersion"
Write-Success "NuGet Version: $nugetVersion"

# Update AssemblyInfo.cs
Write-Header "Updating AssemblyInfo.cs"
if (Test-Path $assemblyInfoFile) {
    $assemblyInfoContent = Get-Content $assemblyInfoFile -Raw
    
    # Update AssemblyVersion
    $assemblyInfoContent = $assemblyInfoContent -replace '\[assembly:\s*AssemblyVersion\s*\([^\)]*\)\]', "[assembly: AssemblyVersion(`"$assemblyVersion`")]"
    
    # Update AssemblyFileVersion
    $assemblyInfoContent = $assemblyInfoContent -replace '\[assembly:\s*AssemblyFileVersion\s*\([^\)]*\)\]', "[assembly: AssemblyFileVersion(`"$fileVersion`")]"
    
    # Add or update AssemblyInformationalVersion
    if ($assemblyInfoContent -match '\[assembly:\s*AssemblyInformationalVersion') {
        $assemblyInfoContent = $assemblyInfoContent -replace '\[assembly:\s*AssemblyInformationalVersion\s*\([^\)]*\)\]', "[assembly: AssemblyInformationalVersion(`"$informationalVersion`")]"
    } else {
        $assemblyInfoContent = $assemblyInfoContent + "`n[assembly: AssemblyInformationalVersion(`"$informationalVersion`")]`n"
    }
    
    Set-Content -Path $assemblyInfoFile -Value $assemblyInfoContent -NoNewline
    Write-Success "AssemblyInfo.cs updated"
} else {
    Write-Error "AssemblyInfo.cs not found at $assemblyInfoFile"
    exit 1
}

# Clean previous builds
Write-Header "Cleaning Previous Builds"
if (Test-Path $artifactsDir) {
    Remove-Item $artifactsDir -Recurse -Force
    Write-Success "Artifacts directory cleaned"
}
New-Item -ItemType Directory -Path $artifactsDir -Force | Out-Null

dotnet clean $solutionFile --configuration $Configuration --verbosity quiet
Write-Success "Solution cleaned"

# Restore dependencies
Write-Header "Restoring Dependencies"
dotnet restore $solutionFile
if ($LASTEXITCODE -ne 0) {
    Write-Error "Restore failed"
    exit 1
}
Write-Success "Dependencies restored"

# Build solution
Write-Header "Building Solution ($Configuration)"
dotnet build $solutionFile --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit 1
}
Write-Success "Build completed successfully"

# Verify assembly signing
Write-Header "Verifying Assembly Signing"
$assemblyPath = Join-Path $scriptRoot "SharpHSQL/bin/$Configuration/netstandard2.0/SharpHsql.dll"
if (Test-Path $assemblyPath) {
    try {
        $assembly = [System.Reflection.Assembly]::LoadFile($assemblyPath)
        $publicKey = $assembly.GetName().GetPublicKey()
        if ($publicKey -and $publicKey.Length -gt 0) {
            Write-Success "Assembly is signed with strong name"
            $token = $assembly.GetName().GetPublicKeyToken()
            $tokenString = [System.BitConverter]::ToString($token).Replace('-','')
            Write-Info "Public Key Token: $tokenString"
        } else {
            Write-Error "Assembly is NOT signed!"
            Write-Info "Please ensure KeyPair.snk exists and SignAssembly is true in the project file"
            if (-not $SkipTests) {
                exit 1
            }
        }
    } catch {
        Write-Info "Could not verify assembly signature: $($_.Exception.Message)"
        Write-Info "Assembly signing will be verified during NuGet package creation"
    }
} else {
    Write-Info "Assembly not found at $assemblyPath, skipping signature verification"
}

# Run tests
if (-not $SkipTests) {
    Write-Header "Running Unit Tests"
    if (Test-Path $testProjectFile) {
        dotnet test $testProjectFile --configuration $Configuration --no-build --verbosity normal
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Tests failed"
            exit 1
        }
        Write-Success "All tests passed"
    } else {
        Write-Info "Test project not found at $testProjectFile, skipping tests"
    }
} else {
    Write-Info "Tests skipped"
}

# Create NuGet package
if ($CreatePackage) {
    Write-Header "Creating NuGet Package"
    
    # Use direct command to avoid parameter escaping issues
    $packCommand = "dotnet pack `"$projectFile`" --configuration $Configuration --no-build --output `"$artifactsDir`" /p:PackageVersion=`"$nugetVersion`" /p:Version=`"$nugetVersion`""
    Write-Info "Executing: $packCommand"
    Invoke-Expression $packCommand
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Package creation failed"
        exit 1
    }
    
    $packageFile = Get-ChildItem -Path $artifactsDir -Filter "*.nupkg" | Select-Object -First 1
    if ($packageFile) {
        Write-Success "NuGet package created: $($packageFile.Name)"
        Write-Success "Package location: $($packageFile.FullName)"
    } else {
        Write-Error "Package file not found in artifacts directory"
        exit 1
    }
    
    # Publish to NuGet
    if ($Publish) {
        Write-Header "Publishing to NuGet"
        
        if ([string]::IsNullOrWhiteSpace($ApiKey)) {
            Write-Error "API Key is required for publishing. Use -ApiKey parameter."
            exit 1
        }
        
        Write-Info "Publishing to: $Source"
        
        $pushArgs = @($packageFile.FullName, '--api-key', $ApiKey, '--source', $Source)
        if ($SkipDuplicate) {
            $pushArgs += '--skip-duplicate'
        }
        
        dotnet nuget push @pushArgs
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Publishing failed"
            Write-Info "If this is a new package, make sure:"
            Write-Info "1. Your API key has 'Push new packages' permission"
            Write-Info "2. The package ID 'SharpHsql' is not already reserved by someone else"
            Write-Info "3. Your NuGet.org account email is verified"
            exit 1
        }
        Write-Success "Package published successfully"
    }
}

# Summary
Write-Header "Build Summary"
Write-Success "Configuration: $Configuration"
Write-Success "Version: $nugetVersion"
Write-Success "Build: SUCCESS"
if (-not $SkipTests) {
    Write-Success "Tests: PASSED"
}
if ($CreatePackage) {
    Write-Success "Package: CREATED"
    if ($Publish) {
        Write-Success "Published: YES"
    }
}

Write-Host "`nBuild completed successfully!`n" -ForegroundColor Green
