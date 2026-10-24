namespace WndoLand
{
    public partial class HotkeyList : Form
    {
        private const int BuiltIn = -1;

        private List<Keybinds> _user = new();
        private List<Keybinds> _defaults = new();

        public bool Changed { get; private set; }

        public HotkeyList()
        {
            InitializeComponent();
            Theme.Apply(this);
            Reload();
        }

        private void Reload()
        {
            listMsg.Text = "";
            hotkeyGrid.Rows.Clear();
            try
            {
                _defaults = KeybindStore.LoadEffectiveDefaults();
                var overridden = KeybindStore.LoadOverrides().Select(o => o.Name).ToHashSet();
                foreach (var kb in _defaults)
                    AddRow(kb, overridden.Contains(kb.Name ?? "") ? "Built-in (changed)" : "Built-in", BuiltIn, kb.Name);

                _user = KeybindStore.LoadUser();
                for (int i = 0; i < _user.Count; i++)
                    AddRow(_user[i], "Yours", i, null);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Couldn't load hotkeys for the list");
                listMsg.Text = $"Couldn't load hotkeys: {ex.Message}";
            }
            UpdateButtons();
        }

        private sealed record RowRef(int UserIndex, string? BuiltInName);

        private void AddRow(Keybinds kb, string source, int userIndex, string? builtInName)
        {
            int row = hotkeyGrid.Rows.Add(kb.Name, kb.HotkeyText, kb.Summary, source);
            hotkeyGrid.Rows[row].Tag = new RowRef(userIndex, builtInName);
        }

        private RowRef? Selected() =>
            hotkeyGrid.SelectedRows.Count == 1 ? hotkeyGrid.SelectedRows[0].Tag as RowRef : null;

        private int SelectedUserIndex() => Selected()?.UserIndex ?? BuiltIn;

        private string? SelectedBuiltIn() => Selected()?.BuiltInName;

        private void UpdateButtons()
        {
            var sel = Selected();
            editButton.Enabled = sel != null;

            // Built-in shortcuts can't be deleted, only put back to what shipped
            bool builtIn = sel?.BuiltInName != null;
            deleteButton.Text = builtIn ? "Reset" : "Delete";
            deleteButton.Enabled = sel != null && (!builtIn || KeybindStore.IsOverridden(sel.BuiltInName!));
        }

        private void hotkeyGrid_SelectionChanged(object sender, EventArgs e) => UpdateButtons();

        private void hotkeyGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && Selected() != null) editButton_Click(sender, e);
        }

        private void editButton_Click(object sender, EventArgs e)
        {
            if (SelectedBuiltIn() is { } builtInName)
            {
                var builtIn = _defaults.FirstOrDefault(d => d.Name == builtInName);
                if (builtIn == null) return;

                using var editor = new EditHotkeyForm(builtIn);
                if (editor.ShowDialog(this) == DialogResult.OK)
                {
                    Changed = true;
                    Reload();
                }
                return;
            }

            int index = SelectedUserIndex();
            if (index == BuiltIn || index >= _user.Count) return;

            var kb = _user[index];
            using Form form = kb.Target == Target.Cmd ? new AddCmdHK(kb, index) : new AddProcHK(kb, index);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                Changed = true;
                Reload();
            }
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            if (SelectedBuiltIn() is { } builtInName)
            {
                try
                {
                    KeybindStore.ClearOverride(builtInName);
                    Log.Info($"Reset built-in hotkey '{builtInName}' to its default");
                    Changed = true;
                    Reload();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Couldn't reset hotkey '{builtInName}'");
                    listMsg.Text = $"Couldn't reset: {ex.Message}";
                }
                return;
            }

            int index = SelectedUserIndex();
            if (index == BuiltIn || index >= _user.Count) return;

            var kb = _user[index];
            if (MessageBox.Show(this, $"Delete the hot key \"{kb.Name}\" ({kb.HotkeyText})?", "WndoLand",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                KeybindStore.RemoveUser(index);
                Log.Info($"Deleted user keybind '{kb.Name}' ({kb.HotkeyText})");
                Changed = true;
                Reload();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Couldn't delete keybind '{kb.Name}'");
                listMsg.Text = $"Couldn't delete: {ex.Message}";
            }
        }
    }
}
