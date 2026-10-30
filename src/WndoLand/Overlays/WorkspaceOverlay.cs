namespace WndoLand
{
    internal class WorkspaceOverlay : Form
    {
        // Sizes at 100% scaling; Flash scales them to the DPI of the monitor the overlay appears on
        private const int Size_ = 96;
        private const int Margin_ = 24;
        private const float FontPt = 44;
        private const int ShowMs = 500;

        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOPMOST = 0x00000008;

        private readonly System.Windows.Forms.Timer _timer = new() { Interval = ShowMs };
        private int _number;
        private int _dpi = 96;

        public WorkspaceOverlay()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = Color.FromArgb(32, 32, 32);
            Opacity = 0.9;
            DoubleBuffered = true;

            _timer.Tick += (_, _) => { _timer.Stop(); Hide(); };
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

        public void Flash(int workspaceNum, Monitors.Info monitor)
        {
            _number = workspaceNum;
            _dpi = monitor.Dpi;

            int size = Scaled(Size_), margin = Scaled(Margin_);
            var area = monitor.Bounds;
            // Create the window first so placing it is a plain move and resize, not something WinForms rescales on creation
            _ = Handle;
            Bounds = new Rectangle(area.Right - size - margin, area.Top + margin, size, size);

            Invalidate();
            Show();
            _timer.Stop();
            _timer.Start();
        }

        private int Scaled(int px) => (int)Math.Round(px * _dpi / 96.0);

        // Moving onto a monitor with a different DPI makes WinForms resize the form by the DPI ratio; Flash has already
        // sized it for that monitor, so skip that or it would be scaled twice
        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            e.Cancel = true;
            base.OnDpiChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Sized in pixels for this monitor's DPI; a point-sized font would use the system DPI instead
            using var font = new Font("Segoe UI", FontPt * _dpi / 72f, FontStyle.Bold, GraphicsUnit.Pixel);
            TextRenderer.DrawText(e.Graphics, _number.ToString(), font, ClientRectangle, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer.Dispose();
            base.Dispose(disposing);
        }
    }
}
