using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;

namespace TaskbarHardwareMonitor.Services;

public sealed class StartupService
{
    private const string Name = "TaskbarHardwareMonitor";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private readonly string _exePath = Environment.ProcessPath ?? "";

    public bool ValidateStartupPath()
    {
        if (string.IsNullOrWhiteSpace(_exePath) || !File.Exists(_exePath)) return false;
        var path = Path.GetFullPath(_exePath);
        if (path.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase)) return false;
        var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return !parts.Any(p => p.Equals("bin", StringComparison.OrdinalIgnoreCase) || p.Equals("obj", StringComparison.OrdinalIgnoreCase)
            || p.Equals("dist", StringComparison.OrdinalIgnoreCase) || p.Equals("publish", StringComparison.OrdinalIgnoreCase));
    }

    public bool IsStartupEnabled()
    {
        dynamic service = Connect();
        dynamic folder = service.GetFolder(@"\");
        try
        {
            dynamic task = folder.GetTask(Name);
            dynamic definition = task.Definition;
            dynamic action = definition.Actions.Item(1);
            return task.Enabled && definition.Principal.RunLevel == 1
                && string.Equals((string)action.Path, _exePath, StringComparison.OrdinalIgnoreCase)
                && string.Equals(((string)action.Arguments).Trim(), "--startup", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (TaskNotFound(ex)) { return false; }
    }

    public void EnableStartup()
    {
        if (!ValidateStartupPath()) throw new InvalidOperationException("Pindahkan aplikasi ke lokasi permanen melalui install.ps1 sebelum mengaktifkan Auto Start.");
        if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            throw new UnauthorizedAccessException("Jalankan aplikasi sebagai Administrator untuk mendaftarkan startup otomatis.");
        if (!IsStartupEnabled())
        {
            dynamic service = Connect();
            dynamic folder = service.GetFolder(@"\");
            dynamic definition = service.NewTask(0);
            var user = WindowsIdentity.GetCurrent().Name;
            definition.RegistrationInfo.Description = "Taskbar Hardware Monitor after user logon";
            definition.Principal.UserId = user;
            definition.Principal.LogonType = 3; // Interactive token: same desktop session as the taskbar.
            definition.Principal.RunLevel = 1; // Highest available privileges.
            dynamic trigger = definition.Triggers.Create(9); // Logon trigger.
            trigger.UserId = user;
            trigger.Delay = "PT10S";
            dynamic action = definition.Actions.Create(0); // Exec action.
            action.Path = _exePath;
            action.Arguments = "--startup";
            definition.Settings.DisallowStartIfOnBatteries = false;
            definition.Settings.StopIfGoingOnBatteries = false;
            definition.Settings.StartWhenAvailable = true;
            definition.Settings.ExecutionTimeLimit = "PT0S";
            definition.Settings.MultipleInstances = 2; // Ignore a second launch.
            folder.RegisterTaskDefinition(Name, definition, 6, user, null, 3, null); // Create or update.
        }
        RemoveLegacyRunEntry();
    }

    public void DisableStartup()
    {
        dynamic service = Connect();
        dynamic folder = service.GetFolder(@"\");
        try { folder.DeleteTask(Name, 0); }
        catch (Exception ex) when (TaskNotFound(ex)) { }
        RemoveLegacyRunEntry();
    }

    private static dynamic Connect()
    {
        var type = Type.GetTypeFromProgID("Schedule.Service") ?? throw new InvalidOperationException("Windows Task Scheduler tidak tersedia.");
        dynamic service = Activator.CreateInstance(type)!;
        service.Connect();
        return service;
    }

    private static bool TaskNotFound(Exception ex) => ex.HResult is unchecked((int)0x80070002) or unchecked((int)0x8004130F);

    private static void RemoveLegacyRunEntry()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
        key?.DeleteValue(Name, false);
    }
}
