<#
.SYNOPSIS
    WinWithWin Performance Tweaks Module
.DESCRIPTION
    Contains all performance-related tweaks including services, visual effects, and power settings.
    Each tweak has a corresponding Test, Set, and Undo function.
.NOTES
    These tweaks are designed to improve system responsiveness without causing instability.
#>

#Requires -Version 5.1
#Requires -RunAsAdministrator

# Import helpers
$helpersPath = Join-Path (Split-Path -Parent $PSScriptRoot) "core\Helpers.psm1"
if (Test-Path $helpersPath) {
    Import-Module $helpersPath -Force -DisableNameChecking
}

#region Visual Effects

function Test-OptimizeVisualEffects {
    <#
    .SYNOPSIS
        Tests if visual effects are optimized for performance
    #>
    [CmdletBinding()]
    param()

    $path = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects"
    $value = Get-RegistryValue -Path $path -Name "VisualFXSetting" -DefaultValue 0

    # 2 = Custom (optimized), 1 = Best appearance, 0 = Let Windows decide
    return $value -eq 2
}

function Set-OptimizeVisualEffects {
    <#
    .SYNOPSIS
        Optimizes visual effects for better performance
    .DESCRIPTION
        Disables unnecessary visual effects while keeping the UI usable
    #>
    [CmdletBinding()]
    param()

    # Set to custom visual effects
    Set-RegistryValue -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects" -Name "VisualFXSetting" -Value 2 -Type DWord

    # Configure individual settings
    $advancedPath = "HKCU:\Control Panel\Desktop"
    
    # Disable window animations
    Set-RegistryValue -Path $advancedPath -Name "UserPreferencesMask" -Value ([byte[]](0x90,0x12,0x03,0x80,0x10,0x00,0x00,0x00)) -Type Binary

    # Disable transparency
    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize" -Name "EnableTransparency" -Value 0 -Type DWord

    # Disable animations
    Set-RegistryValue -Path "HKCU:\Control Panel\Desktop\WindowMetrics" -Name "MinAnimate" -Value "0" -Type String

    # Smooth edges of screen fonts (keep enabled for readability)
    Set-RegistryValue -Path "HKCU:\Control Panel\Desktop" -Name "FontSmoothing" -Value "2" -Type String

    # Disable peek
    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Windows\DWM" -Name "EnableAeroPeek" -Value 0 -Type DWord

    return $true
}

function Undo-OptimizeVisualEffects {
    <#
    .SYNOPSIS
        Restores default visual effects
    #>
    [CmdletBinding()]
    param()

    # Let Windows decide
    Set-RegistryValue -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects" -Name "VisualFXSetting" -Value 0 -Type DWord

    # Restore transparency
    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize" -Name "EnableTransparency" -Value 1 -Type DWord

    # Restore animations
    Set-RegistryValue -Path "HKCU:\Control Panel\Desktop\WindowMetrics" -Name "MinAnimate" -Value "1" -Type String

    # Restore peek
    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Windows\DWM" -Name "EnableAeroPeek" -Value 1 -Type DWord

    return $true
}

#endregion

#region Power Plan

function Test-UltimatePerformancePlan {
    <#
    .SYNOPSIS
        Tests if Ultimate Performance power plan is active
    #>
    [CmdletBinding()]
    param()

    try {
        $activePlan = powercfg /getactivescheme
        return $activePlan -match "Ultimate Performance" -or $activePlan -match "e9a42b02-d5df-448d-aa00-03f14749eb61"
    }
    catch {
        return $false
    }
}

