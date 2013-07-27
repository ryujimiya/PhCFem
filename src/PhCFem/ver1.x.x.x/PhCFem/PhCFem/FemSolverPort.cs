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
    /// ポート固有モード解析
    /// </summary>
    class FemSolverPort
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
            bool isInputPort,
            int IncidentModeIndex,
            int[] nodesBoundary,
            MyDoubleMatrix ryy_1d,
            Complex[] eigenValues,
            Complex[,] eigenVecs,
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

            for (int imode = 0; imode < maxMode; imode++)
            {
                Complex betam = eigenValues[imode];

                Complex[] fmVec = MyMatrixUtil.matrix_GetRowVec(eigenVecs, (int)imode);
                // 2Dの境界積分
                Complex[] veci = MyMatrixUtil.product(ryy_1d, fmVec);
                // モード電力規格化の積分(1Dの積分)
                // [ryy]が実数の場合
                Complex[] vecj = MyMatrixUtil.product(ryy_1d, MyMatrixUtil.vector_Conjugate(fmVec));
                for (int inoB = 0; inoB < nodeCnt; inoB++)
                {
                    for (int jnoB = 0; jnoB < nodeCnt; jnoB++)
                    {
                        //Complex cvalue = (Complex.ImaginaryOne / (omega * myu0)) * betam * Complex.Abs(betam) * veci[inoB] * vecj[jnoB];
                        Complex cvalue;
                        {
                            // H面、平行平板
                            if (WaveModeDv == FemSolver.WaveModeDV.TM)
                            {
                                cvalue = (Complex.ImaginaryOne / (omega * eps0)) * betam * Complex.Abs(betam) * veci[inoB] * vecj[jnoB];
                            }
                            else
                            {
                                cvalue = (Complex.ImaginaryOne / (omega * mu0)) * betam * Complex.Abs(betam) * veci[inoB] * vecj[jnoB];
                            }
                        }
                        matB._body[inoB + jnoB * matB.RowSize] += cvalue;
                    }
                }
            }
            // check 対称行列
            for (int inoB = 0; inoB < matB.RowSize; inoB++)
            {
                for (int jnoB = inoB; jnoB < matB.ColumnSize; jnoB++)
                {
                    if (Math.Abs(matB[inoB, jnoB].Real - matB[jnoB, inoB].Real) >= Constants.PrecisionLowerLimit)
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }
                    if (Math.Abs(matB[inoB, jnoB].Imaginary - matB[jnoB, inoB].Imaginary) >= Constants.PrecisionLowerLimit)
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }
                }
            }
            //MyMatrixUtil.printMatrix("matB", matB);

            // 残差ベクトルの作成
            Complex[] resVecB = new Complex[nodeCnt];
            if (isInputPort)
            {
                int imode = IncidentModeIndex;
                Complex betam = eigenValues[imode];
                Complex[] fmVec = MyMatrixUtil.matrix_GetRowVec(eigenVecs, (int)imode);
                // 2Dの境界積分
                Complex[] veci = MyMatrixUtil.product(ryy_1d, fmVec);
                for (int inoB = 0; inoB < nodeCnt; inoB++)
                {
                    // H面、平行平板、E面
                    Complex cvalue = 2.0 * Complex.ImaginaryOne * betam * veci[inoB];
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
            int iMode,
            bool isIncidentMode,
            int[] nodesBoundary,
            MyDoubleMatrix ryy_1d,
            Complex[] eigenValues,
            Complex[,] eigenVecs,
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

            // {tmp_vec}*t = {fm}*t[ryy]*t
            // {tmp_vec}* = [ryy]* {fm}*
            //   ([ryy]*)t = [ryy]*
            //    [ryy]が実数のときは、[ryy]* -->[ryy]
            Complex[] fmVec = MyMatrixUtil.matrix_GetRowVec(eigenVecs, iMode);
            // ryyが実数のとき
            Complex[] tmp_vec = MyMatrixUtil.product(ryy_1d, MyMatrixUtil.vector_Conjugate(fmVec));

            // s11 = {tmp_vec}t {value_all}
            s11 = MyMatrixUtil.vector_Dot(tmp_vec, valuesB);
            Complex betam = eigenValues[iMode];
            {
                // H面、平行平板
                if (WaveModeDv == FemSolver.WaveModeDV.TM)
                {
                    s11 *= (Complex.Abs(betam) / (omega * eps0));
                    if (isIncidentMode)
                    {
                        s11 += -1.0;
                    }
                }
                else
                {
                    s11 *= (Complex.Abs(betam) / (omega * mu0));
                    if (isIncidentMode)
                    {
                        s11 += -1.0;
                    }
                }
            }

            return s11;
        }

        /// <summary>
        /// ポート固有値解析
        /// </summary>
        public static void SolvePortWaveguideEigen(
            FemSolver.WaveModeDV WaveModeDv,
            double waveLength,
            int maxModeSpecified,
            IList<FemNode> Nodes,
            Dictionary<string, IList<int>> EdgeToElementNoH,
            IList<FemElement> Elements,
            MediaInfo[] Medias,
            Dictionary<int, bool> ForceNodeNumberH,
            IList<int> portNodes,
            out int[] nodesBoundary,
            out MyDoubleMatrix ryy_1d,
            out Complex[] eigenValues,
            out Complex[,] eigenVecs)
        {
            //System.Diagnostics.Debug.WriteLine("solvePortWaveguideEigen: {0},{1}", waveLength, portNo);
            nodesBoundary = null;
            ryy_1d = null;
            eigenValues = null;
            eigenVecs = null;


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
                GetMatNonzeroPatternForEigen(elements, toSorted, out matPattern);
                GetBandMatrixSubDiaSizeAndSuperDiaSizeForEigen(matPattern, out rowcolSize, out subdiaSize, out superdiaSize);
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
            // 固有値、固有ベクトル
            int maxMode = maxModeSpecified;
            if (maxMode > nodeCnt)
            {
                maxMode = nodeCnt;
            }
            eigenValues = new Complex[maxMode];
            eigenVecs = new Complex[maxMode, nodeCnt];
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

            // [A] = [Txx] - k0 * k0 *[Uzz]
            //メモリ節約
            //MyDoubleMatrix matA = new MyDoubleMatrix(nodeCnt, nodeCnt);
            MyDoubleSymmetricBandMatrix matA = new MyDoubleSymmetricBandMatrix(nodeCnt, subdiaSize, superdiaSize);
            for (int ino = 0; ino < nodeCnt; ino++)
            {
                for (int jno = 0; jno < nodeCnt; jno++)
                {
                    // 対称バンド行列対応
                    if (matA is MyDoubleSymmetricBandMatrix && ino > jno)
                    {
                        continue;
                    }
                    // 剛性行列
                    //matA[ino, jno] = txx_1d[ino, jno] - (k0 * k0) * uzz_1d[ino, jno];
                    //  質量行列matBが正定値行列となるように剛性行列matAの方の符号を反転する
                    matA[ino, jno] = -(txx_1d[ino, jno] - (k0 * k0) * uzz_1d[ino, jno]);
                }
            }

            // ( [txx] - k0^2[uzz] + β^2[ryy]){Ez} = {0}より
            // [A]{x} = λ[B]{x}としたとき、λ = β^2 とすると[B] = -[ryy]
            //MyDoubleMatrix matB = MyMatrixUtil.product(-1.0, ryy_1d);
            // 質量行列が正定値となるようにするため、上記符号反転を剛性行列の方に反映し、質量行列はryy_1dをそのまま使用する
            //MyDoubleMatrix matB = new MyDoubleMatrix(ryy_1d);
            MyDoubleSymmetricBandMatrix matB = new MyDoubleSymmetricBandMatrix((MyDoubleSymmetricBandMatrix)ryy_1d);

            // 一般化固有値問題を解く
            Complex[] evals = null;
            Complex[,] evecs = null;
            try
            {
                // 固有値、固有ベクトルを求める
                solveEigen(matA, matB, out evals, out evecs);
                // 固有値のソート
                Sort1DEigenMode(k0, evals, evecs);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
                System.Diagnostics.Debug.Assert(false);
            }
            for (int imode = 0; imode < evecs.GetLength(0); imode++)
            {
                KrdLab.clapack.Complex phaseShift = 1.0;
                double maxAbs = double.MinValue;
                KrdLab.clapack.Complex fValueAtMaxAbs = 0.0;
                {
                    // 境界上で位相調整する
                    for (int ino = 0; ino < evecs.GetLength(1); ino++)
                    {
                        KrdLab.clapack.Complex cvalue = evecs[imode, ino];
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
                //System.Diagnostics.Debug.WriteLine("phaseShift: {0} (°)", Math.Atan2(phaseShift.Imaginary, phaseShift.Real) * 180.0 / pi);
                for (int ino = 0; ino < evecs.GetLength(1); ino++)
                {
                    evecs[imode, ino] /= phaseShift;
                }
            }

            for (int imode = 0; imode < maxMode; imode++)
            {
                eigenValues[imode] = 0;
            }
            for (int tagtModeIdx = evals.Length - 1, imode = 0; tagtModeIdx >= 0 && imode < maxMode; tagtModeIdx--)
            {
                // 伝搬定数は固有値のsqrt
                Complex betam = Complex.Sqrt(evals[tagtModeIdx]);
                // 定式化BUGFIX
                //   減衰定数は符号がマイナス(β = -jα)
                bool isConjugateMode = false;
                if (betam.Imaginary >= 0.0)
                {
                    betam = new Complex(betam.Real, -betam.Imaginary);
                    isConjugateMode = true;
                }
                // 固有ベクトル
                Complex[] evec = MyMatrixUtil.matrix_GetRowVec(evecs, tagtModeIdx);
                if (isConjugateMode)
                {
                    evec = MyMatrixUtil.vector_Conjugate(evec);
                }
                // 規格化定数を求める
                // 実数の場合 [ryy]*t = [ryy]t ryyは対称行列より[ryy]t = [ryy]
                Complex[] workVec = MyMatrixUtil.product(ryy_1d, evec);
                Complex dm = MyMatrixUtil.vector_Dot(MyMatrixUtil.vector_Conjugate(evec), workVec);
                {
                    // H面、平行平板
                    if (WaveModeDv == FemSolver.WaveModeDV.TM)
                    {
                        dm = Complex.Sqrt(omega * eps0 / Complex.Abs(betam) / dm);
                    }
                    else
                    {
                        dm = Complex.Sqrt(omega * mu0 / Complex.Abs(betam) / dm);
                    }
                }
                //System.Diagnostics.Debug.WriteLine("dm = " + dm);

                // 伝搬定数の格納
                eigenValues[imode] = betam;
                // check
                if (imode < 5)
                {
                    //System.Diagnostics.Debug.WriteLine("eigenValues [ " + imode + "] = " + betam.Real + " + " + betam.Imaginary + " i " + " tagtModeIdx :" + tagtModeIdx + " " );
                    System.Diagnostics.Debug.WriteLine("β/k0 [ " + imode + "] = " + betam.Real / k0 + " + " + betam.Imaginary / k0 + " i " + " tagtModeIdx :" + tagtModeIdx + " ");
                }
                // 固有ベクトルの格納(規格化定数を掛ける)
                for (int inoSorted = 0; inoSorted < nodeCnt; inoSorted++)
                {
                    Complex fm = dm * evec[inoSorted];
                    eigenVecs[imode, inoSorted] = fm;
                    //System.Diagnostics.Debug.WriteLine("eigenVecs [ " + imode + ", " + inoSorted + "] = " + fm.Real + " + " + fm.Imaginary + " i  Abs:" + Complex.Abs(fm));
                }
                imode++;
            }

        }


        /// <summary>
        /// 固有値解析FEM行列の非０パターンを取得する
        ///   対称行列を前提条件とする
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="toSorted">ソート済み節点番号→ソート済み節点リストのインデックスマップ</param>
        /// <param name="matPattern">非０パターンの配列(非０の要素はtrue、０要素はfalse)</param>
        public static void GetMatNonzeroPatternForEigen(
            IList<FemLineElement> elements,
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
            foreach (FemLineElement element in elements)
            {
                int[] nodeNumbers = element.NodeNumbers;

                foreach (int iNodeNumber in nodeNumbers)
                {
                    //if (ForceBCNodes.Contains(iNodeNumber)) continue;
                    //int ino_global = sortedNodes.IndexOf(iNodeNumber);
                    //if (ino_global == -1) continue;
                    if (!toSorted.ContainsKey(iNodeNumber)) continue;
                    int ino_global = toSorted[iNodeNumber];
                    foreach (int jNodeNumber in nodeNumbers)
                    {
                        //if (ForceBCNodes.Contains(jNodeNumber)) continue;
                        //int jno_global = sortedNodes.IndexOf(jNodeNumber);
                        //if (jno_global == -1) continue;
                        if (!toSorted.ContainsKey(jNodeNumber)) continue;
                        int jno_global = toSorted[jNodeNumber];
                        matPattern[ino_global, jno_global] = true;
                    }
                }
            }
        }

        /// <summary>
        /// 固有値解析FEM行列のバンドマトリクス情報を取得する
        ///   対称行列を前提条件とする
        /// </summary>
        /// <param name="matPattern">非０パターンの配列</param>
        /// <param name="rowcolSize">行数=列数</param>
        /// <param name="subdiaSize">subdiagonalのサイズ</param>
        /// <param name="superdiaSize">superdiagonalのサイズ</param>
        public static void GetBandMatrixSubDiaSizeAndSuperDiaSizeForEigen(
            bool[,] matPattern,
            out int rowcolSize,
            out int subdiaSize,
            out int superdiaSize)
        {
            rowcolSize = matPattern.GetLength(0);

            // subdiaサイズ、superdiaサイズを取得する
            subdiaSize = 0;
            superdiaSize = 0;
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
            // 対称行列なので、subdiaSize == superdiaSize
            subdiaSize = superdiaSize;

            System.Diagnostics.Debug.WriteLine("(EigenValueProblem) rowcolSize: {0} subdiaSize: {1} superdiaSize: {2}", rowcolSize, subdiaSize, superdiaSize);
        }

        /// <summary>
        /// Lisys(Lapack)による固有値解析(一般化固有値問題)
        /// </summary>
        private static bool solveEigen(MyDoubleSymmetricBandMatrix stiffnessMat, MyDoubleSymmetricBandMatrix massMat, out Complex[] evals, out Complex[,] evecs)
        {
            // Lisys(Lapack)による固有値解析
            double[] A = MyMatrixUtil.matrix_ToBuffer(stiffnessMat, false);
            double[] B = MyMatrixUtil.matrix_ToBuffer(massMat, false);
            double[] r_evals = null;
            double[][] r_evecs = null;
            int a_row = stiffnessMat.RowSize;
            int a_col = stiffnessMat.ColumnSize;
            int a_superdiaSize = stiffnessMat.SuperdiaSize;
            int b_row = massMat.RowSize;
            int b_col = massMat.ColumnSize;
            int b_superdiaSize = massMat.SuperdiaSize;
            KrdLab.clapack.FunctionExt.dsbgv(A, a_row, a_col, a_superdiaSize, B, b_row, b_col, b_superdiaSize, ref r_evals, ref r_evecs);

            evals = new Complex[r_evals.Length];
            for (int i = 0; i < evals.Length; i++)
            {
                //evals[i] = new Complex(r_evals[i], 0.0);
                evals[i].Real = r_evals[i];
                evals[i].Imaginary = 0.0;
                //System.Diagnostics.Debug.WriteLine("( " + i + " ) = " + evals[i].Real + " + " + evals[i].Imaginary + " i ");
            }
            evecs = new Complex[r_evecs.Length, r_evecs[0].Length];
            for (int i = 0; i < evecs.GetLength(0); i++)
            {
                for (int j = 0; j < evecs.GetLength(1); j++)
                {
                    //evecs[i, j] = new Complex(r_evecs[i][j], 0.0);
                    evecs[i, j].Real = r_evecs[i][j];
                    evecs[i, j].Imaginary = 0.0;
                    //System.Diagnostics.Debug.WriteLine("( " + i + ", " + j + " ) = " + evecs[i, j].Real + " + " + evecs[i, j].Imaginary + " i ");
                }
            }

            return true;
        }

        /// <summary>
        /// 固有値のソート用クラス
        /// </summary>
        class EigenModeItem : IComparable<EigenModeItem>
        {
            public int index;
            public Complex eval;
            public Complex[] evec;

            public EigenModeItem(int index_, Complex eval_, Complex[] evec_)
            {
                index = index_;
                eval = eval_;
                evec = evec_;
            }
            int IComparable<EigenModeItem>.CompareTo(EigenModeItem other)
            {
                //BUG: 小数点以下の場合、比較できない
                //int cmp = (int)(this.eval.Real - other.eval.Real);
                double cmpf = this.eval.Real - other.eval.Real;
                int cmp = 0;
                if (Math.Abs(cmpf) < 1.0E-15)
                {
                    cmp = 0;
                }
                else
                {
                    if (cmpf > 0)
                    {
                        cmp = 1;
                    }
                    else
                    {
                        cmp = -1;
                    }
                }
                return cmp;
            }

        }

        /// <summary>
        /// 固有モードをソートする(伝搬モードを先に)
        /// </summary>
        /// <param name="evals"></param>
        /// <param name="evecs"></param>
        public static void Sort1DEigenMode(double k0, Complex[] evals, Complex[,] evecs)
        {
            /*
            // 符号調整
            {
                // 正の固有値をカウント
                //int positive_cnt = 0;
                //foreach (Complex c in evals)
                //{
                //    if (c.Real > 0.0 && Math.Abs(c.Imaginary) <Constants.PrecisionLowerLimit)
                //    {
                //        positive_cnt++;
                //    }
                //}
                // 計算範囲では、正の固有値(伝搬定数の二乗が正)は高々1個のはず
                // 半分以上正の固有値であれば、固有値が逆転して計算されていると判断する
                // 誘電体導波路の場合、これでは破綻する？
                // 最大、最小値を求め、最大値 > 最小値の絶対値なら逆転している
                double minEval = double.MaxValue;
                double maxEval = double.MinValue;
                foreach (Complex c in evals)
                {
                    if (minEval > c.Real) { minEval = c.Real; }
                    if (maxEval < c.Real) { maxEval = c.Real; }
                }
                //if (positive_cnt >= evals.Length / 2)  // 導波管の場合
                if (Math.Abs(maxEval) > Math.Abs(minEval))
                {
                    for (int i = 0; i < evals.Length; i++)
                    {
                        evals[i] = -evals[i];
                    }
                    System.Diagnostics.Debug.WriteLine("eval sign changed");
                }
            }
             */

            EigenModeItem[] items = new EigenModeItem[evals.Length];
            for (int i = 0; i < evals.Length; i++)
            {
                Complex[] evec = MyMatrixUtil.matrix_GetRowVec(evecs, i);
                items[i] = new EigenModeItem(i, evals[i], evec);
            }
            Array.Sort(items);

            int imode = 0;
            foreach (EigenModeItem item in items)
            {
                evals[imode] = item.eval;
                MyMatrixUtil.matrix_setRowVec(evecs, (int)imode, item.evec);
                //System.Diagnostics.Debug.WriteLine("[sorted]( " + imode + " ) = " + evals[imode].Real + " + " + evals[imode].Imaginary + " i ");
                imode++;
            }

        }
    }
}
