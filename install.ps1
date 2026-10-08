param([string]$Source = (Join-Path $PSScriptRoot 'dist/win-x64'))
$ErrorActionPreference = 'Stop'
$sourcePath = (Resolve-Path -LiteralPath $Source).Path
$sourceExe = Join-Path $sourcePath 'TaskbarHardwareMonitor.exe'
if (-not (Test-Path -LiteralPath $sourceExe -PathType Leaf)) { throw "Executable not found: $sourceExe" }
if (Get-Process -Name 'TaskbarHardwareMonitor' -ErrorAction SilentlyContinue) { throw 'Exit Taskbar Hardware Monitor from the tray before installing or upgrading.' }
$destination = Join-Path $env:LOCALAPPDATA 'Programs/TaskbarHardwareMonitor'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
Get-ChildItem -LiteralPath $sourcePath -Force | Copy-Item -Destination $destination -Recurse -Force
$installedExe = Join-Path $destination 'TaskbarHardwareMonitor.exe'
Start-Process -FilePath $installedExe -ArgumentList '--startup' -Verb RunAs
Write-Host "Installed to $destination. Approve UAC so the elevated app can register its logon task."
