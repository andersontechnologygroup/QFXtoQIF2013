namespace QFXtoQIF2013
{
    partial class Form1
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
            txtQFXFile = new TextBox();
            label1 = new Label();
            btnBrowse = new Button();
            lblAccountName = new Label();
            txtAccountName = new TextBox();
            lblDebugging = new Label();
            txtDebugging = new TextBox();
            btnConvert = new Button();
            openFileDialog = new OpenFileDialog();
            saveFileDialog = new SaveFileDialog();
            SuspendLayout();
            // 
            // txtQFXFile
            // 
            txtQFXFile.Location = new Point(79, 10);
            txtQFXFile.Name = "txtQFXFile";
            txtQFXFile.Size = new Size(428, 27);
            txtQFXFile.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 15);
            label1.Name = "label1";
            label1.Size = new Size(61, 20);
            label1.TabIndex = 1;
            label1.Text = "QFX file";
            // 
            // btnBrowse
            // 
            btnBrowse.Location = new Point(513, 9);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(94, 29);
            btnBrowse.TabIndex = 2;
            btnBrowse.Text = "Browse...";
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += btnBrowse_Click;
            // 
            // lblAccountName
            // 
            lblAccountName.AutoSize = true;
            lblAccountName.Location = new Point(12, 46);
            lblAccountName.Name = "lblAccountName";
            lblAccountName.Size = new Size(107, 20);
            lblAccountName.TabIndex = 3;
            lblAccountName.Text = "Account Name";
            // 
            // txtAccountName
            // 
            txtAccountName.Location = new Point(125, 43);
            txtAccountName.Name = "txtAccountName";
            txtAccountName.Size = new Size(482, 27);
            txtAccountName.TabIndex = 4;
            // 
            // lblDebugging
            // 
            lblDebugging.AutoSize = true;
            lblDebugging.Location = new Point(12, 105);
            lblDebugging.Name = "lblDebugging";
            lblDebugging.Size = new Size(134, 20);
            lblDebugging.TabIndex = 5;
            lblDebugging.Text = "Debugging Output";
            // 
            // txtDebugging
            // 
            txtDebugging.Location = new Point(12, 128);
            txtDebugging.Multiline = true;
            txtDebugging.Name = "txtDebugging";
            txtDebugging.ScrollBars = ScrollBars.Vertical;
            txtDebugging.Size = new Size(595, 131);
            txtDebugging.TabIndex = 6;
            // 
            // btnConvert
            // 
            btnConvert.Location = new Point(513, 76);
            btnConvert.Name = "btnConvert";
            btnConvert.Size = new Size(94, 29);
            btnConvert.TabIndex = 7;
            btnConvert.Text = "Convert";
            btnConvert.UseVisualStyleBackColor = true;
            btnConvert.Click += btnConvert_Click;
            // 
            // 
            // 
            // openFileDialog
            // 
            openFileDialog.DefaultExt = "qfx";
            // 
            // saveFileDialog
            // 
            saveFileDialog.DefaultExt = "qif";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(622, 273);
            Controls.Add(btnConvert);
            Controls.Add(txtDebugging);
            Controls.Add(lblDebugging);
            Controls.Add(txtAccountName);
            Controls.Add(lblAccountName);
            Controls.Add(btnBrowse);
            Controls.Add(label1);
            Controls.Add(txtQFXFile);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form1";
            Text = "QFX to QIF (2013)";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtQFXFile;
        private Label label1;
        private Button btnBrowse;
        private Label lblAccountName;
        private TextBox txtAccountName;
        private Label lblDebugging;
        private TextBox txtDebugging;
        private Button btnConvert;
        private OpenFileDialog openFileDialog;
        private SaveFileDialog saveFileDialog;
    }
}
