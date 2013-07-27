using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
//using System.Numerics; // Complex
using KrdLab.clapack; // KrdLab.clapack.Complex
using System.Drawing;

namespace PhCFem
{
    /// <summary>
    /// Femポストプロセッサロジック
    /// </summary>
    class FemPostProLogic : IDisposable
    {
        /////////////////////////////////////////////////////////////////
        // 定数
        /////////////////////////////////////////////////////////////////
        /// <summary>
        /// 凡例の色レベル数
        /// </summary>
        private const int LegendColorCnt = 10;
        /// <summary>
        /// 表示するモードの数
        /// </summary>
        private const int ShowMaxMode = 1;
        //private const int ShowMaxMode = 6;

        private const double InvalidValueForSMat = -1.0e+12;

        /////////////////////////////////////////////////////////////////
        // フィールド
        /////////////////////////////////////////////////////////////////
        /// <summary>
        /// 1度だけの初期化済み?
        /// </summary>
        private bool IsInitializedOnce = false;
        
        /////////////////////////////////////////////////////////////////
        // 入力データ
        /////////////////////////////////////////////////////////////////
        /// <summary>
        /// 節点リスト
        /// </summary>
        private FemNode[] Nodes;
        /// <summary>
        /// 要素リスト
        /// </summary>
        private FemElement[] Elements;
        /// <summary>
        /// 媒質リスト
        /// </summary>
        private MediaInfo[] Medias;
        /// <summary>
        /// ポートの節点番号リストのリスト
        /// </summary>
        private IList<int[]> Ports;
        /// <summary>
        /// 強制境界の節点番号リスト
        /// </summary>
        private int[] ForceNodes;
        /// <summary>
        /// 入射ポート番号
        /// </summary>
        private int IncidentPortNo = 1;

        /////////////////////////////////////////////////////////////////
        // 出力データ
        /////////////////////////////////////////////////////////////////
        /// <summary>
        /// 波長
        /// </summary>
        private double WaveLength = 0.0;
        /// <summary>
        /// 考慮モード数
        /// </summary>
        private int MaxMode = 0;
        /// <summary>
        /// ポートの節点リスト
        /// </summary>
        //private IList<int[]> NodesBoundaryList = new List<int[]>(); //メモリ節約の為、削除
        /// <summary>
        /// ポートの固有値リスト
        /// </summary>
        private IList<Complex[]> EigenValuesList = new List<Complex[]>();
        /// <summary>
        /// ポートの固有ベクトルリスト
        /// </summary>
        private IList<Complex[,]> EigenVecsList = new List<Complex[,]>();
        /// <summary>
        /// 領域内節点リスト
        /// </summary>
        //private int[] NodesRegion; //メモリ節約の為、ローカル変数で処理するようにしたため削除
        /// <summary>
        /// フィールド値リスト
        /// </summary>
        //private Complex[] ValuesAll = null; //メモリ節約の為、ローカル変数で処理するようにしたため削除
        /// <summary>
        /// フィールド値の絶対値の最小値
        /// </summary>
        private double MinFValue = 0.0;
        /// <summary>
        /// フィールド値の絶対値の最大値
        /// </summary>
        private double MaxFValue = 1.0;
        /// <summary>
        /// フィールド値の回転の絶対値の最小値
        /// </summary>
        private double MinRotFValue = 0.0;
        /// <summary>
        /// フィールド値の回転の絶対値の最大値
        /// </summary>
        private double MaxRotFValue = 1.0;
        /// <summary>
        /// 複素ポインティングベクトルの絶対値の最小値
        /// </summary>
        private double MinPoyntingFValue = 0.0;
        /// <summary>
        /// 複素ポインティングベクトルの絶対値の最大値
        /// </summary>
        private double MaxPoyntingFValue = 1.0;
        /// <summary>
        /// 固有モードの界の最小値
        /// </summary>
        private double MinEigenFValue = 0.0;
        /// <summary>
        /// 固有モードの界の最大値
        /// </summary>
        private double MaxEigenFValue = 0.0;
        /// <summary>
        /// 散乱行列
        /// </summary>
        private IList<Complex[]> ScatterVecList = new List<Complex[]>();
        /// <summary>
        /// 導波管幅
        /// </summary>
        private double WaveguideWidth = 0.0;
        /// <summary>
        /// 格子定数
        /// </summary>
        private double LatticeA = 1.0;
        /// <summary>
        /// 計算開始波長
        /// </summary>
        public double FirstWaveLength
        {
            get;
            private set;
        }
        /// <summary>
        /// 計算終了波長
        /// </summary>
        public double LastWaveLength
        {
            get;
            private set;
        }
        /// <summary>
        /// 計算する周波数の個数
        /// </summary>
        public int CalcFreqCnt
        {
            get;
            private set;
        }

        /// <summary>
        /// 波のモード区分
        /// </summary>
        public FemSolver.WaveModeDV WaveModeDv
        {
            get;
            private set;
        }

        /// <summary>
        /// 要素の数を取得する(表示用)
        /// </summary>
        public int ElementCnt
        {
            get
            {
                if (Elements != null)
                {
                    return Elements.Length;
                }
                return 0;
            }
        }
        /// <summary>
        /// 節点の数を取得する(表示用)
        /// </summary>
        public int NodeCnt
        {
            get
            {
                if (Nodes != null)
                {
                    return Nodes.Length;
                }
                return 0;
            }
        }
        /// <summary>
        /// フィールド値カラーパレット
        /// </summary>
        private ColorMap FValueColorMap = new ColorMap();
        /// <summary>
        /// フィールド値カラーパレット(周期構造導波路固有モード用)
        /// </summary>
        private ColorMap EigenFValueColorMap = new ColorMap();
        /// <summary>
        /// フィールド値凡例の色パネル
        /// </summary>
        Panel FValueLegendColorPanel = null;
        /// <summary>
        /// 媒質境界の辺のリスト
        ///   辺は"節点番号_節点番号"として格納
        /// </summary>
        private IList<string> MediaBEdgeList = new List<string>();

        /// <summary>
        /// 表示するフィールドのフィールド区分
        /// </summary>
        public FemElement.FieldDV ShowFieldDv
        {
            get;
            set;
        }

        /// <summary>
        /// 表示するフィールドの値区分
        /// </summary>
        public FemElement.ValueDV ShowValueDv
        {
            get;
            set;
        }

        /// <summary>
        /// フィールド値描画を荒くする？
        /// </summary>
        private bool _IsCoarseFieldMesh = false;

        /// <summary>
        /// フィールド値描画を荒くする？
        /// </summary>
        public bool IsCoarseFieldMesh
        {
            get
            {
                return _IsCoarseFieldMesh;
            }
            set
            {
                if (value != _IsCoarseFieldMesh)
                {
                    if (Elements != null)
                    {
                        foreach (FemElement element in Elements)
                        {
                            element.IsCoarseFieldMesh = value;
                        }
                    }
                }
                _IsCoarseFieldMesh = value;
            }
        }

        /// <summary>
        /// 自動計算モード？
        /// </summary>
        public bool IsAutoCalc
        {
            get;
            set;
        }

        /// <summary>
        /// 対数グラフ表示する？
        /// </summary>
        public bool IsSMatChartLogarithmic
        {
            get;
            private set;
        }

        /// <summary>
        /// 散乱係数チャートのXの値のリスト
        /// </summary>
        private IList<double> SMatChartXValueList = new List<double>();
        /// <summary>
        /// 散乱係数チャートのYの値のリスト
        /// </summary>
        private IList<IList<double[]>> SMatChartYValuesList = new List<IList<double[]>>();
        /// <summary>
        /// 伝搬定数チャートのXの値のリスト
        /// </summary>
        private IList<double> BetaChartXValueList = new List<double>();
        /// <summary>
        /// 伝搬定数チャートのYの値のリスト
        /// </summary>
        private IList<IList<double[]>> BetaChartYValuesList = new List<IList<double[]>>();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FemPostProLogic()
        {
            IsInitializedOnce = false;
            ShowFieldDv = FemElement.FieldDV.Field;
            ShowValueDv = FemElement.ValueDV.Abs;
            IsAutoCalc = false;
            IsSMatChartLogarithmic = false;
            initInput();
            initOutput();
        }

        /// <summary>
        /// 初期化処理(入力)
        /// </summary>
        private void initInput()
        {
            Nodes = null;
            Elements = null;
            Medias = null;
            Ports = null;
            ForceNodes = null;
            WaveguideWidth = FemSolver.DefWaveguideWidth;
            IncidentPortNo = 1;
            CalcFreqCnt = 0;
            FirstWaveLength = 0.0;
            LastWaveLength = 0.0;
            WaveModeDv = FemSolver.WaveModeDV.TE;
            _IsCoarseFieldMesh = false;
        }

        /// <summary>
        /// 初期化処理(出力)
        /// </summary>
        private void initOutput()
        {
            WaveLength = 0;
            MaxMode = 0;
            //NodesBoundaryList.Clear();
            MediaBEdgeList.Clear();
            EigenValuesList.Clear();
            EigenVecsList.Clear();
            //NodesRegion = null;
            //ValuesAll = null;
            MaxFValue = 1.0;
            MinFValue = 0.0;
            MaxRotFValue = 1.0;
            MinRotFValue = 0.0;
            MaxPoyntingFValue = 1.0;
            MinPoyntingFValue = 0.0;
        }

        /// <summary>
        /// デストラクタ
        /// </summary>
        ~FemPostProLogic()
        {
            Dispose(false);
        }

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        /// <summary>
        /// リソース破棄
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 2点間距離の計算
        /// </summary>
        /// <param name="p"></param>
        /// <param name="p0"></param>
        /// <returns></returns>
        private double getDistance(double[] p, double[] p0)
        {
            return Math.Sqrt((p[0] - p0[0]) * (p[0] - p0[0]) + (p[1] - p0[1]) * (p[1] - p0[1]));
        }

        /// <summary>
        /// 出力ファイルから計算済み周波数の数を取得する
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public int GetCalculatedFreqCnt(string filename, out int firstFreqNo, out int lastFreqNo)
        {
            return FemOutputDatFile.GetCalculatedFreqCnt(filename, out firstFreqNo, out lastFreqNo);
        }

        /// <summary>
        /// 入出力データの初期化
        /// </summary>
        private void initDataOnce(
            Panel FValueLegendPanel,
            Label labelFreqValue
        )
        {
            if (IsInitializedOnce) return;

            // フィールド値凡例パネルの初期化
            InitFValueLegend(FValueLegendPanel, labelFreqValue);
            
            IsInitializedOnce = true;
        }

