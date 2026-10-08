using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using TaskbarHardwareMonitor.Core;
using TaskbarHardwareMonitor.Services;
using Forms = System.Windows.Forms;

namespace TaskbarHardwareMonitor;

public partial class App : System.Windows.Application
{
    private Mutex? _mutex;
    private AppSettings _settings = new();
    private SettingsService? _settingsService;
    private StartupService? _startup;
    private HardwareMonitorService? _hardware;
    private OverlayWindow? _overlay;
    private TaskbarOverlayService? _position;
    private SettingsWindow? _settingsWindow;
    private Forms.NotifyIcon? _tray;
    private Forms.ToolStripMenuItem? _showItem;
    private Forms.ToolStripMenuItem? _startupItem;
    private DispatcherTimer? _timer;
    private IReadOnlyList<DeviceChoice> _devices = Array.Empty<DeviceChoice>();
    private string? _status;
    private bool _closing;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _mutex = new Mutex(true, @"Local\TaskbarHardwareMonitor.SingleInstance", out bool first);
        if (!first) { _mutex.Dispose(); _mutex = null; Shutdown(); return; }
        _settingsService = new SettingsService();
        _settings = _settingsService.Load();
        _status = _settingsService.LastError;
        _startup = new StartupService();
        if (_settingsService.LastError is not null)
        {
            try { _settings.StartWithWindows = _startup.IsStartupEnabled(); }
            catch { _settings.StartWithWindows = false; }
        }
        if (_settingsService.LastError is null) SyncStartup();
        _overlay = new OverlayWindow();
        _overlay.SetSettings(_settings);
        _position = new TaskbarOverlayService(_overlay);
        _hardware = new HardwareMonitorService(_settings);
        _hardware.ReadingChanged += reading => Dispatcher.BeginInvoke(() => { _overlay?.SetReading(reading); UpdateOverlay(); });
        _hardware.DevicesChanged += devices => Dispatcher.BeginInvoke(() => { _devices = devices; _settingsWindow?.UpdateDevices(devices); });
        _hardware.Error += error => Dispatcher.BeginInvoke(() => { _status = error; _settingsWindow?.SetStatus(error); });
        _hardware.Start();
        BuildTray();
        _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_, _) => UpdateOverlay(), Dispatcher);
        _timer.Start();
        SystemEvents.DisplaySettingsChanged += SystemDisplayChanged;
        SystemEvents.PowerModeChanged += SystemPowerChanged;
        SessionEnding += (_, _) => _hardware?.Refresh();
        if (!_settings.StartMinimized && !e.Args.Contains("--startup")) OpenSettings();
        UpdateOverlay();
    }

    private void SystemDisplayChanged(object? sender, EventArgs e) => Dispatcher.BeginInvoke(UpdateOverlay);
    private void SystemPowerChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume) Dispatcher.BeginInvoke(() => { _hardware?.Refresh(); UpdateOverlay(); });
    }

    private void UpdateOverlay()
    {
        if (!_closing && _position is not null)
        {
            try { _position.Update(_settings); }
            catch (Exception ex) { _status = $"Overlay: {ex.Message}"; _overlay?.Hide(); }
        }
    }

    private void BuildTray()
    {
        var menu = new Forms.ContextMenuStrip();
        _showItem = new Forms.ToolStripMenuItem("Show Overlay") { Checked = _settings.OverlayVisible, CheckOnClick = false };
        _showItem.Click += (_, _) => { _settings.OverlayVisible = !_settings.OverlayVisible; SaveAndApply(); };
        menu.Items.Add(_showItem);
        menu.Items.Add("Settings", null, (_, _) => OpenSettings());
        menu.Items.Add("Refresh Sensors", null, (_, _) => _hardware?.Refresh());
        _startupItem = new Forms.ToolStripMenuItem("Start with Windows") { Checked = _settings.StartWithWindows, CheckOnClick = false };
        _startupItem.Click += (_, _) => { _settings.StartWithWindows = !_settings.StartWithWindows; SaveAndApply(); };
        menu.Items.Add(_startupItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, async (_, _) => await ExitAsync());
        _tray = new Forms.NotifyIcon { Icon = System.Drawing.SystemIcons.Application, Text = "Taskbar Hardware Monitor", ContextMenuStrip = menu, Visible = true };
        _tray.DoubleClick += (_, _) => OpenSettings();
    }

    private void OpenSettings()
    {
        if (_settingsWindow is not null) { _settingsWindow.Activate(); return; }
        _settingsWindow = new SettingsWindow(_settings, SaveAndApply, () => _hardware?.Refresh(), _position?.Monitors() ?? [], _devices);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.SetStatus(_status);
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void SaveAndApply()
    {
        try { _settingsService?.Save(_settings); _status = null; }
        catch (Exception ex) { _status = $"Pengaturan gagal disimpan: {ex.Message}"; _settingsWindow?.SetStatus(_status); return; }
        SyncStartup();
        _overlay?.SetSettings(_settings);
        _hardware?.UpdateSettings(_settings);
        if (_showItem is not null) _showItem.Checked = _settings.OverlayVisible;
        if (_startupItem is not null) _startupItem.Checked = _settings.StartWithWindows;
        _settingsWindow?.SetStatus(_status);
        UpdateOverlay();
    }

    private void SyncStartup()
    {
        try
        {
            if (_settings.StartWithWindows) _startup?.EnableStartup();
            else _startup?.DisableStartup();
        }
        catch (Exception ex) { _status = $"Auto Start: {ex.Message}"; }
    }

    private async Task ExitAsync()
    {
        if (_closing) return;
        _closing = true;
        _timer?.Stop();
        SystemEvents.DisplaySettingsChanged -= SystemDisplayChanged;
        SystemEvents.PowerModeChanged -= SystemPowerChanged;
        if (_tray is not null) { _tray.Visible = false; _tray.Dispose(); _tray = null; }
        _settingsWindow?.Close();
        _overlay?.Close();
        if (_hardware is not null) { await _hardware.StopAsync(); _hardware.Dispose(); }
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_mutex is not null) { _mutex.ReleaseMutex(); _mutex.Dispose(); }
        base.OnExit(e);
    }
}
