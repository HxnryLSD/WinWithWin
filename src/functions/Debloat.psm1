<#
.SYNOPSIS
    WinWithWin Debloat Module
.DESCRIPTION
    Contains functions for removing preinstalled apps and bloatware.
    All removals are reversible through Microsoft Store reinstallation.
.NOTES
    Apps are categorized by safety level for removal.
#>

#Requires -Version 5.1
#Requires -RunAsAdministrator

# Import helpers
$helpersPath = Join-Path (Split-Path -Parent $PSScriptRoot) "core\Helpers.psm1"
if (Test-Path $helpersPath) {
    Import-Module $helpersPath -Force -DisableNameChecking
}

# App categories
$script:BloatwareApps = @(
    # Third-party bloatware (always safe to remove)
    @{ Name = "*.CandyCrush*"; Category = "ThirdParty"; SafeToRemove = $true },
    @{ Name = "*king.com*"; Category = "ThirdParty"; SafeToRemove = $true },
    @{ Name = "*Disney*"; Category = "ThirdParty"; SafeToRemove = $true },
    @{ Name = "*Netflix*"; Category = "ThirdParty"; SafeToRemove = $true },
    @{ Name = "*Spotify*"; Category = "ThirdParty"; SafeToRemove = $true },
    @{ Name = "*TikTok*"; Category = "ThirdParty"; SafeToRemove = $true },
    @{ Name = "*Facebook*"; Category = "ThirdParty"; SafeToRemove = $true },
    @{ Name = "*Twitter*"; Category = "ThirdParty"; SafeToRemove = $true },
    @{ Name = "*Instagram*"; Category = "ThirdParty"; SafeToRemove = $true },
    @{ Name = "*Amazon*"; Category = "ThirdParty"; SafeToRemove = $true },
    @{ Name = "*Clipchamp*"; Category = "ThirdParty"; SafeToRemove = $true },
    @{ Name = "*LinkedInforWindows*"; Category = "ThirdParty"; SafeToRemove = $true },

    # Microsoft apps that are safe to remove
    @{ Name = "Microsoft.3DBuilder"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.Microsoft3DViewer"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.MixedReality.Portal"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.Print3D"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.BingNews"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.BingWeather"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.BingFinance"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.BingSports"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.GetHelp"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.Getstarted"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.Messaging"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.MicrosoftOfficeHub"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.MicrosoftSolitaireCollection"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.OneConnect"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.People"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.SkypeApp"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.Office.Sway"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.Wallet"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.WindowsFeedbackHub"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.WindowsMaps"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.ZuneMusic"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.ZuneVideo"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.YourPhone"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.WindowsAlarms"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.WindowsSoundRecorder"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.Todos"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.PowerAutomateDesktop"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "MicrosoftTeams"; Category = "Microsoft"; SafeToRemove = $true },
    @{ Name = "Microsoft.MicrosoftStickyNotes"; Category = "Microsoft"; SafeToRemove = $true },

    # Xbox apps (optional - only if not gaming)
    @{ Name = "Microsoft.Xbox.TCUI"; Category = "Xbox"; SafeToRemove = $false },
    @{ Name = "Microsoft.XboxApp"; Category = "Xbox"; SafeToRemove = $false },
    @{ Name = "Microsoft.XboxGameOverlay"; Category = "Xbox"; SafeToRemove = $false },
    @{ Name = "Microsoft.XboxGamingOverlay"; Category = "Xbox"; SafeToRemove = $false },
    @{ Name = "Microsoft.XboxIdentityProvider"; Category = "Xbox"; SafeToRemove = $false },
    @{ Name = "Microsoft.XboxSpeechToTextOverlay"; Category = "Xbox"; SafeToRemove = $false },

    # Copilot/AI apps (Windows 11)
    @{ Name = "Microsoft.Copilot"; Category = "AI"; SafeToRemove = $true },
    @{ Name = "Microsoft.Windows.Ai.Copilot.Provider"; Category = "AI"; SafeToRemove = $true }
)

# Apps that should NEVER be removed (system dependencies)
$script:ProtectedApps = @(
    "Microsoft.WindowsStore",
    "Microsoft.StorePurchaseApp",
    "Microsoft.DesktopAppInstaller",
    "Microsoft.WindowsCalculator",
    "Microsoft.Windows.Photos",
    "Microsoft.WindowsCamera",
    "Microsoft.WindowsNotepad",
    "Microsoft.Paint",
    "Microsoft.ScreenSketch",
    "Microsoft.Windows.Terminal",
    "Microsoft.WindowsTerminal",
    "Microsoft.WebMediaExtensions",
    "Microsoft.HEIFImageExtension",
    "Microsoft.VP9VideoExtensions",
    "Microsoft.WebpImageExtension",
    "Microsoft.VCLibs*",
    "Microsoft.NET*",
    "Microsoft.UI.Xaml*",
    "Microsoft.Services.Store.Engagement",
    "Microsoft.WindowsAppRuntime*"
)

