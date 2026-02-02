<#
.SYNOPSIS
    WinWithWin - Main Entry Point
.DESCRIPTION
    The main script that initializes WinWithWin and provides an interactive CLI interface.
.NOTES
    Run as Administrator: powershell -ExecutionPolicy Bypass -File WinWithWin.ps1
#>

#Requires -Version 5.1

param(
    [Parameter()]
    [ValidateSet("Balanced", "Gaming", "Privacy", "Minimal")]
    [string]$Preset,

    [Parameter()]
    [string]$Locale = "en",

    [Parameter()]
    [switch]$NoGui,

    [Parameter()]
    [switch]$Help
)

# Show help
if ($Help) {
    Write-Host @"

WinWithWin - Windows 10/11 Multi-Function Tweaker
================================================

Usage:
    .\WinWithWin.ps1 [options]

Options:
    -Preset <name>      Apply a preset (Balanced, Gaming, Privacy, Minimal)
    -Locale <code>      Set language (en, de)
    -NoGui              Run in console mode
    -Help               Show this help message

Examples:
    .\WinWithWin.ps1                    # Interactive mode
    .\WinWithWin.ps1 -Preset Gaming     # Apply Gaming preset
    .\WinWithWin.ps1 -Locale de         # German language

"@
    exit 0
}

# Check for admin rights
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Warning "WinWithWin requires Administrator privileges."
    Write-Host "`nRestarting as Administrator..." -ForegroundColor Yellow
    
    $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    if ($Preset) { $arguments += " -Preset $Preset" }
    if ($Locale) { $arguments += " -Locale $Locale" }
    if ($NoGui) { $arguments += " -NoGui" }
    
    Start-Process -FilePath "powershell.exe" -Verb RunAs -ArgumentList $arguments
    exit
}

# Import main module
$modulePath = Join-Path $PSScriptRoot "src\core\WinWithWin.psm1"
if (Test-Path $modulePath) {
    Import-Module $modulePath -Force -DisableNameChecking
} else {
    Write-Error "Core module not found at: $modulePath"
    exit 1
}

# Initialize
$initialized = Initialize-WinWithWin -Locale $Locale

if (-not $initialized) {
    Write-Error "Failed to initialize WinWithWin"
    exit 1
}

# Apply preset if specified
if ($Preset) {
    Write-Host "`n"
    $result = Invoke-TweakPreset -Preset $Preset -Confirm:$false
    
    if ($result) {
        Write-Host "`n[SUCCESS] " -ForegroundColor Green -NoNewline
        Write-Host "Preset '$Preset' has been applied."
        Write-Host "[INFO] " -ForegroundColor Cyan -NoNewline
        Write-Host "Some changes may require a restart to take effect.`n"
    }
    
    exit 0
}

# Interactive mode
function Show-MainMenu {
    Clear-Host
    Write-Host ""
    Write-Host "╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║                                                               ║" -ForegroundColor Cyan
    Write-Host "║   " -ForegroundColor Cyan -NoNewline
    Write-Host "WinWithWin - Windows Multi-Function Tweaker" -ForegroundColor White -NoNewline
    Write-Host "              ║" -ForegroundColor Cyan
    Write-Host "║                                                               ║" -ForegroundColor Cyan
    Write-Host "╚═══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
    
    $osInfo = Get-WindowsVersion
    Write-Host "  System: " -NoNewline
    Write-Host "$($osInfo.Name) (Build $($osInfo.Build))" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "  ┌─────────────────────────────────────────────────────────────┐" -ForegroundColor DarkGray
    Write-Host "  │                      " -ForegroundColor DarkGray -NoNewline
    Write-Host "MAIN MENU" -ForegroundColor White -NoNewline
    Write-Host "                           │" -ForegroundColor DarkGray
    Write-Host "  └─────────────────────────────────────────────────────────────┘" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "    [1] " -ForegroundColor Yellow -NoNewline
    Write-Host "Privacy Tweaks" -ForegroundColor White
    Write-Host "    [2] " -ForegroundColor Yellow -NoNewline
    Write-Host "Performance Tweaks" -ForegroundColor White
    Write-Host "    [3] " -ForegroundColor Yellow -NoNewline
    Write-Host "Security Tweaks" -ForegroundColor White
    Write-Host "    [4] " -ForegroundColor Yellow -NoNewline
    Write-Host "Debloat & Cleanup" -ForegroundColor White
    Write-Host ""
    Write-Host "  ┌─────────────────────────────────────────────────────────────┐" -ForegroundColor DarkGray
    Write-Host "  │                      " -ForegroundColor DarkGray -NoNewline
    Write-Host "PRESETS" -ForegroundColor Magenta -NoNewline
    Write-Host "                            │" -ForegroundColor DarkGray
    Write-Host "  └─────────────────────────────────────────────────────────────┘" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "    [B] " -ForegroundColor Magenta -NoNewline
    Write-Host "Balanced " -ForegroundColor White -NoNewline
    Write-Host "- Recommended for most users" -ForegroundColor Gray
    Write-Host "    [G] " -ForegroundColor Magenta -NoNewline
    Write-Host "Gaming " -ForegroundColor White -NoNewline
    Write-Host "- Maximum performance" -ForegroundColor Gray
    Write-Host "    [P] " -ForegroundColor Magenta -NoNewline
    Write-Host "Privacy " -ForegroundColor White -NoNewline
    Write-Host "- Maximum privacy protection" -ForegroundColor Gray
    Write-Host "    [M] " -ForegroundColor Magenta -NoNewline
    Write-Host "Minimal " -ForegroundColor White -NoNewline
    Write-Host "- Aggressive debloating" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  ┌─────────────────────────────────────────────────────────────┐" -ForegroundColor DarkGray
    Write-Host "  │                      " -ForegroundColor DarkGray -NoNewline
    Write-Host "SAFETY" -ForegroundColor Green -NoNewline
    Write-Host "                             │" -ForegroundColor DarkGray
    Write-Host "  └─────────────────────────────────────────────────────────────┘" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "    [R] " -ForegroundColor Green -NoNewline
    Write-Host "Create Restore Point"
    Write-Host "    [S] " -ForegroundColor Green -NoNewline
    Write-Host "System Stability Check"
    Write-Host "    [L] " -ForegroundColor Green -NoNewline
    Write-Host "View Logs"
    Write-Host ""
    Write-Host "    [Q] " -ForegroundColor Red -NoNewline
    Write-Host "Quit"
    Write-Host ""
}

