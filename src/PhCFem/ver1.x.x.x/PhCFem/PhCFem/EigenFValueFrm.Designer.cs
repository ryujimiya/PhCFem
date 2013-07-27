namespace PhCFem
{
    partial class EigenFValueFrm
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
            this.TopPanel = new System.Windows.Forms.Panel();
            this.btnNextMode = new System.Windows.Forms.Button();
            this.btnPrevMode = new System.Windows.Forms.Button();
            this.labelMode = new System.Windows.Forms.Label();
            this.labelFreq = new System.Windows.Forms.Label();
            this.EigenFValuePanel = new System.Windows.Forms.Panel();
            this.TopPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // TopPanel
            // 
            this.TopPanel.Controls.Add(this.btnNextMode);
            this.TopPanel.Controls.Add(this.btnPrevMode);
            this.TopPanel.Controls.Add(this.labelMode);
            this.TopPanel.Controls.Add(this.labelFreq);
            this.TopPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.TopPanel.Location = new System.Drawing.Point(0, 0);
            this.TopPanel.Name = "TopPanel";
            this.TopPanel.Size = new System.Drawing.Size(358, 38);
            this.TopPanel.TabIndex = 1;
            // 
            // btnNextMode
            // 
            this.btnNextMode.Location = new System.Drawing.Point(271, 5);
            this.btnNextMode.Name = "btnNextMode";
            this.btnNextMode.Size = new System.Drawing.Size(75, 23);
            this.btnNextMode.TabIndex = 3;
            this.btnNextMode.Text = "次のモード";
            this.btnNextMode.UseVisualStyleBackColor = true;
            this.btnNextMode.Click += new System.EventHandler(this.btnNextMode_Click);
            // 
            // btnPrevMode
            // 
            this.btnPrevMode.Location = new System.Drawing.Point(186, 5);
            this.btnPrevMode.Name = "btnPrevMode";
            this.btnPrevMode.Size = new System.Drawing.Size(75, 23);
            this.btnPrevMode.TabIndex = 2;
            this.btnPrevMode.Text = "前のモード";
            this.btnPrevMode.UseVisualStyleBackColor = true;
            this.btnPrevMode.Click += new System.EventHandler(this.btnPrevMode_Click);
            // 
            // labelMode
            // 
            this.labelMode.AutoSize = true;
            this.labelMode.Location = new System.Drawing.Point(107, 16);
            this.labelMode.Name = "labelMode";
            this.labelMode.Size = new System.Drawing.Size(31, 12);
            this.labelMode.TabIndex = 1;
            this.labelMode.Text = "1 / 1";
            // 
            // labelFreq
            // 
            this.labelFreq.AutoSize = true;
            this.labelFreq.Location = new System.Drawing.Point(12, 16);
            this.labelFreq.Name = "labelFreq";
            this.labelFreq.Size = new System.Drawing.Size(61, 12);
            this.labelFreq.TabIndex = 0;
            this.labelFreq.Text = "a/λ = ---";
            // 
            // EigenFValuePanel
            // 
            this.EigenFValuePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.EigenFValuePanel.Location = new System.Drawing.Point(0, 38);
            this.EigenFValuePanel.Name = "EigenFValuePanel";
            this.EigenFValuePanel.Size = new System.Drawing.Size(358, 357);
            this.EigenFValuePanel.TabIndex = 2;
            this.EigenFValuePanel.Paint += new System.Windows.Forms.PaintEventHandler(this.EigenFValuePanel_Paint);
            this.EigenFValuePanel.Resize += new System.EventHandler(this.EigenFValuePanel_Resize);
            // 
            // EigenFValueFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(358, 395);
            this.Controls.Add(this.EigenFValuePanel);
            this.Controls.Add(this.TopPanel);
            this.Name = "EigenFValueFrm";
            this.Text = "固有モード分布";
            this.Load += new System.EventHandler(this.EigenFValueFrm_Load);
            this.TopPanel.ResumeLayout(false);
            this.TopPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel TopPanel;
        private System.Windows.Forms.Panel EigenFValuePanel;
        private System.Windows.Forms.Button btnNextMode;
        private System.Windows.Forms.Button btnPrevMode;
        private System.Windows.Forms.Label labelMode;
        private System.Windows.Forms.Label labelFreq;

    }
}