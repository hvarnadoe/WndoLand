using System.Runtime.InteropServices;
using static WndoLand.Win32;

namespace WndoLand
{
    internal static class Theme
    {
        public static readonly Color Background = Color.FromArgb(0x16, 0x17, 0x1A);
        public static readonly Color Surface = Color.FromArgb(0x1F, 0x20, 0x24);
        public static readonly Color Raised = Color.FromArgb(0x2A, 0x2B, 0x31);
        public static readonly Color Hover = Color.FromArgb(0x36, 0x37, 0x3E);
        public static readonly Color Border = Color.FromArgb(0x3A, 0x3B, 0x42);
        public static readonly Color Text = Color.FromArgb(0xE8, 0xE8, 0xEC);
        public static readonly Color Muted = Color.FromArgb(0x9A, 0x9B, 0xA4);
        public static readonly Color Error = Color.FromArgb(0xFF, 0x6B, 0x6B);

        private static Color Accent => Settings.Current.BorderColorValue;

        public static void Apply(Form form)
        {
            form.Icon = AppIcon.Window;
            form.BackColor = Background;
            form.ForeColor = Text;

            StyleChildren(form);

            if (form.IsHandleCreated) DarkTitleBar(form.Handle);
            form.HandleCreated += (_, _) => DarkTitleBar(form.Handle);
        }

        private static void DarkTitleBar(nint hwnd)
        {
            int on = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref on, sizeof(int));
        }

