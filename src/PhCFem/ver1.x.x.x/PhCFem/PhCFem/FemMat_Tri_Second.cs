using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
//using System.Numerics; // Complex
using KrdLab.clapack; // KrdLab.clapack.Complex
//using System.Text.RegularExpressions;
using MyUtilLib.Matrix;

namespace PhCFem
{
    /// <summary>
    /// ２次三角形要素：ヘルムホルツ方程式の要素行列
    /// </summary>
    class FemMat_Tri_Second
    {
        /// <summary>
        /// ヘルムホルツ方程式に対する有限要素マトリクス作成
        /// </summary>
        /// <param name="waveLength">波長</param>
        /// <param name="toSorted">ソートされた節点インデックス（ 2D節点番号→ソート済みリストインデックスのマップ）</param>
        /// <param name="element">有限要素</param>
        /// <param name="Nodes">節点リスト</param>
        /// <param name="Medias">媒質リスト</param>
        /// <param name="ForceNodeNumberH">強制境界節点ハッシュ</param>
        /// <param name="WaveModeDv">計算する波のモード区分</param>
        /// <param name="mat">マージされる全体行列</param>
        public static  void AddElementMat(
            double waveLength,
            Dictionary<int, int> toSorted,
            FemElement element,
            IList<FemNode> Nodes,
            MediaInfo[] Medias,
            Dictionary<int, bool> ForceNodeNumberH,
            FemSolver.WaveModeDV WaveModeDv,
            ref MyComplexMatrix mat)
        {
            // 定数
            const double pi = Constants.pi;
            const double c0 = Constants.c0;
            // 波数
            double k0 = 2.0 * pi / waveLength;
            // 角周波数
            double omega = k0 * c0;

            // 要素頂点数
            const int vertexCnt = Constants.TriVertexCnt; //3;
            // 要素内節点数
            const int nno = Constants.TriNodeCnt_SecondOrder; //6;  // 2次三角形要素
            // 座標次元数
            const int ndim = Constants.CoordDim2D; //2;

            int[] nodeNumbers = element.NodeNumbers;
            int[] no_c = new int[nno];
            MediaInfo media = Medias[element.MediaIndex];  // ver1.1.0.0 媒質情報の取得
            double[,] media_P = null;
            double[,] media_Q = null;
            // ヘルムホルツ方程式のパラメータP,Qを取得する
            FemSolver.GetHelmholtzMediaPQ(
                k0,
                media,
                WaveModeDv,
                out media_P,
                out media_Q);

            // 節点座標(IFの都合上配列の配列形式の2次元配列を作成)
            double[][] pp = new double[nno][];
            for (int ino = 0; ino < nno; ino++)
            {
                int nodeNumber = nodeNumbers[ino];
                int nodeIndex = nodeNumber - 1;
                FemNode node = Nodes[nodeIndex];

                no_c[ino] = nodeNumber;
                pp[ino] = new double[ndim];
                for (int n = 0; n < ndim; n++)
                {
                    pp[ino][n] = node.Coord[n];
                }
            }
            // 面積を求める
            double area = KerEMatTri.TriArea(pp[0], pp[1], pp[2]);
            //System.Diagnostics.Debug.WriteLine("Elem No {0} area:  {1}", element.No, area);
            System.Diagnostics.Debug.Assert(area >= 0.0);

            // 面積座標の微分を求める
            //   dldx[k, n] k面積座標Lkのn方向微分
            double[,] dldx = null;
            double[] const_term = null;
            KerEMatTri.TriDlDx(out dldx, out const_term, pp[0], pp[1], pp[2]);

            // 形状関数の微分の係数を求める
            //    dndxC[ino,n,k]  ino節点のn方向微分のLk(k面積座標)の係数
            //       dNino/dn = dndxC[ino, n, 0] * L0 + dndxC[ino, n, 1] * L1 + dndxC[ino, n, 2] * L2 + dndxC[ino, n, 3]
            double[, ,] dndxC = new double[nno, ndim, vertexCnt + 1]
                {
                    {
                        {4.0 * dldx[0, 0], 0.0, 0.0, -1.0 * dldx[0, 0]},
                        {4.0 * dldx[0, 1], 0.0, 0.0, -1.0 * dldx[0, 1]},
                    },
                    {
                        {0.0, 4.0 * dldx[1, 0], 0.0, -1.0 * dldx[1, 0]},
                        {0.0, 4.0 * dldx[1, 1], 0.0, -1.0 * dldx[1, 1]},
                    },
                    {
                        {0.0, 0.0, 4.0 * dldx[2, 0], -1.0 * dldx[2, 0]},
                        {0.0, 0.0, 4.0 * dldx[2, 1], -1.0 * dldx[2, 1]},
                    },
                    {
                        {4.0 * dldx[1, 0], 4.0 * dldx[0, 0], 0.0, 0.0},
                        {4.0 * dldx[1, 1], 4.0 * dldx[0, 1], 0.0, 0.0},
                    },
                    {
                        {0.0, 4.0 * dldx[2, 0], 4.0 * dldx[1, 0], 0.0},
                        {0.0, 4.0 * dldx[2, 1], 4.0 * dldx[1, 1], 0.0},
                    },
                    {
                        {4.0 * dldx[2, 0], 0.0, 4.0 * dldx[0, 0], 0.0},
                        {4.0 * dldx[2, 1], 0.0, 4.0 * dldx[0, 1], 0.0},
                    },
                };

            // ∫dN/dndN/dn dxdy
            //     integralDNDX[n, ino, jno]  n = 0 --> ∫dN/dxdN/dx dxdy
            //                                n = 1 --> ∫dN/dydN/dy dxdy
            double[, ,] integralDNDX = new double[ndim, nno, nno];
            for (int n = 0; n < ndim; n++)
            {
                for (int ino = 0; ino < nno; ino++)
                {
                    for (int jno = 0; jno < nno; jno++)
                    {
                        integralDNDX[n, ino, jno]
                            = area / 6.0 * (dndxC[ino, n, 0] * dndxC[jno, n, 0] + dndxC[ino, n, 1] * dndxC[jno, n, 1] + dndxC[ino, n, 2] * dndxC[jno, n, 2])
                                  + area / 12.0 * (dndxC[ino, n, 0] * dndxC[jno, n, 1] + dndxC[ino, n, 0] * dndxC[jno, n, 2]
                                                      + dndxC[ino, n, 1] * dndxC[jno, n, 0] + dndxC[ino, n, 1] * dndxC[jno, n, 2]
                                                      + dndxC[ino, n, 2] * dndxC[jno, n, 0] + dndxC[ino, n, 2] * dndxC[jno, n, 1])
                                  + area / 3.0 * (dndxC[ino, n, 0] * dndxC[jno, n, 3] + dndxC[ino, n, 1] * dndxC[jno, n, 3]
                                                      + dndxC[ino, n, 2] * dndxC[jno, n, 3]
                                                      + dndxC[ino, n, 3] * dndxC[jno, n, 0] + dndxC[ino, n, 3] * dndxC[jno, n, 1]
                                                      + dndxC[ino, n, 3] * dndxC[jno, n, 2])
                                  + area * dndxC[ino, n, 3] * dndxC[jno, n, 3];
                    }
                }
            }
            // ∫N N dxdy
            double[,] integralN = new double[nno, nno]
                {
                    {  6.0 * area / 180.0, -1.0 * area / 180.0, -1.0 * area / 180.0,                 0.0, -4.0 * area / 180.0,                 0.0},
                    { -1.0 * area / 180.0,  6.0 * area / 180.0, -1.0 * area / 180.0,                 0.0,                 0.0, -4.0 * area / 180.0},
                    { -1.0 * area / 180.0, -1.0 * area / 180.0,  6.0 * area / 180.0, -4.0 * area / 180.0,                 0.0,                 0.0},
                    {                 0.0,                 0.0, -4.0 * area / 180.0, 32.0 * area / 180.0, 16.0 * area / 180.0, 16.0 * area / 180.0},
                    { -4.0 * area / 180.0,                 0.0,                 0.0, 16.0 * area / 180.0, 32.0 * area / 180.0, 16.0 * area / 180.0},
                    {                 0.0, -4.0 * area / 180.0,                 0.0, 16.0 * area / 180.0, 16.0 * area / 180.0, 32.0 * area / 180.0},
                };

            // 要素剛性行列を作る
            double[,] emat = new double[nno, nno];
            for (int ino = 0; ino < nno; ino++)
            {
                for (int jno = 0; jno < nno; jno++)
                {
                    emat[ino, jno] = media_P[0, 0] * integralDNDX[1, ino, jno] + media_P[1, 1] * integralDNDX[0, ino, jno]
                                         - k0 * k0 * media_Q[2, 2] * integralN[ino, jno];
                }
            }

            // 要素剛性行列にマージする
            for (int ino = 0; ino < nno; ino++)
            {
                int iNodeNumber = no_c[ino];
                if (ForceNodeNumberH.ContainsKey(iNodeNumber)) continue;
                int inoGlobal = toSorted[iNodeNumber];
                for (int jno = 0; jno < nno; jno++)
                {
                    int jNodeNumber = no_c[jno];
                    if (ForceNodeNumberH.ContainsKey(jNodeNumber)) continue;
                    int jnoGlobal = toSorted[jNodeNumber];

                    //mat[inoGlobal, jnoGlobal] += emat[ino, jno];
                    //mat._body[inoGlobal + jnoGlobal * mat.RowSize] += emat[ino, jno];
                    // 実数部に加算する
                    //mat._body[inoGlobal + jnoGlobal * mat.RowSize].Real += emat[ino, jno];
                    // バンドマトリクス対応
                    mat._body[mat.GetBufferIndex(inoGlobal, jnoGlobal)].Real += emat[ino, jno];
                }
            }
        }

