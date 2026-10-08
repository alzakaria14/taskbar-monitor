// Compile-time API stand-in for an offline sandbox. Never included in a normal build.
namespace LibreHardwareMonitor.Hardware;

public enum HardwareType { Cpu, GpuNvidia, GpuAmd, GpuIntel }
public enum SensorType { Load, Temperature }

public interface ISensor
{
    SensorType SensorType { get; }
    string Identifier { get; }
    string Name { get; }
    float? Value { get; }
}

public interface IHardware
{
    HardwareType HardwareType { get; }
    string Identifier { get; }
    string Name { get; }
    IEnumerable<IHardware> SubHardware { get; }
    IEnumerable<ISensor> Sensors { get; }
    void Update();
}

public sealed class Computer
{
    public bool IsCpuEnabled { get; set; }
    public bool IsGpuEnabled { get; set; }
    public IEnumerable<IHardware> Hardware => throw new NotSupportedException("Compile check only");
    public void Open() => throw new NotSupportedException("Compile check only");
    public void Close() => throw new NotSupportedException("Compile check only");
}
