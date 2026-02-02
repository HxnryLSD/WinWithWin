<#
.SYNOPSIS
    WinWithWin Helper Functions
.DESCRIPTION
    Utility functions used across all modules
#>

#Requires -Version 5.1

function Set-RegistryValue {
    <#
    .SYNOPSIS
        Sets a registry value with proper error handling
    .DESCRIPTION
        Creates the key path if it doesn't exist and sets the value
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        $Value,

        [Parameter(Mandatory)]
        [ValidateSet("String", "ExpandString", "Binary", "DWord", "MultiString", "QWord")]
        [string]$Type
    )

    try {
        # Create path if it doesn't exist
        if (-not (Test-Path $Path)) {
            New-Item -Path $Path -Force | Out-Null
        }

        Set-ItemProperty -Path $Path -Name $Name -Value $Value -Type $Type -Force
        return $true
    }
    catch {
        Write-Warning "Failed to set registry value $Path\$Name : $_"
        return $false
    }
}

function Get-RegistryValue {
    <#
    .SYNOPSIS
        Gets a registry value safely
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter()]
        $DefaultValue = $null
    )

    try {
        $value = Get-ItemPropertyValue -Path $Path -Name $Name -ErrorAction Stop
        return $value
    }
    catch {
        return $DefaultValue
    }
}

function Remove-RegistryValue {
    <#
    .SYNOPSIS
        Removes a registry value
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [string]$Name
    )

    try {
        if (Test-Path $Path) {
            Remove-ItemProperty -Path $Path -Name $Name -Force -ErrorAction Stop
        }
        return $true
    }
    catch {
        Write-Warning "Failed to remove registry value $Path\$Name : $_"
        return $false
    }
}

function Set-ServiceStartup {
    <#
    .SYNOPSIS
        Sets a service startup type safely
    .DESCRIPTION
        Changes service startup type with proper error handling
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ServiceName,

        [Parameter(Mandatory)]
        [ValidateSet("Automatic", "Boot", "Disabled", "Manual", "System")]
        [string]$StartupType,

        [Parameter()]
        [switch]$StopService
    )

    try {
        $service = Get-Service -Name $ServiceName -ErrorAction Stop
        
        Set-Service -Name $ServiceName -StartupType $StartupType
        
        if ($StopService -and $service.Status -eq "Running") {
            Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
        }

        return $true
    }
    catch {
        Write-Warning "Failed to configure service $ServiceName : $_"
        return $false
    }
}

function Get-ServiceStatus {
    <#
    .SYNOPSIS
        Gets service status and startup type
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ServiceName
    )

    try {
        $service = Get-Service -Name $ServiceName -ErrorAction Stop
        $wmiService = Get-CimInstance -ClassName Win32_Service -Filter "Name='$ServiceName'" -ErrorAction Stop

        return [PSCustomObject]@{
            Name = $service.Name
            DisplayName = $service.DisplayName
            Status = $service.Status.ToString()
            StartupType = $wmiService.StartMode
            Exists = $true
        }
    }
    catch {
        return [PSCustomObject]@{
            Name = $ServiceName
            Exists = $false
        }
    }
}

function Disable-ScheduledTaskSafe {
    <#
    .SYNOPSIS
        Disables a scheduled task safely
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$TaskPath,

        [Parameter(Mandatory)]
        [string]$TaskName
    )

    try {
        $task = Get-ScheduledTask -TaskPath $TaskPath -TaskName $TaskName -ErrorAction Stop
        
        if ($task.State -ne "Disabled") {
            Disable-ScheduledTask -TaskPath $TaskPath -TaskName $TaskName -ErrorAction Stop | Out-Null
        }
        
        return $true
    }
    catch {
        Write-Verbose "Scheduled task not found or already disabled: $TaskPath$TaskName"
        return $false
    }
}

function Enable-ScheduledTaskSafe {
    <#
    .SYNOPSIS
        Enables a scheduled task safely
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$TaskPath,

        [Parameter(Mandatory)]
        [string]$TaskName
    )

    try {
        $task = Get-ScheduledTask -TaskPath $TaskPath -TaskName $TaskName -ErrorAction Stop
        
        if ($task.State -eq "Disabled") {
            Enable-ScheduledTask -TaskPath $TaskPath -TaskName $TaskName -ErrorAction Stop | Out-Null
        }
        
        return $true
    }
    catch {
        Write-Verbose "Scheduled task not found: $TaskPath$TaskName"
        return $false
    }
}

