using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using Microsoft.Win32;
using TaskbarHardwareMonitor.Core;
using TaskbarHardwareMonitor.Native;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace TaskbarHardwareMonitor;

public partial class OverlayWindow : Window
{
    private SensorReading _reading;
    private AppSettings _settings = new();

    public OverlayWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            var handle = new WindowInteropHelper(this).Handle;
            Win32Interop.MakeOverlay(handle);
            HwndSource.FromHwnd(handle)?.AddHook((nint hwnd, int message, nint wParam, nint lParam, ref bool handled) =>
            {
                if (message == 0x0084) { handled = true; return new nint(-1); }
                return 0;
            });
        };
        Render();
    }

    public void SetReading(SensorReading reading)
    {
        if (_reading == reading) return;
        _reading = reading;
        Render();
    }

    public void SetSettings(AppSettings settings)
    {
        _settings = settings;
        Render();
    }

    private void Render()
    {
        var parts = ReadingFormatter.Parts(_reading, _settings.Format);
        CpuText.Text = parts.Cpu;
        GpuText.Text = parts.Gpu;
        CpuText.FontSize = GpuText.FontSize = _settings.FontSize;
        var theme = _settings.Theme == DisplayTheme.Auto ? SystemTheme() : _settings.Theme;
        var background = theme == DisplayTheme.Light ? Colors.White : Color.FromRgb(13, 17, 23);
        Panel.Background = new SolidColorBrush(Color.FromArgb((byte)Math.Round(_settings.BackgroundOpacity * 255), background.R, background.G, background.B));
        CpuText.Foreground = BrushFor(_reading.CpuTemperature, _settings.CpuWarningTemperature, _settings.CpuCriticalTemperature, theme == DisplayTheme.Light ? Color.FromRgb(3, 105, 161) : Color.FromRgb(125, 211, 252));
        GpuText.Foreground = BrushFor(_reading.GpuTemperature, _settings.GpuWarningTemperature, _settings.GpuCriticalTemperature, theme == DisplayTheme.Light ? Color.FromRgb(13, 148, 136) : Color.FromRgb(94, 234, 212));
    }

    private static Brush BrushFor(float? temperature, int warning, int critical, Color normal) =>
        new SolidColorBrush(ReadingFormatter.Level(temperature, warning, critical) switch
        {
            TemperatureLevel.Critical => Color.FromRgb(248, 113, 113),
            TemperatureLevel.Warning => Color.FromRgb(251, 146, 60),
            _ => normal
        });

    private static DisplayTheme SystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("SystemUsesLightTheme") is int light && light != 0 ? DisplayTheme.Light : DisplayTheme.Dark;
        }
        catch { return DisplayTheme.Dark; }
    }
}
