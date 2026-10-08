using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TaskbarHardwareMonitor.Core;
using TaskbarHardwareMonitor.Native;
using ComboBox = System.Windows.Controls.ComboBox;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using TextBox = System.Windows.Controls.TextBox;
using Brushes = System.Windows.Media.Brushes;
using TaskbarHardwareMonitor.Services;

namespace TaskbarHardwareMonitor;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly Action _save;
    private readonly Action _refresh;
    private readonly ComboBox _monitorCombo;
    private readonly ComboBox _cpuCombo;
    private readonly ComboBox _gpuCombo;
    private readonly ComboBox _cpuLoadCombo;
    private readonly ComboBox _cpuTempCombo;
    private readonly ComboBox _gpuLoadCombo;
    private readonly ComboBox _gpuTempCombo;
    private readonly TextBlock _status;
    private bool _loading = true;

    public SettingsWindow(AppSettings settings, Action save, Action refresh, IReadOnlyList<Win32Interop.DisplayMonitor> monitors, IReadOnlyList<DeviceChoice> devices)
    {
        InitializeComponent();
        _settings = settings;
        _save = save;
        _refresh = refresh;
        Heading("General");
        Check("Start automatically with Windows (Administrator)", settings.StartWithWindows, v => settings.StartWithWindows = v);
        Check("Start minimized", settings.StartMinimized, v => settings.StartMinimized = v);
        Combo("Refresh interval", new[] { new Option("1 second", "1"), new Option("2 seconds", "2"), new Option("3 seconds", "3"), new Option("5 seconds", "5") }, settings.RefreshSeconds.ToString(), v => settings.RefreshSeconds = int.Parse(v));
        Heading("Display");
        Number("Font size", settings.FontSize, 9, 24, v => settings.FontSize = v);
        Combo("Format", Enum.GetNames<DisplayFormat>().Select(v => new Option(v, v)), settings.Format.ToString(), v => settings.Format = Enum.Parse<DisplayFormat>(v));
        Number("Horizontal offset from tray (pixels)", settings.HorizontalOffset, -4000, 4000, v => settings.HorizontalOffset = v);
        Number("Vertical offset (pixels)", settings.VerticalOffset, -300, 300, v => settings.VerticalOffset = v);
        Number("Background opacity (%)", (int)Math.Round(settings.BackgroundOpacity * 100), 0, 100, v => settings.BackgroundOpacity = v / 100.0);
        Combo("Theme", Enum.GetNames<DisplayTheme>().Select(v => new Option(v, v)), settings.Theme.ToString(), v => settings.Theme = Enum.Parse<DisplayTheme>(v));
        _monitorCombo = Combo("Monitor", Array.Empty<Option>(), settings.MonitorDevice, v => settings.MonitorDevice = v);
        SetOptions(_monitorCombo, new[] { new Option("Primary monitor (automatic)", "") }.Concat(monitors.Select(m => new Option($"{m.Device} ({m.Bounds.Width}×{m.Bounds.Height})", m.Device))), settings.MonitorDevice);
        Heading("Hardware");
        _cpuCombo = Combo("CPU", Array.Empty<Option>(), settings.CpuDevice, v => { settings.CpuDevice = v; UpdateSensorOptions(); });
        _cpuLoadCombo = Combo("CPU load sensor", Array.Empty<Option>(), settings.CpuLoadSensor, v => settings.CpuLoadSensor = v);
        _cpuTempCombo = Combo("CPU temperature sensor", Array.Empty<Option>(), settings.CpuTemperatureSensor, v => settings.CpuTemperatureSensor = v);
        _gpuCombo = Combo("GPU", Array.Empty<Option>(), settings.GpuDevice, v => { settings.GpuDevice = v; UpdateSensorOptions(); });
        _gpuLoadCombo = Combo("GPU load sensor", Array.Empty<Option>(), settings.GpuLoadSensor, v => settings.GpuLoadSensor = v);
        _gpuTempCombo = Combo("GPU temperature sensor", Array.Empty<Option>(), settings.GpuTemperatureSensor, v => settings.GpuTemperatureSensor = v);
        var refreshButton = new Button { Content = "Refresh hardware detection", Margin = new Thickness(0, 8, 0, 4), Padding = new Thickness(10, 6, 10, 6), HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
        refreshButton.Click += (_, _) => _refresh();
        Fields.Children.Add(refreshButton);
        Heading("Warnings");
        Number("CPU warning temperature (°C)", settings.CpuWarningTemperature, 1, 150, v => settings.CpuWarningTemperature = v);
        Number("CPU critical temperature (°C)", settings.CpuCriticalTemperature, 1, 150, v => settings.CpuCriticalTemperature = v);
        Number("GPU warning temperature (°C)", settings.GpuWarningTemperature, 1, 150, v => settings.GpuWarningTemperature = v);
        Number("GPU critical temperature (°C)", settings.GpuCriticalTemperature, 1, 150, v => settings.GpuCriticalTemperature = v);
        _status = new TextBlock { Foreground = Brushes.Orange, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 4) };
        Fields.Children.Add(_status);
        _loading = false;
        UpdateDevices(devices);
    }

    public void SetStatus(string? message) => _status.Text = message ?? "";

    public void UpdateDevices(IReadOnlyList<DeviceChoice> devices)
    {
        _devices = devices;
        _loading = true;
        SetOptions(_cpuCombo, new[] { new Option("Automatic", "") }.Concat(devices.Where(d => d.IsCpu).Select(d => new Option(d.Name, d.Id))), _settings.CpuDevice);
        SetOptions(_gpuCombo, new[] { new Option("Automatic", "") }.Concat(devices.Where(d => !d.IsCpu).Select(d => new Option(d.Name, d.Id))), _settings.GpuDevice);
        UpdateSensorOptions();
        _loading = false;
    }

    private IReadOnlyList<DeviceChoice> _devices = Array.Empty<DeviceChoice>();

    private void UpdateSensorOptions()
    {
        if (_cpuLoadCombo is null || _gpuLoadCombo is null) return;
        var cpu = _devices.FirstOrDefault(d => d.IsCpu && d.Id == _settings.CpuDevice) ?? (_settings.CpuDevice == "" ? _devices.FirstOrDefault(d => d.IsCpu) : null);
        var gpu = _devices.FirstOrDefault(d => !d.IsCpu && d.Id == _settings.GpuDevice) ?? (_settings.GpuDevice == "" ? _devices.FirstOrDefault(d => !d.IsCpu) : null);
        bool wasLoading = _loading;
        _loading = true;
        SetOptions(_cpuLoadCombo, SensorOptions(cpu?.LoadSensors), _settings.CpuLoadSensor);
        SetOptions(_cpuTempCombo, SensorOptions(cpu?.TemperatureSensors), _settings.CpuTemperatureSensor);
        SetOptions(_gpuLoadCombo, SensorOptions(gpu?.LoadSensors), _settings.GpuLoadSensor);
        SetOptions(_gpuTempCombo, SensorOptions(gpu?.TemperatureSensors), _settings.GpuTemperatureSensor);
        _loading = wasLoading;
    }

    private static IEnumerable<Option> SensorOptions(IReadOnlyList<SensorChoice>? sensors) => new[] { new Option("Automatic", "") }.Concat(sensors?.Select(s => new Option(s.Name, s.Id)) ?? []);

    private static void SetOptions(ComboBox box, IEnumerable<Option> options, string selected)
    {
        var available = options.ToList();
        if (!string.IsNullOrEmpty(selected) && available.All(option => option.Value != selected))
            available.Add(new Option($"Unavailable: {selected}", selected));
        box.ItemsSource = available;
        box.SelectedValue = selected;
        if (box.SelectedItem is null) box.SelectedIndex = 0;
    }

    private void Heading(string label) => Fields.Children.Add(new TextBlock { Text = label, FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = Brushes.LightSkyBlue, Margin = new Thickness(0, 14, 0, 6) });

    private void Check(string label, bool value, Action<bool> change)
    {
        var box = new CheckBox { Content = label, IsChecked = value, Margin = new Thickness(0, 3, 0, 6) };
        box.Checked += (_, _) => { if (!_loading) { change(true); _save(); } };
        box.Unchecked += (_, _) => { if (!_loading) { change(false); _save(); } };
        Fields.Children.Add(box);
    }

    private ComboBox Combo(string label, IEnumerable<Option> options, string selected, Action<string> change)
    {
        Label(label);
        var box = new ComboBox { Margin = new Thickness(0, 0, 0, 6), DisplayMemberPath = nameof(Option.Label), SelectedValuePath = nameof(Option.Value), ItemsSource = options.ToArray() };
        box.SelectedValue = selected;
        box.SelectionChanged += (_, _) => { if (!_loading && box.SelectedValue is string v) { change(v); _save(); } };
        Fields.Children.Add(box);
        return box;
    }

    private void Number(string label, int value, int min, int max, Action<int> change)
    {
        Label(label);
        var lastAccepted = value;
        var box = new TextBox { Text = value.ToString(), Margin = new Thickness(0, 0, 0, 6), Padding = new Thickness(5) };
        box.LostFocus += (_, _) =>
        {
            if (int.TryParse(box.Text, out var parsed) && parsed >= min && parsed <= max) { lastAccepted = parsed; change(parsed); _save(); }
            else { box.Text = lastAccepted.ToString(); SetStatus($"{label}: masukkan angka antara {min} dan {max}."); }
        };
        Fields.Children.Add(box);
    }

    private void Label(string label) => Fields.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 4, 0, 3) });
    private sealed record Option(string Label, string Value);
}
