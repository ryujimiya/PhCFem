using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

namespace PhCFem
{
    class Constants
    {
        ////////////////////////////////////////////////////////////////////////
        // 定数
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// π
        /// </summary>
        public const double pi = wg2d.WgUtil.pi;
        /// <summary>
        /// 真空中の光速
        /// </summary>
        public const double c0 = wg2d.WgUtil.c0;
        /// <summary>
        /// 真空中の透磁率
        /// </summary>
        public const double mu0 = wg2d.WgUtil.mu0;
        /// <summary>
        /// 真空中の誘電率
        /// </summary>1
        public const double eps0 = wg2d.WgUtil.eps0;
        /// <summary>
        /// 計算精度下限
        /// </summary>
        public const double PrecisionLowerLimit = 1.0e-12;

        /// <summary>
        /// 分割数
        /// </summary>
        //public static readonly Size MaxDiv = new Size(30, 30);
        public static readonly Size MaxDiv = new Size(40, 40);
        /// <summary>
        /// CADデータファイル拡張子
        /// </summary>
        public static readonly string CadExt = ".cad";
        /// <summary>
        /// Fem入力データファイル拡張子
        /// </summary>
        public static readonly string FemInputExt = ".fem";
        /// <summary>
        /// Fem出力結果データファイル拡張子
        /// </summary>
        public static readonly string FemOutputExt = ".out";
        /// <summary>
        /// Fem出力結果インデックスファイル拡張子
        /// </summary>
        public static readonly string FemOutputIndexExt = ".idx";
        /// <summary>
        /// Fem出力結果データファイル拡張子(周期構造固有モード格納用)
        /// </summary>
        public static readonly string FemOutputPeriodicExt = ".outp";
        /// <summary>
        /// 媒質の個数
        /// </summary>
        public const int MaxMediaCount = 2;
        /// <summary>
        /// 計算周波数範囲(既定値)
        /// </summary>
        public static readonly double[] DefNormalizedFreqRange = new double[] { 0.300, 0.440 };
        /// <summary>
        /// 計算する周波数の数(既定値)
        /// </summary>
        //public const int DefCalcFreqencyPointCount = 20;
        public const int DefCalcFreqencyPointCount = 28;
        /// <summary>
        /// 考慮するモード数
        /// </summary>
        public const int MaxModeCount = int.MaxValue;
        /// <summary>
        /// 格子１辺の分割数(既定値)
        /// </summary>
        public const int DefNDivForOneLattice = 7;
        /// <summary>
        /// ロッドの円周の分割数(既定値)
        /// </summary>
        public const int DefRodCircleDiv = 8;
        /// <summary>
        /// ロッドの半径の分割数(既定値)
        /// </summary>
        public const int DefRodRadiusDiv = 1;
        /// <summary>
        /// 誘電体ロッドの比誘電率(既定値)
        /// </summary>
        public const double DefRodEps = 3.4 * 3.4;
        /// <summary>
        /// 誘電体ロッドの半径割合(既定値)
        /// </summary>
        public const double DefRodRadiusRatio = 0.18;
        /// <summary>
        /// 計算する波のモード区分（既定値）
        /// </summary>
        public const FemSolver.WaveModeDV DefWaveModeDv = FemSolver.WaveModeDV.TE;
        /// <summary>
        /// 界分布表示における三角形要素内の分割数
        /// </summary>
        public const int TriDrawFieldMshDivCnt = 4;

        /////////////////////////////////////////////////////////////////////////
        // 要素関連
        /////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 要素形状区分
        /// </summary>
        public enum FemElementShapeDV { Line, Triangle };
        /// <summary>
        /// ２次元座標次数
        /// </summary>
        public const int CoordDim2D = 2;
        /// <summary>
        /// 三角形要素頂点数
        /// </summary>
        public const int TriVertexCnt = 3;
        /// <summary>
        /// 線要素頂点数
        /// </summary>
        public const int LineVertexCnt = 2;
        /// <summary>
        /// 要素次数１次
        /// </summary>
        public const int FirstOrder = 1;
        /// <summary>
        /// 要素次数２次
        /// </summary>
        public const int SecondOrder = 2;
        /// <summary>
        /// 線要素節点数
        /// </summary>
        public const int LineNodeCnt_FirstOrder = 2;
        public const int LineNodeCnt_SecondOrder = 3;
        /// <summary>
        /// 三角形要素節点数
        /// </summary>
        public const int TriNodeCnt_FirstOrder = 3;
        public const int TriNodeCnt_SecondOrder = 6;
        /// <summary>
        /// 三角形要素の辺の数
        /// </summary>
        public const int TriEdgeCnt_FirstOrder = 3;
        public const int TriEdgeCnt_SecondOrder = 6; // 要素内部の辺は含まない

        /// <summary>
        /// 有限要素の形状区分既定値
        /// </summary>
        public const FemElementShapeDV DefElemShapeDv = FemElementShapeDV.Triangle;
        /// <summary>
        /// 有限要素の補間次数既定値
        /// </summary>
        //public const int DefElementOrder = SecondOrder;
        public const int DefElementOrder = FirstOrder;

    }
}