function Set-UltimatePerformancePlan {
    <#
    .SYNOPSIS
        Enables Ultimate Performance power plan
    .DESCRIPTION
        Enables the hidden Ultimate Performance power plan for maximum performance
    #>
    [CmdletBinding()]
    param()

    # Unhide Ultimate Performance plan
    powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 2>$null

    # Find and activate it
    $plans = powercfg /list
    $ultimatePlan = $plans | Select-String -Pattern "e9a42b02-d5df-448d-aa00-03f14749eb61|Ultimate Performance"
    
    if ($ultimatePlan) {
        if ($ultimatePlan -match "([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12})") {
            $guid = $Matches[1]
            powercfg /setactive $guid
            return $true
        }
    }

    # If Ultimate Performance not available, use High Performance
    $highPerf = $plans | Select-String -Pattern "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c|High performance"
    if ($highPerf -and $highPerf -match "([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12})") {
        powercfg /setactive $Matches[1]
    }

    return $true
}

function Undo-UltimatePerformancePlan {
    <#
    .SYNOPSIS
        Restores Balanced power plan
    #>
    [CmdletBinding()]
    param()

    # Balanced plan GUID
    powercfg /setactive 381b4222-f694-41f0-9685-ff5bb260df2e

    return $true
}

#endregion

#region Disable Unnecessary Services

function Test-DisableUnnecessaryServices {
    <#
    .SYNOPSIS
        Tests if unnecessary services are disabled
    #>
    [CmdletBinding()]
    param()

    $services = @("SysMain", "WSearch")
    
    foreach ($svc in $services) {
        $service = Get-Service -Name $svc -ErrorAction SilentlyContinue
        if ($service -and $service.StartType -ne "Disabled") {
            return $false
        }
    }

    return $true
}

function Set-DisableUnnecessaryServices {
    <#
    .SYNOPSIS
        Disables services that are not needed for most users
    .DESCRIPTION
        Disables services that consume resources but provide minimal benefit.
        Does NOT disable critical system services.
    #>
    [CmdletBinding()]
    param()

    # Services to disable (carefully selected, non-critical)
    $servicesToDisable = @(
        @{ Name = "SysMain"; Description = "Superfetch - Can cause high disk usage on SSDs" },
        @{ Name = "DiagTrack"; Description = "Diagnostics Tracking Service - Telemetry" },
        @{ Name = "dmwappushservice"; Description = "Device Management WAP Push - Telemetry" },
        @{ Name = "WMPNetworkSvc"; Description = "Windows Media Player Network Sharing" },
        @{ Name = "WerSvc"; Description = "Windows Error Reporting" },
        @{ Name = "MapsBroker"; Description = "Downloaded Maps Manager" },
        @{ Name = "lfsvc"; Description = "Geolocation Service" },
        @{ Name = "RetailDemo"; Description = "Retail Demo Service" },
        @{ Name = "RemoteRegistry"; Description = "Remote Registry - Security risk" }
    )

    foreach ($svc in $servicesToDisable) {
        $service = Get-Service -Name $svc.Name -ErrorAction SilentlyContinue
        if ($service) {
            Write-Verbose "Disabling: $($svc.Name) - $($svc.Description)"
            Set-ServiceStartup -ServiceName $svc.Name -StartupType Disabled -StopService
        }
    }

    return $true
}

function Undo-DisableUnnecessaryServices {
    <#
    .SYNOPSIS
        Re-enables the disabled services
    #>
    [CmdletBinding()]
    param()

    $servicesToEnable = @(
        @{ Name = "SysMain"; StartType = "Automatic" },
        @{ Name = "DiagTrack"; StartType = "Automatic" },
        @{ Name = "dmwappushservice"; StartType = "Manual" },
        @{ Name = "WMPNetworkSvc"; StartType = "Manual" },
        @{ Name = "WerSvc"; StartType = "Manual" },
        @{ Name = "MapsBroker"; StartType = "Manual" },
        @{ Name = "lfsvc"; StartType = "Manual" },
        @{ Name = "RetailDemo"; StartType = "Manual" },
        @{ Name = "RemoteRegistry"; StartType = "Manual" }
    )

    foreach ($svc in $servicesToEnable) {
        Set-ServiceStartup -ServiceName $svc.Name -StartupType $svc.StartType
    }

    return $true
}

#endregion

#region Background Apps

