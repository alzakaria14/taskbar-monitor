namespace TaskbarHardwareMonitor.Core;

public enum TemperatureLevel { Normal, Warning, Critical }

public readonly record struct SensorReading(float? CpuLoad, float? CpuTemperature, float? GpuLoad, float? GpuTemperature);

public static class ReadingFormatter
{
    public static string Number(float? value, string suffix)
        => value is { } n && float.IsFinite(n) ? $"{Math.Round(n, MidpointRounding.AwayFromZero):0}{suffix}" : "N/A";

    public static (string Cpu, string Gpu) Parts(SensorReading reading, DisplayFormat format) => format switch
    {
        DisplayFormat.Compact => ($"C {Number(reading.CpuLoad, "%")} {Number(reading.CpuTemperature, "°")}", $"G {Number(reading.GpuLoad, "%")} {Number(reading.GpuTemperature, "°")}"),
        DisplayFormat.TemperatureOnly => ($"CPU {Number(reading.CpuTemperature, "°C")}", $"GPU {Number(reading.GpuTemperature, "°C")}"),
        DisplayFormat.UtilizationOnly => ($"CPU {Number(reading.CpuLoad, "%")}", $"GPU {Number(reading.GpuLoad, "%")}"),
        _ => ($"CPU {Number(reading.CpuLoad, "%")} {Number(reading.CpuTemperature, "°C")}", $"GPU {Number(reading.GpuLoad, "%")} {Number(reading.GpuTemperature, "°C")}")
    };

    public static TemperatureLevel Level(float? value, int warning, int critical)
        => value is null || !float.IsFinite(value.Value) ? TemperatureLevel.Normal
            : value >= critical ? TemperatureLevel.Critical
            : value >= warning ? TemperatureLevel.Warning : TemperatureLevel.Normal;
}
