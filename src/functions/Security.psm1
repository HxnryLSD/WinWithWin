<#
.SYNOPSIS
    WinWithWin Security Tweaks Module
.DESCRIPTION
    Contains security-related tweaks. These are handled carefully to not reduce system security.
.NOTES
    Unlike other modules, many security tweaks are about ENABLING protections, not disabling.
#>

#Requires -Version 5.1
#Requires -RunAsAdministrator

# Import helpers
$helpersPath = Join-Path (Split-Path -Parent $PSScriptRoot) "core\Helpers.psm1"
if (Test-Path $helpersPath) {
    Import-Module $helpersPath -Force -DisableNameChecking
}

#region Windows Update

function Test-ConfigureWindowsUpdate {
    <#
    .SYNOPSIS
        Tests Windows Update configuration
    #>
    [CmdletBinding()]
    param()

    $path = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU"
    $value = Get-RegistryValue -Path $path -Name "NoAutoUpdate" -DefaultValue 0

    # We want auto updates enabled but with user control
    return $value -eq 0
}

function Set-ConfigureWindowsUpdate {
    <#
    .SYNOPSIS
        Configures Windows Update for user control
    .DESCRIPTION
        Enables Windows Update but gives user more control over when to install.
        Does NOT disable updates - that's a security risk.
    #>
    [CmdletBinding()]
    param()

    $auPath = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU"

    # Keep auto updates enabled
    Set-RegistryValue -Path $auPath -Name "NoAutoUpdate" -Value 0 -Type DWord

    # Notify before download (option 2)
    Set-RegistryValue -Path $auPath -Name "AUOptions" -Value 2 -Type DWord

    # Disable auto-restart
    Set-RegistryValue -Path $auPath -Name "NoAutoRebootWithLoggedOnUsers" -Value 1 -Type DWord

    # Disable forced restart
    Set-RegistryValue -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate" -Name "SetDisableUXWUAccess" -Value 0 -Type DWord

    return $true
}

function Undo-ConfigureWindowsUpdate {
    <#
    .SYNOPSIS
        Restores default Windows Update settings
    #>
    [CmdletBinding()]
    param()

    $auPath = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU"
    
    Remove-RegistryValue -Path $auPath -Name "AUOptions"
    Remove-RegistryValue -Path $auPath -Name "NoAutoRebootWithLoggedOnUsers"
    Remove-RegistryValue -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate" -Name "SetDisableUXWUAccess"

    return $true
}

#endregion

#region Windows Defender

function Test-EnsureDefenderEnabled {
    <#
    .SYNOPSIS
        Tests if Windows Defender is properly enabled
    #>
    [CmdletBinding()]
    param()

    try {
        $status = Get-MpComputerStatus -ErrorAction Stop
        return $status.AntivirusEnabled -and $status.RealTimeProtectionEnabled
    }
    catch {
        return $false
    }
}

function Set-EnsureDefenderEnabled {
    <#
    .SYNOPSIS
        Ensures Windows Defender is enabled with recommended settings
    .DESCRIPTION
        This tweak ENABLES security features, not disables them.
    #>
    [CmdletBinding()]
    param()

    try {
        # Enable real-time protection
        Set-MpPreference -DisableRealtimeMonitoring $false -ErrorAction SilentlyContinue

        # Enable cloud protection
        Set-MpPreference -MAPSReporting Advanced -ErrorAction SilentlyContinue
        Set-MpPreference -SubmitSamplesConsent SendAllSamples -ErrorAction SilentlyContinue

        # Enable PUA protection
        Set-MpPreference -PUAProtection Enabled -ErrorAction SilentlyContinue

        # Enable network protection
        Set-MpPreference -EnableNetworkProtection Enabled -ErrorAction SilentlyContinue

        # Remove any policies that disable Defender
        Remove-RegistryValue -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender" -Name "DisableAntiSpyware"
        Remove-RegistryValue -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection" -Name "DisableRealtimeMonitoring"
    }
    catch {
        Write-Warning "Some Defender settings could not be applied: $_"
    }

    return $true
}

function Undo-EnsureDefenderEnabled {
    <#
    .SYNOPSIS
        Restores default Defender settings (keeps it enabled)
    #>
    [CmdletBinding()]
    param()

    # We don't want to disable Defender, just reset to defaults
    try {
        Set-MpPreference -PUAProtection AuditMode -ErrorAction SilentlyContinue
    }
    catch {
        # Ignore errors
    }

    return $true
}

#endregion

#region SmartScreen

function Test-ConfigureSmartScreen {
    <#
    .SYNOPSIS
        Tests if SmartScreen is configured
    #>
    [CmdletBinding()]
    param()

    $path = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer"
    $value = Get-RegistryValue -Path $path -Name "SmartScreenEnabled" -DefaultValue "Off"

    return $value -ne "Off"
}

