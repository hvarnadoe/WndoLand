using System.Runtime.InteropServices;
using static WndoLand.Win32;

namespace WndoLand
{
    internal class ActiveBorder : Form
    {
        private const int PollMs = 50;

        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOPMOST = 0x00000008;

        private readonly Func<nint, bool> _isTracked;
        private readonly System.Windows.Forms.Timer _timer = new() { Interval = PollMs };
        private Rectangle _lastBounds;
        private int _lastThickness;

        public ActiveBorder(Func<nint, bool> isTracked)
        {
            _isTracked = isTracked;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;

            _timer.Tick += (_, _) => Update_();
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TRANSPARENT | WS_EX_TOPMOST;
                return cp;
            }
        }

        public void Start()
        {
            CreateControl();
            _ = Handle;
            _timer.Start();
        }

        private void Update_()
        {
            Watchdog.Beat();
            nint fg = GetForegroundWindow();
            nint target = _isTracked(fg) ? fg : GetAncestor(fg, GA_ROOTOWNER);

            // Closed UWP/WinUI apps are cloaked rather than destroyed, so IsWindowVisible alone still says "visible"
            if (target == IntPtr.Zero || !_isTracked(target) || IsIconic(target) || !IsWindowVisible(target) || IsCloaked(target)
                || DwmGetWindowAttribute(target, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT r, Marshal.SizeOf<RECT>()) != 0)
            {
                if (Visible) Hide();
                _lastBounds = Rectangle.Empty;
                return;
            }

            var settings = Settings.Current;
            int thickness = Math.Clamp(settings.BorderThickness, 1, 20);
            if (BackColor != settings.BorderColorValue) BackColor = settings.BorderColorValue;

            var bounds = Rectangle.FromLTRB(r.left, r.top, r.right, r.bottom);
            if (bounds != _lastBounds || thickness != _lastThickness)
            {
                _lastBounds = bounds;
                _lastThickness = thickness;
                Bounds = bounds;
                var region = new Region(new Rectangle(Point.Empty, bounds.Size));
                region.Exclude(new Rectangle(thickness, thickness, bounds.Width - 2 * thickness, bounds.Height - 2 * thickness));
                Region?.Dispose();
                Region = region;
            }
            if (!Visible) Show();
        }

        private static bool IsCloaked(nint hwnd) =>
            DwmGetWindowAttribute(hwnd, DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0 && cloaked != 0;

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer.Dispose();
            base.Dispose(disposing);
        }
    }
}