        private static void StyleChildren(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                switch (c)
                {
                    case Button b: StyleButton(b); break;
                    case TextBox t: t.BackColor = Raised; t.ForeColor = Text; t.BorderStyle = BorderStyle.FixedSingle; break;
                    case NumericUpDown n: n.BackColor = Raised; n.ForeColor = Text; n.BorderStyle = BorderStyle.FixedSingle; break;
                    case CheckBox cb: StyleCheckBox(cb); break;
                    case Label l: StyleLabel(l); break;
                    case DataGridView g: StyleGrid(g); break;
                    default: c.BackColor = Background; c.ForeColor = Text; break;
                }
                StyleChildren(c);
            }
        }

        private static void StyleCheckBox(CheckBox cb)
        {
            cb.FlatStyle = FlatStyle.Flat;
            cb.ForeColor = Text;
            cb.BackColor = Color.Transparent;
            cb.Cursor = Cursors.Hand;
            cb.Paint += (_, e) =>
            {
                // Redraw the box with a black check; the flat style tints it with the (light) text color
                // and paints its own box underneath, so wipe the whole control and draw box + text ourselves
                const int size = 13;
                var box = new Rectangle(1, (cb.Height - size) / 2, size, size);
                var g = e.Graphics;
                g.Clear(cb.Parent?.BackColor ?? Background);
                TextRenderer.DrawText(g, cb.Text, cb.Font,
                    new Rectangle(size + 6, 0, cb.Width - size - 6, cb.Height), cb.ForeColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);
                using (var brush = new SolidBrush(Color.FromArgb(0xF2, 0xF2, 0xF4))) g.FillRectangle(brush, box);
                using (var pen = new Pen(Border)) g.DrawRectangle(pen, box.X, box.Y, box.Width - 1, box.Height - 1);

                if (cb.Checked)
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using var pen = new Pen(Color.Black, 2f);
                    g.DrawLines(pen, new[]
                    {
                        new Point(box.X + 3, box.Y + 7),
                        new Point(box.X + 5, box.Y + 9),
                        new Point(box.X + 10, box.Y + 4)
                    });
                }
            };
        }

        private static void StyleLabel(Label l)
        {
            l.BackColor = Color.Transparent;
            l.ForeColor = l.ForeColor == Color.Firebrick ? Error
                        : l.Tag as string == "muted" ? Muted
                        : Text;
        }

        private static void StyleButton(Button b)
        {
            bool primary = b.Tag as string == "primary";
            bool swatch = b.Tag as string == "swatch";

            b.FlatStyle = FlatStyle.Flat;
            b.UseVisualStyleBackColor = false;
            b.FlatAppearance.BorderSize = 0;
            b.Cursor = Cursors.Hand;
            typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(b, true);

            Color accent = Accent;
            Color back = primary ? accent : Raised;
            Color hover = primary ? ControlPaint.Light(accent, 0.3f) : Hover;
            Color pressed = primary ? ControlPaint.Dark(accent, 0.1f) : Border;
            Color fore = primary ? (accent.GetBrightness() > 0.55f ? Color.FromArgb(0x0B, 0x0C, 0x0E) : Color.White) : Text;

            bool hot = false, down = false;
            b.MouseEnter += (_, _) => { hot = true; b.Invalidate(); };
            b.MouseLeave += (_, _) => { hot = false; down = false; b.Invalidate(); };
            b.MouseDown += (_, _) => { down = true; b.Invalidate(); };
            b.MouseUp += (_, _) => { down = false; b.Invalidate(); };
            b.EnabledChanged += (_, _) => { b.Cursor = b.Enabled ? Cursors.Hand : Cursors.Default; b.Invalidate(); };
            b.GotFocus += (_, _) => b.Invalidate();
            b.LostFocus += (_, _) => b.Invalidate();

            b.Paint += (_, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(b.Parent?.BackColor ?? Background);

                Color fill = swatch ? b.BackColor : !b.Enabled ? Surface : down ? pressed : hot ? hover : back;
                Color edge = primary && b.Enabled ? accent : Border;
                Color textColor = !b.Enabled ? Muted : swatch ? (b.BackColor.GetBrightness() > 0.55f ? Color.Black : Color.White) : fore;

                var rect = new Rectangle(0, 0, b.Width - 1, b.Height - 1);
                int radius = Math.Min(ButtonRadius, b.Height / 3);
                using var path = RoundedRect(rect, radius);
                using (var brush = new SolidBrush(fill)) g.FillPath(brush, path);
                using (var pen = new Pen(edge)) g.DrawPath(pen, path);

                if (b.Focused)
                {
                    using var focusPath = RoundedRect(Rectangle.Inflate(rect, -3, -3), Math.Max(2, radius - 3));
                    using var focusPen = new Pen(Color.FromArgb(120, accent));
                    g.DrawPath(focusPen, focusPath);
                }

                var textArea = new Rectangle(b.Padding.Left + 4, 0, b.Width - b.Padding.Horizontal - 8, b.Height);
                TextRenderer.DrawText(g, b.Text, b.Font, textArea, textColor, TextFlagsFor(b.TextAlign));
            };
        }

        private const int ButtonRadius = 10;

        private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static TextFormatFlags TextFlagsFor(ContentAlignment a)
        {
            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis;
            return flags | (a is ContentAlignment.TopLeft or ContentAlignment.MiddleLeft or ContentAlignment.BottomLeft ? TextFormatFlags.Left
                          : a is ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight ? TextFormatFlags.Right
                          : TextFormatFlags.HorizontalCenter);
        }

        private static void StyleGrid(DataGridView g)
        {
            g.EnableHeadersVisualStyles = false;
            g.BorderStyle = BorderStyle.None;
            g.BackgroundColor = Surface;
            g.GridColor = Border;
            g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.ColumnHeadersHeight = 34;
            g.RowTemplate.Height = 30;
            g.RowHeadersVisible = false;

            Color selection = Color.FromArgb(0x2B, Accent.R * 2 / 5 + 0x10, Accent.G * 2 / 5 + 0x10, Accent.B * 2 / 5 + 0x10);

            g.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Raised,
                ForeColor = Muted,
                SelectionBackColor = Raised,
                SelectionForeColor = Muted,
                Padding = new Padding(8, 0, 0, 0),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };
            g.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Surface,
                ForeColor = Text,
                SelectionBackColor = Color.FromArgb(Math.Min(Accent.R / 3 + 0x14, 255), Math.Min(Accent.G / 3 + 0x14, 255), Math.Min(Accent.B / 3 + 0x14, 255)),
                SelectionForeColor = Text,
                Padding = new Padding(8, 0, 0, 0)
            };
            g.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(0x24, 0x25, 0x2A) };
        }
    }
}
