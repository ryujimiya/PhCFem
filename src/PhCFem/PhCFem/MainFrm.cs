using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;
using System.Threading;

namespace PhCFem
{
    public partial class MainFrm : Form
    {
        ////////////////////////////////////////////////////////////////////////
        // 型
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// フォームコントロール同期用デリゲート
        /// </summary>
        delegate void InvokeDelegate();
        delegate void ParameterizedInvokeDelegate(params Object[] parameter);

        /// <summary>
        /// セルの種類構造体
        ///   (ComboboxのItemsに割り当てるオブジェクトとして使用)
        /// </summary>
        struct CellTypeStruct
        {
            /// <summary>
            /// セルの種類
            /// </summary>
            public CadLogic.CellType CellTypeVal;
            /// <summary>
            /// 表示用テキスト
            /// </summary>
            public string Text;

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="text"></param>
            /// <param name="cadMode"></param>
            public CellTypeStruct(string text, CadLogic.CellType cellType)
            {
                Text = text;
                CellTypeVal = cellType;
            }

            /// <summary>
            /// 文字列変換
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                //return base.ToString();
                return Text;
            }
        }
        ////////////////////////////////////////////////////////////////////////
        // 定数
        ////////////////////////////////////////////////////////////////////////
        
        /// <summary>
        /// 等高線図パネルのフィールド区分-値区分リスト
        /// </summary>
        private readonly KeyValuePair<FemElement.FieldDV, FemElement.ValueDV>[] FValuePanelFieldDV_ValueDVPairList = 
            {
                // Ez (絶対値)
                new KeyValuePair<FemElement.FieldDV, FemElement.ValueDV>(FemElement.FieldDV.Field, FemElement.ValueDV.Abs),
                // Ez (実数部)
                new KeyValuePair<FemElement.FieldDV, FemElement.ValueDV>(FemElement.FieldDV.Field, FemElement.ValueDV.Real),
                // (Hx, Hy)ベクトル表示(実数部)
                new KeyValuePair<FemElement.FieldDV, FemElement.ValueDV>(FemElement.FieldDV.RotXY, FemElement.ValueDV.Real),
                // 複素ポインティングベクトル表示(実数部)
                new KeyValuePair<FemElement.FieldDV, FemElement.ValueDV>(FemElement.FieldDV.PoyntingXY, FemElement.ValueDV.Real),
            };
        /// <summary>
        /// 等高線図パネルのコンテンツ表示名(TEモード)
        /// </summary>
        private readonly string[] FValuePanelContentNameForE = 
            {
                "|Ez|等高線図",
                "Ezの実数部の等高線図",
                "(Hx, Hy)ベクトルの実数部のベクトル表示",
                "複素ポインティングベクトルのベクトル表示",
            };
        /// <summary>
        /// 等高線図パネルのコンテンツ表示名(TEモード)
        /// </summary>
        private readonly string[] FValuePanelContentNameForH = 
            {
                "|Hz|等高線図",
                "Hzの実数部の等高線図",
                "(Ex, Ey)ベクトルの実数部のベクトル表示",
                "複素ポインティングベクトルのベクトル表示",
            };

        ////////////////////////////////////////////////////////////////////////
        // フィールド
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// フォームのタイトル
        /// </summary>
        private string TitleBaseName = "";
        /// <summary>
        /// データファイルパス拡張子抜き
        /// </summary>
        //private string FilePathWithoutExt = "";
        /// <summary>
        /// CADデータファイルパス
        /// </summary>
        private string CadDatFilePath = "";
        /// <summary>
        /// Fem入力データファイルパス
        /// </summary>
        private string FemInputDatFilePath = "";
        /// <summary>
        /// Fem出力データファイルパス
        /// </summary>
        private string FemOutputDatFilePath = "";
        /// <summary>
        /// Cadロジック
        /// </summary>
        private CadLogic CadLgc = null;
        /// <summary>
        /// 解析機
        /// </summary>
        private FemSolver Solver = null;
        /// <summary>
        /// ポストプロセッサ
        /// </summary>
        private FemPostProLogic PostPro = null;
        /// <summary>
        /// Cadモード選択ラジオボタンリスト
        /// </summary>
        private RadioButton[] CadModeRadioButtons = null;
        /// <summary>
        /// 周波数に対応するインデックス(1..FemSolver.CalcFreqCnt - 1)
        /// </summary>
        private int FreqNo = -1;
        /// <summary>
        /// 計算スレッド
        /// </summary>
        private Thread SolverThread = null;
        /// <summary>
        /// メッシュ表示ウィンドウ
        /// </summary>
        private MeshViewFrm MeshView = null;
        /// <summary>
        /// 周期構造導波路固有モード分布フォーム
        /// </summary>
        private EigenFValueFrm EigenFValueFrm = null;
        /// <summary>
        /// フォームの初期サイズ
        /// </summary>
        private Size FrmBaseSize;
        private Point CadPanelBaseLocation;
        private Size CadPanelBaseSize;
        private Point GroupBoxCadModeBaseLocation;
        private Size GroupBoxCadModeBaseSize;
        private Point FValuePanelBaseLocation;
        private Size FValuePanelBaseSize;
        private Point FValueLegendPanelBaseLocation;
        private Size FValueLegendPanelBaseSize;
        private Point SMatChartBaseLocation;
        private Size SMatChartBaseSize;
        private Point SettingBtnBaseLocation;
        private Size SettingBtnBaseSize;
        private Point LinkLblEigenShowBaseLocation;
        private Size LinkLblEigenShowBaseSize;
        private Point BetaChartBaseLocation;
        private Size BetaChartBaseSize;
        private Point EigenVecChartBaseLocation;
        private Size EigenVecChartBaseSize;
        private Control MaximizedControl = null;
        /// <summary>
        /// 固有モード表示フラグ
        /// </summary>
        private bool EigenShowFlg = false;
        /// <summary>
        /// フォームの通常時のサイズ(ほぼRestoreBounds.Sizeと同じだが、最大化時[固有モードを見る]を実行すると異なってくる
        /// </summary>
        private Size FrmNormalSize;
        private FormWindowState PrevWindowState = FormWindowState.Normal;
        /// <summary>
        /// 計算中？
        /// </summary>
        private bool IsCalculating
        {
            get
            {
                return SolverThread != null && SolverThread.IsAlive;
            }
        }
        /// <summary>
        /// 読み込み中？
        /// </summary>
        private bool IsLoading = false;
        /// <summary>
        /// 読み込みアニメーションをキャンセルする
        /// </summary>
        private bool IsLoadCancelled = false;

        /// <summary>
        /// 等高線図パネルのインデックス
        /// (表示内容の切り替え:FValuePanelFieldDV_ValueDVPairListに対応するインデックス、ただし、FValuePanelFieldDV_ValueDVPairList.Lengthの場合は4画面)
        /// </summary>
        private int FValuePanelIndex = 0;
        /// <summary>
        /// 等高線図パネルのマウスエンターイベントのイベントハンドラ処理を実行しない?
        /// </summary>
        private bool IsDisabledMouseEnterOfFValuePanel = false;
        /// <summary>
        /// 等高線図パネルのマウス移動位置
        /// </summary>
        private Point FValuePanelMovPt;

