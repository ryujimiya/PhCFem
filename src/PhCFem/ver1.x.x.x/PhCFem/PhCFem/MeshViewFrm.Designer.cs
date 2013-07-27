namespace PhCFem
{
    partial class MeshViewFrm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.panelTop = new System.Windows.Forms.Panel();
            this.labelMeshInfo = new System.Windows.Forms.Label();
            this.labelElemShape = new System.Windows.Forms.Label();
            this.panelMesh = new System.Windows.Forms.Panel();
            this.panelTop.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.labelMeshInfo);
            this.panelTop.Controls.Add(this.labelElemShape);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(401, 29);
            this.panelTop.TabIndex = 0;
            // 
            // labelMeshInfo
            // 
            this.labelMeshInfo.AutoSize = true;
            this.labelMeshInfo.Font = new System.Drawing.Font("MS UI Gothic", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelMeshInfo.Location = new System.Drawing.Point(158, 8);
            this.labelMeshInfo.Name = "labelMeshInfo";
            this.labelMeshInfo.Size = new System.Drawing.Size(139, 15);
            this.labelMeshInfo.TabIndex = 1;
            this.labelMeshInfo.Text = "節点数: 0　要素数: 0";
            // 
            // labelElemShape
            // 
            this.labelElemShape.AutoSize = true;
            this.labelElemShape.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelElemShape.Location = new System.Drawing.Point(10, 8);
            this.labelElemShape.Name = "labelElemShape";
            this.labelElemShape.Size = new System.Drawing.Size(122, 16);
            this.labelElemShape.TabIndex = 0;
            this.labelElemShape.Text = "２次三角形要素";
            // 
            // panelMesh
            // 
            this.panelMesh.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMesh.Location = new System.Drawing.Point(0, 29);
            this.panelMesh.Name = "panelMesh";
            this.panelMesh.Size = new System.Drawing.Size(401, 360);
            this.panelMesh.TabIndex = 1;
            this.panelMesh.Paint += new System.Windows.Forms.PaintEventHandler(this.panelMesh_Paint);
            this.panelMesh.Resize += new System.EventHandler(this.panelMesh_Resize);
            // 
            // MeshViewFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(401, 389);
            this.Controls.Add(this.panelMesh);
            this.Controls.Add(this.panelTop);
            this.Name = "MeshViewFrm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "有限要素メッシュ表示";
            this.Load += new System.EventHandler(this.MeshViewFrm_Load);
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label labelElemShape;
        private System.Windows.Forms.Panel panelMesh;
        private System.Windows.Forms.Label labelMeshInfo;
    }
}