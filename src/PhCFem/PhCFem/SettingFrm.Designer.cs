namespace PhCFem
{
    partial class SettingFrm
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
            this.NdivForOneLatticeLabel = new System.Windows.Forms.Label();
            this.RodRadiusLabel = new System.Windows.Forms.Label();
            this.RodCircleDivLabel = new System.Windows.Forms.Label();
            this.RodRadiusDivLabel = new System.Windows.Forms.Label();
            this.OKBtn = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.NdivForOneLatticeTB = new System.Windows.Forms.TextBox();
            this.RodEpsLabel = new System.Windows.Forms.Label();
            this.RodRadiusTB = new System.Windows.Forms.TextBox();
            this.RodEpsTB = new System.Windows.Forms.TextBox();
            this.RodCircleDivTB = new System.Windows.Forms.TextBox();
            this.RodRadiusDivTB = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // NdivForOneLatticeLabel
            // 
            this.NdivForOneLatticeLabel.AutoSize = true;
            this.NdivForOneLatticeLabel.Location = new System.Drawing.Point(13, 22);
            this.NdivForOneLatticeLabel.Name = "NdivForOneLatticeLabel";
            this.NdivForOneLatticeLabel.Size = new System.Drawing.Size(95, 12);
            this.NdivForOneLatticeLabel.TabIndex = 0;
            this.NdivForOneLatticeLabel.Text = "格子１辺の分割数";
            // 
            // RodRadiusLabel
            // 
            this.RodRadiusLabel.AutoSize = true;
            this.RodRadiusLabel.Location = new System.Drawing.Point(13, 55);
            this.RodRadiusLabel.Name = "RodRadiusLabel";
            this.RodRadiusLabel.Size = new System.Drawing.Size(64, 12);
            this.RodRadiusLabel.TabIndex = 1;
            this.RodRadiusLabel.Text = "ロッドの半径";
            // 
            // RodCircleDivLabel
            // 
            this.RodCircleDivLabel.AutoSize = true;
            this.RodCircleDivLabel.Location = new System.Drawing.Point(12, 122);
            this.RodCircleDivLabel.Name = "RodCircleDivLabel";
            this.RodCircleDivLabel.Size = new System.Drawing.Size(124, 12);
            this.RodCircleDivLabel.TabIndex = 2;
            this.RodCircleDivLabel.Text = "ロッドの円周方向分割数";
            // 
            // RodRadiusDivLabel
            // 
            this.RodRadiusDivLabel.AutoSize = true;
            this.RodRadiusDivLabel.Location = new System.Drawing.Point(12, 155);
            this.RodRadiusDivLabel.Name = "RodRadiusDivLabel";
            this.RodRadiusDivLabel.Size = new System.Drawing.Size(124, 12);
            this.RodRadiusDivLabel.TabIndex = 3;
            this.RodRadiusDivLabel.Text = "ロッドの半径方向分割数";
            // 
            // OKBtn
            // 
            this.OKBtn.Location = new System.Drawing.Point(33, 207);
            this.OKBtn.Name = "OKBtn";
            this.OKBtn.Size = new System.Drawing.Size(75, 23);
            this.OKBtn.TabIndex = 4;
            this.OKBtn.Text = "OK";
            this.OKBtn.UseVisualStyleBackColor = true;
            this.OKBtn.Click += new System.EventHandler(this.OKBtn_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(134, 207);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 5;
            this.button1.Text = "キャンセル";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // NdivForOneLatticeTB
            // 
            this.NdivForOneLatticeTB.Location = new System.Drawing.Point(155, 22);
            this.NdivForOneLatticeTB.Name = "NdivForOneLatticeTB";
            this.NdivForOneLatticeTB.Size = new System.Drawing.Size(100, 19);
            this.NdivForOneLatticeTB.TabIndex = 6;
            // 
            // RodEpsLabel
            // 
            this.RodEpsLabel.AutoSize = true;
            this.RodEpsLabel.Location = new System.Drawing.Point(13, 89);
            this.RodEpsLabel.Name = "RodEpsLabel";
            this.RodEpsLabel.Size = new System.Drawing.Size(76, 12);
            this.RodEpsLabel.TabIndex = 7;
            this.RodEpsLabel.Text = "ロッドの屈折率";
            // 
            // RodRadiusTB
            // 
            this.RodRadiusTB.Location = new System.Drawing.Point(155, 55);
            this.RodRadiusTB.Name = "RodRadiusTB";
            this.RodRadiusTB.Size = new System.Drawing.Size(100, 19);
            this.RodRadiusTB.TabIndex = 8;
            // 
            // RodEpsTB
            // 
            this.RodEpsTB.Location = new System.Drawing.Point(155, 89);
            this.RodEpsTB.Name = "RodEpsTB";
            this.RodEpsTB.Size = new System.Drawing.Size(100, 19);
            this.RodEpsTB.TabIndex = 9;
            // 
            // RodCircleDivTB
            // 
            this.RodCircleDivTB.Location = new System.Drawing.Point(155, 122);
            this.RodCircleDivTB.Name = "RodCircleDivTB";
            this.RodCircleDivTB.Size = new System.Drawing.Size(100, 19);
            this.RodCircleDivTB.TabIndex = 10;
            // 
            // RodRadiusDivTB
            // 
            this.RodRadiusDivTB.Location = new System.Drawing.Point(155, 155);
            this.RodRadiusDivTB.Name = "RodRadiusDivTB";
            this.RodRadiusDivTB.Size = new System.Drawing.Size(100, 19);
            this.RodRadiusDivTB.TabIndex = 11;
            // 
            // SettingFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 242);
            this.Controls.Add(this.RodRadiusDivTB);
            this.Controls.Add(this.RodCircleDivTB);
            this.Controls.Add(this.RodEpsTB);
            this.Controls.Add(this.RodRadiusTB);
            this.Controls.Add(this.RodEpsLabel);
            this.Controls.Add(this.NdivForOneLatticeTB);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.OKBtn);
            this.Controls.Add(this.RodRadiusDivLabel);
            this.Controls.Add(this.RodCircleDivLabel);
            this.Controls.Add(this.RodRadiusLabel);
            this.Controls.Add(this.NdivForOneLatticeLabel);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingFrm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "設定";
            this.Load += new System.EventHandler(this.SettingFrm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label NdivForOneLatticeLabel;
        private System.Windows.Forms.Label RodRadiusLabel;
        private System.Windows.Forms.Label RodCircleDivLabel;
        private System.Windows.Forms.Label RodRadiusDivLabel;
        private System.Windows.Forms.Button OKBtn;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox NdivForOneLatticeTB;
        private System.Windows.Forms.Label RodEpsLabel;
        private System.Windows.Forms.TextBox RodRadiusTB;
        private System.Windows.Forms.TextBox RodEpsTB;
        private System.Windows.Forms.TextBox RodCircleDivTB;
        private System.Windows.Forms.TextBox RodRadiusDivTB;
    }
}