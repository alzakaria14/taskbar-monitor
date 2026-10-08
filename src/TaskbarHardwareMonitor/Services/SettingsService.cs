using System.IO;
using System.Text.Json;
using TaskbarHardwareMonitor.Core;

namespace TaskbarHardwareMonitor.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public SettingsService(string? path = null) => Path = path ?? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TaskbarHardwareMonitor", "settings.json");
    public string Path { get; }
    public string? LastError { get; private set; }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(Path)) return new AppSettings();
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(Path), JsonOptions) ?? new AppSettings();
            settings.Validate();
            return settings;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            LastError = $"Settings tidak dapat dibaca: {ex.Message}";
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        settings.Validate();
        var directory = System.IO.Path.GetDirectoryName(Path)!;
        Directory.CreateDirectory(directory);
        var temporary = Path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temporary, Path, true);
    }
}
