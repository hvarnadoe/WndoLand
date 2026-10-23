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
    public partial class AddProcHK : Form
    {
        private readonly int _editIndex = -1;

        public AddProcHK()
        {
            InitializeComponent();
            Theme.Apply(this);
        }

        internal AddProcHK(Keybinds existing, int userIndex) : this()
        {
            _editIndex = userIndex;
            Text = "Edit app hot key";
            selectedProgram.Text = existing.ProcessPath ?? "";

            _modifierList.AddRange(KeyNames.ModifierKeys(existing.Modifiers));
            _keyList.Add(existing.Key);
            modifierKeys.Text = _modifierList.Count == 0 ? "None" : string.Join(" + ", _modifierList.Select(DisplayName));
            label2.Text = DisplayName(existing.Key);
        }

        private void fileselect_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = @"C:\Program Files";
                openFileDialog.Title = "Select a Text File";
                openFileDialog.Filter = "Text files (*.exe)|*.exe|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedProgram.Text = openFileDialog.FileName;
                }
            }
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

        // ProcessCmdKey sees keys before the focused button does, so Space/Enter/Tab/arrows
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

            if (string.IsNullOrEmpty(selectedProgram.Text))
            {
                errMsg.Text = "Select a program first.";
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
                Name = $"Launch {Path.GetFileNameWithoutExtension(selectedProgram.Text)}",
                Modifiers = modifiers,
                Key = _keyList[0],
                Target = WndoLand.Target.Process,
                Action = Actions.LaunchProcess,
                ProcessPath = selectedProgram.Text
            };

            try
            {
                if (_editIndex >= 0) KeybindStore.ReplaceUser(_editIndex, keybind);
                else KeybindStore.AddUser(keybind);
                Log.Info($"Saved new keybind '{keybind.Name}' mods=0x{modifiers:X} key={keybind.Key} path='{keybind.ProcessPath}'");
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
