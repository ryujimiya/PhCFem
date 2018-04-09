using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace PhCFem
{
    /// <summary>
    /// 境界リスト
    /// </summary>
    class Edge : IComparable<Edge>
    {
        /// <summary>
        /// 辺の集合の番号
        /// </summary>
        public int No
        {
            get;
            set;
        }
        /// <summary>
        /// 辺の長さ
        ///    X方向境界の場合 Delta.Heigthは0 (new Size(1,0)を指定)
        ///    Y方向境界の場合 Delta,Widthは0 (new Size(0, 1)を指定)
        /// </summary>
        public Size Delta
        {
            get;
            private set;
        }
        /// <summary>
        /// 辺の始点、終点
        /// </summary>
        public Point[] Points
        {
            get;
            private set;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="delta">辺の方向を表す差分サイズ new Size(1,0): X方向 new Size(0, 1): Y方向</param>
        public Edge(Size delta)
        {
            No = 0;
            Delta = delta;
            Points = null;
        }

        /// <summary>
        /// コピー
        /// </summary>
        /// <param name="src"></param>
        public void CP(Edge src)
        {
            No = src.No;
            Delta = src.Delta;
            if (src.Points != null)
            {
                Points = new Point[src.Points.Length];
                for (int i = 0; i < src.Points.Length; i++)
                {
                    Points[i] = new Point(src.Points[i].X, src.Points[i].Y);
                }
            }
            else
            {
                Points = null;
            }
        }

        /// <summary>
        /// 辺番号比較
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Edge other)
        {
            int diff = this.No - other.No;
            return diff;
        }

        /// <summary>
        /// 空?
        /// 辺の始点、終点の指定がない場合空とする
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            return (Points == null);
        }

        /// <summary>
        /// 始点、終点の指定
        /// </summary>
        /// <param name="stp"></param>
        /// <param name="edp"></param>
        public void Set(Point stp, Point edp)
        {
            if (stp.X == edp.X && stp.Y == edp.Y)
            {
                Points = null;
            }
            else
            {
                if (Delta.Height == 0)
                {
                    System.Diagnostics.Debug.Assert(stp.Y == edp.Y);
                }
                else if (Delta.Width == 0)
                {
                    System.Diagnostics.Debug.Assert(stp.X == edp.X);
                }
                else
                {
                    MessageBox.Show("Not implemented");
                }

                if (Points == null)
                {
                    Points = new Point[2];
                }
                Points[0] = stp;
                Points[1] = edp;
            }
        }

        /// <summary>
        /// 辺をなす線分の始点、終点をすべて取得する
        /// </summary>
        /// <returns></returns>
        public IList<Point> GetAllPoints()
        {
            IList<Point> allPoints = new List<Point>();
            if (IsEmpty())
            {
                return allPoints;
            }
            if (Delta.Height == 0)
            {
                int y = Points[0].Y;
                int minX = Points[0].X;
                int maxX = Points[1].X;
                for (int x = minX; x < maxX + Delta.Width; x += Delta.Width)
                {
                    allPoints.Add(new Point(x, y));
                }
            }
            else if (Delta.Width == 0)
            {
                int x = Points[0].X;
                int minY = Points[0].Y;
                int maxY = Points[1].Y;
                for (int y = minY; y < maxY + Delta.Height; y += Delta.Height)
                {
                    allPoints.Add(new Point(x, y));
                }
            }
            else
            {
                MessageBox.Show("Not implemented");
            }
            return allPoints;
        }

        /// <summary>
        /// 指定された点のヒットテスト
        /// ※始点を含む、終点は含まない
        /// <param name="p">チェックするポイント</param>
        /// <param name="delta">辺方向を表すサイズ new Size(1,0) か new Size(0, 1)</param>
        /// </summary>
        public bool HitTest(Point p, Size delta)
        {
            if (IsEmpty())
            {
                return false;
            }
            if (!delta.Equals(Delta))
            {
                return false;
            }
            bool isHit = false;
            Point stp = Points[0];
            Point edp = Points[1];
            if (Delta.Height == 0)
            {
                isHit = (p.Y == stp.Y && (p.X >= stp.X && p.X < edp.X));
            }
            else if (Delta.Width == 0)
            {
                isHit = (p.X == stp.X && (p.Y >= stp.Y && p.Y < edp.Y));
            }
            else
            {
                MessageBox.Show("Not implemented");
            }
            return isHit;
        }

        /// <summary>
        /// 指定された点が含まれるかチェックする
        /// ※始点、終点を含む
        /// <param name="p">チェックするポイント</param>
        /// </summary>
        public bool ContainsPoint(Point p)
        {
            if (IsEmpty())
            {
                return false;
            }
            bool isHit = false;
            Point stp = Points[0];
            Point edp = Points[1];
            if (Delta.Height == 0)
            {
                isHit = (p.Y == stp.Y && (p.X >= stp.X && p.X <= edp.X));  // 終点を含む
            }
            else if (Delta.Width == 0)
            {
                isHit = (p.X == stp.X && (p.Y >= stp.Y && p.Y <= edp.Y));  // 終点を含む
            }
            else
            {
                MessageBox.Show("Not implemented");
            }
            return isHit;
        }

        /// <summary>
        /// 指定された辺が含まれるかチェックする
        /// ※隣接する場合を含む
        /// <param name="chkEdge">辺</param>
        /// </summary>
        public bool ContainsEdge(Edge chkEdge)
        {
            if (Delta != chkEdge.Delta)
            {
                // 辺方向が異なる場合何もしない
                return false;
            }
            if (IsEmpty() || chkEdge.IsEmpty())
            {
                return false;
            }
            bool isHit = false;
            IList<Point> chkPoints = chkEdge.GetAllPoints();
            foreach (Point chkp in chkPoints)
            {
                if (this.ContainsPoint(chkp))
                {
                    isHit = true;
                    break;
                }
            }
            return isHit;
        }
        /// <summary>
        /// 辺に指定された辺を追加する(始点または終点を延長する)
        /// <param name="tgtEdge">辺</param>
        /// </summary>
        public void MergeEdge(Edge tgtEdge)
        {
            if (Delta != tgtEdge.Delta)
            {
                // 辺方向が異なる場合何もしない
                return;
            }

            if (IsEmpty() || tgtEdge.IsEmpty())
            {
                return;
            }
            if (Delta.Height == 0)
            {
                int y = Points[0].Y;
                int minX = (Points[0].X < tgtEdge.Points[0].X) ? Points[0].X : tgtEdge.Points[0].X;
                int maxX = (Points[1].X > tgtEdge.Points[1].X) ? Points[1].X : tgtEdge.Points[1].X;

                Points[0] = new Point(minX, y);
                Points[1] = new Point(maxX, y);
            }
            else if (Delta.Width == 0)
            {
                int x = Points[0].X;
                int minY = (Points[0].Y < tgtEdge.Points[0].Y) ? Points[0].Y : tgtEdge.Points[0].Y;
                int maxY = (Points[1].Y > tgtEdge.Points[1].Y) ? Points[1].Y : tgtEdge.Points[1].Y;
                Points[0] = new Point(x, minY);
                Points[1] = new Point(x, maxY);
            }
            else
            {
                MessageBox.Show("Not implemented");
            }
        }
        /// <summary>
        /// 辺を指定された辺で分割する
        /// <param name="delimiterEdge">区切り辺</param>
        /// </summary>
        public IList<Edge> SplitEdge(Edge delimiterEdge)
        {
            IList<Edge> splitEdges = new List<Edge>();
            Edge newEdge = null;

            if (Delta != delimiterEdge.Delta)
            {
                // 辺方向が異なる場合何もしない
                return splitEdges;
            }

            if (IsEmpty() || delimiterEdge.IsEmpty())
            {
                newEdge = new Edge(Delta);
                newEdge.No = No;
                newEdge.Set(Points[0], Points[1]);
                splitEdges.Add(newEdge);
                return splitEdges;
            }
            if (Delta.Height == 0)
            {
                int y = Points[0].Y;
                int stx = Points[0].X;
                int edx = Points[1].X;
                int stx2 = delimiterEdge.Points[0].X;
                int edx2 = delimiterEdge.Points[1].X;
                if (stx >= edx2 || edx <= stx2)
                {
                    // 重なりなし
                    newEdge = new Edge(Delta);
                    newEdge.No = No;
                    newEdge.Set(Points[0], Points[1]);
                    splitEdges.Add(newEdge);
                }
                else
                {
                    if (stx >= stx2)
                    {
                        int stx3 = edx2;
                        int edx3 = edx;
                        if (stx3 >= edx)
                        {
                            stx3 = edx;
                        }
                        // 既存の辺の後半部分のみ残る
                        newEdge = new Edge(Delta);
                        newEdge.No = No;
                        newEdge.Set(new Point(stx3, y), new Point(edx3, y));
                        splitEdges.Add(newEdge);
                    }
                    else if (stx < stx2 && edx > stx2)
                    {
                        int stx3 = stx;
                        int edx3 = stx2;
                        // 既存の辺の前半部分
                        newEdge = new Edge(Delta);
                        newEdge.No = No;
                        newEdge.Set(new Point(stx3, y), new Point(edx3, y));
                        splitEdges.Add(newEdge);

                        if (edx > edx2)
                        {
                            // 新たに分割された後半部分
                            stx3 = edx2;
                            edx3 = edx;
                            newEdge = new Edge(Delta);
                            newEdge.No = 0;  // 新規
                            newEdge.Set(new Point(stx3, y), new Point(edx3, y));
                            splitEdges.Add(newEdge);
                        }
                    }
                    else
                    {
                        // 重なりなし
                        newEdge = new Edge(Delta);
                        newEdge.No = No;
                        newEdge.Set(Points[0], Points[1]);
                        splitEdges.Add(newEdge);
                    }
                }
            }
            else if (Delta.Width == 0)
            {
                int x = Points[0].X;
                int sty = Points[0].Y;
                int edy = Points[1].Y;
                int sty2 = delimiterEdge.Points[0].Y;
                int edy2 = delimiterEdge.Points[1].Y;
                if (sty >= edy2 || edy <= sty2)
                {
                    // 重なりなし
                    newEdge = new Edge(Delta);
                    newEdge.No = No;
                    newEdge.Set(Points[0], Points[1]);
                    splitEdges.Add(newEdge);
                }
                else
                {
                    if (sty >= sty2)
                    {
                        int sty3 = edy2;
                        int edy3 = edy;
                        if (sty3 >= edy)
                        {
                            sty3 = edy;
                        }
                        // 既存の辺の後半部分のみ残る
                        newEdge = new Edge(Delta);
                        newEdge.No = No;
                        newEdge.Set(new Point(x, sty3), new Point(x, edy3));
                        splitEdges.Add(newEdge);
                    }
                    else if (sty < sty2 && edy > sty2)
                    {
                        int sty3 = sty;
                        int edy3 = sty2;
                        // 既存の辺の前半部分
                        newEdge = new Edge(Delta);
                        newEdge.No = No;
                        newEdge.Set(new Point(x, sty3), new Point(x, edy3));
                        splitEdges.Add(newEdge);

                        if (edy > edy2)
                        {
                            // 新たに分割された後半部分
                            sty3 = edy2;
                            edy3 = edy;
                            newEdge = new Edge(Delta);
                            newEdge.No = 0;  // 新規
                            newEdge.Set(new Point(x, sty3), new Point(x, edy3));
                            splitEdges.Add(newEdge);
                        }
                    }
                    else
                    {
                        // 重なりなし
                        newEdge = new Edge(Delta);
                        newEdge.No = No;
                        newEdge.Set(Points[0], Points[1]);
                        splitEdges.Add(newEdge);
                    }
                }
            }
            else
            {
                MessageBox.Show("Not implemented");
            }
            return splitEdges;
        }
    }  // class Edge
}
