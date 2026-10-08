using LibreHardwareMonitor.Hardware;

StreamWriter? output = null;
if (args is ["--output", var outputPath])
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
    output = new StreamWriter(outputPath);
    Console.SetOut(output);
}

var computer = new Computer { IsCpuEnabled = true, IsGpuEnabled = true, IsMotherboardEnabled = true };
computer.Open();
try
{
    foreach (var hardware in computer.Hardware)
        Dump(hardware);
}
finally { computer.Close(); output?.Dispose(); }

static void Dump(IHardware hardware)
{
    hardware.Update();
    Console.WriteLine($"{hardware.HardwareType}: {hardware.Name} [{hardware.Identifier}]");
    foreach (var sensor in hardware.Sensors.Where(s => s.SensorType is SensorType.Temperature or SensorType.Load))
        Console.WriteLine($"  {sensor.SensorType}: {sensor.Name} = {sensor.Value?.ToString("0.0") ?? "N/A"} [{sensor.Identifier}]");
    foreach (var child in hardware.SubHardware) Dump(child);
}
