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
    /// Fem要素クラス
    /// </summary>
    class FemElement
    {
        /////////////////////////////////////////////////////////////////////////
        // 型
        /////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///  値の区分
        /// </summary>
        public enum ValueDV { Abs, Real, Imaginary };
        /// <summary>
        /// フィールド区分
        /// </summary>
        public enum FieldDV { Field, RotX, RotY, RotXY, PoyntingXY };

        /////////////////////////////////////////////////////////////////////////
        // 定数
        /////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// ベクトル表示の矢印の長さ
        /// </summary>
        public const double ArrowLength = 40.0;
        /// <summary>
        /// 描画パネルの基準幅
        /// </summary>
        public const double DefPanelWidth = 300.0;

        /////////////////////////////////////////////////////////////////////////
        // フィールド
        /////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 要素番号
        /// </summary>
        public int No;
        /// <summary>
        /// 要素節点1-3の全体節点番号
        /// </summary>
        public int[] NodeNumbers;
        /// <summary>
        /// 媒質インデックス
        /// </summary>
        public int MediaIndex;
        /// <summary>
        /// 線の色
        /// </summary>
        public Color LineColor;
        /// <summary>
        /// 背景色
        /// </summary>
        public Color BackColor;
        /// <summary>
        /// 節点(クラス内部で使用)
        /// </summary>
        protected FemNode[] _Nodes;
        /// <summary>
        /// フィールド値(クラス内部で使用)
        /// </summary>
        protected Complex[] _FValues;
        /// <summary>
        /// フィールド値の回転のX成分(クラス内部で使用)
        /// </summary>
        protected Complex[] _RotXFValues;
        /// <summary>
        /// フィールド値の回転のY成分(クラス内部で使用)
        /// </summary>
        protected Complex[] _RotYFValues;
        /// 複素ポインティングベクトルのX成分(クラス内部で使用)
        /// </summary>
        protected Complex[] _PoyntingXFValues;
        /// <summary>
        /// 複素ポインティングベクトルのY成分(クラス内部で使用)
        /// </summary>
        protected Complex[] _PoyntingYFValues;
        /// <summary>
        /// 回転に掛ける因子(磁界または電界への変換)
        /// </summary>
        protected Complex _FactorForRot = 1.0;
        /// <summary>
        /// 固有モード界分布
        /// </summary>
        protected Complex[] _EigenFValues;

        /// <summary>
        /// 比透磁率or比誘電率
        /// </summary>
        protected double[,] _media_Q = new double[,]
            {
                {1.0, 0.0, 0.0},
                {0.0, 1.0, 0.0},
                {0.0, 0.0, 1.0},
            };
        /// <summary>
        /// 波のモード区分
        /// </summary>
        protected FemSolver.WaveModeDV _WaveModeDv = FemSolver.WaveModeDV.TE;

        /// <summary>
        /// フィールド値描画を荒くする？
        /// </summary>
        public virtual bool IsCoarseFieldMesh
        {
            set;
            get;
        }

        /// <summary>
        ///  フィールドの値(Z成分)
        /// </summary>
        /// <param name="nodeIndex"></param>
        /// <returns></returns>
        public Complex getFValue(int nodeIndex)
        {
            System.Diagnostics.Debug.Assert(_FValues != null);
            return _FValues[nodeIndex];
        }
        /// <summary>
        ///  回転のX成分
        /// </summary>
        /// <param name="nodeIndex"></param>
        /// <returns></returns>
        public Complex getRotXFValue(int nodeIndex)
        {
            System.Diagnostics.Debug.Assert(_RotXFValues != null);
            return _RotXFValues[nodeIndex];
        }
        /// <summary>
        /// 回転のY成分
        /// </summary>
        /// <param name="nodeIndex"></param>
        /// <returns></returns>
        public Complex getRotYFValue(int nodeIndex)
        {
            System.Diagnostics.Debug.Assert(_RotYFValues != null);
            return _RotYFValues[nodeIndex];
        }
        /// <summary>
        ///  複素ポインティングベクトルのX成分
        /// </summary>
        /// <param name="nodeIndex"></param>
        /// <returns></returns>
        public Complex getPoyntingXFValue(int nodeIndex)
        {
            System.Diagnostics.Debug.Assert(_PoyntingXFValues != null);
            return _PoyntingXFValues[nodeIndex];
        }
        /// <summary>
        /// 複素ポインティングベクトルのY成分
        /// </summary>
        /// <param name="nodeIndex"></param>
        /// <returns></returns>
        public Complex getPoyntingYFValue(int nodeIndex)
        {
            System.Diagnostics.Debug.Assert(_PoyntingYFValues != null);
            return _PoyntingYFValues[nodeIndex];
        }

        /// <summary>
        /// 固有モード分布の値
        /// </summary>
        /// <param name="nodeIndex"></param>
        /// <returns></returns>
        public Complex getEigenFValue(int nodeIndex)
        {
            if (_EigenFValues == null)
            {
                return new Complex();
            }
            return _EigenFValues[nodeIndex];
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FemElement()
        {
            No = 0;
            NodeNumbers = null;
            MediaIndex = 0;
            LineColor = Color.Black;
            BackColor = Color.White;
            _Nodes = null;
            _FValues = null;
            _RotXFValues = null;
            _RotYFValues = null;
            _PoyntingXFValues = null;
            _PoyntingYFValues = null;
            _FactorForRot = 1.0;
            _EigenFValues = null;
            _media_Q = new double[,]
                {
                    {1.0, 0.0, 0.0},
                    {0.0, 1.0, 0.0},
                    {0.0, 0.0, 1.0},
                };
            _WaveModeDv = FemSolver.WaveModeDV.TE;
            IsCoarseFieldMesh = false;
        }

        /// <summary>
        /// コピー
        /// </summary>
        /// <param name="src"></param>
        public virtual void CP(FemElement src)
        {
            if (src == this)
            {
                return;
            }
            No = src.No;
            NodeNumbers = null;
            if (src.NodeNumbers != null)
            {
                NodeNumbers = new int[src.NodeNumbers.Length];
                for (int i = 0; i < src.NodeNumbers.Length; i++)
                {
                    NodeNumbers[i] = src.NodeNumbers[i];
                }
            }
            MediaIndex = src.MediaIndex;
            LineColor = src.LineColor;
            BackColor = src.BackColor;

            // 内部使用のフィールドはコピーしない
            _Nodes = null;
            _FValues = null;
            _RotXFValues = null;
            _RotYFValues = null;
            _PoyntingXFValues = null;
            _PoyntingYFValues = null;
            _FactorForRot = 1.0;
            _EigenFValues = null;
            _media_Q = new double[,]
                {
                    {1.0, 0.0, 0.0},
                    {0.0, 1.0, 0.0},
                    {0.0, 0.0, 1.0},
                };
            _WaveModeDv = FemSolver.WaveModeDV.TE;
            IsCoarseFieldMesh = false;
        }

        /// <summary>
        /// 節点情報をセットする
        /// </summary>
        /// <param name="nodes">節点情報配列（強制境界を含む全節点を節点番号順に格納した配列)</param>
        public virtual void SetNodesFromAllNodes(FemNode[] nodes)
        {
            _Nodes = new FemNode[NodeNumbers.Length];
            for (int i = 0; i < NodeNumbers.Length; i++)
            {
                int nodeNumber = NodeNumbers[i];
                _Nodes[i] = nodes[nodeNumber - 1];
                System.Diagnostics.Debug.Assert(nodeNumber == _Nodes[i].No);
            }
        }

        /// <summary>
        /// フィールド値をセットする
        /// </summary>
        /// <param name="valuesAll"></param>
        /// <param name="nodesRegionToIndex"></param>
        /// <param name="factorForRot">回転に掛ける因子(磁界または電界への変換)</param>
        public virtual void SetFieldValueFromAllValues(Complex[] valuesAll, Dictionary<int, int> nodesRegionToIndex,
            Complex factorForRot, double[,] media_Q, FemSolver.WaveModeDV waveModeDv)
        {
            _FValues = new Complex[NodeNumbers.Length];
            for (int ino = 0; ino < NodeNumbers.Length; ino++)
            {
                int nodeNumber = NodeNumbers[ino];
                if (nodesRegionToIndex.ContainsKey(nodeNumber))
                {
                    int nodeIndex = nodesRegionToIndex[nodeNumber];
                    //_FValues[ino] = valuesAll[nodeIndex];
                    _FValues[ino].Real = valuesAll[nodeIndex].Real;
                    _FValues[ino].Imaginary = valuesAll[nodeIndex].Imaginary;
                }
                else
                {
                    // 強制境界とみなす
                    //_FValues[ino] = new Complex();
                }
            }
            _FactorForRot = factorForRot;
            for (int i = 0; i < _media_Q.GetLength(0); i++)
            {
                for (int j = 0; j < _media_Q.GetLength(1); j++)
                {
                    _media_Q[i, j] = media_Q[i, j];
                }
            }
            _WaveModeDv = waveModeDv;

            // フィールドの回転を求める
            calcRotField(out _RotXFValues, out _RotYFValues);

            // 複素共役を格納
            //if (_RotXFValues != null && _RotYFValues != null)
            //{
            //    int nno = NodeNumbers.Length;
            //    for (int ino = 0; ino < nno; ino++)
            //    {
            //        _RotXFValues[ino] = Complex.Conjugate(_RotXFValues[ino]);
            //        _RotYFValues[ino] = Complex.Conjugate(_RotYFValues[ino]);
            //    }
            //}

            // 回転を計算できたら（実装されていたら)、複素ポインティングベクトルを計算する
            _PoyntingXFValues = null;
            _PoyntingYFValues = null;
            if (_RotXFValues != null && _RotYFValues != null)
            {
                int nno = NodeNumbers.Length;
                _PoyntingXFValues = new Complex[nno];
                _PoyntingYFValues = new Complex[nno];
                for (int ino = 0; ino < nno; ino++)
                {
                    {
                        if (_WaveModeDv == FemSolver.WaveModeDV.TM)
                        {
                            // F:磁界(Z成分)
                            // G:電界(XY成分)
                            // E x H* = rot x (fz)* = { (fz)*(roty), - (fz)* (rotx) }  (rotは_FactorForRotを乗算済み)
                            //_PoyntingXFValues[ino] = -1.0 * Complex.Conjugate(_FValues[ino]) * _RotYFValues[ino];
                            //_PoyntingYFValues[ino] = Complex.Conjugate(_FValues[ino]) * _RotXFValues[ino];
                            _PoyntingXFValues[ino] = -1.0 * _FValues[ino] * Complex.Conjugate(_RotYFValues[ino]);
                            _PoyntingYFValues[ino] = _FValues[ino] * Complex.Conjugate(_RotXFValues[ino]);
                        }
                        else
                        {
                            // F:電界(Z成分)
                            // G:磁界(XY成分)
                            // (E x H*) = (fz x (rot)*) = { - fz(roty)*, fz (rotx)* }  (rotは_FactorForRotを乗算済み)
                            _PoyntingXFValues[ino] = _FValues[ino] * Complex.Conjugate(_RotYFValues[ino]);
                            //_PoyntingYFValues[ino] = -1.0 * _FValues[ino] * Complex.Conjugate(_RotXFValues[ino]);
                            _PoyntingYFValues[ino] = _FValues[ino] * Complex.Conjugate(_RotXFValues[ino]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// フィールドの回転を取得する
        /// </summary>
        /// <param name="rotXFValues"></param>
        /// <param name="rotYFValues"></param>
        protected virtual void calcRotField(out Complex[] rotXFValues, out Complex[] rotYFValues)
        {
            rotXFValues = null;
            rotYFValues = null;
        }

        /// <summary>
        /// 要素境界を描画する
        /// </summary>
        /// <param name="g"></param>
        /// <param name="panel"></param>
        public void Draw(Graphics g, Size ofs, Size delta, Size regionSize, bool backFillFlg = false)
        {
            //const int vertexCnt = Constants.TriVertexCnt; //3; // 三角形の頂点の数(2次要素でも同じ)
            Constants.FemElementShapeDV elemShapeDv;
            int order;
            int vertexCnt;
            FemMeshLogic.GetElementShapeDvAndOrderByElemNodeCnt(this.NodeNumbers.Length, out elemShapeDv, out order, out vertexCnt);

            // 三角形(or 四角形)の頂点を取得
            Point[] points = new Point[vertexCnt];
            for (int ino = 0; ino < vertexCnt; ino++)
            {
                FemNode node = _Nodes[ino];
                System.Diagnostics.Debug.Assert(node.Coord.Length == 2);
                int x = (int)((double)node.Coord[0] * delta.Width);
                //int y = (int)(regionSize.Height - (double)node.Coord[1] * delta.Height);
                int y = (int)((double)node.Coord[1] * delta.Height);
                points[ino] = new Point(x, y) + ofs;
            }
            // 三角形(or 四角形)を描画
            if (backFillFlg)
            {
                // 要素の背景を塗りつぶす
                using (Brush brush = new SolidBrush(BackColor))
                {
                    g.FillPolygon(brush, points);
                }
            }
            using (Pen selectedPen = new Pen(LineColor, 1))
            {
                // 境界線の描画
                //selectedPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                g.DrawPolygon(selectedPen, points);
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
        public virtual void DrawField(Graphics g, Size ofs, Size delta, Size regionSize, FemElement.FieldDV fieldDv, FemElement.ValueDV valueDv, ColorMap colorMap)
        {
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
        public virtual void DrawRotField(Graphics g, Size ofs, Size delta, Size regionSize, Color drawColor, FemElement.FieldDV fieldDv, double minRotFValue, double maxRotFValue)
        {
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        // 周期構造導波路固有モード分布表示用
        public void ClearEigenFieldValue()
        {
            _EigenFValues = null;
        }

        public virtual void SetEigenFieldValueFromAllValues(Complex[] eigenVec, Dictionary<int, int> toNodePeriodic)
        {
            // 周期構造導波路領域の節点かチェック
            for (int ino = 0; ino < NodeNumbers.Length; ino++)
            {
                int nodeNumber = NodeNumbers[ino];
                if (!toNodePeriodic.ContainsKey(nodeNumber))
                {
                    // 1点でも領域内節点でなければ領域内でない
                    return;
                }
            }

            _EigenFValues = new Complex[NodeNumbers.Length];
            for (int ino = 0; ino < NodeNumbers.Length; ino++)
            {
                int nodeNumber = NodeNumbers[ino];
                if (toNodePeriodic.ContainsKey(nodeNumber))
                {
                    int nodeIndex = toNodePeriodic[nodeNumber];
                    _EigenFValues[ino].Real = eigenVec[nodeIndex].Real;
                    _EigenFValues[ino].Imaginary = eigenVec[nodeIndex].Imaginary;
                }
                else
                {
                }
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
        public virtual void DrawEigenField(Graphics g, Size ofs, Size delta, Size regionSize, FemElement.ValueDV valueDv, ColorMap colorMap)
        {
        }
    }
}
