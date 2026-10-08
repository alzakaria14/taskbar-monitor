namespace TaskbarHardwareMonitor.Core;

public enum DisplayFormat { Full, Compact, TemperatureOnly, UtilizationOnly }
public enum DisplayTheme { Auto, Dark, Light }

public sealed class AppSettings
{
    public AppSettings Copy() => (AppSettings)MemberwiseClone();

    public bool StartWithWindows { get; set; } = true;
    public bool StartMinimized { get; set; } = true;
    public bool OverlayVisible { get; set; } = true;
    public int RefreshSeconds { get; set; } = 1;
    public int FontSize { get; set; } = 11;
    public DisplayFormat Format { get; set; } = DisplayFormat.Full;
    public DisplayTheme Theme { get; set; } = DisplayTheme.Auto;
    public int HorizontalOffset { get; set; }
    public int VerticalOffset { get; set; }
    public double BackgroundOpacity { get; set; } = 0.35;
    public string MonitorDevice { get; set; } = "";
    public string CpuDevice { get; set; } = "";
    public string CpuLoadSensor { get; set; } = "";
    public string CpuTemperatureSensor { get; set; } = "";
    public string GpuDevice { get; set; } = "";
    public string GpuLoadSensor { get; set; } = "";
    public string GpuTemperatureSensor { get; set; } = "";
    public int CpuWarningTemperature { get; set; } = 80;
    public int CpuCriticalTemperature { get; set; } = 95;
    public int GpuWarningTemperature { get; set; } = 80;
    public int GpuCriticalTemperature { get; set; } = 95;

    public void Validate()
    {
        if (RefreshSeconds is not (1 or 2 or 3 or 5)) RefreshSeconds = 1;
        FontSize = Math.Clamp(FontSize, 9, 24);
        HorizontalOffset = Math.Clamp(HorizontalOffset, -4000, 4000);
        VerticalOffset = Math.Clamp(VerticalOffset, -300, 300);
        BackgroundOpacity = double.IsFinite(BackgroundOpacity) ? Math.Clamp(BackgroundOpacity, 0, 1) : 0.35;
        if (!Enum.IsDefined(Format)) Format = DisplayFormat.Full;
        if (!Enum.IsDefined(Theme)) Theme = DisplayTheme.Auto;
        CpuWarningTemperature = Math.Clamp(CpuWarningTemperature, 1, 150);
        CpuCriticalTemperature = Math.Clamp(CpuCriticalTemperature, CpuWarningTemperature, 150);
        GpuWarningTemperature = Math.Clamp(GpuWarningTemperature, 1, 150);
        GpuCriticalTemperature = Math.Clamp(GpuCriticalTemperature, GpuWarningTemperature, 150);
    }
}
