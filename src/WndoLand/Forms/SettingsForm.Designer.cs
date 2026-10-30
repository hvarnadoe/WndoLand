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
            gapLabel = new Label();
            gapSize = new NumericUpDown();
            stripTitleBars = new CheckBox();
            startWithWindows = new CheckBox();
            runElevated = new CheckBox();
            exitButton = new Button();
            autoUpdate = new CheckBox();
            editRulesButton = new Button();
            saveButton = new Button();
            cancelButton = new Button();
            settingsMsg = new Label();
            versionLabel = new Label();
            checkUpdatesButton = new Button();
            updateMsg = new Label();
            colorDialog = new ColorDialog();
            ((System.ComponentModel.ISupportInitialize)gapSize).BeginInit();
            SuspendLayout();
            // 
            // colorLabel
            // 
            colorLabel.AutoSize = true;
            colorLabel.Location = new Point(12, 19);
            colorLabel.Name = "colorLabel";
            colorLabel.Size = new Size(126, 15);
            colorLabel.TabIndex = 0;
            colorLabel.Text = "Active window border:";
            // 
            // borderColorButton
            // 
            borderColorButton.Location = new Point(232, 12);
            borderColorButton.Name = "borderColorButton";
            borderColorButton.Size = new Size(100, 28);
            borderColorButton.TabIndex = 1;
            borderColorButton.Tag = "swatch";
            borderColorButton.Text = "Color...";
            borderColorButton.UseVisualStyleBackColor = false;
            borderColorButton.Click += borderColorButton_Click;
            // 
            // gapLabel
            // 
            gapLabel.AutoSize = true;
            gapLabel.Location = new Point(12, 54);
            gapLabel.Name = "gapLabel";
            gapLabel.Size = new Size(169, 15);
            gapLabel.TabIndex = 4;
            gapLabel.Text = "Gap between windows (pixels):";
            // 
            // gapSize
            // 
            gapSize.Location = new Point(232, 52);
            gapSize.Name = "gapSize";
            gapSize.Size = new Size(100, 23);
            gapSize.TabIndex = 5;
            // 
            // stripTitleBars
            // 
            stripTitleBars.AutoSize = true;
            stripTitleBars.Location = new Point(12, 90);
            stripTitleBars.Name = "stripTitleBars";
            stripTitleBars.Size = new Size(288, 19);
            stripTitleBars.TabIndex = 6;
            stripTitleBars.Text = "Remove title bars and borders from tiled windows";
            stripTitleBars.UseVisualStyleBackColor = true;
            // 
            // startWithWindows
            // 
            startWithWindows.AutoSize = true;
            startWithWindows.Location = new Point(12, 117);
            startWithWindows.Name = "startWithWindows";
            startWithWindows.Size = new Size(128, 19);
            startWithWindows.TabIndex = 7;
            startWithWindows.Text = "Start with Windows";
            startWithWindows.UseVisualStyleBackColor = true;
            // 
            // runElevated
            // 
            runElevated.AutoSize = true;
            runElevated.Location = new Point(12, 144);
            runElevated.Name = "runElevated";
            runElevated.Size = new Size(286, 19);
            runElevated.TabIndex = 16;
            runElevated.Text = "Run as administrator (manage elevated windows)";
            runElevated.UseVisualStyleBackColor = true;
            // 
            // exitButton
            // 
            exitButton.Location = new Point(12, 274);
            exitButton.Name = "exitButton";
            exitButton.Size = new Size(320, 28);
            exitButton.TabIndex = 17;
            exitButton.Text = "Exit WndoLand and restore windows";
            exitButton.UseVisualStyleBackColor = true;
            exitButton.Click += exitButton_Click;
            // 
            // autoUpdate
            // 
            autoUpdate.AutoSize = true;
            autoUpdate.Location = new Point(12, 171);
            autoUpdate.Name = "autoUpdate";
            autoUpdate.Size = new Size(177, 19);
            autoUpdate.TabIndex = 12;
            autoUpdate.Text = "Install updates automatically";
            autoUpdate.UseVisualStyleBackColor = true;
            // 
            // editRulesButton
            // 
            editRulesButton.Location = new Point(12, 206);
            editRulesButton.Name = "editRulesButton";
            editRulesButton.Size = new Size(320, 28);
            editRulesButton.TabIndex = 8;
            editRulesButton.Text = "Edit per-app rules (float / workspace)...";
            editRulesButton.UseVisualStyleBackColor = true;
            editRulesButton.Click += editRulesButton_Click;
            // 
            // saveButton
            // 
            saveButton.Location = new Point(176, 308);
            saveButton.Name = "saveButton";
            saveButton.Size = new Size(75, 23);
            saveButton.TabIndex = 9;
            saveButton.Tag = "primary";
            saveButton.Text = "Save";
            saveButton.UseVisualStyleBackColor = true;
            saveButton.Click += saveButton_Click;
            // 
            // cancelButton
            // 
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(257, 308);
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
            settingsMsg.Location = new Point(12, 296);
            settingsMsg.Name = "settingsMsg";
            settingsMsg.Size = new Size(0, 15);
            settingsMsg.TabIndex = 11;
            // 
            // versionLabel
            // 
            versionLabel.AutoSize = true;
            versionLabel.Location = new Point(12, 325);
            versionLabel.Name = "versionLabel";
            versionLabel.Size = new Size(0, 15);
            versionLabel.TabIndex = 13;
            versionLabel.Tag = "muted";
            // 
            // checkUpdatesButton
            // 
            checkUpdatesButton.Location = new Point(12, 240);
            checkUpdatesButton.Name = "checkUpdatesButton";
            checkUpdatesButton.Size = new Size(320, 28);
            checkUpdatesButton.TabIndex = 14;
            checkUpdatesButton.Text = "Check for updates";
            checkUpdatesButton.UseVisualStyleBackColor = true;
            checkUpdatesButton.Click += checkUpdatesButton_Click;
            // 
            // updateMsg
            // 
            updateMsg.AutoSize = true;
            updateMsg.Location = new Point(12, 276);
            updateMsg.Name = "updateMsg";
            updateMsg.Size = new Size(0, 15);
            updateMsg.TabIndex = 15;
            updateMsg.Tag = "muted";
            // 
            // SettingsForm
            // 
            AcceptButton = saveButton;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(344, 343);
            Controls.Add(exitButton);
            Controls.Add(versionLabel);
            Controls.Add(updateMsg);
            Controls.Add(checkUpdatesButton);
            Controls.Add(settingsMsg);
            Controls.Add(cancelButton);
            Controls.Add(saveButton);
            Controls.Add(editRulesButton);
            Controls.Add(autoUpdate);
            Controls.Add(runElevated);
            Controls.Add(startWithWindows);
            Controls.Add(stripTitleBars);
            Controls.Add(gapSize);
            Controls.Add(gapLabel);
            Controls.Add(borderColorButton);
            Controls.Add(colorLabel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "WndoLand settings";
            ((System.ComponentModel.ISupportInitialize)gapSize).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label colorLabel;
        private Button borderColorButton;
        private Label gapLabel;
        private NumericUpDown gapSize;
        private CheckBox stripTitleBars;
        private CheckBox startWithWindows;
        private CheckBox runElevated;
        private Button exitButton;
        private CheckBox autoUpdate;
        private Button editRulesButton;
        private Button saveButton;
        private Button cancelButton;
        private Label settingsMsg;
        private Label versionLabel;
        private Button checkUpdatesButton;
        private Label updateMsg;
        private ColorDialog colorDialog;
    }
}