        /// <summary>
        /// ウィンドウコンストラクタ
        /// </summary>
        public MainFrm()
        {
            InitializeComponent();
            init();
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void init()
        {
            CadLgc = new CadLogic(CadPanel);
            CadLgc.Change += new CadLogic.ChangeDeleagte(CadLgc_Change);
            Solver = new FemSolver();
            PostPro = new FemPostProLogic();

            CadModeRadioButtons = new RadioButton[]
            {
                radioBtnNone,
                radioBtnLocation,
                radioBtnArea,
                radioBtnPort,
                radioBtnErase,
                radioBtnIncidentPort,
                radioBtnPortNumbering
            };
            // Cadモードをラジオボタンに紐づける
            CadLogic.CadModeType[] cadModeTypeForRadioButtons = new CadLogic.CadModeType[]
            {
                CadLogic.CadModeType.None,
                CadLogic.CadModeType.Location,
                CadLogic.CadModeType.Area,
                CadLogic.CadModeType.Port,
                CadLogic.CadModeType.Erase,
                CadLogic.CadModeType.IncidentPort,
                CadLogic.CadModeType.PortNumbering
            };
            System.Diagnostics.Debug.Assert(CadModeRadioButtons.Length == cadModeTypeForRadioButtons.Length);
            for (int i = 0; i < CadModeRadioButtons.Length; i++)
            {
                CadModeRadioButtons[i].Tag = cadModeTypeForRadioButtons[i];
            }
            // エリア選択描画モードタイプコンボボックスのItemにCadモードを紐づける
            CellTypeStruct[] cellTypeStructsForImgCBoxCadModeArea = new CellTypeStruct[]
            {
                new CellTypeStruct("真空", CadLogic.CellType.Defect),
                new CellTypeStruct("誘電体ロッド", CadLogic.CellType.Rod),
            };
            // コンボボックスのアイテムをクリア
            imgcbxCellType.Items.Clear();
            foreach (CellTypeStruct cellTypeStruct in cellTypeStructsForImgCBoxCadModeArea)
            {
                // コンボボックスにアイテムを追加
                imgcbxCellType.Items.Add(cellTypeStruct);
                if (CadLgc != null)
                {
                    if (CadLgc.SelectedCellType == cellTypeStruct.CellTypeVal)
                    {
                        imgcbxCellType.SelectedItem = cellTypeStruct;
                    }
                }
            }
            imgcbxCellType.Visible = false;
            btnLoadCancel.Visible = false;

            //TEST 4画面表示
            FValuePanelIndex = FValuePanelFieldDV_ValueDVPairList.Length; //0;
            // 等高線図パネルインデックス変更時の処理
            changeFValuePanelIndexProc(false);

            // アプリケーションの終了イベントハンドラを設定する
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine("Process exiting");
                //System.Diagnostics.Debug.WriteLine("Process exiting");
                // フォームの破棄処理を呼び出す
                this.Dispose();
            };

            // パネルサイズを記憶する
            savePanelSize();

            //this.DoubleBuffered = true;
            // ダブルバッファ制御用のプロパティを強制的に取得する
            System.Reflection.PropertyInfo p;
            p = typeof(System.Windows.Forms.Control).GetProperty(
                         "DoubleBuffered",
                          System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            // ダブルバッファを有効にする
            p.SetValue(CadPanel, true, null);
            p.SetValue(FValuePanel, true, null);

            // フォームのタイトルを退避
            TitleBaseName = this.Text + " " + MyUtilLib.MyUtil.getAppVersion();

            // ファイル名付きフォームタイトルを設定
            setFrmTitle();

            // GUI初期化
            resetGUI();
        }

        /// <summary>
        /// Cadモードをラジオボタンに反映する
        /// </summary>
        /// <param name="cadMode"></param>
        private void setupCadModeRadioButtons(CadLogic.CadModeType cadMode)
        {
            // ラジオボタンのチェック状態を設定する
            foreach (RadioButton rb in CadModeRadioButtons)
            {
                if ((CadLogic.CadModeType)rb.Tag == cadMode)
                {
                    rb.Checked = true;
                }
                else
                {
                    rb.Checked = false;
                }
            }
        }

        /// <summary>
        /// 自動スクロールを設定する
        /// </summary>
        /// <param name="autoScroll"></param>
        private void setAutoScroll(bool autoScroll)
        {
            System.Diagnostics.Debug.WriteLine("setAutoScroll:{0}", autoScroll);
            this.AutoScroll = autoScroll;
            this.AutoScrollOffset = new Point(0, 0);
            this.AutoScrollPosition = new Point(0, 0);
        }

        /// <summary>
        /// パネルの初期値、サイズを記憶する
        /// </summary>
        private void savePanelSize()
        {
            // 初期の位置、サイズを記憶する
            FrmBaseSize = this.ClientSize;
            CadPanelBaseLocation = CadPanel.Location;
            CadPanelBaseSize = CadPanel.Size;
            GroupBoxCadModeBaseLocation = GroupBoxCadMode.Location;
            GroupBoxCadModeBaseSize = GroupBoxCadMode.Size;
            SettingBtnBaseLocation = btnSetting.Location;
            SettingBtnBaseSize = btnSetting.Size;
            LinkLblEigenShowBaseLocation = linkLblEigenShow.Location;
            LinkLblEigenShowBaseSize = linkLblEigenShow.Size;
            FValuePanelBaseLocation = FValuePanel.Location;
            FValuePanelBaseSize = FValuePanel.Size;
            FValueLegendPanelBaseLocation = FValueLegendPanel.Location;
            FValueLegendPanelBaseSize = FValueLegendPanel.Size;
            SMatChartBaseLocation = SMatChart.Location;
            SMatChartBaseSize = SMatChart.Size;
            BetaChartBaseLocation = BetaChart.Location;
            BetaChartBaseSize = BetaChart.Size;
            EigenVecChartBaseLocation = EigenVecChart.Location;
            EigenVecChartBaseSize = EigenVecChart.Size;
        }

        /// <summary>
        /// パネルのサイズをフォームのサイズに合わせる
        /// </summary>
        private void fitPanelSizeToFrmSize()
        {
            System.Diagnostics.Debug.WriteLine("fitPanelSizeToFrmSize");
            Control[] ctrlList = { SMatChart, BetaChart, EigenVecChart };
            Point[] ctrlBaseLocationList = { SMatChartBaseLocation, BetaChartBaseLocation, EigenVecChartBaseLocation };
            Size[] ctrlBaseSizeList = { SMatChartBaseSize, BetaChartBaseSize, EigenVecChartBaseSize };

            // [前のパネル]ボタン、[次のパネル]ボタン
            btnPrevFValuePanel.Visible = false;
            btnNextFValuePanel.Visible = false;

            // 個別パネルの最大化処理
            if (MaximizedControl == CadPanel)
            {
                doCadPanelMaximize(ctrlList);
                return;
            }
            else if (MaximizedControl == FValuePanel)
            {
                doFValuePanelMaximize(ctrlList);
                return;
            }
            else if (MaximizedControl != null && ctrlList.Contains(MaximizedControl))
            {
                doControlMaximize(MaximizedControl, ctrlList, ctrlBaseLocationList, ctrlBaseSizeList);
                return;
            }
            else if (MaximizedControl != null)
            {
                // 対応コントロールでない
                // 通常モードに戻す
                MaximizedControl = null; // fail safe
            }

            // 通常の場合
            if (!CadPanel.Visible)
            {
                CadPanel.Visible = true;
            }
            if (!GroupBoxCadMode.Visible)
            {
                GroupBoxCadMode.Visible = true;
            }
            if (!FValuePanel.Visible)
            {
                FValuePanel.Visible = true;
            }
            if (!FValueLegendPanel.Visible)
            {
                FValueLegendPanel.Visible = true;
            }
            if (!SMatChart.Visible)
            {
                SMatChart.Visible = true;
            }
            if (!btnSetting.Visible)
            {
                btnSetting.Visible = true;
            }
            if (!linkLblEigenShow.Visible)
            {
                linkLblEigenShow.Visible = true;
            }
            BetaChart.Visible = EigenShowFlg;
            EigenVecChart.Visible = EigenShowFlg;
            btnEigenFieldShow.Visible = EigenShowFlg;

            // 自動スクロールを一旦やめる
            setAutoScroll(false);

            this.SuspendLayout();
            // パネル配置変更
            double r = (this.ClientSize.Width - SystemInformation.VerticalScrollBarWidth) / (double)(FrmBaseSize.Width - SystemInformation.VerticalScrollBarWidth);
            //            double r = this.ClientSize.Width / (double)FrmBaseSize.Width;
            if (r <= 1.0)
            {
                r = 1.0;
            }
            CadPanel.Location = CadPanelBaseLocation;
            CadPanel.Size = new Size((int)((double)CadPanelBaseSize.Width * r), (int)((double)CadPanelBaseSize.Height * r));
            GroupBoxCadMode.Location = new Point(CadPanel.Left, CadPanel.Bottom);
            GroupBoxCadMode.Size = GroupBoxCadModeBaseSize;
            btnSetting.Location = new Point(SettingBtnBaseLocation.X, GroupBoxCadMode.Top);
            linkLblEigenShow.Location = new Point(LinkLblEigenShowBaseLocation.X, btnSetting.Bottom + 10);
            FValuePanel.Location = new Point((ClientSize.Width - SystemInformation.VerticalScrollBarWidth - FValueLegendPanelBaseSize.Width - (int)((double)FValuePanelBaseSize.Width * r)), FValuePanelBaseLocation.Y);
            FValuePanel.Size = new Size((int)((double)FValuePanelBaseSize.Width * r), (int)((double)FValuePanelBaseSize.Height * r));
            FValueLegendPanel.Location = new Point(ClientSize.Width - SystemInformation.VerticalScrollBarWidth - FValueLegendPanelBaseSize.Width, FValueLegendPanelBaseLocation.Y);
            FValueLegendPanel.Size = FValueLegendPanelBaseSize;
            SMatChart.Location = new Point(ClientSize.Width - SystemInformation.VerticalScrollBarWidth - (int)((double)SMatChartBaseSize.Width * r), (FValuePanel.Bottom > FValueLegendPanel.Bottom) ? FValuePanel.Bottom : FValueLegendPanel.Bottom);
            SMatChart.Size = new Size((int)((double)SMatChartBaseSize.Width * r), (int)((double)SMatChartBaseSize.Height * r));
            int betaChartYPos = (GroupBoxCadMode.Bottom > SMatChart.Bottom)? GroupBoxCadMode.Bottom : SMatChart.Bottom;
            BetaChart.Location = new Point(CadPanel.Left, betaChartYPos);
            BetaChart.Size = new Size((int)((double)BetaChartBaseSize.Width * r), (int)((double)BetaChartBaseSize.Height * r));
            EigenVecChart.Location = new Point(SMatChart.Right - (int)((double)EigenVecChartBaseSize.Width * r), betaChartYPos);
            EigenVecChart.Size = new Size((int)((double)EigenVecChartBaseSize.Width * r), (int)((double)EigenVecChartBaseSize.Height * r));
            btnEigenFieldShow.Location = new Point(EigenVecChart.Left, EigenVecChart.Bottom - btnEigenFieldShow.Size.Height);
            this.ResumeLayout();

            CadLgc.SetupRegionSize();
            CadPanel.Invalidate();
            FValuePanel.Invalidate();

            // 自動スクロールの情報を更新するため明示的に再設定しています。
            setAutoScroll(true);
        }

        /// <summary>
        /// Cadパネルを最大化する
        /// </summary>
        private void doCadPanelMaximize(Control[] ctrlList)
        {
            // 自動スクロールを一旦やめる
            setAutoScroll(false);

            this.SuspendLayout();
            // パネル配置変更
            double r = (this.ClientSize.Width - SystemInformation.VerticalScrollBarWidth) / (double)CadPanelBaseSize.Width;
            CadPanel.Location = new Point(0, btnNew.Bottom); // ファイル操作ボタンの高さ分ずらす
            CadPanel.Size = new Size((int)((double)CadPanelBaseSize.Width * r), (int)((double)CadPanelBaseSize.Height * r));
            //GroupBoxCadMode.Location = CadPanel.Location + new Size(0, CadPanel.Height - GroupBoxCadModeBaseSize.Height);
            GroupBoxCadMode.Location = new Point(CadPanel.Left, CadPanel.Bottom);
            GroupBoxCadMode.Size = GroupBoxCadModeBaseSize;
            this.ResumeLayout();

            btnSetting.Visible = false;
            linkLblEigenShow.Visible = false;
            FValuePanel.Visible = false;
            FValueLegendPanel.Visible = false;
            btnEigenFieldShow.Visible = false;
            foreach (Control workCtrl in ctrlList)
            {
                if (CadPanel != workCtrl)
                {
                    workCtrl.Visible = false;
                }
            }
            CadLgc.SetupRegionSize();
            CadPanel.Invalidate();

            // 自動スクロールの情報を更新するため明示的に再設定しています。
            setAutoScroll(true);
        }

        /// <summary>
        /// フィールド値パネルを最大化する
        /// </summary>
        private void doFValuePanelMaximize(Control[] ctrlList)
        {
            // 自動スクロールを一旦やめる
            setAutoScroll(false);

            this.SuspendLayout();
            // パネル配置変更
            //double r = (this.ClientSize.Width - SystemInformation.VerticalScrollBarWidth) / (double)FValuePanelBaseSize.Width;
            double r = (this.ClientSize.Width - FValueLegendPanelBaseSize.Width - SystemInformation.VerticalScrollBarWidth) / (double)FValuePanelBaseSize.Width; // 凡例分幅を縮める
            FValuePanel.Location = new Point(0, btnNew.Bottom); // ファイル操作ボタンの高さ分ずらす
            FValuePanel.Size = new Size((int)((double)FValuePanelBaseSize.Width * r), (int)((double)FValuePanelBaseSize.Height * r));
            //FValueLegendPanel.Location = FValuePanel.Location + new Size(FValuePanel.Width - FValueLegendPanelBaseSize.Width, 0);
            FValueLegendPanel.Location = FValuePanel.Location + new Size(FValuePanel.Width, 0);
            FValueLegendPanel.Size = FValueLegendPanelBaseSize;
            this.ResumeLayout();

            CadPanel.Visible = false;
            GroupBoxCadMode.Visible = false;
            btnSetting.Visible = false;
            linkLblEigenShow.Visible = false;
            btnEigenFieldShow.Visible = false;
            foreach (Control workCtrl in ctrlList)
            {
                if (FValuePanel != workCtrl)
                {
                    workCtrl.Visible = false;
                }
            }
            FValuePanel.Invalidate();

            // 自動スクロールの情報を更新するため明示的に再設定しています。
            setAutoScroll(true);
        }

        /// <summary>
        /// コントロールを最大化する
        /// </summary>
        private void doControlMaximize(Control tagtCtrl, Control[] ctrlList, Point[] ctrlBaseLocationList, Size[] ctrlBaseSizeList)
        {
            // 自動スクロールを一旦やめる
            setAutoScroll(false);

            Point tagtBaseLocation = new Point(0, 0);
            Size tagtBaseSize = new Size(0, 0);
            for (int i = 0; i < ctrlList.Length; i++)
            {
                if (tagtCtrl == ctrlList[i])
                {
                    tagtBaseLocation = ctrlBaseLocationList[i];
                    tagtBaseSize = ctrlBaseSizeList[i];
                    break;
                }
            }

            this.SuspendLayout();
            // パネル配置変更
            double r = (this.ClientSize.Width - SystemInformation.VerticalScrollBarWidth) / (double)tagtBaseSize.Width;
            tagtCtrl.Location = new Point(0, btnNew.Bottom); // ファイル操作ボタンの高さ分ずらす
            tagtCtrl.Size = new Size((int)((double)tagtBaseSize.Width * r), (int)((double)tagtBaseSize.Height * r));
            this.ResumeLayout();

            CadPanel.Visible = false;
            GroupBoxCadMode.Visible = false;
            btnSetting.Visible = false;
            linkLblEigenShow.Visible = false;
            FValuePanel.Visible = false;
            FValueLegendPanel.Visible = false;
            btnEigenFieldShow.Visible = false;
            foreach (Control workCtrl in ctrlList)
            {
                if (tagtCtrl != workCtrl)
                {
                    workCtrl.Visible = false;
                }
            }

            // 自動スクロールの情報を更新するため明示的に再設定しています。
            setAutoScroll(true);
        }

        /// <summary>
        /// フォームが閉じられる前のイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (IsCalculating)
            {
                MessageBox.Show("計算中は終了できません", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // イベントをキャンセルする
                e.Cancel = true;
                return;
            }
            if (IsLoading)
            {
                MessageBox.Show("読み込み中は終了できません", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                e.Cancel = true;
                return;
            }
            // 変更保存確認ダイアログを表示する
            DialogResult result = confirmSave();
            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                // Yes
                // 変更保存した、または変更箇所なし
            }
            else if (result == System.Windows.Forms.DialogResult.No)
            {
                // No
                // 変更を保存しなかった(破棄扱い)
            }
            else if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                // キャンセル
                e.Cancel = true;
                return;
            }
        }

