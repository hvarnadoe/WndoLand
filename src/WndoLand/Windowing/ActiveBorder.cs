using static WndoLand.Win32;

namespace WndoLand
{
    /// <summary>
    /// Colours the focused window's own frame. Windows (DWM) draws it as part of the window, so it follows the window
    /// exactly with no separate overlay. Needs Windows 11, and Windows only draws that frame on a stripped window once
    /// it has rounded corners, so the focused window also gets small rounded corners.
    /// </summary>
    internal sealed class ActiveBorder : IDisposable
    {
        private const int PollMs = 50;

        private readonly Func<nint, bool> _isTracked;
        private readonly System.Windows.Forms.Timer _timer = new() { Interval = PollMs };
        private nint _highlighted;
        private Color _color;
        private bool _reportedFailure;

        public ActiveBorder(Func<nint, bool> isTracked)
        {
            _isTracked = isTracked;
            _timer.Tick += (_, _) => Update_();
        }

        public void Start() => _timer.Start();

        private void Update_()
        {
            Watchdog.Beat();
            nint fg = GetForegroundWindow();
            nint target = _isTracked(fg) ? fg : GetAncestor(fg, GA_ROOTOWNER);
            if (!_isTracked(target)) target = 0;

            var color = Settings.Current.BorderColorValue;
            if (target == _highlighted && color == _color) return;

            Clear();
            _color = color;
            if (target != 0) Highlight(target);
        }

        private void Highlight(nint hwnd)
        {
            int corner = DWMWCP_ROUNDSMALL, color = ColorTranslator.ToWin32(_color);
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
            int hr = DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref color, sizeof(int));
            if (hr != 0 && !_reportedFailure)
            {
                _reportedFailure = true;
                Log.Warn($"Windows refused a border colour for hwnd=0x{hwnd:X} (HRESULT 0x{hr:X8}); coloured window borders need Windows 11");
            }
            _highlighted = hwnd;
        }

        // Hands the frame back to Windows: square corners and no border for a stripped window, the usual look otherwise
        private void Clear()
        {
            if (_highlighted == 0) return;
            if (IsWindow(_highlighted))
            {
                int corner = DWMWCP_DEFAULT, color = DWMWA_COLOR_DEFAULT;
                DwmSetWindowAttribute(_highlighted, DWMWA_BORDER_COLOR, ref color, sizeof(int));
                DwmSetWindowAttribute(_highlighted, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
            }
            _highlighted = 0;
        }

        public void Dispose()
        {
            _timer.Dispose();
            Clear();
        }
    }
}
