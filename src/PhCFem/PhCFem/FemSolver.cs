using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
//using System.Numerics; // Complex
using KrdLab.clapack; // KrdLab.clapack.Complex
//using System.Text.RegularExpressions;
using MyUtilLib.Matrix;

namespace PhCFem
{
    /// <summary>
    /// 解析機
    /// </summary>
    class FemSolver
    {
        ////////////////////////////////////////////////////////////////////////
        // 定数
        ////////////////////////////////////////////////////////////////////////
        private const double pi = Constants.pi;
        private const double c0 = Constants.c0;
        private const double mu0 = Constants.mu0;
        private const double eps0 = Constants.eps0;
        /// <summary>
        /// 考慮モード数
        /// </summary>
        private const int MaxModeCnt = Constants.MaxModeCount;
        /// <summary>
        /// 導波路の幅既定値  規格化周波数が定義できるように初期値を設定
        /// </summary>
        public const double DefWaveguideWidth = 1000.0; // ありえない幅
        /// <summary>
        /// 入射モードインデックス
        /// </summary>
        private const int IncidentModeIndex = 0;

        ////////////////////////////////////////////////////////////////////////
        // 型
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 波のモード区分
        /// </summary>
        public enum WaveModeDV { TE, TM };
        /// <summary>
        /// 境界条件区分
        /// </summary>
        public enum BoundaryDV { ElectricWall, MagneticWall };

        ////////////////////////////////////////////////////////////////////////
        // フィールド
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 節点リスト
        /// </summary>
        private IList<FemNode> Nodes = new List<FemNode>();
        /// <summary>
        /// 要素リスト
        /// </summary>
        private IList<FemElement> Elements = new List<FemElement>();
        /// <summary>
        /// ポートリスト
        ///   各ポートのリスト要素は節点のリスト
        /// </summary>
        private IList<IList<int>> Ports = new List<IList<int>>();
        /// <summary>
        /// 強制境界節点リスト
        /// </summary>
        private IList<int> ForceBCNodes = new List<int>();
        /// <summary>
        /// 領域全体の強制境界節点番号ハッシュ
        /// </summary>
        private Dictionary<int, bool> ForceNodeNumberH = new Dictionary<int, bool>();
        /// <summary>
        ///  周期構造領域の要素番号リスト(ポート毎)
        /// </summary>
        private IList<IList<uint>> ElemNoPeriodicList = new List<IList<uint>>();
        /// <summary>
        ///  周期構造領域境界の節点番号リスト(ポート毎 - 境界毎)
        /// </summary>
        private IList<IList<IList<int>>> NodePeriodicBList = new List<IList<IList<int>>>();
        /// <summary>
        /// 周期構造領域欠陥部の節点番号リスト(ポート毎)
        /// </summary>
        private IList<IList<int>> DefectNodePeriodicList = new List<IList<int>>();

        /// <summary>
        /// 入射ポート番号
        /// </summary>
        private int IncidentPortNo = 1;
        /// <summary>
        /// 媒質リスト
        /// </summary>
        private MediaInfo[] Medias = new MediaInfo[Constants.MaxMediaCount];
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
        /// 計算する波のモード区分
        /// </summary>
        public WaveModeDV WaveModeDv
        {
            get;
            set;
        }
        /// <summary>
        /// 境界区分
        /// </summary>
        public BoundaryDV BoundaryDv
        {
            get;
            set;
        }

        /// <summary>
        /// 設定された要素形状区分(これはSolver内部では使用しない。要素形状は要素分割データから判断する)
        /// </summary>
        public Constants.FemElementShapeDV ElemShapeDvToBeSet
        {
            get;
            set;
        }
        /// <summary>
        /// 設定された要素の補間次数(これはSolver内部では使用しない。補間次数は要素分割データから判断する)
        /// </summary>
        public int ElemOrderToBeSet
        {
            get;
            set;
        }

        /// <summary>
        /// 辺と要素番号の対応マップ
        /// </summary>
        private Dictionary<string, IList<int>> EdgeToElementNoH = new Dictionary<string, IList<int>>();
        /// <summary>
        /// 導波路の幅
        ///   H面解析の場合は、導波管の幅
        ///   座標から自動計算される
        /// </summary>
        private double WaveguideWidth;
        /// <summary>
        /// 格子定数
        /// </summary>
        private double LatticeA = 1.0;
        /// <summary>
        /// 計算中止された？
        /// </summary>
        public bool IsCalcAborted
        {
            get;
            set;
        }

        /// <summary>
        /// clapack使用時のFEM行列
        /// </summary>
        private MyComplexMatrix FemMat = null;
        /// <summary>
        /// ソート済み節点番号リスト
        /// </summary>
        private IList<int> SortedNodes = null;
        /// <summary>
        /// FEM行列の非０要素パターン
        /// </summary>
        private bool[,] FemMatPattern = null;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FemSolver()
        {
            init();
        }

        /// <summary>
        /// 入力データの初期化
        /// </summary>
        private void init()
        {
            IsCalcAborted = false;
            Nodes.Clear();
            Elements.Clear();
            foreach (IList<int> portNodes in Ports)
            {
                portNodes.Clear();
            }
            Ports.Clear();
            ForceBCNodes.Clear();
            ForceNodeNumberH.Clear();
            ElemNoPeriodicList.Clear();
            NodePeriodicBList.Clear();
            DefectNodePeriodicList.Clear();
            IncidentPortNo = 1;
            Color[] workColorList = { CadLogic.VacumnBackColor, CadLogic.RodBackColor };
            for (int i = 0; i < Medias.Length; i++)
            {
                MediaInfo media = new MediaInfo();
                media.BackColor = workColorList[i];
                Medias[i] = media;
            }
            EdgeToElementNoH.Clear();
            //WaveguideWidth = 0.0;
            WaveguideWidth = DefWaveguideWidth;  // 規格化周波数が定義できるように初期値を設定
            FirstWaveLength = 0.0;
            LastWaveLength = 0.0;
            CalcFreqCnt = 0;
            WaveModeDv = Constants.DefWaveModeDv;
            BoundaryDv = BoundaryDV.ElectricWall;
            ElemShapeDvToBeSet = Constants.DefElemShapeDv;
            ElemOrderToBeSet = Constants.DefElementOrder;
        }

        /// <summary>
        /// 入力データの初期化
        /// </summary>
        public void InitData()
        {
            init();
        }