        /// <summary>
        /// 入出力データの初期化
        /// </summary>
        public void InitData(
            FemSolver solver,
            Panel CadPanel,
            Panel FValuePanel,
            Panel FValueLegendPanel, Label labelFreqValue,
            Chart SMatChart,
            Chart BetaChart,
            Chart EigenVecChart
            )
        {
            initInput();
            initOutput();

            // 一度だけの初期化処理
            initDataOnce(FValueLegendPanel, labelFreqValue);

            // ポストプロセッサに入力データをコピー
            // 入力データの取得
            solver.GetFemInputInfo(out Nodes, out Elements, out Medias, out Ports, out ForceNodes, out IncidentPortNo, out WaveguideWidth, out LatticeA);
            // チャートの設定用に開始終了波長を取得
            FirstWaveLength = solver.FirstWaveLength;
            LastWaveLength = solver.LastWaveLength;
            CalcFreqCnt = solver.CalcFreqCnt;
            // 波のモード区分を取得
            WaveModeDv = solver.WaveModeDv;

            //if (isInputDataReady())
            // ポートが指定されていなくてもメッシュを表示できるように条件を変更
            if (Elements != null && Elements.Length > 0 && Nodes != null && Nodes.Length > 0 && Medias != null && Medias.Length > 0)
            {
                // 各要素に節点情報を補完する
                foreach (FemElement element in Elements)
                {
                    element.SetNodesFromAllNodes(Nodes);
                    element.LineColor = Color.Black;
                    element.BackColor = Medias[element.MediaIndex].BackColor;
                }
            }

            // メッシュ描画
            //using (Graphics g = CadPanel.CreateGraphics())
            //{
            //    DrawMesh(g, CadPanel);
            //}
            //CadPanel.Invalidate();

            if (!IsAutoCalc)
            {
                // チャート初期化
                ResetSMatChart(SMatChart);
                // 等高線図の凡例
                UpdateFValueLegend(FValueLegendPanel, labelFreqValue);
                // 等高線図
                //FValuePanel.Invalidate();
                FValuePanel.Refresh();
                // 固有値チャート初期化
                // この段階ではMaxModeの値が0なので、後に計算値ロード後一回だけ初期化する
                ResetEigenValueChart(BetaChart);
                // 固有ベクトル表示(空のデータで初期化)
                SetEigenVecToChart(EigenVecChart);
            }
        }

        /// <summary>
        /// 規格化周波数をセットする(自動計算モード用)
        /// </summary>
        /// <param name="normalizedFreq"></param>
        public void SetNormalizedFrequency(double normalizedFreq)
        {
            // 波長をセット
            WaveLength = FemSolver.GetWaveLengthFromNormalizedFreq(normalizedFreq, WaveguideWidth, LatticeA);
        }

        /// <summary>
        /// 出力データだけ初期化する(自動計算モード用)
        /// </summary>
        /// <param name="CadPanel"></param>
        /// <param name="FValuePanel"></param>
        /// <param name="FValueLegendPanel"></param>
        /// <param name="labelFreqValue"></param>
        /// <param name="SMatChart"></param>
        /// <param name="BetaChart"></param>
        /// <param name="EigenVecChart"></param>
        public void InitOutputData(
            Panel CadPanel,
            Panel FValuePanel,
            Panel FValueLegendPanel, Label labelFreqValue,
            Chart SMatChart,
            Chart BetaChart,
            Chart EigenVecChart
            )
        {
            initOutput();

            // メッシュ描画
            //using (Graphics g = CadPanel.CreateGraphics())
            //{
            //    DrawMesh(g, CadPanel);
            //}
            //CadPanel.Invalidate();

            if (!IsAutoCalc)
            {
                // チャート初期化
                ResetSMatChart(SMatChart);
                // 等高線図の凡例
                UpdateFValueLegend(FValueLegendPanel, labelFreqValue);
                //FValuePanel.Invalidate();
                // 等高線図
                FValuePanel.Refresh();
                // 固有値チャート初期化
                // この段階ではMaxModeの値が0なので、後に計算値ロード後一回だけ初期化する
                ResetEigenValueChart(BetaChart);
                // 固有ベクトル表示(空のデータで初期化)
                SetEigenVecToChart(EigenVecChart);
            }
        }

