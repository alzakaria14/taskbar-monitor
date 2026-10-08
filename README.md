# Taskbar Hardware Monitor

A lightweight Windows 11 taskbar overlay for actual CPU and GPU load and temperature readings. The app uses WPF, .NET 10, [LibreHardwareMonitorLib 0.9.6](https://www.nuget.org/packages/LibreHardwareMonitorLib/0.9.6), and documented Win32 window APIs. This installation runs with Administrator privileges because CPU temperature sensors on some systems require driver access. Sensors without a readable value still show `N/A`.

## Build and install

Requires Windows 11 x64 and the .NET 10 SDK. In PowerShell, from this directory:

```powershell
dotnet restore
dotnet build -c Release
dotnet run --project tests/TaskbarHardwareMonitor.Tests -c Release
dotnet publish src/TaskbarHardwareMonitor/TaskbarHardwareMonitor.csproj -c Release -r win-x64 --self-contained true
```

For a complete build, test, publish, and user-level install:

```powershell
.\build.ps1 -Install
```

`build.ps1` publishes to `dist/win-x64` as a self-contained folder. `install.ps1` copies those files to `%LOCALAPPDATA%\Programs\TaskbarHardwareMonitor`, then launches the installed executable with an explicit UAC prompt. WPF and the hardware library have native dependencies, so folder deployment is used for reliability. Do not move or delete individual files inside that folder.

Auto Start is **on by default** at the first launch from a permanent location. The elevated app registers a per-user Task Scheduler logon task named `TaskbarHardwareMonitor`, with an interactive user token and highest privileges. It starts after a short delay with `--startup`, without opening Settings or prompting again at login. The previous HKCU Run entry is removed during migration. Runs from `bin`, `obj`, `dist`, `publish`, or the temporary folder are deliberately not registered; install the app first. Moving an installed executable to a new permanent location updates the task action when the app next launches, as long as Auto Start is enabled. Disabling Auto Start in Settings removes the task immediately and remains disabled in `settings.json`. Manual launches show a UAC prompt.

## Use

The tray icon opens Settings on double click. Its menu can show or hide the overlay, refresh sensor discovery, toggle Auto Start, or exit. Exit stops polling and releases resources. Settings are saved to `%APPDATA%\TaskbarHardwareMonitor\settings.json`.

Select a monitor, CPU/GPU device, and individual load/temperature sensors if the automatic choices are unsuitable. By default the overlay sits immediately left of the system tray's upward arrow; a negative horizontal offset moves it further left. Select a display format, theme, font size, opacity, warning temperatures, and 1/2/3/5 second polling. The overlay ignores mouse clicks and keyboard focus. It hides if the selected bottom taskbar is unavailable, auto-hidden, a fullscreen app occupies the monitor, or it would overlap the detected tray. It periodically rechecks taskbar geometry so Explorer restart, display changes, and DPI changes are handled. The monitoring service refreshes hardware discovery after resume.

## Troubleshooting and limits

- `N/A` means the chosen sensor does not exist or has no current value. Try **Refresh hardware detection** and select another sensor. Automatic selection skips sensors whose current values are missing. CPU temperature can still be unavailable if driver access is blocked even with elevation.
- Windows 11 taskbar button positions are not exposed reliably through a stable public API. The tray-adjacent location is a best effort. Use the horizontal offset setting when pinned buttons, widgets, or third-party taskbars occupy that space. The tray region is explicitly avoided.
- Only taskbars at the **bottom** of a monitor are supported. The overlay deliberately hides for a vertical taskbar or one that cannot be located.
- A fullscreen window and some shell flyouts are detected periodically; a short transition may occur before the overlay hides.
- Startup registration needs the final installed path. Launching the executable from a build or temporary folder leaves Auto Start pending and shows an explanation in Settings.
- For a clean removal, exit from the tray and run `.\uninstall.ps1`. It removes the scheduled task, any legacy HKCU Run entry, and the installed files. User settings are retained in AppData.

## Manual Windows validation

After installing, confirm readings against Libre Hardware Monitor, test each format and sensor selection, and restart Windows to check Auto Start. Then disable Auto Start, restart again, re-enable it, and restart once more. Test 100%, 125%, 150%, and 200% scaling; secondary monitors with negative coordinates; resolution changes; Explorer restart; taskbar auto-hide; fullscreen apps; Start and notification flyouts; sleep/resume; and Exit. Check that the overlay does not intercept taskbar clicks.

The console test project verifies formatting, thresholds, JSON round trips, validation, overlay placement, tray avoidance, and multi-monitor coordinates without needing live hardware.

To inspect live LibreHardwareMonitor sensor names and values, run `dotnet run --project tools/SensorProbe/SensorProbe.csproj -c Release`. Compare the output from a normal terminal and an Administrator terminal when diagnosing CPU temperature access.

For source-level WPF compilation in an offline environment, `-p:OfflineCompileCheck=true` substitutes a throwing API shim from `tests/OfflineHardwareApiShim.cs`. This mode is only a compile check; its output is not a usable application. A normal restore, build, and publish always use the real LibreHardwareMonitor NuGet package.
