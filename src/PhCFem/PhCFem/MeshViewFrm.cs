using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PhCFem
{
    /// <summary>
    /// メッシュ表示フォーム
    /// </summary>
    /*public */partial class MeshViewFrm : Form
    {
        /// <summary>
        /// 要素形状区分
        /// </summary>
        private Constants.FemElementShapeDV ElemShapeDv;
        /// <summary>
        /// 要素補間次数
        /// </summary>
        private int ElemOrder;
        /// <summary>
        /// ポストプロセッサ
        /// </summary>
        private FemPostProLogic PostPro = null;
        public MeshViewFrm(Constants.FemElementShapeDV elemShapeDv, int elemOrder, FemPostProLogic postPro)
        {
            InitializeComponent();
            
            // データを受け取る
            ElemShapeDv = elemShapeDv;
            ElemOrder = elemOrder;
            PostPro = postPro;

            //this.DoubleBuffered = true;
            // ダブルバッファ制御用のプロパティを強制的に取得する
            System.Reflection.PropertyInfo p;
            p = typeof(System.Windows.Forms.Control).GetProperty(
                         "DoubleBuffered",
                          System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // ダブルバッファを有効にする
            p.SetValue(panelMesh, true, null);

            // メッシュ形状ラベル
            string shapeStr = "三角形";
            labelElemShape.Text = string.Format("{0}次{1}要素", ElemOrder, shapeStr);
            labelMeshInfo.Text = string.Format("節点数: {0}　要素数: {1}", PostPro.NodeCnt, PostPro.ElementCnt);
        }
        /// <summary>
        /// フォームがロードされたときのイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeshViewFrm_Load(object sender, EventArgs e)
        {
            if (this.Owner != null)
            {
                // 色を親ウィンドウに合わせる
                this.BackColor = this.Owner.BackColor;
                this.ForeColor = this.Owner.ForeColor;
            }
            if (!this.Modal && this.Owner != null && this.StartPosition == FormStartPosition.CenterParent)
            {
                // モードレスの場合は、StartPositionが処理されないようなので、自力で位置を合わせる
                this.Location = new Point(this.Owner.Location.X + (this.Owner.Width - this.Width) / 2, this.Owner.Location.Y + (this.Owner.Height - this.Height) / 2);
            }
        }

        /// <summary>
        /// ペイントイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panelMesh_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // 背景消去
            using (Brush brush = new SolidBrush(panelMesh.BackColor))
            {
                g.FillRectangle(brush, panelMesh.ClientRectangle);
            }
            if (PostPro != null)
            {
                PostPro.DrawMesh(g, panelMesh, true);
            }
        }

        /// <summary>
        /// リサイズイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panelMesh_Resize(object sender, EventArgs e)
        {
            panelMesh.Refresh();
        }
    }
}