        /// <summary>
        /// 出力結果ファイル読み込み
        /// </summary>
        /// <param name="filename"></param>
        public bool LoadOutput(string filename, int freqNo)
        {
            initOutput();
            if (!isInputDataReady())
            {
                //MessageBox.Show("入力データがセットされていません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            int incidentPortNo = 0;
            IList<int[]> nodesBoundaryList = null;
            int[] nodesRegion = null;
            Complex[] valuesAll = null;
            bool ret = FemOutputDatFile.LoadFromFile(
                filename, freqNo,
                out WaveLength, out MaxMode, out incidentPortNo,
                out nodesBoundaryList, out EigenValuesList, out EigenVecsList,
                out nodesRegion, out valuesAll,
                out ScatterVecList);

            if (ret)
            {
                //System.Diagnostics.Debug.Assert(maxMode == MaxMode);
                System.Diagnostics.Debug.Assert(incidentPortNo == IncidentPortNo);

                // メモリ節約の為必要なモード数だけ取り出す
                /*
                if (EigenValuesList != null)
                {
                    for (int portIndex = 0; portIndex < EigenValuesList.Count; portIndex++)
                    {
                        Complex[] eigenValues = EigenValuesList[portIndex];
                        Complex[] eigenValues2 = new Complex[ShowMaxMode];
                        for (int imode = 0; imode < eigenValues.Length; imode++)
                        {
                            if (imode >= ShowMaxMode) break;
                            eigenValues2[imode] = eigenValues[imode];
                        }
                        // 入れ替える
                        EigenValuesList[portIndex] = eigenValues2;
                        eigenValues = null;
                        eigenValues2 = null;
                    }
                }
                 */
                /*
                if (EigenVecsList != null)
                {
                    for (int portIndex = 0; portIndex < EigenVecsList.Count; portIndex++)
                    {
                        Complex[,] eigenVecs = EigenVecsList[portIndex];
                        Complex[,] eigenVecs2 = new Complex[ShowMaxMode, eigenVecs.GetLength(1)];
                        for (int imode = 0; imode < eigenVecs.GetLength(0); imode++)
                        {
                            if (imode >= ShowMaxMode) break;
                            for (int ino = 0; ino < eigenVecs.GetLength(1); ino++)
                            {
                                eigenVecs2[imode, ino] = eigenVecs[imode, ino];
                            }
                        }
                        // 入れ替える
                        EigenVecsList[portIndex] = eigenVecs2;
                        eigenVecs = null;
                        eigenVecs2 = null;
                    }
                }
                 */
                /*
                if (ScatterVecList != null)
                {
                    for (int portIndex = 0; portIndex < ScatterVecList.Count; portIndex++)
                    {
                        Complex[] portScatterVec = ScatterVecList[portIndex];
                        Complex[] portScatterVec2 = new Complex[ShowMaxMode];
                        for (int imode = 0; imode < portScatterVec.Length; imode++)
                        {
                            if (imode >= ShowMaxMode) break;
                            portScatterVec2[imode] = portScatterVec[imode];
                        }
                        // 入れ替える
                        ScatterVecList[portIndex] = portScatterVec2;
                        portScatterVec = null;
                        portScatterVec2 = null;
                    }
                }
                 */

                // 要素にフィールド値をセットする
                setupFieldValueToElements(nodesRegion, valuesAll);

                // 媒質境界の辺を取得する
                setupMediaBEdgeList();
            }
            return ret;
        }

        /// <summary>
        /// 要素にフィールド値をセットする
        /// </summary>
        /// <param name="nodesRegion">節点番号リスト</param>
        /// <param name="valuesAll">節点のフィールド値のリスト</param>
        private void setupFieldValueToElements(int[] nodesRegion, Complex[] valuesAll)
        {
            //System.Diagnostics.Debug.Assert(Math.Abs(WaveLength) < Constants.PrecisionLowerLimit);
            if (Math.Abs(WaveLength) < Constants.PrecisionLowerLimit)
            {
                return;
            }
            if (Elements == null || Elements.Length == 0)
            {
                return;
            }

            // 定数
            const double pi = Constants.pi;
            const double c0 = Constants.c0;
            // 波数
            double k0 = 2.0 * pi / WaveLength;
            // 角周波数
            double omega = k0 * c0;

            // 回転に掛ける因子
            Complex factorForRot = 1.0;
            {
                // H面、平行平板
                //    rot(F) = factor * q * (G)
                if (WaveModeDv == FemSolver.WaveModeDV.TM)
                {
                    // TMモードの場合、q :比誘電率
                    //   F:磁界
                    //   G:電界
                    factorForRot = Complex.ImaginaryOne / (omega * Constants.eps0);
                }
                else
                {
                    // TEモードの場合、q:比透磁率
                    //   F:電界
                    //   G:磁界
                    factorForRot = -1.0 * Complex.ImaginaryOne / (omega * Constants.mu0);
                }
            }

            /// 領域内節点の節点番号→インデックスマップ
            Dictionary<int, int> nodesRegionToIndex = new Dictionary<int, int>();
            // 節点番号→インデックスのマップ作成
            for (int ino = 0; ino < nodesRegion.Length; ino++)
            {
                int nodeNumber = nodesRegion[ino];
                if (!nodesRegionToIndex.ContainsKey(nodeNumber))
                {
                    nodesRegionToIndex.Add(nodeNumber, ino);
                }
            }
            // 要素リストにフィールド値を格納
            foreach (FemElement element in Elements)
            {
                MediaInfo media = Medias[element.MediaIndex];
                double[,] media_Q = null;
                {
                    // H面、平行平板
                    //    rot(F) = factor * q * (G)
                    if (WaveModeDv == FemSolver.WaveModeDV.TM)
                    {
                        // TMモードの場合、q :比誘電率
                        //   F:磁界
                        //   G:電界
                        media_Q = media.Q;
                    }
                    else
                    {
                        // TEモードの場合、q:比透磁率
                        //   F:電界
                        //   G:磁界
                        media_Q = media.P;
                    }
                }
                element.SetFieldValueFromAllValues(valuesAll, nodesRegionToIndex,
                    factorForRot, media_Q, WaveModeDv);
            }

            // フィールド値の絶対値の最小、最大
            double minFValue = double.MaxValue;
            double maxFValue = double.MinValue;
            double minRotFValue = double.MaxValue;
            double maxRotFValue = double.MinValue;
            double minPoyntingFValue = double.MaxValue;
            double maxPoyntingFValue = double.MinValue;
            foreach (FemElement element in Elements)
            {
                int nno = element.NodeNumbers.Length;
                for (int ino = 0; ino < nno; ino++)
                {
                    Complex fValue = element.getFValue(ino);
                    Complex rotXFValue = element.getRotXFValue(ino);
                    Complex rotYFValue = element.getRotYFValue(ino);
                    Complex poyntingXFValue = element.getPoyntingXFValue(ino);
                    Complex poyntingYFValue = element.getPoyntingYFValue(ino);
                    double fValueAbs = Complex.Abs(fValue);
                    //double rotFValueAbs = Math.Sqrt(Math.Pow(rotXFValue.Magnitude, 2) + Math.Pow(rotYFValue.Magnitude, 2));
                    //double rotFValueAbs = Math.Sqrt(Math.Pow(rotXFValue.Real, 2) + Math.Pow(rotYFValue.Real, 2));
                    double rotFValueAbs = Math.Sqrt(rotXFValue.Real * rotXFValue.Real + rotYFValue.Real * rotYFValue.Real);
                    //double poyntingFValueAbs = Math.Sqrt(Math.Pow(poyntingXFValue.Magnitude, 2) + Math.Pow(poyntingYFValue.Magnitude, 2));
                    //double poyntingFValueAbs = Math.Sqrt(Math.Pow(poyntingXFValue.Real, 2) + Math.Pow(poyntingYFValue.Real, 2));
                    double poyntingFValueAbs = Math.Sqrt(poyntingXFValue.Real * poyntingXFValue.Real + poyntingYFValue.Real * poyntingYFValue.Real);

                    if (fValueAbs > maxFValue)
                    {
                        maxFValue = fValueAbs;
                    }
                    if (fValueAbs < minFValue)
                    {
                        minFValue = fValueAbs;
                    }
                    if (rotFValueAbs > maxRotFValue)
                    {
                        maxRotFValue = rotFValueAbs;
                    }
                    if (rotFValueAbs < minRotFValue)
                    {
                        minRotFValue = rotFValueAbs;
                    }

                    if (poyntingFValueAbs > maxPoyntingFValue)
                    {
                        maxPoyntingFValue = poyntingFValueAbs;
                    }
                    if (poyntingFValueAbs < minPoyntingFValue)
                    {
                        minPoyntingFValue = poyntingFValueAbs;
                    }
                }
            }
            // 節点上の値より要素内部の値の方が大きいことがある
            double scaleFactor = 1.05;
            MinFValue = minFValue * scaleFactor;
            MaxFValue = maxFValue * scaleFactor;
            MinRotFValue = minRotFValue * scaleFactor;
            MaxRotFValue = maxRotFValue * scaleFactor;
            MinPoyntingFValue = minPoyntingFValue * scaleFactor;
            MaxPoyntingFValue = maxPoyntingFValue * scaleFactor;

            /*
            // 等高線図描画の為に最大、最小値を取得する
            // フィールド値の絶対値の最小、最大
            double minfValue = double.MaxValue;
            double maxfValue = double.MinValue;
            foreach (Complex fValue in valuesAll)
            {
                double v = Complex.Abs(fValue);
                if (v > maxfValue)
                {
                    maxfValue = v;
                }
                if (v < minfValue)
                {
                    minfValue = v;
                }
            }
            MinFValue = minfValue;
            MaxFValue = maxfValue;
             */
        }

        /// <summary>
        /// 媒質境界の辺を取得
        /// </summary>
        private void setupMediaBEdgeList()
        {
            // 辺と要素番号の対応マップを取得
            Dictionary<string, IList<int>> edgeToElementNoH = new Dictionary<string, IList<int>>();
            FemSolver.MkEdgeToElementNoH(Elements, ref edgeToElementNoH);

            MediaBEdgeList.Clear();
            // 媒質の境界の辺を取得
            foreach (KeyValuePair<string, IList<int>> pair in edgeToElementNoH)
            {
                string edgeKeyStr = pair.Key;
                IList<int> elementNoList = pair.Value;
                if (elementNoList.Count >= 2)
                {
                    if (Elements[elementNoList[0] - 1].MediaIndex != Elements[elementNoList[1] - 1].MediaIndex)
                    {
                        MediaBEdgeList.Add(edgeKeyStr);
                    }
                }
            }
        }

        /// <summary>
        /// データが準備できてる？
        /// </summary>
        /// <returns></returns>
        public bool IsDataReady()
        {
            return isInputDataReady() && isOutputDataReady();
        }

        /// <summary>
        /// 入力データ準備済み？
        /// </summary>
        /// <returns></returns>
        private bool isInputDataReady()
        {
            bool isReady = false;

            if (Nodes == null)
            {
                return isReady;
            }
            if (Elements == null)
            {
                return isReady;
            }
            if (Ports == null)
            {
                return isReady;
            }
            if (ForceNodes == null)
            {
                return isReady;
            }
            if (Math.Abs(WaveguideWidth - FemSolver.DefWaveguideWidth) < Constants.PrecisionLowerLimit)
            {
                return isReady;
            }
            if (Ports.Count == 0)
            {
                return isReady;
            }

            isReady = true;
            return isReady;
        }
        /// <summary>
        /// 出力データ準備済み？
        /// </summary>
        /// <returns></returns>
        private bool isOutputDataReady()
        {
            bool isReady = false;
            if (WaveLength == 0)
            {
                return isReady;
            }
            if (MaxMode == 0)
            {
                return isReady;
            }
            //if (NodesBoundaryList.Count == 0)
            //{
            //    return isReady;
            //}
            if (EigenValuesList.Count == 0)
            {
                return isReady;
            }
            if (EigenVecsList.Count == 0)
            {
                return isReady;
            }
            //if (NodesRegion == null)
            //{
            //    return isReady;
            //}
            //if (ValuesAll == null)
            //{
            //    return isReady;
            //}
            if (ScatterVecList.Count == 0)
            {
                return isReady;
            }

            isReady = true;
            return isReady;
        }
        
        /// <summary>
        /// 出力をGUIへセットする
        /// </summary>
        /// <param name="addFlg">周波数特性グラフに読み込んだ周波数のデータを追加する？</param>
        /// <param name="isUpdateFValuePanel">等高線図を更新する？(データ読み込み時のアニメーションが遅いので更新しないようにするため導入)</param>
        public void SetOutputToGui(
            string FemOutputDatFilePath,
            Panel CadPanel,
            Panel FValuePanel,
            Panel FValueLegendPanel, Label labelFreqValue,
            Chart SMatChart,
            Chart BetaChart,
            Chart EigenVecChart,
            bool addFlg = true,
            bool isUpdateFValuePanel = true)
        {
            if (!isInputDataReady())
            {
                return;
            }
            if (!isOutputDataReady())
            {
                return;
            }
            if (IsAutoCalc)
            {
                ResetSMatChart(SMatChart);
                ResetEigenValueChart(BetaChart);
                /*
                // チャートのデータをクリア
                foreach (Series series in SMatChart.Series)
                {
                    series.Points.Clear();
                }
                foreach (Series series in BetaChart.Series)
                {
                    series.Points.Clear();
                }
                 */
                //foreach (Series series in EigenVecChart.Series)
                //{
                //    series.Points.Clear();
                //}
            }

            if (addFlg)
            {
                // Sマトリックス周波数特性グラフに計算した点を追加
                AddScatterMatrixToChart(SMatChart);

                // 固有値(伝搬定数)周波数特性グラフに計算した点を追加
                AddEigenValueToChart(BetaChart);
            }

            // 等高線図の凡例
            UpdateFValueLegend(FValueLegendPanel, labelFreqValue);
            if (isUpdateFValuePanel)
            {
                // 等高線図
                FValuePanel.Refresh();
            }
            // 固有ベクトル表示
            SetEigenVecToChart(EigenVecChart);

            if (IsAutoCalc)
            {
                // チャートの表示をポイント表示にする
                ShowChartDataLabel(SMatChart);
                ShowChartDataLabel(BetaChart);
            }
        }
        
        
        /// <summary>
        /// メッシュ描画
        /// </summary>
        /// <param name="g"></param>
        /// <param name="panel"></param>
        public void DrawMesh(Graphics g, Panel panel, bool fitFlg = false, bool transparent = false)
        {
            DrawMesh(g, panel, panel.ClientRectangle, fitFlg, transparent);
        }

        public void DrawMesh(Graphics g, Panel panel, Rectangle clientRectangle, bool fitFlg = false, bool transparent = false)
        {
            //if (!isInputDataReady())
            // ポートが指定されていなくてもメッシュを表示できるように条件を変更
            if (!(Elements != null && Elements.Length > 0 && Nodes != null && Nodes.Length > 0))
            {
                return;
            }
            Size ofs;
            Size delta;
            Size regionSize;
            if (!fitFlg)
            {
                //getDrawRegion(panel, out delta, out ofs, out regionSize);
                getDrawRegion(clientRectangle.Width, clientRectangle.Height, out delta, out ofs, out regionSize);
            }
            else
            {
                //getFitDrawRegion(panel, out delta, out ofs, out regionSize);
                getFitDrawRegion(clientRectangle.Width, clientRectangle.Height, out delta, out ofs, out regionSize);
            }
            ofs.Width += clientRectangle.Left;
            ofs.Height += clientRectangle.Top;

            foreach (FemElement element in Elements)
            {
                element.LineColor = panel.ForeColor;
                Color saveBackColor = element.BackColor;
                Color saveLineColor = element.LineColor;
                if (transparent)
                {
                    element.BackColor = Color.FromArgb(64, saveBackColor.R, saveBackColor.G, saveBackColor.B);
                    element.LineColor = element.BackColor;
                }
                element.Draw(g, ofs, delta, regionSize, true);
                if (transparent)
                {
                    element.LineColor = saveLineColor;
                    element.BackColor = saveBackColor;
                }
            }
        }

        /// <summary>
        /// 描画領域を取得
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="delta"></param>
        /// <param name="ofs"></param>
        /// <param name="regionSize"></param>
        private void getDrawRegion(Panel panel, out Size delta, out Size ofs, out Size regionSize)
        {
            getDrawRegion(panel.Width, panel.Height, out delta, out ofs, out regionSize);
        }

        private void getDrawRegion(int panelWidth, int panelHeight, out Size delta, out Size ofs, out Size regionSize)
        {
            // 描画領域の方眼桝目の寸法を決定
            double deltaxx = panelWidth / (double)(Constants.MaxDiv.Width + 2);
            int deltax = (int)deltaxx;
            double deltayy = panelHeight / (double)(Constants.MaxDiv.Height + 2);
            int deltay = (int)deltayy;
            ofs = new Size(deltax, deltay);
            delta = new Size(deltax, deltay);
            regionSize = new Size(delta.Width * Constants.MaxDiv.Width, delta.Height * Constants.MaxDiv.Height);
        }

        /// <summary>
        /// パネルに合わせて領域を拡縮する
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="delta"></param>
        /// <param name="ofs"></param>
        /// <param name="regionSize"></param>
        private void getFitDrawRegion(Panel panel, out Size delta, out Size ofs, out Size regionSize)
        {
            getFitDrawRegion(panel.Width, panel.Height, out delta, out ofs, out regionSize);
        }

        private void getFitDrawRegion(int panelWidth, int panelHeight, out Size delta, out Size ofs, out Size regionSize)
        {
            const int ndim = 2;

            // 節点座標の最小、最大
            double[] minPt = new double[] { double.MaxValue, double.MaxValue };
            double[] maxPt = new double[] { double.MinValue, double.MinValue };
            foreach (FemNode node in Nodes)
            {
                for (int i = 0; i < ndim; i++)
                {
                    if (node.Coord[i] > maxPt[i])
                    {
                        maxPt[i] = node.Coord[i];
                    }
                    if (node.Coord[i] < minPt[i])
                    {
                        minPt[i] = node.Coord[i];
                    }
                }
            }
            double[] midPt = new double[] { (minPt[0] + maxPt[0]) * 0.5, (minPt[1] + maxPt[1]) * 0.5 };

            int panel_width = panelWidth;
            int panel_height = panel_height = (int)((double)panelWidth * (Constants.MaxDiv.Height + 2) / (double)(Constants.MaxDiv.Width + 2));
            if (panelHeight < panel_height)
            {
                panel_height = panelHeight;
                panel_width = (int)((double)panelHeight * (Constants.MaxDiv.Width + 2) / (double)(Constants.MaxDiv.Height + 2));
            }
            // 描画領域の方眼桝目の寸法を決定
            // 図形をパネルのサイズにあわせて拡縮する
            int w = (int)(maxPt[0] - minPt[0]);
            int h = (int)(maxPt[1] - minPt[1]);
            int boxWidth = w > h ? w : h;
            System.Diagnostics.Debug.Assert(boxWidth > 0);
            double marginxx = panel_width / (double)(Constants.MaxDiv.Width + 2);
            int marginx = (int)marginxx;
            double marginyy = panel_height / (double)(Constants.MaxDiv.Height + 2);
            int marginy = (int)marginyy;
            double deltaxx = (panel_width - marginx * 2) / (double)boxWidth;
            int deltax = (int)deltaxx;
            double deltayy = (panel_height - marginy * 2) / (double)boxWidth;
            int deltay = (int)deltayy;
            // 図形の左下がパネルの左下にくるようにする
            int ofsx = marginx - (int)(deltaxx * (minPt[0] - 0));
            //int ofsy = marginy - (int)(deltayy * ((Constants.MaxDiv.Height - minPt[1]) - Constants.MaxDiv.Height));
            int ofsy = marginy - (int)(deltayy * (minPt[1] - 0));
            // 図形の中央がパネルの中央に来るようにする
            ofsx += (int)(deltaxx * (boxWidth - w) * 0.5);
            //ofsy -= (int)(deltayy * (boxWidth - h) * 0.5);
            ofsy += (int)(deltayy * (boxWidth - h) * 0.5);
            // アスペクト比を調整した分
            ofsx += (int)((panelWidth - panel_width) * 0.5);
            ofsy += (int)((panelHeight - panel_height) * 0.5);

            delta = new Size(deltax, deltay);
            ofs = new Size(ofsx, ofsy);
            regionSize = new Size(delta.Width * boxWidth, delta.Height * boxWidth);
            //System.Diagnostics.Debug.WriteLine("{0},{1}", ofs.Width, ofs.Height);
            //System.Diagnostics.Debug.WriteLine("{0},{1}", regionSize.Width, regionSize.Height);
        }

        /// <summary>
        /// フィールド値等高線図描画
        /// </summary>
        /// <param name="g"></param>
        /// <param name="panel"></param>
        public void DrawField(Graphics g, Panel panel)
        {
            DrawFieldEx(g, panel, panel.ClientRectangle, ShowFieldDv, ShowValueDv);
        }

        /// <summary>
        /// フィールド値等高線図描画
        /// </summary>
        /// <param name="g"></param>
        /// <param name="panel"></param>
        public void DrawFieldEx(Graphics g, Panel panel, Rectangle clientRectangle, FemElement.FieldDV fieldDv, FemElement.ValueDV valueDv)
        {
            if (!isInputDataReady())
            {
                return;
            }
            if (!isOutputDataReady())
            {
                return;
            }
            Size delta;
            Size ofs;
            Size regionSize;
            //getFitDrawRegion(panel, out delta, out ofs, out regionSize);
            getFitDrawRegion(clientRectangle.Width, clientRectangle.Height, out delta, out ofs, out regionSize);
            ofs.Width += clientRectangle.Left;
            ofs.Height += clientRectangle.Top;

            double min = 0.0;
            double max = 1.0;
            if (fieldDv == FemElement.FieldDV.Field)
            {
                min = MinFValue;
                max = MaxFValue;
            }
            else if (fieldDv == FemElement.FieldDV.RotX || fieldDv == FemElement.FieldDV.RotY)
            {
                min = MinRotFValue;
                max = MaxRotFValue;
            }
            else
            {
                return;
            }

            // カラーマップに最小、最大を設定
            if (valueDv == FemElement.ValueDV.Real || valueDv == FemElement.ValueDV.Imaginary)
            {
                FValueColorMap.Min = -max;
                FValueColorMap.Max = max;
            }
            else
            {
                // 既定値は絶対値で処理する
                //FValueColorMap.Min = min;
                FValueColorMap.Min = 0.0;
                FValueColorMap.Max = max;
            }

            foreach (FemElement element in Elements)
            {
                // 等高線描画
                element.DrawField(g, ofs, delta, regionSize, fieldDv, valueDv, FValueColorMap);
            }
        }

        /// <summary>
        /// フィールドの回転ベクトル描画
        /// </summary>
        /// <param name="g"></param>
        /// <param name="panel"></param>
        public void DrawRotField(Graphics g, Panel panel)
        {
            DrawRotFieldEx(g, panel, panel.ClientRectangle, ShowFieldDv);
        }

        /// <summary>
        /// フィールドの回転ベクトル描画
        /// </summary>
        /// <param name="g"></param>
        /// <param name="panel"></param>
        public void DrawRotFieldEx(Graphics g, Panel panel, Rectangle clientRectangle, FemElement.FieldDV fieldDv)
        {
            if (!isInputDataReady())
            {
                return;
            }
            if (!isOutputDataReady())
            {
                return;
            }
            Size delta;
            Size ofs;
            Size regionSize;
            //getFitDrawRegion(panel, out delta, out ofs, out regionSize);
            getFitDrawRegion(clientRectangle.Width, clientRectangle.Height, out delta, out ofs, out regionSize);
            ofs.Width += clientRectangle.Left;
            ofs.Height += clientRectangle.Top;

            Color drawColor = Color.Gray;
            double min = 0.0;
            double max = 1.0;
            if (fieldDv == FemElement.FieldDV.PoyntingXY)
            {
                drawColor = Color.Green;//Color.YellowGreen;
                min = -MaxPoyntingFValue;
                max = MaxPoyntingFValue;
            }
            else if (fieldDv == FemElement.FieldDV.RotXY)
            {
                drawColor = Color.Red;
                {
                    drawColor = (WaveModeDv == FemSolver.WaveModeDV.TM) ? Color.Blue : Color.Red;
                }
                min = -MaxRotFValue;
                max = MaxRotFValue;
            }
            else
            {
                return;
            }
            foreach (FemElement element in Elements)
            {
                // 回転ベクトル描画
                element.DrawRotField(g, ofs, delta, regionSize, drawColor, fieldDv, min, max);
            }
        }

        /// <summary>
        /// 媒質の境界を描画する
        /// </summary>
        /// <param name="g"></param>
        /// <param name="panel"></param>
        public void DrawMediaB(Graphics g, Panel panel, bool fitFlg = false)
        {
            DrawMediaB(g, panel, panel.ClientRectangle, fitFlg);
        }

        public void DrawMediaB(Graphics g, Panel panel, Rectangle clientRectangle, bool fitFlg = false)
        {
            if (!isInputDataReady())
            {
                return;
            }
            // 線の色
            Color lineColor = Color.Black;
            // 線の太さ
            int lineWidth = 1;
            Size ofs;
            Size delta;
            Size regionSize;
            if (!fitFlg)
            {
                //getDrawRegion(panel, out delta, out ofs, out regionSize);
                getDrawRegion(clientRectangle.Width, clientRectangle.Height, out delta, out ofs, out regionSize);
            }
            else
            {
                //getFitDrawRegion(panel, out delta, out ofs, out regionSize);
                getFitDrawRegion(clientRectangle.Width, clientRectangle.Height, out delta, out ofs, out regionSize);
            }
            ofs.Width += clientRectangle.Left;
            ofs.Height += clientRectangle.Top;

            foreach (string edgeKeyStr in MediaBEdgeList)
            {
                string[] tokens = edgeKeyStr.Split('_');
                System.Diagnostics.Debug.Assert(tokens.Length == 2);
                if (tokens.Length != 2)
                {
                    continue;
                }
                int[] nodeNumbers = { int.Parse(tokens[0]), int.Parse(tokens[1]) };
                double[][] pps = new double[2][]
                {
                    Nodes[nodeNumbers[0] - 1].Coord,
                    Nodes[nodeNumbers[1] - 1].Coord
                };
                Point[] points = new Point[2];
                for (int ino = 0; ino < 2; ino++)
                {
                    points[ino] = new Point();
                    points[ino].X = (int)((double)pps[ino][0] * delta.Width);
                    //points[ino].Y = (int)(regionSize.Height - (double)pps[ino][1] * delta.Height);
                    points[ino].Y = (int)((double)pps[ino][1] * delta.Height);
                    points[ino] += ofs;
                }
                using (Pen pen = new Pen(lineColor, lineWidth))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                    g.DrawLine(pen, points[0], points[1]);
                }
            }
        }

