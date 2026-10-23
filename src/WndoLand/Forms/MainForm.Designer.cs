namespace WndoLand
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            addProcButton = new Button();
            addCmdButton = new Button();
            hotkeysButton = new Button();
            settingsButton = new Button();
            hintLabel = new Label();
            SuspendLayout();
            // 
            // addProcButton
            // 
            addProcButton.Location = new Point(27, 12);
            addProcButton.Name = "addProcButton";
            addProcButton.Padding = new Padding(14, 0, 0, 0);
            addProcButton.Size = new Size(364, 44);
            addProcButton.TabIndex = 2;
            addProcButton.Text = "Add an app shortcut";
            addProcButton.TextAlign = ContentAlignment.MiddleLeft;
            addProcButton.UseVisualStyleBackColor = true;
            addProcButton.Click += addProcButton_Click;
            // 
            // addCmdButton
            // 
            addCmdButton.Location = new Point(27, 64);
            addCmdButton.Name = "addCmdButton";
            addCmdButton.Padding = new Padding(14, 0, 0, 0);
            addCmdButton.Size = new Size(364, 44);
            addCmdButton.TabIndex = 3;
            addCmdButton.Text = "Add a command shortcut";
            addCmdButton.TextAlign = ContentAlignment.MiddleLeft;
            addCmdButton.UseVisualStyleBackColor = true;
            addCmdButton.Click += addCmdButton_Click;
            // 
            // hotkeysButton
            // 
            hotkeysButton.Location = new Point(27, 116);
            hotkeysButton.Name = "hotkeysButton";
            hotkeysButton.Padding = new Padding(14, 0, 0, 0);
            hotkeysButton.Size = new Size(364, 44);
            hotkeysButton.TabIndex = 4;
            hotkeysButton.Text = "View or Manage hot keys";
            hotkeysButton.TextAlign = ContentAlignment.MiddleLeft;
            hotkeysButton.UseVisualStyleBackColor = true;
            hotkeysButton.Click += hotkeysButton_Click;
            // 
            // settingsButton
            // 
            settingsButton.Location = new Point(27, 168);
            settingsButton.Name = "settingsButton";
            settingsButton.Padding = new Padding(14, 0, 0, 0);
            settingsButton.Size = new Size(364, 44);
            settingsButton.TabIndex = 5;
            settingsButton.Text = "Settings";
            settingsButton.TextAlign = ContentAlignment.MiddleLeft;
            settingsButton.UseVisualStyleBackColor = true;
            settingsButton.Click += settingsButton_Click;
            // 
            // hintLabel
            // 
            hintLabel.AutoSize = true;
            hintLabel.Location = new Point(26, 232);
            hintLabel.Name = "hintLabel";
            hintLabel.Size = new Size(312, 15);
            hintLabel.TabIndex = 6;
            hintLabel.Tag = "muted";
            hintLabel.Text = "Closing this window keeps WndoLand running in the tray.";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(420, 267);
            Controls.Add(hintLabel);
            Controls.Add(settingsButton);
            Controls.Add(hotkeysButton);
            Controls.Add(addCmdButton);
            Controls.Add(addProcButton);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "WndoLand";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Button addProcButton;
        private Button addCmdButton;
        private Button hotkeysButton;
        private Button settingsButton;
        private Label hintLabel;
    }
}
