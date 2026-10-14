using System.Windows.Interop;
using System.Windows;
using TaskbarHardwareMonitor.Core;
using TaskbarHardwareMonitor.Native;

namespace TaskbarHardwareMonitor.Services;

public sealed class TaskbarOverlayService
{
    private readonly OverlayWindow _window;
    public TaskbarOverlayService(OverlayWindow window) => _window = window;

    public IReadOnlyList<Win32Interop.DisplayMonitor> Monitors() => Win32Interop.Monitors();

    public void Update(AppSettings settings)
    {
        if (!settings.OverlayVisible) { _window.Hide(); return; }
        var handle = new WindowInteropHelper(_window).EnsureHandle();
        // A taskbar overlay must not disappear merely because Explorer temporarily
        // fails to expose its taskbar or notification-area child windows.
        if (!_window.IsVisible) _window.Show();
        var monitors = Win32Interop.Monitors();
        var monitor = monitors.FirstOrDefault(m => m.Device == settings.MonitorDevice)
            ?? monitors.FirstOrDefault(m => m.Primary) ?? monitors.FirstOrDefault();
        if (monitor is null) return;
        var hasBottomTaskbar = Win32Interop.TryGetTaskbar(monitor, out var taskbar, out var tray)
            && taskbar.Top >= monitor.Bounds.Top
            && taskbar.Top < monitor.Bounds.Bottom - 2
            && taskbar.Bottom >= monitor.Bounds.Bottom - 2
            && taskbar.Height <= 120;
        if (!hasBottomTaskbar)
        {
            var fallbackHeight = Math.Max(1, monitor.Bounds.Bottom - monitor.WorkArea.Bottom);
            taskbar = new PixelRect(monitor.Bounds.Left, monitor.Bounds.Bottom - fallbackHeight, monitor.Bounds.Right, monitor.Bounds.Bottom);
            tray = null;
        }
        var scale = Win32Interop.GetDpiForWindow(handle) / 96.0;
        if (scale <= 0) scale = 1;
        _window.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
        var width = (int)Math.Ceiling(_window.DesiredSize.Width * scale);
        var height = (int)Math.Ceiling(_window.DesiredSize.Height * scale);
        if (width <= 0 || height <= 0) return;
        var position = OverlayPosition.Calculate(monitor.Bounds, taskbar, width, height, settings.HorizontalOffset, settings.VerticalOffset, tray);
        Win32Interop.SetWindowPos(handle, Win32Interop.HwndTopmost, position.Left, position.Top, width, height, Win32Interop.SwpNoActivate);
    }
}
