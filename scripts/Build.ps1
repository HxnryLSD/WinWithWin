<#
.SYNOPSIS
    Build script for WinWithWin
.DESCRIPTION
    Compiles the PowerShell modules and builds the C# GUI application.
#>

param(
    [Parameter()]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [Parameter()]
    [switch]$CompilePS,

    [Parameter()]
    [switch]$BuildGUI,

    [Parameter()]
    [switch]$All
)

$ErrorActionPreference = "Stop"
$projectRoot = $PSScriptRoot | Split-Path -Parent

Write-Host "`n╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║              WinWithWin Build Script                          ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

# Create output directory
$outDir = Join-Path $projectRoot "out"
if (-not (Test-Path $outDir)) {
    New-Item -Path $outDir -ItemType Directory -Force | Out-Null
}

# Compile PowerShell modules into single file
if ($CompilePS -or $All) {
    Write-Host "[BUILD] Compiling PowerShell modules..." -ForegroundColor Yellow
    
    $compiledScript = Join-Path $outDir "WinWithWin.Compiled.ps1"
    $content = @()
    
    # Add header
    $content += @"
<#
.SYNOPSIS
    WinWithWin - Windows 10/11 Multi-Function Tweaker (Compiled)
.DESCRIPTION
    Single-file compiled version of WinWithWin.
.NOTES
    Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
    Version: 1.0.0
#>

#Requires -Version 5.1
#Requires -RunAsAdministrator

"@

    # Add core modules
    $coreModules = @(
        "src\core\Helpers.psm1",
        "src\core\Safety.psm1",
        "src\core\WinWithWin.psm1"
    )

    foreach ($module in $coreModules) {
        $modulePath = Join-Path $projectRoot $module
        if (Test-Path $modulePath) {
            $content += "`n# === $module ===`n"
            $moduleContent = Get-Content $modulePath -Raw
            # Remove module-specific requirements and exports
            $moduleContent = $moduleContent -replace '#Requires -RunAsAdministrator', ''
            $moduleContent = $moduleContent -replace 'Export-ModuleMember.*', ''
            $content += $moduleContent
        }
    }

    # Add function modules
    $functionModules = @(
        "src\functions\Privacy.psm1",
        "src\functions\Performance.psm1",
        "src\functions\Security.psm1",
        "src\functions\Debloat.psm1"
    )

    foreach ($module in $functionModules) {
        $modulePath = Join-Path $projectRoot $module
        if (Test-Path $modulePath) {
            $content += "`n# === $module ===`n"
            $moduleContent = Get-Content $modulePath -Raw
            $moduleContent = $moduleContent -replace '#Requires -RunAsAdministrator', ''
            $moduleContent = $moduleContent -replace 'Export-ModuleMember.*', ''
            $moduleContent = $moduleContent -replace 'Import-Module.*', ''
            $content += $moduleContent
        }
    }

    # Add main script logic
    $content += @"

# === Main Entry Point ===

`$script:ProjectRoot = `$PSScriptRoot
`$script:ConfigPath = Join-Path `$PSScriptRoot "config"
`$script:LocalesPath = Join-Path `$PSScriptRoot "locales"

# Embed config
`$script:TweakRegistry = @{
    tweaks = @()
}

# Load tweaks.json if available
`$tweaksFile = Join-Path `$script:ConfigPath "tweaks.json"
if (Test-Path `$tweaksFile) {
    `$script:TweakRegistry = Get-Content `$tweaksFile -Raw | ConvertFrom-Json
}

Write-Host "`nWinWithWin Compiled Version - Ready`n" -ForegroundColor Green
Write-Host "Run 'Initialize-WinWithWin' to start.`n"

"@

    $content -join "`n" | Set-Content -Path $compiledScript -Encoding UTF8
    
    Write-Host "[OK] PowerShell compiled to: $compiledScript" -ForegroundColor Green
}

# Build C# GUI
if ($BuildGUI -or $All) {
    Write-Host "`n[BUILD] Building C# WPF GUI..." -ForegroundColor Yellow
    
    $guiProject = Join-Path $projectRoot "src\gui\WinWithWin.GUI\WinWithWin.GUI.csproj"
    
    if (-not (Test-Path $guiProject)) {
        Write-Warning "GUI project not found at: $guiProject"
    } else {
        # Check for dotnet
        $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
        if (-not $dotnet) {
            Write-Warning ".NET SDK not found. Please install .NET 6.0 SDK or later."
            Write-Host "Download from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
        } else {
            $publishDir = Join-Path $outDir "gui"
            
            Write-Host "  Restoring packages..." -ForegroundColor Gray
            dotnet restore $guiProject
            
            Write-Host "  Building project..." -ForegroundColor Gray
            dotnet build $guiProject -c $Configuration --no-restore
            
            Write-Host "  Publishing..." -ForegroundColor Gray
            dotnet publish $guiProject -c $Configuration -o $publishDir --no-build
            
            Write-Host "[OK] GUI published to: $publishDir" -ForegroundColor Green
        }
    }
}

# Copy config files
if ($CompilePS -or $BuildGUI -or $All) {
    Write-Host "`n[BUILD] Copying configuration files..." -ForegroundColor Yellow
    
    $configSrc = Join-Path $projectRoot "config"
    $configDst = Join-Path $outDir "config"
    
    if (Test-Path $configSrc) {
        Copy-Item -Path $configSrc -Destination $outDir -Recurse -Force
        Write-Host "[OK] Config files copied" -ForegroundColor Green
    }
    
    $localesSrc = Join-Path $projectRoot "locales"
    $localesDst = Join-Path $outDir "locales"
    
    if (Test-Path $localesSrc) {
        Copy-Item -Path $localesSrc -Destination $outDir -Recurse -Force
        Write-Host "[OK] Locale files copied" -ForegroundColor Green
    }
}

Write-Host "`n[COMPLETE] Build finished successfully!`n" -ForegroundColor Green
Write-Host "Output directory: $outDir" -ForegroundColor Cyan
Write-Host ""
