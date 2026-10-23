namespace WndoLand
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            colorLabel = new Label();
            borderColorButton = new Button();
            thicknessLabel = new Label();
            borderThickness = new NumericUpDown();
            gapLabel = new Label();
            gapSize = new NumericUpDown();
            stripTitleBars = new CheckBox();
            startWithWindows = new CheckBox();
            editRulesButton = new Button();
            saveButton = new Button();
            cancelButton = new Button();
            settingsMsg = new Label();
            colorDialog = new ColorDialog();
            ((System.ComponentModel.ISupportInitialize)borderThickness).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gapSize).BeginInit();
            SuspendLayout();
            //
            // colorLabel
            //
            colorLabel.AutoSize = true;
            colorLabel.Location = new Point(12, 19);
            colorLabel.Name = "colorLabel";
            colorLabel.Size = new Size(131, 15);
            colorLabel.TabIndex = 0;
            colorLabel.Text = "Active window border:";
            //
            // borderColorButton
            //
            borderColorButton.Location = new Point(232, 12);
            borderColorButton.Name = "borderColorButton";
            borderColorButton.Size = new Size(100, 28);
            borderColorButton.TabIndex = 1;
            borderColorButton.Text = "Color...";
            borderColorButton.Tag = "swatch";
            borderColorButton.UseVisualStyleBackColor = false;
            borderColorButton.Click += borderColorButton_Click;
            //
            // thicknessLabel
            //
            thicknessLabel.AutoSize = true;
            thicknessLabel.Location = new Point(12, 54);
            thicknessLabel.Name = "thicknessLabel";
            thicknessLabel.Size = new Size(153, 15);
            thicknessLabel.TabIndex = 2;
            thicknessLabel.Text = "Border thickness (pixels):";
            //
            // borderThickness
            //
            borderThickness.Location = new Point(232, 52);
            borderThickness.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            borderThickness.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            borderThickness.Name = "borderThickness";
            borderThickness.Size = new Size(100, 23);
            borderThickness.TabIndex = 3;
            borderThickness.Value = new decimal(new int[] { 2, 0, 0, 0 });
            //
            // gapLabel
            //
            gapLabel.AutoSize = true;
            gapLabel.Location = new Point(12, 89);
            gapLabel.Name = "gapLabel";
            gapLabel.Size = new Size(200, 15);
            gapLabel.TabIndex = 4;
            gapLabel.Text = "Gap between windows (pixels):";
            //
            // gapSize
            //
            gapSize.Location = new Point(232, 87);
            gapSize.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            gapSize.Name = "gapSize";
            gapSize.Size = new Size(100, 23);
            gapSize.TabIndex = 5;
            //
            // stripTitleBars
            //
            stripTitleBars.AutoSize = true;
            stripTitleBars.Location = new Point(12, 125);
            stripTitleBars.Name = "stripTitleBars";
            stripTitleBars.Size = new Size(309, 19);
            stripTitleBars.TabIndex = 6;
            stripTitleBars.Text = "Remove title bars and borders from tiled windows";
            stripTitleBars.UseVisualStyleBackColor = true;
            //
            // startWithWindows
            //
            startWithWindows.AutoSize = true;
            startWithWindows.Location = new Point(12, 152);
            startWithWindows.Name = "startWithWindows";
            startWithWindows.Size = new Size(119, 19);
            startWithWindows.TabIndex = 7;
            startWithWindows.Text = "Start with Windows";
            startWithWindows.UseVisualStyleBackColor = true;
            //
            // editRulesButton
            //
            editRulesButton.Location = new Point(12, 187);
            editRulesButton.Name = "editRulesButton";
            editRulesButton.Size = new Size(320, 28);
            editRulesButton.TabIndex = 8;
            editRulesButton.Text = "Edit per-app rules (float / workspace)...";
            editRulesButton.UseVisualStyleBackColor = true;
            editRulesButton.Click += editRulesButton_Click;
            //
            // saveButton
            //
            saveButton.Location = new Point(176, 262);
            saveButton.Name = "saveButton";
            saveButton.Size = new Size(75, 23);
            saveButton.TabIndex = 9;
            saveButton.Text = "Save";
            saveButton.UseVisualStyleBackColor = true;
            saveButton.Tag = "primary";
            saveButton.Click += saveButton_Click;
            //
            // cancelButton
            //
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(257, 262);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(75, 23);
            cancelButton.TabIndex = 10;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            //
            // settingsMsg
            //
            settingsMsg.AutoSize = true;
            settingsMsg.ForeColor = Color.Firebrick;
            settingsMsg.Location = new Point(12, 230);
            settingsMsg.Name = "settingsMsg";
            settingsMsg.Size = new Size(0, 15);
            settingsMsg.TabIndex = 11;
            //
            // SettingsForm
            //
            AcceptButton = saveButton;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(344, 297);
            Controls.Add(settingsMsg);
            Controls.Add(cancelButton);
            Controls.Add(saveButton);
            Controls.Add(editRulesButton);
            Controls.Add(startWithWindows);
            Controls.Add(stripTitleBars);
            Controls.Add(gapSize);
            Controls.Add(gapLabel);
            Controls.Add(borderThickness);
            Controls.Add(thicknessLabel);
            Controls.Add(borderColorButton);
            Controls.Add(colorLabel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "WndoLand settings";
            ((System.ComponentModel.ISupportInitialize)borderThickness).EndInit();
            ((System.ComponentModel.ISupportInitialize)gapSize).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label colorLabel;
        private Button borderColorButton;
        private Label thicknessLabel;
        private NumericUpDown borderThickness;
        private Label gapLabel;
        private NumericUpDown gapSize;
        private CheckBox stripTitleBars;
        private CheckBox startWithWindows;
        private Button editRulesButton;
        private Button saveButton;
        private Button cancelButton;
        private Label settingsMsg;
        private ColorDialog colorDialog;
    }
}