        /// <summary>
        /// 周期構造導波路固有値問題用FEM行列の追加
        /// </summary>
        /// <param name="isYDirectionPeriodic"></param>
        /// <param name="waveLength"></param>
        /// <param name="nodeCntPeriodic"></param>
        /// <param name="freeNodeCntPeriodic_0"></param>
        /// <param name="toSortedPeriodic"></param>
        /// <param name="element"></param>
        /// <param name="Nodes"></param>
        /// <param name="Medias"></param>
        /// <param name="ForceNodeNumberH"></param>
        /// <param name="WaveModeDv"></param>
        /// <param name="KMat"></param>
        public static void AddElementMatPeriodic(
            bool isYDirectionPeriodic,
            bool isSVEA,
            double waveLength,
            int nodeCntPeriodic,
            int freeNodeCntPeriodic_0,
            Dictionary<int, int> toSortedPeriodic,
            FemElement element,
            IList<FemNode> Nodes,
            MediaInfo[] Medias,
            Dictionary<int, bool> ForceNodeNumberH,
            FemSolver.WaveModeDV WaveModeDv,
            ref double[] KMat,
            ref double[] CMat,
            ref double[] MMat)
        {
            // 定数
            const double pi = Constants.pi;
            const double c0 = Constants.c0;
            // 波数
            double k0 = 2.0 * pi / waveLength;
            // 角周波数
            double omega = k0 * c0;

            // 要素頂点数
            const int vertexCnt = Constants.TriVertexCnt; //3;
            // 要素内節点数
            const int nno = Constants.TriNodeCnt_SecondOrder; //6;  // 2次三角形要素
            // 座標次元数
            const int ndim = Constants.CoordDim2D; //2;

            int[] nodeNumbers = element.NodeNumbers;
            int[] no_c = new int[nno];
            MediaInfo media = Medias[element.MediaIndex];  // ver1.1.0.0 媒質情報の取得
            double[,] media_P = null;
            double[,] media_Q = null;
            // ヘルムホルツ方程式のパラメータP,Qを取得する
            FemSolver.GetHelmholtzMediaPQ(
                k0,
                media,
                WaveModeDv,
                out media_P,
                out media_Q);

            // 節点座標(IFの都合上配列の配列形式の2次元配列を作成)
            double[][] pp = new double[nno][];
            for (int ino = 0; ino < nno; ino++)
            {
                int nodeNumber = nodeNumbers[ino];
                int nodeIndex = nodeNumber - 1;
                FemNode node = Nodes[nodeIndex];

                no_c[ino] = nodeNumber;
                pp[ino] = new double[ndim];
                for (int n = 0; n < ndim; n++)
                {
                    pp[ino][n] = node.Coord[n];
                }
            }
            // 面積を求める
            double area = KerEMatTri.TriArea(pp[0], pp[1], pp[2]);
            //System.Diagnostics.Debug.WriteLine("Elem No {0} area:  {1}", element.No, area);
            System.Diagnostics.Debug.Assert(area >= 0.0);

            // 面積座標の微分を求める
            //   dldx[k, n] k面積座標Lkのn方向微分
            double[,] dldx = null;
            double[] const_term = null;
            KerEMatTri.TriDlDx(out dldx, out const_term, pp[0], pp[1], pp[2]);

            // 形状関数の微分の係数を求める
            //    dndxC[ino,n,k]  ino節点のn方向微分のLk(k面積座標)の係数
            //       dNino/dn = dndxC[ino, n, 0] * L0 + dndxC[ino, n, 1] * L1 + dndxC[ino, n, 2] * L2 + dndxC[ino, n, 3]
            double[, ,] dndxC = new double[nno, ndim, vertexCnt + 1]
                {
                    {
                        {4.0 * dldx[0, 0], 0.0, 0.0, -1.0 * dldx[0, 0]},
                        {4.0 * dldx[0, 1], 0.0, 0.0, -1.0 * dldx[0, 1]},
                    },
                    {
                        {0.0, 4.0 * dldx[1, 0], 0.0, -1.0 * dldx[1, 0]},
                        {0.0, 4.0 * dldx[1, 1], 0.0, -1.0 * dldx[1, 1]},
                    },
                    {
                        {0.0, 0.0, 4.0 * dldx[2, 0], -1.0 * dldx[2, 0]},
                        {0.0, 0.0, 4.0 * dldx[2, 1], -1.0 * dldx[2, 1]},
                    },
                    {
                        {4.0 * dldx[1, 0], 4.0 * dldx[0, 0], 0.0, 0.0},
                        {4.0 * dldx[1, 1], 4.0 * dldx[0, 1], 0.0, 0.0},
                    },
                    {
                        {0.0, 4.0 * dldx[2, 0], 4.0 * dldx[1, 0], 0.0},
                        {0.0, 4.0 * dldx[2, 1], 4.0 * dldx[1, 1], 0.0},
                    },
                    {
                        {4.0 * dldx[2, 0], 0.0, 4.0 * dldx[0, 0], 0.0},
                        {4.0 * dldx[2, 1], 0.0, 4.0 * dldx[0, 1], 0.0},
                    },
                };

            // ∫dN/dndN/dn dxdy
            //     integralDNDX[n, ino, jno]  n = 0 --> ∫dN/dxdN/dx dxdy
            //                                n = 1 --> ∫dN/dydN/dy dxdy
            double[, ,] integralDNDX = new double[ndim, nno, nno];
            for (int n = 0; n < ndim; n++)
            {
                for (int ino = 0; ino < nno; ino++)
                {
                    for (int jno = 0; jno < nno; jno++)
                    {
                        integralDNDX[n, ino, jno]
                            = area / 6.0 * (dndxC[ino, n, 0] * dndxC[jno, n, 0] + dndxC[ino, n, 1] * dndxC[jno, n, 1] + dndxC[ino, n, 2] * dndxC[jno, n, 2])
                                  + area / 12.0 * (dndxC[ino, n, 0] * dndxC[jno, n, 1] + dndxC[ino, n, 0] * dndxC[jno, n, 2]
                                                      + dndxC[ino, n, 1] * dndxC[jno, n, 0] + dndxC[ino, n, 1] * dndxC[jno, n, 2]
                                                      + dndxC[ino, n, 2] * dndxC[jno, n, 0] + dndxC[ino, n, 2] * dndxC[jno, n, 1])
                                  + area / 3.0 * (dndxC[ino, n, 0] * dndxC[jno, n, 3] + dndxC[ino, n, 1] * dndxC[jno, n, 3]
                                                      + dndxC[ino, n, 2] * dndxC[jno, n, 3]
                                                      + dndxC[ino, n, 3] * dndxC[jno, n, 0] + dndxC[ino, n, 3] * dndxC[jno, n, 1]
                                                      + dndxC[ino, n, 3] * dndxC[jno, n, 2])
                                  + area * dndxC[ino, n, 3] * dndxC[jno, n, 3];
                    }
                }
            }
            // ∫(dNi/dx)Nj dxdy
            // ∫(dNi/dy)Nj dxdy
            double[, ,] integralDNDXL = new double[2, nno, nno];
            for (int ino = 0; ino < nno; ino++)
            {
                for (int n = 0; n < ndim; n++)
                {
                    integralDNDXL[n, ino, 0] = (area / 30.0) * dndxC[ino, n, 0] - (area / 60.0) * (dndxC[ino, n, 1] + dndxC[ino, n, 2]);
                    integralDNDXL[n, ino, 1] = (area / 30.0) * dndxC[ino, n, 1] - (area / 60.0) * (dndxC[ino, n, 0] + dndxC[ino, n, 2]);
                    integralDNDXL[n, ino, 2] = (area / 30.0) * dndxC[ino, n, 2] - (area / 60.0) * (dndxC[ino, n, 0] + dndxC[ino, n, 1]);
                    integralDNDXL[n, ino, 3] = (area / 15.0) * (2.0 * dndxC[ino, n, 0] + 2.0 * dndxC[ino, n, 1] + dndxC[ino, n, 2] + 5.0 * dndxC[ino, n, 3]);
                    integralDNDXL[n, ino, 4] = (area / 15.0) * (dndxC[ino, n, 0] + 2.0 * dndxC[ino, n, 1] + 2.0 * dndxC[ino, n, 2] + 5.0 * dndxC[ino, n, 3]);
                    integralDNDXL[n, ino, 5] = (area / 15.0) * (2.0 * dndxC[ino, n, 0] + dndxC[ino, n, 1] + 2.0 * dndxC[ino, n, 2] + 5.0 * dndxC[ino, n, 3]);
                }
            }

            // ∫N N dxdy
            double[,] integralN = new double[nno, nno]
                {
                    {  6.0 * area / 180.0, -1.0 * area / 180.0, -1.0 * area / 180.0,                 0.0, -4.0 * area / 180.0,                 0.0},
                    { -1.0 * area / 180.0,  6.0 * area / 180.0, -1.0 * area / 180.0,                 0.0,                 0.0, -4.0 * area / 180.0},
                    { -1.0 * area / 180.0, -1.0 * area / 180.0,  6.0 * area / 180.0, -4.0 * area / 180.0,                 0.0,                 0.0},
                    {                 0.0,                 0.0, -4.0 * area / 180.0, 32.0 * area / 180.0, 16.0 * area / 180.0, 16.0 * area / 180.0},
                    { -4.0 * area / 180.0,                 0.0,                 0.0, 16.0 * area / 180.0, 32.0 * area / 180.0, 16.0 * area / 180.0},
                    {                 0.0, -4.0 * area / 180.0,                 0.0, 16.0 * area / 180.0, 16.0 * area / 180.0, 32.0 * area / 180.0},
                };

            // 要素剛性行列を作る
            double[,] eKMat = new double[nno, nno];
            double[,] eCMat = new double[nno, nno];
            double[,] eMMat = new double[nno, nno];
            for (int ino = 0; ino < nno; ino++)
            {
                for (int jno = 0; jno < nno; jno++)
                {
                    eKMat[ino, jno] = -media_P[0, 0] * integralDNDX[1, ino, jno] - media_P[1, 1] * integralDNDX[0, ino, jno]
                                         + k0 * k0 * media_Q[2, 2] * integralN[ino, jno];
                    if (isSVEA)
                    {
                        if (isYDirectionPeriodic)
                        {
                            eCMat[ino, jno] = -media.P[1, 1] * (integralDNDXL[1, ino, jno] - integralDNDXL[1, jno, ino]);
                        }
                        else
                        {
                            eCMat[ino, jno] = -media.P[1, 1] * (integralDNDXL[0, ino, jno] - integralDNDXL[0, jno, ino]);
                        }
                        // 要素質量行列
                        eMMat[ino, jno] = media.P[1, 1] * integralN[ino, jno];
                    }
                }
            }

            // 要素剛性行列にマージする
            for (int ino = 0; ino < nno; ino++)
            {
                int iNodeNumber = no_c[ino];
                if (ForceNodeNumberH.ContainsKey(iNodeNumber)) continue;
                int inoGlobal = toSortedPeriodic[iNodeNumber];
                for (int jno = 0; jno < nno; jno++)
                {
                    int jNodeNumber = no_c[jno];
                    if (ForceNodeNumberH.ContainsKey(jNodeNumber)) continue;
                    int jnoGlobal = toSortedPeriodic[jNodeNumber];

                    KMat[inoGlobal + freeNodeCntPeriodic_0 * jnoGlobal] += eKMat[ino, jno];
                    if (isSVEA)
                    {
                        CMat[inoGlobal + freeNodeCntPeriodic_0 * jnoGlobal] += eCMat[ino, jno];
                        MMat[inoGlobal + freeNodeCntPeriodic_0 * jnoGlobal] += eMMat[ino, jno];
                    }
                }
            }
        }
    }
}
