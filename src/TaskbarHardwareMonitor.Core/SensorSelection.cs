namespace TaskbarHardwareMonitor.Core;

public readonly record struct SensorSample(string Id, string Name, float? Value);

public static class SensorSelection
{
    public static float? Select(IEnumerable<SensorSample> samples, string selectedId, params string[] preferredNames)
    {
        var valid = samples.Where(s => s.Value is { } value && float.IsFinite(value)).ToArray();
        if (!string.IsNullOrEmpty(selectedId))
            return valid.FirstOrDefault(s => s.Id == selectedId).Value;
        foreach (var name in preferredNames)
        {
            var match = valid.FirstOrDefault(s => s.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            if (match.Value is not null) return match.Value;
        }
        return valid.FirstOrDefault().Value;
    }
}
