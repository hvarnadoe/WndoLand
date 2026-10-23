using System.Diagnostics;

namespace WndoLand
{
    public partial class SettingsForm : Form
    {
        private Color _color;

        public SettingsForm()
        {
            InitializeComponent();
            Theme.Apply(this);

            var s = Settings.Current;
            SetColor(s.BorderColorValue);
            borderThickness.Value = Math.Clamp(s.BorderThickness, (int)borderThickness.Minimum, (int)borderThickness.Maximum);
            gapSize.Value = Math.Clamp(s.Gap, (int)gapSize.Minimum, (int)gapSize.Maximum);
            stripTitleBars.Checked = s.StripTitleBars;
            startWithWindows.Checked = StartupRegistration.IsEnabled;
            autoUpdate.Checked = s.AutoUpdate;
        }

        private void SetColor(Color color)
        {
            _color = color;
            borderColorButton.BackColor = color;
            borderColorButton.ForeColor = color.GetBrightness() > 0.5f ? Color.Black : Color.White;
        }

        private void borderColorButton_Click(object sender, EventArgs e)
        {
            colorDialog.Color = _color;
            if (colorDialog.ShowDialog(this) == DialogResult.OK)
                SetColor(colorDialog.Color);
        }

        private void editRulesButton_Click(object sender, EventArgs e)
        {
            try
            {
                Rules.EnsureFile();
                Process.Start(new ProcessStartInfo("notepad.exe", $"\"{AppPaths.RulesFile}\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Couldn't open the rules file");
                settingsMsg.Text = $"Couldn't open rules: {ex.Message}";
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            try
            {
                new Settings
                {
                    BorderColor = $"#{_color.R:X2}{_color.G:X2}{_color.B:X2}",
                    BorderThickness = (int)borderThickness.Value,
                    Gap = (int)gapSize.Value,
                    StripTitleBars = stripTitleBars.Checked,
                    AutoUpdate = autoUpdate.Checked
                }.Save();

                if (startWithWindows.Checked != StartupRegistration.IsEnabled)
                    StartupRegistration.Set(startWithWindows.Checked);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Couldn't save settings");
                settingsMsg.Text = $"Couldn't save: {ex.Message}";
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
