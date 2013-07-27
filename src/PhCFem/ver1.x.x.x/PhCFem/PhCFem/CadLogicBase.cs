using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace PhCFem
{
    /// <summary>
    /// CadLogicのデータを管理
    /// </summary>
    class CadLogicBase
    {
        ////////////////////////////////////////////////////////////////////////
        // 型
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Cadモード
        ///   None 操作なし
        ///   Locatiton 矩形領域位置移動
        ///   Area マス目選択
        ///   Port ポート境界選択
        ///   Erase 消しゴム
        ///   IncidentPort 入力ポート選択
        ///   PortNumbering 入出力ポート番号振り
        /// </summary>
        public enum CadModeType { None, Location, Area, Port, Erase, IncidentPort, PortNumbering };
        /// <summary>
        /// セルの種類
        ///   Defect  真空(欠陥部)
        ///   Rod     誘電体ロッド
        /// </summary>
        public enum CellType { Empty, Defect, Rod };
        /// <summary>
        /// セルの種類一覧
        /// </summary>
        public static readonly CellType[] CellTypeList = { CellType.Empty, CellType.Defect, CellType.Rod };
        /// <summary>
        /// セルの種類一覧（文字列)
        /// </summary>
        public static readonly string[] CellTypeStrList = { "Empty", "Defect", "Rod" };
        /// <summary>
        /// セルの種類→文字列
        /// </summary>
        /// <param name="cellType"></param>
        /// <returns></returns>
        public static string GetCellTypeStr(CellType cellType)
        {
            for (int i = 0; i < CellTypeList.Length; i++)
            {
                if (CellTypeList[i] == cellType)
                {
                    return CellTypeStrList[i];
                }
            }
            return "";
        }
        /// <summary>
        /// 文字列→セルの種類
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static CellType GetCellTypeFromStr(string str)
        {
            for (int i = 0; i < CellTypeList.Length; i++)
            {
                if (CellTypeStrList[i] == str)
                {
                    return CellTypeList[i];
                }
            }
            return CellType.Empty;
        }

        ////////////////////////////////////////////////////////////////////////
        // 定数
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 分割数
        /// </summary>
        protected static readonly Size MaxDiv = Constants.MaxDiv;
        /// <summary>
        /// 媒質の背景色：真空
        /// </summary>
        public static readonly Color VacumnBackColor = Color.Gray;
        /// <summary>
        /// 媒質の背景色：誘電体ロッド
        /// </summary>
        public static readonly Color RodBackColor = Color.Orange;
        /// <summary>
        /// セルの既定値
        /// </summary>
        public const CellType DefCellType = CellType.Rod;

        ////////////////////////////////////////////////////////////////////////
        // フィールド
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 領域選択フラグアレイ
        /// </summary>
        protected CellType[,] AreaSelection = new CellType[MaxDiv.Height, MaxDiv.Width];
        /// <summary>
        /// Y軸方向境界フラグアレイ
        /// </summary>
        protected bool[,] YBoundarySelection = new bool[MaxDiv.Height, MaxDiv.Width + 1];
        /// <summary>
        /// X軸方向境界フラグアレイ
        /// </summary>
        protected bool[,] XBoundarySelection = new bool[MaxDiv.Height + 1, MaxDiv.Width];
        /// <summary>
        /// 境界リストのリスト
        /// </summary>
        protected IList<Edge> EdgeList = new List<Edge>();
        /// <summary>
        /// 入射ポート番号
        /// </summary>
        protected int IncidentPortNo = 1;
        /// <summary>
        /// 媒質リスト
        /// </summary>
        protected MediaInfo[] Medias = new MediaInfo[Constants.MaxMediaCount];
        /// <summary>
        /// 格子1辺の分割数
        /// </summary>
        protected int _NdivForOneLattice = 0;
        /// <summary>
        /// ロッドの半径割合
        /// </summary>
        protected double _RodRadiusRatio = 0.0;
        /// <summary>
        /// ロッドの円周方向分割数
        /// </summary>
        protected int _RodCircleDiv = 0;
        /// <summary>
        /// ロッドの半径方向分割数(1でもメッシュサイズが小さければ複数に分割される)
        /// </summary>
        protected int _RodRadiusDiv = 0;
        /// <summary>
        /// Cadモード
        /// </summary>
        protected CadModeType _CadMode = CadModeType.None;
        
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public CadLogicBase()
        {
            init();
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected void init()
        {
            _CadMode = CadModeType.None;

            for (int y = 0; y < MaxDiv.Height; y++)
            {
                for (int x = 0; x < MaxDiv.Width; x++)
                {
                    AreaSelection[y, x] = CellType.Empty;
                }
            }
            for (int x = 0; x < MaxDiv.Width + 1; x++)
            {
                for (int y = 0; y < MaxDiv.Height; y++)
                {
                    YBoundarySelection[y, x] = false;
                }
            }
            for (int y = 0; y < MaxDiv.Height + 1; y++)
            {
                for (int x = 0; x < MaxDiv.Width; x++)
                {
                    XBoundarySelection[y, x] = false;
                }
            }
            EdgeList.Clear();
            IncidentPortNo = 1;
            System.Diagnostics.Debug.Assert(Medias.Length == 2);
            Color[] workBackColorList = { VacumnBackColor, RodBackColor };
            double[] workEpsList = { 1.0, Constants.DefRodEps };
            for (int i = 0; i < Medias.Length; i++)
            {
                MediaInfo media = new MediaInfo();
                double eps = workEpsList[i];
                media.BackColor = workBackColorList[i];
                media.SetP(new double[3,3] {
                    {1.0 / 1.0, 0.0, 0.0},
                    {0.0, 1.0 / 1.0, 0.0},
                    {0.0, 0.0, 1.0 / 1.0},
                } );
                media.SetQ(new double[3, 3] {
                    {eps, 0.0, 0.0},
                    {0.0, eps, 0.0},
                    {0.0, 0.0, eps},
                });
                Medias[i] = media;
            }
            _NdivForOneLattice = Constants.DefNDivForOneLattice;
            _RodRadiusRatio = Constants.DefRodRadiusRatio;
            _RodCircleDiv = Constants.DefRodCircleDiv;
            _RodRadiusDiv = Constants.DefRodRadiusDiv;
        }

        /// <summary>
        /// Cadデータをコピーする
        /// </summary>
        /// <param name="src"></param>
        public void CopyData(CadLogicBase src)
        {
            // CadモードもUndo/Redo対象に入れる
            _CadMode = src._CadMode;

            for (int y = 0; y < MaxDiv.Height; y++)
            {
                for (int x = 0; x < MaxDiv.Width; x++)
                {
                    AreaSelection[y, x] = src.AreaSelection[y, x];
                }
            }
            for (int x = 0; x < MaxDiv.Width + 1; x++)
            {
                for (int y = 0; y < MaxDiv.Height; y++)
                {
                    YBoundarySelection[y, x] = src.YBoundarySelection[y, x];
                }
            }
            for (int y = 0; y < MaxDiv.Height + 1; y++)
            {
                for (int x = 0; x < MaxDiv.Width; x++)
                {
                    XBoundarySelection[y, x] = src.XBoundarySelection[y, x];
                }
            }
            EdgeList.Clear();
            foreach (Edge srcEdge in src.EdgeList)
            {
                Edge edge = new Edge(srcEdge.Delta);
                edge.CP(srcEdge);
                EdgeList.Add(edge);
            }
            IncidentPortNo = src.IncidentPortNo;
            //SelectedMediaIndex = src.SelectedMediaIndex;
            for (int i = 0; i < src.Medias.Length; i++)
            {
                Medias[i].SetP(src.Medias[i].P);
                Medias[i].SetQ(src.Medias[i].Q);
            }
            _NdivForOneLattice = src._NdivForOneLattice;
            _RodRadiusRatio = src._RodRadiusRatio;
            _RodCircleDiv = src._RodCircleDiv;
            _RodRadiusDiv = src._RodRadiusDiv;
        }

    }
}
