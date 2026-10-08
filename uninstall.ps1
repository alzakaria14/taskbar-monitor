$ErrorActionPreference = 'Stop'
$destination = Join-Path $env:LOCALAPPDATA 'Programs/TaskbarHardwareMonitor'
if (Get-Process -Name 'TaskbarHardwareMonitor' -ErrorAction SilentlyContinue) { throw 'Exit Taskbar Hardware Monitor from the tray before uninstalling.' }
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    $helper = Start-Process -FilePath 'powershell.exe' -Verb RunAs -Wait -PassThru -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    if ($helper.ExitCode -ne 0) { throw "Elevated uninstall failed with exit code $($helper.ExitCode)." }
    return
}
$task = Get-ScheduledTask -TaskName 'TaskbarHardwareMonitor' -ErrorAction SilentlyContinue
if ($task) { Unregister-ScheduledTask -TaskName 'TaskbarHardwareMonitor' -Confirm:$false }
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
Remove-ItemProperty -Path $runKey -Name 'TaskbarHardwareMonitor' -ErrorAction SilentlyContinue
if (Test-Path -LiteralPath $destination) {
    Remove-Item -LiteralPath $destination -Recurse -Force
}
Write-Host 'Uninstalled. AppData settings were retained.'
