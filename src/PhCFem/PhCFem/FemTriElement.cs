using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
//using System.Numerics; // Complex
using KrdLab.clapack; // KrdLab.clapack.Complex

namespace PhCFem
{
    /// <summary>
    /// ２次三角形要素
    /// </summary>
    class FemTriElement : FemElement
    {
        /// <summary>
        /// 描画長方形領域のリスト
        /// </summary>
        protected IList<double[][]> _RectLiList = null;
        /// <summary>
        /// 描画ロジックで原点となる頂点
        /// </summary>
        protected int _OrginVertexNo = 2;

        /// <summary>
        /// フィールド値描画を荒くする？
        /// </summary>
        public override bool IsCoarseFieldMesh
        {
            get
            {
                return base.IsCoarseFieldMesh;
            }
            set
            {
                base.IsCoarseFieldMesh = value;

                // 描画領域を準備する
                setupDrawRect(out _RectLiList, out _OrginVertexNo);
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FemTriElement()
            : base()
        {
        }

        /// <summary>
        /// 節点情報を設定する
        /// </summary>
        /// <param name="nodes"></param>
        public override void SetNodesFromAllNodes(FemNode[] nodes)
        {
            // ベースクラスの処理を実行
            base.SetNodesFromAllNodes(nodes);

            // 描画領域を準備する
            setupDrawRect(out _RectLiList, out _OrginVertexNo);

        }

        /// <summary>
        /// フィールドの回転を取得する
        /// </summary>
        /// <param name="rotXFValues"></param>
        /// <param name="rotYFValues"></param>
        protected override void calcRotField(out Complex[] rotXFValues, out Complex[] rotYFValues)
        {
            base.calcRotField(out rotXFValues, out rotYFValues);
            
            rotXFValues = new Complex[NodeNumbers.Length];
            rotYFValues = new Complex[NodeNumbers.Length];

            const int ndim = Constants.CoordDim2D; //2;      // 座標の次元数
            const int vertexCnt = Constants.TriVertexCnt; //3; // 三角形の頂点の数(2次要素でも同じ)
            //const int nodeCnt = Constants.TriNodeCnt_SecondOrder; //6;  // 三角形2次要素
            int nodeCnt = NodeNumbers.Length;
            if (nodeCnt != Constants.TriNodeCnt_SecondOrder && nodeCnt != Constants.TriNodeCnt_FirstOrder)
            {
                return;
            }
            // 三角形の頂点を取得
            double[][] pp = new double[vertexCnt][];
            for (int ino = 0; ino < pp.GetLength(0); ino++)
            {
                FemNode node = _Nodes[ino];
                System.Diagnostics.Debug.Assert(node.Coord.Length == ndim);
                pp[ino] = new double[ndim];
                pp[ino][0] = node.Coord[0];
                pp[ino][1] = node.Coord[1];
            }
            // 面積座標の微分を求める
            //   dldx[k, n] k面積座標Lkのn方向微分
            double[,] dldx = null;
            double[] const_term = null;
            KerEMatTri.TriDlDx(out dldx, out const_term, pp[0], pp[1], pp[2]);
            // 形状関数の微分の係数を求める
            //    dndxC[ino,n,k]  ino節点のn方向微分のLk(k面積座標)の係数
            //       dNino/dn = dndxC[ino, n, 0] * L0 + dndxC[ino, n, 1] * L1 + dndxC[ino, n, 2] * L2 + dndxC[ino, n, 3]
            double[, ,] dndxC = null;
            if (nodeCnt == Constants.TriNodeCnt_FirstOrder)
            {
                dndxC = new double[Constants.TriNodeCnt_FirstOrder, ndim, vertexCnt + 1]
                    {
                        {
                            {0.0, 0.0, 0.0, dldx[0, 0]},
                            {0.0, 0.0, 0.0, dldx[0, 1]},
                        },
                        {
                            {0.0, 0.0, 0.0, dldx[1, 0]},
                            {0.0, 0.0, 0.0, dldx[1, 1]},
                        },
                        {
                            {0.0, 0.0, 0.0, dldx[2, 0]},
                            {0.0, 0.0, 0.0, dldx[2, 1]},
                        },
                    };
            }
            else
            {
                dndxC = new double[Constants.TriNodeCnt_SecondOrder, ndim, vertexCnt + 1]
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
            }
            // 節点の面積座標
            double[][] n_pts = null;
            if (nodeCnt == Constants.TriNodeCnt_FirstOrder)
            {
                n_pts = new double[Constants.TriNodeCnt_FirstOrder][]
                    {
                        new double[vertexCnt] {1.0, 0.0, 0.0},
                        new double[vertexCnt] {0.0, 1.0, 0.0},
                        new double[vertexCnt] {0.0, 0.0, 1.0},
                    };
            }
            else
            {
                n_pts = new double[Constants.TriNodeCnt_SecondOrder][]
                    {
                        new double[vertexCnt] {1.0, 0.0, 0.0},
                        new double[vertexCnt] {0.0, 1.0, 0.0},
                        new double[vertexCnt] {0.0, 0.0, 1.0},
                        new double[vertexCnt] {0.5, 0.5, 0.0},
                        new double[vertexCnt] {0.0, 0.5, 0.5},
                        new double[vertexCnt] {0.5, 0.0, 0.5},
                    };
            }
            for (int ino = 0; ino < nodeCnt; ino++)
            {
                double[] L = n_pts[ino];
                double[] dNdx = new double[nodeCnt];
                double[] dNdy = new double[nodeCnt];
                for (int k = 0; k < nodeCnt; k++)
                {
                    int direction;
                    direction = 0;
                    dNdx[k] = dndxC[k, direction, 0] * L[0] + dndxC[k, direction, 1] * L[1] + dndxC[k, direction, 2] * L[2] + dndxC[k, direction, 3];
                    direction = 1;
                    dNdy[k] = dndxC[k, direction, 0] * L[0] + dndxC[k, direction, 1] * L[1] + dndxC[k, direction, 2] * L[2] + dndxC[k, direction, 3];
                }
                rotXFValues[ino] = new Complex();
                rotYFValues[ino] = new Complex();
                for (int k = 0; k < nodeCnt; k++)
                {
                    // (rot(Ez)x = dEz/dy
                    rotXFValues[ino] += _FValues[k] * dNdy[k];
                    // (rot(Ez)y = - dEz/dx
                    rotYFValues[ino] += - 1.0 * _FValues[k] * dNdx[k];
                }
                // rot(Ez)を磁界の値に変換する
                rotXFValues[ino] *= _FactorForRot / _media_Q[0, 0];
                rotYFValues[ino] *= _FactorForRot / _media_Q[1, 1];
            }
        }

        /// <summary>
        /// フィールド値を描画する
        /// </summary>
        /// <param name="g"></param>
        /// <param name="ofs"></param>
        /// <param name="delta"></param>
        /// <param name="regionSize"></param>
        /// <param name="fieldDv"></param>
        /// <param name="valueDv"></param>
        /// <param name="colorMap"></param>
        public override void DrawField(Graphics g, Size ofs, Size delta, Size regionSize, FemElement.FieldDV fieldDv, FemElement.ValueDV valueDv, ColorMap colorMap)
        {
            //base.DrawField(g, ofs, delta, regionSize, colorMap);
            if (_Nodes == null || _FValues == null || _RotXFValues == null || _RotYFValues == null || _PoyntingXFValues == null || _PoyntingYFValues == null)
            {
                return;
            }
            Complex[] tagtValues = null;
            if (fieldDv == FemElement.FieldDV.Field)
            {
                tagtValues = _FValues;
            }
            else if (fieldDv == FemElement.FieldDV.RotX)
            {
                tagtValues = _RotXFValues;
            }
            else if (fieldDv == FemElement.FieldDV.RotY)
            {
                tagtValues = _RotYFValues;
            }
            else
            {
                return;
            }
            doDrawField(g, ofs, delta, regionSize, tagtValues, valueDv, colorMap);
        }

        /// <summary>
        /// 対象のフィールドを描画する
        /// </summary>
        /// <param name="g"></param>
        /// <param name="ofs"></param>
        /// <param name="delta"></param>
        /// <param name="regionSize"></param>
        /// <param name="tagtValues"></param>
        /// <param name="valueDv"></param>
        /// <param name="colorMap"></param>
        public void doDrawField(Graphics g, Size ofs, Size delta, Size regionSize, Complex[] tagtValues, FemElement.ValueDV valueDv, ColorMap colorMap)
        {

            const int ndim = Constants.CoordDim2D; //2;      // 座標の次元数
            const int vertexCnt = Constants.TriVertexCnt; //3; // 三角形の頂点の数(2次要素でも同じ)
            //const int nodeCnt = Constants.TriNodeCnt_SecondOrder; //6;  // 三角形2次要素
            int nodeCnt = NodeNumbers.Length;
            if (nodeCnt != Constants.TriNodeCnt_SecondOrder && nodeCnt != Constants.TriNodeCnt_FirstOrder)
            {
                return;
            }
            // 三角形の節点座標を取得
            double[][] pp = new double[nodeCnt][];
            for (int ino = 0; ino < pp.GetLength(0); ino++)
            {
                FemNode node = _Nodes[ino];
                System.Diagnostics.Debug.Assert(node.Coord.Length == ndim);
                pp[ino] = new double[ndim];
                pp[ino][0] = node.Coord[0] * delta.Width + ofs.Width;
                //pp[ino][1] = regionSize.Height - node.Coord[1] * delta.Height + ofs.Height;
                pp[ino][1] = node.Coord[1] * delta.Height + ofs.Height;
            }

            // 長方形描画領域のリスト
            IList<double[][]> rectLiList = _RectLiList;
            // 描画ロジック上の原点となる頂点
            int orginVertexNo = _OrginVertexNo;

            // 四角形の頂点
            const int rectVCnt = 4;
            foreach (double[][] rectLi in rectLiList)
            {
                double[][] rectpp = new double[rectVCnt][];
                for (int ino = 0; ino < rectVCnt; ino++)
                {
                    double[] vLpp = rectLi[ino];
                    double xx = 0.0;
                    double yy = 0.0;
                    for (int k = 0; k < vertexCnt; k++)
                    {
                        xx += pp[k][0] * vLpp[(k + orginVertexNo) % vertexCnt];
                        yy += pp[k][1] * vLpp[(k + orginVertexNo) % vertexCnt];
                    }
                    rectpp[ino] = new double[] { xx, yy };
                }
                // 表示する位置
                double[] vLi = new double[] { (rectLi[0][0] + rectLi[1][0]) * 0.5, (rectLi[0][1] + rectLi[3][1]) * 0.5, 0 };
                if (vLi[0] < 0.0)
                {
                    vLi[0] = 0.0;
                }
                if (vLi[0] > 1.0)
                {
                    vLi[0] = 1.0;
                }
                if (vLi[1] < 0.0)
                {
                    vLi[1] = 0.0;
                }
                if (vLi[1] > (1.0 - vLi[0]))
                {
                    vLi[1] = (1.0 - vLi[0]);
                }
                vLi[2] = 1.0 - vLi[0] - vLi[1];
                if (vLi[2] < 0.0)
                {
                    System.Diagnostics.Debug.WriteLine("logic error vLi[2] = {0}", vLi[2]);
                }
                // 表示する値
                Complex cvalue = new Complex(0.0, 0.0);
                // 表示する位置の形状関数値
                double[] vNi = null;
                double[] shiftedLi = new double[vertexCnt];
                for (int i = 0; i < vertexCnt; i++)
                {
                    shiftedLi[i] = vLi[(i + orginVertexNo) % vertexCnt];
                }
                if (nodeCnt == Constants.TriNodeCnt_FirstOrder)
                {
                    vNi = new double[]
                            {
                                shiftedLi[0],
                                shiftedLi[1],
                                shiftedLi[2]
                            };
                }
                else
                {
                    vNi = new double[]
                            {
                                shiftedLi[0] * (2.0 * shiftedLi[0] - 1.0),
                                shiftedLi[1] * (2.0 * shiftedLi[1] - 1.0),
                                shiftedLi[2] * (2.0 * shiftedLi[2] - 1.0),
                                4.0 * shiftedLi[0] * shiftedLi[1],
                                4.0 * shiftedLi[1] * shiftedLi[2],
                                4.0 * shiftedLi[2] * shiftedLi[0],
                            };
                }
                for (int k = 0; k < nodeCnt; k++)
                {
                    cvalue += tagtValues[k] * vNi[k];
                }
                // 四角形の頂点（描画用）
                Point[] rectp = new Point[rectVCnt];
                for (int ino = 0; ino < rectVCnt; ino++)
                {
                    rectp[ino] = new Point((int)rectpp[ino][0], (int)rectpp[ino][1]);
                }
                try
                {
                    // 表示する値
                    double showValue = 0.0;
                    if (valueDv == ValueDV.Real)
                    {
                        showValue = cvalue.Real;
                    }
                    else if (valueDv == ValueDV.Imaginary)
                    {
                        showValue = cvalue.Imaginary;
                    }
                    else
                    {
                        // 既定値は絶対値
                        showValue = Complex.Abs(cvalue);
                    }
                    // 塗りつぶし色の取得
                    Color fillColor = colorMap.GetColor(showValue);
                    // 塗りつぶし
                    using (Brush brush = new SolidBrush(fillColor))
                    {
                        g.FillPolygon(brush, rectp);
                    }
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
                }
            }
        }

        /// <summary>
        /// 描画領域をセットアップ
        /// </summary>
        /// <param name="out_rectLiList"></param>
        private void setupDrawRect(out IList<double[][]> out_rectLiList, out int orginVertexNo)
        {
            orginVertexNo = 2;
            out_rectLiList = new List<double[][]>();

            if (_Nodes == null)
            {
                return;
            }
            const int ndim = Constants.CoordDim2D; //2;      // 座標の次元数
            const int vertexCnt = Constants.TriVertexCnt; //3; // 三角形の頂点の数(2次要素でも同じ)
            //const int nodeCnt = Constants.TriNodeCnt_SecondOrder; //6;  // 三角形2次要素
            int nodeCnt = NodeNumbers.Length;
            if (nodeCnt != Constants.TriNodeCnt_SecondOrder && nodeCnt != Constants.TriNodeCnt_FirstOrder)
            {
                return;
            }

            // 三角形の節点座標を取得
            double[][] pp = new double[nodeCnt][];
            for (int ino = 0; ino < pp.GetLength(0); ino++)
            {
                FemNode node = _Nodes[ino];
                System.Diagnostics.Debug.Assert(node.Coord.Length == ndim);
                pp[ino] = new double[ndim];
                // 取りあえず規格化した値で計算する
                pp[ino][0] = node.Coord[0];
                pp[ino][1] = - node.Coord[1];  // Y方向は逆にする
            }

            // 下記分割ロジックの原点となる頂点
            //   頂点0固定で計算していたが、原点の内角が直角のとき長方形メッシュになるので原点を2（頂点を0,1,2としたとき）にする
            //int orginVertexNo = 2;
            // 内角が最大の頂点を取得し、その頂点を原点とする(後のロジックは原点が頂点を0,1,2としたとき、2になっている
            {
                double minCosth = double.MaxValue;
                int minCosthVertexNo = 0;
                for (int ino = 0; ino < vertexCnt; ino++)
                {
                    const int vecCnt = 2;
                    double[][] vec = new double[vecCnt][] { new double[ndim] { 0, 0 }, new double[ndim] { 0, 0 } };
                    double[] len = new double[vecCnt];
                    double costh;
                    {
                        int n1 = ino;
                        int n2 = (ino + 1) % 3;
                        int n3 = (ino + 2) % 3;
                        vec[0][0] = pp[n2][0] - pp[n1][0];
                        vec[0][1] = pp[n2][1] - pp[n1][1];
                        vec[1][0] = pp[n3][0] - pp[n1][0];
                        vec[1][1] = pp[n3][1] - pp[n1][1];
                        len[0] = FemMeshLogic.GetDistance(pp[n1], pp[n2]);
                        len[1] = FemMeshLogic.GetDistance(pp[n1], pp[n3]);
                        costh = (vec[0][0] * vec[1][0] + vec[0][1] * vec[1][1]) / (len[0] * len[1]);
                        if (costh < minCosth)
                        {
                            minCosth = costh;
                            minCosthVertexNo = ino;
                        }
                    }
                }
                orginVertexNo = (minCosthVertexNo + 2) % 3;
            }
            // 三角形内部を四角形で分割
            // 面積座標L1方向分割数
            //int ndiv = 4;
            //int ndiv = this.IsCoarseFieldMesh ? (Constants.TriDrawFieldMshDivCnt / 2) : Constants.TriDrawFieldMshDivCnt;
            int ndiv = this.IsCoarseFieldMesh ? 1 : Constants.TriDrawFieldMshDivCnt;
            double defdL1 = 1.0 / (double)ndiv;
            double defdL2 = defdL1;
            for (int i1 = 0; i1 < ndiv; i1++)
            {
                double vL1 = i1 * defdL1;
                double vL1Next = (i1 + 1) * defdL1;
                if (i1 == ndiv - 1)
                {
                    vL1Next = 1.0;
                }
                double vL2max = 1.0 - vL1;
                if (vL2max < 0.0)
                {
                    // ERROR
                    System.Diagnostics.Debug.WriteLine("logic error vL2max = {0}", vL2max);
                    continue;
                }
                double fdiv2 = (double)ndiv * vL2max;
                int ndiv2 = (int)fdiv2;
                if (fdiv2 - (double)ndiv2 > Constants.PrecisionLowerLimit)
                {
                    ndiv2++;
                }
                for (int i2 = 0; i2 < ndiv2; i2++)
                {
                    double vL2 = i2 * defdL2;
                    double vL2Next = (i2 + 1) * defdL2;
                    if (i2 == ndiv2 - 1)
                    {
                        vL2Next = vL2max;
                    }
                    double vL3 = 1.0 - vL1 - vL2;
                    if (vL3 < 0.0)
                    {
                        // ERROR
                        System.Diagnostics.Debug.WriteLine("logic error vL3 = {0}", vL3);
                        continue;
                    }

                    // 四角形の頂点
                    const int rectVCnt = 4;
                    double[][] rectLi = new double[rectVCnt][]
                    {
                        new double[]{vL1    , vL2    , 0},
                        new double[]{vL1Next, vL2    , 0},
                        new double[]{vL1Next, vL2Next, 0},
                        new double[]{vL1    , vL2Next, 0}
                    };
                    if ((i1 == ndiv - 1) || (i2 == ndiv2 - 1))
                    {
                        for (int k = 0; k < 3; k++)
                        {
                            rectLi[2][k] = rectLi[3][k];
                        }
                    }
                    for (int ino = 0; ino < rectVCnt; ino++)
                    {
                        if (rectLi[ino][0] < 0.0)
                        {
                            rectLi[ino][0] = 0.0;
                            System.Diagnostics.Debug.WriteLine("logical error rectLi[{0}][0] = {1}", ino, rectLi[ino][0]);
                        }
                        if (rectLi[ino][0] > 1.0)
                        {
                            rectLi[ino][0] = 1.0;
                            System.Diagnostics.Debug.WriteLine("logical error rectLi[{0}][0] = {1}", ino, rectLi[ino][0]);
                        }
                        if (rectLi[ino][1] < 0.0)
                        {
                            rectLi[ino][1] = 0.0;
                            System.Diagnostics.Debug.WriteLine("logical error rectLi[{0}][1] = {1}", ino, rectLi[ino][1]);
                        }
                        if (rectLi[ino][1] > (1.0 - rectLi[ino][0]))  // L2最大値(1 - L1)チェック
                        {
                            rectLi[ino][1] = 1.0 - rectLi[ino][0];
                        }
                        rectLi[ino][2] = 1.0 - rectLi[ino][0] - rectLi[ino][1];
                        if (rectLi[ino][2] < 0.0)
                        {
                            System.Diagnostics.Debug.WriteLine("logical error rectLi[{0}][2] = {1}", ino, rectLi[ino][2]);
                        }
                    }
                    /*
                    double[][] shiftedRectLi = new double[rectVCnt][];
                    for (int ino = 0; ino < rectVCnt; ino++)
                    {
                        shiftedRectLi[ino] = new double[vertexCnt];
                        for (int k = 0; k < vertexCnt; k++)
                        {
                            shiftedRectLi[ino][k] = rectLi[ino][(k + orginVertexNo) % vertexCnt];
                        }
                    }
                    out_rectLiList.Add(shiftedRectLi);
                     */
                    out_rectLiList.Add(rectLi);
                }
            }
        }


        /// <summary>
        /// フィールド値の回転を描画する
        /// </summary>
        /// <param name="g"></param>
        /// <param name="ofs"></param>
        /// <param name="delta"></param>
        /// <param name="regionSize"></param>
        /// <param name="drawColor"></param>
        /// <param name="fieldDv"></param>
        /// <param name="minRotFValue"></param>
        /// <param name="maxRotFValue"></param>
        public override void DrawRotField(Graphics g, Size ofs, Size delta, Size regionSize, Color drawColor, FemElement.FieldDV fieldDv, double minRotFValue, double maxRotFValue)
        {
            if (_Nodes == null || _FValues == null || _RotXFValues == null || _RotYFValues == null || _PoyntingXFValues == null || _PoyntingYFValues == null)
            {
                return;
            }
            Complex[] tagtXValues = null;
            Complex[] tagtYValues = null;
            if (fieldDv == FemElement.FieldDV.PoyntingXY)
            {
                tagtXValues = _PoyntingXFValues;
                tagtYValues = _PoyntingYFValues;
            }
            else if (fieldDv == FemElement.FieldDV.RotXY)
            {
                tagtXValues = _RotXFValues;
                tagtYValues = _RotYFValues;
            }
            else
            {
                return;
            }

            const int ndim = Constants.CoordDim2D; //2;      // 座標の次元数
            const int vertexCnt = Constants.TriVertexCnt; //3; // 三角形の頂点の数(2次要素でも同じ)
            //const int nodeCnt = Constants.TriNodeCnt_SecondOrder; //6;  // 三角形2次要素
            int nodeCnt = NodeNumbers.Length;
            if (nodeCnt != Constants.TriNodeCnt_SecondOrder && nodeCnt != Constants.TriNodeCnt_FirstOrder)
            {
                return;
            }
            // 三角形の節点座標を取得
            double[][] pp = new double[nodeCnt][];
            for (int ino = 0; ino < pp.GetLength(0); ino++)
            {
                FemNode node = _Nodes[ino];
                System.Diagnostics.Debug.Assert(node.Coord.Length == ndim);
                pp[ino] = new double[ndim];
                pp[ino][0] = node.Coord[0] * delta.Width + ofs.Width;
                //pp[ino][1] = regionSize.Height - node.Coord[1] * delta.Height + ofs.Height;
                pp[ino][1] = node.Coord[1] * delta.Height + ofs.Height;
            }

            // 表示する位置の面積座標
            double[] Li = new double[vertexCnt]
                {
                    1.0 / 3.0, 1.0 / 3.0, 1.0 / 3.0
                };
            // 表示する位置の形状関数
            double[] vNi = null;
            if (nodeCnt == Constants.TriNodeCnt_FirstOrder)
            {
                vNi = new double[]
                            {
                                Li[0],
                                Li[1],
                                Li[2]
                            };
            }
            else
            {
                vNi = new double[]
                            {
                                Li[0] * (2.0 * Li[0] - 1.0),
                                Li[1] * (2.0 * Li[1] - 1.0),
                                Li[2] * (2.0 * Li[2] - 1.0),
                                4.0 * Li[0] * Li[1],
                                4.0 * Li[1] * Li[2],
                                4.0 * Li[2] * Li[0],
                            };
            }
            // 表示する位置
            double showPosX = 0;
            double showPosY = 0;
            for (int k = 0; k < nodeCnt; k++)
            {
                showPosX += pp[k][0] * vNi[k];
                showPosY += pp[k][1] * vNi[k];
            }
            Complex cvalueX = new Complex(0, 0);
            Complex cvalueY = new Complex(0, 0);
            for (int k = 0; k < nodeCnt; k++)
            {
                cvalueX += tagtXValues[k] * vNi[k];
                cvalueY += tagtYValues[k] * vNi[k];
            }
            try
            {
                double showScale = ((double)regionSize.Width / DefPanelWidth) * ArrowLength;
                // 実数部のベクトル表示
                int lenX = 0;
                int lenY = 0;
                if (Math.Abs(maxRotFValue) >= Constants.PrecisionLowerLimit)
                {
                    lenX = (int)((double)(cvalueX.Real / maxRotFValue) * showScale);
                    lenY = (int)((double)(cvalueY.Real / maxRotFValue) * showScale);
                }
                if (lenX != 0 || lenY != 0)
                {
                    // Y方向は表示上逆になる
                    lenY = -lenY;
                    using (Pen pen = new Pen(drawColor, 1))
                    {
                        //pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                        //pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                        pen.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;
                        //pen.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(3, 3, false); // 重い
                        g.DrawLine(pen, (int)showPosX, (int)showPosY, (int)(showPosX + lenX), (int)(showPosY + lenY));
                    }
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
            }
        }

        /// <summary>
        /// 周期構造導波路の固有モード分布を描画する
        /// </summary>
        /// <param name="g"></param>
        /// <param name="ofs"></param>
        /// <param name="delta"></param>
        /// <param name="regionSize"></param>
        /// <param name="valueDv"></param>
        /// <param name="colorMap"></param>
        public override void DrawEigenField(Graphics g, Size ofs, Size delta, Size regionSize, FemElement.ValueDV valueDv, ColorMap colorMap)
        {
            Complex[] tagtValues = _EigenFValues;
            if (tagtValues != null)
            {
                doDrawField(g, ofs, delta, regionSize, tagtValues, valueDv, colorMap);
            }
        }

    }
}
