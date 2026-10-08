using System.Runtime.InteropServices;
using System.Text;
using TaskbarHardwareMonitor.Core;

namespace TaskbarHardwareMonitor.Native;

public static class Win32Interop
{
    internal const int GwlExstyle = -20;
    internal static readonly nint WsExTransparent = 0x20;
    internal static readonly nint WsExToolWindow = 0x80;
    internal static readonly nint WsExNoActivate = 0x08000000;
    internal const uint SwpNoActivate = 0x0010;
    internal static readonly nint HwndTopmost = new(-1);

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect { public int Left, Top, Right, Bottom; public readonly PixelRect Pixels => new(Left, Top, Right, Bottom); }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct MonitorInfo
    {
        public int Size;
        public Rect Monitor;
        public Rect Work;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string Device;
    }

    public sealed record DisplayMonitor(nint Handle, string Device, PixelRect Bounds, PixelRect WorkArea, bool Primary);

    private delegate bool EnumWindowsProc(nint window, nint param);
    private delegate bool MonitorEnumProc(nint monitor, nint hdc, ref Rect rect, nint param);

    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, nint param);
    [DllImport("user32.dll")] private static extern bool EnumDisplayMonitors(nint hdc, nint clip, MonitorEnumProc callback, nint param);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(nint hwnd, StringBuilder name, int maxCount);
    [DllImport("user32.dll")] private static extern nint MonitorFromWindow(nint window, uint flags);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint hwnd, out Rect rect);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern nint FindWindowEx(nint parent, nint after, string? className, string? title);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint hwnd, out uint processId);
    [DllImport("user32.dll")] internal static extern uint GetDpiForWindow(nint hwnd);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern nint GetWindowLongPtr(nint hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern nint SetWindowLongPtr(nint hwnd, int index, nint value);
    [DllImport("user32.dll")] internal static extern bool SetWindowPos(nint hwnd, nint insertAfter, int x, int y, int width, int height, uint flags);

    internal static void MakeOverlay(nint hwnd)
    {
        var style = GetWindowLongPtr(hwnd, GwlExstyle);
        SetWindowLongPtr(hwnd, GwlExstyle, style | WsExTransparent | WsExToolWindow | WsExNoActivate);
    }

    internal static IReadOnlyList<DisplayMonitor> Monitors()
    {
        var list = new List<DisplayMonitor>();
        EnumDisplayMonitors(0, 0, (nint handle, nint hdc, ref Rect bounds, nint data) =>
        {
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>(), Device = "" };
            if (GetMonitorInfo(handle, ref info)) list.Add(new DisplayMonitor(handle, info.Device, info.Monitor.Pixels, info.Work.Pixels, (info.Flags & 1) != 0));
            return true;
        }, 0);
        return list;
    }

    internal static bool TryGetTaskbar(DisplayMonitor monitor, out PixelRect taskbar, out PixelRect? tray)
    {
        PixelRect? found = null;
        PixelRect? foundTray = null;
        EnumWindows((window, _) =>
        {
            var name = new StringBuilder(128);
            GetClassName(window, name, name.Capacity);
            if (name.ToString() is not ("Shell_TrayWnd" or "Shell_SecondaryTrayWnd") || MonitorFromWindow(window, 2) != monitor.Handle || !IsWindowVisible(window)) return true;
            if (!GetWindowRect(window, out var rect)) return true;
            found = rect.Pixels;
            var trayWindow = FindWindowEx(window, 0, "TrayNotifyWnd", null);
            if (trayWindow != 0 && GetWindowRect(trayWindow, out var trayRect)) foundTray = trayRect.Pixels;
            return false;
        }, 0);
        taskbar = found ?? default;
        tray = foundTray;
        return found is not null;
    }

    internal static bool ShouldYieldToForeground(DisplayMonitor monitor, nint taskbarWindow)
    {
        var foreground = GetForegroundWindow();
        if (foreground == 0 || foreground == taskbarWindow) return false;
        var className = new StringBuilder(128);
        GetClassName(foreground, className, className.Capacity);
        if (className.ToString() is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd") return false;
        try
        {
            GetWindowThreadProcessId(foreground, out var pid);
            using var process = System.Diagnostics.Process.GetProcessById((int)pid);
            if (process.ProcessName is "StartMenuExperienceHost" or "ShellExperienceHost") return true;
        }
        catch (ArgumentException) { }
        catch (System.ComponentModel.Win32Exception) { }
        if (!GetWindowRect(foreground, out var bounds)) return false;
        var rect = bounds.Pixels;
        return rect.Left <= monitor.Bounds.Left && rect.Top <= monitor.Bounds.Top && rect.Right >= monitor.Bounds.Right && rect.Bottom >= monitor.Bounds.Bottom;
    }
}
