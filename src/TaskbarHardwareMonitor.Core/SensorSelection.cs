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

    /// <summary>
    /// Matches Task Manager's overall GPU reading by reporting the busiest
    /// Direct3D engine. A manually selected sensor remains an exact override.
    /// </summary>
    public static float? SelectTaskManagerGpuUtilization(IEnumerable<SensorSample> samples, string selectedId)
    {
        var valid = samples.Where(s => s.Value is { } value && float.IsFinite(value)).ToArray();
        if (!string.IsNullOrEmpty(selectedId))
            return valid.FirstOrDefault(s => s.Id == selectedId).Value;

        var d3dEngine = valid
            .Where(s => s.Name.StartsWith("D3D ", StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Value!.Value)
            .DefaultIfEmpty(float.NaN)
            .Max();

        if (float.IsFinite(d3dEngine))
            return Math.Clamp(d3dEngine, 0f, 100f);

        var fallback = Select(valid, "", "gpu core", "core");
        return fallback is { } value ? Math.Clamp(value, 0f, 100f) : null;
    }
}
