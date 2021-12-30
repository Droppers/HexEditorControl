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
            this.hexEditorControl2 = new HexControl.WinForms.HexEditorControl();
            this.SuspendLayout();
            // 
            // hexEditorControl2
            // 
            this.hexEditorControl2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.hexEditorControl2.Location = new System.Drawing.Point(0, 0);
            this.hexEditorControl2.Name = "hexEditorControl2";
            this.hexEditorControl2.Size = new System.Drawing.Size(787, 533);
            this.hexEditorControl2.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(787, 533);
            this.Controls.Add(this.hexEditorControl2);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion
        
        private HexControl.WinForms.HexEditorControl hexEditorControl2;
    }
}