function Show-TweakCategory {
    param([string]$Category)
    
    $tweaks = Get-TweakStatus -Category $Category
    
    Clear-Host
    Write-Host "`n  === $Category Tweaks ===" -ForegroundColor Cyan
    Write-Host ""
    
    $index = 1
    $tweakMap = @{}
    
    foreach ($tweak in $tweaks) {
        $status = if ($tweak.CurrentState) { "[ON] " } else { "[OFF]" }
        $statusColor = if ($tweak.CurrentState) { "Green" } else { "Gray" }
        $riskColor = switch ($tweak.Risk) {
            "Safe" { "Green" }
            "Moderate" { "Yellow" }
            "Advanced" { "Red" }
            default { "White" }
        }
        
        Write-Host "    [$index] " -ForegroundColor Yellow -NoNewline
        Write-Host $status -ForegroundColor $statusColor -NoNewline
        Write-Host " $($tweak.Name) " -NoNewline
        Write-Host "[$($tweak.Risk)]" -ForegroundColor $riskColor
        Write-Host "        $($tweak.Description)" -ForegroundColor Gray
        Write-Host ""
        
        $tweakMap[$index] = $tweak
        $index++
    }
    
    Write-Host "    [A] Apply All  [U] Undo All  [B] Back"
    Write-Host ""
    
    return $tweakMap
}

# Main loop
$running = $true

while ($running) {
    Show-MainMenu
    $choice = Read-Host "  Select an option"
    
    switch ($choice.ToUpper()) {
        "1" {
            $tweakMap = Show-TweakCategory -Category "Privacy"
            $subChoice = Read-Host "  Select tweak number or action"
            # Handle selection
        }
        "2" {
            $tweakMap = Show-TweakCategory -Category "Performance"
            $subChoice = Read-Host "  Select tweak number or action"
        }
        "3" {
            $tweakMap = Show-TweakCategory -Category "Security"
            $subChoice = Read-Host "  Select tweak number or action"
        }
        "4" {
            $tweakMap = Show-TweakCategory -Category "Debloat"
            $subChoice = Read-Host "  Select tweak number or action"
        }
        "B" {
            Invoke-TweakPreset -Preset "Balanced" -Confirm:$false
            Read-Host "`nPress Enter to continue"
        }
        "G" {
            Invoke-TweakPreset -Preset "Gaming" -Confirm:$false
            Read-Host "`nPress Enter to continue"
        }
        "P" {
            Invoke-TweakPreset -Preset "Privacy" -Confirm:$false
            Read-Host "`nPress Enter to continue"
        }
        "M" {
            Invoke-TweakPreset -Preset "Minimal" -Confirm:$false
            Read-Host "`nPress Enter to continue"
        }
        "R" {
            Write-Host "`n  Creating restore point..." -ForegroundColor Yellow
            New-SafetyRestorePoint -Description "WinWithWin Manual Backup"
            Read-Host "`nPress Enter to continue"
        }
        "S" {
            Write-Host "`n  Running system stability check..." -ForegroundColor Yellow
            $results = Test-SystemStability
            Write-Host "`n  Overall Status: $($results.OverallStatus)" -ForegroundColor $(if ($results.OverallStatus -eq "OK") { "Green" } else { "Red" })
            foreach ($check in $results.Checks) {
                $color = switch ($check.Status) {
                    "OK" { "Green" }
                    "Warning" { "Yellow" }
                    "Critical" { "Red" }
                    default { "White" }
                }
                Write-Host "    $($check.Name): $($check.Status)" -ForegroundColor $color
            }
            Read-Host "`nPress Enter to continue"
        }
        "L" {
            Write-Host "`n  Recent Log Entries:" -ForegroundColor Cyan
            $logs = Get-LogEntries -Last 20
            foreach ($log in $logs) {
                $color = switch ($log.Level) {
                    "INFO" { "Cyan" }
                    "WARNING" { "Yellow" }
                    "ERROR" { "Red" }
                    default { "White" }
                }
                Write-Host "    [$($log.Timestamp.ToString('HH:mm:ss'))] [$($log.Level)] $($log.Message)" -ForegroundColor $color
            }
            Read-Host "`nPress Enter to continue"
        }
        "Q" {
            $running = $false
            Write-Host "`n  Thank you for using WinWithWin!" -ForegroundColor Cyan
            Write-Host "  Some changes may require a restart to take effect.`n" -ForegroundColor Gray
        }
        default {
            Write-Host "`n  Invalid option. Please try again." -ForegroundColor Red
            Start-Sleep -Seconds 1
        }
    }
}