        /// <summary>
        /// 入力データ読み込み
        /// </summary>
        /// <param name="filename"></param>
        public void Load(string filename)
        {
            // 入力データ初期化
            init();

            IList<FemNode> nodes = null;
            IList<FemElement> elements = null;
            IList<IList<int>> ports = null;
            IList<int> forceBCNodes = null;
            IList<IList<uint>> elemNoPeriodicList = null;
            IList<IList<IList<int>>> nodePeriodicBList = null;
            IList<IList<int>> defectNodePeriodicList = null;
            int incidentPortNo = 1;
            MediaInfo[] medias = null;
            double firstWaveLength = 0.0;
            double lastWaveLength = 0.0;
            int calcCnt = 0;
            WaveModeDV waveModeDv = WaveModeDV.TE;
            bool ret = FemInputDatFile.LoadFromFile(
                filename,
                out nodes,
                out elements,
                out ports,
                out forceBCNodes,
                out elemNoPeriodicList,
                out nodePeriodicBList,
                out defectNodePeriodicList,
                out incidentPortNo,
                out medias,
                out firstWaveLength,
                out lastWaveLength,
                out calcCnt,
                out waveModeDv);
            if (ret)
            {
                System.Diagnostics.Debug.Assert(medias.Length == Medias.Length);
                Nodes = nodes;
                Elements = elements;
                Ports = ports;
                ForceBCNodes = forceBCNodes;
                ElemNoPeriodicList = elemNoPeriodicList;
                NodePeriodicBList = nodePeriodicBList;
                DefectNodePeriodicList = defectNodePeriodicList;
                IncidentPortNo = incidentPortNo;
                Medias = medias;
                FirstWaveLength = firstWaveLength;
                LastWaveLength = lastWaveLength;
                CalcFreqCnt = calcCnt;
                WaveModeDv = waveModeDv;

                // 要素形状と次数の判定
                if (Elements.Count > 0)
                {
                    Constants.FemElementShapeDV elemShapeDv;
                    int order;
                    int vertexCnt;
                    FemMeshLogic.GetElementShapeDvAndOrderByElemNodeCnt(Elements[0].NodeNumbers.Length, out elemShapeDv, out order, out vertexCnt);
                    ElemShapeDvToBeSet = elemShapeDv;
                    ElemOrderToBeSet = order;
                }

                {
                    // H面の場合および平行平板導波路の場合
                    if ((BoundaryDv == BoundaryDV.ElectricWall && WaveModeDv == WaveModeDV.TM) // TMモード磁界Hz(H面に垂直な磁界)による解析では電気壁は自然境界
                        || (BoundaryDv == BoundaryDV.MagneticWall && WaveModeDv == WaveModeDV.TE) // TEST
                        )
                    {
                        // 自然境界条件なので強制境界をクリアする
                        ForceBCNodes.Clear();
                    }
                }

                // 強制境界節点番号ハッシュの作成(2D節点番号)
                foreach (int nodeNumber in ForceBCNodes)
                {
                    if (!ForceNodeNumberH.ContainsKey(nodeNumber))
                    {
                        ForceNodeNumberH[nodeNumber] = true;
                    }
                }
                // 辺と要素の対応マップ作成
                MkEdgeToElementNoH(Elements, ref EdgeToElementNoH);
                // 導波管幅の決定
                setupWaveguideWidth();

                if (CalcFreqCnt == 0)
                {
                    // 旧型式のデータの可能性があるので既定値をセットする（ファイル読み込みエラーにはなっていないので）
                    FirstWaveLength = GetWaveLengthFromNormalizedFreq(Constants.DefNormalizedFreqRange[0], WaveguideWidth, LatticeA);
                    LastWaveLength = GetWaveLengthFromNormalizedFreq(Constants.DefNormalizedFreqRange[1], WaveguideWidth, LatticeA);
                    CalcFreqCnt = Constants.DefCalcFreqencyPointCount;
                }
            }
        }

        /// <summary>
        /// 規格化周波数→波長変換
        /// </summary>
        /// <param name="normalizedFreq">規格化周波数</param>
        /// <param name="waveguideWidth">導波路の幅</param>
        /// <returns>波長</returns>
        public static double GetWaveLengthFromNormalizedFreq(double normalizedFreq, double waveguideWidth, double latticeA)
        {
            if (Math.Abs(normalizedFreq) < Constants.PrecisionLowerLimit)
            {
                return 0.0;
            }
            //return 2.0 * waveguideWidth / normalizedFreq;
            return latticeA / normalizedFreq;
        }

        /// <summary>
        /// 波長→規格化周波数変換
        /// </summary>
        /// <param name="waveLength">波長</param>
        /// <param name="waveguideWidth">導波路の幅</param>
        /// <returns>規格化周波数</returns>
        public static double GetNormalizedFreq(double waveLength, double waveguideWidth, double latticeA)
        {
            if (Math.Abs(waveLength) < Constants.PrecisionLowerLimit)
            {
                return 0.0;
            }
            //return 2.0 * waveguideWidth / waveLength;
            return latticeA / waveLength;
        }

        /// <summary>
        /// 計算開始規格化周波数
        /// </summary>
        public double FirstNormalizedFreq
        {
            get
            {
                double normalizedFreq1 = (Math.Abs(FirstWaveLength) < Constants.PrecisionLowerLimit) ? 0.0 : GetNormalizedFreq(FirstWaveLength, WaveguideWidth, LatticeA);
                normalizedFreq1 = Math.Round(normalizedFreq1, 2);
                return normalizedFreq1;
            }
        }
        /// <summary>
        /// 計算終了規格化周波数
        /// </summary>
        public double LastNormalizedFreq
        {
            get
            {
                double normalizedFreq2 = (Math.Abs(LastWaveLength) < Constants.PrecisionLowerLimit) ? 0.0 : GetNormalizedFreq(LastWaveLength, WaveguideWidth, LatticeA);
                normalizedFreq2 = Math.Round(normalizedFreq2, 2);
                return normalizedFreq2;
            }
        }

        /// <summary>
        /// 周波数番号から規格化周波数を取得する
        /// </summary>
        /// <param name="freqNo"></param>
        /// <returns></returns>
        public double GetNormalizedFreqFromFreqNo(int freqNo)
        {
            double normalizedFreq = 0;
            int freqIndex = freqNo - 1;
            int calcFreqCnt = CalcFreqCnt;
            double firstNormalizedFreq = FirstNormalizedFreq;
            double lastNormalizedFreq = LastNormalizedFreq;
            double deltaf = (lastNormalizedFreq - firstNormalizedFreq) / calcFreqCnt;

            normalizedFreq = firstNormalizedFreq + freqIndex * deltaf;

            return normalizedFreq;
        }

        /// <summary>
        /// 計算条件の更新
        /// </summary>
        /// <param name="normalizedFreq1">計算開始規格化周波数</param>
        /// <param name="normalizedFreq2">計算終了規格化周波数</param>
        /// <param name="calcCnt">計算する周波数の数</param>
        public void SetNormalizedFreqRange(double normalizedFreq1, double normalizedFreq2, int calcCnt)
        {
            // 計算対象周波数を波長に変換
            double firstWaveLength = GetWaveLengthFromNormalizedFreq(normalizedFreq1, WaveguideWidth, LatticeA);
            double lastWaveLength = GetWaveLengthFromNormalizedFreq(normalizedFreq2, WaveguideWidth, LatticeA);

            // セット
            FirstWaveLength = firstWaveLength;
            LastWaveLength = lastWaveLength;
            CalcFreqCnt = calcCnt;
        }

        /// <summary>
        /// 辺と要素の対応マップ作成
        /// </summary>
        /// <param name="in_Elements"></param>
        /// <param name="out_EdgeToElementNoH"></param>
        public static void MkEdgeToElementNoH(IList<FemElement> in_Elements, ref Dictionary<string, IList<int>> out_EdgeToElementNoH)
        {
            FemMeshLogic.MkEdgeToElementNoH(in_Elements, ref out_EdgeToElementNoH);
        }

        /// <summary>
        /// 節点と要素番号のマップ作成
        /// </summary>
        /// <param name="in_Elements"></param>
        /// <param name="out_NodeToElementNoH"></param>
        public static void MkNodeToElementNoH(IList<FemElement> in_Elements, ref Dictionary<int, IList<int>> out_NodeToElementNoH)
        {
            FemMeshLogic.MkNodeToElementNoH(in_Elements, ref out_NodeToElementNoH);
        }

