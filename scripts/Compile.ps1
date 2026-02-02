<#
.SYNOPSIS
    Quick compile script for PowerShell modules
.DESCRIPTION
    Creates a single compiled PS1 file for distribution
#>

$projectRoot = $PSScriptRoot | Split-Path -Parent
$outFile = Join-Path $projectRoot "out\WinWithWin.ps1"

# Ensure output directory exists
$outDir = Split-Path $outFile -Parent
if (-not (Test-Path $outDir)) {
    New-Item -Path $outDir -ItemType Directory -Force | Out-Null
}

Write-Host "Compiling WinWithWin..." -ForegroundColor Cyan

# Get all module content
$modules = @(
    "src\core\Helpers.psm1",
    "src\core\Safety.psm1", 
    "src\functions\Privacy.psm1",
    "src\functions\Performance.psm1",
    "src\functions\Security.psm1",
    "src\functions\Debloat.psm1",
    "src\core\WinWithWin.psm1"
)

$output = @()

# Header
$output += @"
#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
    WinWithWin - Windows 10/11 Multi-Function Tweaker
    Compiled: $(Get-Date -Format 'yyyy-MM-dd')
    https://github.com/yourusername/WinWithWin
#>

"@

foreach ($module in $modules) {
    $path = Join-Path $projectRoot $module
    if (Test-Path $path) {
        $content = Get-Content $path -Raw
        # Clean up module-specific stuff
        $content = $content -replace '#Requires.*', ''
        $content = $content -replace 'Export-ModuleMember.*', ''
        $content = $content -replace '\$helpersPath.*Import-Module.*', ''
        $output += "`n#region $module`n$content`n#endregion`n"
    }
}

# Add main entry
$mainScript = Join-Path $projectRoot "WinWithWin.ps1"
if (Test-Path $mainScript) {
    $mainContent = Get-Content $mainScript -Raw
    $mainContent = $mainContent -replace 'Import-Module \$modulePath.*', ''
    $output += "`n#region Main`n$mainContent`n#endregion`n"
}

$output -join "`n" | Set-Content $outFile -Encoding UTF8

Write-Host "Compiled to: $outFile" -ForegroundColor Green
Write-Host "Size: $([math]::Round((Get-Item $outFile).Length / 1KB, 2)) KB" -ForegroundColor Gray
