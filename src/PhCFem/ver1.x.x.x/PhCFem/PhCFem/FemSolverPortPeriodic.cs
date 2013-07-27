using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
//using System.Numerics; // Complex
using KrdLab.clapack; // KrdLab.clapack.Complex
//using System.Text.RegularExpressions;
using MyUtilLib.Matrix;

namespace PhCFem
{
    /// <summary>
    /// ポート固有モード解析（周期構造導波路）
    /// </summary>
    class FemSolverPortPeriodic
    {
        ////////////////////////////////////////////////////////////////////////
        // 定数
        ////////////////////////////////////////////////////////////////////////
        private const double pi = Constants.pi;
        private const double c0 = Constants.c0;
        private const double mu0 = Constants.mu0;
        private const double eps0 = Constants.eps0;


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
        public static void AddPortBC(
            FemSolver.WaveModeDV WaveModeDv,
            double waveLength,
            double latticeA,
            bool isInputPort,
            int IncidentModeIndex,
            int[] nodesBoundary,
            MyDoubleMatrix ryy_1d,
            Complex[] eigenValues,
            Complex[,] eigenVecs,
            Complex[,] eigen_dFdXs,
            int[] nodesRegion,
            IList<FemElement> Elements,
            MediaInfo[] Medias,
            Dictionary<int, bool> ForceNodeNumberH,
            MyComplexMatrix mat,
            Complex[] resVec)
        {
            double k0 = 2.0 * pi / waveLength;
            double omega = k0 / Math.Sqrt(mu0 * eps0);

            // 境界上の節点数(1次線要素を想定)
            int nodeCnt = nodesBoundary.Length;
            // 考慮するモード数
            int maxMode = eigenValues.Length;

            // 全体剛性行列の作成
            MyComplexMatrix matB = new MyComplexMatrix(nodeCnt, nodeCnt);

            double periodicDistance = latticeA; // 正方格子
            for (int imode = 0; imode < maxMode; imode++)
            {
                Complex betam = eigenValues[imode];

                Complex[] fmVec = MyMatrixUtil.matrix_GetRowVec(eigenVecs, (int)imode);
                Complex[] dfmdxVec = MyMatrixUtil.matrix_GetRowVec(eigen_dFdXs, (int)imode);
                Complex[] fmVec_Modify = new Complex[fmVec.Length];
                Complex betam_periodic = ToBetaPeriodic(betam, periodicDistance);
                for (uint inoB = 0; inoB < nodeCnt; inoB++)
                {
                    fmVec_Modify[inoB] = fmVec[inoB] - dfmdxVec[inoB] / (Complex.ImaginaryOne * betam_periodic);
                }
                // 2Dの境界積分
                //Complex[] veci = MyMatrixUtil.product(ryy_1d, fmVec);
                // モード電力規格化の積分(1Dの積分)
                // [ryy]が実数の場合
                //Complex[] vecj = MyMatrixUtil.product(ryy_1d, MyMatrixUtil.vector_Conjugate(fmVec));
                Complex[] veci = MyMatrixUtil.product(ryy_1d, fmVec_Modify);
                Complex[] vecj = MyMatrixUtil.product(ryy_1d, MyMatrixUtil.vector_Conjugate(fmVec_Modify));

                for (int inoB = 0; inoB < nodeCnt; inoB++)
                {
                    for (int jnoB = 0; jnoB < nodeCnt; jnoB++)
                    {
                        Complex cvalue;
                        {
                            // H面、平行平板
                            if (WaveModeDv == FemSolver.WaveModeDV.TM)
                            {
                                //cvalue = (Complex.ImaginaryOne / (omega * eps0)) * betam * Complex.Abs(betam) * veci[inoB] * vecj[jnoB];
                                cvalue = (Complex.ImaginaryOne / (omega * eps0)) * (Complex.Abs(betam) * betam_periodic * Complex.Conjugate(betam_periodic) / Complex.Conjugate(betam)) * veci[inoB] * vecj[jnoB];
                            }
                            else
                            {
                                //cvalue = (Complex.ImaginaryOne / (omega * mu0)) * betam * Complex.Abs(betam) * veci[inoB] * vecj[jnoB];
                                cvalue = (Complex.ImaginaryOne / (omega * mu0)) * (Complex.Abs(betam) * betam_periodic * Complex.Conjugate(betam_periodic) / Complex.Conjugate(betam)) * veci[inoB] * vecj[jnoB];
                            }
                        }
                        matB._body[inoB + jnoB * matB.RowSize] += cvalue;
                    }
                }
            }
            // check 対称行列
            bool isSymmetrix = true;
            for (int inoB = 0; inoB < matB.RowSize; inoB++)
            {
                for (int jnoB = inoB; jnoB < matB.ColumnSize; jnoB++)
                {
                    if (Math.Abs(matB[inoB, jnoB].Real - matB[jnoB, inoB].Real) >= Constants.PrecisionLowerLimit)
                    {
                        //System.Diagnostics.Debug.Assert(false);
                        isSymmetrix = false;
                        break;
                    }
                    if (Math.Abs(matB[inoB, jnoB].Imaginary - matB[jnoB, inoB].Imaginary) >= Constants.PrecisionLowerLimit)
                    {
                        //System.Diagnostics.Debug.Assert(false);
                        isSymmetrix = false;
                        break;
                    }
                }
            }
            if (!isSymmetrix)
            {
                System.Diagnostics.Debug.WriteLine("!!!!!!!!!!matrix is NOT symmetric!!!!!!!!!!!!!!");
                //System.Diagnostics.Debug.Assert(false);
            }
            //MyMatrixUtil.printMatrix("matB", matB);

            // 残差ベクトルの作成
            Complex[] resVecB = new Complex[nodeCnt];
            if (isInputPort)
            {
                int imode = IncidentModeIndex;
                Complex betam = eigenValues[imode];
                Complex[] fmVec = MyMatrixUtil.matrix_GetRowVec(eigenVecs, (int)imode);
                Complex[] dfmdxVec = MyMatrixUtil.matrix_GetRowVec(eigen_dFdXs, (int)imode);
                Complex[] fmVec_Modify = new Complex[fmVec.Length];
                Complex betam_periodic = ToBetaPeriodic(betam, periodicDistance);
                for (uint inoB = 0; inoB < nodeCnt; inoB++)
                {
                    fmVec_Modify[inoB] = fmVec[inoB] - dfmdxVec[inoB] / (Complex.ImaginaryOne * betam_periodic);
                }

                // 2Dの境界積分
                //Complex[] veci = MyMatrixUtil.product(ryy_1d, fmVec);
                Complex[] veci = MyMatrixUtil.product(ryy_1d, fmVec_Modify);
                for (int inoB = 0; inoB < nodeCnt; inoB++)
                {
                    // H面、平行平板、E面
                    //Complex cvalue = 2.0 * Complex.ImaginaryOne * betam * veci[inoB];
                    Complex cvalue = 2.0 * Complex.ImaginaryOne * betam_periodic * veci[inoB];
                    resVecB[inoB].Real = cvalue.Real;
                    resVecB[inoB].Imaginary = cvalue.Imaginary;
                }
            }
            //printVec("resVecB", resVecB);

            // 2D節点番号→ソート済みリストインデックスのマップ
            Dictionary<int, int> toSorted = new Dictionary<int, int>();
            // 2D節点番号→ソート済みリストインデックスのマップ作成
            for (int i = 0; i < nodesRegion.Length; i++)
            {
                int nodeNumber = nodesRegion[i];
                if (!toSorted.ContainsKey(nodeNumber))
                {
                    toSorted.Add(nodeNumber, i);
                }
            }

            // 要素剛性行列にマージ
            //   この定式化では行列のスパース性は失われている(隣接していない要素の節点間にも関連がある)
            // 要素剛性行列にマージする
            for (int inoB = 0; inoB < nodeCnt; inoB++)
            {
                int iNodeNumber = nodesBoundary[inoB];
                if (ForceNodeNumberH.ContainsKey(iNodeNumber)) continue;
                int inoGlobal = toSorted[iNodeNumber];
                for (int jnoB = 0; jnoB < nodeCnt; jnoB++)
                {
                    int jNodeNumber = nodesBoundary[jnoB];
                    if (ForceNodeNumberH.ContainsKey(jNodeNumber)) continue;
                    int jnoGlobal = toSorted[jNodeNumber];

                    // Note: matBは一般行列 matはバンド行列
                    mat._body[mat.GetBufferIndex(inoGlobal, jnoGlobal)] += matB._body[inoB + jnoB * matB.RowSize];
                }
            }

            // 残差ベクトルにマージ
            for (int inoB = 0; inoB < nodeCnt; inoB++)
            {
                int iNodeNumber = nodesBoundary[inoB];
                if (ForceNodeNumberH.ContainsKey(iNodeNumber)) continue;
                int inoGlobal = toSorted[iNodeNumber];

                resVec[inoGlobal] += resVecB[inoB];
            }
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
        public static Complex GetWaveguidePortReflectionCoef(
            FemSolver.WaveModeDV WaveModeDv,
            double waveLength,
            double latticeA,
            int iMode,
            bool isIncidentMode,
            int[] nodesBoundary,
            MyDoubleMatrix ryy_1d,
            Complex[] eigenValues,
            Complex[,] eigenVecs,
            Complex[,] eigen_dFdXs,
            int[] nodesRegion,
            IList<FemElement> Elements,
            MediaInfo[] Medias,
            Complex[] valuesAll)
        {
            // 2D節点番号→ソート済みリストインデックスのマップ
            Dictionary<int, int> toSorted = new Dictionary<int, int>();
            // 2D節点番号→ソート済みリストインデックスのマップ作成
            for (int i = 0; i < nodesRegion.Length; i++)
            {
                int nodeNumber = nodesRegion[i];
                if (!toSorted.ContainsKey(nodeNumber))
                {
                    toSorted.Add(nodeNumber, i);
                }
            }

            // ポート上の界を取得する
            int nodeCnt = nodesBoundary.Length;
            Complex[] valuesB = new Complex[nodeCnt];
            for (int ino = 0; ino < nodeCnt; ino++)
            {
                int nodeNumber = nodesBoundary[ino];
                int inoGlobal = toSorted[nodeNumber];
                valuesB[ino] = valuesAll[inoGlobal];
            }

            double k0 = 2.0 * pi / waveLength;
            double omega = k0 * c0;

            Complex s11 = new Complex(0.0, 0.0);

            int maxMode = eigenValues.Length;

            double periodicDistance = latticeA; // 正方格子

            // {tmp_vec}*t = {fm}*t[ryy]*t
            // {tmp_vec}* = [ryy]* {fm}*
            //   ([ryy]*)t = [ryy]*
            //    [ryy]が実数のときは、[ryy]* -->[ryy]
            Complex[] fmVec = MyMatrixUtil.matrix_GetRowVec(eigenVecs, iMode);
            Complex[] dFdXVec = MyMatrixUtil.matrix_GetRowVec(eigen_dFdXs, (int)iMode);
            Complex[] fmVec_Modify = new Complex[fmVec.Length];
            Complex betam = eigenValues[iMode];
            Complex betam_periodic = ToBetaPeriodic(betam, periodicDistance);
            for (int ino = 0; ino < fmVec_Modify.Length; ino++)
            {
                fmVec_Modify[ino] = fmVec[ino] - dFdXVec[ino] / (Complex.ImaginaryOne * betam_periodic);
            }

            // ryyが実数のとき
            //Complex[] tmp_vec = MyMatrixUtil.product(ryy_1d, MyMatrixUtil.vector_Conjugate(fmVec));
            Complex[] tmp_vec = MyMatrixUtil.product(ryy_1d, MyMatrixUtil.vector_Conjugate(fmVec_Modify));

            // s11 = {tmp_vec}t {value_all}
            s11 = MyMatrixUtil.vector_Dot(tmp_vec, valuesB);
            {
                // H面、平行平板
                if (WaveModeDv == FemSolver.WaveModeDV.TM)
                {
                    //s11 *= (Complex.Abs(betam) / (omega * eps0));
                    s11 *= ((Complex.Abs(betam) * Complex.Conjugate(betam_periodic) / Complex.Conjugate(betam)) / (omega * eps0));
                    if (isIncidentMode)
                    {
                        s11 += -1.0;
                    }
                }
                else
                {
                    //s11 *= (Complex.Abs(betam) / (omega * mu0));
                    s11 *= ((Complex.Abs(betam) * Complex.Conjugate(betam_periodic) / Complex.Conjugate(betam)) / (omega * mu0));
                    if (isIncidentMode)
                    {
                        s11 += -1.0;
                    }
                }
            }

            return s11;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// ポート境界の1D FEM行列 ryy_1dを取得する
        /// </summary>
        /// <param name="WaveModeDv"></param>
        /// <param name="waveLength"></param>
        /// <param name="latticeA"></param>
        /// <param name="Nodes"></param>
        /// <param name="EdgeToElementNoH"></param>
        /// <param name="Elements"></param>
        /// <param name="Medias"></param>
        /// <param name="ForceNodeNumberH"></param>
        /// <param name="nodesBoundary"></param>
        /// <param name="ryy_1d"></param>
        private static void getPortFemMat1D(
            FemSolver.WaveModeDV WaveModeDv,
            double waveLength,
            double latticeA,
            IList<FemNode> Nodes,
            Dictionary<string, IList<int>> EdgeToElementNoH,
            IList<FemElement> Elements,
            MediaInfo[] Medias,
            Dictionary<int, bool> ForceNodeNumberH,
            IList<int> portNodes,
            out int[] nodesBoundary,
            out MyDoubleMatrix ryy_1d
            )
        {
            nodesBoundary = null;
            ryy_1d = null;

            // 2D次元数
            const int ndim2d = Constants.CoordDim2D; //2;
            // 波数
            double k0 = 2.0 * pi / waveLength;
            // 角周波数
            double omega = k0 * c0;

            // 節点番号リスト(要素インデックス: 1D節点番号 - 1 要素:2D節点番号)
            IList<int> nodes = portNodes;
            // 2D→1D節点番号マップ
            Dictionary<int, int> to1dNodes = new Dictionary<int, int>();
            // 節点座標リスト
            IList<double> coords = new List<double>();
            // 要素リスト
            IList<FemLineElement> elements = new List<FemLineElement>();
            // 1D節点番号リスト（ソート済み）
            IList<int> sortedNodes = new List<int>();
            // 1D節点番号→ソート済みリストインデックスのマップ
            Dictionary<int, int> toSorted = new Dictionary<int, int>();

            // 2Dの要素から次数を取得する
            Constants.FemElementShapeDV elemShapeDv2d;
            int order;
            int vertexCnt2d;
            FemMeshLogic.GetElementShapeDvAndOrderByElemNodeCnt(Elements[0].NodeNumbers.Length, out elemShapeDv2d, out order, out vertexCnt2d);

            // 2D→1D節点番号マップ作成
            for (int i = 0; i < nodes.Count; i++)
            {
                int nodeNumber2d = nodes[i];
                if (!to1dNodes.ContainsKey(nodeNumber2d))
                {
                    to1dNodes.Add(nodeNumber2d, i + 1);
                }
            }
            // 原点
            int nodeNumber0 = nodes[0];
            int nodeIndex0 = nodeNumber0 - 1;
            FemNode node0 = Nodes[nodeIndex0];
            double[] coord0 = new double[ndim2d];
            coord0[0] = node0.Coord[0];
            coord0[1] = node0.Coord[1];
            // 座標リスト作成
            double[] coord = new double[ndim2d];
            foreach (int nodeNumber in nodes)
            {
                int nodeIndex = nodeNumber - 1;
                FemNode node = Nodes[nodeIndex];
                coord[0] = node.Coord[0];
                coord[1] = node.Coord[1];
                double x = FemMeshLogic.GetDistance(coord, coord0);
                //System.Diagnostics.Debug.WriteLine("{0},{1},{2},{3}", nodeIndex, coord[0], coord[1], x);
                coords.Add(x);
            }

            // 線要素を作成する
            if (order == Constants.FirstOrder)
            {
                // １次線要素
                FemMat_Line_First.MkElements(
                    nodes,
                    EdgeToElementNoH,
                    Elements,
                    ref elements);
            }
            else
            {
                // ２次線要素
                FemMat_Line_Second.MkElements(
                    nodes,
                    EdgeToElementNoH,
                    Elements,
                    ref elements);
            }

            // 強制境界節点と内部領域節点を分離
            foreach (int nodeNumber2d in nodes)
            {
                int nodeNumber = to1dNodes[nodeNumber2d];
                if (ForceNodeNumberH.ContainsKey(nodeNumber2d))
                {
                    System.Diagnostics.Debug.WriteLine("{0}:    {1}    {2}", nodeNumber, Nodes[nodeNumber2d - 1].Coord[0], Nodes[nodeNumber2d - 1].Coord[1]);
                }
                else
                {
                    sortedNodes.Add(nodeNumber);
                    toSorted.Add(nodeNumber, sortedNodes.Count - 1);
                }
            }
            // 対称バンド行列のパラメータを取得する
            int rowcolSize = 0;
            int subdiaSize = 0;
            int superdiaSize = 0;
            {
                bool[,] matPattern = null;
                FemSolverPort.GetMatNonzeroPatternForEigen(elements, toSorted, out matPattern);
                FemSolverPort.GetBandMatrixSubDiaSizeAndSuperDiaSizeForEigen(matPattern, out rowcolSize, out subdiaSize, out superdiaSize);
            }
            // ソート済み1D節点インデックス→2D節点番号マップ
            nodesBoundary = new int[sortedNodes.Count];
            for (int i = 0; i < sortedNodes.Count; i++)
            {
                int nodeNumber = sortedNodes[i];
                int nodeIndex = nodeNumber - 1;
                int nodeNumber2d = nodes[nodeIndex];
                nodesBoundary[i] = nodeNumber2d;
            }

            // 節点数
            int nodeCnt = sortedNodes.Count;
            // 固有モード解析でのみ使用するuzz_1d, txx_1d
            MyDoubleMatrix txx_1d = new MyDoubleSymmetricBandMatrix(nodeCnt, subdiaSize, superdiaSize);
            MyDoubleMatrix uzz_1d = new MyDoubleSymmetricBandMatrix(nodeCnt, subdiaSize, superdiaSize);
            // ryy_1dマトリクス (線要素)
            ryy_1d = new MyDoubleSymmetricBandMatrix(nodeCnt, subdiaSize, superdiaSize);

            for (int elemIndex = 0; elemIndex < elements.Count; elemIndex++)
            {
                // 線要素
                FemLineElement element = elements[elemIndex];

                // 1Dヘルムホルツ方程式固有値問題の要素行列を加算する
                if (order == Constants.FirstOrder)
                {
                    // １次線要素
                    FemMat_Line_First.AddElementMatOf1dEigenValueProblem(
                        waveLength, // E面の場合のみ使用
                        element,
                        coords,
                        toSorted,
                        Medias,
                        WaveModeDv,
                        ref txx_1d, ref ryy_1d, ref uzz_1d);
                }
                else
                {
                    // ２次線要素
                    FemMat_Line_Second.AddElementMatOf1dEigenValueProblem(
                        waveLength, // E面の場合のみ使用
                        element,
                        coords,
                        toSorted,
                        Medias,
                        WaveModeDv,
                        ref txx_1d, ref ryy_1d, ref uzz_1d);
                }
            }
        }

        private static void getCoordListPeriodic(
            IList<FemNode> Nodes,
            IList<FemElement> Elements,
            IList<uint> elemNoPeriodic,
            out IList<int> nodePeriodic,
            out Dictionary<int, int> toNodePeriodic,
            out IList<double[]> coordsPeriodic
            )
        {
            nodePeriodic = new List<int>();
            toNodePeriodic = new Dictionary<int, int>();
            coordsPeriodic = new List<double[]>();

            int elemCnt = elemNoPeriodic.Count;
            for (int ie = 0; ie < elemCnt; ie++)
            {
                int elemNo = (int)elemNoPeriodic[ie];
                FemElement femElement = Elements[elemNo - 1];
                System.Diagnostics.Debug.Assert(femElement.No == elemNo);
                int[] nodeNumbers = femElement.NodeNumbers;
                int nno = nodeNumbers.Length;
                for (int inoe = 0; inoe < nno; inoe++)
                {
                    int nodeNumber = nodeNumbers[inoe];
                    if (toNodePeriodic.ContainsKey(nodeNumber))
                    {
                        // 追加済み
                        continue;
                    }
                    FemNode femNode = Nodes[nodeNumber - 1];
                    System.Diagnostics.Debug.Assert(femNode.No == nodeNumber);
                    double[] coord = femNode.Coord;

                    nodePeriodic.Add(nodeNumber);
                    int index = nodePeriodic.Count - 1;
                    toNodePeriodic.Add(nodeNumber, index);
                    coordsPeriodic.Add(coord);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        // Φ = φexp(-jβx)と置く方法(SVEA)
        private static void solveSVEAAsQuadraticGeneralizedEigenToStandardWithRealMat(
            int incidentModeIndex,
            double k0,
            double[] KMat0,
            double[] CMat0,
            double[] MMat0,
            bool isPortBc2Reverse,
            int nodeCntPeriodic,
            int freeNodeCntPeriodic_0,
            int freeNodeCnt,
            int nodeCntBPeriodic,
            IList<int> sortedNodesPeriodic,
            Dictionary<int, int> toSortedPeriodic,
            Dictionary<int, int> toNodePeriodic,
            Dictionary<int, int> toNodePeriodicB1,
            IList<int> defectNodePeriodic,
            bool isModeTrace,
            ref KrdLab.clapack.Complex[] PrevModalVec,
            double minBeta,
            double maxBeta,
            double betaNormalizingFactor,
            out KrdLab.clapack.Complex[] betamToSolveList,
            out KrdLab.clapack.Complex[][] resVecList)
        {
            betamToSolveList = null;
            resVecList = null;

            double[] KMat = new double[freeNodeCnt * freeNodeCnt];
            double[] CMat = new double[freeNodeCnt * freeNodeCnt];
            double[] MMat = new double[freeNodeCnt * freeNodeCnt];
            for (int i = 0; i < freeNodeCnt; i++)
            {
                for (int j = 0; j < freeNodeCnt; j++)
                {
                    KMat[i + freeNodeCnt * j] = KMat0[i + freeNodeCntPeriodic_0 * j];
                    CMat[i + freeNodeCnt * j] = CMat0[i + freeNodeCntPeriodic_0 * j];
                    MMat[i + freeNodeCnt * j] = MMat0[i + freeNodeCntPeriodic_0 * j];
                }
            }
            for (int i = 0; i < freeNodeCnt; i++)
            {
                for (int j = 0; j < nodeCntBPeriodic; j++)
                {
                    int jno_B2 = isPortBc2Reverse ? (int)(freeNodeCnt + nodeCntBPeriodic - 1 - j) : (int)(freeNodeCnt + j);
                    KMat[i + freeNodeCnt * j] += KMat0[i + freeNodeCntPeriodic_0 * jno_B2];
                    CMat[i + freeNodeCnt * j] += CMat0[i + freeNodeCntPeriodic_0 * jno_B2];
                    MMat[i + freeNodeCnt * j] += MMat0[i + freeNodeCntPeriodic_0 * jno_B2];
                }
            }
            for (int i = 0; i < nodeCntBPeriodic; i++)
            {
                for (int j = 0; j < freeNodeCnt; j++)
                {
                    int ino_B2 = isPortBc2Reverse ? (int)(freeNodeCnt + nodeCntBPeriodic - 1 - i) : (int)(freeNodeCnt + i);
                    KMat[i + freeNodeCnt * j] += KMat0[ino_B2 + freeNodeCntPeriodic_0 * j];
                    CMat[i + freeNodeCnt * j] += CMat0[ino_B2 + freeNodeCntPeriodic_0 * j];
                    MMat[i + freeNodeCnt * j] += MMat0[ino_B2 + freeNodeCntPeriodic_0 * j];
                }
                for (int j = 0; j < nodeCntBPeriodic; j++)
                {
                    int ino_B2 = isPortBc2Reverse ? (int)(freeNodeCnt + nodeCntBPeriodic - 1 - i) : (int)(freeNodeCnt + i);
                    int jno_B2 = isPortBc2Reverse ? (int)(freeNodeCnt + nodeCntBPeriodic - 1 - j) : (int)(freeNodeCnt + j);
                    KMat[i + freeNodeCnt * j] += KMat0[ino_B2 + freeNodeCntPeriodic_0 * jno_B2];
                    CMat[i + freeNodeCnt * j] += CMat0[ino_B2 + freeNodeCntPeriodic_0 * jno_B2];
                    MMat[i + freeNodeCnt * j] += MMat0[ino_B2 + freeNodeCntPeriodic_0 * jno_B2];
                }
            }
            // 行列要素check
            {
                for (int i = 0; i < freeNodeCnt; i++)
                {
                    for (int j = i; j < freeNodeCnt; j++)
                    {
                        // [K]は対称行列
                        System.Diagnostics.Debug.Assert(Math.Abs(KMat[i + freeNodeCnt * j] - KMat[j + freeNodeCnt * i]) < Constants.PrecisionLowerLimit);
                        // [M]は対称行列
                        System.Diagnostics.Debug.Assert(Math.Abs(MMat[i + freeNodeCnt * j] - MMat[j + freeNodeCnt * i]) < Constants.PrecisionLowerLimit);
                        // [C]は反対称行列
                        System.Diagnostics.Debug.Assert(Math.Abs((-CMat[i + freeNodeCnt * j]) - CMat[j + freeNodeCnt * i]) < Constants.PrecisionLowerLimit);
                    }
                }
            }

            // 非線形固有値問題
            //  { [K] - jβ[C] - β^2[M] }{Φ}= {0}
            //  λ= - jβとおくと
            //  [K] + λ[C] + λ^2[M]{Φ}= {0}
            //
            // Lisys(Lapack)による固有値解析
            // マトリクスサイズは、強制境界及び境界3を除いたサイズ
            int matLen = (int)freeNodeCnt;
            KrdLab.clapack.Complex[] evals = null;
            KrdLab.clapack.Complex[,] evecs = null;

            // [M]の逆行列が存在する緩慢変化包絡線近似の場合のみ有効な方法
            //   Φを直接解く場合は使えない
            System.Diagnostics.Debug.WriteLine("calc [M]-1");
            // [M]の逆行列を求める
            double[] invMMat = MyUtilLib.Matrix.MyMatrixUtil.matrix_Inverse(MMat, matLen);
            System.Diagnostics.Debug.WriteLine("calc [M]-1[K]");
            // [M]-1[K]
            double[] invMKMat = MyUtilLib.Matrix.MyMatrixUtil.product(invMMat, matLen, matLen, KMat, matLen, matLen);
            System.Diagnostics.Debug.WriteLine("calc [M]-1[C]");
            // [M]-1[C]
            double[] invMCMat = MyUtilLib.Matrix.MyMatrixUtil.product(invMMat, matLen, matLen, CMat, matLen, matLen);

            // 標準固有値解析(実行列として解く)
            double[] A = new double[(matLen * 2) * (matLen * 2)];
            System.Diagnostics.Debug.WriteLine("set [A]");
            for (int i = 0; i < matLen; i++)
            {
                for (int j = 0; j < matLen; j++)
                {
                    A[i + j * (matLen * 2)] = 0.0;
                    A[i + (j + matLen) * (matLen * 2)] = (i == j) ? 1.0 : 0.0;
                    //  { [K] - jβ[C] - β^2[M] }{Φ}= {0}
                    // λ = -jβと置いた場合
                    //A[(i + matLen) + j * (matLen * 2)] = -1.0 * invMKMat[i + j * matLen];
                    //A[(i + matLen) + (j + matLen) * (matLen * 2)] = -1.0 * invMCMat[i + j * matLen];
                    // λ = -j(β/k0)と置いた場合
                    //A[(i + matLen) + j * (matLen * 2)] = -1.0 * invMKMat[i + j * matLen] / (k0 * k0);
                    //A[(i + matLen) + (j + matLen) * (matLen * 2)] = -1.0 * invMCMat[i + j * matLen] / (k0);
                    // λ = -j(β/betaNormalizingFactor)と置いた場合
                    A[(i + matLen) + j * (matLen * 2)] = -1.0 * invMKMat[i + j * matLen] / (betaNormalizingFactor * betaNormalizingFactor);
                    A[(i + matLen) + (j + matLen) * (matLen * 2)] = -1.0 * invMCMat[i + j * matLen] / (betaNormalizingFactor);
                }
            }
            double[] ret_r_evals = null;
            double[] ret_i_evals = null;
            double[][] ret_r_evecs = null;
            double[][] ret_i_evecs = null;
            System.Diagnostics.Debug.WriteLine("KrdLab.clapack.FunctionExt.dgeev");
            KrdLab.clapack.FunctionExt.dgeev(A, (matLen * 2), (matLen * 2), ref ret_r_evals, ref ret_i_evals, ref ret_r_evecs, ref ret_i_evecs);

            evals = new KrdLab.clapack.Complex[ret_r_evals.Length];
            // βを格納
            for (int i = 0; i < ret_r_evals.Length; i++)
            {
                KrdLab.clapack.Complex eval = new KrdLab.clapack.Complex(ret_r_evals[i], ret_i_evals[i]);
                //  { [K] - jβ[C] - β^2[M] }{Φ}= {0}
                // λ = -jβと置いた場合(β = jλ)
                //evals[i] = eval * KrdLab.clapack.Complex.ImaginaryOne;
                // λ = -j(β/k0)と置いた場合
                //evals[i] = eval * KrdLab.clapack.Complex.ImaginaryOne * k0;
                // λ = -j(β/betaNormalizingFactor)と置いた場合
                evals[i] = eval * KrdLab.clapack.Complex.ImaginaryOne * betaNormalizingFactor;
            }

            System.Diagnostics.Debug.Assert(ret_r_evals.Length == ret_r_evecs.Length);
            // 2次元配列に格納する
            evecs = new KrdLab.clapack.Complex[ret_r_evecs.Length, (matLen * 2)];
            for (int i = 0; i < ret_r_evecs.Length; i++)
            {
                double[] ret_r_evec = ret_r_evecs[i];
                double[] ret_i_evec = ret_i_evecs[i];
                for (int j = 0; j < ret_r_evec.Length; j++)
                {
                    evecs[i, j] = new KrdLab.clapack.Complex(ret_r_evec[j], ret_i_evec[j]);
                }
            }

            // 固有値をソートする
            System.Diagnostics.Debug.Assert(evecs.GetLength(1) == freeNodeCnt * 2);
            GetSortedModes(
                incidentModeIndex,
                k0,
                nodeCntPeriodic,
                freeNodeCnt,
                nodeCntBPeriodic,
                sortedNodesPeriodic,
                toSortedPeriodic,
                toNodePeriodic,
                defectNodePeriodic,
                isModeTrace,
                ref PrevModalVec,
                minBeta,
                maxBeta,
                evals,
                evecs,
                true, // isDebugShow
                out betamToSolveList,
                out resVecList);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////
        // 周期構造導波路固有値問題：Φを直接解く方法
        private static void solveNonSVEAModeAsQuadraticGeneralizedEigenWithRealMat(
            int incidentModeIndex,
            double periodicDistance,
            double k0,
            double[] KMat0,
            bool isPortBc2Reverse,
            int nodeCntPeriodic,
            int freeNodeCntPeriodic_0,
            int freeNodeCntPeriodic,
            int nodeCntBPeriodic,
            IList<int> sortedNodesPeriodic,
            Dictionary<int, int> toSortedPeriodic,
            Dictionary<int, int> toNodePeriodic,
            Dictionary<int, int> toNodePeriodicB1,
            IList<double[]> coordsPeriodic,
            IList<int> defectNodePeriodic,
            bool isModeTrace,
            ref KrdLab.clapack.Complex[] PrevModalVec,
            double minBeta,
            double maxBeta,
            double betaNormalizingFactor,
            out KrdLab.clapack.Complex[] betamToSolveList,
            out KrdLab.clapack.Complex[][] resVecList)
        {
            betamToSolveList = null;
            resVecList = null;

            // 複素モード、エバネセントモードの固有ベクトル計算をスキップする？ (計算時間短縮するため)
            bool isSkipCalcComplexAndEvanescentModeVec = true;
            System.Diagnostics.Debug.WriteLine("isSkipCalcComplexAndEvanescentModeVec: {0}", isSkipCalcComplexAndEvanescentModeVec);
            // 緩慢変化包絡線近似？ Φを直接解く方法なので常にfalse
            const bool isSVEA = false; // Φを直接解く方法
            // 境界1のみの式に変換
            int inner_node_cnt = freeNodeCntPeriodic - nodeCntBPeriodic;
            double[] P11 = new double[nodeCntBPeriodic * nodeCntBPeriodic];
            double[] P10 = new double[nodeCntBPeriodic * inner_node_cnt];
            double[] P12 = new double[nodeCntBPeriodic * nodeCntBPeriodic];
            double[] P01 = new double[inner_node_cnt * nodeCntBPeriodic];
            double[] P00 = new double[inner_node_cnt * inner_node_cnt];
            double[] P02 = new double[inner_node_cnt * nodeCntBPeriodic];
            double[] P21 = new double[nodeCntBPeriodic * nodeCntBPeriodic];
            double[] P20 = new double[nodeCntBPeriodic * inner_node_cnt];
            double[] P22 = new double[nodeCntBPeriodic * nodeCntBPeriodic];

            for (int i = 0; i < nodeCntBPeriodic; i++)
            {
                int ino_B2 = isPortBc2Reverse ? (int)(freeNodeCntPeriodic + nodeCntBPeriodic - 1 - i) : (int)(freeNodeCntPeriodic + i);
                for (int j = 0; j < nodeCntBPeriodic; j++)
                {
                    int jno_B2 = isPortBc2Reverse ? (int)(freeNodeCntPeriodic + nodeCntBPeriodic - 1 - j) : (int)(freeNodeCntPeriodic + j);
                    // [K11]
                    P11[i + nodeCntBPeriodic * j] = KMat0[i + freeNodeCntPeriodic_0 * j];
                    // [K12]
                    P12[i + nodeCntBPeriodic * j] = KMat0[i + freeNodeCntPeriodic_0 * jno_B2];
                    // [K21]
                    P21[i + nodeCntBPeriodic * j] = KMat0[ino_B2 + freeNodeCntPeriodic_0 * j];
                    // [K22]
                    P22[i + nodeCntBPeriodic * j] = KMat0[ino_B2 + freeNodeCntPeriodic_0 * jno_B2];
                }
                for (int j = 0; j < inner_node_cnt; j++)
                {
                    // [K10]
                    P10[i + nodeCntBPeriodic * j] = KMat0[i + freeNodeCntPeriodic_0 * (j + nodeCntBPeriodic)];
                    // [K20]
                    P20[i + nodeCntBPeriodic * j] = KMat0[ino_B2 + freeNodeCntPeriodic_0 * (j + nodeCntBPeriodic)];
                }
            }
            for (int i = 0; i < inner_node_cnt; i++)
            {
                for (int j = 0; j < nodeCntBPeriodic; j++)
                {
                    int jno_B2 = isPortBc2Reverse ? (int)(freeNodeCntPeriodic + nodeCntBPeriodic - 1 - j) : (int)(freeNodeCntPeriodic + j);
                    // [K01]
                    P01[i + inner_node_cnt * j] = KMat0[(i + nodeCntBPeriodic) + freeNodeCntPeriodic_0 * j];
                    // [K02]
                    P02[i + inner_node_cnt * j] = KMat0[(i + nodeCntBPeriodic) + freeNodeCntPeriodic_0 * jno_B2];
                }
                for (int j = 0; j < inner_node_cnt; j++)
                {
                    // [K00]
                    P00[i + inner_node_cnt * j] = KMat0[(i + nodeCntBPeriodic) + freeNodeCntPeriodic_0 * (j + nodeCntBPeriodic)];
                }
            }

            System.Diagnostics.Debug.WriteLine("setup [K]B [C]B [M]B");
            double[] invP00 = MyMatrixUtil.matrix_Inverse(P00, (int)(freeNodeCntPeriodic - nodeCntBPeriodic));
            double[] P10_invP00 = MyMatrixUtil.product(
                P10, (int)nodeCntBPeriodic, (int)inner_node_cnt,
                invP00, (int)inner_node_cnt, (int)inner_node_cnt);
            double[] P20_invP00 = MyMatrixUtil.product(
                P20, (int)nodeCntBPeriodic, (int)inner_node_cnt,
                invP00, (int)inner_node_cnt, (int)inner_node_cnt);
            // for [C]B
            double[] P10_invP00_P01 = MyMatrixUtil.product(
                P10_invP00, (int)nodeCntBPeriodic, (int)inner_node_cnt,
                P01, (int)inner_node_cnt, (int)nodeCntBPeriodic);
            double[] P20_invP00_P02 = MyMatrixUtil.product(
                P20_invP00, (int)nodeCntBPeriodic, (int)inner_node_cnt,
                P02, (int)inner_node_cnt, (int)nodeCntBPeriodic);
            // for [M]B
            double[] P10_invP00_P02 = MyMatrixUtil.product(
                P10_invP00, (int)nodeCntBPeriodic, (int)inner_node_cnt,
                P02, (int)inner_node_cnt, (int)nodeCntBPeriodic);
            // for [K]B
            double[] P20_invP00_P01 = MyMatrixUtil.product(
                P20_invP00, (int)nodeCntBPeriodic, (int)inner_node_cnt,
                P01, (int)inner_node_cnt, (int)nodeCntBPeriodic);
            // [C]B
            double[] CMatB = new double[nodeCntBPeriodic * nodeCntBPeriodic];
            // [M]B
            double[] MMatB = new double[nodeCntBPeriodic * nodeCntBPeriodic];
            // [K]B
            double[] KMatB = new double[nodeCntBPeriodic * nodeCntBPeriodic];
            for (int i = 0; i < nodeCntBPeriodic; i++)
            {
                for (int j = 0; j < nodeCntBPeriodic; j++)
                {
                    CMatB[i + nodeCntBPeriodic * j] = 
                        - P10_invP00_P01[i + nodeCntBPeriodic * j]
                        + P11[i + nodeCntBPeriodic * j]
                        - P20_invP00_P02[i + nodeCntBPeriodic * j]
                        + P22[i + nodeCntBPeriodic * j];
                    MMatB[i + nodeCntBPeriodic * j] =
                        - P10_invP00_P02[i + nodeCntBPeriodic * j]
                        + P12[i + nodeCntBPeriodic * j];
                    KMatB[i + nodeCntBPeriodic * j] =
                        - P20_invP00_P01[i + nodeCntBPeriodic * j]
                        + P21[i + nodeCntBPeriodic * j];
                }
            }

            // 非線形固有値問題
            //  [K] + λ[C] + λ^2[M]{Φ}= {0}
            //
            // Lisys(Lapack)による固有値解析
            // マトリクスサイズは、強制境界及び境界3を除いたサイズ
            int matLen = (int)nodeCntBPeriodic;
            KrdLab.clapack.Complex[] evals = null;
            KrdLab.clapack.Complex[,] evecs = null;

            // 一般化固有値解析(実行列として解く)
            double[] A = new double[(matLen * 2) * (matLen * 2)];
            double[] B = new double[(matLen * 2) * (matLen * 2)];
            for (int i = 0; i < matLen; i++)
            {
                for (int j = 0; j < matLen; j++)
                {
                    A[i + j * (matLen * 2)] = 0.0;
                    A[i + (j + matLen) * (matLen * 2)] = (i == j) ? 1.0 : 0.0;
                    A[(i + matLen) + j * (matLen * 2)] = -1.0 * KMatB[i + j * matLen];
                    A[(i + matLen) + (j + matLen) * (matLen * 2)] = -1.0 * CMatB[i + j * matLen];
                }
            }
            for (int i = 0; i < matLen; i++)
            {
                for (int j = 0; j < matLen; j++)
                {
                    B[i + j * (matLen * 2)] = (i == j) ? 1.0 : 0.0;
                    B[i + (j + matLen) * (matLen * 2)] = 0.0;
                    B[(i + matLen) + j * (matLen * 2)] = 0.0;
                    B[(i + matLen) + (j + matLen) * (matLen * 2)] = MMatB[i + j * matLen];
                }
            }
            double[] ret_r_evals = null;
            double[] ret_i_evals = null;
            double[][] ret_r_evecs = null;
            double[][] ret_i_evecs = null;
            System.Diagnostics.Debug.WriteLine("KrdLab.clapack.FunctionExt.dggev");
            KrdLab.clapack.FunctionExt.dggev(A, (matLen * 2), (matLen * 2), B, (matLen * 2), (matLen * 2), ref ret_r_evals, ref ret_i_evals, ref ret_r_evecs, ref ret_i_evecs);

            evals = new KrdLab.clapack.Complex[ret_r_evals.Length];
            // βを格納
            for (int i = 0; i < ret_r_evals.Length; i++)
            {
                KrdLab.clapack.Complex eval = new KrdLab.clapack.Complex(ret_r_evals[i], ret_i_evals[i]);
                //System.Diagnostics.Debug.WriteLine("exp(-jβd) = {0} + {1} i", eval.Real, eval.Imaginary);
                if ((Math.Abs(eval.Real) < MyUtilLib.Matrix.Constants.PrecisionLowerLimit && Math.Abs(eval.Imaginary) < MyUtilLib.Matrix.Constants.PrecisionLowerLimit)
                    || double.IsInfinity(eval.Real) || double.IsInfinity(eval.Imaginary)
                    || double.IsNaN(eval.Real) || double.IsNaN(eval.Imaginary)
                    )
                {
                    // 無効な固有値
                    //evals[i] = -1.0 * KrdLab.clapack.Complex.ImaginaryOne * double.MaxValue;
                    evals[i] = KrdLab.clapack.Complex.ImaginaryOne * double.MaxValue;
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine("exp(-jβd) = {0} + {1} i", eval.Real, eval.Imaginary);
                    KrdLab.clapack.Complex betatmp = -1.0 * MyUtilLib.Matrix.MyMatrixUtil.complex_Log(eval) / (KrdLab.clapack.Complex.ImaginaryOne * periodicDistance);
                    evals[i] = new KrdLab.clapack.Complex(betatmp.Real, betatmp.Imaginary);
                }
            }
            System.Diagnostics.Debug.Assert(ret_r_evals.Length == ret_r_evecs.Length);
            // 2次元配列に格納する ({Φ}のみ格納)
            evecs = new KrdLab.clapack.Complex[ret_r_evecs.Length, freeNodeCntPeriodic];

            System.Diagnostics.Debug.WriteLine("calc {Φ}0");
            double[] invP00_P01 = MyMatrixUtil.product(
                invP00, (int)inner_node_cnt, (int)inner_node_cnt,
                P01, (int)inner_node_cnt, (int)nodeCntBPeriodic);
            double[] invP00_P02 = MyMatrixUtil.product(
                invP00, (int)inner_node_cnt, (int)inner_node_cnt,
                P02, (int)inner_node_cnt, (int)nodeCntBPeriodic);
            KrdLab.clapack.Complex[] transMat = new KrdLab.clapack.Complex[inner_node_cnt * nodeCntBPeriodic];
            System.Diagnostics.Debug.Assert(evals.Length == ret_r_evecs.Length);
            System.Diagnostics.Debug.Assert(evals.Length == ret_i_evecs.Length);
            for (int imode = 0; imode < evals.Length; imode++)
            {
                KrdLab.clapack.Complex betam = evals[imode];
                // 複素モード、エバネセントモードの固有モード計算をスキップする？
                if (isSkipCalcComplexAndEvanescentModeVec)
                {
                    if (Math.Abs(betam.Imaginary) >= Constants.PrecisionLowerLimit)
                    {
                        // 複素モード、エバネセントモードの固有モード計算をスキップする
                        continue;
                    }
                }

                KrdLab.clapack.Complex expA = KrdLab.clapack.Complex.Exp(-1.0 * KrdLab.clapack.Complex.ImaginaryOne * betam * periodicDistance);
                double[] ret_r_evec = ret_r_evecs[imode];
                double[] ret_i_evec = ret_i_evecs[imode];
                System.Diagnostics.Debug.Assert(ret_r_evec.Length == nodeCntBPeriodic * 2);
                KrdLab.clapack.Complex[] fVecB = new KrdLab.clapack.Complex[nodeCntBPeriodic];
                ///////////////////////////////
                // {Φ}Bのみ格納
                for (int ino = 0; ino < nodeCntBPeriodic; ino++)
                {
                    KrdLab.clapack.Complex cvalue = new KrdLab.clapack.Complex(ret_r_evec[ino], ret_i_evec[ino]);
                    evecs[imode, ino] = cvalue;
                    fVecB[ino] = cvalue;
                }

                ///////////////////////////////
                // {Φ}0を計算
                //   変換行列を計算
                for (int i = 0; i < inner_node_cnt; i++)
                {
                    for (int j = 0; j < nodeCntBPeriodic; j++)
                    {
                        transMat[i + inner_node_cnt * j] = -1.0 * (invP00_P01[i + inner_node_cnt * j] + expA * invP00_P02[i + inner_node_cnt * j]);
                    }
                }
                //   {Φ}0を計算
                KrdLab.clapack.Complex[] fVecInner = MyMatrixUtil.product(
                    transMat, (int)inner_node_cnt, (int)nodeCntBPeriodic,
                    fVecB, (int)nodeCntBPeriodic);
                //   {Φ}0を格納
                for (int ino = 0; ino < inner_node_cnt; ino++)
                {
                    evecs[imode, ino + nodeCntBPeriodic] = fVecInner[ino];
                }
            }

            ////////////////////////////////////////////////////////////////////

            if (!isSVEA)
            {
                System.Diagnostics.Debug.Assert(freeNodeCntPeriodic == (sortedNodesPeriodic.Count - nodeCntBPeriodic));
                for (int imode = 0; imode < evals.Length; imode++)
                {
                    // 伝搬定数
                    KrdLab.clapack.Complex betatmp = evals[imode];
                    // 界ベクトル
                    KrdLab.clapack.Complex[] fieldVec = MyUtilLib.Matrix.MyMatrixUtil.matrix_GetRowVec(evecs, imode);

                    KrdLab.clapack.Complex beta_d_tmp = betatmp * periodicDistance;
                    if (
                        // [-π, 0]の解を[π, 2π]に移動する
                        ((minBeta * k0 * periodicDistance / (2.0 * pi)) >= (0.5 - MyUtilLib.Matrix.Constants.PrecisionLowerLimit)
                             && (minBeta * k0 * periodicDistance / (2.0 * pi)) < 1.0
                             && Math.Abs(betatmp.Real) >= MyUtilLib.Matrix.Constants.PrecisionLowerLimit
                             && Math.Abs(betatmp.Imaginary) < MyUtilLib.Matrix.Constants.PrecisionLowerLimit
                             && (beta_d_tmp.Real / (2.0 * pi)) >= (-0.5 - MyUtilLib.Matrix.Constants.PrecisionLowerLimit)
                             && (beta_d_tmp.Real / (2.0 * pi)) < 0.0)
                        // [0, π]の解を[2π, 3π]に移動する
                        || ((minBeta * k0 * periodicDistance / (2.0 * pi)) >= (1.0 - MyUtilLib.Matrix.Constants.PrecisionLowerLimit)
                                && (minBeta * k0 * periodicDistance / (2.0 * pi)) < 1.5
                                && Math.Abs(betatmp.Real) >= MyUtilLib.Matrix.Constants.PrecisionLowerLimit
                                && Math.Abs(betatmp.Imaginary) < MyUtilLib.Matrix.Constants.PrecisionLowerLimit
                                && (beta_d_tmp.Real / (2.0 * pi)) >= (0.0 - MyUtilLib.Matrix.Constants.PrecisionLowerLimit)
                                && (beta_d_tmp.Real / (2.0 * pi)) < 0.5)
                        )
                    {
                        // [0, π]の解を2πだけ移動する
                        double delta_phase = 2.0 * pi;
                        beta_d_tmp.Real += delta_phase;
                        betatmp = beta_d_tmp / periodicDistance;
                        //check
                        System.Diagnostics.Debug.WriteLine("shift beta * d / (2π): {0} + {1} i to {2} + {3} i",
                            evals[imode].Real * periodicDistance / (2.0 * pi),
                            evals[imode].Imaginary * periodicDistance / (2.0 * pi),
                            beta_d_tmp.Real / (2.0 * pi),
                            beta_d_tmp.Imaginary / (2.0 * pi));
                        // 再設定
                        evals[imode] = betatmp;
                    }
                }
            }

            // 固有値をソートする
            System.Diagnostics.Debug.Assert(evecs.GetLength(1) == freeNodeCntPeriodic);
            GetSortedModes(
                incidentModeIndex,
                k0,
                nodeCntPeriodic,
                freeNodeCntPeriodic,
                nodeCntBPeriodic,
                sortedNodesPeriodic,
                toSortedPeriodic,
                toNodePeriodic,
                defectNodePeriodic,
                isModeTrace,
                ref PrevModalVec,
                minBeta,
                maxBeta,
                evals,
                evecs,
                true, // isDebugShow
                out betamToSolveList,
                out resVecList);
        }

        public static void GetSortedModes(
            int incidentModeIndex,
            double k0,
            int nodeCntPeriodic,
            int freeNodeCntPeriodic,
            int nodeCntBPeriodic,
            IList<int> sortedNodesPeriodic,
            Dictionary<int, int> toSortedPeriodic,
            Dictionary<int, int> toNodePeriodic,
            IList<int> defectNodePeriodic,
            bool isModeTrace,
            ref KrdLab.clapack.Complex[] PrevModalVec,
            double minBeta,
            double maxBeta,
            KrdLab.clapack.Complex[] evals,
            KrdLab.clapack.Complex[,] evecs,
            bool isDebugShow,
            out KrdLab.clapack.Complex[] betamToSolveList,
            out KrdLab.clapack.Complex[][] resVecList)
        {
            betamToSolveList = null;
            resVecList = null;

            // 固有値のソート
            FemSolverPort.Sort1DEigenMode(k0, evals, evecs);

            // 欠陥モードを取得
            IList<int> defectModeIndexList = new List<int>();
            // 追跡するモードのインデックス退避
            int traceModeIndex = -1;
            // フォトニック結晶導波路解析用
            {
                double hitNorm = 0.0;
                for (int imode = 0; imode < evals.Length; imode++)
                {
                    // 伝搬定数
                    KrdLab.clapack.Complex betam = evals[imode];
                    // 界ベクトル
                    KrdLab.clapack.Complex[] fieldVec = MyUtilLib.Matrix.MyMatrixUtil.matrix_GetRowVec(evecs, imode);

                    // フォトニック結晶導波路の導波モードを判定する
                    //System.Diagnostics.Debug.Assert((free_node_cnt * 2) == fieldVec.Length);
                    bool isHitDefectMode = isDefectMode(
                        k0,
                        freeNodeCntPeriodic,
                        sortedNodesPeriodic,
                        toSortedPeriodic,
                        defectNodePeriodic,
                        minBeta,
                        maxBeta,
                        betam,
                        fieldVec);

                    // 入射モードを追跡する
                    if (isHitDefectMode
                        && isModeTrace && PrevModalVec != null)
                    {
                        // 同じ固有モード？
                        double ret_norm = 0.0;
                        bool isHitSameMode = isSameMode(
                            k0,
                            nodeCntPeriodic,
                            PrevModalVec,
                            freeNodeCntPeriodic,
                            toNodePeriodic,
                            sortedNodesPeriodic,
                            toSortedPeriodic,
                            betam,
                            fieldVec,
                            out ret_norm);
                        if (isHitSameMode)
                        {
                            // より分布の近いモードを採用する
                            if (Math.Abs(ret_norm - 1.0) < Math.Abs(hitNorm - 1.0))
                            {
                                // 追跡するモードのインデックス退避
                                traceModeIndex = imode;
                                hitNorm = ret_norm;
                                if (isDebugShow)
                                {
                                    System.Diagnostics.Debug.WriteLine("PCWaveguideMode(ModeTrace): imode = {0} β/k0 = {1} + {2} i", imode, betam.Real / k0, betam.Imaginary / k0);
                                }
                            }
                        }
                    }

                    if (isHitDefectMode)
                    {
                        if (isDebugShow)
                        {
                            System.Diagnostics.Debug.WriteLine("PCWaveguideMode: imode = {0} β/k0 = {1} + {2} i", imode, betam.Real / k0, betam.Imaginary / k0);
                        }
                        if (imode != traceModeIndex) // 追跡するモードは除外、あとで追加
                        {
                            defectModeIndexList.Add(imode);
                        }
                    }
                }
                if (isModeTrace && traceModeIndex != -1)
                {
                    if (isDebugShow)
                    {
                        System.Diagnostics.Debug.WriteLine("traceModeIndex:{0}", traceModeIndex);
                    }
                    // 追跡している入射モードがあれば最後に追加する
                    //defectModeIndexList.Add(traceModeIndex);
                    // 追跡している入射モードがあれば最後から入射モードインデックス分だけシフトした位置に挿入する
                    if (defectModeIndexList.Count >= (0 + incidentModeIndex))
                    {
                        defectModeIndexList.Insert(defectModeIndexList.Count - incidentModeIndex, traceModeIndex);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("other modes dissappeared ! defectModeIndexList cleared.");
                        traceModeIndex = -1;
                        defectModeIndexList.Clear();
                    }
                }
            }
            IList<int> selectedModeList = new List<int>();
            // フォトニック結晶導波路解析用
            {
                // フォトニック結晶導波路
                if (defectModeIndexList.Count > 0)
                {
                    // フォトニック結晶欠陥部閉じ込めモード
                    for (int iDefectModeIndex = defectModeIndexList.Count - 1; iDefectModeIndex >= 0; iDefectModeIndex--)
                    {
                        int imode = defectModeIndexList[iDefectModeIndex];
                        // 伝搬定数
                        KrdLab.clapack.Complex betam = evals[imode];
                        // 界ベクトル
                        KrdLab.clapack.Complex[] fieldVec = MyUtilLib.Matrix.MyMatrixUtil.matrix_GetRowVec(evecs, imode);
                        if (Math.Abs(betam.Real) < 1.0e-12 && Math.Abs(betam.Imaginary) >= 1.0e-12)
                        {
                            // 減衰モード
                            // 正しく計算できていないように思われる
                            if (selectedModeList.Count > incidentModeIndex)
                            {
                                // 基本モード以外の減衰モードは除外する
                                //System.Diagnostics.Debug.WriteLine("skip evanesent mode:β/k0  ( " + imode + " ) = " + betam.Real / k0 + " + " + betam.Imaginary / k0 + " i ");
                                continue;
                            }
                        }
                        selectedModeList.Add(imode);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("!!!!!!!!! Not converged photonic crystal waveguide mode");
                }
            }
            if (selectedModeList.Count > 0)
            {
                betamToSolveList = new KrdLab.clapack.Complex[selectedModeList.Count];
                resVecList = new KrdLab.clapack.Complex[selectedModeList.Count][];
                for (int imode = 0; imode < betamToSolveList.Length; imode++)
                {
                    resVecList[imode] = new KrdLab.clapack.Complex[nodeCntPeriodic]; // 全節点
                }
                for (int i = selectedModeList.Count - 1, selectedModeIndex = 0; i >= 0; i--, selectedModeIndex++)
                {
                    int imode = selectedModeList[i];
                    // 伝搬定数、固有ベクトルの格納
                    betamToSolveList[selectedModeIndex] = evals[imode];

                    KrdLab.clapack.Complex[] evec = MyUtilLib.Matrix.MyMatrixUtil.matrix_GetRowVec(evecs, imode);
                    // 非線形固有値方程式の解は{Φ} {λΦ}の順になっている
                    //System.Diagnostics.Debug.Assert((evec.Length == free_node_cnt * 2));
                    // 前半の{Φ}のみ取得する
                    for (int ino = 0; ino < freeNodeCntPeriodic; ino++)
                    {
                        int nodeNumber = sortedNodesPeriodic[ino];
                        int ino_InLoop = toNodePeriodic[nodeNumber];
                        resVecList[selectedModeIndex][ino_InLoop] = evec[ino];
                    }
                    if (isDebugShow)
                    {
                        System.Diagnostics.Debug.WriteLine("mode({0}): index:{1} β/k0 = {2} + {3} i", selectedModeIndex, imode, (betamToSolveList[selectedModeIndex].Real / k0), (betamToSolveList[selectedModeIndex].Imaginary / k0));
                    }
                }
            }

            if (isModeTrace)
            {
                if (PrevModalVec != null && (traceModeIndex == -1))
                {
                    // モード追跡に失敗した場合
                    betamToSolveList = null;
                    resVecList = null;
                    System.Diagnostics.Debug.WriteLine("fail to trace mode at k0 = {0}", k0);
                    System.Diagnostics.Debug.WriteLine("fail to trace mode at k0 = {0}", k0);
                    return;
                }

                // 前回の固有ベクトルを更新
                if ((PrevModalVec == null || (PrevModalVec != null && traceModeIndex != -1)) // 初回格納、またはモード追跡に成功した場合だけ更新
                    && (betamToSolveList != null && betamToSolveList.Length >= (1 + incidentModeIndex))
                    )
                {
                    // 返却用リストでは伝搬定数の昇順に並んでいる→入射モードは最後
                    KrdLab.clapack.Complex betam = betamToSolveList[betamToSolveList.Length - 1 - incidentModeIndex];
                    KrdLab.clapack.Complex[] resVec = resVecList[betamToSolveList.Length - 1 - incidentModeIndex];
                    if (betam.Real != 0.0 && Math.Abs(betam.Imaginary) < MyUtilLib.Matrix.Constants.PrecisionLowerLimit)
                    {
                        //PrevModalVec = resVec;
                        PrevModalVec = new KrdLab.clapack.Complex[nodeCntPeriodic];
                        resVec.CopyTo(PrevModalVec, 0);
                    }
                    else
                    {
                        // クリアしない(特定周波数で固有値が求まらないときがある。その場合でも同じモードを追跡できるように)
                        //PrevModalVec = null;
                    }
                }
                else
                {
                    // クリアしない(特定周波数で固有値が求まらないときがある。その場合でも同じモードを追跡できるように)
                    //PrevModalVec = null;
                }
            }
        }

        /// <summary>
        /// 欠陥モード？
        /// </summary>
        /// <param name="k0"></param>
        /// <param name="freeNodeCntPeriodic"></param>
        /// <param name="sortedNodesPeriodic"></param>
        /// <param name="toSortedPeriodic"></param>
        /// <param name="defectNodePeriodic"></param>
        /// <param name="minBeta"></param>
        /// <param name="maxBeta"></param>
        /// <param name="betam"></param>
        /// <param name="fieldVec"></param>
        /// <returns></returns>
        private static bool isDefectMode(
            double k0,
            int freeNodeCntPeriodic,
            IList<int> sortedNodesPeriodic,
            Dictionary<int, int> toSortedPeriodic,
            IList<int> defectNodePeriodic,
            double minBeta,
            double maxBeta,
            KrdLab.clapack.Complex betam,
            KrdLab.clapack.Complex[] fieldVec)
        {
            bool isHit = false;
            if (Math.Abs(betam.Real) >= MyUtilLib.Matrix.Constants.PrecisionLowerLimit && Math.Abs(betam.Imaginary) >= MyUtilLib.Matrix.Constants.PrecisionLowerLimit)
            {
                // 複素モードは除外する
                return isHit;
            }
            else if (Math.Abs(betam.Real) >= MyUtilLib.Matrix.Constants.PrecisionLowerLimit && Math.Abs(betam.Imaginary) < MyUtilLib.Matrix.Constants.PrecisionLowerLimit)
            {
                // 伝搬モード
                // 後進波は除外する
                if (betam.Real < 0)
                {
                    return isHit;
                }
            }
            else if (Math.Abs(betam.Real) < MyUtilLib.Matrix.Constants.PrecisionLowerLimit && Math.Abs(betam.Imaginary) >= MyUtilLib.Matrix.Constants.PrecisionLowerLimit)
            {
                
                // 減衰モード
                //  利得のある波は除外する
                if (betam.Imaginary > 0)
                {
                    return isHit;
                }
                 
                /*
                //除外する
                return isHit;
                 */
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
                return isHit;
            }

            
            // フォトニック結晶導波路の導波モードを判定する
            //
            // 領域内の節点の界の絶対値の２乗の和を計算
            //   要素分割が均一であることが前提。面積を考慮しない。
            double totalPower = 0.0;
            //for (int ino = 0; ino < fieldVec.Length; ino++)
            for (int ino = 0; ino < freeNodeCntPeriodic; ino++)
            {
                double fieldAbs = fieldVec[ino].Magnitude;
                double power = fieldAbs * fieldAbs;
                totalPower += power;
            }
            // チャンネル上の節点の界の絶対値の２乗の和を計算
            //   要素分割が均一であることが前提。面積を考慮しない。
            int channelNodeCnt = 0;
            double channelTotalPower = 0.0;

            foreach (int defectNodeNumber in defectNodePeriodic)
            {
                if (!toSortedPeriodic.ContainsKey(defectNodeNumber)) continue;
                int noSorted = toSortedPeriodic[defectNodeNumber];
                //if (noSorted >= fieldVec.Length) continue;
                if (noSorted >= freeNodeCntPeriodic) continue;
                KrdLab.clapack.Complex cvalue = fieldVec[noSorted];
                double valAbs = cvalue.Magnitude;
                double channelPower = valAbs * valAbs;
                channelTotalPower += channelPower;
                channelNodeCnt++;
            }
            // 密度で比較する
            //totalPower /= fieldVec.Length;
            //channelTotalPower /= channelNodeCnt;
            ////const double powerRatioLimit = 3.0;
            //const double powerRatioLimit = 2.0;
            ////System.Diagnostics.Debug.WriteLine("channelTotalPower = {0}", (channelTotalPower / totalPower));
            // 総和で比較する
            const double powerRatioLimit = 0.5;
            if (Math.Abs(totalPower) >= Constants.PrecisionLowerLimit && (channelTotalPower / totalPower) >= powerRatioLimit)
            {
                //if (((Math.Abs(betam.Real) / k0) > maxBeta))
                if (Math.Abs(betam.Real / k0) > maxBeta || Math.Abs(betam.Real / k0) < minBeta)
                {
                    //System.Diagnostics.Debug.WriteLine("PCWaveguideMode: beta is invalid skip: β/k0 = {0} at k0 = {1}", betam.Real / k0, k0);
                    System.Diagnostics.Debug.WriteLine("PCWaveguideMode: beta is invalid skip: β/k0 = {0} at k0 = {1}", betam.Real / k0, k0);
                }
                else
                {
                    isHit = true;
                }
            }
             
            /*
            ////////////////////////暫定
            if (Math.Abs(betam.Real / k0) > maxBeta || Math.Abs(betam.Real / k0) < minBeta)
            {
                //System.Diagnostics.Debug.WriteLine("PCWaveguideMode: beta is invalid skip: β/k0 = {0} at k0 = {1}", betam.Real / k0, k0);
                System.Diagnostics.Debug.WriteLine("PCWaveguideMode: beta is invalid skip: β/k0 = {0} at k0 = {1}", betam.Real / k0, k0);
            }
            else
            {
                isHit = true;
            }
             */
            return isHit;
        }

        /// <summary>
        /// 同じ固有モード？
        /// </summary>
        /// <param name="k0"></param>
        /// <param name="nodeCntPeriodic"></param>
        /// <param name="PrevModalVec"></param>
        /// <param name="freeNodeCntPeriodic"></param>
        /// <param name="sortedNodes"></param>
        /// <param name="toSorted"></param>
        /// <param name="betam"></param>
        /// <param name="fieldVec"></param>
        /// <returns></returns>
        private static bool isSameMode(
            double k0,
            int nodeCntPeriodic,
            KrdLab.clapack.Complex[] PrevModalVec,
            int freeNodeCntPeriodic,
            Dictionary<int, int> toNodePeriodic,
            IList<int> sortedNodesPeriodic,
            Dictionary<int, int> toSorted,
            KrdLab.clapack.Complex betam,
            KrdLab.clapack.Complex[] fieldVec,
            out double ret_norm)
        {
            bool isHit = false;
            ret_norm = 0.0;
            if (betam.Real > 0.0 && Math.Abs(betam.Imaginary) < MyUtilLib.Matrix.Constants.PrecisionLowerLimit)
            {
                KrdLab.clapack.Complex[] workModalVec1 = new KrdLab.clapack.Complex[nodeCntPeriodic]; // 前回
                KrdLab.clapack.Complex[] workModalVec2 = new KrdLab.clapack.Complex[nodeCntPeriodic]; // 今回
                // 前半の{Φ}のみ取得する
                for (int ino = 0; ino < freeNodeCntPeriodic; ino++)
                {
                    // 今回の固有ベクトル
                    //System.Diagnostics.Debug.WriteLine("    ( " + imode + ", " + ino + " ) = " + evec[ino].Real + " + " + evec[ino].Imaginary + " i ");
                    int nodeNumber = sortedNodesPeriodic[ino];
                    int ino_InLoop = toNodePeriodic[nodeNumber];
                    workModalVec2[ino_InLoop] = fieldVec[ino];
                    // 対応する前回の固有ベクトル
                    workModalVec1[ino_InLoop] = PrevModalVec[ino_InLoop];
                }
                KrdLab.clapack.Complex norm1 = MyUtilLib.Matrix.MyMatrixUtil.vector_Dot(MyUtilLib.Matrix.MyMatrixUtil.vector_Conjugate(workModalVec1), workModalVec1);
                KrdLab.clapack.Complex norm2 = MyUtilLib.Matrix.MyMatrixUtil.vector_Dot(MyUtilLib.Matrix.MyMatrixUtil.vector_Conjugate(workModalVec2), workModalVec2);
                for (int i = 0; i < nodeCntPeriodic; i++)
                {
                    workModalVec1[i] /= Math.Sqrt(norm1.Magnitude);
                    workModalVec2[i] /= Math.Sqrt(norm2.Magnitude);
                }
                KrdLab.clapack.Complex norm12 = MyUtilLib.Matrix.MyMatrixUtil.vector_Dot(MyUtilLib.Matrix.MyMatrixUtil.vector_Conjugate(workModalVec1), workModalVec2);
                double thLikeMin = 0.9;
                double thLikeMax = 1.1;
                if (norm12.Magnitude >= thLikeMin && norm12.Magnitude < thLikeMax)
                {
                    isHit = true;
                    ret_norm = norm12.Magnitude;
                    System.Diagnostics.Debug.WriteLine("norm (prev * current): {0} + {1}i (Abs: {2})", norm12.Real, norm12.Imaginary, norm12.Magnitude);
                }
            }
            return isHit;
        }

        /// <summary>
        /// 周期構造導波路の伝搬定数に変換する(βdが[-π, π]に収まるように変換)
        /// </summary>
        /// <param name="beta"></param>
        /// <param name="periodicDistance"></param>
        /// <returns></returns>
        public static KrdLab.clapack.Complex ToBetaPeriodic(KrdLab.clapack.Complex betam, double periodicDistance)
        {
            // βの再変換
            KrdLab.clapack.Complex expA = KrdLab.clapack.Complex.Exp(-1.0 * KrdLab.clapack.Complex.ImaginaryOne * betam * periodicDistance);
            KrdLab.clapack.Complex betam_periodic = -1.0 * MyUtilLib.Matrix.MyMatrixUtil.complex_Log(expA) / (KrdLab.clapack.Complex.ImaginaryOne * periodicDistance);
            return betam_periodic;
        }

        /// <summary>
        /// 回転移動する
        /// </summary>
        /// <param name="srcPt"></param>
        /// <param name="rotAngle"></param>
        /// <param name="rotOrigin"></param>
        /// <returns></returns>
        public static double[] GetRotCoord(double[] srcPt, double rotAngle, double[] rotOrigin = null)
        {
            double[] destPt = new double[2];
            double x0 = 0;
            double y0 = 0;
            if (rotOrigin != null)
            {
                x0 = rotOrigin[0];
                y0 = rotOrigin[1];
            }
            double cosA = Math.Cos(rotAngle);
            double sinA = Math.Sin(rotAngle);
            destPt[0] = cosA * (srcPt[0] - x0) + sinA * (srcPt[1] - y0);
            destPt[1] = -sinA * (srcPt[0] - x0) + cosA * (srcPt[1] - y0);
            return destPt;
        }

        /// <summary>
        /// 界のx方向微分値を取得する(1次三角形要素)
        /// </summary>
        /// <param name="world"></param>
        /// <param name="fieldValId"></param>
        private static void getDFDXValues_Tri_FirstOrder(
            FemSolver.WaveModeDV WaveModeDv,
            double k0,
            double rotAngle,
            double[] rotOrigin,
            IList<FemNode> Nodes,
            IList<FemElement> Elements,
            MediaInfo[] Medias,
            Dictionary<int, bool> ForceNodeNumberH,
            IList<uint> elemNoPeriodic,
            IList<IList<int>> nodePeriodicB,
            Dictionary<int, int> toNodePeriodic,
            KrdLab.clapack.Complex[] fVec,
            out KrdLab.clapack.Complex[] dFdXVec,
            out KrdLab.clapack.Complex[] dFdYVec)
        {
            dFdXVec = new KrdLab.clapack.Complex[fVec.Length];
            dFdYVec = new KrdLab.clapack.Complex[fVec.Length];

            Dictionary<int, int> nodeElemCntH = new Dictionary<int, int>();
            Dictionary<int, double> nodeAreaSum = new Dictionary<int, double>();

            int elemCnt = elemNoPeriodic.Count;
            for (int ie = 0; ie < elemCnt; ie++)
            {
                int elemNo = (int)elemNoPeriodic[ie];
                FemElement element = Elements[elemNo - 1];
                System.Diagnostics.Debug.Assert(element.No == elemNo);

                // 要素内節点数
                const int nno = Constants.TriNodeCnt_FirstOrder; //3;  // 1次三角形要素
                // 座標次元数
                const int ndim = Constants.CoordDim2D; //2;

                int[] nodeNumbers = element.NodeNumbers;
                int[] no_c = new int[nno];
                MediaInfo media = Medias[element.MediaIndex];
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
                if (Math.Abs(rotAngle) >= Constants.PrecisionLowerLimit)
                {
                    // 座標を回転移動する
                    for (uint inoes = 0; inoes < nno; inoes++)
                    {
                        double[] srcPt = new double[] { pp[inoes][0], pp[inoes][1] };
                        double[] destPt = GetRotCoord(srcPt, rotAngle, rotOrigin);
                        for (int i = 0; i < ndim; i++)
                        {
                            pp[inoes][i] = destPt[i];
                        }
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

                // 界の微分値を計算
                // 界の微分値は一次三角形要素の場合、要素内で一定値
                KrdLab.clapack.Complex dFdX = 0.0;
                KrdLab.clapack.Complex dFdY = 0.0;
                for (int inoes = 0; inoes < nno; inoes++)
                {
                    int iNodeNumber = no_c[inoes];
                    if (!toNodePeriodic.ContainsKey(iNodeNumber))
                    {
                        System.Diagnostics.Debug.Assert(false);
                        continue;
                    }
                    int inoGlobal = toNodePeriodic[iNodeNumber];
                    KrdLab.clapack.Complex fVal = fVec[inoGlobal];
                    dFdX += dldx[inoes, 0] * fVal;
                    dFdY += dldx[inoes, 1] * fVal;
                }
                // 格納
                for (int inoes = 0; inoes < nno; inoes++)
                {
                    int iNodeNumber = no_c[inoes];
                    if (!toNodePeriodic.ContainsKey(iNodeNumber))
                    {
                        System.Diagnostics.Debug.Assert(false);
                        continue;
                    }
                    int inoGlobal = toNodePeriodic[iNodeNumber];

                    //dFdXVec[inoGlobal] += dFdX;
                    //dFdYVec[inoGlobal] += dFdY;
                    // Note:
                    //  TEzモードのとき -dHz/dx = jωDy
                    dFdXVec[inoGlobal] += dFdX * area;
                    dFdYVec[inoGlobal] += dFdY * area;

                    if (nodeElemCntH.ContainsKey(inoGlobal))
                    {
                        nodeElemCntH[inoGlobal]++;
                        // 面積を格納
                        nodeAreaSum[inoGlobal] += area;
                    }
                    else
                    {
                        nodeElemCntH.Add(inoGlobal, 1);
                        nodeAreaSum.Add(inoGlobal, area);
                    }
                }
            }
            for (int i = 0; i < dFdXVec.Length; i++)
            {
                //int cnt = nodeElemCntH[i];
                //dFdXVec[i] /= cnt;
                //dFdYVec[i] /= cnt;
                double areaSum = nodeAreaSum[i];
                dFdXVec[i] = (dFdXVec[i] / areaSum);
                dFdYVec[i] = (dFdYVec[i] / areaSum);
            }
        }

        /// <summary>
        /// 界のx方向微分値を取得する (2次三角形要素)
        /// </summary>
        /// <param name="world"></param>
        /// <param name="fieldValId"></param>
        private static void getDFDXValues_Tri_SecondOrder(
            FemSolver.WaveModeDV WaveModeDv,
            double k0,
            double rotAngle,
            double[] rotOrigin,
            IList<FemNode> Nodes,
            IList<FemElement> Elements,
            MediaInfo[] Medias,
            Dictionary<int, bool> ForceNodeNumberH,
            IList<uint> elemNoPeriodic,
            IList<IList<int>> nodePeriodicB,
            Dictionary<int, int> toNodePeriodic,
            KrdLab.clapack.Complex[] fVec,
            out KrdLab.clapack.Complex[] dFdXVec,
            out KrdLab.clapack.Complex[] dFdYVec)
        {
            dFdXVec = new KrdLab.clapack.Complex[fVec.Length];
            dFdYVec = new KrdLab.clapack.Complex[fVec.Length];

            Dictionary<int, int> nodeElemCntH = new Dictionary<int, int>();
            Dictionary<int, double> nodeAreaSum = new Dictionary<int, double>();

            int elemCnt = elemNoPeriodic.Count;
            for (int ie = 0; ie < elemCnt; ie++)
            {
                int elemNo = (int)elemNoPeriodic[ie];
                FemElement element = Elements[elemNo - 1];
                System.Diagnostics.Debug.Assert(element.No == elemNo);

                // 要素内節点数
                const int nno = Constants.TriNodeCnt_SecondOrder; //6;  // 2次三角形要素
                // 座標次元数
                const int ndim = Constants.CoordDim2D; //2;

                int[] nodeNumbers = element.NodeNumbers;
                int[] no_c = new int[nno];
                MediaInfo media = Medias[element.MediaIndex];
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
                if (Math.Abs(rotAngle) >= Constants.PrecisionLowerLimit)
                {
                    // 座標を回転移動する
                    for (uint inoes = 0; inoes < nno; inoes++)
                    {
                        double[] srcPt = new double[] { pp[inoes][0], pp[inoes][1] };
                        double[] destPt = GetRotCoord(srcPt, rotAngle, rotOrigin);
                        for (int i = 0; i < ndim; i++)
                        {
                            pp[inoes][i] = destPt[i];
                        }
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
                double[, ,] dndxC = new double[nno, ndim, Constants.TriVertexCnt + 1]
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

                // 界の微分値を計算
                KrdLab.clapack.Complex[] dFdXs = new KrdLab.clapack.Complex[nno];
                KrdLab.clapack.Complex[] dFdYs = new KrdLab.clapack.Complex[nno];
                // 節点の面積座標
                double[,] L_node = new double[Constants.TriNodeCnt_SecondOrder, 3]
                {
                    {1.0, 0.0, 0.0},
                    {0.0, 1.0, 0.0},
                    {0.0, 0.0, 1.0},
                    {0.5, 0.5, 0.0},
                    {0.0, 0.5, 0.5},
                    {0.5, 0.0, 0.5}
                };
                for (int inoes = 0; inoes < nno; inoes++)
                {
                    for (int jnoes = 0; jnoes < nno; jnoes++)
                    {
                        int jNodeNumber = no_c[jnoes];
                        if (!toNodePeriodic.ContainsKey(jNodeNumber))
                        {
                            System.Diagnostics.Debug.Assert(false);
                            continue;
                        }
                        int jnoGlobal = toNodePeriodic[jNodeNumber];
                        KrdLab.clapack.Complex fVal = fVec[jnoGlobal];
                        double[] dNdx_node = new double[ndim];
                        for (int n = 0; n < ndim; n++)
                        {
                            dNdx_node[n] = dndxC[jnoes, n, 0] * L_node[inoes, 0]
                                           + dndxC[jnoes, n, 1] * L_node[inoes, 1]
                                           + dndxC[jnoes, n, 2] * L_node[inoes, 2]
                                           + dndxC[jnoes, n, 3];
                        }
                        dFdXs[inoes] += dNdx_node[0] * fVal;
                        dFdYs[inoes] += dNdx_node[1] * fVal;
                    }
                }
                // 格納
                for (int inoes = 0; inoes < nno; inoes++)
                {
                    int iNodeNumber = no_c[inoes];
                    if (!toNodePeriodic.ContainsKey(iNodeNumber))
                    {
                        System.Diagnostics.Debug.Assert(false);
                        continue;
                    }
                    int inoGlobal = toNodePeriodic[iNodeNumber];

                    //dFdXVec[inoGlobal] += dFdXs[inoes];
                    //dFdYVec[inoGlobal] += dFdYs[inoes];
                    // Note:
                    //  TEzモードのとき -dHz/dx = jωDy
                    dFdXVec[inoGlobal] += dFdXs[inoes] * area;
                    dFdYVec[inoGlobal] += dFdYs[inoes] * area;

                    if (nodeElemCntH.ContainsKey(inoGlobal))
                    {
                        nodeElemCntH[inoGlobal]++;
                        // 面積を格納
                        nodeAreaSum[inoGlobal] += area;
                    }
                    else
                    {
                        nodeElemCntH.Add(inoGlobal, 1);
                        nodeAreaSum.Add(inoGlobal, area);
                    }
                }
            }
            for (int i = 0; i < dFdXVec.Length; i++)
            {
                //int cnt = nodeElemCntH[i];
                //dFdXVec[i] /= cnt;
                //dFdYVec[i] /= cnt;
                double areaSum = nodeAreaSum[i];
                dFdXVec[i] = (dFdXVec[i] / areaSum);
                dFdYVec[i] = (dFdYVec[i] / areaSum);
            }
        }

        /// <summary>
        /// ポート固有値解析
        /// </summary>
        public static void SolvePortWaveguideEigen(
            FemSolver.WaveModeDV WaveModeDv,
            double waveLength,
            double latticeA,
            int maxModeSpecified,
            IList<FemNode> Nodes,
            Dictionary<string, IList<int>> EdgeToElementNoH,
            IList<FemElement> Elements,
            MediaInfo[] Medias,
            Dictionary<int, bool> ForceNodeNumberH,
            IList<int> portNodes,
            IList<uint> portElemNoPeriodic,
            IList<IList<int>> portNodePeriodicB,
            IList<int> defectNodePeriodic,
            out int[] nodesBoundary,
            out MyDoubleMatrix ryy_1d,
            out Complex[] eigenValues,
            out Complex[,] eigenVecsB1,
            out Complex[,] eigen_dFdXsB1,
            out IList<int> nodePeriodic,
            out Dictionary<int, int> toNodePeriodic,
            out IList<double[]> coordsPeriodic,
            out KrdLab.clapack.Complex[][] eigenVecsPeriodic
            )
        {
            // ポート境界上
            nodesBoundary = null;
            ryy_1d = null;
            eigenValues = null;
            eigenVecsB1 = null;
            eigen_dFdXsB1 = null;
            // ポート周期構造領域
            nodePeriodic = null;
            toNodePeriodic = null;
            coordsPeriodic = null;
            eigenVecsPeriodic = null;

            // 波数
            double k0 = 2.0 * pi / waveLength;
            // 角周波数
            double omega = k0 * c0;

            getPortFemMat1D(
                WaveModeDv,
                waveLength,
                latticeA,
                Nodes,
                EdgeToElementNoH,
                Elements,
                Medias,
                ForceNodeNumberH,
                portNodes,
                out nodesBoundary,
                out ryy_1d
                );
            Dictionary<int, int> toNodesB = new Dictionary<int, int>();
            for (int ino = 0; ino < nodesBoundary.Length; ino++)
            {
                int nodeNumber = nodesBoundary[ino];
                //System.Diagnostics.Debug.WriteLine("nodesBoundary[{0}] = {1}", ino, nodesBoundary[ino]);
                toNodesB.Add(nodeNumber, ino);
            }

            //////////////////////////////////////////////////////////////////////////////
            // 周期構造導波路
            /////////////////////////////////////////////////////////////////////////////
            int incidentModeIndex = 0;
            Constants.FemElementShapeDV elemShapeDv;
            FemElement workElement = Elements[0];
            int order;
            int vertexCnt;
            FemMeshLogic.GetElementShapeDvAndOrderByElemNodeCnt(workElement.NodeNumbers.Length, out elemShapeDv, out order, out vertexCnt);

            // 領域の節点リスト、座標リストを取得する
            //IList<int> nodePeriodic = null;
            //Dictionary<int, int> toNodePeriodic = null;
            //IList<double[]> coordsPeriodic = null;
            getCoordListPeriodic(
                Nodes,
                Elements,
                portElemNoPeriodic,
                out nodePeriodic,
                out toNodePeriodic,
                out coordsPeriodic
                );
            // 全節点数
            int nodeCntPeriodic = nodePeriodic.Count;

            IList<IList<int>> nodePeriodicB = portNodePeriodicB;
            IList<int> nodePeriodicB1 = nodePeriodicB[0];
            IList<int> nodePeriodicB2 = nodePeriodicB[1];
            Dictionary<int, int> toNodePeriodicB1 = new Dictionary<int, int>();
            Dictionary<int, int> toNodePeriodicB2 = new Dictionary<int, int>();
            for (int ino = 0; ino < nodePeriodicB1.Count; ino++)
            {
                int nodeNumber = nodePeriodicB1[ino];
                toNodePeriodicB1.Add(nodeNumber, ino);
            }
            for (int ino = 0; ino < nodePeriodicB2.Count; ino++)
            {
                int nodeNumber = nodePeriodicB2[ino];
                toNodePeriodicB2.Add(nodeNumber, ino);
            }

            // Y方向に周期構造?
            bool isYDirectionPeriodic = false;
            {
                int nodeNumber_first = nodePeriodicB1[0];
                int nodeNumber_last = nodePeriodicB1[nodePeriodicB1.Count - 1];
                int noB_first = toNodePeriodic[nodeNumber_first];
                int noB_last = toNodePeriodic[nodeNumber_last];
                double[] coord_first = coordsPeriodic[noB_first];
                double[] coord_last = coordsPeriodic[noB_last];
                if (Math.Abs(coord_first[1] - coord_last[1]) < 1.0e-12)
                {
                    isYDirectionPeriodic = true;
                }
            }
            System.Diagnostics.Debug.WriteLine("isYDirectionPeriodic: {0}", isYDirectionPeriodic);

            // 節点の並び替え
            IList<int> sortedNodesPeriodic = new List<int>();
            Dictionary<int, int> toSortedPeriodic = new Dictionary<int, int>();
            // ポート境界1
            for (int ino = 0; ino < nodePeriodicB1.Count; ino++)
            {
                // 境界1の節点番号
                int nodeNumberB1 = nodePeriodicB1[ino];
                if (ForceNodeNumberH.ContainsKey(nodeNumberB1))
                {
                    // 強制境界は除外
                    continue;
                }
                sortedNodesPeriodic.Add(nodeNumberB1);
                int index = sortedNodesPeriodic.Count - 1;
                toSortedPeriodic.Add(nodeNumberB1, index);
            }
            int nodeCntBPeriodic = sortedNodesPeriodic.Count; // 境界1
            // 内部領域
            for (int ino = 0; ino < nodeCntPeriodic; ino++)
            {
                int nodeNumber = nodePeriodic[ino];
                if (ForceNodeNumberH.ContainsKey(nodeNumber))
                {
                    // 強制境界は除外
                    continue;
                }
                if (toNodePeriodicB1.ContainsKey(nodeNumber))
                {
                    // 境界1は除外
                    continue;
                }
                if (toNodePeriodicB2.ContainsKey(nodeNumber))
                {
                    // 境界2は除外
                    continue;
                }
                sortedNodesPeriodic.Add(nodeNumber);
                int index = sortedNodesPeriodic.Count - 1;
                toSortedPeriodic.Add(nodeNumber, index);
            }
            int freeNodeCntPeriodic = sortedNodesPeriodic.Count; // 境界1 + 内部領域
            // ポート境界2
            for (int ino = 0; ino < nodePeriodicB2.Count; ino++)
            {
                // 境界2の節点番号
                int nodeNumberB2 = nodePeriodicB2[ino];
                if (ForceNodeNumberH.ContainsKey(nodeNumberB2))
                {
                    // 強制境界は除外
                    continue;
                }
                sortedNodesPeriodic.Add(nodeNumberB2);
                int index = sortedNodesPeriodic.Count - 1;
                toSortedPeriodic.Add(nodeNumberB2, index);
            }
            int freeNodeCntPeriodic_0 = sortedNodesPeriodic.Count; // 境界1 + 内部領域 + 境界2

            // 周期構造導波路固有値問題用FEM行列の作成
            //bool isSVEA = false; // Φを直接解く方法
            bool isSVEA = true; // Φ= φexp(-jβx)と置く方法(SVEA)
            double[] KMat0 = null;
            double[] CMat0 = null;
            double[] MMat0 = null;
            if (isSVEA)
            {
                // Φ = φexp(-jβx)と置く方法
                KMat0 = new double[freeNodeCntPeriodic_0 * freeNodeCntPeriodic_0];
                CMat0 = new double[freeNodeCntPeriodic_0 * freeNodeCntPeriodic_0];
                MMat0 = new double[freeNodeCntPeriodic_0 * freeNodeCntPeriodic_0];
            }
            else
            {
                // Φを直接解く方法
                KMat0 = new double[freeNodeCntPeriodic_0 * freeNodeCntPeriodic_0];
                CMat0 = null;
                MMat0 = null;
            }
            IList<uint> elemNoPeriodic = portElemNoPeriodic;
            int elemCnt = elemNoPeriodic.Count;
            for (int ie = 0; ie < elemCnt; ie++)
            {
                int elemNo = (int)elemNoPeriodic[ie];
                FemElement femElement = Elements[elemNo - 1];
                System.Diagnostics.Debug.Assert(femElement.No == elemNo);

                if (elemShapeDv == Constants.FemElementShapeDV.Triangle && order == Constants.SecondOrder)
                {
                    // 2次三角形要素
                    FemMat_Tri_Second.AddElementMatPeriodic(
                        isYDirectionPeriodic,
                        isSVEA,
                        waveLength,
                        nodeCntPeriodic,
                        freeNodeCntPeriodic_0,
                        toSortedPeriodic,
                        femElement,
                        Nodes,
                        Medias,
                        ForceNodeNumberH,
                        WaveModeDv,
                        ref KMat0,
                        ref CMat0,
                        ref MMat0);
                }
                else if (elemShapeDv == Constants.FemElementShapeDV.Triangle && order == Constants.FirstOrder)
                {
                    // 1次三角形要素
                    FemMat_Tri_First.AddElementMatPeriodic(
                        isYDirectionPeriodic,
                        isSVEA,
                        waveLength,
                        nodeCntPeriodic,
                        freeNodeCntPeriodic_0,
                        toSortedPeriodic,
                        femElement,
                        Nodes,
                        Medias,
                        ForceNodeNumberH,
                        WaveModeDv,
                        ref KMat0,
                        ref CMat0,
                        ref MMat0);
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
            }

            double periodicDistance = latticeA; // 正方格子誘電体ロッド限定
            bool isModeTrace = false;
            KrdLab.clapack.Complex[] tmpPrevModalVec_1stMode = null;
            double minBeta = 0.00; // 正方格子誘電体ロッド限定
            //double maxBeta = 0.90; // 正方格子誘電体ロッド限定
            double maxBeta = 0.5 * (2.0 * pi / periodicDistance) / k0;
            bool isPortBc2Reverse = false;
            // 伝搬定数
            KrdLab.clapack.Complex[] betamToSolveList = null;
            // 界ベクトルは全節点分作成
            KrdLab.clapack.Complex[][] resVecList = null;
            if (isSVEA)
            {
                System.Diagnostics.Debug.Assert(isSVEA == true);
                solveSVEAAsQuadraticGeneralizedEigenToStandardWithRealMat(
                    incidentModeIndex,
                    k0,
                    KMat0,
                    CMat0,
                    MMat0,
                    isPortBc2Reverse,
                    nodeCntPeriodic,
                    freeNodeCntPeriodic_0,
                    freeNodeCntPeriodic,
                    nodeCntBPeriodic,
                    sortedNodesPeriodic,
                    toSortedPeriodic,
                    toNodePeriodic,
                    toNodePeriodicB1,
                    defectNodePeriodic,
                    isModeTrace,
                    ref tmpPrevModalVec_1stMode,
                    minBeta,
                    maxBeta,
                    k0, //(2.0 * pi / periodicDistance), //k0, //1.0,
                    out betamToSolveList,
                    out resVecList);
            }
            else
            {
                System.Diagnostics.Debug.Assert(isSVEA == false);
                solveNonSVEAModeAsQuadraticGeneralizedEigenWithRealMat(
                    incidentModeIndex,
                    periodicDistance,
                    k0,
                    KMat0,
                    isPortBc2Reverse,
                    nodeCntPeriodic,
                    freeNodeCntPeriodic_0,
                    freeNodeCntPeriodic,
                    nodeCntBPeriodic,
                    sortedNodesPeriodic,
                    toSortedPeriodic,
                    toNodePeriodic,
                    toNodePeriodicB1,
                    coordsPeriodic,
                    defectNodePeriodic,
                    isModeTrace,
                    ref tmpPrevModalVec_1stMode,
                    minBeta,
                    maxBeta,
                    (2.0 * pi / periodicDistance), //k0, //1.0,
                    out betamToSolveList,
                    out resVecList);
            }

            // 固有値が1つでも取得できているかチェック
            if (betamToSolveList == null)
            {
                System.Diagnostics.Debug.WriteLine("betamToSolveList == null");
                //System.Diagnostics.Debug.Assert(false);
                return;
            }
            for (int imode = 0; imode < betamToSolveList.Length; imode++)
            {
                KrdLab.clapack.Complex betam = betamToSolveList[imode];
                KrdLab.clapack.Complex[] resVec = resVecList[imode];
                // ポート境界1の節点の値を境界2にも格納する
                for (int ino = 0; ino < nodePeriodicB1.Count; ino++)
                {
                    // 境界1の節点
                    int nodeNumberPortBc1 = nodePeriodicB1[ino];
                    int ino_InLoop_PortBc1 = toNodePeriodic[nodeNumberPortBc1];
                    // 境界1の節点の界の値を取得
                    KrdLab.clapack.Complex cvalue = resVec[ino_InLoop_PortBc1];

                    // 境界2の節点
                    int ino_B2 = isPortBc2Reverse ? (int)(nodePeriodicB2.Count - 1 - ino) : (int)ino;
                    int nodeNumberPortBc2 = nodePeriodicB2[ino_B2];

                    int ino_InLoop_PortBc2 = toNodePeriodic[nodeNumberPortBc2];
                    if (isSVEA)
                    {
                        // 緩慢変化包絡線近似の場合は、Φ2 = Φ1
                        resVec[ino_InLoop_PortBc2] = cvalue;
                    }
                    else
                    {
                        // 緩慢変化包絡線近似でない場合は、Φ2 = expA * Φ1
                        KrdLab.clapack.Complex expA = KrdLab.clapack.Complex.Exp(-1.0 * KrdLab.clapack.Complex.ImaginaryOne * betam * periodicDistance);
                        resVec[ino_InLoop_PortBc2] = expA * cvalue; // 直接Bloch境界条件を指定する場合
                    }
                }
            }

            /////////////////////////////////////////////////////////////////////////////////////
            // 位相調整

            for (int imode = 0; imode < betamToSolveList.Length; imode++)
            {
                KrdLab.clapack.Complex[] resVec = resVecList[imode];
                KrdLab.clapack.Complex phaseShift = 1.0;
                double maxAbs = double.MinValue;
                KrdLab.clapack.Complex fValueAtMaxAbs = 0.0;
                {
                    /*
                    // 境界上で位相調整する
                    for (int ino = 0; ino < nodePeriodicB1.Count; ino++)
                    {
                        int nodeNumberPortBc1 = nodePeriodicB1[ino];
                        int ino_InLoop_PortBc1 = toNodePeriodic[nodeNumberPortBc1];
                        KrdLab.clapack.Complex cvalue = resVec[ino_InLoop_PortBc1];
                        double abs = KrdLab.clapack.Complex.Abs(cvalue);
                        if (abs > maxAbs)
                        {
                            maxAbs = abs;
                            fValueAtMaxAbs = cvalue;
                        }
                    }
                     */

                    
                    // 領域全体で位相調整する
                    for (int ino_InLoop = 0; ino_InLoop < resVec.Length; ino_InLoop++)
                    {
                        KrdLab.clapack.Complex cvalue = resVec[ino_InLoop];
                        double abs = KrdLab.clapack.Complex.Abs(cvalue);
                        if (abs > maxAbs)
                        {
                            maxAbs = abs;
                            fValueAtMaxAbs = cvalue;
                        }
                    }
                     

                }
                if (maxAbs >= MyUtilLib.Matrix.Constants.PrecisionLowerLimit)
                {
                    phaseShift = fValueAtMaxAbs / maxAbs;
                }
                System.Diagnostics.Debug.WriteLine("phaseShift: {0} (°)", Math.Atan2(phaseShift.Imaginary, phaseShift.Real) * 180.0 / pi);
                for (int i = 0; i < resVec.Length; i++)
                {
                    resVec[i] /= phaseShift;
                }
            }

            /////////////////////////////////////////////////////////////////////////////////////
            // X方向の微分値を取得する
            KrdLab.clapack.Complex[][] resDFDXVecList = new KrdLab.clapack.Complex[betamToSolveList.Length][];
            for (int imode = 0; imode < betamToSolveList.Length; imode++)
            {
                KrdLab.clapack.Complex[] resVec = resVecList[imode];
                KrdLab.clapack.Complex[] resDFDXVec = null;
                KrdLab.clapack.Complex[] resDFDYVec = null;
                double rotAngle = 0.0; // 暫定
                double[] rotOrigin = null; // 暫定

                if (elemShapeDv == Constants.FemElementShapeDV.Triangle && order == Constants.SecondOrder)
                {
                    // 2次三角形要素
                    getDFDXValues_Tri_SecondOrder(
                        WaveModeDv,
                        k0,
                        rotAngle,
                        rotOrigin,
                        Nodes,
                        Elements,
                        Medias,
                        ForceNodeNumberH,
                        elemNoPeriodic,
                        nodePeriodicB,
                        toNodePeriodic,
                        resVec,
                        out resDFDXVec,
                        out resDFDYVec);
                }
                else if (elemShapeDv == Constants.FemElementShapeDV.Triangle && order == Constants.FirstOrder)
                {
                    // 1次三角形要素
                    getDFDXValues_Tri_FirstOrder(
                        WaveModeDv,
                        k0,
                        rotAngle,
                        rotOrigin,
                        Nodes,
                        Elements,
                        Medias,
                        ForceNodeNumberH,
                        elemNoPeriodic,
                        nodePeriodicB,
                        toNodePeriodic,
                        resVec,
                        out resDFDXVec,
                        out resDFDYVec);
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                if (isYDirectionPeriodic)
                {
                    // Y方向周期構造の場合
                    resDFDXVecList[imode] = resDFDYVec;
                }
                else
                {
                    // X方向周期構造の場合
                    resDFDXVecList[imode] = resDFDXVec;
                }
            }
            // 境界1と境界2の節点の微分値は同じという条件を弱形式で課している為、微分値は同じにならない。
            // 加えて、getDFDXValuesは内部節点からの寄与を片側のみしか計算していない。
            // →境界の両側からの寄与を考慮する為に境界1の微分値と境界2の微分値を平均してみる
            for (int imode = 0; imode < betamToSolveList.Length; imode++)
            {
                KrdLab.clapack.Complex betam = betamToSolveList[imode];
                KrdLab.clapack.Complex[] resDFDXVec = resDFDXVecList[imode];
                // ポート境界1の節点の微分値、ポート境界2の微分値は、両者の平均をとる
                for (int ino = 0; ino < nodePeriodicB1.Count; ino++)
                {
                    // 境界1の節点
                    int nodeNumberPortBc1 = nodePeriodicB1[ino];
                    int ino_InLoop_PortBc1 = toNodePeriodic[nodeNumberPortBc1];
                    // 境界1の節点の界の微分値値を取得
                    KrdLab.clapack.Complex cdFdXValue1 = resDFDXVec[ino_InLoop_PortBc1];

                    // 境界2の節点
                    int ino_B2 = isPortBc2Reverse ? (int)(nodePeriodicB2.Count - 1 - ino) : (int)ino;
                    int nodeNumberPortBc2 = nodePeriodicB2[ino_B2];
                    int ino_InLoop_PortBc2 = toNodePeriodic[nodeNumberPortBc2];
                    // 境界2の節点の界の微分値値を取得
                    KrdLab.clapack.Complex cdFdXValue2 = resDFDXVec[ino_InLoop_PortBc2];

                    // 平均値を計算し、境界の微分値として再格納する
                    if (isSVEA)
                    {
                        KrdLab.clapack.Complex cdFdXValue = (cdFdXValue1 + cdFdXValue2) * 0.5;
                        resDFDXVec[ino_InLoop_PortBc1] = cdFdXValue;
                        resDFDXVec[ino_InLoop_PortBc2] = cdFdXValue;
                    }
                    else
                    {
                        // 緩慢変化包絡線近似でない場合は、Φ2 = expA * Φ1
                        KrdLab.clapack.Complex expA = KrdLab.clapack.Complex.Exp(-1.0 * KrdLab.clapack.Complex.ImaginaryOne * betam * periodicDistance);
                        // Φ2から逆算でΦ1を求め、平均をとる
                        KrdLab.clapack.Complex cdFdXValue = (cdFdXValue1 + cdFdXValue2 / expA) * 0.5;
                        resDFDXVec[ino_InLoop_PortBc1] = cdFdXValue;
                        resDFDXVec[ino_InLoop_PortBc2] = cdFdXValue * expA;
                    }
                }
            }

            //////////////////////////////////////////////////////////////////////////////////////
            if (!isSVEA)
            {
                // 緩慢変化包絡線近似でない場合は、緩慢変化包絡線近似の分布に変換する
                for (int imode = 0; imode < betamToSolveList.Length; imode++)
                {
                    KrdLab.clapack.Complex betam = betamToSolveList[imode];
                    KrdLab.clapack.Complex betam_periodic = ToBetaPeriodic(betam, periodicDistance);
                    KrdLab.clapack.Complex[] resVec = resVecList[imode];
                    KrdLab.clapack.Complex[] resDFDXVec = resDFDXVecList[imode];
                    System.Diagnostics.Debug.Assert(resVec.Length == coordsPeriodic.Count);
                    int nodeNumber1st = sortedNodesPeriodic[0];
                    int ino_InLoop_1st = toNodePeriodic[nodeNumber1st];
                    double[] coord1st = coordsPeriodic[ino_InLoop_1st];
                    for (int ino_InLoop = 0; ino_InLoop < resVec.Length; ino_InLoop++)
                    {
                        // 節点の界の値
                        KrdLab.clapack.Complex fieldVal = resVec[ino_InLoop];
                        // 節点の界の微分値
                        KrdLab.clapack.Complex dFdXVal = resDFDXVec[ino_InLoop];
                        // 節点の座標
                        double[] coord = coordsPeriodic[ino_InLoop];
                        double x_pt = 0.0;
                        if (isYDirectionPeriodic)
                        {
                            x_pt = (coord[1] - coord1st[1]);
                        }
                        else
                        {
                            x_pt = (coord[0] - coord1st[0]);
                        }
                        //KrdLab.clapack.Complex expX = KrdLab.clapack.Complex.Exp(-1.0 * KrdLab.clapack.Complex.ImaginaryOne * betam * x_pt);
                        KrdLab.clapack.Complex expX = KrdLab.clapack.Complex.Exp(-1.0 * KrdLab.clapack.Complex.ImaginaryOne * betam_periodic * x_pt);

                        // ΦのSVEA(φ)
                        KrdLab.clapack.Complex fieldVal_SVEA = fieldVal / expX;
                        resVec[ino_InLoop] = fieldVal_SVEA;

                        // SVEAの微分( exp(-jβx)dφ/dx = dΦ/dx + jβφexp(-jβx))
                        //resDFDXVec[ino_InLoop] = dFdXVal / expX + KrdLab.clapack.Complex.ImaginaryOne * betam * fieldVal_SVEA;
                        resDFDXVec[ino_InLoop] = dFdXVal / expX + KrdLab.clapack.Complex.ImaginaryOne * betam_periodic * fieldVal_SVEA;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine("betamToSolveList.Length {0}", betamToSolveList.Length);

            //////////////////////////////////////////////////////////////////////////////////////
            // モード数の修正
            int maxMode = maxModeSpecified;
            if (maxMode > betamToSolveList.Length)
            {
                maxMode = betamToSolveList.Length;
            }

            //////////////////////////////////////////////////////////////////////////////////////
            // 表示用に周期構造領域の固有モード分布を格納する
            eigenVecsPeriodic = new KrdLab.clapack.Complex[maxMode][];
            for (int imode = betamToSolveList.Length - 1, tagtModeIndex = 0; imode >= 0 && tagtModeIndex < maxMode; imode--, tagtModeIndex++)
            {
                KrdLab.clapack.Complex[] resVec = resVecList[imode];
                System.Diagnostics.Debug.Assert(resVec.Length == nodePeriodic.Count);
                eigenVecsPeriodic[tagtModeIndex] = new KrdLab.clapack.Complex[nodePeriodic.Count];
                resVec.CopyTo(eigenVecsPeriodic[tagtModeIndex], 0);
            }

            //////////////////////////////////////////////////////////////////////////////////////
            // 固有値、固有ベクトル
            // 格納
            int nodeCntB1 = nodePeriodicB1.Count;
            eigenValues = new Complex[maxMode];
            //eigenVecsB1 = new Complex[maxMode, nodeCntB1];
            //eigen_dFdXsB1 = new Complex[maxMode, nodeCntB1];
            eigenVecsB1 = new Complex[maxMode, nodesBoundary.Length]; // 強制境界を除く
            eigen_dFdXsB1 = new Complex[maxMode, nodesBoundary.Length]; // 強制境界を除く
            for (int imode = 0; imode < maxMode; imode++)
            {
                eigenValues[imode] = new Complex(0, 0);
                for (int ino = 0; ino < eigenVecsB1.GetLength(1); ino++)
                {
                    eigenVecsB1[imode, ino] = new Complex(0, 0);
                    eigen_dFdXsB1[imode, ino] = new Complex(0, 0);
                }
            }
            for (int imode = betamToSolveList.Length - 1, tagtModeIndex = 0; imode >= 0 && tagtModeIndex < maxMode; imode--, tagtModeIndex++)
            {
                //System.Diagnostics.Debug.WriteLine("imode = {0}", imode);

                KrdLab.clapack.Complex workBetam = betamToSolveList[imode];
                KrdLab.clapack.Complex[] resVec = resVecList[imode];
                KrdLab.clapack.Complex[] resDFDXVec = resDFDXVecList[imode];

                Complex betam = new Complex(workBetam.Real, workBetam.Imaginary);
                bool isComplexConjugateMode = false;
                //   減衰定数は符号がマイナス(β = -jα)
                if (betam.Imaginary > 0.0 && Math.Abs(betam.Real) <= 1.0e-12)
                {
                    betam = new Complex(betam.Real, -betam.Imaginary);
                    isComplexConjugateMode = true;
                }
                //Complex[] evec = new Complex[nodeCntB1];
                //Complex[] evec_dFdX = new Complex[nodeCntB1];
                Complex[] evec = new Complex[nodesBoundary.Length]; // 強制境界を除く
                Complex[] evec_dFdX = new Complex[nodesBoundary.Length]; // 強制境界を除く
                for (int ino = 0; ino < nodeCntB1; ino++)
                {
                    int nodeNumberPortBc1 = nodePeriodicB1[ino];
                    //System.Diagnostics.Debug.WriteLine("ino = {0} nodeNumberPortBc1 = {1}", ino, nodeNumberPortBc1);
                    int ino_InLoop_PortBc1 = toNodePeriodic[nodeNumberPortBc1];
                    Complex cvalue = new Complex(resVec[ino_InLoop_PortBc1].Real, resVec[ino_InLoop_PortBc1].Imaginary);
                    Complex dFdXValue = new Complex(resDFDXVec[ino_InLoop_PortBc1].Real, resDFDXVec[ino_InLoop_PortBc1].Imaginary);
                    if (isComplexConjugateMode)
                    {
                        cvalue = Complex.Conjugate(cvalue);
                        dFdXValue = Complex.Conjugate(dFdXValue);
                    }

                    //evec[ino] = cvalue;
                    //evec_dFdX[ino] = dFdXValue;
                    ///////////////////////////////////////////////////
                    // 強制境界を除く
                    if (!toNodesB.ContainsKey(nodeNumberPortBc1))
                    {
                        continue;
                    }
                    int ino_f = toNodesB[nodeNumberPortBc1];
                    //System.Diagnostics.Debug.WriteLine("ino_f = {0}", ino_f);

                    evec[ino_f] = cvalue;
                    evec_dFdX[ino_f] = dFdXValue;
                    ///////////////////////////////////////////////////

                    //if (tagtModeIndex == incidentModeIndex)
                    {
                        //System.Diagnostics.Debug.WriteLine("evec[{0}] = {1} + {2} i", ino_f, evec[ino_f].Real, evec[ino_f].Imaginary);
                    }
                }
                // 規格化定数を求める
                Complex[] workVec = MyMatrixUtil.product(ryy_1d, evec);
                //Complex[] evec_Modify = new Complex[nodeCntB1];
                Complex[] evec_Modify = new Complex[nodesBoundary.Length];//強制境界を除く
                Complex imagOne = new Complex(0.0, 1.0);
                Complex betam_periodic = ToBetaPeriodic(betam, periodicDistance);
                for (int ino = 0; ino < nodeCntB1; ino++)
                {
                    //evec_Modify[ino] = evec[ino] - evec_dFdX[ino] / (imagOne * betam_periodic);
                    ///////////////////////////////////////////////////
                    // 強制境界を除く
                    int nodeNumberPortBc1 = nodePeriodicB1[ino];
                    if (!toNodesB.ContainsKey(nodeNumberPortBc1))
                    {
                        continue;
                    }
                    int ino_f = toNodesB[nodeNumberPortBc1];
                    evec_Modify[ino_f] = evec[ino_f] - evec_dFdX[ino_f] / (imagOne * betam_periodic);
                    ///////////////////////////////////////////////////
                }
                //Complex dm = MyMatrixUtil.vector_Dot(MyMatrixUtil.vector_Conjugate(evec), workVec);
                Complex dm = MyMatrixUtil.vector_Dot(MyMatrixUtil.vector_Conjugate(evec_Modify), workVec);
                if (WaveModeDv == FemSolver.WaveModeDV.TM)
                {
                    // TMモード
                    dm = Complex.Sqrt(omega * eps0 * Complex.Conjugate(betam) / (Complex.Abs(betam) * Complex.Conjugate(betam_periodic)) / dm);
                }
                else
                {
                    // TEモード
                    dm = Complex.Sqrt(omega * mu0 * Complex.Conjugate(betam) / (Complex.Abs(betam) * Complex.Conjugate(betam_periodic)) / dm);
                }

                // 伝搬定数の格納
                eigenValues[tagtModeIndex] = betam;
                if (tagtModeIndex < 10)
                {
                    System.Diagnostics.Debug.WriteLine("β/k0  ( " + tagtModeIndex + " ) = " + betam.Real / k0 + " + " + betam.Imaginary / k0 + " i " + ((incidentModeIndex == tagtModeIndex) ? " incident" : ""));
                }
                // 固有ベクトルの格納(規格化定数を掛ける)
                for (int ino = 0; ino < evec.Length; ino++)
                {
                    Complex fm = dm * evec[ino];
                    Complex dfmdx = dm * evec_dFdX[ino];
                    eigenVecsB1[tagtModeIndex, ino] = fm;
                    eigen_dFdXsB1[tagtModeIndex, ino] = dfmdx;
                    //System.Diagnostics.Debug.WriteLine("eigen_vecs_Bc1({0}, {1}) = {2} + {3} i", imode, ino, fm.Real, fm.Imag);
                }
            }
            System.Diagnostics.Debug.WriteLine("eigen end");
        }

    
    }
}
