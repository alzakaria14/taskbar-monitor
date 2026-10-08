using System.Text.Json;
using TaskbarHardwareMonitor.Core;
using TaskbarHardwareMonitor.Services;

int passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception($"FAILED: {name}");
    Console.WriteLine($"PASS: {name}");
    passed++;
}

var reading = new SensorReading(18.4f, 52.1f, 41.2f, null);
Check(ReadingFormatter.Parts(reading, DisplayFormat.Full) == ("CPU 18% 52°C", "GPU 41% N/A"), "full format and absent sensor");
Check(ReadingFormatter.Parts(reading, DisplayFormat.Compact) == ("C 18% 52°", "G 41% N/A"), "compact format");
Check(ReadingFormatter.Parts(reading, DisplayFormat.TemperatureOnly) == ("CPU 52°C", "GPU N/A"), "temperature only");
Check(ReadingFormatter.Parts(reading, DisplayFormat.UtilizationOnly) == ("CPU 18%", "GPU 41%"), "utilization only");
Check(ReadingFormatter.Level(null, 80, 95) == TemperatureLevel.Normal && ReadingFormatter.Level(80, 80, 95) == TemperatureLevel.Warning && ReadingFormatter.Level(95, 80, 95) == TemperatureLevel.Critical, "temperature thresholds");

var settings = new AppSettings { StartWithWindows = false, RefreshSeconds = 5, GpuDevice = "/gpu-nvidia/0", HorizontalOffset = -17 };
var copy = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(settings))!;
Check(!copy.StartWithWindows && copy.RefreshSeconds == 5 && copy.GpuDevice == "/gpu-nvidia/0" && copy.HorizontalOffset == -17, "settings JSON round trip");
var temp = Directory.CreateTempSubdirectory("taskbar-monitor-tests-");
try
{
    var service = new SettingsService(Path.Combine(temp.FullName, "settings.json"));
    service.Save(settings);
    Check(service.Load().GpuDevice == settings.GpuDevice && !service.Load().StartWithWindows, "settings service save and load");
    File.WriteAllText(service.Path, "{broken");
    Check(service.Load().RefreshSeconds == 1 && service.LastError is not null, "corrupt settings fallback");
}
finally { temp.Delete(true); }
copy.RefreshSeconds = 17;
copy.CpuCriticalTemperature = 50;
copy.Validate();
Check(copy.RefreshSeconds == 1 && copy.CpuCriticalTemperature >= copy.CpuWarningTemperature, "settings validation");

var primary = new PixelRect(0, 0, 1920, 1080);
var bar = new PixelRect(0, 1032, 1920, 1080);
var pos = OverlayPosition.Calculate(primary, bar, 180, 24, 0, 0, new PixelRect(1700, 1032, 1920, 1080));
Check(pos == new PixelRect(1512, 1044, 1692, 1068), "tray-adjacent taskbar placement");
var second = new PixelRect(-2560, -200, 0, 1240);
var secondBar = new PixelRect(-2560, 1192, 0, 1240);
var secondPos = OverlayPosition.Calculate(second, secondBar, 200, 24, 0, 0, null);
Check(secondPos.Left == -448 && secondPos.Top == 1204, "negative multi monitor coordinates");
var trayAvoid = OverlayPosition.Calculate(primary, bar, 180, 24, 1700, 0, new PixelRect(1700, 1032, 1920, 1080));
Check(!trayAvoid.Intersects(new PixelRect(1700, 1032, 1920, 1080)), "system tray avoidance");
Check(OverlayPosition.Calculate(primary, bar, 180, 24, -25, 0, new PixelRect(1700, 1032, 1920, 1080)).Left == 1487, "manual offset from tray");
var cpuSamples = new[] { new SensorSample("package", "CPU Package", null), new SensorSample("average", "Core Average", 54.2f) };
Check(SensorSelection.Select(cpuSamples, "", "package", "core average") == 54.2f, "automatic sensor skips missing reading");
Check(SensorSelection.Select(cpuSamples, "package", "core average") is null, "explicit sensor selection remains exact");
Check(!new StartupService().IsStartupEnabled(), "Task Scheduler rejects a task for a different executable");
Console.WriteLine($"{passed} tests passed.");