function Test-DisableBackgroundApps {
    <#
    .SYNOPSIS
        Tests if background apps are disabled
    #>
    [CmdletBinding()]
    param()

    $path = "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications"
    $value = Get-RegistryValue -Path $path -Name "GlobalUserDisabled" -DefaultValue 0

    return $value -eq 1
}

function Set-DisableBackgroundApps {
    <#
    .SYNOPSIS
        Disables background apps to save resources
    #>
    [CmdletBinding()]
    param()

    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications" -Name "GlobalUserDisabled" -Value 1 -Type DWord
    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Search" -Name "BackgroundAppGlobalToggle" -Value 0 -Type DWord

    return $true
}

function Undo-DisableBackgroundApps {
    <#
    .SYNOPSIS
        Re-enables background apps
    #>
    [CmdletBinding()]
    param()

    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications" -Name "GlobalUserDisabled" -Value 0 -Type DWord
    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Search" -Name "BackgroundAppGlobalToggle" -Value 1 -Type DWord

    return $true
}

#endregion

#region Startup Apps

function Get-StartupPrograms {
    <#
    .SYNOPSIS
        Gets list of startup programs
    #>
    [CmdletBinding()]
    param()

    $startupItems = @()

    # Registry Run keys
    $runKeys = @(
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run",
        "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"
    )

    foreach ($key in $runKeys) {
        if (Test-Path $key) {
            $items = Get-ItemProperty -Path $key -ErrorAction SilentlyContinue
            $items.PSObject.Properties | Where-Object { $_.Name -notlike "PS*" } | ForEach-Object {
                $startupItems += [PSCustomObject]@{
                    Name = $_.Name
                    Command = $_.Value
                    Location = $key
                    Type = "Registry"
                }
            }
        }
    }

    # Startup folder
    $startupFolder = [Environment]::GetFolderPath("Startup")
    if (Test-Path $startupFolder) {
        Get-ChildItem $startupFolder | ForEach-Object {
            $startupItems += [PSCustomObject]@{
                Name = $_.BaseName
                Command = $_.FullName
                Location = $startupFolder
                Type = "Folder"
            }
        }
    }

    return $startupItems
}

#endregion

#region Game Mode & Game Bar

function Test-OptimizeGaming {
    <#
    .SYNOPSIS
        Tests if gaming optimizations are applied
    #>
    [CmdletBinding()]
    param()

    $gameBar = Get-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR" -Name "AppCaptureEnabled" -DefaultValue 1
    
    return $gameBar -eq 0
}

function Set-OptimizeGaming {
    <#
    .SYNOPSIS
        Optimizes Windows for gaming performance
    .DESCRIPTION
        Disables Game DVR/Bar recording which can cause FPS drops.
        Keeps Game Mode enabled as it can help.
    #>
    [CmdletBinding()]
    param()

    # Disable Game DVR (can cause FPS drops)
    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR" -Name "AppCaptureEnabled" -Value 0 -Type DWord
    Set-RegistryValue -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\GameDVR" -Name "AllowGameDVR" -Value 0 -Type DWord

    # Disable Game Bar
    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\GameBar" -Name "UseNexusForGameBarEnabled" -Value 0 -Type DWord
    Set-RegistryValue -Path "HKCU:\System\GameConfigStore" -Name "GameDVR_Enabled" -Value 0 -Type DWord

    # Keep Game Mode ON (it helps with performance)
    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\GameBar" -Name "AutoGameModeEnabled" -Value 1 -Type DWord

    # Disable fullscreen optimizations globally (optional, can help some games)
    Set-RegistryValue -Path "HKCU:\System\GameConfigStore" -Name "GameDVR_FSEBehaviorMode" -Value 2 -Type DWord
    Set-RegistryValue -Path "HKCU:\System\GameConfigStore" -Name "GameDVR_HonorUserFSEBehaviorMode" -Value 1 -Type DWord

    return $true
}

