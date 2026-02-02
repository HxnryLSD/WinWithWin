<#
.SYNOPSIS
    WinWithWin - Windows 10/11 Multi-Function Tweaker Core Module
.DESCRIPTION
    Main module that orchestrates all tweaking functionality.
    Provides the core framework for loading modules, applying tweaks, and managing state.
.NOTES
    Version: 1.0.0
    Author: WinWithWin Contributors
    License: MIT
#>

#Requires -Version 5.1
#Requires -RunAsAdministrator

# Module variables
$script:ModuleRoot = $PSScriptRoot
$script:ProjectRoot = Split-Path -Parent (Split-Path -Parent $script:ModuleRoot)
$script:ConfigPath = Join-Path $script:ProjectRoot "config"
$script:LocalesPath = Join-Path $script:ProjectRoot "locales"
$script:CurrentLocale = "en"
$script:Localization = @{}
$script:TweakRegistry = @{}

# Import helper modules
$modulesToImport = @(
    "Safety.psm1",
    "Helpers.psm1"
)

foreach ($module in $modulesToImport) {
    $modulePath = Join-Path $script:ModuleRoot $module
    if (Test-Path $modulePath) {
        Import-Module $modulePath -Force -DisableNameChecking
    }
}

# Import function modules
$functionModules = @(
    "Privacy.psm1",
    "Performance.psm1",
    "Security.psm1",
    "Debloat.psm1"
)

$functionsPath = Join-Path (Split-Path -Parent $script:ModuleRoot) "functions"
foreach ($module in $functionModules) {
    $modulePath = Join-Path $functionsPath $module
    if (Test-Path $modulePath) {
        Import-Module $modulePath -Force -DisableNameChecking
    }
}

function Initialize-WinWithWin {
    <#
    .SYNOPSIS
        Initializes the WinWithWin environment
    .DESCRIPTION
        Loads configuration, localization, and verifies system requirements
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$Locale = "en"
    )

    Write-Host "`n" -NoNewline
    Write-Host "╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║                                                               ║" -ForegroundColor Cyan
    Write-Host "║   " -ForegroundColor Cyan -NoNewline
    Write-Host "WinWithWin - Windows Multi-Function Tweaker" -ForegroundColor White -NoNewline
    Write-Host "              ║" -ForegroundColor Cyan
    Write-Host "║   " -ForegroundColor Cyan -NoNewline
    Write-Host "Version 1.0.0" -ForegroundColor Gray -NoNewline
    Write-Host "                                              ║" -ForegroundColor Cyan
    Write-Host "║                                                               ║" -ForegroundColor Cyan
    Write-Host "╚═══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""

    # Check Windows version
    $osInfo = Get-WindowsVersion
    if (-not $osInfo.IsSupported) {
        Write-Warning "This Windows version may not be fully supported."
    }

    Write-Host "[INFO] " -ForegroundColor Cyan -NoNewline
    Write-Host "Detected: $($osInfo.Name) (Build $($osInfo.Build))"

    # Load localization
    Set-Locale -Locale $Locale

    # Load tweak configurations
    $tweaksFile = Join-Path $script:ConfigPath "tweaks.json"
    if (Test-Path $tweaksFile) {
        $script:TweakRegistry = Get-Content $tweaksFile -Raw | ConvertFrom-Json
        Write-Host "[INFO] " -ForegroundColor Cyan -NoNewline
        Write-Host "Loaded $($script:TweakRegistry.tweaks.Count) tweak definitions"
    }

    Write-Host "[OK] " -ForegroundColor Green -NoNewline
    Write-Host "WinWithWin initialized successfully`n"

    return $true
}

