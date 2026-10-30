using System.Runtime.InteropServices;
using static WndoLand.Win32;

namespace WndoLand
{
    /// <summary>
    /// Monitor geometry read straight from Windows on every call, in physical pixels. Unlike Screen.AllScreens, which
    /// caches whatever it saw first, this can never hand back DPI-scaled sizes captured before DPI awareness was on.
    /// </summary>
    internal static class Monitors
    {
        public sealed record Info(string DeviceName, Rectangle Bounds, Rectangle WorkArea, int Dpi, bool Primary)
        {
            public int ScalePercent => Dpi * 100 / 96;
        }

        public static List<Info> All()
        {
            var all = new List<Info>();
            EnumDisplayMonitors(0, 0, (hMonitor, _, _, _) => { all.Add(Read(hMonitor)); return true; }, 0);
            return all;
        }

        public static Info? Find(string deviceName) => All().FirstOrDefault(m => m.DeviceName == deviceName);

        // The primary monitor is the one whose top-left corner is the desktop origin
        public static Info Primary => Read(MonitorFromPoint(Point.Empty, MONITOR_DEFAULTTOPRIMARY));

        public static Info FromWindow(nint hwnd) => Read(MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST));

        private static Info Read(nint hMonitor)
        {
            var mi = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
            GetMonitorInfo(hMonitor, ref mi);
            int dpi = GetDpiForMonitor(hMonitor, MDT_EFFECTIVE_DPI, out uint dpiX, out _) == 0 ? (int)dpiX : 96;
            return new Info(mi.szDevice ?? "", ToRectangle(mi.rcMonitor), ToRectangle(mi.rcWork), dpi,
                            (mi.dwFlags & MONITORINFOF_PRIMARY) != 0);
        }

        private static Rectangle ToRectangle(RECT r) => Rectangle.FromLTRB(r.left, r.top, r.right, r.bottom);
    }
}
