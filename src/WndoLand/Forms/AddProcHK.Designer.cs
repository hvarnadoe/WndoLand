namespace WndoLand
{
    partial class AddProcHK
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
            done = new Button();
            openFileDialog1 = new OpenFileDialog();
            fileselect = new Button();
            addModifierKeys = new Button();
            label1 = new Label();
            modifierKeys = new Label();
            label2 = new Label();
            label3 = new Label();
            addOtherKeys = new Button();
            errMsg = new Label();
            selectedProgram = new Label();
            SuspendLayout();
            // 
            // done
            // 
            done.Location = new Point(233, 225);
            done.Name = "done";
            done.Size = new Size(75, 23);
            done.TabIndex = 0;
            done.Text = "Done";
            done.UseVisualStyleBackColor = true;
            done.Tag = "primary";
            done.Click += done_Click;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // fileselect
            // 
            fileselect.Location = new Point(95, 12);
            fileselect.Name = "fileselect";
            fileselect.Size = new Size(114, 23);
            fileselect.TabIndex = 1;
            fileselect.Text = "Select a Program";
            fileselect.UseVisualStyleBackColor = true;
            fileselect.Click += fileselect_Click;
            // 
            // addModifierKeys
            // 
            addModifierKeys.Location = new Point(52, 73);
            addModifierKeys.Name = "addModifierKeys";
            addModifierKeys.Size = new Size(75, 62);
            addModifierKeys.TabIndex = 2;
            addModifierKeys.Text = "Click to set modifier key(s)";
            addModifierKeys.UseVisualStyleBackColor = true;
            addModifierKeys.Click += addModifierKeys_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(133, 87);
            label1.Name = "label1";
            label1.Size = new Size(89, 15);
            label1.TabIndex = 3;
            label1.Text = "Selected Key(s):";
            // 
            // modifierKeys
            // 
            modifierKeys.AutoSize = true;
            modifierKeys.Location = new Point(155, 107);
            modifierKeys.Name = "modifierKeys";
            modifierKeys.Size = new Size(36, 15);
            modifierKeys.TabIndex = 4;
            modifierKeys.Text = "None";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(155, 175);
            label2.Name = "label2";
            label2.Size = new Size(36, 15);
            label2.TabIndex = 7;
            label2.Text = "None";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(133, 155);
            label3.Name = "label3";
            label3.Size = new Size(89, 15);
            label3.TabIndex = 6;
            label3.Text = "Selected Key(s):";
            // 
            // addOtherKeys
            // 
            addOtherKeys.Location = new Point(52, 141);
            addOtherKeys.Name = "addOtherKeys";
            addOtherKeys.Size = new Size(75, 62);
            addOtherKeys.TabIndex = 5;
            addOtherKeys.Text = "Click to set key(s)";
            addOtherKeys.UseVisualStyleBackColor = true;
            addOtherKeys.Click += addOtherKeys_Click;
            // 
            // errMsg
            // 
            errMsg.AutoSize = true;
            errMsg.Location = new Point(133, 249);
            errMsg.Name = "errMsg";
            errMsg.Size = new Size(0, 15);
            errMsg.TabIndex = 8;
            // 
            // selectedProgram
            // 
            selectedProgram.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            selectedProgram.AutoSize = true;
            selectedProgram.Location = new Point(50, 38);
            selectedProgram.Name = "selectedProgram";
            selectedProgram.Size = new Size(0, 15);
            selectedProgram.TabIndex = 9;
            // 
            // AddProcHK
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(324, 261);
            Controls.Add(selectedProgram);
            Controls.Add(errMsg);
            Controls.Add(label2);
            Controls.Add(label3);
            Controls.Add(addOtherKeys);
            Controls.Add(modifierKeys);
            Controls.Add(label1);
            Controls.Add(addModifierKeys);
            Controls.Add(fileselect);
            Controls.Add(done);
            Name = "AddProcHK";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Add a Program";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button done;
        private OpenFileDialog openFileDialog1;
        private Button fileselect;
        private Button addModifierKeys;
        private Label label1;
        private Label modifierKeys;
        private Label label2;
        private Label label3;
        private Button addOtherKeys;
        private Label errMsg;
        private Label selectedProgram;
    }
}