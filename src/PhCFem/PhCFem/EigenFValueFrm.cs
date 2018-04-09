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
    /// 固有モード分布フォーム
    /// </summary>
    /*public*/ partial class EigenFValueFrm : Form
    {
        /// <summary>
        /// ポストプロセッサ
        /// </summary>
        private FemPostProLogic PostPro = null;
        /// <summary>
        /// 出力ファイル名
        /// </summary>
        private string OutputPeriodicFilename = "";
        /// <summary>
        /// 周波数番号
        /// </summary>
        private int _FreqNo = -1;
        /// <summary>
        /// モードインデックス
        /// </summary>
        private int ModeIndex = 0;
        /// <summary>
        /// 最大モード数
        /// </summary>
        private int MaxModeCnt = 0;
        /// <summary>
        /// 周波数番号
        /// </summary>
        public int FreqNo
        {
            get { return _FreqNo; }
            set
            {
                _FreqNo = value;
                setupGUI();
                updateFValuePanel();
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="postPro"></param>
        public EigenFValueFrm(FemPostProLogic postPro, string outputPeriodicFilename)
        {
            InitializeComponent();

            // ダブルバッファ制御用のプロパティを強制的に取得する
            System.Reflection.PropertyInfo p;
            p = typeof(System.Windows.Forms.Control).GetProperty(
                         "DoubleBuffered",
                          System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            // ダブルバッファを有効にする
            p.SetValue(EigenFValuePanel, true, null);

            PostPro = postPro;
            OutputPeriodicFilename = outputPeriodicFilename;
            _FreqNo = -1;
        }

        /// <summary>
        /// GUIを準備する
        /// </summary>
        private void setupGUI()
        {
            MaxModeCnt = PostPro.GetMaxModeCnt();
            if (ModeIndex > (MaxModeCnt - 1))
            {
                ModeIndex = 0;
            }

            labelFreq.Text = string.Format("a/λ = {0:F3}", PostPro.GetNormalizedFrequency());
            labelMode.Text = string.Format("{0} / {1}", (ModeIndex + 1), MaxModeCnt);
            bool isEnabled = (MaxModeCnt > 0);
            btnPrevMode.Enabled = isEnabled;
            btnNextMode.Enabled = isEnabled;
        }

        /// <summary>
        /// 固有モード分布を更新する
        /// </summary>
        private void updateFValuePanel()
        {
            PostPro.LoadOutputPeriodic(OutputPeriodicFilename, _FreqNo, ModeIndex);
            EigenFValuePanel.Refresh();
        }

        /// <summary>
        /// フォームのロードイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EigenFValueFrm_Load(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// フィールドパネルの描画
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EigenFValuePanel_Paint(object sender, PaintEventArgs e)
        {
            Panel panel = (Panel)sender;
            Rectangle r = panel.ClientRectangle;
            Graphics g = e.Graphics;

            // 背景クリア
            using (Brush brush = new SolidBrush(panel.BackColor))
            {
                g.FillRectangle(brush, r);
            }

            PostPro.DrawMesh(g, panel, r, true);
            PostPro.DrawEigenField(g, panel, r, FemElement.ValueDV.Real); // 実部表示
            PostPro.DrawMediaB(g, panel, r, true);
        }

        /// <summary>
        /// フィールドパネルのリサイズイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EigenFValuePanel_Resize(object sender, EventArgs e)
        {
            Panel panel = (Panel)sender;
            panel.Refresh();
        }

        /// <summary>
        /// 前のモードボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPrevMode_Click(object sender, EventArgs e)
        {
            ModeIndex--;
            if (ModeIndex < 0)
            {
                ModeIndex = MaxModeCnt - 1;
            }
            setupGUI();
            updateFValuePanel();
        }

        /// <summary>
        /// 次のモードボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnNextMode_Click(object sender, EventArgs e)
        {
            ModeIndex++;
            if (ModeIndex > (MaxModeCnt - 1))
            {
                ModeIndex = 0;
            }
            setupGUI();
            updateFValuePanel();
        }

    }
}