        /// <summary>
        /// フィールド値凡例の初期化
        /// </summary>
        /// <param name="legendPanel"></param>
        public void InitFValueLegend(Panel legendPanel, Label labelFreqValue)
        {
            if (FValueLegendColorPanel != null)
            {
                // 初期化済み
                return;
            }
            const int cnt = LegendColorCnt;

            FValueLegendColorPanel = new Panel();
            FValueLegendColorPanel.Location = new Point(0, 15);
            FValueLegendColorPanel.Size = new Size(legendPanel.Width, 5 + 20 * cnt + 5);
            FValueLegendColorPanel.Paint += new PaintEventHandler(FValueLegendColorPanel_Paint);
            legendPanel.Controls.Add(FValueLegendColorPanel);

            labelFreqValue.Text = "---";
        }

        /// <summary>
        /// フィールド値凡例内カラーマップパネルのペイントイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FValueLegendColorPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            if (ShowFieldDv == FemElement.FieldDV.PoyntingXY || ShowFieldDv == FemElement.FieldDV.RotXY)
            {
                // ベクトル表示時はカラーマップを表示しない
                return;
            }
            // カラーマップを表示する
            drawFValueLegendColormap(g);
            // カラーマップの目盛を表示する
            drawFValueLegendColorScale(g, ShowFieldDv, ShowValueDv);
        }

