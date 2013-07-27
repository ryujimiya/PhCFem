using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhCFem
{
    /// <summary>
    /// 設定画面
    /// </summary>
    public partial class SettingFrm : Form
    {
        /// <summary>
        /// 格子１辺の分割数
        /// </summary>
        public int NdivForOneLattice
        {
            private set;
            get;
        }
        /// <summary>
        /// ロッドの半径割合
        /// </summary>
        public double RodRadiusRatio
        {
            private set;
            get;
        }
        /// <summary>
        /// ロッドの比誘電率
        /// </summary>
        public double RodEps
        {
            private set;
            get;
        }
        /// <summary>
        /// ロッドの円周方向分割数
        /// </summary>
        public int RodCircleDiv
        {
            private set;
            get;
        }
        /// <summary>
        /// ロッドの半径方向分割数
        /// </summary>
        public int RodRadiusDiv
        {
            private set;
            get;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SettingFrm(
            int ndivForOneLattice,
            double rodRadiusRatio,
            double rodEps,
            int rodCircleDiv,
            int rodRadiusDiv)
        {
            InitializeComponent();

            NdivForOneLattice = ndivForOneLattice;
            RodRadiusRatio = rodRadiusRatio;
            RodEps = rodEps;
            RodCircleDiv = rodCircleDiv;
            RodRadiusDiv = rodRadiusDiv;
        }

        /// <summary>
        /// フォームのロード
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingFrm_Load(object sender, EventArgs e)
        {
            NdivForOneLatticeTB.Text = string.Format("{0}", NdivForOneLattice);
            RodRadiusTB.Text = string.Format("{0:F6}", RodRadiusRatio);
            RodEpsTB.Text = string.Format("{0:F6}", Math.Sqrt(RodEps));
            RodCircleDivTB.Text = string.Format("{0}", RodCircleDiv);
            RodRadiusDivTB.Text = string.Format("{0}", RodRadiusDiv);
        }

        private void OKBtn_Click(object sender, EventArgs e)
        {
            NdivForOneLattice = int.Parse(NdivForOneLatticeTB.Text);
            RodRadiusRatio = double.Parse(RodRadiusTB.Text);
            RodEps = double.Parse(RodEpsTB.Text);
            RodEps = RodEps * RodEps; // 屈折率→比誘電率へ変換
            RodCircleDiv = int.Parse(RodCircleDivTB.Text);
            RodRadiusDiv = int.Parse(RodRadiusDivTB.Text);
            if (NdivForOneLattice <= 0)
            {
                MessageBox.Show("格子１辺の分割数が不正な値です。");
                return;
            }
            if (RodRadiusRatio < Constants.PrecisionLowerLimit || RodRadiusRatio >= 0.5 - Constants.PrecisionLowerLimit)
            {
                MessageBox.Show("ロッド半径が範囲外です。0～0.5の間を指定してください。");
                return;
            }
            if (RodEps < 1.0 - Constants.PrecisionLowerLimit)
            {
                MessageBox.Show("ロッドの屈折率が範囲外です。1.0以上を指定してください。");
                return;
            }
            if (RodCircleDiv <= 7)
            {
                MessageBox.Show("ロッドの円周方向の分割数が不正な値です。8以上を指定してください。");
                return;
            }
            if (RodRadiusDiv <= 0)
            {
                MessageBox.Show("ロッドの半径方向の分割数が不正な値です。");
                return;
            }

            DialogResult = DialogResult.OK;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
