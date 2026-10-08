using LibreHardwareMonitor.Hardware;
using TaskbarHardwareMonitor.Core;

namespace TaskbarHardwareMonitor.Services;

public sealed record SensorChoice(string Id, string Name);
public sealed record DeviceChoice(string Id, string Name, bool IsCpu, IReadOnlyList<SensorChoice> LoadSensors, IReadOnlyList<SensorChoice> TemperatureSensors);

public sealed class HardwareMonitorService : IDisposable
{
    private readonly CancellationTokenSource _stop = new();
    private readonly object _settingsLock = new();
    private AppSettings _settings;
    private Task? _worker;
    private volatile bool _rediscover;
    private bool _cpuTemperatureUnavailable;
    public event Action<SensorReading>? ReadingChanged;
    public event Action<IReadOnlyList<DeviceChoice>>? DevicesChanged;
    public event Action<string>? Error;

    public HardwareMonitorService(AppSettings settings) => _settings = settings.Copy();

    public void UpdateSettings(AppSettings settings)
    {
        lock (_settingsLock) _settings = settings.Copy();
    }

    public void Refresh() => _rediscover = true;
    public void Start() => _worker ??= Task.Run(() => RunAsync(_stop.Token));

    private async Task RunAsync(CancellationToken token)
    {
        Computer? computer = null;
        int failureCount = 0;
        while (!token.IsCancellationRequested)
        {
            var started = Environment.TickCount64;
            try
            {
                if (computer is null || _rediscover)
                {
                    _rediscover = false;
                    computer?.Close();
                    computer = new Computer { IsCpuEnabled = true, IsGpuEnabled = true };
                    computer.Open();
                    DevicesChanged?.Invoke(Discover(computer));
                }
                var settings = SnapshotSettings();
                foreach (var hardware in computer.Hardware)
                    UpdateTree(hardware);
                var reading = Read(computer, settings);
                ReadingChanged?.Invoke(reading);
                var cpuHasTemperatureSensors = computer.Hardware.Any(h => h.HardwareType == HardwareType.Cpu && AllSensors(h).Any(s => s.SensorType == SensorType.Temperature));
                var unavailable = cpuHasTemperatureSensors && reading.CpuTemperature is null;
                if (unavailable && !_cpuTemperatureUnavailable)
                    Error?.Invoke("Sensor suhu CPU terdeteksi tetapi nilainya kosong. Periksa akses driver/hak administrator dan sensor yang dipilih.");
                _cpuTemperatureUnavailable = unavailable;
                failureCount = 0;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                ReadingChanged?.Invoke(default);
                Error?.Invoke($"Pembacaan sensor gagal: {ex.Message}");
                if (++failureCount >= 3)
                {
                    try { computer?.Close(); } catch { }
                    computer = null;
                    failureCount = 0;
                }
            }
            var interval = SnapshotSettings().RefreshSeconds * 1000;
            try { await Task.Delay(Math.Max(100, interval - (int)(Environment.TickCount64 - started)), token); }
            catch (OperationCanceledException) { break; }
        }
        try { computer?.Close(); } catch { }
    }

    private static void UpdateTree(IHardware hardware)
    {
        hardware.Update();
        foreach (var child in hardware.SubHardware) UpdateTree(child);
    }

    private static IReadOnlyList<DeviceChoice> Discover(Computer computer) => computer.Hardware
        .Where(h => h.HardwareType is HardwareType.Cpu or HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel)
        .Select(h =>
        {
            var sensors = AllSensors(h).ToArray();
            return new DeviceChoice(h.Identifier.ToString(), h.Name, h.HardwareType == HardwareType.Cpu,
                sensors.Where(s => s.SensorType == SensorType.Load).Select(s => new SensorChoice(s.Identifier.ToString(), s.Name)).ToArray(),
                sensors.Where(s => s.SensorType == SensorType.Temperature).Select(s => new SensorChoice(s.Identifier.ToString(), s.Name)).ToArray());
        }).ToArray();

    private static IEnumerable<ISensor> AllSensors(IHardware hardware)
    {
        foreach (var sensor in hardware.Sensors) yield return sensor;
        foreach (var child in hardware.SubHardware)
            foreach (var sensor in AllSensors(child)) yield return sensor;
    }

    private static SensorReading Read(Computer computer, AppSettings settings)
    {
        var devices = computer.Hardware.Where(h => h.HardwareType is HardwareType.Cpu or HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel).ToArray();
        var cpu = ChooseDevice(devices.Where(h => h.HardwareType == HardwareType.Cpu), settings.CpuDevice);
        var gpu = ChooseDevice(devices.Where(h => h.HardwareType != HardwareType.Cpu), settings.GpuDevice);
        return new SensorReading(
            Value(cpu, SensorType.Load, settings.CpuLoadSensor, "total"),
            Value(cpu, SensorType.Temperature, settings.CpuTemperatureSensor, "package", "core (tctl/tdie)", "core average"),
            Value(gpu, SensorType.Load, settings.GpuLoadSensor, "core", "gpu core"),
            Value(gpu, SensorType.Temperature, settings.GpuTemperatureSensor, "core", "gpu core"));
    }

    private static IHardware? ChooseDevice(IEnumerable<IHardware> devices, string id)
        => devices.FirstOrDefault(h => h.Identifier.ToString() == id) ?? (string.IsNullOrEmpty(id) ? devices.FirstOrDefault() : null);

    private static float? Value(IHardware? hardware, SensorType type, string id, params string[] preferred)
    {
        if (hardware is null) return null;
        var sensors = AllSensors(hardware).Where(s => s.SensorType == type)
            .Select(s => new SensorSample(s.Identifier.ToString(), s.Name, s.Value));
        return SensorSelection.Select(sensors, id, preferred);
    }

    private AppSettings SnapshotSettings()
    {
        lock (_settingsLock) return _settings.Copy();
    }

    public async Task StopAsync()
    {
        _stop.Cancel();
        if (_worker is not null) await _worker;
    }

    public void Dispose() => _stop.Dispose();
}