        /// <summary>
        /// 凡例カラーマップの描画
        /// </summary>
        /// <param name="g"></param>
        private void drawFValueLegendColormap(Graphics g)
        {
            const int cnt = LegendColorCnt;
            const int ofsX = 0;
            const int ofsY = 5;
            const int width = 20;         // カラーマップ領域の幅
            const int height = 20 * cnt;  // カラーマップ領域の高さ

            FValueColorMap.Min = 0;
            FValueColorMap.Max = 1.0;

            for (int y = 0; y < height * cnt; y++)
            {
                double value = (height - y) / (double)height;
                Color backColor = FValueColorMap.GetColor(value);
                using (Brush brush = new SolidBrush(backColor))
                {
                    g.FillRectangle(brush, new Rectangle(ofsX, y + ofsY, width, 1));
                }
            }
        }

        /// <summary>
        /// 凡例カラーマップ目盛の描画
        /// </summary>
        /// <param name="g"></param>
        private void drawFValueLegendColorScale(Graphics g, FemElement.FieldDV fieldDv, FemElement.ValueDV valueDv)
        {
            const int cnt = LegendColorCnt;
            const int ofsX = 22;
            const int ofsY = 0;
            const int height = 20; // 1目盛の高さ
            //double divValue = 1.0 / (double)cnt;
            double min = 0.0;
            double max = 1.0;
            if (fieldDv == FemElement.FieldDV.Field)
            {
                min = MinFValue;
                max = MaxFValue;
            }
            else if (fieldDv == FemElement.FieldDV.RotX || fieldDv == FemElement.FieldDV.RotY)
            {
                min = MinRotFValue;
                max = MaxRotFValue;
            }
            else
            {
                return;
            }
            if (valueDv == FemElement.ValueDV.Abs)
            {
            }
            else
            {
                min = -max;
            }
            double divValue = (max - min) / (double)cnt;

            using (Font font = new Font("MS UI Gothic", 9))
            using (Brush brush = new SolidBrush(FValueLegendColorPanel.ForeColor))
            {
                for (int i = 0; i < cnt + 1; i++)
                {
                    int y = i * height;
                    double value = min + (cnt - i) * divValue;
                    string text = string.Format("{0:E3}", value);
                    g.DrawString(text, font, brush, new Point(ofsX, y + ofsY));
                }
            }
        }

        /// <summary>
        /// フィールド値凡例の更新
        /// </summary>
        /// <param name="legendPanel"></param>
        /// <param name="labelFreqValue"></param>
        public void UpdateFValueLegend(Panel legendPanel, Label labelFreqValue)
        {
            if (Math.Abs(WaveLength) < Constants.PrecisionLowerLimit)
            {
                labelFreqValue.Text = "---";
            }
            else
            {
                labelFreqValue.Text = string.Format("{0:F3}", GetNormalizedFrequency());
            }
            // BUGFIX [次の周波数][前の周波数]ボタンで周波数が遅れて表示される不具合を修正
            labelFreqValue.Refresh();
            legendPanel.Refresh();
        }

        /// <summary>
        /// チャートの色をセットアップ
        /// </summary>
        /// <param name="chart1"></param>
        private void setupChartColor(Chart chart1)
        {
            Color foreColor = chart1.Parent.ForeColor;
            Color backColor = chart1.Parent.BackColor;
            Color lineColor = Color.DarkGray;
            chart1.BackColor = backColor;
            //chart1.ForeColor = foreColor; // 無視される
            chart1.ChartAreas[0].BackColor = backColor;
            chart1.Titles[0].ForeColor = foreColor;
            foreach (Axis axis in chart1.ChartAreas[0].Axes)
            {
                axis.TitleForeColor = foreColor;
                axis.LabelStyle.ForeColor = foreColor;
                axis.LineColor = lineColor;
                axis.MajorGrid.LineColor = lineColor;
                axis.MajorTickMark.LineColor = lineColor;
                //axis.ScaleBreakStyle.LineColor = lineColor;
                //axis.MinorGrid.LineColor = lineColor;
            }
            chart1.Legends[0].BackColor = backColor;
            chart1.Legends[0].ForeColor = foreColor;
        }

