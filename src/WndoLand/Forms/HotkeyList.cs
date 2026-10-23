namespace WndoLand
{
    public partial class HotkeyList : Form
    {
        private const int BuiltIn = -1;

        private List<Keybinds> _user = new();

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
                foreach (var kb in KeybindStore.LoadDefault())
                    AddRow(kb, "Built-in", BuiltIn);

                _user = KeybindStore.LoadUser();
                for (int i = 0; i < _user.Count; i++)
                    AddRow(_user[i], "Yours", i);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Couldn't load hotkeys for the list");
                listMsg.Text = $"Couldn't load hotkeys: {ex.Message}";
            }
            UpdateButtons();
        }

        private void AddRow(Keybinds kb, string source, int userIndex)
        {
            int row = hotkeyGrid.Rows.Add(kb.Name, kb.HotkeyText, kb.Summary, source);
            hotkeyGrid.Rows[row].Tag = userIndex;
        }

        private int SelectedUserIndex() =>
            hotkeyGrid.SelectedRows.Count == 1 && hotkeyGrid.SelectedRows[0].Tag is int i ? i : BuiltIn;

        private void UpdateButtons()
        {
            bool editable = SelectedUserIndex() != BuiltIn;
            editButton.Enabled = editable;
            deleteButton.Enabled = editable;
        }

        private void hotkeyGrid_SelectionChanged(object sender, EventArgs e) => UpdateButtons();

        private void hotkeyGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && SelectedUserIndex() != BuiltIn) editButton_Click(sender, e);
        }

        private void editButton_Click(object sender, EventArgs e)
        {
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
