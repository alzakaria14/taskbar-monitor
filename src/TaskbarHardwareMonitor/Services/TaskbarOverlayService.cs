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
        var monitors = Win32Interop.Monitors();
        var monitor = monitors.FirstOrDefault(m => m.Device == settings.MonitorDevice)
            ?? monitors.FirstOrDefault(m => m.Primary) ?? monitors.FirstOrDefault();
        if (monitor is null || !Win32Interop.TryGetTaskbar(monitor, out var taskbar, out var tray) || taskbar.Top < monitor.Bounds.Top || taskbar.Top >= monitor.Bounds.Bottom - 2 || taskbar.Bottom < monitor.Bounds.Bottom - 2 || taskbar.Height > 120)
        {
            _window.Hide();
            return;
        }
        var handle = new WindowInteropHelper(_window).EnsureHandle();
        if (Win32Interop.ShouldYieldToForeground(monitor, handle)) { _window.Hide(); return; }
        var scale = Win32Interop.GetDpiForWindow(handle) / 96.0;
        if (scale <= 0) scale = 1;
        _window.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
        var width = (int)Math.Ceiling(_window.DesiredSize.Width * scale);
        var height = (int)Math.Ceiling(_window.DesiredSize.Height * scale);
        if (width <= 0 || height <= 0) return;
        var position = OverlayPosition.Calculate(monitor.Bounds, taskbar, width, height, settings.HorizontalOffset, settings.VerticalOffset, tray);
        if (tray is { } reserved && position.Intersects(reserved)) { _window.Hide(); return; }
        if (!_window.IsVisible) _window.Show();
        Win32Interop.SetWindowPos(handle, Win32Interop.HwndTopmost, position.Left, position.Top, width, height, Win32Interop.SwpNoActivate);
    }
}
