namespace WndoLand
{
    partial class HotkeyList
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
            hotkeyGrid = new DataGridView();
            nameColumn = new DataGridViewTextBoxColumn();
            hotkeyColumn = new DataGridViewTextBoxColumn();
            actionColumn = new DataGridViewTextBoxColumn();
            sourceColumn = new DataGridViewTextBoxColumn();
            editButton = new Button();
            deleteButton = new Button();
            closeButton = new Button();
            listMsg = new Label();
            ((System.ComponentModel.ISupportInitialize)hotkeyGrid).BeginInit();
            SuspendLayout();
            //
            // hotkeyGrid
            //
            hotkeyGrid.AllowUserToAddRows = false;
            hotkeyGrid.AllowUserToDeleteRows = false;
            hotkeyGrid.AllowUserToResizeRows = false;
            hotkeyGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            hotkeyGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            hotkeyGrid.Columns.AddRange(new DataGridViewColumn[] { nameColumn, hotkeyColumn, actionColumn, sourceColumn });
            hotkeyGrid.Location = new Point(12, 12);
            hotkeyGrid.MultiSelect = false;
            hotkeyGrid.Name = "hotkeyGrid";
            hotkeyGrid.ReadOnly = true;
            hotkeyGrid.RowHeadersVisible = false;
            hotkeyGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            hotkeyGrid.Size = new Size(676, 330);
            hotkeyGrid.TabIndex = 0;
            hotkeyGrid.SelectionChanged += hotkeyGrid_SelectionChanged;
            hotkeyGrid.CellDoubleClick += hotkeyGrid_CellDoubleClick;
            //
            // nameColumn
            //
            nameColumn.HeaderText = "Name";
            nameColumn.Name = "nameColumn";
            nameColumn.ReadOnly = true;
            nameColumn.Width = 190;
            //
            // hotkeyColumn
            //
            hotkeyColumn.HeaderText = "Hot key";
            hotkeyColumn.Name = "hotkeyColumn";
            hotkeyColumn.ReadOnly = true;
            hotkeyColumn.Width = 130;
            //
            // actionColumn
            //
            actionColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            actionColumn.HeaderText = "Action";
            actionColumn.Name = "actionColumn";
            actionColumn.ReadOnly = true;
            //
            // sourceColumn
            //
            sourceColumn.HeaderText = "Source";
            sourceColumn.Name = "sourceColumn";
            sourceColumn.ReadOnly = true;
            sourceColumn.Width = 70;
            //
            // editButton
            //
            editButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            editButton.Enabled = false;
            editButton.Location = new Point(12, 352);
            editButton.Name = "editButton";
            editButton.Size = new Size(75, 23);
            editButton.TabIndex = 1;
            editButton.Text = "Edit";
            editButton.UseVisualStyleBackColor = true;
            editButton.Click += editButton_Click;
            //
            // deleteButton
            //
            deleteButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            deleteButton.Enabled = false;
            deleteButton.Location = new Point(93, 352);
            deleteButton.Name = "deleteButton";
            deleteButton.Size = new Size(75, 23);
            deleteButton.TabIndex = 2;
            deleteButton.Text = "Delete";
            deleteButton.UseVisualStyleBackColor = true;
            deleteButton.Click += deleteButton_Click;
            //
            // closeButton
            //
            closeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            closeButton.DialogResult = DialogResult.OK;
            closeButton.Location = new Point(613, 352);
            closeButton.Name = "closeButton";
            closeButton.Size = new Size(75, 23);
            closeButton.TabIndex = 3;
            closeButton.Text = "Close";
            closeButton.UseVisualStyleBackColor = true;
            //
            // listMsg
            //
            listMsg.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            listMsg.AutoSize = true;
            listMsg.ForeColor = Color.Firebrick;
            listMsg.Location = new Point(184, 356);
            listMsg.Name = "listMsg";
            listMsg.Size = new Size(0, 15);
            listMsg.TabIndex = 4;
            //
            // HotkeyList
            //
            AcceptButton = closeButton;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(700, 387);
            Controls.Add(listMsg);
            Controls.Add(closeButton);
            Controls.Add(deleteButton);
            Controls.Add(editButton);
            Controls.Add(hotkeyGrid);
            MinimumSize = new Size(560, 300);
            Name = "HotkeyList";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Hot keys";
            ((System.ComponentModel.ISupportInitialize)hotkeyGrid).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridView hotkeyGrid;
        private DataGridViewTextBoxColumn nameColumn;
        private DataGridViewTextBoxColumn hotkeyColumn;
        private DataGridViewTextBoxColumn actionColumn;
        private DataGridViewTextBoxColumn sourceColumn;
        private Button editButton;
        private Button deleteButton;
        private Button closeButton;
        private Label listMsg;
    }
}
