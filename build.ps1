param([switch]$Install)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
if (-not $dotnet) { $dotnet = 'C:\Program Files\dotnet\dotnet.exe' }
& $dotnet restore (Join-Path $root 'TaskbarHardwareMonitor.sln') --configfile (Join-Path $root 'NuGet.Config')
if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed' }
& $dotnet build (Join-Path $root 'TaskbarHardwareMonitor.sln') -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed' }
& $dotnet run --project (Join-Path $root 'tests/TaskbarHardwareMonitor.Tests/TaskbarHardwareMonitor.Tests.csproj') -c Release --no-build --no-restore
if ($LASTEXITCODE -ne 0) { throw 'unit tests failed' }
$publish = Join-Path $root 'dist/win-x64'
$appProject = Join-Path $root 'src/TaskbarHardwareMonitor/TaskbarHardwareMonitor.csproj'
& $dotnet restore $appProject -r win-x64 -p:SelfContained=true --configfile (Join-Path $root 'NuGet.Config')
if ($LASTEXITCODE -ne 0) { throw 'runtime restore failed' }
& $dotnet publish $appProject -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o $publish --no-restore
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }
Write-Host "Published to $publish"
if ($Install) { & (Join-Path $root 'install.ps1') -Source $publish }