function Get-InstalledBloatware {
    <#
    .SYNOPSIS
        Gets a list of installed bloatware apps
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [switch]$IncludeXbox,

        [Parameter()]
        [switch]$AllUsers
    )

    $installedApps = if ($AllUsers) {
        Get-AppxPackage -AllUsers
    } else {
        Get-AppxPackage
    }

    $bloatware = @()

    foreach ($app in $script:BloatwareApps) {
        if ($app.Category -eq "Xbox" -and -not $IncludeXbox) { continue }

        $matchingApps = $installedApps | Where-Object { $_.Name -like $app.Name }
        
        foreach ($match in $matchingApps) {
            $bloatware += [PSCustomObject]@{
                Name = $match.Name
                DisplayName = $match.Name -replace "Microsoft\.", ""
                PackageFullName = $match.PackageFullName
                Category = $app.Category
                SafeToRemove = $app.SafeToRemove
                Version = $match.Version
            }
        }
    }

    return $bloatware | Sort-Object -Property Category, Name
}

function Test-RemoveBloatware {
    <#
    .SYNOPSIS
        Tests if bloatware has been removed
    #>
    [CmdletBinding()]
    param()

    $bloatware = Get-InstalledBloatware
    return $bloatware.Count -eq 0
}

function Set-RemoveBloatware {
    <#
    .SYNOPSIS
        Removes common bloatware apps
    .DESCRIPTION
        Removes third-party bloatware and optional Microsoft apps.
        Protected system apps are never removed.
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [switch]$IncludeXbox,

        [Parameter()]
        [switch]$AllUsers,

        [Parameter()]
        [switch]$Aggressive
    )

    $bloatware = Get-InstalledBloatware -IncludeXbox:$IncludeXbox -AllUsers:$AllUsers

    if ($bloatware.Count -eq 0) {
        Write-Host "[INFO] " -ForegroundColor Cyan -NoNewline
        Write-Host "No bloatware found to remove"
        return $true
    }

    Write-Host "[INFO] " -ForegroundColor Cyan -NoNewline
    Write-Host "Found $($bloatware.Count) bloatware apps to remove"

    $removed = 0
    $failed = 0

    foreach ($app in $bloatware) {
        # Skip unsafe apps unless aggressive mode
        if (-not $app.SafeToRemove -and -not $Aggressive) {
            Write-Verbose "Skipping $($app.Name) (not marked as safe)"
            continue
        }

        try {
            Write-Host "  Removing: $($app.DisplayName)... " -NoNewline

            if ($AllUsers) {
                Get-AppxPackage -Name $app.Name -AllUsers | Remove-AppxPackage -AllUsers -ErrorAction Stop
            } else {
                Get-AppxPackage -Name $app.Name | Remove-AppxPackage -ErrorAction Stop
            }

            # Also remove provisioned package
            Get-AppxProvisionedPackage -Online | 
                Where-Object { $_.DisplayName -eq $app.Name } | 
                Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue | Out-Null

            Write-Host "Done" -ForegroundColor Green
            $removed++
        }
        catch {
            Write-Host "Failed" -ForegroundColor Red
            Write-Verbose "Error removing $($app.Name): $_"
            $failed++
        }
    }

    Write-Host "`n[COMPLETE] " -ForegroundColor Green -NoNewline
    Write-Host "Removed $removed apps, $failed failed"

    return $true
}

function Undo-RemoveBloatware {
    <#
    .SYNOPSIS
        Shows instructions to reinstall removed apps
    #>
    [CmdletBinding()]
    param()

    Write-Host @"

To reinstall removed apps:

1. Open Microsoft Store
2. Search for the app name
3. Click 'Install' or 'Get'

Or run this command to reinstall all default Windows apps:
    Get-AppxPackage -AllUsers| Foreach {Add-AppxPackage -DisableDevelopmentMode -Register "`$(`$_.InstallLocation)\AppXManifest.xml"}

"@ -ForegroundColor Yellow

    return $true
}

#region OneDrive

function Test-RemoveOneDrive {
    <#
    .SYNOPSIS
        Tests if OneDrive is removed
    #>
    [CmdletBinding()]
    param()

    $oneDrive = Get-Process -Name "OneDrive" -ErrorAction SilentlyContinue
    $oneDriveSetup = Test-Path "$env:SYSTEMROOT\System32\OneDriveSetup.exe"
    
    return (-not $oneDrive) -and (-not $oneDriveSetup)
}