        /// <summary>
        /// 変更保存確認
        /// </summary>
        /// <returns>DialogResult.Yes/No/Cancel 保存する必要のないときはDialogResult.Yes</returns>
        private DialogResult confirmSave()
        {
            DialogResult result = DialogResult.Cancel;
            // 現在編集中の図面があれば上書きする
            if (CadLgc.IsDirty)
            {
                result = MessageBox.Show("Cadデータが変更されています。Cadデータを保存しますか", "", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    //上書き保存
                    doSave(true);
                }
                else if (result == System.Windows.Forms.DialogResult.Cancel)
                {
                    // キャンセル
                }
            }
            else
            {
                // 変更なしの場合は、[Yes]と同じ動作にする
                result = DialogResult.Yes;
            }
            return result;
        }

        /// <summary>
        /// メインフォームが閉じられた後のイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            // 破棄処理
            CadLgc.Dispose();
            PostPro.Dispose();
        }

        /// <summary>
        /// フォームのリサイズイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Resize(object sender, EventArgs e)
        {
            fitPanelSizeToFrmSize();
        }

        /// <summary>
        /// Cad画面の描画ハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CadPanel_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                Graphics g = e.Graphics;
                if (CadLgc != null)
                {
                    CadLgc.CadPanelPaint(g);
                }
                //if (PostPro != null && isCalculating)
                //{
                //    // 計算実行中はメッシュ表示
                //    PostPro.DrawMesh(g, CadPanel);
                //}
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
            }
        }

        /// <summary>
        /// マウスクリックイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CadPanel_MouseClick(object sender, MouseEventArgs e)
        {
            if (IsCalculating)
            {
                return;
            }
            CadLgc.CadPanelMouseClick(e);
            // 元に戻す、やり直しボタンの操作可能フラグをセットアップ
            setupUndoRedoEnable();
        }


        /// <summary>
        /// マウス押下イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CadPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (IsCalculating)
            {
                return;
            }
            CadLgc.CadPanelMouseDown(e);
        }

        /// <summary>
        /// マウス移動イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CadPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsCalculating)
            {
                return;
            }
            CadLgc.CadPanelMouseMove(e);
        }

        /// <summary>
        /// マウスアップイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CadPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (IsCalculating)
            {
                return;
            }
            CadLgc.CadPanelMouseUp(e);
            // 元に戻す、やり直しボタンの操作可能フラグをセットアップ
            setupUndoRedoEnable();
        }

        /// <summary>
        /// Cadロジックの変更通知イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        private void CadLgc_Change(object sender, CadLogic.CadModeType prevCadMode)
        {
        }
        
        /// <summary>
        /// 周波数１箇所だけ計算する
        /// </summary>
        private void runAtOneFreq()
        {
            if (IsCalculating)
            {
                return;
            }
            if (IsLoading)
            {
                return;
            }
            if (FreqNo == -1)
            {
                FreqNo = (Solver.CalcFreqCnt + 1) / 2 + 1;
            }

            // 保存時に対象周波数がクリアされるので退避する
            int saveFreqNo = FreqNo;

            // Cadデータ保存＆Fem入力データ作成保存
            doSave(true);
            if (FemInputDatFilePath == "")
            {
                return;
            }

            // 対象周波数を再設定
            FreqNo = saveFreqNo;

            // 対象周波数１点について計算する
            doCalc(false, false); // allFlg: false appendFileFlg: false (ファイルを削除する)

        }

        /// <summary>
        /// フィールド値パネル描画イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FValuePanel_Paint(object sender, PaintEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("FValiePanel_Paint");
            try
            {
                Graphics g = e.Graphics;

                if (PostPro != null)
                {
                    DrawField(g);
                    if (PostPro != null && IsCalculating)
                    {
                        //見づらいので削除
                        // 計算実行中はメッシュ表示
                        //PostPro.DrawMesh(g, FValuePanel, true);
                    }
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
            }
        }

        /// <summary>
        /// フィールド描画
        ///   等高線図 or ベクトル表示
        /// </summary>
        /// <param name="g"></param>
        private void DrawField(Graphics g)
        {
            if (!PostPro.IsDataReady())
            {
                return;
            }
            //PostPro.DrawRotFieldEx(g, FValuePanel, FemElement.FieldDV.PoyntingXY);
            //PostPro.DrawRotFieldEx(g, FValuePanel, FemElement.FieldDV.RotXY);
            //PostPro.DrawFieldEx(g, FValuePanel, FemElement.FieldDV.Field, FemElement.ValueDV.Abs);
            //PostPro.DrawFieldEx(g, FValuePanel, FemElement.FieldDV.Field, FemElement.ValueDv.Real);
            if (FValuePanelIndex < FValuePanelFieldDV_ValueDVPairList.Length)
            {
                PostPro.IsCoarseFieldMesh = false;
                if (PostPro.ShowFieldDv == FemElement.FieldDV.RotXY || PostPro.ShowFieldDv == FemElement.FieldDV.PoyntingXY)
                {
                    // メッシュ表示
                    PostPro.DrawMesh(g, FValuePanel, true, true); // fitFlg: true transparent: true
                    // 回転系のベクトル表示
                    //PostPro.ShowValueDv = FemElement.ValueDV.Real;
                    PostPro.DrawRotField(g, FValuePanel);
                }
                else
                {
                    // 等高線図表示
                    PostPro.DrawField(g, FValuePanel);
                }
                // 媒質の境界を表示
                PostPro.DrawMediaB(g, FValuePanel, true);
            }
            else
            {
                // 描画を軽くするために粗いメッシュで描画
                PostPro.IsCoarseFieldMesh = true;
                Rectangle r;
                r = new Rectangle(0, 0, FValuePanel.Width / 2, FValuePanel.Height / 2);
                PostPro.DrawFieldEx(g, FValuePanel, r, FemElement.FieldDV.Field, FemElement.ValueDV.Abs);
                PostPro.DrawMediaB(g, FValuePanel, r, true);

                r = new Rectangle(FValuePanel.Width / 2, 0, FValuePanel.Width / 2, FValuePanel.Height / 2);
                PostPro.DrawFieldEx(g, FValuePanel, r, FemElement.FieldDV.Field, FemElement.ValueDV.Real);
                PostPro.DrawMediaB(g, FValuePanel, r, true);

                r = new Rectangle(0, FValuePanel.Height / 2, FValuePanel.Width / 2, FValuePanel.Height / 2);
                PostPro.DrawMesh(g, FValuePanel, r, true, true); // メッシュ表示
                PostPro.DrawRotFieldEx(g, FValuePanel, r, FemElement.FieldDV.RotXY);
                PostPro.DrawMediaB(g, FValuePanel, r, true);

                r = new Rectangle(FValuePanel.Width / 2, FValuePanel.Height / 2, FValuePanel.Width / 2, FValuePanel.Height / 2);
                PostPro.DrawMesh(g, FValuePanel, r, true, true); // メッシュ表示
                PostPro.DrawRotFieldEx(g, FValuePanel, r, FemElement.FieldDV.PoyntingXY);
                PostPro.DrawMediaB(g, FValuePanel, r, true);
            }
        }

        /// <summary>
        /// [計算開始]ボタン押下イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCalc_Click(object sender, EventArgs e)
        {
            if (SolverThread != null && SolverThread.IsAlive)
            {
                if (!Solver.IsCalcAborted)
                {
                    if (MessageBox.Show("計算をキャンセルしますか", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
                    {
                        // 2重チェック
                        if (SolverThread != null && SolverThread.IsAlive)
                        {
                            new Thread(new ThreadStart(delegate()
                                {
                                    Solver.IsCalcAborted = true;
                                    SolverThread.Join();
                                    SolverThread = null;
                                })).Start();
                        }
                    }
                }
                return;
            }

            // 自動計算モードを解除する
            PostPro.IsAutoCalc = false;

            // 計算範囲ダイアログを表示する
            CalcSettingFrm calcSettingFrm = new CalcSettingFrm(
                Solver.FirstNormalizedFreq, Solver.LastNormalizedFreq, Solver.CalcFreqCnt,
                Solver.WaveModeDv,
                Solver.ElemShapeDvToBeSet, Solver.ElemOrderToBeSet);
            DialogResult result = calcSettingFrm.ShowDialog();
            if (result != DialogResult.OK)
            {
                return;
            }
            // 要素形状、次数の設定をSolverに格納する
            Solver.ElemShapeDvToBeSet = calcSettingFrm.ElemShapeDv;
            Solver.ElemOrderToBeSet = calcSettingFrm.ElemOrder;

            // ソルバーに計算範囲画面で設定した情報をセットする
            Solver.SetNormalizedFreqRange(calcSettingFrm.NormalizedFreq1, calcSettingFrm.NormalizedFreq2, calcSettingFrm.CalcFreqCnt);
            Solver.WaveModeDv = calcSettingFrm.WaveModeDv;

            // Cadデータ保存＆Fem入力データ作成保存
            doSave(true);
            if (FemInputDatFilePath == "")
            {
                return;
            }

            // 解析機へ入力データを読み込む
            Solver.Load(FemInputDatFilePath);

            // 計算処理
            doCalc();
        }

        /// <summary>
        /// 計算処理
        /// </summary>
        /// <param name="allFlg">全周波数計算する?</param>
        private void doCalc(bool allFlg = true, bool appendFileFlg = false)
        {
            // ポストプロセッサの初期化
            PostPro.InitData(
                Solver,
                CadPanel,
                FValuePanel,
                FValueLegendPanel, labelFreqValue,
                SMatChart,
                BetaChart,
                EigenVecChart);
            // 計算モードのラベル表示
            setLabelCalcModeText(Solver.WaveModeDv);
            // ツールチップ表示更新
            setFValuePanelToolTip();
            if (EigenFValueFrm != null && EigenFValueFrm.Visible)
            {
                EigenFValueFrm.FreqNo = -1;
            }
            
            // 解析機のデータチェック
            bool chkResult = Solver.ChkInputData();
            if (!chkResult)
            {
                return;
            }

            if (allFlg)
            {
                // Cadモードを操作なしにする
                setupCadModeRadioButtons(CadLogic.CadModeType.None);
                CadLgc.CadMode = CadLogic.CadModeType.None;
            }

            // [計算開始]ボタンの無効化
            setCtrlEnable(false);
            btnCalc.Text = "計算キャンセル";

            /*
            // 選択中媒質を真空にする
            radioBtnMedia0.Checked = true;
            CadLgc.SelectedMediaIndex = CadLogic.VacumnMediaIndex;
            // 媒質選択グループボックスへ読み込み値を反映
            setupGroupBoxMedia();
            // 媒質選択ボタンの背景色とテキストを設定
            btnMediaSelect_SetColorAndText();
             */

            if (allFlg)
            {
                // 周波数インデックス初期化
                FreqNo = -1;
            }
            // 固有モード分布表示
            if (EigenFValueFrm != null && EigenFValueFrm.Visible)
            {
                EigenFValueFrm.FreqNo = -1;
            }

            SolverThread = new Thread(new ParameterizedThreadStart(solverThreadProc));
            SolverThread.Name = "solverThread";
            SolverThread.Start(new object[]{allFlg, appendFileFlg});
        }

        /// <summary>
        /// 計算スレッド関数
        /// </summary>
        /// <param name="param1"></param>
        private void solverThreadProc(object param1)
        {
            object[] paramList = (object[])param1;
            bool allFlg = (bool)paramList[0];
            bool appendFileFlg = (bool)paramList[1];

            // 各波長の結果出力時に呼ばれるコールバックの定義
            ParameterizedInvokeDelegate eachDoneCallback = new ParameterizedInvokeDelegate(delegate(Object[] args)
            {
                // ポストプロセッサへ結果読み込み(freqNo: -1は最後の結果を読み込み)
                PostPro.LoadOutput(FemOutputDatFilePath, -1);

                // 結果をグラフィック表示
                this.Invoke(new InvokeDelegate(delegate()
                    {
                        PostPro.SetOutputToGui(
                            FemOutputDatFilePath,
                            CadPanel,
                            FValuePanel,
                            FValueLegendPanel, labelFreqValue,
                            SMatChart,
                            BetaChart,
                            EigenVecChart,
                            true);
                        }));
                // 描画イベントを処理させる
                Application.DoEvents();

                // 固有モード分布表示
                if (EigenFValueFrm != null && EigenFValueFrm.Visible)
                {
                    this.Invoke(new InvokeDelegate(delegate()
                    {
                        EigenFValueFrm.FreqNo = -1;
                    }));
                    // 描画イベントを処理させる
                    Application.DoEvents();
                }

            });
            if (!allFlg)
            {
                // 対象周波数１点だけ計算する
                // 解析実行
                Solver.RunAtOneFreq(FemOutputDatFilePath, FreqNo, this, eachDoneCallback, appendFileFlg);
            }
            else
            {
                // 解析実行
                Solver.Run(FemOutputDatFilePath, this, eachDoneCallback);
            }
            // 解析終了したので[計算開始]ボタンを有効化
            this.Invoke(new InvokeDelegate(delegate()
                {
                    //[計算開始]ボタンを有効化
                    setCtrlEnable(true);
                    btnCalc.Text = "計算開始";

                    if (allFlg)
                    {
                        // 周波数インデックスを最後にセット
                        //BUGFIX
                        //周波数番号は1起点なので、件数 = 最後の番号となる
                        //計算失敗の場合、上記は成り立たない
                        int firstFreqNo;
                        int lastFreqNo;
                        int cnt = PostPro.GetCalculatedFreqCnt(FemOutputDatFilePath, out firstFreqNo, out lastFreqNo);
                        FreqNo = lastFreqNo;
                        // 周波数ボタンの有効・無効化
                        setupBtnFreqEnable();
                    }

                    // Cadパネル再描画（メッシュを消す）
                    //CadPanel.Invalidate();

                    // 等高線図再描画（メッシュを消す）
                    //FValuePanel.Invalidate();
                }));
        }

        /// <summary>
        /// 計算中のボタン非有効化
        /// </summary>
        /// <param name="enabled"></param>
        /// <returns></returns>
        private void setCtrlEnable(bool enabled)
        {
            //btnCalc.Enabled = enabled;
            // ポストプロセッサ系ボタン
            //BUGFIX 読み込み後に前の周波数ボタンが無効にならないバグ
            //btnPrevFreq.Enabled = enabled;
            //btnNextFreq.Enabled = enabled;
            if (enabled)
            {
                setupBtnFreqEnable();
            }
            else
            {
                btnPrevFreq.Enabled = enabled;
                btnNextFreq.Enabled = enabled;
            }
            // 編集系ボタン
            btnNew.Enabled = enabled;
            btnOpen.Enabled = enabled;
            btnSave.Enabled = enabled;
            btnSaveAs.Enabled = enabled;
            btnUndo.Enabled = enabled;
            btnRedo.Enabled = enabled;
            if (enabled)
            {
                setupUndoRedoEnable();
            }
            GroupBoxCadMode.Enabled = enabled;
            btnSetting.Enabled = enabled;
        }

        /// <summary>
        /// ラジオボタンから描画モードを取得する
        /// </summary>
        /// <returns></returns>
        private CadLogic.CadModeType getCadModeFromCadModeRadioButtons()
        {
            CadLogic.CadModeType cadMode = CadLogic.CadModeType.None;

            foreach (RadioButton rb in CadModeRadioButtons)
            {
                if (rb.Checked)
                {
                    cadMode = (CadLogic.CadModeType)rb.Tag;
                    break;
                }
            }
            return cadMode;
        }
        /// <summary>
        /// 描画モードラジオボタンのチェック状態が変更されたときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CadModeRadionBtn_CheckedChangedProc(object sender, EventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            if (!rb.Checked)
            {
                // OFFのイベントは無視する(必ず対でONのイベントがあるので)
                return;
            }
            // 選択されたモードを取得
            CadLogic.CadModeType nextCadMode = getCadModeFromCadModeRadioButtons();
            // 変更されたCadモードをセットする
            CadLgc.CadMode = nextCadMode;

            // セルの種類コンボボックスの表示・非表示
            imgcbxCellType.Visible = (CadLgc.CadMode == CadLogicBase.CadModeType.Area);
        }
        /// <summary>
        /// 「描画モード解除」ラジオボタンチェック状態変更イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void radioBtnNone_CheckedChanged(object sender, EventArgs e)
        {
            CadModeRadionBtn_CheckedChangedProc(sender, e);
        }
        /// <summary>
        /// [位置移動]ラジオボタンチェック状態変更イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void radioBtnLocation_CheckedChanged(object sender, EventArgs e)
        {
            CadModeRadionBtn_CheckedChangedProc(sender, e);
        }
        /// <summary>
        /// 「マス目」ラジオボタンチェック状態変更イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void radioBtnArea_CheckedChanged(object sender, EventArgs e)
        {
            CadModeRadionBtn_CheckedChangedProc(sender, e);
        }
        /// <summary>
        /// 「ポート境界」ラジオボタンチェック状態変更イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void radioBtnPort_CheckedChanged(object sender, EventArgs e)
        {
            CadModeRadionBtn_CheckedChangedProc(sender, e);
        }
        /// <summary>
        /// 「入射ポート選択」ラジオボタンチェック状態変更イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void radioBtnIncidentPort_CheckedChanged(object sender, EventArgs e)
        {
            CadModeRadionBtn_CheckedChangedProc(sender, e);
        }
        /// <summary>
        /// 「ポート番号振り」ラジオボタンチェック状態変更イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void radioBtnPortNumbering_CheckedChanged(object sender, EventArgs e)
        {
            CadModeRadionBtn_CheckedChangedProc(sender, e);
        }
        /// <summary>
        /// 「消しゴム」ラジオボタンチェック状態変更イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void radioBtnErase_CheckedChanged(object sender, EventArgs e)
        {
            CadModeRadionBtn_CheckedChangedProc(sender, e);
        }

        /// <summary>
        /// エリア描画モードイメージコンボボックスの選択アイテムインデックス変更イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void imgcbxCadModeArea_SelectedIndexChanged(object sender, EventArgs e)
        {
            CellTypeStruct selCellTypeStruct = (CellTypeStruct)imgcbxCellType.SelectedItem;
            CadLogic.CellType selCellType = selCellTypeStruct.CellTypeVal;

            CadLgc.SelectedCellType = selCellType;
        }

        /// <summary>
        /// [前の周波数]ボタン押下イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPrevFreq_Click(object sender, EventArgs e)
        {
            if (FreqNo == -1)
            {
                return;
            }
            int firstFreqNo = -1;
            int lastFreqNo = -1;
            int cnt = PostPro.GetCalculatedFreqCnt(FemOutputDatFilePath, out firstFreqNo, out lastFreqNo);
            if (FreqNo <= firstFreqNo)
            {
                //return;
                // 繰り返し
                FreqNo = lastFreqNo + 1; // 後の処理でマイナス1される
            }
            // 前の周波数
            FreqNo--;
            // 周波数ボタンの有効・無効化
            setupBtnFreqEnable();

            // ポストプロセッサへ結果読み込み
            bool ret = PostPro.LoadOutput(FemOutputDatFilePath, FreqNo);
            if (ret)
            {
                // 結果をグラフィック表示 (周波数特性のデータは追加しない. 等高線図と固有ベクトル分布図のみ更新)
                bool addFlg = false;
                PostPro.SetOutputToGui(
                    FemOutputDatFilePath,
                    CadPanel,
                    FValuePanel,
                    FValueLegendPanel, labelFreqValue,
                    SMatChart,
                    BetaChart,
                    EigenVecChart,
                    addFlg);
            }
            else
            {
                // 自動計算モード対応
                // PostProの出力データだけ初期化する
                PostPro.InitOutputData(
                    CadPanel,
                    FValuePanel,
                    FValueLegendPanel, labelFreqValue,
                    SMatChart,
                    BetaChart,
                    EigenVecChart);
                // PostProの周波数を変更する
                PostPro.SetNormalizedFrequency(Solver.GetNormalizedFreqFromFreqNo(FreqNo));
                // 等高線図の凡例を更新する
                PostPro.UpdateFValueLegend(FValueLegendPanel, labelFreqValue);
            }
            // 固有モード分布表示
            if (EigenFValueFrm != null && EigenFValueFrm.Visible)
            {
                EigenFValueFrm.FreqNo = FreqNo;
            }
        }

        /// <summary>
        /// [次の周波数]ボタン押下イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnNextFreq_Click(object sender, EventArgs e)
        {
            if (FreqNo == -1)
            {
                return;
            }
            int firstFreqNo;
            int lastFreqNo;
            int cnt = PostPro.GetCalculatedFreqCnt(FemOutputDatFilePath, out firstFreqNo, out lastFreqNo);
            if (FreqNo >= lastFreqNo)
            {
                //return;
                // 繰り返し
                FreqNo = firstFreqNo - 1; // 後の処理でプラス１される
            }
            // 次の周波数
            FreqNo++;
                // 周波数ボタンの有効・無効化
                setupBtnFreqEnable();

            // ポストプロセッサへ結果読み込み
            bool ret = PostPro.LoadOutput(FemOutputDatFilePath, FreqNo);
            if (ret)
            {
                // 結果をグラフィック表示 (周波数特性のデータは追加しない. 等高線図と固有ベクトル分布図のみ更新)
                bool addFlg = false;
                PostPro.SetOutputToGui(
                    FemOutputDatFilePath,
                    CadPanel,
                    FValuePanel,
                    FValueLegendPanel, labelFreqValue,
                    SMatChart,
                    BetaChart,
                    EigenVecChart,
                    addFlg);
            }
            else
            {
                // 自動計算対応
                // PostProの出力データだけ初期化する
                PostPro.InitOutputData(
                    CadPanel,
                    FValuePanel,
                    FValueLegendPanel, labelFreqValue,
                    SMatChart,
                    BetaChart,
                    EigenVecChart);
                // PostProの周波数を変更する
                PostPro.SetNormalizedFrequency(Solver.GetNormalizedFreqFromFreqNo(FreqNo));
                // 等高線図の凡例を更新する
                PostPro.UpdateFValueLegend(FValueLegendPanel, labelFreqValue);
            }
            // 固有モード分布表示
            if (EigenFValueFrm != null && EigenFValueFrm.Visible)
            {
                EigenFValueFrm.FreqNo = FreqNo;
            }
        }

        /// <summary>
        /// 周波数ボタンの有効・無効化
        /// </summary>
        private void setupBtnFreqEnable()
        {
            int firstFreqNo;
            int lastFreqNo;
            int cnt = PostPro.GetCalculatedFreqCnt(FemOutputDatFilePath, out firstFreqNo, out lastFreqNo);
            //btnPrevFreq.Enabled = (FreqNo > firstFreqNo && FreqNo <= lastFreqNo);
            //btnNextFreq.Enabled = (FreqNo >= firstFreqNo && FreqNo <lastFreqNo);
            // 繰り返しの場合、始点終点もOK
            btnPrevFreq.Enabled = (FreqNo >= firstFreqNo && FreqNo <= lastFreqNo);
            btnNextFreq.Enabled = (FreqNo >= firstFreqNo && FreqNo <= lastFreqNo);
        }

        /// <summary>
        /// フォーム初期表示イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Shown(object sender, EventArgs e)
        {
            FrmNormalSize = this.Size;
            //System.Diagnostics.Debug.WriteLine("FrmNormalSize:{0},{1}", FrmNormalSize.Width, FrmNormalSize.Height);

            // パネルを再配置
            fitPanelSizeToFrmSize();
        }

        /// <summary>
        /// キー押下イベントハンドラ
        ///   フォームのKeyPreviewをtrueにするとすべてのキーイベントをフォームが受け取れます
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.O)
            {
                // ファイルを開く
                doOpen();
                // 子コントロールへイベントを伝搬させないようにする
                e.Handled = true;
            }
            if (e.Control && e.KeyCode == Keys.S)
            {
                // 上書き保存
                doSave(true);
                // 子コントロールへイベントを伝搬させないようにする
                e.Handled = true;
            }
            if (e.Control && e.KeyCode == Keys.Z)
            {
                // 元に戻す
                bool executed = doUndo();
                if (executed)
                {
                    // 子コントロールへイベントを伝搬させないようにする
                    e.Handled = true;
                }
            }
            if (e.Control && e.KeyCode == Keys.Y)
            {
                // やり直し
                bool executed = doRedo();
                if (executed)
                {
                    // 子コントロールへイベントを伝搬させないようにする
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// [新規作成]ボタンクリックイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnNew_Click(object sender, EventArgs e)
        {
            // 変更保存確認ダイアログを表示する
            DialogResult result = confirmSave();
            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                // Yes
                // 変更保存した、または変更箇所なし
            }
            else if (result == System.Windows.Forms.DialogResult.No)
            {
                // No
                // 変更を保存しなかった(破棄扱い)
            }
            else if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                // キャンセル
                return;
            }

            //////////////////////////////
            // 新規作成処理
            /////////////////////////////
            setupFilenames("");
            setFrmTitle();
            
            // GUIの初期化
            resetGUI();

            // 固有モード分布再表示(データファイル名の差し替え)
            showEigenFValueFrm();
        }

        /// <summary>
        /// [ファイルを開く]ボタンクリックイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOpen_Click(object sender, EventArgs e)
        {
            // 変更保存確認ダイアログを表示する
            DialogResult result = confirmSave();
            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                // Yes
                // 変更保存した、または変更箇所なし
            }
            else if (result == System.Windows.Forms.DialogResult.No)
            {
                // No
                // 変更を保存しなかった(破棄扱い)
            }
            else if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                // キャンセル
                return;
            }

            //////////////////////////////
            // ファイルを開く
            //////////////////////////////
            doOpen();
        }

        /// <summary>
        /// ファイルを開く処理
        /// </summary>
        private void doOpen()
        {
            openFileDialog1.InitialDirectory = Application.UserAppDataPath;
            openFileDialog1.FileName = "";
            if (CadDatFilePath.Length > 0)
            {
                openFileDialog1.InitialDirectory = Path.GetDirectoryName(CadDatFilePath);
                openFileDialog1.FileName = Path.GetFileName(CadDatFilePath);
            }
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                // ファイル名の格納
                string path = openFileDialog1.FileName;
                setupFilenames(path);

                // タイトル変更
                setFrmTitle();
                // 読み込み処理
                loadFromFile();
            }
        }

        /// <summary>
        /// ファイルを開くダイアログのファイル名を元にCad, Fem入出力ファイル名を決定する
        /// </summary>
        /// <param name="path"></param>
        private void setupFilenames(string path)
        {
            if (path == "")
            {
                CadDatFilePath = "";
                FemInputDatFilePath = "";
                FemOutputDatFilePath = "";
            }
            else
            {
                string basename = MainFrm.GetFilePathWithoutExt(path);
                CadDatFilePath = basename + Constants.CadExt;
                FemInputDatFilePath = basename + Constants.FemInputExt;
                FemOutputDatFilePath = basename + Constants.FemOutputExt;
            }
        }

        /// <summary>
        /// 計算モードのラベル表示
        /// </summary>
        /// <param name="waveModeDv"></param>
        private void setLabelCalcModeText(FemSolver.WaveModeDV waveModeDv)
        {
            string text;
            text =((waveModeDv == FemSolver.WaveModeDV.TM) ? "TM" : "TE");

            labelCalcMode.Text = text;
        }

        /// <summary>
        /// GUI初期化
        /// </summary>
        private void resetGUI()
        {
            // Cadデータの初期化
            CadLgc.InitData();
            // 元に戻す、やり直しボタンの操作可能フラグをセットアップ
            setupUndoRedoEnable();

            // Cadモードを操作なしにする
            setupCadModeRadioButtons(CadLogic.CadModeType.None);
            /*
            foreach (RadioButton rb in CadModeRadioButtons)
            {
                rb.Checked = false;
            }
            radioBtnNone.Checked = true;
             */
            CadLgc.CadMode = CadLogic.CadModeType.None;
            /*
            // 選択中媒質を真空にする
            radioBtnMedia0.Checked = true;
            CadLgc.SelectedMediaIndex = CadLogic.VacumnMediaIndex;
            // 媒質選択グループボックスへ読み込み値を反映
            setupGroupBoxMedia();
            // 媒質選択ボタンの背景色とテキストを設定
            btnMediaSelect_SetColorAndText();
             */
            // 方眼線描画
            CadPanel.Invalidate();

            // 解析機の入力データ初期化
            Solver.InitData();
            // ポストプロセッサの入力データ初期化
            PostPro.InitData(
                Solver,
                CadPanel,
                FValuePanel,
                FValueLegendPanel, labelFreqValue,
                SMatChart,
                BetaChart,
                EigenVecChart
                );

            // 計算モードのラベル表示
            setLabelCalcModeText(Solver.WaveModeDv);
            // ツールチップ表示更新
            setFValuePanelToolTip();

            // 周波数インデックス初期化
            FreqNo = -1;
            // 周波数ボタンの有効・無効化
            setupBtnFreqEnable();
        }

        /// <summary>
        /// ファイル読み込み処理
        /// </summary>
        private void loadFromFile()
        {
            // ロード中は前のパネル、次のパネルのボタンを非表示にする
            hidePrevNextFValuePanelBtn();
            // ロード中は操作させない
            IsLoading = true;
            //this.Enabled = false;
            setCtrlEnable(false);
            btnCalc.Enabled = false;
            IsLoadCancelled = false;
            btnLoadCancel.Visible = true;

            // 自動計算モードを解除する
            PostPro.IsAutoCalc = false;

            resetGUI();

            if (EigenFValueFrm != null && EigenFValueFrm.Visible)
            {
                // 固有モード分布フォームが表示されているときは、再表示する(データファイル名を更新する為)
                showEigenFValueFrm();
            }

            // Cadデータの読み込み
            CadLgc.DeserializeCadData(CadDatFilePath);
            // 元に戻す、やり直しボタンの操作可能フラグをセットアップ
            setupUndoRedoEnable();
            // 方眼線描画
            CadPanel.Invalidate();

            // 解析機へ入力データを読み込む
            Solver.Load(FemInputDatFilePath);
            // ポストプロセッサの入力データ初期化
            PostPro.InitData(
                Solver,
                CadPanel,
                FValuePanel,
                FValueLegendPanel, labelFreqValue,
                SMatChart,
                BetaChart,
                EigenVecChart
                );
            // 計算モードのラベル表示
            setLabelCalcModeText(Solver.WaveModeDv);
            // ツールチップ表示更新
            setFValuePanelToolTip();
            //描画の途中経過を表示
            Application.DoEvents();

            if (File.Exists(FemOutputDatFilePath))
            {
                bool ret;

                // 周波数インデックス初期化
                FreqNo = -1;
                // 周波数ボタンの有効・無効化
                setupBtnFreqEnable();

                // 周波数特性グラフの表示
                int loadcnt = 0; // 計算失敗を考慮
                int firstFreqNo;
                int lastFreqNo;
                int cnt = PostPro.GetCalculatedFreqCnt(FemOutputDatFilePath, out firstFreqNo, out lastFreqNo);
                double firstNormalizedFreq = Solver.FirstNormalizedFreq;
                double lastNormalizedFreq = Solver.LastNormalizedFreq;
                int calcFreqCnt = Solver.CalcFreqCnt;
                if (calcFreqCnt == 0)
                {
                    firstNormalizedFreq = Constants.DefNormalizedFreqRange[0];
                    lastNormalizedFreq = Constants.DefNormalizedFreqRange[1];
                    calcFreqCnt = Constants.DefCalcFreqencyPointCount;
                }
                double freqDelta = (Solver.LastNormalizedFreq - Solver.FirstNormalizedFreq) / calcFreqCnt;
                for (int freqIndex = firstFreqNo - 1; freqIndex <= lastFreqNo - 1; freqIndex++)
                {
                    int freqNo = freqIndex + 1;
                    // ポストプロセッサへ結果読み込み
                    ret = PostPro.LoadOutput(FemOutputDatFilePath, freqNo);
                    if (!ret)
                    {
                        continue;  // 計算失敗を考慮
                    }
                    loadcnt++; // 計算失敗を考慮

                    double normalizedFreq = PostPro.GetNormalizedFrequency();
                    if (loadcnt == 1)
                    {
                        firstNormalizedFreq = normalizedFreq;
                    }
                    else if (loadcnt == 2)
                    {
                        freqDelta = (normalizedFreq - firstNormalizedFreq) / (freqNo - firstFreqNo);
                    }
                    lastNormalizedFreq = normalizedFreq;

                    // グラフィック表示
                    PostPro.SetOutputToGui(
                        FemOutputDatFilePath,
                        CadPanel,
                        FValuePanel,
                        FValueLegendPanel, labelFreqValue,
                        SMatChart,
                        BetaChart,
                        EigenVecChart,
                        true,
                        false /* isUpdateFValuePanel*/
                        );

                    //描画の途中経過を表示
                    Application.DoEvents();

                    // 固有モード分布表示
                    if (EigenFValueFrm != null && EigenFValueFrm.Visible)
                    {
                        EigenFValueFrm.FreqNo = freqNo;
                        Application.DoEvents();
                    }

                    if (IsLoadCancelled)
                    {
                        break;
                    }
                }

                // 周波数
                //FreqNo = 1;
                FreqNo = firstFreqNo;
                // 周波数ボタンの有効・無効化
                setupBtnFreqEnable();

                // ポストプロセッサへ結果読み込み
                ret = PostPro.LoadOutput(FemOutputDatFilePath, FreqNo);
                if (ret)
                {
                    // グラフィック表示(等高線図と固有ベクトル表示のみ更新)
                    PostPro.SetOutputToGui(
                        FemOutputDatFilePath,
                        CadPanel,
                        FValuePanel,
                        FValueLegendPanel, labelFreqValue,
                        SMatChart,
                        BetaChart,
                        EigenVecChart,
                        false);
                }
                else
                {
                    // 自動計算対応
                    // PostProの出力データだけ初期化する
                    PostPro.InitOutputData(
                        CadPanel,
                        FValuePanel,
                        FValueLegendPanel, labelFreqValue,
                        SMatChart,
                        BetaChart,
                        EigenVecChart);
                    // PostProの周波数を変更する
                    PostPro.SetNormalizedFrequency(Solver.GetNormalizedFreqFromFreqNo(FreqNo));
                    // 等高線図の凡例を更新する
                    PostPro.UpdateFValueLegend(FValueLegendPanel, labelFreqValue);
                }
            }
            // 固有モード分布表示
            if (EigenFValueFrm != null && EigenFValueFrm.Visible)
            {
                EigenFValueFrm.FreqNo = FreqNo;
            }

            // ロードが完了したので操作可にする
            //this.Enabled = true;
            setCtrlEnable(true);
            btnCalc.Enabled = true;
            IsLoadCancelled = false;
            btnLoadCancel.Visible = false;
            IsLoading = false;

        }

        /// <summary>
        /// 拡張子を抜いたパスを取得する
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetFilePathWithoutExt(string path)
        {
            return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(path);
        }

        /// <summary>
        /// [上書き保存]ボタンクリックイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSave_Click(object sender, EventArgs e)
        {
            doSave(true);
        }

        /// <summary>
        /// [名前を付けて保存]ボタンクリックイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSaveAs_Click(object sender, EventArgs e)
        {
            doSave(false);
        }

        /// <summary>
        /// ファイル保存処理
        /// </summary>
        /// <param name="overwriteFlg">上書きフラグ</param>
        /// <returns>通常保存は成功する(true)。失敗(false)になるのは名前を付けて保存ダイアログでキャンセルした場合のみ</returns>
        private bool doSave(bool overwriteFlg)
        {
            if (CadDatFilePath.Length == 0 || !overwriteFlg)
            {
                // 名前を付けて保存
                saveFileDialog1.InitialDirectory = Application.UserAppDataPath;
                saveFileDialog1.FileName = "";
                if (CadDatFilePath.Length > 0)
                {
                    saveFileDialog1.InitialDirectory = Path.GetDirectoryName(CadDatFilePath);
                    saveFileDialog1.FileName = Path.GetFileName(CadDatFilePath);
                }
                DialogResult result = saveFileDialog1.ShowDialog();
                if (result == DialogResult.OK)
                {
                    // ファイル名の格納
                    string path = saveFileDialog1.FileName;
                    setupFilenames(path);

                    // フォームタイトル変更
                    setFrmTitle();
                }
                else
                {
                    return false;
                }
            }
            // ファイル書き込み処理
            saveToFile();
            return true;
        }
        
        /// <summary>
        /// ファイル書き込み処理
        ///   Cadデータファイル作成、Fem入力データファイル作成
        /// </summary>
        private void saveToFile()
        {
            // 計算範囲の退避
            double firstNormalizedFreq = Solver.FirstNormalizedFreq;
            double lastNormalizedFreq = Solver.LastNormalizedFreq;
            int calcFreqCnt = Solver.CalcFreqCnt;
            // 波のモード区分の退避
            FemSolver.WaveModeDV waveModeDv = Solver.WaveModeDv;
            // 要素形状、補間次数の退避
            Constants.FemElementShapeDV elemShapeDv = Solver.ElemShapeDvToBeSet;
            int elemOrder = Solver.ElemOrderToBeSet;

            if (calcFreqCnt == 0)
            {
                firstNormalizedFreq = Constants.DefNormalizedFreqRange[0];
                lastNormalizedFreq = Constants.DefNormalizedFreqRange[1];
                calcFreqCnt = Constants.DefCalcFreqencyPointCount;
            }

            // Fem入出力データの削除
            removeAllFemDatFile();

            // 解析機の入力データ初期化
            Solver.InitData();
            
            // 周波数インデックス初期化
            FreqNo = -1;
            // 周波数ボタンの有効・無効化
            setupBtnFreqEnable();

            // Cadデータの書き込み
            CadLgc.SerializeCadData(CadDatFilePath);
            // FEM入力データの作成
            CadLgc.MkFemInputData(
                FemInputDatFilePath,
                elemShapeDv, elemOrder,
                firstNormalizedFreq, lastNormalizedFreq, calcFreqCnt,
                waveModeDv);

            // 解析機へ入力データを読み込む
            Solver.Load(FemInputDatFilePath);
            // ポストプロセッサの入力データ初期化
            PostPro.InitData(
                Solver,
                CadPanel,
                FValuePanel,
                FValueLegendPanel, labelFreqValue,
                SMatChart,
                BetaChart,
                EigenVecChart
                );
            // 固有モード分布表示
            if (EigenFValueFrm != null && EigenFValueFrm.Visible)
            {
                EigenFValueFrm.FreqNo = FreqNo;
            }
            // 計算モードのラベル表示
            setLabelCalcModeText(Solver.WaveModeDv);
            // ツールチップ表示更新
            setFValuePanelToolTip();

            // 元に戻す、やり直しボタンの操作可能フラグをセットアップ
            setupUndoRedoEnable();
        }

        /// <summary>
        /// Fem入出力データの削除
        /// </summary>
        private void removeAllFemDatFile()
        {
            if (File.Exists(FemInputDatFilePath))
            {
                File.Delete(FemInputDatFilePath);
            }
            string basename = MainFrm.GetFilePathWithoutExt(FemOutputDatFilePath);
            string outfilename = basename + Constants.FemOutputExt;
            string indexfilename = outfilename + Constants.FemOutputIndexExt;
            System.Diagnostics.Debug.Assert(outfilename == FemOutputDatFilePath);
            if (File.Exists(outfilename))
            {
                File.Delete(outfilename);
            }
            if (File.Exists(indexfilename))
            {
                File.Delete(indexfilename);
            }
            // 周期構造導波路固有モード出力ファイル
            string outPeriodicFilename = FemOutputPeriodicDatFile.GetOutputPeriodicDatFilename(outfilename);
            string outPeriodicIndexFilename = outPeriodicFilename + Constants.FemOutputIndexExt;
            if (File.Exists(outPeriodicFilename))
            {
                File.Delete(outPeriodicFilename);
            }
            if (File.Exists(outPeriodicIndexFilename))
            {
                File.Delete(outPeriodicIndexFilename);
            }

        }

        /// <summary>
        /// フォームのタイトル(ウィンドウテキスト)を設定する
        /// </summary>
        private void setFrmTitle()
        {
            string fn = Path.GetFileName(CadDatFilePath);
            if (fn.Length == 0)
            {
                fn = "(無題)";
            }
            this.Text = fn + " - " + TitleBaseName;
        }

        /// <summary>
        /// [元に戻す]ボタンクリックイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUndo_Click(object sender, EventArgs e)
        {
            doUndo();
        }

        /// <summary>
        /// [やり直し]ボタンクリックイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRedo_Click(object sender, EventArgs e)
        {
            doRedo();
        }

        /// <summary>
        /// 元に戻す、やり直しボタンの操作可能フラグを設定する
        /// </summary>
        private void setupUndoRedoEnable()
        {
            btnUndo.Enabled = CadLgc.CanUndo();
            btnRedo.Enabled = CadLgc.CanRedo();
        }

        /// <summary>
        /// Cadパネルダブルクリックイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CadPanel_DoubleClick(object sender, EventArgs e)
        {
            if (CadLgc.CadMode != CadLogic.CadModeType.None)
            {
                MessageBox.Show("実行するには描画モードを解除してください", MaximizedControl == CadPanel? "元のサイズに戻す" : "最大化");
                return;
            }
            flipMaximizedControl(CadPanel);
        }

        /// <summary>
        /// 最大化コントロールをフリップ(セット済みならクリア、未セットならセット)
        /// </summary>
        /// <param name="tagtCtrl"></param>
        private void flipMaximizedControl(Control tagtCtrl)
        {
            if (MaximizedControl != null)
            {
                if (MaximizedControl == tagtCtrl)
                {
                    MaximizedControl = null;
                }
            }
            else
            {
                MaximizedControl = tagtCtrl;
            }
            fitPanelSizeToFrmSize();
        }

        /// <summary>
        /// フィールド値パネルダブルクリックイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FValuePanel_DoubleClick(object sender, EventArgs e)
        {
            flipMaximizedControl(FValuePanel);
        }

        /// <summary>
        /// S行列チャートダブルクリックイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SMatChart_DoubleClick(object sender, EventArgs e)
        {
            flipMaximizedControl(SMatChart);
        }

        /// <summary>
        /// 伝搬定数チャートダブルクリックイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BetaChart_DoubleClick(object sender, EventArgs e)
        {
            flipMaximizedControl(BetaChart);
        }

        /// <summary>
        /// 固有モードチャートダブルクリックイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EigenVecChart_DoubleClick(object sender, EventArgs e)
        {
            flipMaximizedControl(EigenVecChart);
        }

        /// <summary>
        /// 「元に戻す」処理
        /// </summary>
        /// <returns></returns>
        private bool doUndo()
        {
            bool executed = false;
            if (CadLgc.CanUndo() && (MaximizedControl == null || MaximizedControl == CadPanel) && !IsCalculating)
            {
                // 元に戻す
                CadLgc.Undo();
                //CadPanel.Invalidate(); // CadLogic内で処理される
                executed = true;
            }
            if (executed)
            {
                setupCadModeRadioButtons(CadLgc.CadMode);
                setupUndoRedoEnable();
            }
            return executed;
        }

        /// <summary>
        /// 「やり直し」処理
        /// </summary>
        /// <returns></returns>
        private bool doRedo()
        {
            bool executed = false;
            if (CadLgc.CanRedo() && (MaximizedControl == null || MaximizedControl == CadPanel) && !IsCalculating)
            {
                // やり直し
                CadLgc.Redo();
                //CadPanel.Invalidate(); // CadLogic内で処理される
                // Cadモードが変更される可能性があるので、CadLgcのCadModeをGuiに反映させる

                // 子コントロールへイベントを伝搬させないようにする
                executed = true;
            }
            if (executed)
            {
                setupCadModeRadioButtons(CadLgc.CadMode);
                setupUndoRedoEnable();
            }
            return executed;
        }

        /// <summary>
        /// [固有モード表示]リンクラベルリッククリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void linkLblEigenShow_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (MaximizedControl != null)
            {
                // パネル最大化中は表示切替しない
                return;
            }

            // 固有モード表示フラグを切り替える
            EigenShowFlg = !EigenShowFlg;

            // リンクのテキスト変更
            linkLblEigenShow.Text = EigenShowFlg ? "隠す" : "固有モードを見る";

            setupFrmSizeForEigenShow();
        }

        /// <summary>
        /// 固有モードの表示/非表示にあったフォームのサイズを設定する
        /// </summary>
        private void setupFrmSizeForEigenShow()
        {
            if (MaximizedControl != null)
            {
                // パネル最大化中は配置変更しなくてよい
                return;
            }
            int dY = 0;
            if (EigenShowFlg)
            {
                // ウィンドウを広げる
                // ウィンドウの高さを伝搬定数チャートの右下Y座標の高さにする
                dY = BetaChart.Bottom - this.ClientSize.Height;
                // 微調整
                if (PrevWindowState == FormWindowState.Maximized && this.WindowState == FormWindowState.Normal)
                {
                    // 最大化→最小化のときタイトルバーの高さ分、フォームの高さが設定したい高さより大きい（目視で確認）
                    //dY -= SystemInformation.CaptionHeight;
                    dY -= SystemInformation.CaptionButtonSize.Height - 5; // 少し大きすぎるので調整
                }
            }
            else
            {
                // ウィンドウを折りたたむ
                dY = BetaChart.Top - this.ClientSize.Height;
                // 微調整
                if (PrevWindowState == FormWindowState.Maximized && this.WindowState == FormWindowState.Normal)
                {
                    // 最大化→最小化のときタイトルバーの高さ分、フォームの高さが設定したい高さより大きい（目視で確認）
                    //dY -= SystemInformation.CaptionHeight;
                    dY -= SystemInformation.CaptionButtonSize.Height - 5; // 少し大きすぎるので調整
                }
            }
            //this.Size += new Size(0, dY);
            Size sizeToSet = this.Size + new Size(0, dY);
            //System.Diagnostics.Debug.WriteLine("■same? Size:{0},{1}", this.Size.Width, this.Size.Height);
            //System.Diagnostics.Debug.WriteLine("■add Size:{0},{1}", 0, dY);
            //System.Diagnostics.Debug.WriteLine("■sizeToSet:{0},{1}", sizeToSet.Width, sizeToSet.Height);
            this.Size = sizeToSet;  // ここでセットされない or セットした後書き換わっている
            //System.Diagnostics.Debug.WriteLine("■Set! ret Size:{0},{1}", this.Size.Width, this.Size.Height);
            if (!this.Size.Equals(sizeToSet))
            {
                //System.Diagnostics.Debug.WriteLine("■Set! replaced? ret Size:{0},{1}", this.Size.Width, this.Size.Height);
                if (this.WindowState == FormWindowState.Normal)
                {
                    // 追記: Form1_SizeChangedの処理を遅延実行することで、ここに来ることはなくなった
                    // Note: 最大化状態では、常にセットに失敗する(retryMaxまで到達)
                    //       最大化状態→元のサイズに戻すの過程では、何度か実行するとセットに成功する(こちらが必要な処理なので、上記ウィンドウステートの条件を追加した)
                    int retryCnt = 0;
                    int retryMax = 5;
                    while (!this.Size.Equals(sizeToSet))
                    {
                        retryCnt++;
                        this.Size = sizeToSet;  // 再度セットする
                        System.Diagnostics.Debug.WriteLine("■Set! ret Size:{0},{1}  retry = {2}", this.Size.Width, this.Size.Height, retryCnt);
                        if (retryCnt >= retryMax)
                        {
                            break;
                        }
                    }
                }
            }
            fitPanelSizeToFrmSize();
        }

        /// <summary>
        /// フォームのサイズが変更された
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            FormWindowState windowState = this.WindowState;

            //System.Diagnostics.Debug.WriteLine("■■■Size:{0},{1}", this.Size.Width, this.Size.Height);
            if (windowState == FormWindowState.Normal)
            {
                if (PrevWindowState == FormWindowState.Maximized)
                {
                    // 最大化状態→元のサイズに戻すの場合

                    // スレッド化により、スレッドが終了するまでPrevWindowStateとFrmNormalSizeが更新されないようにイベントハンドラを削除する
                    this.SizeChanged -= Form1_SizeChanged;
                    this.Visible = false;
                    // スレッド化して処理を遅延実行する
                    int delayMsec = 0;
                    new Thread(new ThreadStart(delegate()
                        {
                            Thread.Sleep(delayMsec);
                            this.Invoke(new InvokeDelegate(delegate()
                                {
                                    // 最大化前に記録したフォームのサイズを復元する
                                    this.Size = FrmNormalSize;
                                    //System.Diagnostics.Debug.WriteLine("■■Set! ret Size:{0},{1}", this.Size.Width, this.Size.Height);
                                    // パネル再配置
                                    fitPanelSizeToFrmSize();
                                    //System.Diagnostics.Debug.WriteLine("■■same? ret Size:{0},{1}", this.Size.Width, this.Size.Height);
                                    if (MaximizedControl == null)
                                    {
                                        // 固有モードの表示/非表示にあったフォームのサイズを設定する
                                        setupFrmSizeForEigenShow();
                                        //System.Diagnostics.Debug.WriteLine("■■changed!!!! ret Size:{0},{1}", this.Size.Width, this.Size.Height);
                                    }
                                    // サイズ変更イベントハンドラを元に戻す
                                    this.SizeChanged += Form1_SizeChanged;
                                    this.Visible = true;
                                }), null);
                        })).Start();

                }
                else
                {
                    // 通常のサイズの場合

                    // サイズを記録する
                    FrmNormalSize = this.Size;
                    //System.Diagnostics.Debug.WriteLine("■■FrmNormalSize:{0},{1}", FrmNormalSize.Width, FrmNormalSize.Height);
                }
            }
            if (PrevWindowState != windowState)
            {
                PrevWindowState = windowState;
            }
        }

        /// <summary>
        /// メッシュ表示ダイアログを表示する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void linkLabelMeshShow_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // ファイルを保存しないとメッシュを作成できないのでファイルを保存する
            if (IsCalculating)
            {
                // 計算実行中は保存しない（既にメッシュは確定している)
            }
            else if (!CadLgc.IsDirty)
            {
                // 図面が変更されていない
                // 保存しない
            }
            else
            {
                DialogResult result = confirmSave();
                if (result != DialogResult.Yes)
                {
                    return;
                }
                bool ret = doSave(true);
                if (!ret)
                {
                    MessageBox.Show("メッシュを表示するにはファイルにCadデータを保存する必要があります", "メッシュ表示キャンセル", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            //  メッシュ表示ダイアログを表示
            if (MeshView != null)
            {
                if (!MeshView.IsDisposed)
                {
                    MeshView.Close();
                    MeshView.Dispose();
                    MeshView = null;
                }
            }
            MeshView = new MeshViewFrm(Solver.ElemShapeDvToBeSet, Solver.ElemOrderToBeSet, PostPro);
            MeshView.Owner = this;
            MeshView.Show();
        }

        /// <summary>
        /// 固有モード分布を表示する
        /// </summary>
        private void showEigenFValueFrm()
        {
            if (EigenFValueFrm != null)
            {
                if (!EigenFValueFrm.IsDisposed)
                {
                    EigenFValueFrm.Close();
                    EigenFValueFrm.Dispose();
                    EigenFValueFrm = null;
                }
            }
            // フォームの表示
            string outPerioidcFilename = FemOutputPeriodicDatFile.GetOutputPeriodicDatFilename(FemOutputDatFilePath);
            EigenFValueFrm = new EigenFValueFrm(PostPro, outPerioidcFilename);
            EigenFValueFrm.Owner = this;
            // 周波数設定
            EigenFValueFrm.FreqNo = FreqNo;
            // 表示
            EigenFValueFrm.Show();
            // 表示位置調整
            Point screenPt = this.PointToScreen(new Point(this.Width, 0));
            EigenFValueFrm.SetDesktopLocation(screenPt.X, screenPt.Y);
        }

        /// <summary>
        /// [キャンセル]ボタンクリックイベントハンドラ
        ///     ロード時のデータ読み込みキャンセル
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLoadCancel_Click(object sender, EventArgs e)
        {
            if (IsLoading)
            {
                IsLoadCancelled = true;
            }
        }

        /// <summary>
        /// 等高線図パネルのマウスエンターイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FValuePanel_MouseEnter(object sender, EventArgs e)
        {
            if (IsLoading)
            {
                return;
            }
            if (IsDisabledMouseEnterOfFValuePanel)
            {
                return;
            }
            //System.Diagnostics.Debug.WriteLine("FValuePanel_MouseEnter");
            FValuePanelMovPt = new Point();
            // MouseMoveで表示させるように変更
            //showPrevNextFValuePanelBtn();
        }

        /// <summary>
        /// [前のパネル][次のパネル]ボタンを表示する
        /// </summary>
        private void showPrevNextFValuePanelBtn()
        {
            if (!btnPrevFValuePanel.Visible)
            {
                //System.Diagnostics.Debug.WriteLine("showPrevNextFValuePanelBtn");
                int ofsX = 20;
                int ofsY = 5;
                // Note:[前のパネル][次のパネル]の親は等高線図パネル
                btnPrevFValuePanel.Location = new Point(FValuePanel.Width / 2 - btnPrevFValuePanel.Width - ofsX, ofsY);
                btnNextFValuePanel.Location = new Point(FValuePanel.Width / 2 + ofsX, ofsY);
                btnPrevFValuePanel.Visible = true;
                btnNextFValuePanel.Visible = true;
            }
        }

        /// <summary>
        /// [前のパネル][次のパネル]ボタンを非表示にする
        ///   等高線図パネルのマウスエンターイベントを抑止して実行
        /// </summary>
        private void hidePrevNextFValuePanelBtn()
        {
            if (btnPrevFValuePanel.Visible)
            {
                //System.Diagnostics.Debug.WriteLine("hidePrevNextFValuePanelBtn");
                // 等高線図パネルのマウスエンターイベントのイベントハンドラ処理を実行しない
                IsDisabledMouseEnterOfFValuePanel = true;
                FValuePanel.MouseEnter -= FValuePanel_MouseEnter;
                // ボタン非表示
                btnPrevFValuePanel.Visible = false;
                btnNextFValuePanel.Visible = false;
                new Thread(new ThreadStart(delegate()
                {
                    Thread.Sleep(100);
                    IsDisabledMouseEnterOfFValuePanel = false;
                    FValuePanel.MouseEnter += FValuePanel_MouseEnter;
                })).Start();
            }
        }

        /// <summary>
        /// 等高線図パネルのマウスリーブイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FValuePanel_MouseLeave(object sender, EventArgs e)
        {
            if (IsLoading)
            {
                return;
            }
            //System.Diagnostics.Debug.WriteLine("FValuePanel_MouseLeave");
            FValuePanelMovPt = new Point();
            // 等高線図パネルのツールチップを非表示にする
            //toolTip1.Hide(FValuePanel);
            //toolTip1.Active = false;

            if (btnPrevFValuePanel.Visible)
            {
                // ボタンにフォーカスが当たる前にこちらのマウスリーブイベントが来るのでクリックできない
                // したかないので、スレッドで遅延実行する
                new Thread(new ThreadStart(delegate()
                    {
                        //[前のパネル][次のパネル]ボタンにフォーカスがあたるまで遅延させる
                        Thread.Sleep(100);
                        if (this.Disposing || this.IsDisposed)
                        {
                            return;
                        }
                        this.Invoke(new InvokeDelegate(delegate()
                            {
                                // ボタンにフォーカスが当たっていなければ、本当にパネル外に移動したと判定する
                                if (!btnPrevFValuePanel.Focused && !btnNextFValuePanel.Focused)
                                {
                                    btnPrevFValuePanel.Visible = false;
                                    btnNextFValuePanel.Visible = false;
                                }
                            }));

                    })).Start();
            }
        }

        /// <summary>
        /// 等高線図パネルマウスムーブイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FValuePanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsLoading)
            {
                return;
            }
            //System.Diagnostics.Debug.WriteLine("FValuePanel_MouseMove");
            if (FValuePanelMovPt.IsEmpty)
            {
                FValuePanelMovPt = e.Location;
            }
            else
            {
                Size movSize = new Size(Math.Abs(e.Location.X - FValuePanelMovPt.X), Math.Abs(e.Location.Y - FValuePanelMovPt.Y));
                if (movSize.Width >= 10 || movSize.Height >= 10)
                {
                    showPrevNextFValuePanelBtn();
                }
            }
        }

        /// <summary>
        /// 等高線図パネルのマウスホバーイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FValuePanel_MouseHover(object sender, EventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("FValuePanel_Hover");
        }

        /// <summary>
        /// [前のパネル]ボタンクリックイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPrevFValuePanel_Click(object sender, EventArgs e)
        {
            // 画面の切り替え(前の画面)
            FValuePanelIndex--;
            if (FValuePanelIndex < 0)
            {
                //FValuePanelIndex = FValuePanelFieldDV_ValueDVPairList.Length - 1;
                FValuePanelIndex = FValuePanelFieldDV_ValueDVPairList.Length;  //TEST 4画面
            }
            // 等高線図パネルインデックス変更時の処理
            changeFValuePanelIndexProc(true);

            //// ボタン非表示
            //hidePrevNextFValuePanelBtn();
        }
        /// <summary>
        /// [次のパネル]ボタンクリックイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnNextFValuePanel_Click(object sender, EventArgs e)
        {
            // 画面の切り替え(次の画面)
            FValuePanelIndex++;
            //if (FValuePanelIndex >= FValuePanelFieldDV_ValueDVPairList.Length)
            if (FValuePanelIndex > FValuePanelFieldDV_ValueDVPairList.Length)
            {
                FValuePanelIndex = 0;
            }
            // 等高線図パネルインデックス変更時の処理
            changeFValuePanelIndexProc(true);

            //// ボタン非表示
            //hidePrevNextFValuePanelBtn();
        }

        /// <summary>
        /// 等高線図パネルインデックス変更時の処理
        /// </summary>
        private void changeFValuePanelIndexProc(bool refreshFlg)
        {
            if (FValuePanelIndex < FValuePanelFieldDV_ValueDVPairList.Length)
            {
                PostPro.ShowFieldDv = FValuePanelFieldDV_ValueDVPairList[FValuePanelIndex].Key;
                PostPro.ShowValueDv = FValuePanelFieldDV_ValueDVPairList[FValuePanelIndex].Value;
            }
            else
            {
                // TEST 4画面、凡例は絶対値の場合と同じにする
                PostPro.ShowFieldDv = FValuePanelFieldDV_ValueDVPairList[0].Key;
                PostPro.ShowValueDv = FValuePanelFieldDV_ValueDVPairList[0].Value;
            }
            // 等高線図パネルのツールチップテキストを設定
            setFValuePanelToolTip();
            
            if (refreshFlg)
            {
                // 凡例を更新
                FValueLegendPanel.Refresh();
                // 等高線図パネルを更新
                FValuePanel.Refresh();
            }
        }

        /// <summary>
        /// 等高線図パネルのツールチップを設定する
        /// </summary>
        private void setFValuePanelToolTip()
        {
            // 等高線図パネルのツールチップを設定する
            string text = "";
            string[] contentName = FValuePanelContentNameForE;
            {
                // H面、平行平板
                if (PostPro.WaveModeDv == FemSolver.WaveModeDV.TM)
                {
                    // H面、平行平板TM
                    // 解析対象が磁界
                    contentName = FValuePanelContentNameForH;
                }
                else
                {
                    // H面、平行平板TE
                    // 解析対象が電界
                    contentName = FValuePanelContentNameForE;
                }
            }
            if (FValuePanelIndex >= FValuePanelFieldDV_ValueDVPairList.Length)
            {
                foreach (string tmp in contentName)
                {
                    text += tmp + " " + System.Environment.NewLine;
                }
            }
            else
            {
                text = contentName[FValuePanelIndex];
            }
            toolTip1.SetToolTip(FValuePanel, text);
        }

        /// <summary>
        /// [前のパネル]ボタンのマウスエンターイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPrevFValuePanel_MouseEnter(object sender, EventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("btnPrevFValuePanel_MouseEnter");
            // [前のパネル]ボタン上にマウスポインタを持ってくると、親のFValuePanelがMouseEnter→MouseLeaveを繰り返す現象を抑制
            btnPrevFValuePanel.Focus();
        }

        /// <summary>
        /// [前のパネル]ボタンのマウスリーブイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPrevFValuePanel_MouseLeave(object sender, EventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("btnPrevFValuePanel_MouseLeave");
            hidePrevNextFValuePanelBtn();
        }

        /// <summary>
        /// [次のパネル]ボタンのマウスエンターイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnNextFValuePanel_MouseEnter(object sender, EventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("btnNextFValuePanel_MouseEnter");
            // [次のパネル]ボタン上にマウスポインタを持ってくると、親のFValuePanelがMouseEnter→MouseLeaveを繰り返す現象を抑制
            btnNextFValuePanel.Focus();
        }

        /// <summary>
        /// [次のパネル]ボタンのマウスリーブイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnNextFValuePanel_MouseLeave(object sender, EventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("btnNextFValuePanel_MouseLeave");
            // ボタン非表示
            hidePrevNextFValuePanelBtn();
        }

        /// <summary>
        /// 「対数表示」メニュークリックイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMILogGraph_Click(object sender, EventArgs e)
        {
            bool isLogarithmic = PostPro.IsSMatChartLogarithmic;
            isLogarithmic = !isLogarithmic;
            toolStripMILogGraph.Checked = isLogarithmic;
            PostPro.SetSMatChartLogarithmic(SMatChart, isLogarithmic);
        }

        /// <summary>
        /// 「設定」ボタンクリックイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSetting_Click(object sender, EventArgs e)
        {
            double rodEps = 0.0;
            int mediaIndexCore = 1; // ロッドの媒質情報インデックス
            MediaInfo mediaCore = CadLgc.GetMediaInfo(mediaIndexCore);
            rodEps = mediaCore.Q[2, 2];
            
            SettingFrm settingFrm = new SettingFrm(
                CadLgc.NdivForOneLattice,
                CadLgc.RodRadiusRatio,
                rodEps,
                CadLgc.RodCircleDiv,
                CadLgc.RodRadiusDiv);
            DialogResult result = settingFrm.ShowDialog();
            if (result == DialogResult.OK)
            {
                CadLgc.NdivForOneLattice = settingFrm.NdivForOneLattice;
                CadLgc.RodRadiusRatio = settingFrm.RodRadiusRatio;
                rodEps = settingFrm.RodEps;
                CadLgc.RodCircleDiv = settingFrm.RodCircleDiv;
                CadLgc.RodRadiusDiv = settingFrm.RodRadiusDiv;

                mediaCore.SetQ(new double[,]
                            {
                                {rodEps, 0.0, 0.0},
                                {0.0, rodEps, 0.0},
                                {0.0, 0.0, rodEps},
                            });
                CadLgc.SetMediaInfo(mediaIndexCore, mediaCore);
            }
        }

        /// <summary>
        /// モード分布フォーム表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEigenFValueShow_Click(object sender, EventArgs e)
        {
            // 固有モード分布フォームを表示する
            showEigenFValueFrm();
        }

    }
}