function Get-WindowsVersion {
    <#
    .SYNOPSIS
        Gets detailed Windows version information
    #>
    [CmdletBinding()]
    param()

    $os = Get-CimInstance -ClassName Win32_OperatingSystem
    $build = [System.Environment]::OSVersion.Version.Build

    $isWin11 = $build -ge 22000
    $isWin10 = $build -ge 18362 -and $build -lt 22000

    return [PSCustomObject]@{
        Name        = if ($isWin11) { "Windows 11" } elseif ($isWin10) { "Windows 10" } else { "Windows" }
        Build       = $build
        Version     = $os.Version
        Edition     = (Get-WindowsEdition -Online).Edition
        IsWin11     = $isWin11
        IsWin10     = $isWin10
        IsSupported = $isWin10 -or $isWin11
        Architecture = if ([Environment]::Is64BitOperatingSystem) { "x64" } else { "x86" }
    }
}

function Set-Locale {
    <#
    .SYNOPSIS
        Sets the current localization
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Locale
    )

    $localeFile = Join-Path $script:LocalesPath "$Locale.json"
    
    if (Test-Path $localeFile) {
        $script:CurrentLocale = $Locale
        $script:Localization = Get-Content $localeFile -Raw -Encoding UTF8 | ConvertFrom-Json
        Write-Host "[INFO] " -ForegroundColor Cyan -NoNewline
        Write-Host "Language set to: $($script:Localization.meta.name)"
    }
    else {
        Write-Warning "Locale '$Locale' not found, using English"
        $enFile = Join-Path $script:LocalesPath "en.json"
        if (Test-Path $enFile) {
            $script:Localization = Get-Content $enFile -Raw -Encoding UTF8 | ConvertFrom-Json
        }
    }
}

function Get-LocalizedString {
    <#
    .SYNOPSIS
        Gets a localized string by key
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Key,

        [Parameter()]
        [hashtable]$Variables = @{}
    )

    $keys = $Key -split '\.'
    $value = $script:Localization

    foreach ($k in $keys) {
        if ($null -ne $value.$k) {
            $value = $value.$k
        }
        else {
            return $Key # Return key if not found
        }
    }

    # Replace variables
    foreach ($var in $Variables.GetEnumerator()) {
        $value = $value -replace "\{$($var.Key)\}", $var.Value
    }

    return $value
}

function Get-TweakStatus {
    <#
    .SYNOPSIS
        Gets the current status of all tweaks or a specific tweak
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$Category,

        [Parameter()]
        [string]$TweakId
    )

    $results = @()

    foreach ($tweak in $script:TweakRegistry.tweaks) {
        if ($Category -and $tweak.category -ne $Category) { continue }
        if ($TweakId -and $tweak.id -ne $TweakId) { continue }

        # Get current state using the check function
        $checkFunction = "Test-$($tweak.id -replace '_','')"
        $currentState = $null

        if (Get-Command $checkFunction -ErrorAction SilentlyContinue) {
            try {
                $currentState = & $checkFunction
            }
            catch {
                $currentState = "Unknown"
            }
        }

        $results += [PSCustomObject]@{
            Id           = $tweak.id
            Name         = $tweak.name
            Category     = $tweak.category
            Risk         = $tweak.risk
            CurrentState = $currentState
            Description  = $tweak.description
        }
    }

    return $results
}

function Invoke-Tweak {
    <#
    .SYNOPSIS
        Applies a single tweak with safety checks
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string]$TweakId,

        [Parameter()]
        [switch]$Undo,

        [Parameter()]
        [switch]$Force,

        [Parameter()]
        [switch]$NoBackup
    )

    $tweak = $script:TweakRegistry.tweaks | Where-Object { $_.id -eq $TweakId }
    
    if (-not $tweak) {
        Write-Error "Tweak '$TweakId' not found"
        return $false
    }

    $actionName = if ($Undo) { "Undo" } else { "Apply" }
    $functionName = if ($Undo) { "Undo-$($TweakId -replace '_','')" } else { "Set-$($TweakId -replace '_','')" }

    if (-not (Get-Command $functionName -ErrorAction SilentlyContinue)) {
        Write-Error "Function '$functionName' not found"
        return $false
    }

    if ($PSCmdlet.ShouldProcess($tweak.name, $actionName)) {
        Write-Host "[$actionName] " -ForegroundColor Yellow -NoNewline
        Write-Host $tweak.name

        # Create backup unless disabled
        if (-not $NoBackup) {
            $backupResult = New-TweakBackup -TweakId $TweakId
            if (-not $backupResult) {
                if (-not $Force) {
                    Write-Error "Failed to create backup. Use -Force to skip."
                    return $false
                }
            }
        }

        try {
            & $functionName
            Write-Host "[OK] " -ForegroundColor Green -NoNewline
            Write-Host "$($tweak.name) - $actionName completed"
            return $true
        }
        catch {
            Write-Error "Failed to $actionName tweak: $_"
            return $false
        }
    }

    return $false
}