function Set-RemoveOneDrive {
    <#
    .SYNOPSIS
        Uninstalls OneDrive
    .DESCRIPTION
        Completely removes OneDrive from the system.
        User data in OneDrive folder is preserved.
    #>
    [CmdletBinding()]
    param()

    Write-Host "[INFO] " -ForegroundColor Cyan -NoNewline
    Write-Host "Stopping OneDrive..."

    # Stop OneDrive process
    Stop-Process -Name "OneDrive" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2

    # Uninstall OneDrive
    Write-Host "[INFO] " -ForegroundColor Cyan -NoNewline
    Write-Host "Uninstalling OneDrive..."

    $oneDriveSetup = "$env:SYSTEMROOT\System32\OneDriveSetup.exe"
    if (-not (Test-Path $oneDriveSetup)) {
        $oneDriveSetup = "$env:SYSTEMROOT\SysWOW64\OneDriveSetup.exe"
    }

    if (Test-Path $oneDriveSetup) {
        Start-Process $oneDriveSetup -ArgumentList "/uninstall" -Wait -NoNewWindow
    }

    # Clean up folders
    $foldersToRemove = @(
        "$env:LOCALAPPDATA\Microsoft\OneDrive",
        "$env:PROGRAMDATA\Microsoft OneDrive",
        "$env:USERPROFILE\OneDrive"  # Only removes if empty
    )

    foreach ($folder in $foldersToRemove) {
        if (Test-Path $folder) {
            try {
                Remove-Item -Path $folder -Recurse -Force -ErrorAction SilentlyContinue
            }
            catch {
                Write-Verbose "Could not remove $folder"
            }
        }
    }

    # Remove from Explorer sidebar
    Set-RegistryValue -Path "HKCR:\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}" -Name "System.IsPinnedToNameSpaceTree" -Value 0 -Type DWord
    Set-RegistryValue -Path "HKCR:\Wow6432Node\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}" -Name "System.IsPinnedToNameSpaceTree" -Value 0 -Type DWord

    # Disable OneDrive via Group Policy
    Set-RegistryValue -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\OneDrive" -Name "DisableFileSyncNGSC" -Value 1 -Type DWord

    Write-Host "[OK] " -ForegroundColor Green -NoNewline
    Write-Host "OneDrive has been removed"

    return $true
}

function Undo-RemoveOneDrive {
    <#
    .SYNOPSIS
        Shows instructions to reinstall OneDrive
    #>
    [CmdletBinding()]
    param()

    # Re-enable OneDrive
    Remove-RegistryValue -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\OneDrive" -Name "DisableFileSyncNGSC"

    Write-Host @"

To reinstall OneDrive:

1. Download from: https://www.microsoft.com/microsoft-365/onedrive/download
2. Or run: winget install Microsoft.OneDrive

"@ -ForegroundColor Yellow

    return $true
}

#endregion

#region Edge

function Test-DisableEdgeFeatures {
    <#
    .SYNOPSIS
        Tests if Edge features are disabled
    #>
    [CmdletBinding()]
    param()

    $value = Get-RegistryValue -Path "HKLM:\SOFTWARE\Policies\Microsoft\Edge" -Name "HubsSidebarEnabled" -DefaultValue 1
    return $value -eq 0
}