function Undo-OptimizeGaming {
    <#
    .SYNOPSIS
        Restores default gaming settings
    #>
    [CmdletBinding()]
    param()

    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR" -Name "AppCaptureEnabled" -Value 1 -Type DWord
    Remove-RegistryValue -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\GameDVR" -Name "AllowGameDVR"
    Set-RegistryValue -Path "HKCU:\SOFTWARE\Microsoft\GameBar" -Name "UseNexusForGameBarEnabled" -Value 1 -Type DWord
    Set-RegistryValue -Path "HKCU:\System\GameConfigStore" -Name "GameDVR_Enabled" -Value 1 -Type DWord
    Remove-RegistryValue -Path "HKCU:\System\GameConfigStore" -Name "GameDVR_FSEBehaviorMode"
    Remove-RegistryValue -Path "HKCU:\System\GameConfigStore" -Name "GameDVR_HonorUserFSEBehaviorMode"

    return $true
}

#endregion

#region Scheduled Tasks

function Test-DisableScheduledTasks {
    <#
    .SYNOPSIS
        Tests if unnecessary scheduled tasks are disabled
    #>
    [CmdletBinding()]
    param()

    $task = Get-ScheduledTask -TaskPath "\Microsoft\Windows\Customer Experience Improvement Program\" -TaskName "Consolidator" -ErrorAction SilentlyContinue
    
    if ($task -and $task.State -ne "Disabled") {
        return $false
    }

    return $true
}

