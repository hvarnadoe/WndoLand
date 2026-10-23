namespace WndoLand
{
    internal class WorkspaceOverlay : Form
    {
        private const int Size_ = 96;
        private const int Margin_ = 24;
        private const int ShowMs = 500;

        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOPMOST = 0x00000008;

        private readonly System.Windows.Forms.Timer _timer = new() { Interval = ShowMs };
        private int _number;

        public WorkspaceOverlay()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = Color.FromArgb(32, 32, 32);
            Opacity = 0.9;
            Size = new Size(Size_, Size_);
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

        public void Flash(int workspaceNum)
        {
            _number = workspaceNum;

            var area = Screen.FromPoint(Cursor.Position).Bounds;
            Location = new Point(area.Right - Size_ - Margin_, area.Top + Margin_);

            Invalidate();
            Show();
            _timer.Stop();
            _timer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var font = new Font("Segoe UI", 44, FontStyle.Bold, GraphicsUnit.Point);
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
