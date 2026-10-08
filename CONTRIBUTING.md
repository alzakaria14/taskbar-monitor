# Contributing to Taskbar Hardware Monitor

Thank you for contributing. This project is a native Windows 11 hardware monitor built with C#, .NET 10, WPF, Win32 APIs, and LibreHardwareMonitorLib.

## Before you begin

- Use Windows 11 x64 and the .NET 10 SDK.
- Build from a normal PowerShell session. Run the app as Administrator only when testing CPU temperature access that requires it.
- Keep changes focused. Do not include `bin`, `obj`, `dist`, AppData settings, or local installation files in commits.
- Follow the existing nullable C# and implicit-using conventions. Prefer clear names and small, testable services.

## Development workflow

1. Fork the repository and create a branch from the default branch.
2. Make the change in the relevant project under `src/`.
3. Add or update tests under `tests/` for behavior that can be verified without live hardware.
4. Run the checks below.
5. Open a pull request that explains the user-visible change, implementation notes, and testing performed.

```powershell
dotnet restore
dotnet build -c Release
dotnet run --project tests/TaskbarHardwareMonitor.Tests -c Release
```

`build.ps1` runs restore, build, tests, and publish. Use `build.ps1 -Install` only when you want to replace your local installation. The installed app requests Administrator privileges because some CPU temperature sensors need driver access.

## Testing hardware and overlay changes

For sensor changes, test with the CPU and GPU available on your machine. The sensor probe lists LibreHardwareMonitor values and helps compare normal versus elevated access:

```powershell
dotnet run --project tools/SensorProbe/SensorProbe.csproj -c Release
```

For overlay changes, manually check 100%, 125%, 150%, and 200% DPI scaling; taskbar auto-hide; Explorer restart; sleep/resume; fullscreen applications; system tray interaction; and secondary monitors. The overlay must remain click-through and must not overlap the notification area.

## Pull request guidelines

- Describe the problem and the approach taken.
- Include test commands and their results.
- Include screenshots for visible UI or overlay changes.
- State any hardware, Windows version, DPI scale, or monitor setup used for manual verification.
- Do not submit credentials, machine-specific settings, generated binaries, or unrelated formatting changes.

## Reporting issues

Include Windows version, .NET SDK version, CPU/GPU model, display scale, taskbar location, expected result, actual result, and relevant logs or screenshots. Do not publish personally sensitive information.

## License

By submitting a contribution, you agree that your contribution is licensed under the [MIT License](LICENCE.md).