function Set-DisableEdgeFeatures {
    <#
    .SYNOPSIS
        Disables annoying Edge features
    .DESCRIPTION
        Disables sidebar, shopping features, and other annoyances.
        Does NOT remove Edge (it's a system component).
    #>
    [CmdletBinding()]
    param()

    $edgePath = "HKLM:\SOFTWARE\Policies\Microsoft\Edge"

    # Disable sidebar/hub
    Set-RegistryValue -Path $edgePath -Name "HubsSidebarEnabled" -Value 0 -Type DWord

    # Disable shopping features
    Set-RegistryValue -Path $edgePath -Name "EdgeShoppingAssistantEnabled" -Value 0 -Type DWord

    # Disable collections
    Set-RegistryValue -Path $edgePath -Name "EdgeCollectionsEnabled" -Value 0 -Type DWord

    # Disable first run experience
    Set-RegistryValue -Path $edgePath -Name "HideFirstRunExperience" -Value 1 -Type DWord

    # Disable default browser prompt
    Set-RegistryValue -Path $edgePath -Name "DefaultBrowserSettingEnabled" -Value 0 -Type DWord

    # Disable importing from other browsers
    Set-RegistryValue -Path $edgePath -Name "ImportAutofillFormData" -Value 0 -Type DWord
    Set-RegistryValue -Path $edgePath -Name "ImportBrowserSettings" -Value 0 -Type DWord
    Set-RegistryValue -Path $edgePath -Name "ImportFavorites" -Value 0 -Type DWord
    Set-RegistryValue -Path $edgePath -Name "ImportHistory" -Value 0 -Type DWord

    return $true
}

function Undo-DisableEdgeFeatures {
    <#
    .SYNOPSIS
        Re-enables Edge features
    #>
    [CmdletBinding()]
    param()

    $edgePath = "HKLM:\SOFTWARE\Policies\Microsoft\Edge"

    $valuesToRemove = @(
        "HubsSidebarEnabled",
        "EdgeShoppingAssistantEnabled",
        "EdgeCollectionsEnabled",
        "HideFirstRunExperience",
        "DefaultBrowserSettingEnabled",
        "ImportAutofillFormData",
        "ImportBrowserSettings",
        "ImportFavorites",
        "ImportHistory"
    )

    foreach ($value in $valuesToRemove) {
        Remove-RegistryValue -Path $edgePath -Name $value
    }

    return $true
}

#endregion

#region Context Menu

function Test-RestoreClassicContextMenu {
    <#
    .SYNOPSIS
        Tests if classic context menu is restored (Windows 11)
    #>
    [CmdletBinding()]
    param()

    $build = [System.Environment]::OSVersion.Version.Build
    if ($build -lt 22000) { return $true } # Not Windows 11

    $value = Get-RegistryValue -Path "HKCU:\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32" -Name "(Default)" -DefaultValue $null

    return $value -eq ""
}

function Set-RestoreClassicContextMenu {
    <#
    .SYNOPSIS
        Restores the classic Windows 10 context menu in Windows 11
    #>
    [CmdletBinding()]
    param()

    $build = [System.Environment]::OSVersion.Version.Build
    if ($build -lt 22000) {
        Write-Host "[INFO] " -ForegroundColor Cyan -NoNewline
        Write-Host "This tweak is only for Windows 11"
        return $true
    }

    # Create the registry key to restore classic context menu
    $path = "HKCU:\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32"
    
    if (-not (Test-Path $path)) {
        New-Item -Path $path -Force | Out-Null
    }
    
    Set-ItemProperty -Path $path -Name "(Default)" -Value "" -Force

    Write-Host "[INFO] " -ForegroundColor Yellow -NoNewline
    Write-Host "Restart Explorer or log out for changes to take effect"

    return $true
}

function Undo-RestoreClassicContextMenu {
    <#
    .SYNOPSIS
        Restores the Windows 11 context menu
    #>
    [CmdletBinding()]
    param()

    $path = "HKCU:\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}"
    
    if (Test-Path $path) {
        Remove-Item -Path $path -Recurse -Force -ErrorAction SilentlyContinue
    }

    return $true
}

#endregion

#region Widgets

function Test-DisableWidgets {
    <#
    .SYNOPSIS
        Tests if Widgets are disabled
    #>
    [CmdletBinding()]
    param()

    $value = Get-RegistryValue -Path "HKLM:\SOFTWARE\Policies\Microsoft\Dsh" -Name "AllowNewsAndInterests" -DefaultValue 1
    return $value -eq 0
}

function Set-DisableWidgets {
    <#
    .SYNOPSIS
        Disables Windows Widgets/News and Interests
    #>
    [CmdletBinding()]
    param()

    # Windows 11 Widgets
    Set-RegistryValue -Path "HKLM:\SOFTWARE\Policies\Microsoft\Dsh" -Name "AllowNewsAndInterests" -Value 0 -Type DWord

    # Windows 10 News and Interests
    Set-RegistryValue -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Feeds" -Name "EnableFeeds" -Value 0 -Type DWord

    # Hide from taskbar
    Set-RegistryValue -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" -Name "TaskbarDa" -Value 0 -Type DWord

    return $true
}

function Undo-DisableWidgets {
    <#
    .SYNOPSIS
        Re-enables Widgets
    #>
    [CmdletBinding()]
    param()

    Remove-RegistryValue -Path "HKLM:\SOFTWARE\Policies\Microsoft\Dsh" -Name "AllowNewsAndInterests"
    Remove-RegistryValue -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Feeds" -Name "EnableFeeds"
    Set-RegistryValue -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" -Name "TaskbarDa" -Value 1 -Type DWord

    return $true
}

#endregion

# Export functions
Export-ModuleMember -Function @(
    # Bloatware
    'Get-InstalledBloatware', 'Test-RemoveBloatware', 'Set-RemoveBloatware', 'Undo-RemoveBloatware',
    # OneDrive
    'Test-RemoveOneDrive', 'Set-RemoveOneDrive', 'Undo-RemoveOneDrive',
    # Edge
    'Test-DisableEdgeFeatures', 'Set-DisableEdgeFeatures', 'Undo-DisableEdgeFeatures',
    # Context Menu
    'Test-RestoreClassicContextMenu', 'Set-RestoreClassicContextMenu', 'Undo-RestoreClassicContextMenu',
    # Widgets
    'Test-DisableWidgets', 'Set-DisableWidgets', 'Undo-DisableWidgets'
)
