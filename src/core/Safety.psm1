<#
.SYNOPSIS
    WinWithWin Safety Module
.DESCRIPTION
    Provides safety mechanisms including backup, restore points, and undo functionality.
.NOTES
    All tweaks MUST have corresponding undo functions.
#>

#Requires -Version 5.1
#Requires -RunAsAdministrator

$script:BackupPath = Join-Path $env:LOCALAPPDATA "WinWithWin\Backups"
$script:LogPath = Join-Path $env:LOCALAPPDATA "WinWithWin\Logs"

# Ensure directories exist
if (-not (Test-Path $script:BackupPath)) {
    New-Item -Path $script:BackupPath -ItemType Directory -Force | Out-Null
}
if (-not (Test-Path $script:LogPath)) {
    New-Item -Path $script:LogPath -ItemType Directory -Force | Out-Null
}

function New-SafetyRestorePoint {
    <#
    .SYNOPSIS
        Creates a Windows System Restore Point
    .DESCRIPTION
        Creates a restore point before applying tweaks for safety
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$Description = "WinWithWin Backup"
    )

    Write-Host "[SAFETY] " -ForegroundColor Blue -NoNewline
    Write-Host "Creating system restore point..."

    try {
        # Enable System Restore if disabled
        $systemDrive = $env:SystemDrive
        Enable-ComputerRestore -Drive $systemDrive -ErrorAction SilentlyContinue

        # Check if we can create restore points (frequency limit)
        $lastRestore = Get-ComputerRestorePoint -ErrorAction SilentlyContinue | 
            Sort-Object -Property CreationTime -Descending | 
            Select-Object -First 1

        # Windows limits restore points to one per 24 hours by default
        # We'll try anyway and handle the error gracefully
        
        Checkpoint-Computer -Description $Description -RestorePointType "MODIFY_SETTINGS" -ErrorAction Stop
        
        Write-Host "[OK] " -ForegroundColor Green -NoNewline
        Write-Host "Restore point created: $Description"
        
        # Log the restore point
        Add-LogEntry -Message "Created restore point: $Description" -Level "INFO"
        
        return $true
    }
    catch {
        if ($_.Exception.Message -like "*1058*" -or $_.Exception.Message -like "*frequency*") {
            Write-Warning "Restore point creation skipped (frequency limit). Last restore point is recent."
            return $true
        }
        else {
            Write-Error "Failed to create restore point: $_"
            Add-LogEntry -Message "Failed to create restore point: $_" -Level "ERROR"
            return $false
        }
    }
}

