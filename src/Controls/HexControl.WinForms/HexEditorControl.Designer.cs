namespace HexControl.WinForms
{
    public partial class HexEditorControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.d2dControl = new HexControl.WinForms.D2DControl();
            this.sbVertical = new System.Windows.Forms.VScrollBar();
            this.sbHorizontal = new System.Windows.Forms.HScrollBar();
            this.panel1 = new System.Windows.Forms.Panel();
            this.txtFake = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // d2dControl
            // 
            this.d2dControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.d2dControl.Location = new System.Drawing.Point(0, 0);
            this.d2dControl.Margin = new System.Windows.Forms.Padding(0);
            this.d2dControl.Name = "d2dControl";
            this.d2dControl.Size = new System.Drawing.Size(322, 289);
            this.d2dControl.TabIndex = 0;
            this.d2dControl.Text = "direct3dRenderer1";
            // 
            // sbVertical
            // 
            this.sbVertical.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sbVertical.Location = new System.Drawing.Point(322, 0);
            this.sbVertical.Name = "sbVertical";
            this.sbVertical.Size = new System.Drawing.Size(17, 289);
            this.sbVertical.TabIndex = 1;
            // 
            // sbHorizontal
            // 
            this.sbHorizontal.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sbHorizontal.Location = new System.Drawing.Point(0, 289);
            this.sbHorizontal.Name = "sbHorizontal";
            this.sbHorizontal.Size = new System.Drawing.Size(322, 17);
            this.sbHorizontal.TabIndex = 2;
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Location = new System.Drawing.Point(322, 289);
            this.panel1.Margin = new System.Windows.Forms.Padding(0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(17, 17);
            this.panel1.TabIndex = 3;
            // 
            // txtFake
            // 
            this.txtFake.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFake.Location = new System.Drawing.Point(252, 263);
            this.txtFake.Name = "txtFake";
            this.txtFake.Size = new System.Drawing.Size(67, 23);
            this.txtFake.TabIndex = 0;
            this.txtFake.Width = 0;
            this.txtFake.Height = 0;
            // 
            // HexEditorControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.txtFake);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.sbHorizontal);
            this.Controls.Add(this.sbVertical);
            this.Controls.Add(this.d2dControl);
            this.Name = "HexEditorControl";
            this.Size = new System.Drawing.Size(339, 306);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private D2DControl d2dControl;
        private System.Windows.Forms.VScrollBar sbVertical;
        private System.Windows.Forms.HScrollBar sbHorizontal;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TextBox txtFake;
    }
}