function Invoke-TweakPreset {
    <#
    .SYNOPSIS
        Applies a preset configuration
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [ValidateSet("Balanced", "Gaming", "Privacy", "Minimal")]
        [string]$Preset,

        [Parameter()]
        [switch]$Force
    )

    $presetFile = Join-Path $script:ConfigPath "presets\$Preset.json"
    
    if (-not (Test-Path $presetFile)) {
        Write-Error "Preset '$Preset' not found"
        return $false
    }

    $presetConfig = Get-Content $presetFile -Raw | ConvertFrom-Json

    Write-Host "`n[PRESET] " -ForegroundColor Magenta -NoNewline
    Write-Host "Applying '$Preset' preset..."
    Write-Host "[INFO] " -ForegroundColor Cyan -NoNewline
    Write-Host "$($presetConfig.description)`n"

    if ($PSCmdlet.ShouldProcess("System", "Apply $Preset preset")) {
        # Create system restore point
        if (-not $Force) {
            $restorePoint = New-SafetyRestorePoint -Description "WinWithWin - $Preset Preset"
            if (-not $restorePoint) {
                Write-Warning "Could not create restore point. Continue anyway? (Use -Force to skip)"
                $continue = Read-Host "Continue? (y/n)"
                if ($continue -ne 'y') { return $false }
            }
        }

        $successCount = 0
        $failCount = 0

        foreach ($tweakId in $presetConfig.tweaks) {
            $result = Invoke-Tweak -TweakId $tweakId -NoBackup -Force:$Force
            if ($result) { $successCount++ } else { $failCount++ }
        }

        Write-Host "`n[COMPLETE] " -ForegroundColor Green -NoNewline
        Write-Host "Preset applied: $successCount succeeded, $failCount failed`n"
    }

    return $true
}

function Show-TweakMenu {
    <#
    .SYNOPSIS
        Shows an interactive tweak selection menu
    #>
    [CmdletBinding()]
    param()

    $categories = $script:TweakRegistry.tweaks | Group-Object -Property category

    Write-Host "`n=== WinWithWin Tweak Menu ===" -ForegroundColor Cyan
    Write-Host ""

    $index = 1
    $menuItems = @{}

    foreach ($cat in $categories) {
        Write-Host "[$($cat.Name)]" -ForegroundColor Yellow
        foreach ($tweak in $cat.Group) {
            $riskColor = switch ($tweak.risk) {
                "Safe" { "Green" }
                "Moderate" { "Yellow" }
                "Advanced" { "Red" }
                default { "White" }
            }
            
            Write-Host "  $index. " -NoNewline
            Write-Host $tweak.name -NoNewline
            Write-Host " [$($tweak.risk)]" -ForegroundColor $riskColor
            
            $menuItems[$index] = $tweak.id
            $index++
        }
        Write-Host ""
    }

    Write-Host "P. Apply Preset" -ForegroundColor Magenta
    Write-Host "R. Create Restore Point" -ForegroundColor Blue
    Write-Host "S. Show Current Status" -ForegroundColor Cyan
    Write-Host "Q. Quit" -ForegroundColor Gray
    Write-Host ""

    return $menuItems
}

# Export functions
Export-ModuleMember -Function @(
    'Initialize-WinWithWin',
    'Get-WindowsVersion',
    'Set-Locale',
    'Get-LocalizedString',
    'Get-TweakStatus',
    'Invoke-Tweak',
    'Invoke-TweakPreset',
    'Show-TweakMenu'
)