function New-RegistryBackup {
    <#
    .SYNOPSIS
        Backs up specified registry keys
    .DESCRIPTION
        Exports registry keys to .reg files for restoration
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string[]]$RegistryPaths,

        [Parameter()]
        [string]$BackupName = "RegistryBackup"
    )

    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupFolder = Join-Path $script:BackupPath $timestamp
    
    if (-not (Test-Path $backupFolder)) {
        New-Item -Path $backupFolder -ItemType Directory -Force | Out-Null
    }

    $backupInfo = @{
        Timestamp = $timestamp
        BackupName = $BackupName
        Files = @()
    }

    foreach ($regPath in $RegistryPaths) {
        try {
            # Convert PowerShell registry path to reg.exe format
            $regExePath = $regPath -replace "^HKLM:", "HKEY_LOCAL_MACHINE" `
                                   -replace "^HKCU:", "HKEY_CURRENT_USER" `
                                   -replace "^HKU:", "HKEY_USERS" `
                                   -replace "^HKCR:", "HKEY_CLASSES_ROOT"

            $safeName = ($regPath -replace "[:\\]", "_") -replace "^_+", ""
            $backupFile = Join-Path $backupFolder "$safeName.reg"

            # Export using reg.exe
            $result = & reg.exe export $regExePath $backupFile /y 2>&1
            
            if ($LASTEXITCODE -eq 0) {
                $backupInfo.Files += @{
                    OriginalPath = $regPath
                    BackupFile = $backupFile
                }
                Write-Verbose "Backed up: $regPath"
            }
            else {
                Write-Warning "Could not backup registry path: $regPath (may not exist)"
            }
        }
        catch {
            Write-Warning "Error backing up $regPath : $_"
        }
    }

    # Save backup manifest
    $manifestPath = Join-Path $backupFolder "manifest.json"
    $backupInfo | ConvertTo-Json -Depth 10 | Set-Content -Path $manifestPath -Encoding UTF8

    Add-LogEntry -Message "Created registry backup: $BackupName ($($backupInfo.Files.Count) keys)" -Level "INFO"

    return $backupFolder
}

function Restore-RegistryBackup {
    <#
    .SYNOPSIS
        Restores a registry backup
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string]$BackupFolder
    )

    $manifestPath = Join-Path $BackupFolder "manifest.json"
    
    if (-not (Test-Path $manifestPath)) {
        Write-Error "Backup manifest not found: $manifestPath"
        return $false
    }

    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json

    Write-Host "[RESTORE] " -ForegroundColor Yellow -NoNewline
    Write-Host "Restoring registry backup from $($manifest.Timestamp)..."

    foreach ($backup in $manifest.Files) {
        if ($PSCmdlet.ShouldProcess($backup.OriginalPath, "Restore registry key")) {
            try {
                & reg.exe import $backup.BackupFile 2>&1 | Out-Null
                Write-Verbose "Restored: $($backup.OriginalPath)"
            }
            catch {
                Write-Warning "Failed to restore: $($backup.OriginalPath)"
            }
        }
    }

    Add-LogEntry -Message "Restored registry backup: $BackupFolder" -Level "INFO"
    Write-Host "[OK] " -ForegroundColor Green -NoNewline
    Write-Host "Registry backup restored"

    return $true
}

function New-TweakBackup {
    <#
    .SYNOPSIS
        Creates a backup for a specific tweak
    .DESCRIPTION
        Stores the current state before applying a tweak
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$TweakId,

        [Parameter()]
        [hashtable]$CurrentState
    )

    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $tweakBackupPath = Join-Path $script:BackupPath "Tweaks\$TweakId"
    
    if (-not (Test-Path $tweakBackupPath)) {
        New-Item -Path $tweakBackupPath -ItemType Directory -Force | Out-Null
    }

    $backupFile = Join-Path $tweakBackupPath "$timestamp.json"
    
    $backupData = @{
        TweakId = $TweakId
        Timestamp = (Get-Date).ToString("o")
        CurrentState = $CurrentState
        WindowsBuild = [System.Environment]::OSVersion.Version.Build
    }

    $backupData | ConvertTo-Json -Depth 10 | Set-Content -Path $backupFile -Encoding UTF8

    return $backupFile
}

function Get-TweakBackups {
    <#
    .SYNOPSIS
        Gets all backups for a tweak
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$TweakId
    )

    $basePath = Join-Path $script:BackupPath "Tweaks"
    
    if ($TweakId) {
        $basePath = Join-Path $basePath $TweakId
    }

    if (-not (Test-Path $basePath)) {
        return @()
    }

    $backups = Get-ChildItem -Path $basePath -Filter "*.json" -Recurse | ForEach-Object {
        $content = Get-Content $_.FullName -Raw | ConvertFrom-Json
        [PSCustomObject]@{
            TweakId = $content.TweakId
            Timestamp = [DateTime]$content.Timestamp
            Path = $_.FullName
            WindowsBuild = $content.WindowsBuild
        }
    } | Sort-Object -Property Timestamp -Descending

    return $backups
}

function Add-LogEntry {
    <#
    .SYNOPSIS
        Adds an entry to the WinWithWin log
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Message,

        [Parameter()]
        [ValidateSet("INFO", "WARNING", "ERROR", "DEBUG")]
        [string]$Level = "INFO"
    )

    $logFile = Join-Path $script:LogPath "WinWithWin_$(Get-Date -Format 'yyyyMMdd').log"
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logEntry = "[$timestamp] [$Level] $Message"

    Add-Content -Path $logFile -Value $logEntry -Encoding UTF8
}

function Get-LogEntries {
    <#
    .SYNOPSIS
        Gets log entries
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [DateTime]$Since,

        [Parameter()]
        [string]$Level,

        [Parameter()]
        [int]$Last = 100
    )

    $logFiles = Get-ChildItem -Path $script:LogPath -Filter "WinWithWin_*.log" -ErrorAction SilentlyContinue

    $entries = foreach ($file in $logFiles) {
        Get-Content $file.FullName -Tail $Last | ForEach-Object {
            if ($_ -match '^\[(.+?)\] \[(.+?)\] (.+)$') {
                [PSCustomObject]@{
                    Timestamp = [DateTime]$Matches[1]
                    Level = $Matches[2]
                    Message = $Matches[3]
                }
            }
        }
    }

    if ($Since) {
        $entries = $entries | Where-Object { $_.Timestamp -ge $Since }
    }
    if ($Level) {
        $entries = $entries | Where-Object { $_.Level -eq $Level }
    }

    return $entries | Sort-Object -Property Timestamp -Descending | Select-Object -First $Last
}

function Test-SystemStability {
    <#
    .SYNOPSIS
        Performs basic system stability checks
    .DESCRIPTION
        Verifies critical system components are functioning
    #>
    [CmdletBinding()]
    param()

    $results = @{
        OverallStatus = "OK"
        Checks = @()
    }

    # Check Windows Update service
    $wuService = Get-Service -Name wuauserv -ErrorAction SilentlyContinue
    $results.Checks += @{
        Name = "Windows Update Service"
        Status = if ($wuService.Status -eq "Running" -or $wuService.StartType -ne "Disabled") { "OK" } else { "Warning" }
    }

    # Check Windows Defender
    $defenderStatus = Get-MpComputerStatus -ErrorAction SilentlyContinue
    $results.Checks += @{
        Name = "Windows Defender"
        Status = if ($defenderStatus.AntivirusEnabled) { "OK" } else { "Warning" }
    }

    # Check critical services
    $criticalServices = @("RpcSs", "DcomLaunch", "LSM", "Schedule")
    foreach ($svc in $criticalServices) {
        $service = Get-Service -Name $svc -ErrorAction SilentlyContinue
        $status = if ($service.Status -eq "Running") { "OK" } else { "Critical" }
        $results.Checks += @{
            Name = "Service: $svc"
            Status = $status
        }
        if ($status -eq "Critical") {
            $results.OverallStatus = "Critical"
        }
    }

    # Check disk space
    $systemDrive = Get-PSDrive -Name C
    $freeGB = [math]::Round($systemDrive.Free / 1GB, 2)
    $results.Checks += @{
        Name = "System Drive Space"
        Status = if ($freeGB -gt 10) { "OK" } elseif ($freeGB -gt 5) { "Warning" } else { "Critical" }
        Details = "$freeGB GB free"
    }

    return $results
}

# Export functions
Export-ModuleMember -Function @(
    'New-SafetyRestorePoint',
    'New-RegistryBackup',
    'Restore-RegistryBackup',
    'New-TweakBackup',
    'Get-TweakBackups',
    'Add-LogEntry',
    'Get-LogEntries',
    'Test-SystemStability'
)
