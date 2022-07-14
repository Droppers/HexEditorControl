namespace HexControl.Samples.WinForms
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
            this.hexEditorControl1 = new HexControl.WinForms.HexEditorControl();
            this.SuspendLayout();
            // 
            // hexEditorControl1
            // 
            this.hexEditorControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.hexEditorControl1.Document = null;
            this.hexEditorControl1.EvenForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.hexEditorControl1.HeaderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(190)))));
            this.hexEditorControl1.Location = new System.Drawing.Point(0, 0);
            this.hexEditorControl1.Name = "hexEditorControl1";
            this.hexEditorControl1.OffsetForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(190)))));
            this.hexEditorControl1.OffsetHeader = "Offset";
            this.hexEditorControl1.Size = new System.Drawing.Size(1911, 1061);
            this.hexEditorControl1.TabIndex = 0;
            this.hexEditorControl1.TextHeader = "Decoded text";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(17F, 41F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1911, 1061);
            this.Controls.Add(this.hexEditorControl1);
            this.Margin = new System.Windows.Forms.Padding(7, 8, 7, 8);
            this.Name = "Form1";
            this.Text = "WinForms";
            this.ResumeLayout(false);

        }

        #endregion

        private HexControl.WinForms.HexEditorControl hexEditorControl1;
    }
}
