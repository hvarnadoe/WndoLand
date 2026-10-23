using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using static WndoLand.Win32;

namespace WndoLand
{
    public partial class AddCmdHK : Form
    {
        private readonly int _editIndex = -1;

        public AddCmdHK()
        {
            InitializeComponent();
            Theme.Apply(this);
        }

        internal AddCmdHK(Keybinds existing, int userIndex) : this()
        {
            _editIndex = userIndex;
            Text = "Edit command hot key";
            textBox1.Text = existing.Command ?? "";
            openWindow.Checked = existing.Action == Actions.RunCmdInWindow;

            _modifierList.AddRange(KeyNames.ModifierKeys(existing.Modifiers));
            _keyList.Add(existing.Key);
            modifierKeys.Text = _modifierList.Count == 0 ? "None" : string.Join(" + ", _modifierList.Select(DisplayName));
            label2.Text = DisplayName(existing.Key);
        }

        private enum CaptureMode { None, Modifiers, Keys }

        private CaptureMode _mode = CaptureMode.None;
        private readonly List<Keys> _modifierList = new();
        private readonly List<Keys> _keyList = new();

        private void addModifierKeys_Click(object sender, EventArgs e)
        {
            StartCapture(CaptureMode.Modifiers);
        }

        private void addOtherKeys_Click(object sender, EventArgs e)
        {
            StartCapture(CaptureMode.Keys);
        }

        private (Button button, Label label, List<Keys> list, string idleText) Target(CaptureMode mode) =>
            mode == CaptureMode.Modifiers
                ? (addModifierKeys, modifierKeys, _modifierList, "Click to set modifier key(s)")
                : (addOtherKeys, label2, _keyList, "Click to set key(s)");

        private void StartCapture(CaptureMode mode)
        {
            StopCapture();

            _mode = mode;
            var t = Target(mode);
            t.list.Clear();
            t.label.Text = "Press keys...";
            t.button.Text = "Press Esc when done";
        }

        private void StopCapture()
        {
            if (_mode == CaptureMode.None) return;

            var t = Target(_mode);
            t.button.Text = t.idleText;
            t.label.Text = t.list.Count == 0 ? "None" : string.Join(" + ", t.list.Select(DisplayName));
            _mode = CaptureMode.None;
        }

        // ProcessCmdKey sees keys before the focused control does, so Space/Enter/Tab/arrows
        // are captured as input instead of clicking or moving focus
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_mode == CaptureMode.None)
                return base.ProcessCmdKey(ref msg, keyData);

            Keys key = Normalize(keyData & Keys.KeyCode);

            if (key == Keys.Escape)
            {
                StopCapture();
                return true;
            }

            if (IsModifier(key) == (_mode == CaptureMode.Modifiers))
            {
                var t = Target(_mode);
                if (!t.list.Contains(key))
                {
                    t.list.Add(key);
                    t.label.Text = string.Join(" + ", t.list.Select(DisplayName));
                }
            }
            return true;
        }

        private static Keys Normalize(Keys key) => key switch
        {
            Keys.LControlKey or Keys.RControlKey => Keys.ControlKey,
            Keys.LShiftKey or Keys.RShiftKey => Keys.ShiftKey,
            Keys.LMenu or Keys.RMenu => Keys.Menu,
            Keys.RWin => Keys.LWin,
            _ => key
        };

        private static bool IsModifier(Keys key) =>
            key is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin;

        private static string DisplayName(Keys key) => key switch
        {
            Keys.ControlKey => "Ctrl",
            Keys.ShiftKey => "Shift",
            Keys.Menu => "Alt",
            Keys.LWin => "Win",
            _ => key.ToString()
        };

        private void done_Click(object sender, EventArgs e)
        {
            StopCapture();

            string command = textBox1.Text.Trim();
            if (command.Length == 0)
            {
                errMsg.Text = "Enter a command first.";
                return;
            }
            if (_keyList.Count != 1)
            {
                errMsg.Text = _keyList.Count == 0 ? "Set a key." : "Only one non-modifier key is allowed.";
                return;
            }

            uint modifiers = 0;
            foreach (var m in _modifierList)
            {
                modifiers |= m switch
                {
                    Keys.Menu => MOD_ALT,
                    Keys.ControlKey => MOD_CONTROL,
                    Keys.ShiftKey => MOD_SHIFT,
                    Keys.LWin => MOD_WIN,
                    _ => 0
                };
            }

            string? problem = HotkeyValidator.Check(modifiers, _keyList[0], _editIndex, Handle);
            if (problem != null)
            {
                errMsg.Text = problem;
                return;
            }

            var keybind = new Keybinds
            {
                Name = $"Run {command}",
                Modifiers = modifiers,
                Key = _keyList[0],
                Target = WndoLand.Target.Cmd,
                Action = openWindow.Checked ? Actions.RunCmdInWindow : Actions.RunCmdHidden,
                Command = command
            };

            try
            {
                if (_editIndex >= 0) KeybindStore.ReplaceUser(_editIndex, keybind);
                else KeybindStore.AddUser(keybind);
                Log.Info($"Saved new keybind '{keybind.Name}' mods=0x{modifiers:X} key={keybind.Key} action={keybind.Action} command='{command}'");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Couldn't save keybind to {AppPaths.UserKeybinds}");
                errMsg.Text = $"Couldn't save: {ex.Message}";
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