function Set-ConfigureSmartScreen {
    <#
    .SYNOPSIS
        Ensures SmartScreen is enabled (SECURITY ENHANCEMENT)
    #>
    [CmdletBinding()]
    param()

    # Enable SmartScreen
    Set-RegistryValue -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer" -Name "SmartScreenEnabled" -Value "RequireAdmin" -Type String

    # Enable SmartScreen for Edge
    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Edge\SmartScreenEnabled" -Name "" -Value 1 -Type DWord

    # Enable SmartScreen for Store apps
    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppHost" -Name "EnableWebContentEvaluation" -Value 1 -Type DWord

    return $true
}

function Undo-ConfigureSmartScreen {
    <#
    .SYNOPSIS
        Restores default SmartScreen settings (keeps enabled)
    #>
    [CmdletBinding()]
    param()

    # Keep SmartScreen on by default
    Set-RegistryValue -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer" -Name "SmartScreenEnabled" -Value "Prompt" -Type String

    return $true
}

#endregion

#region UAC

function Test-ConfigureUAC {
    <#
    .SYNOPSIS
        Tests UAC configuration
    #>
    [CmdletBinding()]
    param()

    $path = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"
    
    # UAC should be enabled
    return (Get-RegistryValue -Path $path -Name "EnableLUA" -DefaultValue 1) -eq 1
}

function Set-ConfigureUAC {
    <#
    .SYNOPSIS
        Configures UAC for security with less annoyance
    .DESCRIPTION
        Keeps UAC enabled but reduces unnecessary prompts for admin accounts.
        NEVER fully disables UAC.
    #>
    [CmdletBinding()]
    param()

    $path = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"

    # Keep UAC enabled (CRITICAL)
    Set-RegistryValue -Path $path -Name "EnableLUA" -Value 1 -Type DWord

    # Prompt for consent on secure desktop (level 5 - default)
    Set-RegistryValue -Path $path -Name "ConsentPromptBehaviorAdmin" -Value 5 -Type DWord

    # Standard users always prompt
    Set-RegistryValue -Path $path -Name "ConsentPromptBehaviorUser" -Value 3 -Type DWord

    # Enable secure desktop
    Set-RegistryValue -Path $path -Name "PromptOnSecureDesktop" -Value 1 -Type DWord

    return $true
}

function Undo-ConfigureUAC {
    <#
    .SYNOPSIS
        Restores default UAC settings
    #>
    [CmdletBinding()]
    param()

    $path = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"

    Set-RegistryValue -Path $path -Name "EnableLUA" -Value 1 -Type DWord
    Set-RegistryValue -Path $path -Name "ConsentPromptBehaviorAdmin" -Value 5 -Type DWord
    Set-RegistryValue -Path $path -Name "ConsentPromptBehaviorUser" -Value 3 -Type DWord
    Set-RegistryValue -Path $path -Name "PromptOnSecureDesktop" -Value 1 -Type DWord

    return $true
}

#endregion

#region Remote Desktop

function Test-DisableRemoteDesktop {
    <#
    .SYNOPSIS
        Tests if Remote Desktop is disabled
    #>
    [CmdletBinding()]
    param()

    $value = Get-RegistryValue -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server" -Name "fDenyTSConnections" -DefaultValue 1

    return $value -eq 1
}

function Set-DisableRemoteDesktop {
    <#
    .SYNOPSIS
        Disables Remote Desktop for security
    .DESCRIPTION
        Remote Desktop can be a security risk if not needed
    #>
    [CmdletBinding()]
    param()

    Set-RegistryValue -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server" -Name "fDenyTSConnections" -Value 1 -Type DWord

    # Disable the service
    Set-ServiceStartup -ServiceName "TermService" -StartupType Disabled -StopService

    # Disable firewall rules
    Disable-NetFirewallRule -DisplayGroup "Remote Desktop" -ErrorAction SilentlyContinue

    return $true
}

function Undo-DisableRemoteDesktop {
    <#
    .SYNOPSIS
        Re-enables Remote Desktop
    #>
    [CmdletBinding()]
    param()

    Set-RegistryValue -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server" -Name "fDenyTSConnections" -Value 0 -Type DWord
    Set-ServiceStartup -ServiceName "TermService" -StartupType Manual
    Enable-NetFirewallRule -DisplayGroup "Remote Desktop" -ErrorAction SilentlyContinue

    return $true
}

#endregion

#region Remote Assistance

function Test-DisableRemoteAssistance {
    <#
    .SYNOPSIS
        Tests if Remote Assistance is disabled
    #>
    [CmdletBinding()]
    param()

    $value = Get-RegistryValue -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Remote Assistance" -Name "fAllowToGetHelp" -DefaultValue 1

    return $value -eq 0
}