        /// <summary>
        /// 導波管幅の決定
        /// </summary>
        private void setupWaveguideWidth()
        {
            if (Ports.Count == 0)
            {
                WaveguideWidth = DefWaveguideWidth; // 規格化周波数が定義できるように初期値を設定
                return;
            }
            // ポート1の導波管幅
            int port1NodeNumber1 = Ports[0][0];
            int port1NodeNumber2 = Ports[0][Ports[0].Count - 1];
            double w1 = FemMeshLogic.GetDistance(Nodes[port1NodeNumber1 - 1].Coord, Nodes[port1NodeNumber2 - 1].Coord);

            WaveguideWidth = w1;
            System.Diagnostics.Debug.WriteLine("WaveguideWidth:{0}", w1);
        }

        /// <summary>
        /// ヘルムホルツ方程式のパラメータP,Qを取得する
        /// </summary>
        /// <param name="k0">波数</param>
        /// <param name="media">媒質</param>
        /// <param name="WGStructureDv">導波路構造区分</param>
        /// <param name="WaveModeDv">波のモード区分</param>
        /// <param name="media_P">ヘルムホルツ方程式のパラメータP</param>
        /// <param name="media_Q">ヘルムホルツ方程式のパラメータQ</param>
        public static void GetHelmholtzMediaPQ(
            double k0,
            MediaInfo media,
            WaveModeDV WaveModeDv,
            out double[,] media_P,
            out double[,] media_Q)
        {
            media_P = null;
            media_Q = null;
            double[,] erMat = media.Q;
            double[,] urMat = media.P;
            // 平行平板導波路の場合
            if (WaveModeDv == FemSolver.WaveModeDV.TE)
            {
                // TEモード(H面)
                // 界方程式: Ez(H面に垂直な電界)
                //  p = (μr)-1
                //  q = εr
                //media_P = urMat;
                //media_Q = erMat;
                //// [p]は逆数をとる
                //media_P = MyMatrixUtil.matrix_Inverse(media_P);
                // 比透磁率の逆数
                media_P = new double[3, 3];
                media_P[0, 0] = 1.0 / urMat[0, 0];
                media_P[1, 1] = 1.0 / urMat[1, 1];
                media_P[2, 2] = 1.0 / urMat[2, 2];
                // 比誘電率
                media_Q = new double[3, 3];
                media_Q[0, 0] = erMat[0, 0];
                media_Q[1, 1] = erMat[1, 1];
                media_Q[2, 2] = erMat[2, 2];
            }
            else if (WaveModeDv == FemSolver.WaveModeDV.TM)
            {
                // TMモード(TEMモードを含む)(H面)
                // 界方程式: Hz(H面に垂直な磁界)
                //  p = (εr)-1
                //  q = μr
                //media_P = erMat;
                //media_Q = urMat;
                //// [p]は逆数をとる
                //media_P = MyMatrixUtil.matrix_Inverse(media_P);
                // 比誘電率の逆数
                media_P = new double[3, 3];
                media_P[0, 0] = 1.0 / erMat[0, 0];
                media_P[1, 1] = 1.0 / erMat[1, 1];
                media_P[2, 2] = 1.0 / erMat[2, 2];
                // 比透磁率
                media_Q = new double[3, 3];
                media_Q[0, 0] = urMat[0, 0];
                media_Q[1, 1] = urMat[1, 1];
                media_Q[2, 2] = urMat[2, 2];
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
        }

        /// <summary>
        /// 点が要素内に含まれる？
        /// </summary>
        /// <param name="element">要素</param>
        /// <param name="test_pp">テストする点</param>
        /// <param name="Nodes">節点リスト（要素は座標を持たないので節点リストが必要）</param>
        /// <returns></returns>
        public static bool IsPointInElement(FemElement element, double[] test_pp, IList<FemNode> nodes)
        {
            return FemMeshLogic.IsPointInElement(element, test_pp, nodes);
        }

        /// <summary>
        /// Fem入力データの取得
        /// </summary>
        /// <param name="outNodes">節点リスト</param>
        /// <param name="outElements">要素リスト</param>
        /// <param name="outMedias">媒質リスト</param>
        /// <param name="outPorts">ポート節点番号リストのリスト</param>
        /// <param name="outForceNodes">強制境界節点番号リスト</param>
        /// <param name="outIncidentPortNo">入射ポート番号</param>
        /// <param name="outWaveguideWidth">導波路の幅</param>
        public void GetFemInputInfo(
            out FemNode[] outNodes, out FemElement[] outElements,
            out MediaInfo[] outMedias,
            out IList<int[]> outPorts,
            out int[] outForceNodes,
            out int outIncidentPortNo,
            out double outWaveguideWidth,
            out double outLatticeA)
        {
            outNodes = null;
            outElements = null;
            outMedias = null;
            outPorts = null;
            outForceNodes = null;
            outIncidentPortNo = 1;
            outWaveguideWidth = DefWaveguideWidth;
            outLatticeA = 1.0;

            /* データの判定は取得した側が行う（メッシュ表示で、ポートを指定しないとメッシュが表示されないのを解消するためここで判定するのを止める)
            if (!isInputDataValid())
            {
                return;
            }
             */

            int nodeCnt = Nodes.Count;
            outNodes = new FemNode[nodeCnt];
            for (int i = 0; i < nodeCnt; i++)
            {
                FemNode femNode = new FemNode();
                femNode.CP(Nodes[i]);
                outNodes[i] = femNode;
            }
            int elementCnt = Elements.Count;
            outElements = new FemElement[elementCnt];
            for (int i = 0; i < elementCnt; i++)
            {
                //FemElement femElement = new FemElement();
                FemElement femElement = FemMeshLogic.CreateFemElementByElementNodeCnt(Elements[i].NodeNumbers.Length);
                femElement.CP(Elements[i]);
                outElements[i] = femElement;
            }
            if (Medias != null)
            {
                outMedias = new MediaInfo[Medias.Length];
                for (int i = 0; i < Medias.Length; i++)
                {
                    outMedias[i] = Medias[i].Clone() as MediaInfo;
                }
            }
            int portCnt = Ports.Count;
            outPorts = new List<int[]>();
            foreach (IList<int> portNodes in Ports)
            {
                int[] outPortNodes = new int[portNodes.Count];
                for (int inoB = 0; inoB < portNodes.Count; inoB++)
                {
                    outPortNodes[inoB] = portNodes[inoB];
                }
                outPorts.Add(outPortNodes);
            }
            outForceNodes = new int[ForceBCNodes.Count];
            for (int i = 0; i < ForceBCNodes.Count; i++)
            {
                outForceNodes[i] = ForceBCNodes[i];
            }
            outIncidentPortNo = IncidentPortNo;

            outWaveguideWidth = WaveguideWidth;

            outLatticeA = LatticeA;
        }
        
        /// <summary>
        /// 入力データ妥当？(解析開始前にメッセージを表示する)
        /// </summary>
        /// <returns></returns>
        public bool ChkInputData()
        {
            return isInputDataValid(true);
        }

        /// <summary>
        /// 入力データ妥当？
        /// </summary>
        /// <returns></returns>
        private bool isInputDataValid(bool showMessageFlg = false)
        {
            bool valid = false;
            if (Nodes.Count == 0)
            {
                if (showMessageFlg)
                {
                    MessageBox.Show("節点がありません", "計算できません", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return valid;
            }
            if (Elements.Count == 0)
            {
                if (showMessageFlg)
                {
                    MessageBox.Show("要素がありません", "計算できません", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return valid;
            }
            //if (ForceBCNodes.Count == 0)
            //{
            //    if (showMessageFlg)
            //    {
            //        MessageBox.Show("強制境界条件がありません", "計算できません", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    }
            //    // Note: H面導波路対象なので強制境界はあるはず
            //    return valid;
            //}
            if (Ports.Count == 0 || Ports[0].Count == 0)
            {
                if (showMessageFlg)
                {
                    MessageBox.Show("入出力ポートがありません", "計算できません", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return valid;
            }
            if (Math.Abs(WaveguideWidth - DefWaveguideWidth) < Constants.PrecisionLowerLimit)
            {
                if (showMessageFlg)
                {
                    MessageBox.Show("入力ポートがありません", "計算できません", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return valid;
            }
            if (CalcFreqCnt == 0)
            {
                if (showMessageFlg)
                {
                    MessageBox.Show("計算間隔が未設定です", "計算できません", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return valid;
            }
            if (LastNormalizedFreq <= FirstNormalizedFreq)
            {
                if (showMessageFlg)
                {
                    MessageBox.Show("計算範囲が不正です", "計算できません", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return valid;
            }

            valid = true;
            return valid;
        }

        /// <summary>
        /// 計算実行
        /// </summary>
        public void Run(string filename, object eachDoneCallbackObj, Delegate eachDoneCallback)
        {
            IsCalcAborted = false;
            if (!isInputDataValid())
            {
                return;
            }
            string basefilename = Path.GetDirectoryName(filename) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(filename);
            string outfilename = basefilename + Constants.FemOutputExt;
            //string indexfilename = basefilename + Constants.FemOutputIndexExt;
            // BUGFIX インデックスファイル名は.out.idx
            string indexfilename = outfilename + Constants.FemOutputIndexExt;

            // 結果出力ファイルの削除(結果を追記モードで書き込むため)
            if (File.Exists(outfilename))
            {
                File.Delete(outfilename);
            }
            if (File.Exists(indexfilename))
            {
                File.Delete(indexfilename);
            }

            FemMat = null;
            SortedNodes = null;
            FemMatPattern = null;
            try
            {
                System.Diagnostics.Debug.WriteLine("TotalMemory: {0}", GC.GetTotalMemory(false));
                // GC.Collect 呼び出し後に GC.WaitForPendingFinalizers を呼び出します。これにより、すべてのオブジェクトに対するファイナライザが呼び出されるまで、現在のスレッドは待機します。
                // ファイナライザ作動後は、回収すべき、(ファイナライズされたばかりの) アクセス不可能なオブジェクトが増えます。もう1度 GC.Collect を呼び出し、それらを回収します。
                GC.Collect(); // アクセス不可能なオブジェクトを除去
                GC.WaitForPendingFinalizers(); // ファイナライゼーションが終わるまでスレッド待機
                GC.Collect(0); // ファイナライズされたばかりのオブジェクトに関連するメモリを開放
                System.Diagnostics.Debug.WriteLine("TotalMemory: {0}", GC.GetTotalMemory(false));
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
                MessageBox.Show(exception.Message);
            }

            int calcFreqCnt = CalcFreqCnt;
            double firstNormalizedFreq = FirstNormalizedFreq;
            double lastNormalizedFreq = LastNormalizedFreq;
            int maxMode = MaxModeCnt;
            double deltaf = (lastNormalizedFreq - firstNormalizedFreq) / calcFreqCnt;
            // 始点と終点も計算するように変更
            for (int freqIndex = 0; freqIndex < calcFreqCnt + 1; freqIndex++)
            {
                double normalizedFreq = firstNormalizedFreq + freqIndex * deltaf;
                if (normalizedFreq < Constants.PrecisionLowerLimit)
                {
                    normalizedFreq = 1.0e-4;
                }
                double waveLength = GetWaveLengthFromNormalizedFreq(normalizedFreq, WaveguideWidth, LatticeA);
                System.Diagnostics.Debug.WriteLine("a/lamda = {0}", normalizedFreq);
                int freqNo = freqIndex + 1;
                runEach(freqNo, outfilename, waveLength, maxMode);
                eachDoneCallback.Method.Invoke(eachDoneCallbackObj, new object[]{new object[]{}, });
                if (IsCalcAborted)
                {
                    break;
                }
            }
            FemMat = null;
            SortedNodes = null;
            FemMatPattern = null;
        }

        /// <summary>
        /// 周波数１箇所だけ計算する
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="in_freqNo"></param>
        public void RunAtOneFreq(string filename, int in_freqNo, object eachDoneCallbackObj, Delegate eachDoneCallback, bool appendFileFlg = false)
        {
            IsCalcAborted = false;
            if (!isInputDataValid())
            {
                return;
            
            }
            string basefilename = Path.GetDirectoryName(filename) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(filename);
            string outfilename = basefilename + Constants.FemOutputExt;
            //string indexfilename = basefilename + Constants.FemOutputIndexExt;
            // BUGFIX インデックスファイル名は.out.idx
            string indexfilename = outfilename + Constants.FemOutputIndexExt;
            if (!appendFileFlg)
            {
                // 結果出力ファイルの削除(結果を追記モードで書き込むため)
                if (File.Exists(outfilename))
                {
                    File.Delete(outfilename);
                }
                if (File.Exists(indexfilename))
                {
                    File.Delete(indexfilename);
                }
            }

            FemMat = null;
            SortedNodes = null;
            FemMatPattern = null;
            try
            {
                System.Diagnostics.Debug.WriteLine("TotalMemory: {0}", GC.GetTotalMemory(false));
                // GC.Collect 呼び出し後に GC.WaitForPendingFinalizers を呼び出します。これにより、すべてのオブジェクトに対するファイナライザが呼び出されるまで、現在のスレッドは待機します。
                // ファイナライザ作動後は、回収すべき、(ファイナライズされたばかりの) アクセス不可能なオブジェクトが増えます。もう1度 GC.Collect を呼び出し、それらを回収します。
                GC.Collect(); // アクセス不可能なオブジェクトを除去
                GC.WaitForPendingFinalizers(); // ファイナライゼーションが終わるまでスレッド待機
                GC.Collect(0); // ファイナライズされたばかりのオブジェクトに関連するメモリを開放
                System.Diagnostics.Debug.WriteLine("TotalMemory: {0}", GC.GetTotalMemory(false));
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
                MessageBox.Show(exception.Message);
            }

            int calcFreqCnt = CalcFreqCnt;
            double firstNormalizedFreq = FirstNormalizedFreq;
            double lastNormalizedFreq = LastNormalizedFreq;
            int maxMode = MaxModeCnt;
            double deltaf = (lastNormalizedFreq - firstNormalizedFreq) / calcFreqCnt;

            {
                int freqIndex = in_freqNo - 1;
                if (freqIndex < 0 || freqIndex >= calcFreqCnt + 1)
                {
                    return;
                }
                double normalizedFreq = firstNormalizedFreq + freqIndex * deltaf;
                if (normalizedFreq < Constants.PrecisionLowerLimit)
                {
                    normalizedFreq = 1.0e-4;
                }
                double waveLength = GetWaveLengthFromNormalizedFreq(normalizedFreq, WaveguideWidth, LatticeA);
                System.Diagnostics.Debug.WriteLine("a/lamda = {0}", normalizedFreq);
                int freqNo = freqIndex + 1;
                runEach(freqNo, outfilename, waveLength, maxMode);
                eachDoneCallback.Method.Invoke(eachDoneCallbackObj, new object[] { new object[] { }, });
            }
            FemMat = null;
            SortedNodes = null;
            FemMatPattern = null;
        }

        /// <summary>
        /// 各波長について計算実行
        /// </summary>
        /// <param name="freqNo">計算する周波数に対応する番号(1,...,CalcFreqCnt - 1)</param>
        /// <param name="filename">出力ファイル</param>
        /// <param name="waveLength">波長</param>
        private void runEach(int freqNo, string filename, double waveLength, int maxMode)
        {
            System.Diagnostics.Debug.WriteLine("runEach 1");
            bool ret;
            // 波数
            double k0 = 2.0 * pi / waveLength;

            // 全体剛性行列作成
            int[] nodesRegion = null;
            MyComplexMatrix mat = null;
            ret = getHelmholtzLinearSystemMatrix(waveLength, out nodesRegion, out mat);
            if (!ret)
            {
                System.Diagnostics.Debug.WriteLine("Error at getHelmholtzLinearSystemMatrix ret: {0}", ret);
                // 計算を中止する
                IsCalcAborted = true;
                return;
            }
            System.Diagnostics.Debug.WriteLine("runEach 2");

            // 残差ベクトル初期化
            int nodeCnt = nodesRegion.Length;
            Complex[] resVec = new Complex[nodeCnt];
            /*
            for (int i = 0; i < nodeCnt; i++)
            {
                resVec[i] = new Complex();
            }
             */
            System.Diagnostics.Debug.WriteLine("runEach 3");

            // 開口面境界条件の適用
            int portCnt = Ports.Count;
            IList<int[]> nodesBoundaryList = new List<int[]>();
            IList<MyDoubleMatrix> ryy_1dList = new List<MyDoubleMatrix>();
            IList<Complex[]> eigenValuesList = new List<Complex[]>();
            IList<Complex[,]> eigenVecsList = new List<Complex[,]>();
            IList<Complex[,]> eigen_dFdXsList = new List<Complex[,]>();
            for (int i = 0; i < portCnt; i++)
            {
                int portNo = i + 1;
                int[] nodesBoundary = null;
                MyDoubleMatrix ryy_1d = null;
                Complex[] eigenValues = null;
                Complex[,] eigenVecs = null;
                Complex[,] eigen_dFdXs = null;

                /*
                // ポート固有値解析
                solvePortWaveguideEigen(waveLength, portNo, maxMode, out nodesBoundary, out ryy_1d, out eigenValues, out eigenVecs);
                */
                // ポート固有値解析（周期構造導波路）
                solvePortPeriodicWaveguideEigen(freqNo, filename, waveLength, portNo, maxMode, out nodesBoundary, out ryy_1d, out eigenValues, out eigenVecs, out eigen_dFdXs);

                nodesBoundaryList.Add(nodesBoundary);
                ryy_1dList.Add(ryy_1d);
                eigenValuesList.Add(eigenValues);
                eigenVecsList.Add(eigenVecs);
                eigen_dFdXsList.Add(eigen_dFdXs);

                // 入射ポートの判定
                bool isInputPort = (i == (IncidentPortNo - 1));
                // 境界条件をリニア方程式に追加
                /*
                addPortBC(waveLength, isInputPort, nodesBoundary, ryy_1d, eigenValues, eigenVecs, nodesRegion, mat, resVec);
                 */
                if (eigenValues == null || Math.Abs(eigenValues[0].Real / k0) < Constants.PrecisionLowerLimit)
                {
                    System.Diagnostics.Debug.WriteLine("!!!!!!!! No propagation mode. Skip: a/lambda {0}", LatticeA / waveLength);
                    return;
                }
                addPeriodicPortBC(waveLength, isInputPort, nodesBoundary, ryy_1d, eigenValues, eigenVecs, eigen_dFdXs, nodesRegion, mat, resVec);
            }
            System.Diagnostics.Debug.WriteLine("runEach 4");
            Complex[] valuesAll = null;
            {
                System.Diagnostics.Debug.Assert(mat is MyComplexBandMatrix);

                valuesAll = null;

                MyComplexBandMatrix bandMat = mat as MyComplexBandMatrix;
                int rowcolSize = bandMat.RowSize;
                int subdiaSize = bandMat.SubdiaSize;
                int superdiaSize = bandMat.SuperdiaSize;

                // リニア方程式を解く
                Complex[] X = null;
                // clapackの行列の1次元ベクトルへの変換は列を先に埋める
                // バンドマトリクス用の格納方法で格納する
                Complex[] A = MyMatrixUtil.matrix_ToBuffer(bandMat, false);
                Complex[] B = resVec;
                int x_row = nodeCnt;
                int x_col = 1;
                int a_row = rowcolSize;
                int a_col = rowcolSize;
                int kl = subdiaSize;
                int ku = superdiaSize;
                int b_row = nodeCnt;
                int b_col = 1;
                System.Diagnostics.Debug.WriteLine("run zgbsv");
                try
                {
                    KrdLab.clapack.FunctionExt.zgbsv(ref X, ref x_row, ref x_col, A, a_row, a_col, kl, ku, B, b_row, b_col);
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
                    MessageBox.Show(string.Format("計算中にエラーが発生しました。a/λ = {0}" + System.Environment.NewLine + "    {1}",
                        GetNormalizedFreq(waveLength, WaveguideWidth, LatticeA), exception.Message));
                    return;
                    // ダミーデータ
                    //X = new ValueType[nodeCnt];
                    //for (int i = 0; i < nodeCnt; i++) { X[i] = new Complex(); }
                }
                valuesAll = X;
            }

            // 散乱行列Sij
            // ポートj = IncidentPortNoからの入射のみ対応
            /*
            Complex[] scatterVec = new Complex[Ports.Count];
            for (int portIndex = 0; portIndex < Ports.Count; portIndex++)
            {
                int iMode = IncidentModeIndex;
                bool isIncidentMode = (portIndex == IncidentPortNo - 1);
                int[] nodesBoundary = nodesBoundaryList[portIndex];
                MyDoubleMatrix ryy_1d = ryy_1dList[portIndex];
                Complex[] eigenValues = eigenValuesList[portIndex];
                Complex[,] eigenVecs = eigenVecsList[portIndex];
                Complex si1 = getWaveguidePortReflectionCoef(waveLength, iMode, isIncidentMode,
                                                             nodesBoundary, ryy_1d, eigenValues, eigenVecs,
                                                             nodesRegion, valuesAll);
                System.Diagnostics.Debug.WriteLine("s{0}{1} = {2} + {3}i (|S| = {4} |S|^2 = {5})", portIndex + 1, IncidentPortNo, si1.Real, si1.Imaginary, Complex.Abs(si1), Complex.Abs(si1) * Complex.Abs(si1));
                scatterVec[portIndex] = si1;
            }
             */
            // 拡張散乱行列Simjn
            //   出力ポートi モードmの散乱係数
            //   入射ポートj = IncindentPortNo n = 0(基本モード)のみ対応
            IList<Complex[]> scatterVecList = new List<Complex[]>();
            double totalPower = 0.0;
            for (int portIndex = 0; portIndex < Ports.Count; portIndex++)
            {
                int[] nodesBoundary = nodesBoundaryList[portIndex];
                MyDoubleMatrix ryy_1d = ryy_1dList[portIndex];
                Complex[] eigenValues = eigenValuesList[portIndex];
                Complex[,] eigenVecs = eigenVecsList[portIndex];
                Complex[,] eigen_dFdXs = eigen_dFdXsList[portIndex];
                int modeCnt = eigenValues.Length;
                Complex[] portScatterVec = new Complex[modeCnt];
                System.Diagnostics.Debug.WriteLine("port {0}", portIndex);
                for (int iMode = 0; iMode < eigenValues.Length; iMode++)
                {
                    bool isPropagationMode = (eigenValues[iMode].Real >= Constants.PrecisionLowerLimit);
                    bool isIncidentMode = ((portIndex == IncidentPortNo - 1) && iMode == 0);
                    /*
                    Complex sim10 = getWaveguidePortReflectionCoef(
                        waveLength,
                        iMode,
                        isIncidentMode,
                        nodesBoundary,
                        ryy_1d,
                        eigenValues,
                        eigenVecs,
                        nodesRegion,
                        valuesAll);
                     */
                    Complex sim10 = getPeriodicWaveguidePortReflectionCoef(
                        waveLength,
                        iMode,
                        isIncidentMode,
                        nodesBoundary,
                        ryy_1d,
                        eigenValues,
                        eigenVecs,
                        eigen_dFdXs,
                        nodesRegion,
                        valuesAll);
                    portScatterVec[iMode] = sim10;
                    if (isPropagationMode)
                    {
                        totalPower += (sim10 * Complex.Conjugate(sim10)).Real;
                    }
                    // check
                    if (iMode == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("  {0} s{1}{2}{3}{4} = {5} + {6}i " + System.Environment.NewLine + "        (|S| = {7} |S|^2 = {8})",
                            isPropagationMode ? "P" : "E",
                            portIndex + 1, (iMode + 1), IncidentPortNo, (IncidentModeIndex + 1),
                            sim10.Real, sim10.Imaginary, Complex.Abs(sim10), Complex.Abs(sim10) * Complex.Abs(sim10));
                    }
                }
                scatterVecList.Add(portScatterVec);
            }
            System.Diagnostics.Debug.WriteLine("totalPower:{0}", totalPower);

            /////////////////////////////////////
            // 結果をファイルに出力
            ////////////////////////////////////
            FemOutputDatFile.AppendToFile(
                filename, freqNo, waveLength, maxMode,
                Ports.Count, IncidentPortNo,
                nodesBoundaryList, eigenValuesList, eigenVecsList,
                nodesRegion, valuesAll,
                scatterVecList);
        }

        /// <summary>
        /// ヘルムホルツ方程式の剛性行列を作成する
        /// </summary>
        /// <param name="waveLength"></param>
        /// <param name="nodesRegion"></param>
        /// <param name="mat"></param>
        /// <returns>true: 成功 false:失敗(メモリの確保に失敗)</returns>
        private bool getHelmholtzLinearSystemMatrix(double waveLength, out int[] nodesRegion, out MyComplexMatrix mat)
        {
            nodesRegion = null;
            mat = null;

            // 2D節点番号リスト（ソート済み）
            IList<int> sortedNodes = new List<int>();
            // 2D節点番号→ソート済みリストインデックスのマップ
            Dictionary<int, int> toSorted = new Dictionary<int, int>();
            // 非０要素のパターン
            bool[,] matPattern = null;

            if (SortedNodes == null)
            {
                // 節点番号リストをソートする
                //   強制境界の除去する
                //   バンドマトリクスのバンド幅を縮小する

                // 強制境界節点と内部領域節点を分離
                foreach (FemNode node in Nodes)
                {
                    int nodeNumber = node.No;
                    if (ForceNodeNumberH.ContainsKey(nodeNumber))
                    {
                        //forceNodes.Add(nodeNumber);
                    }
                    else
                    {
                        sortedNodes.Add(nodeNumber);
                        toSorted.Add(nodeNumber, sortedNodes.Count - 1);
                    }
                }
                {
                    // バンド幅を縮小する
                    // 非０要素のパターンを取得
                    getMatNonzeroPattern(Elements, Ports, toSorted, out matPattern);
                    // subdiagonal、superdiagonalのサイズを取得する
                    int subdiaSizeInitial = 0;
                    int superdiaSizeInitial = 0;
                    {
                        System.Diagnostics.Debug.WriteLine("/////initial BandMat info///////");
                        int rowcolSize;
                        int subdiaSize;
                        int superdiaSize;
                        getBandMatrixSubDiaSizeAndSuperDiaSize(matPattern, out rowcolSize, out subdiaSize, out superdiaSize);
                        subdiaSizeInitial = subdiaSize;
                        superdiaSizeInitial = superdiaSize;
                    }

                    // 非０要素出現順に節点番号を格納
                    IList<int> optNodes = new List<int>();
                    Queue<int> chkQueue = new Queue<int>();
                    int[] remainNodes = new int[matPattern.GetLength(0)];
                    for (int i = 0; i < matPattern.GetLength(0); i++)
                    {
                        remainNodes[i] = i;
                    }
                    while (optNodes.Count < sortedNodes.Count)
                    {
                        // 飛び地領域対応
                        for (int rIndex = 0; rIndex < remainNodes.Length; rIndex++)
                        {
                            int i = remainNodes[rIndex];
                            if (i == -1) continue;
                            //System.Diagnostics.Debug.Assert(!optNodes.Contains(i));
                            chkQueue.Enqueue(i);
                            remainNodes[rIndex] = -1;
                            break;
                        }
                        while (chkQueue.Count > 0)
                        {
                            int i = chkQueue.Dequeue();
                            optNodes.Add(i);
                            for (int rIndex = 0; rIndex < remainNodes.Length; rIndex++)
                            {
                                int j = remainNodes[rIndex];
                                if (j == -1) continue;
                                //System.Diagnostics.Debug.Assert(i != j);
                                if (matPattern[i, j])
                                {
                                    //System.Diagnostics.Debug.Assert(!optNodes.Contains(j) && !chkQueue.Contains(j));
                                    chkQueue.Enqueue(j);
                                    remainNodes[rIndex] = -1;
                                }
                            }
                        }
                    }
                    IList<int> optNodesGlobal = new List<int>();
                    Dictionary<int, int> toOptNodes = new Dictionary<int, int>();
                    foreach (int i in optNodes)
                    {
                        int ino = sortedNodes[i];
                        optNodesGlobal.Add(ino);
                        toOptNodes.Add(ino, optNodesGlobal.Count - 1);
                    }
                    System.Diagnostics.Debug.Assert(optNodesGlobal.Count == sortedNodes.Count);
                    // 改善できないこともあるのでチェックする
                    bool improved = false;
                    bool[,] optMatPattern = null;
                    // 非０パターンを取得
                    getMatNonzeroPattern(Elements, Ports, toOptNodes, out optMatPattern);
                    // check
                    {
                        System.Diagnostics.Debug.WriteLine("/////opt BandMat info///////");
                        int rowcolSize;
                        int subdiaSize;
                        int superdiaSize;
                        getBandMatrixSubDiaSizeAndSuperDiaSize(optMatPattern, out rowcolSize, out subdiaSize, out superdiaSize);
                        if (subdiaSize <= subdiaSizeInitial && superdiaSize <= superdiaSizeInitial)
                        {
                            improved = true;
                        }
                    }
                    if (improved)
                    {
                        // 置き換え
                        sortedNodes = optNodesGlobal;
                        toSorted = toOptNodes;
                        matPattern = optMatPattern;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("band with not optimized!");
                    }
                }
                SortedNodes = sortedNodes;
                FemMatPattern = matPattern;
            }
            else
            {
                // ソート済み節点番号リストを取得
                sortedNodes = SortedNodes;

                // 2D節点番号→ソート済みリストインデックスのマップ作成
                for (int i = 0; i < sortedNodes.Count; i++)
                {
                    int nodeNumber = sortedNodes[i];
                    if (!toSorted.ContainsKey(nodeNumber))
                    {
                        toSorted.Add(nodeNumber, i);
                    }
                }

                // 非０パターンを取得
                //getMatNonzeroPattern(Nodes, Elements, Ports, ForceBCNodes, toSorted, out matPattern);
                matPattern = FemMatPattern;
            }

            // 総節点数
            int nodeCnt = sortedNodes.Count;

            // 全体節点を配列に格納
            nodesRegion = sortedNodes.ToArray();

            // 全体剛性行列初期化
            //mat = new MyComplexMatrix(nodeCnt, nodeCnt);
            // メモリ割り当てのコストが高いので変更する
            if (FemMat == null)
            {
                try
                {
                    {
                        // バンドマトリクス(zgbsv)
                        // バンドマトリクス情報を取得する
                        int rowcolSize = 0;
                        int subdiaSize = 0;
                        int superdiaSize = 0;
                        getBandMatrixSubDiaSizeAndSuperDiaSize(matPattern, out rowcolSize, out subdiaSize, out superdiaSize);
                        FemMat = new MyComplexBandMatrix(rowcolSize, subdiaSize, superdiaSize);
                    }
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
                    MessageBox.Show("メモリの確保に失敗しました。");
                    return false;
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(FemMat.RowSize == nodeCnt);
                int size = FemMat._rsize * FemMat._csize;
                for (int i = 0; i < size; i++)
                {
                    FemMat._body[i].Real = 0;
                    FemMat._body[i].Imaginary = 0;
                }
            }
            mat = FemMat;
            /*
            for (int i = 0; i < nodeCnt * nodeCnt; i++)
            {
                mat._body[i] = new Complex();
            }
             */
            foreach (FemElement element in Elements)
            {
                addElementMat(waveLength, toSorted, element, ref mat);
            }

            return true;
        }

        /// <summary>
        /// FEM行列の非０パターンを取得する
        /// </summary>
        /// <param name="Elements"></param>
        /// <param name="Ports"></param>
        /// <param name="toSorted">ソート済み節点番号→ソート済み節点リストのインデックスマップ</param>
        /// <param name="matPattern">非０パターンの配列(非０の要素はtrue、０要素はfalse)</param>
        private static void getMatNonzeroPattern(
            IList<FemElement> Elements,
            IList<IList<int>> Ports,
            Dictionary<int, int> toSorted,
            out bool[,] matPattern
            )
        {
            matPattern = null;

            // 総節点数
            int nodeCnt = toSorted.Count;
            int matLen = nodeCnt;

            // 行列の非０パターンを取得する
            matPattern = new bool[matLen, matLen];
            for (int ino_global = 0; ino_global < matLen; ino_global++)
            {
                for (int jno_global = 0; jno_global < matLen; jno_global++)
                {
                    matPattern[ino_global, jno_global] = false;
                }
            }
            // 領域の節点の行列要素パターン
            foreach (FemElement element in Elements)
            {
                int[] nodeNumbers = element.NodeNumbers;

                foreach (int iNodeNumber in nodeNumbers)
                {
                    if (!toSorted.ContainsKey(iNodeNumber)) continue;
                    int ino_global = toSorted[iNodeNumber];
                    foreach (int jNodeNumber in nodeNumbers)
                    {
                        if (!toSorted.ContainsKey(jNodeNumber)) continue;
                        int jno_global = toSorted[jNodeNumber];
                        matPattern[ino_global, jno_global] = true;
                    }
                }
            }
            // 境界上の節点の行列要素パターン
            foreach (IList<int> portNodes in Ports)
            {
                foreach (int iNodeNumber in portNodes)
                {
                    if (!toSorted.ContainsKey(iNodeNumber)) continue;
                    int ino_global = toSorted[iNodeNumber];
                    foreach (int jNodeNumber in portNodes)
                    {
                        if (!toSorted.ContainsKey(jNodeNumber)) continue;
                        int jno_global = toSorted[jNodeNumber];
                        if (!matPattern[ino_global, jno_global])
                        {
                            matPattern[ino_global, jno_global] = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// FEM行列のバンドマトリクス情報を取得する
        /// </summary>
        /// <param name="matPattern">非０パターンの配列</param>
        /// <param name="rowcolSize">行数=列数</param>
        /// <param name="subdiaSize">subdiagonalのサイズ</param>
        /// <param name="superdiaSize">superdiagonalのサイズ</param>
        private static void getBandMatrixSubDiaSizeAndSuperDiaSize(
            bool[,] matPattern,
            out int rowcolSize,
            out int subdiaSize,
            out int superdiaSize)
        {
            rowcolSize = matPattern.GetLength(0);

            // subdiaサイズ、superdiaサイズを取得する
            subdiaSize = 0;
            superdiaSize = 0;
            // Note: c == rowcolSize - 1は除く
            for (int c = 0; c < rowcolSize - 1; c++)
            {
                if (subdiaSize >= (rowcolSize - 1 - c))
                {
                    break;
                }
                int cnt = 0;
                for (int r = rowcolSize - 1; r >= c + 1; r--)
                {
                    // 非０要素が見つかったら抜ける
                    if (matPattern[r, c])
                    {
                        cnt = r - c;
                        break;
                    }
                }
                if (cnt > subdiaSize)
                {
                    subdiaSize = cnt;
                }
            }
            // Note: c == 0は除く
            for (int c = rowcolSize - 1; c >= 1; c--)
            {
                if (superdiaSize >= c)
                {
                    break;
                }
                int cnt = 0;
                for (int r = 0; r <= c - 1; r++)
                {
                    // 非０要素が見つかったら抜ける
                    if (matPattern[r, c])
                    {
                        cnt = c - r;
                        break;
                    }
                }
                if (cnt > superdiaSize)
                {
                    superdiaSize = cnt;
                }
            }
            System.Diagnostics.Debug.WriteLine("rowcolSize: {0} subdiaSize: {1} superdiaSize: {2}", rowcolSize, subdiaSize, superdiaSize);
        }

        /// <summary>
        /// ヘルムホルツ方程式に対する有限要素マトリクス作成
        /// </summary>
        /// <param name="waveLength">波長</param>
        /// <param name="toSorted">ソートされた節点インデックス（ 2D節点番号→ソート済みリストインデックスのマップ）</param>
        /// <param name="element">有限要素</param>
        /// <param name="mat">マージされる全体行列</param>
        private void addElementMat(double waveLength, Dictionary<int, int> toSorted, FemElement element, ref MyComplexMatrix mat)
        {
            Constants.FemElementShapeDV elemShapeDv;
            int order;
            int vertexCnt;
            FemMeshLogic.GetElementShapeDvAndOrderByElemNodeCnt(element.NodeNumbers.Length, out elemShapeDv, out order, out vertexCnt);

            if (elemShapeDv == Constants.FemElementShapeDV.Triangle && order == Constants.SecondOrder)
            {
                // ２次三角形要素の要素行列を全体行列に加算する
                FemMat_Tri_Second.AddElementMat(
                    waveLength,
                    toSorted,
                    element,
                    Nodes,
                    Medias,
                    ForceNodeNumberH,
                    WaveModeDv,
                    ref mat);
            }
            else if (elemShapeDv == Constants.FemElementShapeDV.Triangle && order == Constants.FirstOrder)
            {
                // １次三角形要素の要素行列を全体行列に加算する
                FemMat_Tri_First.AddElementMat(
                    waveLength,
                    toSorted,
                    element,
                    Nodes,
                    Medias,
                    ForceNodeNumberH,
                    WaveModeDv,
                    ref mat);
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
        }

        /*
        /// <summary>
        /// 入出力ポート境界条件の追加
        /// </summary>
        /// <param name="waveLength"></param>
        /// <param name="isInputPort"></param>
        /// <param name="nodesBoundary"></param>
        /// <param name="ryy_1d"></param>
        /// <param name="eigenValues"></param>
        /// <param name="eigenVecs"></param>
        /// <param name="nodesRegion"></param>
        /// <param name="mat"></param>
        /// <param name="resVec"></param>
        private void addPortBC(
            double waveLength,
            bool isInputPort,
            int[] nodesBoundary,
            MyDoubleMatrix ryy_1d,
            Complex[] eigenValues,
            Complex[,] eigenVecs,
            int[] nodesRegion,
            MyComplexMatrix mat,
            Complex[] resVec)
        {
            FemSolverPort.AddPortBC(
                WaveModeDv,
                waveLength,
                isInputPort,
                IncidentModeIndex,
                nodesBoundary,
                ryy_1d,
                eigenValues,
                eigenVecs,
                nodesRegion,
                Elements,
                Medias,
                ForceNodeNumberH,
                mat,
                resVec
                );
        }

        /// <summary>
        /// 散乱行列の計算
        /// </summary>
        /// <param name="waveLength"></param>
        /// <param name="iMode"></param>
        /// <param name="isIncidentMode"></param>
        /// <param name="nodesBoundary"></param>
        /// <param name="ryy_1d"></param>
        /// <param name="eigenValues"></param>
        /// <param name="eigenVecs"></param>
        /// <param name="nodesRegion"></param>
        /// <param name="valuesAll"></param>
        /// <returns></returns>
        private Complex getWaveguidePortReflectionCoef(
            double waveLength,
            int iMode,
            bool isIncidentMode,
            int[] nodesBoundary,
            MyDoubleMatrix ryy_1d,
            Complex[] eigenValues,
            Complex[,] eigenVecs,
            int[] nodesRegion,
            Complex[] valuesAll)
        {
            Complex s11 = FemSolverPort.GetWaveguidePortReflectionCoef(
                WaveModeDv,
                waveLength,
                iMode,
                isIncidentMode,
                nodesBoundary,
                ryy_1d,
                eigenValues,
                eigenVecs,
                nodesRegion,
                Elements,
                Medias,
                valuesAll
                );
            return s11;
        }

        /// <summary>
        /// ポート固有値解析
        /// </summary>
        private void solvePortWaveguideEigen(
            double waveLength,
            int portNo,
            int maxModeSpecified,
            out int[] nodesBoundary,
            out MyDoubleMatrix ryy_1d,
            out Complex[] eigenValues,
            out Complex[,] eigenVecs)
        {
            FemSolverPort.SolvePortWaveguideEigen(
                WaveModeDv,
                waveLength,
                maxModeSpecified,
                Nodes,
                EdgeToElementNoH,
                Elements,
                Medias,
                ForceNodeNumberH,
                Ports[portNo - 1],
                out nodesBoundary,
                out ryy_1d,
                out eigenValues,
                out eigenVecs
                );
        }
         */

        ///////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 入出力ポート境界条件の追加
        /// </summary>
        /// <param name="waveLength"></param>
        /// <param name="isInputPort"></param>
        /// <param name="nodesBoundary"></param>
        /// <param name="ryy_1d"></param>
        /// <param name="eigenValues"></param>
        /// <param name="eigenVecs"></param>
        /// <param name="nodesRegion"></param>
        /// <param name="mat"></param>
        /// <param name="resVec"></param>
        private void addPeriodicPortBC(
            double waveLength,
            bool isInputPort,
            int[] nodesBoundary,
            MyDoubleMatrix ryy_1d,
            Complex[] eigenValues,
            Complex[,] eigenVecs,
            Complex[,] eigen_dFdX,
            int[] nodesRegion,
            MyComplexMatrix mat,
            Complex[] resVec)
        {
            
            FemSolverPortPeriodic.AddPortBC(
                WaveModeDv,
                waveLength,
                LatticeA,
                isInputPort,
                IncidentModeIndex,
                nodesBoundary,
                ryy_1d,
                eigenValues,
                eigenVecs,
                eigen_dFdX,
                nodesRegion,
                Elements,
                Medias,
                ForceNodeNumberH,
                mat,
                resVec
                );
             
        }

        private Complex getPeriodicWaveguidePortReflectionCoef(
            double waveLength,
            int iMode,
            bool isIncidentMode,
            int[] nodesBoundary,
            MyDoubleMatrix ryy_1d,
            Complex[] eigenValues,
            Complex[,] eigenVecs,
            Complex[,] eigen_dFdXs,
            int[] nodesRegion,
            Complex[] valuesAll)
        {
            Complex s11 = FemSolverPortPeriodic.GetWaveguidePortReflectionCoef(
                WaveModeDv,
                waveLength,
                LatticeA,
                iMode,
                isIncidentMode,
                nodesBoundary,
                ryy_1d,
                eigenValues,
                eigenVecs,
                eigen_dFdXs,
                nodesRegion,
                Elements,
                Medias,
                valuesAll
                );
            return s11;
        }

        private void solvePortPeriodicWaveguideEigen(
            int freqNo,
            string filename,
            double waveLength,
            int portNo,
            int maxModeSpecified,
            out int[] nodesBoundary,
            out MyDoubleMatrix ryy_1d,
            out Complex[] eigenValues,
            out Complex[,] eigenVecs,
            out Complex[,] eigen_dFdX)
        {
            System.Diagnostics.Debug.WriteLine("/////////// solvePortWaveguideEigen: {0}, {1}", waveLength, portNo);
            // ポート周期構造領域の変数(表示用、伝達問題には必要ない)
            IList<int> nodePeriodic = null;
            Dictionary<int, int> toNodePeriodic = null;
            IList<double[]> coordsPeriodic = null;
            KrdLab.clapack.Complex[][] eigenVecsPeriodic = null;

            // 周期構造導波路固有値解析
            FemSolverPortPeriodic.SolvePortWaveguideEigen(
                WaveModeDv,
                waveLength,
                LatticeA,
                maxModeSpecified,
                Nodes,
                EdgeToElementNoH,
                Elements,
                Medias,
                ForceNodeNumberH,
                Ports[portNo - 1],
                ElemNoPeriodicList[portNo - 1],
                NodePeriodicBList[portNo - 1],
                DefectNodePeriodicList[portNo - 1],
                out nodesBoundary,
                out ryy_1d,
                out eigenValues,
                out eigenVecs,
                out eigen_dFdX,
                out nodePeriodic,
                out toNodePeriodic,
                out coordsPeriodic,
                out eigenVecsPeriodic
                );

            // 周期構造領域のモード分布をファイルに格納する
            string periodicDatFilename = FemOutputPeriodicDatFile.GetOutputPeriodicDatFilename(filename);
            FemOutputPeriodicDatFile.AppendToFile(
                periodicDatFilename,
                freqNo,
                portNo,
                waveLength,
                nodePeriodic,
                toNodePeriodic,
                coordsPeriodic,
                eigenValues,
                eigenVecsPeriodic
                );

        }

    }
}
