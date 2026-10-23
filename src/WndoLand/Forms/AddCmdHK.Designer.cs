namespace WndoLand
{
    partial class AddCmdHK
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
            label2 = new Label();
            label3 = new Label();
            addOtherKeys = new Button();
            modifierKeys = new Label();
            label1 = new Label();
            addModifierKeys = new Button();
            textBox1 = new TextBox();
            done = new Button();
            cancelButton = new Button();
            label4 = new Label();
            openWindow = new CheckBox();
            errMsg = new Label();
            SuspendLayout();
            //
            // errMsg
            //
            errMsg.AutoSize = true;
            errMsg.ForeColor = Color.Firebrick;
            errMsg.Location = new Point(12, 230);
            errMsg.Name = "errMsg";
            errMsg.Size = new Size(0, 15);
            errMsg.TabIndex = 18;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(177, 196);
            label2.Name = "label2";
            label2.Size = new Size(36, 15);
            label2.TabIndex = 13;
            label2.Text = "None";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(155, 176);
            label3.Name = "label3";
            label3.Size = new Size(89, 15);
            label3.TabIndex = 12;
            label3.Text = "Selected Key(s):";
            // 
            // addOtherKeys
            // 
            addOtherKeys.Location = new Point(74, 162);
            addOtherKeys.Name = "addOtherKeys";
            addOtherKeys.Size = new Size(75, 62);
            addOtherKeys.TabIndex = 11;
            addOtherKeys.Text = "Click to set key(s)";
            addOtherKeys.UseVisualStyleBackColor = true;
            addOtherKeys.Click += addOtherKeys_Click;
            // 
            // modifierKeys
            // 
            modifierKeys.AutoSize = true;
            modifierKeys.Location = new Point(177, 128);
            modifierKeys.Name = "modifierKeys";
            modifierKeys.Size = new Size(36, 15);
            modifierKeys.TabIndex = 10;
            modifierKeys.Text = "None";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(155, 108);
            label1.Name = "label1";
            label1.Size = new Size(89, 15);
            label1.TabIndex = 9;
            label1.Text = "Selected Key(s):";
            // 
            // addModifierKeys
            // 
            addModifierKeys.Location = new Point(74, 94);
            addModifierKeys.Name = "addModifierKeys";
            addModifierKeys.Size = new Size(75, 62);
            addModifierKeys.TabIndex = 8;
            addModifierKeys.Text = "Click to set modifier key(s)";
            addModifierKeys.UseVisualStyleBackColor = true;
            addModifierKeys.Click += addModifierKeys_Click;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(12, 21);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(300, 23);
            textBox1.TabIndex = 14;
            // 
            // done
            // 
            done.Location = new Point(156, 226);
            done.Name = "done";
            done.Size = new Size(75, 23);
            done.TabIndex = 15;
            done.Text = "Save";
            done.UseVisualStyleBackColor = true;
            done.Tag = "primary";
            done.Click += done_Click;
            //
            // cancelButton
            //
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(237, 226);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(75, 23);
            cancelButton.TabIndex = 19;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(116, 3);
            label4.Name = "label4";
            label4.Size = new Size(97, 15);
            label4.TabIndex = 16;
            label4.Text = "Enter Command:";
            // 
            // openWindow
            // 
            openWindow.AutoSize = true;
            openWindow.Location = new Point(54, 69);
            openWindow.Name = "openWindow";
            openWindow.Size = new Size(216, 19);
            openWindow.TabIndex = 17;
            openWindow.Text = "Open window when command runs";
            openWindow.UseVisualStyleBackColor = true;
            // 
            // AddCmdHK
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AcceptButton = done;
            CancelButton = cancelButton;
            ClientSize = new Size(324, 261);
            Controls.Add(cancelButton);
            Controls.Add(errMsg);
            Controls.Add(openWindow);
            Controls.Add(label4);
            Controls.Add(done);
            Controls.Add(textBox1);
            Controls.Add(label2);
            Controls.Add(label3);
            Controls.Add(addOtherKeys);
            Controls.Add(modifierKeys);
            Controls.Add(label1);
            Controls.Add(addModifierKeys);
            Name = "AddCmdHK";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Add a command hot key";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label2;
        private Label label3;
        private Button addOtherKeys;
        private Label modifierKeys;
        private Label label1;
        private Button addModifierKeys;
        private TextBox textBox1;
        private Button done;
        private Button cancelButton;
        private Label label4;
        private CheckBox openWindow;
        private Label errMsg;
    }
}