function Set-DisableScheduledTasks {
    <#
    .SYNOPSIS
        Disables unnecessary scheduled tasks
    .DESCRIPTION
        Disables telemetry and other non-essential scheduled tasks
    #>
    [CmdletBinding()]
    param()

    $tasksToDisable = @(
        @{ Path = "\Microsoft\Windows\Customer Experience Improvement Program\"; Name = "Consolidator" },
        @{ Path = "\Microsoft\Windows\Customer Experience Improvement Program\"; Name = "UsbCeip" },
        @{ Path = "\Microsoft\Windows\DiskDiagnostic\"; Name = "Microsoft-Windows-DiskDiagnosticDataCollector" },
        @{ Path = "\Microsoft\Windows\Application Experience\"; Name = "Microsoft Compatibility Appraiser" },
        @{ Path = "\Microsoft\Windows\Application Experience\"; Name = "ProgramDataUpdater" },
        @{ Path = "\Microsoft\Windows\Autochk\"; Name = "Proxy" },
        @{ Path = "\Microsoft\Windows\CloudExperienceHost\"; Name = "CreateObjectTask" },
        @{ Path = "\Microsoft\Windows\Feedback\Siuf\"; Name = "DmClient" },
        @{ Path = "\Microsoft\Windows\Feedback\Siuf\"; Name = "DmClientOnScenarioDownload" }
    )

    foreach ($task in $tasksToDisable) {
        Disable-ScheduledTaskSafe -TaskPath $task.Path -TaskName $task.Name
    }

    return $true
}

function Undo-DisableScheduledTasks {
    <#
    .SYNOPSIS
        Re-enables the disabled scheduled tasks
    #>
    [CmdletBinding()]
    param()

    $tasksToEnable = @(
        @{ Path = "\Microsoft\Windows\Customer Experience Improvement Program\"; Name = "Consolidator" },
        @{ Path = "\Microsoft\Windows\Customer Experience Improvement Program\"; Name = "UsbCeip" },
        @{ Path = "\Microsoft\Windows\DiskDiagnostic\"; Name = "Microsoft-Windows-DiskDiagnosticDataCollector" },
        @{ Path = "\Microsoft\Windows\Application Experience\"; Name = "Microsoft Compatibility Appraiser" },
        @{ Path = "\Microsoft\Windows\Application Experience\"; Name = "ProgramDataUpdater" },
        @{ Path = "\Microsoft\Windows\Autochk\"; Name = "Proxy" },
        @{ Path = "\Microsoft\Windows\CloudExperienceHost\"; Name = "CreateObjectTask" },
        @{ Path = "\Microsoft\Windows\Feedback\Siuf\"; Name = "DmClient" },
        @{ Path = "\Microsoft\Windows\Feedback\Siuf\"; Name = "DmClientOnScenarioDownload" }
    )

    foreach ($task in $tasksToEnable) {
        Enable-ScheduledTaskSafe -TaskPath $task.Path -TaskName $task.Name
    }

    return $true
}

#endregion

#region Memory Optimization

function Set-OptimizeMemory {
    <#
    .SYNOPSIS
        Applies memory optimization settings
    #>
    [CmdletBinding()]
    param()

    # Disable memory compression (can help on systems with enough RAM)
    # Note: Only recommended for 16GB+ RAM systems
    $ram = (Get-CimInstance -ClassName Win32_ComputerSystem).TotalPhysicalMemory / 1GB
    
    if ($ram -ge 16) {
        Disable-MMAgent -MemoryCompression -ErrorAction SilentlyContinue
    }

    # Clear page file at shutdown (security + fresh start)
    Set-RegistryValue -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" -Name "ClearPageFileAtShutdown" -Value 1 -Type DWord

    return $true
}

function Undo-OptimizeMemory {
    <#
    .SYNOPSIS
        Restores default memory settings
    #>
    [CmdletBinding()]
    param()

    Enable-MMAgent -MemoryCompression -ErrorAction SilentlyContinue
    Set-RegistryValue -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" -Name "ClearPageFileAtShutdown" -Value 0 -Type DWord

    return $true
}

function Test-OptimizeMemory {
    <#
    .SYNOPSIS
        Tests if memory optimizations are applied
    #>
    [CmdletBinding()]
    param()

    $value = Get-RegistryValue -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" -Name "ClearPageFileAtShutdown" -DefaultValue 0
    return $value -eq 1
}

#endregion

#region Fast Startup

function Test-DisableFastStartup {
    <#
    .SYNOPSIS
        Tests if Fast Startup is disabled
    #>
    [CmdletBinding()]
    param()

    $value = Get-RegistryValue -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Power" -Name "HiberbootEnabled" -DefaultValue 1
    return $value -eq 0
}

function Set-DisableFastStartup {
    <#
    .SYNOPSIS
        Disables Fast Startup
    .DESCRIPTION
        Fast Startup can cause issues with dual-boot and some drivers.
        Disabling it provides a cleaner shutdown/startup cycle.
    #>
    [CmdletBinding()]
    param()

    Set-RegistryValue -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Power" -Name "HiberbootEnabled" -Value 0 -Type DWord

    return $true
}

function Undo-DisableFastStartup {
    <#
    .SYNOPSIS
        Re-enables Fast Startup
    #>
    [CmdletBinding()]
    param()

    Set-RegistryValue -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Power" -Name "HiberbootEnabled" -Value 1 -Type DWord

    return $true
}

#endregion

# Export functions
Export-ModuleMember -Function @(
    # Visual Effects
    'Test-OptimizeVisualEffects', 'Set-OptimizeVisualEffects', 'Undo-OptimizeVisualEffects',
    # Power Plan
    'Test-UltimatePerformancePlan', 'Set-UltimatePerformancePlan', 'Undo-UltimatePerformancePlan',
    # Services
    'Test-DisableUnnecessaryServices', 'Set-DisableUnnecessaryServices', 'Undo-DisableUnnecessaryServices',
    # Background Apps
    'Test-DisableBackgroundApps', 'Set-DisableBackgroundApps', 'Undo-DisableBackgroundApps',
    # Startup
    'Get-StartupPrograms',
    # Gaming
    'Test-OptimizeGaming', 'Set-OptimizeGaming', 'Undo-OptimizeGaming',
    # Scheduled Tasks
    'Test-DisableScheduledTasks', 'Set-DisableScheduledTasks', 'Undo-DisableScheduledTasks',
    # Memory
    'Test-OptimizeMemory', 'Set-OptimizeMemory', 'Undo-OptimizeMemory',
    # Fast Startup
    'Test-DisableFastStartup', 'Set-DisableFastStartup', 'Undo-DisableFastStartup'
)
