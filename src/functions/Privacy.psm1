<#
.SYNOPSIS
    WinWithWin Privacy Tweaks Module
.DESCRIPTION
    Contains all privacy-related tweaks including telemetry, advertising, and data collection settings.
    Each tweak has a corresponding Test, Set, and Undo function.
.NOTES
    All tweaks are reversible and follow Microsoft's documented methods.
#>

#Requires -Version 5.1
#Requires -RunAsAdministrator

# Import helpers
$helpersPath = Join-Path (Split-Path -Parent $PSScriptRoot) "core\Helpers.psm1"
if (Test-Path $helpersPath) {
    Import-Module $helpersPath -Force -DisableNameChecking
}

#region Telemetry

function Test-DisableTelemetry {
    <#
    .SYNOPSIS
        Tests if telemetry is disabled
    #>
    [CmdletBinding()]
    param()

    $telemetryPath = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection"
    $value = Get-RegistryValue -Path $telemetryPath -Name "AllowTelemetry" -DefaultValue 1

    return $value -eq 0
}

function Set-DisableTelemetry {
    <#
    .SYNOPSIS
        Disables Windows telemetry
    .DESCRIPTION
        Sets telemetry to Security level (0) which is the minimum for Enterprise/Education
        For Home/Pro, this sets it to Basic (1) which is the minimum allowed
    #>
    [CmdletBinding()]
    param()

    $registryPaths = @(
        @{
            Path = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection"
            Name = "AllowTelemetry"
            Value = 0
            Type = "DWord"
        },
        @{
            Path = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection"
            Name = "AllowTelemetry"
            Value = 0
            Type = "DWord"
        },
        @{
            Path = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection"
            Name = "MaxTelemetryAllowed"
            Value = 0
            Type = "DWord"
        }
    )

    foreach ($reg in $registryPaths) {
        Set-RegistryValue -Path $reg.Path -Name $reg.Name -Value $reg.Value -Type $reg.Type
    }

    # Disable DiagTrack service
    Set-ServiceStartup -ServiceName "DiagTrack" -StartupType Disabled -StopService

    # Disable dmwappushservice
    Set-ServiceStartup -ServiceName "dmwappushservice" -StartupType Disabled -StopService

    return $true
}

function Undo-DisableTelemetry {
    <#
    .SYNOPSIS
        Re-enables Windows telemetry to default level
    #>
    [CmdletBinding()]
    param()

    # Set to Basic (1) - default for most users
    Set-RegistryValue -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection" -Name "AllowTelemetry" -Value 1 -Type DWord
    Set-RegistryValue -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection" -Name "AllowTelemetry" -Value 1 -Type DWord
    Remove-RegistryValue -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection" -Name "MaxTelemetryAllowed"

    # Re-enable services
    Set-ServiceStartup -ServiceName "DiagTrack" -StartupType Automatic
    Set-ServiceStartup -ServiceName "dmwappushservice" -StartupType Automatic

    return $true
}

#endregion

#region Advertising ID

function Test-DisableAdvertisingId {
    <#
    .SYNOPSIS
        Tests if Advertising ID is disabled
    #>
    [CmdletBinding()]
    param()

    $path = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo"
    $value = Get-RegistryValue -Path $path -Name "DisabledByGroupPolicy" -DefaultValue 0

    return $value -eq 1
}

function Set-DisableAdvertisingId {
    <#
    .SYNOPSIS
        Disables Windows Advertising ID
    #>
    [CmdletBinding()]
    param()

    # System-wide policy
    Set-RegistryValue -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo" -Name "DisabledByGroupPolicy" -Value 1 -Type DWord

    # Current user setting
    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo" -Name "Enabled" -Value 0 -Type DWord

    return $true
}

function Undo-DisableAdvertisingId {
    <#
    .SYNOPSIS
        Re-enables Advertising ID
    #>
    [CmdletBinding()]
    param()

    Remove-RegistryValue -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo" -Name "DisabledByGroupPolicy"
    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo" -Name "Enabled" -Value 1 -Type DWord

    return $true
}

#endregion

#region Activity History

function Test-DisableActivityHistory {
    <#
    .SYNOPSIS
        Tests if Activity History is disabled
    #>
    [CmdletBinding()]
    param()

    $path = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\System"
    $value = Get-RegistryValue -Path $path -Name "EnableActivityFeed" -DefaultValue 1

    return $value -eq 0
}