function Set-DisableRemoteAssistance {
    <#
    .SYNOPSIS
        Disables Remote Assistance
    #>
    [CmdletBinding()]
    param()

    Set-RegistryValue -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Remote Assistance" -Name "fAllowToGetHelp" -Value 0 -Type DWord

    return $true
}

function Undo-DisableRemoteAssistance {
    <#
    .SYNOPSIS
        Re-enables Remote Assistance
    #>
    [CmdletBinding()]
    param()

    Set-RegistryValue -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Remote Assistance" -Name "fAllowToGetHelp" -Value 1 -Type DWord

    return $true
}

#endregion

#region AutoPlay

function Test-DisableAutoPlay {
    <#
    .SYNOPSIS
        Tests if AutoPlay is disabled
    #>
    [CmdletBinding()]
    param()

    $value = Get-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers" -Name "DisableAutoplay" -DefaultValue 0

    return $value -eq 1
}

function Set-DisableAutoPlay {
    <#
    .SYNOPSIS
        Disables AutoPlay for security
    .DESCRIPTION
        AutoPlay can be used to automatically execute malware from USB drives
    #>
    [CmdletBinding()]
    param()

    # Disable AutoPlay
    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers" -Name "DisableAutoplay" -Value 1 -Type DWord

    # Disable AutoRun
    Set-RegistryValue -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer" -Name "NoDriveTypeAutoRun" -Value 255 -Type DWord

    return $true
}

function Undo-DisableAutoPlay {
    <#
    .SYNOPSIS
        Re-enables AutoPlay
    #>
    [CmdletBinding()]
    param()

    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers" -Name "DisableAutoplay" -Value 0 -Type DWord
    Remove-RegistryValue -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer" -Name "NoDriveTypeAutoRun"

    return $true
}

#endregion

#region DNS over HTTPS

function Test-EnableDNSOverHTTPS {
    <#
    .SYNOPSIS
        Tests if DNS over HTTPS is enabled
    #>
    [CmdletBinding()]
    param()

    try {
        $dnsSettings = Get-DnsClientDohServerAddress -ErrorAction Stop
        return $dnsSettings.Count -gt 0
    }
    catch {
        return $false
    }
}

function Set-EnableDNSOverHTTPS {
    <#
    .SYNOPSIS
        Enables DNS over HTTPS for privacy
    .DESCRIPTION
        Configures Windows to use encrypted DNS queries
    #>
    [CmdletBinding()]
    param()

    # Add known DoH servers if not present
    $dohServers = @(
        @{ ServerAddress = "1.1.1.1"; DohTemplate = "https://cloudflare-dns.com/dns-query" },
        @{ ServerAddress = "8.8.8.8"; DohTemplate = "https://dns.google/dns-query" },
        @{ ServerAddress = "9.9.9.9"; DohTemplate = "https://dns.quad9.net/dns-query" }
    )

    foreach ($server in $dohServers) {
        try {
            Add-DnsClientDohServerAddress -ServerAddress $server.ServerAddress -DohTemplate $server.DohTemplate -AllowFallbackToUdp $true -AutoUpgrade $true -ErrorAction SilentlyContinue
        }
        catch {
            # Server may already exist
        }
    }

    return $true
}

function Undo-EnableDNSOverHTTPS {
    <#
    .SYNOPSIS
        Removes custom DoH configuration
    #>
    [CmdletBinding()]
    param()

    # This just removes custom settings, doesn't break DNS
    try {
        Get-DnsClientDohServerAddress | Remove-DnsClientDohServerAddress -ErrorAction SilentlyContinue
    }
    catch {
        # Ignore errors
    }

    return $true
}

#endregion

# Export functions
Export-ModuleMember -Function @(
    # Windows Update
    'Test-ConfigureWindowsUpdate', 'Set-ConfigureWindowsUpdate', 'Undo-ConfigureWindowsUpdate',
    # Defender
    'Test-EnsureDefenderEnabled', 'Set-EnsureDefenderEnabled', 'Undo-EnsureDefenderEnabled',
    # SmartScreen
    'Test-ConfigureSmartScreen', 'Set-ConfigureSmartScreen', 'Undo-ConfigureSmartScreen',
    # UAC
    'Test-ConfigureUAC', 'Set-ConfigureUAC', 'Undo-ConfigureUAC',
    # Remote Desktop
    'Test-DisableRemoteDesktop', 'Set-DisableRemoteDesktop', 'Undo-DisableRemoteDesktop',
    # Remote Assistance
    'Test-DisableRemoteAssistance', 'Set-DisableRemoteAssistance', 'Undo-DisableRemoteAssistance',
    # AutoPlay
    'Test-DisableAutoPlay', 'Set-DisableAutoPlay', 'Undo-DisableAutoPlay',
    # DNS over HTTPS
    'Test-EnableDNSOverHTTPS', 'Set-EnableDNSOverHTTPS', 'Undo-EnableDNSOverHTTPS'
)