function Test-IsAdmin {
    <#
    .SYNOPSIS
        Checks if running as administrator
    #>
    [CmdletBinding()]
    param()

    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Request-AdminElevation {
    <#
    .SYNOPSIS
        Requests admin elevation if not already admin
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$ScriptPath
    )

    if (-not (Test-IsAdmin)) {
        Write-Warning "Administrator privileges required. Requesting elevation..."
        
        $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$ScriptPath`""
        Start-Process -FilePath "powershell.exe" -Verb RunAs -ArgumentList $arguments
        
        exit
    }
}

function Get-InstalledApps {
    <#
    .SYNOPSIS
        Gets all installed AppX packages
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [switch]$AllUsers
    )

    if ($AllUsers) {
        return Get-AppxPackage -AllUsers | Select-Object Name, PackageFullName, Version
    }
    else {
        return Get-AppxPackage | Select-Object Name, PackageFullName, Version
    }
}

function Remove-AppxPackageSafe {
    <#
    .SYNOPSIS
        Removes an AppX package safely
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$PackageName,

        [Parameter()]
        [switch]$AllUsers
    )

    try {
        if ($AllUsers) {
            Get-AppxPackage -Name $PackageName -AllUsers | Remove-AppxPackage -AllUsers -ErrorAction Stop
        }
        else {
            Get-AppxPackage -Name $PackageName | Remove-AppxPackage -ErrorAction Stop
        }
        
        # Also remove provisioned package to prevent reinstall
        Get-AppxProvisionedPackage -Online | 
            Where-Object { $_.DisplayName -eq $PackageName } | 
            Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue | Out-Null
        
        return $true
    }
    catch {
        Write-Warning "Failed to remove package $PackageName : $_"
        return $false
    }
}

function Invoke-WithProgress {
    <#
    .SYNOPSIS
        Executes actions with progress bar
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [array]$Items,

        [Parameter(Mandatory)]
        [scriptblock]$Action,

        [Parameter()]
        [string]$Activity = "Processing"
    )

    $total = $Items.Count
    $current = 0
    $results = @()

    foreach ($item in $Items) {
        $current++
        $percent = [int](($current / $total) * 100)
        
        Write-Progress -Activity $Activity -Status "$current of $total" -PercentComplete $percent -CurrentOperation $item

        try {
            $result = & $Action $item
            $results += [PSCustomObject]@{
                Item = $item
                Success = $true
                Result = $result
            }
        }
        catch {
            $results += [PSCustomObject]@{
                Item = $item
                Success = $false
                Error = $_.Exception.Message
            }
        }
    }

    Write-Progress -Activity $Activity -Completed
    return $results
}

function Format-FileSize {
    <#
    .SYNOPSIS
        Formats bytes to human readable size
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [long]$Bytes
    )

    $sizes = "Bytes", "KB", "MB", "GB", "TB"
    $order = 0
    
    while ($Bytes -ge 1024 -and $order -lt $sizes.Count - 1) {
        $order++
        $Bytes = $Bytes / 1024
    }

    return "{0:N2} {1}" -f $Bytes, $sizes[$order]
}

function Test-InternetConnection {
    <#
    .SYNOPSIS
        Tests internet connectivity
    #>
    [CmdletBinding()]
    param()

    try {
        $result = Test-NetConnection -ComputerName "www.microsoft.com" -Port 80 -InformationLevel Quiet -WarningAction SilentlyContinue
        return $result
    }
    catch {
        return $false
    }
}

# Export functions
Export-ModuleMember -Function @(
    'Set-RegistryValue',
    'Get-RegistryValue',
    'Remove-RegistryValue',
    'Set-ServiceStartup',
    'Get-ServiceStatus',
    'Disable-ScheduledTaskSafe',
    'Enable-ScheduledTaskSafe',
    'Test-IsAdmin',
    'Request-AdminElevation',
    'Get-InstalledApps',
    'Remove-AppxPackageSafe',
    'Invoke-WithProgress',
    'Format-FileSize',
    'Test-InternetConnection'
)