function Set-DisableActivityHistory {
    <#
    .SYNOPSIS
        Disables Activity History and Timeline
    #>
    [CmdletBinding()]
    param()

    $settings = @(
        @{ Path = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\System"; Name = "EnableActivityFeed"; Value = 0 },
        @{ Path = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\System"; Name = "PublishUserActivities"; Value = 0 },
        @{ Path = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\System"; Name = "UploadUserActivities"; Value = 0 }
    )

    foreach ($setting in $settings) {
        Set-RegistryValue -Path $setting.Path -Name $setting.Name -Value $setting.Value -Type DWord
    }

    return $true
}

function Undo-DisableActivityHistory {
    <#
    .SYNOPSIS
        Re-enables Activity History
    #>
    [CmdletBinding()]
    param()

    $path = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\System"
    Remove-RegistryValue -Path $path -Name "EnableActivityFeed"
    Remove-RegistryValue -Path $path -Name "PublishUserActivities"
    Remove-RegistryValue -Path $path -Name "UploadUserActivities"

    return $true
}

#endregion

#region Location Tracking

function Test-DisableLocationTracking {
    <#
    .SYNOPSIS
        Tests if Location Tracking is disabled
    #>
    [CmdletBinding()]
    param()

    $path = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors"
    $value = Get-RegistryValue -Path $path -Name "DisableLocation" -DefaultValue 0

    return $value -eq 1
}

function Set-DisableLocationTracking {
    <#
    .SYNOPSIS
        Disables Location Tracking
    #>
    [CmdletBinding()]
    param()

    $settings = @(
        @{ Path = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors"; Name = "DisableLocation"; Value = 1 },
        @{ Path = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors"; Name = "DisableLocationScripting"; Value = 1 },
        @{ Path = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors"; Name = "DisableWindowsLocationProvider"; Value = 1 }
    )

    foreach ($setting in $settings) {
        Set-RegistryValue -Path $setting.Path -Name $setting.Name -Value $setting.Value -Type DWord
    }

    return $true
}

function Undo-DisableLocationTracking {
    <#
    .SYNOPSIS
        Re-enables Location Tracking
    #>
    [CmdletBinding()]
    param()

    $path = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors"
    Remove-RegistryValue -Path $path -Name "DisableLocation"
    Remove-RegistryValue -Path $path -Name "DisableLocationScripting"
    Remove-RegistryValue -Path $path -Name "DisableWindowsLocationProvider"

    return $true
}

#endregion

#region Feedback & Diagnostics

function Test-DisableFeedback {
    <#
    .SYNOPSIS
        Tests if Feedback prompts are disabled
    #>
    [CmdletBinding()]
    param()

    $path = "HKCU:\SOFTWARE\Microsoft\Siuf\Rules"
    $value = Get-RegistryValue -Path $path -Name "NumberOfSIUFInPeriod" -DefaultValue 1

    return $value -eq 0
}

function Set-DisableFeedback {
    <#
    .SYNOPSIS
        Disables Windows Feedback prompts
    #>
    [CmdletBinding()]
    param()

    # Disable feedback prompts
    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Siuf\Rules" -Name "NumberOfSIUFInPeriod" -Value 0 -Type DWord
    
    # Disable feedback frequency
    Set-RegistryValue -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection" -Name "DoNotShowFeedbackNotifications" -Value 1 -Type DWord

    return $true
}

function Undo-DisableFeedback {
    <#
    .SYNOPSIS
        Re-enables Feedback prompts
    #>
    [CmdletBinding()]
    param()

    Remove-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Siuf\Rules" -Name "NumberOfSIUFInPeriod"
    Remove-RegistryValue -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection" -Name "DoNotShowFeedbackNotifications"

    return $true
}

#endregion

#region Tailored Experiences

function Test-DisableTailoredExperiences {
    <#
    .SYNOPSIS
        Tests if Tailored Experiences are disabled
    #>
    [CmdletBinding()]
    param()

    $path = "HKCU:\SOFTWARE\Policies\Microsoft\Windows\CloudContent"
    $value = Get-RegistryValue -Path $path -Name "DisableTailoredExperiencesWithDiagnosticData" -DefaultValue 0

    return $value -eq 1
}

function Set-DisableTailoredExperiences {
    <#
    .SYNOPSIS
        Disables Tailored Experiences based on diagnostic data
    #>
    [CmdletBinding()]
    param()

    Set-RegistryValue -Path "HKCU:\SOFTWARE\Policies\Microsoft\Windows\CloudContent" -Name "DisableTailoredExperiencesWithDiagnosticData" -Value 1 -Type DWord

    return $true
}

function Undo-DisableTailoredExperiences {
    <#
    .SYNOPSIS
        Re-enables Tailored Experiences
    #>
    [CmdletBinding()]
    param()

    Remove-RegistryValue -Path "HKCU:\SOFTWARE\Policies\Microsoft\Windows\CloudContent" -Name "DisableTailoredExperiencesWithDiagnosticData"

    return $true
}

#endregion

#region Typing & Inking Data

function Test-DisableTypingData {
    <#
    .SYNOPSIS
        Tests if Typing/Inking data collection is disabled
    #>
    [CmdletBinding()]
    param()

    $path = "HKCU:\SOFTWARE\Microsoft\Input\TIPC"
    $value = Get-RegistryValue -Path $path -Name "Enabled" -DefaultValue 1

    return $value -eq 0
}

function Set-DisableTypingData {
    <#
    .SYNOPSIS
        Disables typing and inking data collection
    #>
    [CmdletBinding()]
    param()

    # Disable typing data
    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Input\TIPC" -Name "Enabled" -Value 0 -Type DWord

    # Disable inking personalization
    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Personalization\Settings" -Name "AcceptedPrivacyPolicy" -Value 0 -Type DWord
    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\InputPersonalization" -Name "RestrictImplicitTextCollection" -Value 1 -Type DWord
    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\InputPersonalization" -Name "RestrictImplicitInkCollection" -Value 1 -Type DWord
    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\InputPersonalization\TrainedDataStore" -Name "HarvestContacts" -Value 0 -Type DWord

    return $true
}

function Undo-DisableTypingData {
    <#
    .SYNOPSIS
        Re-enables typing data collection
    #>
    [CmdletBinding()]
    param()

    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Input\TIPC" -Name "Enabled" -Value 1 -Type DWord
    Remove-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Personalization\Settings" -Name "AcceptedPrivacyPolicy"
    Remove-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\InputPersonalization" -Name "RestrictImplicitTextCollection"
    Remove-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\InputPersonalization" -Name "RestrictImplicitInkCollection"
    Remove-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\InputPersonalization\TrainedDataStore" -Name "HarvestContacts"

    return $true
}

#endregion

#region App Launch Tracking

function Test-DisableAppLaunchTracking {
    <#
    .SYNOPSIS
        Tests if App Launch Tracking is disabled
    #>
    [CmdletBinding()]
    param()

    $path = "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced"
    $value = Get-RegistryValue -Path $path -Name "Start_TrackProgs" -DefaultValue 1

    return $value -eq 0
}

function Set-DisableAppLaunchTracking {
    <#
    .SYNOPSIS
        Disables App Launch Tracking used for Start Menu suggestions
    #>
    [CmdletBinding()]
    param()

    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced" -Name "Start_TrackProgs" -Value 0 -Type DWord

    return $true
}

function Undo-DisableAppLaunchTracking {
    <#
    .SYNOPSIS
        Re-enables App Launch Tracking
    #>
    [CmdletBinding()]
    param()

    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced" -Name "Start_TrackProgs" -Value 1 -Type DWord

    return $true
}

#endregion

#region Windows Spotlight

function Test-DisableWindowsSpotlight {
    <#
    .SYNOPSIS
        Tests if Windows Spotlight is disabled
    #>
    [CmdletBinding()]
    param()

    $path = "HKCU:\SOFTWARE\Policies\Microsoft\Windows\CloudContent"
    $value = Get-RegistryValue -Path $path -Name "DisableWindowsSpotlightFeatures" -DefaultValue 0

    return $value -eq 1
}

function Set-DisableWindowsSpotlight {
    <#
    .SYNOPSIS
        Disables Windows Spotlight on lock screen
    #>
    [CmdletBinding()]
    param()

    $settings = @(
        @{ Name = "DisableWindowsSpotlightFeatures"; Value = 1 },
        @{ Name = "DisableWindowsSpotlightWindowsWelcomeExperience"; Value = 1 },
        @{ Name = "DisableWindowsSpotlightOnActionCenter"; Value = 1 },
        @{ Name = "DisableWindowsSpotlightOnSettings"; Value = 1 },
        @{ Name = "DisableThirdPartySuggestions"; Value = 1 }
    )

    foreach ($setting in $settings) {
        Set-RegistryValue -Path "HKCU:\SOFTWARE\Policies\Microsoft\Windows\CloudContent" -Name $setting.Name -Value $setting.Value -Type DWord
    }

    return $true
}

function Undo-DisableWindowsSpotlight {
    <#
    .SYNOPSIS
        Re-enables Windows Spotlight
    #>
    [CmdletBinding()]
    param()

    $path = "HKCU:\SOFTWARE\Policies\Microsoft\Windows\CloudContent"
    $names = @(
        "DisableWindowsSpotlightFeatures",
        "DisableWindowsSpotlightWindowsWelcomeExperience",
        "DisableWindowsSpotlightOnActionCenter",
        "DisableWindowsSpotlightOnSettings",
        "DisableThirdPartySuggestions"
    )

    foreach ($name in $names) {
        Remove-RegistryValue -Path $path -Name $name
    }

    return $true
}

#endregion

#region Cortana

function Test-DisableCortana {
    <#
    .SYNOPSIS
        Tests if Cortana is disabled
    #>
    [CmdletBinding()]
    param()

    $path = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search"
    $value = Get-RegistryValue -Path $path -Name "AllowCortana" -DefaultValue 1

    return $value -eq 0
}

function Set-DisableCortana {
    <#
    .SYNOPSIS
        Disables Cortana
    #>
    [CmdletBinding()]
    param()

    Set-RegistryValue -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search" -Name "AllowCortana" -Value 0 -Type DWord
    Set-RegistryValue -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search" -Name "AllowCortanaAboveLock" -Value 0 -Type DWord
    Set-RegistryValue -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search" -Name "AllowSearchToUseLocation" -Value 0 -Type DWord
    Set-RegistryValue -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search" -Name "DisableWebSearch" -Value 1 -Type DWord

    return $true
}

function Undo-DisableCortana {
    <#
    .SYNOPSIS
        Re-enables Cortana
    #>
    [CmdletBinding()]
    param()

    $path = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search"
    Remove-RegistryValue -Path $path -Name "AllowCortana"
    Remove-RegistryValue -Path $path -Name "AllowCortanaAboveLock"
    Remove-RegistryValue -Path $path -Name "AllowSearchToUseLocation"
    Remove-RegistryValue -Path $path -Name "DisableWebSearch"

    return $true
}

#endregion

# Export functions
Export-ModuleMember -Function @(
    # Telemetry
    'Test-DisableTelemetry', 'Set-DisableTelemetry', 'Undo-DisableTelemetry',
    # Advertising
    'Test-DisableAdvertisingId', 'Set-DisableAdvertisingId', 'Undo-DisableAdvertisingId',
    # Activity History
    'Test-DisableActivityHistory', 'Set-DisableActivityHistory', 'Undo-DisableActivityHistory',
    # Location
    'Test-DisableLocationTracking', 'Set-DisableLocationTracking', 'Undo-DisableLocationTracking',
    # Feedback
    'Test-DisableFeedback', 'Set-DisableFeedback', 'Undo-DisableFeedback',
    # Tailored Experiences
    'Test-DisableTailoredExperiences', 'Set-DisableTailoredExperiences', 'Undo-DisableTailoredExperiences',
    # Typing Data
    'Test-DisableTypingData', 'Set-DisableTypingData', 'Undo-DisableTypingData',
    # App Launch Tracking
    'Test-DisableAppLaunchTracking', 'Set-DisableAppLaunchTracking', 'Undo-DisableAppLaunchTracking',
    # Windows Spotlight
    'Test-DisableWindowsSpotlight', 'Set-DisableWindowsSpotlight', 'Undo-DisableWindowsSpotlight',
    # Cortana
    'Test-DisableCortana', 'Set-DisableCortana', 'Undo-DisableCortana'
)
