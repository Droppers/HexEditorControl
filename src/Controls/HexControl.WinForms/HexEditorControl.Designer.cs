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
            this.tlpGrid = new System.Windows.Forms.TableLayoutPanel();
            this.tlpGrid.SuspendLayout();
            this.SuspendLayout();
            // 
            // d2dControl
            // 
            this.d2dControl.CanRender = true;
            this.d2dControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.d2dControl.Location = new System.Drawing.Point(0, 0);
            this.d2dControl.Margin = new System.Windows.Forms.Padding(0);
            this.d2dControl.Name = "d2dControl";
            this.d2dControl.Size = new System.Drawing.Size(1372, 905);
            this.d2dControl.TabIndex = 0;
            this.d2dControl.TabStop = false;
            // 
            // sbVertical
            // 
            this.sbVertical.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sbVertical.Location = new System.Drawing.Point(1375, 0);
            this.sbVertical.Name = "sbVertical";
            this.sbVertical.Size = new System.Drawing.Size(17, 905);
            this.sbVertical.TabIndex = 1;
            this.sbVertical.Margin = new System.Windows.Forms.Padding(0);
            // 
            // sbHorizontal
            // 
            this.sbHorizontal.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sbHorizontal.Location = new System.Drawing.Point(0, 908);
            this.sbHorizontal.Name = "sbHorizontal";
            this.sbHorizontal.Size = new System.Drawing.Size(1372, 17);
            this.sbHorizontal.TabIndex = 2;
            this.sbHorizontal.Margin = new System.Windows.Forms.Padding(0);
            // 
            // panel1
            // 
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(1372, 905);
            this.panel1.Margin = new System.Windows.Forms.Padding(0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(0, 0);
            this.panel1.TabIndex = 3;
            // 
            // txtFake
            // 
            this.txtFake.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFake.Location = new System.Drawing.Point(1181, 808);
            this.txtFake.Margin = new System.Windows.Forms.Padding(7, 8, 7, 8);
            this.txtFake.Name = "txtFake";
            this.txtFake.Size = new System.Drawing.Size(0, 47);
            this.txtFake.TabIndex = 0;
            this.txtFake.TabStop = false;
            // 
            // tlpGrid
            // 
            this.tlpGrid.ColumnCount = 2;
            this.tlpGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpGrid.Controls.Add(this.sbVertical, 1, 0);
            this.tlpGrid.Controls.Add(this.sbHorizontal, 0, 1);
            this.tlpGrid.Controls.Add(this.d2dControl, 0, 0);
            this.tlpGrid.Controls.Add(this.panel1, 1, 1);
            this.tlpGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpGrid.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
            this.tlpGrid.Location = new System.Drawing.Point(0, 0);
            this.tlpGrid.Margin = new System.Windows.Forms.Padding(0);
            this.tlpGrid.Name = "tlpGrid";
            this.tlpGrid.RowCount = 2;
            this.tlpGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpGrid.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpGrid.Size = new System.Drawing.Size(1392, 925);
            this.tlpGrid.TabIndex = 4;
            this.txtFake.TabStop = false;
            // 
            // HexEditorControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(17F, 41F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.tlpGrid);
            this.Controls.Add(this.txtFake);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "HexEditorControl";
            this.Size = new System.Drawing.Size(1392, 925);
            this.txtFake.TabStop = false;
            this.tlpGrid.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private D2DControl d2dControl;
        private System.Windows.Forms.VScrollBar sbVertical;
        private System.Windows.Forms.HScrollBar sbHorizontal;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TextBox txtFake;
        private TableLayoutPanel tlpGrid;
    }
}
