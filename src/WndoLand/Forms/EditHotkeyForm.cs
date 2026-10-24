using static WndoLand.Win32;

namespace WndoLand
{
    /// <summary>Captures a replacement key combination for a built-in shortcut.</summary>
    internal class EditHotkeyForm : Form
    {
        private readonly string _builtInName;
        private readonly Label _combo = new();
        private readonly Label _msg = new();
        private uint _modifiers;
        private Keys _key;

        public EditHotkeyForm(Keybinds current)
        {
            _builtInName = current.Name ?? "";
            _modifiers = current.Modifiers;
            _key = current.Key;

            Text = "Edit hot key";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(344, 160);

            var title = new Label { Text = current.Name, AutoSize = true, Location = new Point(12, 12) };
            var hint = new Label { Text = "Press the new key combination.", AutoSize = true, Location = new Point(12, 36), Tag = "muted" };
            _combo.Location = new Point(12, 62);
            _combo.Size = new Size(320, 32);
            _combo.Font = new Font("Segoe UI", 14, FontStyle.Bold, GraphicsUnit.Point);
            _combo.TextAlign = ContentAlignment.MiddleLeft;
            _msg.Location = new Point(12, 98);
            _msg.AutoSize = true;
            _msg.ForeColor = Color.Firebrick;

            var ok = new Button { Text = "Save", Location = new Point(176, 124), Size = new Size(75, 23), Tag = "primary" };
            var cancel = new Button { Text = "Cancel", Location = new Point(257, 124), Size = new Size(75, 23), DialogResult = DialogResult.Cancel };
            ok.Click += (_, _) => Save();

            AcceptButton = ok;
            CancelButton = cancel;
            Controls.AddRange(new Control[] { title, hint, _combo, _msg, ok, cancel });

            Theme.Apply(this);
            ShowCombo();
        }

        private void ShowCombo() => _combo.Text = new Keybinds { Modifiers = _modifiers, Key = _key }.HotkeyText;

        // ProcessCmdKey sees keys before the focused button does, so Enter/Space/Tab/arrows can be captured as the combination
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            if (key is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin)
                return true;

            // A bare Escape or Enter keeps its normal job of cancelling or confirming the dialog
            if (keyData is Keys.Escape or Keys.Enter)
                return base.ProcessCmdKey(ref msg, keyData);

            uint modifiers = 0;
            if ((keyData & Keys.Alt) != 0) modifiers |= MOD_ALT;
            if ((keyData & Keys.Control) != 0) modifiers |= MOD_CONTROL;
            if ((keyData & Keys.Shift) != 0) modifiers |= MOD_SHIFT;
            if ((GetAsyncKeyState((int)Keys.LWin) & 0x8000) != 0 || (GetAsyncKeyState((int)Keys.RWin) & 0x8000) != 0)
                modifiers |= MOD_WIN;

            _modifiers = modifiers;
            _key = key;
            _msg.Text = "";
            ShowCombo();
            return true;
        }

        private void Save()
        {
            string? problem = HotkeyValidator.Check(_modifiers, _key, -1, Handle, _builtInName);
            if (problem != null)
            {
                _msg.Text = problem;
                return;
            }

            try
            {
                KeybindStore.SetOverride(_builtInName, _modifiers, _key);
                Log.Info($"Changed built-in hotkey '{_builtInName}' to mods=0x{_modifiers:X} key={_key}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Couldn't save the hotkey change to {AppPaths.KeybindOverrides}");
                _msg.Text = $"Couldn't save: {ex.Message}";
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
