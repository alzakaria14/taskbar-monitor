namespace TaskbarHardwareMonitor.Core;

public readonly record struct PixelRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;
    public int Height => Bottom - Top;
    public bool Intersects(PixelRect other) => Left < other.Right && Right > other.Left && Top < other.Bottom && Bottom > other.Top;
}

public static class OverlayPosition
{
    // Anchor immediately before the notification area. Windows 11 can center task buttons.
    public static PixelRect Calculate(PixelRect monitor, PixelRect taskbar, int width, int height, int horizontalOffset, int verticalOffset, PixelRect? tray)
    {
        int reservedLeft = tray?.Left ?? taskbar.Right - Math.Min(240, taskbar.Width / 4);
        int rightmostX = reservedLeft - width - 8;
        int x = rightmostX + horizontalOffset;
        int y = taskbar.Top + (taskbar.Height - height) / 2 + verticalOffset;
        x = Math.Clamp(x, Math.Max(monitor.Left, taskbar.Left + 8), Math.Max(Math.Max(monitor.Left, taskbar.Left + 8), rightmostX));
        y = Math.Clamp(y, monitor.Top, Math.Max(monitor.Top, monitor.Bottom - height));
        return new PixelRect(x, y, x + width, y + height);
    }
}