        /// <summary>
        /// 反射、透過係数周波数特性グラフの初期化
        /// </summary>
        /// <param name="chart1"></param>
        public void ResetSMatChart(Chart chart1, bool dataClearFlg = true)
        {
            double normalizedFreq1 = FemSolver.GetNormalizedFreq(FirstWaveLength, WaveguideWidth, LatticeA);
            normalizedFreq1 = Math.Round(normalizedFreq1, 2);
            double normalizedFreq2 = FemSolver.GetNormalizedFreq(LastWaveLength, WaveguideWidth, LatticeA);
            normalizedFreq2 = Math.Round(normalizedFreq2, 2);

            // チャート初期化
            setupChartColor(chart1);
            chart1.Titles[0].Text = "散乱係数周波数特性";
            chart1.ChartAreas[0].Axes[0].Title = "a/λ";
            chart1.ChartAreas[0].Axes[1].Title = string.Format("|Si{0}|", IncidentPortNo);
            SetChartFreqRange(chart1, normalizedFreq1, normalizedFreq2);
            //chart1.ChartAreas[0].Axes[1].Minimum = 0.0;
            //chart1.ChartAreas[0].Axes[1].Maximum = 1.0;
            //chart1.ChartAreas[0].Axes[1].Interval = 0.2;
            chart1.Series.Clear();
            if (dataClearFlg)
            {
                SMatChartXValueList.Clear();
                SMatChartYValuesList.Clear();
            }
            if (!isInputDataReady())
            {
                //MessageBox.Show("入力データがセットされていません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // 表示するモード数をデータから判定する
            int portCnt = Ports.Count;
            int[] portModeCntAry = getPortModeCntFromSMatData();

            for (int portIndex = 0; portIndex < portCnt; portIndex++)
            {
                int portModeCnt = portModeCntAry[portIndex];
                if (portModeCnt <= 1)
                {
                    Series series = new Series();
                    series.Name = string.Format("|S{0}{1}|", portIndex + 1, IncidentPortNo);
                    series.ChartType = SeriesChartType.Line;
                    series.BorderDashStyle = ChartDashStyle.Solid;
                    chart1.Series.Add(series);
                }
                else
                {
                    for (int iMode = 0; iMode < portModeCnt; iMode++)
                    {
                        Series series = new Series();
                        series.Name = string.Format("|S{0}({1}){2}|", portIndex + 1, (iMode + 1), IncidentPortNo);
                        series.ChartType = SeriesChartType.Line;
                        series.BorderDashStyle = ChartDashStyle.Solid;
                        chart1.Series.Add(series);
                    }
                }
            }
            // 基本モード以外への電力損失のルート値
            {
                Series series = new Series();
                series.Name = "√|loss|";
                series.ChartType = SeriesChartType.Line;
                series.BorderDashStyle = ChartDashStyle.Dash;
                series.Color = chart1.ChartAreas[0].AxisX.LineColor;//chart1.Parent.ForeColor;
                chart1.Series.Add(series);
            }
            // 計算されたグラフのプロパティ値をAutoに設定
            chart1.ResetAutoValues();
        }

        /// <summary>
        /// チャートの周波数範囲をセットする
        /// </summary>
        /// <param name="chart1"></param>
        public void SetChartFreqRange(Chart chart1, double normalizedFreq1, double normalizedFreq2)
        {
            //chart1.ChartAreas[0].Axes[0].Minimum = 1.0;
            //chart1.ChartAreas[0].Axes[0].Maximum = 2.0;
            //chart1.ChartAreas[0].Axes[0].Minimum = Constants.DefNormalizedFreqRange[0];
            //chart1.ChartAreas[0].Axes[0].Maximum = Constants.DefNormalizedFreqRange[1];
            double minFreq = normalizedFreq1;
            //minFreq = Math.Floor(minFreq * 10.0) * 0.1;
            minFreq = Math.Floor(minFreq * 100.0) / 100.0;
            double maxFreq = normalizedFreq2;
            //maxFreq = Math.Ceiling(maxFreq * 10.0) * 0.1;
            maxFreq = Math.Ceiling(maxFreq * 100.0) / 100.0;
            chart1.ChartAreas[0].Axes[0].Minimum = minFreq;
            chart1.ChartAreas[0].Axes[0].Maximum = maxFreq;
            //chart1.ChartAreas[0].Axes[0].Interval = (maxFreq - minFreq >= 0.9)? 0.2 : 0.1;
            double range = maxFreq - minFreq;
            double interval = 0.2;
            //interval = range / 5 ;
            interval = range / 6;
            if (1.0 <= interval)
            {
                interval = Math.Round(interval);
            }
            else if (0.1 <= interval && interval < 1.0)
            {
                interval = Math.Round(interval/0.1) * 0.1;
            }
            else if (0.01 <= interval && interval < 0.1)
            {
                interval = Math.Round(interval/0.01) * 0.01;
            }
            else if (0.001 <= interval && interval < 0.01)
            {
                interval = Math.Round(interval / 0.001) * 0.001;
            }
            else if (interval < 0.001)
            {
                interval = Math.Round(interval / 0.0001) * 0.0001;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            chart1.ChartAreas[0].Axes[0].Interval = interval;
        }

        /// <summary>
        /// 散乱係数周波数特性チャートのY軸最大値を調整する
        /// </summary>
        /// <param name="chart1"></param>
        public void AdjustSMatChartYAxisMax(Chart chart1)
        {
            double maxValue = double.MinValue;
            double minValue = double.MaxValue;
            foreach (Series series in chart1.Series)
            {
                foreach (DataPoint dataPoint in series.Points)
                {
                    double workMax = dataPoint.YValues.Max();
                    if (workMax > maxValue)
                    {
                        maxValue = workMax;
                    }
                    double workMin = dataPoint.YValues.Min();
                    if (workMin < minValue)
                    {
                        minValue = workMin;
                    }
                }
            }
            double defMaxValue = 1.0;
            double defMinValue = 0.0;
            if (IsSMatChartLogarithmic)
            {
                defMaxValue = 0;
                defMinValue = -100;
            }
            if (IsSMatChartLogarithmic)
            {
                chart1.ChartAreas[0].Axes[1].Maximum = 0;
                if (minValue > -10)
                {
                    chart1.ChartAreas[0].Axes[1].Minimum = -10;
                }
                else
                {
                    chart1.ChartAreas[0].Axes[1].Minimum = - ((int)(-minValue / 10) + 1) * 10;
                }
                if (minValue >= -5)
                {
                    chart1.ChartAreas[0].Axes[1].Interval = 1.0;
                }
                else if (minValue >= -10)
                {
                    chart1.ChartAreas[0].Axes[1].Interval = 2.0;
                }
                else if (minValue >= -20)
                {
                    chart1.ChartAreas[0].Axes[1].Interval = 4.0;
                }
                else if (minValue >= -30)
                {
                    chart1.ChartAreas[0].Axes[1].Interval = 5.0;
                }
                else
                {
                    chart1.ChartAreas[0].Axes[1].Interval = 10.0;
                }
            }
            else
            {
                if (maxValue <= defMaxValue)
                {
                    // 散乱係数が1.0以下の正常な場合は、最大値を1.0固定で指定する
                    chart1.ChartAreas[0].Axes[1].Maximum = defMaxValue;
                }
                else
                {
                    chart1.ChartAreas[0].Axes[1].Maximum = maxValue;
                }
                // エバネセント波の電力計算ミスを見つけるために追加
                if (minValue >= defMinValue)
                {
                    // 散乱係数が0以上のときは、最小値を0固定で指定する
                    chart1.ChartAreas[0].Axes[1].Minimum = defMinValue;
                }
                else
                {
                    chart1.ChartAreas[0].Axes[1].Minimum = minValue;
                }
                if (maxValue >= 2.0 || minValue <= -2.0)
                {
                    chart1.ChartAreas[0].Axes[1].Interval = maxValue / 5.0;
                }
                else
                {
                    chart1.ChartAreas[0].Axes[1].Interval = 0.2;
                }
            }
        }

        /// <summary>
        /// ポート毎のモード数をデータから取得する(散乱係数データ)
        /// </summary>
        /// <returns></returns>
        private int[] getPortModeCntFromSMatData()
        {
            // 表示するモード数をデータから判定する
            int portCnt = Ports.Count;
            int[] portModeCntAry = new int[portCnt];

            for (int portIndex = 0; portIndex < portCnt; portIndex++)
            {
                portModeCntAry[portIndex] = 1;// 初期化
            }
            int freqCnt = SMatChartXValueList.Count;
            for (int freqIndex = 0; freqIndex < freqCnt; freqIndex++)
            {
                for (int portIndex = 0; portIndex < Ports.Count; portIndex++)
                {
                    int workModeCnt = SMatChartYValuesList[freqIndex][portIndex].Length;
                    if (workModeCnt > portModeCntAry[portIndex])
                    {
                        portModeCntAry[portIndex] = workModeCnt;
                    }
                }
            }
            return portModeCntAry;
        }

        /// <summary>
        /// ポート毎のモード数をデータから取得する(伝搬定数データ)
        /// </summary>
        /// <returns></returns>
        private int[] getPortModeCntFromBetaData()
        {
            // 表示するモード数をデータから判定する
            int portCnt = Ports.Count;
            int[] portModeCntAry = new int[portCnt];

            for (int portIndex = 0; portIndex < portCnt; portIndex++)
            {
                portModeCntAry[portIndex] = 1;// 初期化
            }
            int freqCnt = BetaChartXValueList.Count;
            for (int freqIndex = 0; freqIndex < freqCnt; freqIndex++)
            {
                for (int portIndex = 0; portIndex < Ports.Count; portIndex++)
                {
                    int workModeCnt = BetaChartYValuesList[freqIndex][portIndex].Length;
                    if (workModeCnt > portModeCntAry[portIndex])
                    {
                        portModeCntAry[portIndex] = workModeCnt;
                    }
                }
            }
            return portModeCntAry;
        }

        /// <summary>
        /// 反射、透過係数周波数特性グラフに計算結果を追加
        /// </summary>
        /// <param name="chart1"></param>
        public void AddScatterMatrixToChart(Chart chart1)
        {
            if (!isInputDataReady())
            {
                //MessageBox.Show("入力データがセットされていません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!isOutputDataReady())
            {
                //MessageBox.Show("出力データがセットされていません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // 波数
            double k0 = 2.0 * Constants.pi / WaveLength;
            // 角周波数
            double omega = k0 * Constants.c0;

            double chartXValue = GetNormalizedFrequency();
            double totalPower = 0.0;
            IList<double[]> chartYValues = new List<double[]>();
            bool isDataValid = false;
            for (int portIndex = 0; portIndex < Ports.Count; portIndex++)
            {
                Complex[] portScatterVec = ScatterVecList[portIndex];
                int modeCnt_0 = portScatterVec.Length;
                int modeCnt = 0; // 伝搬モードのみ抽出
                for (int iMode = 0; iMode < modeCnt_0; iMode++)
                {
                    Complex beta = EigenValuesList[portIndex][iMode];
                    if (Math.Abs(beta.Imaginary / k0) >= Constants.PrecisionLowerLimit)
                    {
                        break;
                    }
                    else if (Math.Abs(beta.Real / k0) >= Constants.PrecisionLowerLimit)
                    {
                        modeCnt++;
                    }
                    else
                    {
                        break;
                    }
                }
                double[] chartPortYValues = new double[modeCnt];
                for (int iMode = 0; iMode < modeCnt; iMode++)
                {
                    Complex sim10 = portScatterVec[iMode];
                    double sim10Abs = Complex.Abs(sim10);
                    Complex beta = EigenValuesList[portIndex][iMode];
                    if (Math.Abs(beta.Imaginary / k0) >= Constants.PrecisionLowerLimit)
                    {
                        //減衰モード
                        sim10Abs = InvalidValueForSMat;
                    }
                    else if (Math.Abs(beta.Real / k0) >= Constants.PrecisionLowerLimit)
                    {
                        //伝搬モード
                        isDataValid = true;
                    }
                    chartPortYValues[iMode] = sim10Abs;
                    if (Math.Abs(sim10Abs - InvalidValueForSMat) >= Constants.PrecisionLowerLimit)
                    {
                        totalPower += sim10Abs * sim10Abs;
                    }
                }
                chartYValues.Add(chartPortYValues);
            }
            // トータル電力を最後に追加
            double totalPowerForChart = isDataValid ? totalPower : InvalidValueForSMat;
            chartYValues.Add(new double[] { totalPowerForChart });

            // X, Y軸の値のリストを格納(再表示用)
            SMatChartXValueList.Add(chartXValue);
            SMatChartYValuesList.Add(chartYValues);

            // チャートにデータを追加する
            setSMatChartYValuesToChart(chart1);
        }

        /// <summary>
        /// 散乱係数チャートにデータをセットする
        /// </summary>
        /// <param name="chart1"></param>
        private void setSMatChartYValuesToChart(Chart chart1)
        {
            // チャートの表示項目をリセット(データはクリアしない)
            ResetSMatChart(chart1, false); // dataClearFlg : false
            
            double defMaxValue = 1.0;
            double defMinValue = 0.0;
            if (IsSMatChartLogarithmic)
            {
                defMaxValue = 0;
                defMinValue = -100;
                chart1.ChartAreas[0].Axes[1].Maximum = 0;
                chart1.ChartAreas[0].Axes[1].Minimum = defMinValue;
            }
            int freqCnt = SMatChartXValueList.Count;
            int portCnt = Ports.Count;
            int[] portModeCntAry = getPortModeCntFromSMatData();

            for (int freqIndex = 0; freqIndex < freqCnt; freqIndex++)
            {
                double chartXValue = SMatChartXValueList[freqIndex];
                IList<double[]> chartYValues = SMatChartYValuesList[freqIndex];
                int dataIndex = 0;
                //for (int portIndex = 0; portIndex < portCnt; portIndex++)
                for (int portIndex = 0; portIndex < (portCnt + 1); portIndex++) // 最後は損失表示
                {
                    double[] chartPortYValues = chartYValues[portIndex];
                    int portModeCnt = 1;
                    if (portIndex < portCnt)
                    {
                        portModeCnt = portModeCntAry[portIndex];
                    }
                    else
                    {
                        // 最後(合計電力が格納されている)
                        portModeCnt = 1;
                    }
                    for (int iMode = 0; iMode < portModeCnt; iMode++)
                    {
                        double plotValue = InvalidValueForSMat;
                        Series series = chart1.Series[dataIndex];
                        if (iMode < chartPortYValues.Length)
                        {
                            plotValue = chartPortYValues[iMode];
                            if (portIndex == portCnt)
                            {
                                // 最後は損失を格納する
                                if (plotValue != InvalidValueForSMat)
                                {
                                    plotValue = Math.Sqrt(1.0 - plotValue);
                                }
                            }
                            if (IsSMatChartLogarithmic)
                            {
                                plotValue = 20.0 * Math.Log10(plotValue);
                            }
                        }
                        if (plotValue >= defMinValue && plotValue <= defMaxValue)
                        {
                            series.Points.AddXY(chartXValue, plotValue);
                        }
                        dataIndex++;
                    }
                }
            }
            AdjustSMatChartYAxisMax(chart1);
        }

        /// <summary>
        /// 散乱係数チャートの対数表示するかを設定する
        /// </summary>
        /// <param name="chart1"></param>
        /// <param name="isLogaritmic"></param>
        public void SetSMatChartLogarithmic(Chart chart1, bool isLogaritmic)
        {
            IsSMatChartLogarithmic = isLogaritmic;

            setSMatChartYValuesToChart(chart1);
            if (IsAutoCalc)
            {
                // チャートの表示をポイント表示にする
                ShowChartDataLabel(chart1);
            }
        }

        /// <summary>
        /// 伝搬定数分散特性(グラフの初期化
        /// </summary>
        /// <param name="chart1"></param>
        public void ResetEigenValueChart(Chart chart1, bool isDataClear = true)
        {
            double normalizedFreq1 = FemSolver.GetNormalizedFreq(FirstWaveLength, WaveguideWidth, LatticeA);
            normalizedFreq1 = Math.Round(normalizedFreq1, 2);
            double normalizedFreq2 = FemSolver.GetNormalizedFreq(LastWaveLength, WaveguideWidth, LatticeA);
            normalizedFreq2 = Math.Round(normalizedFreq2, 2);

            if (isDataClear)
            {
                BetaChartXValueList.Clear();
                BetaChartYValuesList.Clear();
            }

            // 表示モード数
            int[] portModeCntAry = getPortModeCntFromBetaData();

            // チャート初期化
            setupChartColor(chart1);
            chart1.Titles[0].Text = "規格化伝搬定数周波数特性";
            chart1.ChartAreas[0].Axes[0].Title = "a/λ";
            chart1.ChartAreas[0].Axes[1].Title = "β/ k0";
            SetChartFreqRange(chart1, normalizedFreq1, normalizedFreq2);
            chart1.ChartAreas[0].Axes[1].Minimum = 0.0;
            //chart1.ChartAreas[0].Axes[1].Maximum = 1.0; // 誘電体比誘電率の最大となるので可変
            //chart1.ChartAreas[0].Axes[1].Interval = 0.2;
            chart1.Series.Clear();
            if (!isInputDataReady())
            {
                //MessageBox.Show("入力データがセットされていません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            for (int portIndex = 0; portIndex < Ports.Count; portIndex++)
            {
                for (int modeIndex = 0; modeIndex < portModeCntAry[portIndex]; modeIndex++)
                {
                    Series series = new Series();
                    series.Name = string.Format(((WaveModeDv == FemSolver.WaveModeDV.TM) ? "TM({0}) at {1}" : "TE({0}) at {1}"), modeIndex + 1, portIndex + 1);
                    series.ChartType = SeriesChartType.Line;
                    chart1.Series.Add(series);
                }
            }
            // 計算されたグラフのプロパティ値をAutoに設定
            chart1.ResetAutoValues();
        }

        /// <summary>
        /// 伝搬定数分散特性グラフに計算結果を追加
        /// </summary>
        /// <param name="chart1"></param>
        public void AddEigenValueToChart(Chart chart1)
        {
            // 波数
            double k0 = 2.0 * Constants.pi / WaveLength;
            // 角周波数
            double omega = k0 * Constants.c0;

            if (!isInputDataReady())
            {
                //MessageBox.Show("入力データがセットされていません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!isOutputDataReady())
            {
                //MessageBox.Show("出力データがセットされていません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // データを追加
            double chartXValue = GetNormalizedFrequency();
            IList<double[]> chartYValues = new List<double[]>();
            for (int portIndex = 0; portIndex < Ports.Count; portIndex++)
            {
                Complex[] eigenValueVec = EigenValuesList[portIndex];
                int modeCnt_0 = eigenValueVec.Length;
                int modeCnt = 0; // 伝搬モードのみ抽出
                for (int iMode = 0; iMode < modeCnt_0; iMode++)
                {
                    Complex beta = EigenValuesList[portIndex][iMode];
                    if (Math.Abs(beta.Imaginary / k0) >= Constants.PrecisionLowerLimit)
                    {
                        break;
                    }
                    else if (Math.Abs(beta.Real / k0) >= Constants.PrecisionLowerLimit)
                    {
                        modeCnt++;
                    }
                    else
                    {
                        break;
                    }
                }
                double[] chartPortYValues = new double[modeCnt];
                for (int iMode = 0; iMode < modeCnt; iMode++)
                {
                    Complex beta = eigenValueVec[iMode];
                    double betaReal = beta.Real;
                    if (Math.Abs(beta.Imaginary / k0) >= Constants.PrecisionLowerLimit)
                    {
                        //減衰モード
                        betaReal = InvalidValueForSMat;
                    }
                    else if (Math.Abs(beta.Real / k0) >= Constants.PrecisionLowerLimit)
                    {
                        //伝搬モード
                    }
                    chartPortYValues[iMode] = betaReal / k0;
                }
                chartYValues.Add(chartPortYValues);
            }
            BetaChartXValueList.Add(chartXValue);
            BetaChartYValuesList.Add(chartYValues);

            // 伝搬定数チャートにデータをセットする
            setBetaChartYValuesToChart(chart1);
        }

        /// <summary>
        /// 伝搬定数チャートにデータをセットする
        /// </summary>
        /// <param name="chart1"></param>
        private void setBetaChartYValuesToChart(Chart chart1)
        {
            // チャートの表示項目をリセット(データはクリアしない)
            ResetEigenValueChart(chart1, false); // dataClearFlg : false

            // チャートへデータをセットする
            int[] portModeCntAry = getPortModeCntFromBetaData();
            int freqCnt = BetaChartXValueList.Count;
            int portCnt = Ports.Count;
            for (int freqIndex =0; freqIndex < freqCnt; freqIndex++)
            {
                double chartXValue = BetaChartXValueList[freqIndex];
                IList<double[]> chartYValues = BetaChartYValuesList[freqIndex];
                int dataIndex = 0;
                for (int portIndex = 0; portIndex < portCnt; portIndex++)
                {
                    double[] portChartYValues = chartYValues[portIndex];
                    int modeCnt = portModeCntAry[portIndex];
                    for (int iMode = 0; iMode < modeCnt; iMode++)
                    {
                        Series series = chart1.Series[dataIndex];
                        double chartYValue = 0.0;
                        if (iMode < portChartYValues.Length)
                        {
                            chartYValue = portChartYValues[iMode];
                        }
                        if (chartYValue > chart1.ChartAreas[0].Axes[1].Minimum)
                        {
                            series.Points.AddXY(chartXValue, chartYValue);
                        }
                        dataIndex++;
                    }
                }
            }
        }

        public void SetEigenVecToChart(Chart chart1)
        {
            // 波数
            double k0 = 2.0 * Constants.pi / WaveLength;
            // 角周波数
            double omega = k0 * Constants.c0;

            int[] portModeCntAry = getPortModeCntFromBetaData();

            // チャート初期化
            setupChartColor(chart1);
            chart1.Titles[0].Text = "固有モードEz実部、虚部の分布 (a/λ = " + (isInputDataReady()? string.Format("{0:F2}", GetNormalizedFrequency()) : "---") + ")";
            chart1.ChartAreas[0].Axes[0].Title = "x / W";
            chart1.ChartAreas[0].Axes[1].Title = "Ez";
            chart1.ChartAreas[0].Axes[0].Minimum = 0.0;
            chart1.ChartAreas[0].Axes[0].Maximum = 1.0;
            chart1.ChartAreas[0].Axes[0].Interval = 0.2;
            chart1.ChartAreas[0].Axes[1].Minimum = -1.0;
            chart1.ChartAreas[0].Axes[1].Maximum = 1.0;
            chart1.ChartAreas[0].Axes[1].Interval = 0.2;
            chart1.Series.Clear();
            if (!isInputDataReady())
            {
                //MessageBox.Show("入力データがセットされていません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            for (int portIndex = 0; portIndex < Ports.Count; portIndex++)
            {
                for (int iMode = 0; iMode < portModeCntAry[portIndex]; iMode++)
                {
                    string modeDescr = "---モード";
                    if (isOutputDataReady())
                    {
                        Complex beta = 0.0;
                        if (iMode < EigenValuesList[portIndex].Length)
                        {
                            beta = EigenValuesList[portIndex][iMode];
                        }
                        if (Math.Abs(beta.Imaginary / k0) >= Constants.PrecisionLowerLimit)
                        {
                            modeDescr = "減衰モード";
                        }
                        else if (Math.Abs(beta.Real / k0) >= Constants.PrecisionLowerLimit)
                        {
                            modeDescr = "伝搬モード";
                        }
                    }

                    Series series;
                    series = new Series();
                    series.Name = string.Format(((WaveModeDv == FemSolver.WaveModeDV.TM) ? "TM({0}) 実部 at {1}" : "TE({0}) 実部 at {1}")
                        + Environment.NewLine + modeDescr, iMode + 1, portIndex + 1);
                    series.ChartType = SeriesChartType.Line;
                    //series.MarkerStyle = MarkerStyle.Square;
                    series.BorderDashStyle = ChartDashStyle.Solid;
                    chart1.Series.Add(series);
                    series = new Series();
                    series.Name = string.Format(((WaveModeDv == FemSolver.WaveModeDV.TM) ? "TM({0}) 虚部 at {1}" : "TE({0}) 虚部 at {1}")
                        + Environment.NewLine + modeDescr, iMode + 1, portIndex + 1);
                    series.ChartType = SeriesChartType.Line;
                    //series.MarkerStyle = MarkerStyle.Cross;
                    series.BorderDashStyle = ChartDashStyle.Dash;
                    chart1.Series.Add(series);
                }
            }
            // 計算されたグラフのプロパティ値をAutoに設定
            chart1.ResetAutoValues();

            if (!isInputDataReady())
            {
                //MessageBox.Show("入力データがセットされていません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!isOutputDataReady())
            {
                //MessageBox.Show("出力データがセットされていません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            int dataIndex = 0;
            for (int portIndex = 0; portIndex < Ports.Count; portIndex++)
            {
                IList<int> portNodes = Ports[portIndex];
                Complex[,] eigenVecs = EigenVecsList[portIndex];
                int nodeCnt = eigenVecs.GetLength(1);
                for (int iMode = 0; iMode < portModeCntAry[portIndex]; iMode++)
                {
                    Complex beta = 0.0;
                    if (iMode < EigenValuesList[portIndex].Length)
                    {
                        beta = EigenValuesList[portIndex][iMode];
                    }
                    /*
                    if (Math.Abs(beta.Imaginary/k0) >= Constants.PrecisionLowerLimit)
                    {
                        // 減衰モードは除外
                        continue;
                    }
                    */

                    double maxValue = 0.0;
                    for (int ino = 0; ino < nodeCnt; ino++)
                    {
                        Complex cval = 0.0;
                        if (iMode < eigenVecs.GetLength(0))
                        {
                            cval = eigenVecs[iMode, ino];
                        }
                        double abs = cval.Magnitude;
                        if (abs > maxValue)
                        {
                            maxValue = abs;
                        }
                    }
                    Series seriesReal = chart1.Series[dataIndex];
                    Series seriesImag = chart1.Series[dataIndex + 1];
                    {
                        int ino = 0;  // 強制境界を除いた節点のインデックス
                        for (int inoB = 0; inoB < portNodes.Count; inoB++)
                        {
                            int nodeNumber = portNodes[inoB];
                            // 正確には座標を取ってくる必要があるが等間隔が保障されていれば、下記で規格化された位置は求まる
                            double x0;
                            x0 = inoB / (double)(portNodes.Count - 1);
                            if (ForceNodes.Contains(nodeNumber))
                            {
                                seriesReal.Points.AddXY(x0, 0.0); // 実数部
                                seriesImag.Points.AddXY(x0, 0.0); // 虚数部
                            }
                            else if (Math.Abs(maxValue) < Constants.PrecisionLowerLimit)
                            {
                                seriesReal.Points.AddXY(x0, 0.0); // 実数部
                                seriesImag.Points.AddXY(x0, 0.0); // 虚数部
                                ino++;
                            }
                            else
                            {
                                Complex cval = eigenVecs[iMode, ino];
                                double real = cval.Real / maxValue;
                                double imag = cval.Imaginary / maxValue;
                                seriesReal.Points.AddXY(x0, real); // 実数部
                                seriesImag.Points.AddXY(x0, imag); // 虚数部
                                ino++;
                            }
                        }
                    }
                    // 実数部と虚数部の2データ分進める
                    dataIndex += 2;
                }
            }
        }

        /// <summary>
        /// チャートのデータをラベル表示する(自動計算モード用)
        /// </summary>
        /// <param name="chart1"></param>
        public void ShowChartDataLabel(Chart chart1)
        {
            foreach (Series series in chart1.Series)
            {
                series.ChartType = SeriesChartType.Point;
                series.Label = "#VALY{N4}";
            }
        }

        public double GetWaveLength()
        {
            return WaveLength;
        }

        public double GetWaveGuideWidth()
        {
            return WaveguideWidth;
        }

        public double GetNormalizedFrequency()
        {
            if (!isInputDataReady())
            {
                return 0.0;
            }
            return FemSolver.GetNormalizedFreq(WaveLength, WaveguideWidth, LatticeA);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 周期構造導波路固有モードのデータを読み込む
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="freqNo"></param>
        /// <returns></returns>
        public bool LoadOutputPeriodic(string filename, int freqNo, int modeIndex)
        {
            // 界分布をクリアする
            clearEigenFieldValueOfElements();

            if (!isInputDataReady())
            {
                return false;
            }
            if (!isOutputDataReady())
            {
                return false;
            }
            int portCnt = Ports.Count;
            if (portCnt == 0)
            {
                return false;
            }

            // 最大モード数を取得する
            // 既にロード済みの伝搬定数リストからモードの数を取得する
            int modeCnt = GetMaxModeCnt();
            if (modeCnt <= modeIndex)
            {
                return false;
            }

            IList<Dictionary<int, int>> toNodePeriodicList = new List<Dictionary<int, int>>();
            KrdLab.clapack.Complex[][][] eigenVecsPeriodicList = new KrdLab.clapack.Complex[portCnt][][];
            for (int portIndex = 0; portIndex < portCnt; portIndex++)
            {
                // 周期構造導波路固有モード出力ファイルから読み込む
                double dummy_waveLength = 0.0;
                IList<int> nodePeriodic = null;
                Dictionary<int, int> toNodePeriodic = null;
                IList<double[]> coordsPeriodic = null;
                KrdLab.clapack.Complex[] dummy_eigenValues = null;
                KrdLab.clapack.Complex[][] eigenVecsPeriodic = null;
                bool ret = FemOutputPeriodicDatFile.LoadFromFile(
                    filename,
                    freqNo,
                    (portIndex + 1),
                    out dummy_waveLength,
                    out nodePeriodic,
                    out toNodePeriodic,
                    out coordsPeriodic,
                    out dummy_eigenValues,
                    out eigenVecsPeriodic
                    );
                if (!ret)
                {
                    return false;
                }

                if (Math.Abs(dummy_waveLength - WaveLength) >= Constants.PrecisionLowerLimit)
                {
                    return false;
                }
                System.Diagnostics.Debug.Assert(Math.Abs(dummy_waveLength - WaveLength) < Constants.PrecisionLowerLimit);

                // 格納
                toNodePeriodicList.Add(toNodePeriodic);
                eigenVecsPeriodicList[portIndex] = eigenVecsPeriodic;
            }

            // 要素にフィールド値をセットする
            setupEigenFieldValueToElements(toNodePeriodicList, eigenVecsPeriodicList, modeIndex);

            return true;
        }

        /// <summary>
        /// 最大モード数を取得する
        /// </summary>
        public int GetMaxModeCnt()
        {
            if (Ports == null || EigenValuesList == null)
            {
                return 0;
            }
            if (EigenValuesList.Count != Ports.Count)
            {
                // 不正な状態
                return 0;
            }
            int modeCnt = 0;
            int portCnt = Ports.Count;
            for (int portIndex = 0; portIndex < portCnt; portIndex++)
            {
                int portModeCnt = EigenValuesList[portIndex].Length;
                if (modeCnt < portModeCnt)
                {
                    modeCnt = portModeCnt;
                }
            }
            return modeCnt;
        }

        /// <summary>
        /// 固有モードの界をクリアする
        /// </summary>
        private void clearEigenFieldValueOfElements()
        {
            MinEigenFValue = 0.0;
            MaxEigenFValue = 0.0;
            if (Elements == null || Elements.Length == 0)
            {
                return;
            }
            foreach (FemElement element in Elements)
            {
                element.ClearEigenFieldValue();
            }
        }

        /// <summary>
        /// 固有モードの界を要素に設定する
        /// </summary>
        /// <param name="toNodePeriodicList"></param>
        /// <param name="eigenVecsPeriodicList"></param>
        private void setupEigenFieldValueToElements(IList<Dictionary<int, int>> toNodePeriodicList, Complex[][][] eigenVecsPeriodicList, int modeIndex)
        {
            MinEigenFValue = 0.0;
            MaxEigenFValue = 0.0;

            if (Math.Abs(WaveLength) < Constants.PrecisionLowerLimit)
            {
                return;
            }
            if (Elements == null || Elements.Length == 0)
            {
                return;
            }

            // ポート数
            int portCnt = toNodePeriodicList.Count;

            // フィールド値の絶対値の最小、最大
            double minEigenFValue = double.MaxValue;
            double maxEigenFValue = double.MinValue;

            // 要素リストにフィールド値を格納
            foreach (FemElement element in Elements)
            {
                // 値を格納
                for (int portIndex = 0; portIndex < portCnt; portIndex++)
                {
                    Dictionary<int, int> toNodePeriodic = toNodePeriodicList[portIndex];
                    KrdLab.clapack.Complex[][] eigenVecsPeriodic = eigenVecsPeriodicList[portIndex];
                    // モードは指定されたモードを設定する
                    if (modeIndex < eigenVecsPeriodic.Length)
                    {
                        // 値を設定する
                        KrdLab.clapack.Complex[] eigenVec = eigenVecsPeriodic[modeIndex];
                        element.SetEigenFieldValueFromAllValues(eigenVec, toNodePeriodic);
                    }
                }

                // 最大、最小
                int nno = element.NodeNumbers.Length;
                for (int ino = 0; ino < nno; ino++)
                {
                    Complex fValue = element.getEigenFValue(ino);
                    double fValueAbs = fValue.Magnitude;
                    if (fValueAbs > maxEigenFValue)
                    {
                        maxEigenFValue = fValueAbs;
                    }
                    if (fValueAbs < minEigenFValue)
                    {
                        minEigenFValue = fValueAbs;
                    }
                }
            }

            MinEigenFValue = minEigenFValue;
            MaxEigenFValue = maxEigenFValue;
        }

        /// <summary>
        /// フィールド値等高線図描画
        /// </summary>
        /// <param name="g"></param>
        /// <param name="panel"></param>
        public void DrawEigenField(Graphics g, Panel panel, Rectangle clientRectangle, FemElement.ValueDV valueDv)
        {
            if (!isInputDataReady())
            {
                return;
            }
            if (!isOutputDataReady())
            {
                return;
            }
            Size delta;
            Size ofs;
            Size regionSize;
            //getFitDrawRegion(panel, out delta, out ofs, out regionSize);
            getFitDrawRegion(clientRectangle.Width, clientRectangle.Height, out delta, out ofs, out regionSize);
            ofs.Width += clientRectangle.Left;
            ofs.Height += clientRectangle.Top;

            double min = MinEigenFValue;
            double max = MaxEigenFValue;

            // カラーマップに最小、最大を設定
            if (valueDv == FemElement.ValueDV.Real || valueDv == FemElement.ValueDV.Imaginary)
            {
                EigenFValueColorMap.Min = -max;
                EigenFValueColorMap.Max = max;
            }
            else
            {
                // 既定値は絶対値で処理する
                EigenFValueColorMap.Min = 0.0;
                EigenFValueColorMap.Max = max;
            }

            foreach (FemElement element in Elements)
            {
                // 等高線描画(固有モード分布)
                element.DrawEigenField(g, ofs, delta, regionSize, valueDv, EigenFValueColorMap);
            }
        }

    }
}
