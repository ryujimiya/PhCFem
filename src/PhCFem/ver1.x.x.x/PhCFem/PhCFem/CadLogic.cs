using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace PhCFem
{
    /// <summary>
    /// Cadロジック
    /// </summary>
    class CadLogic : CadLogicBase, IDisposable
    {
        ////////////////////////////////////////////////////////////////////////
        // 型
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 変更通知デリゲート
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="prevCadMode"></param>
        public delegate void ChangeDeleagte(object sender, CadModeType prevCadMode);

        /// <summary>
        /// 領域選択フラグアレイの思い出具象クラス
        /// </summary>
        class CadLogicBaseMemento : MyUtilLib.Memento<CadLogicBase, CadLogicBase>
        {
            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="mementoData"></param>
            /// <param name="target"></param>
            public CadLogicBaseMemento(CadLogicBase mementoData, CadLogicBase target)
            {
                CadLogicBase cadLogicBase = new CadLogicBase();
                cadLogicBase.CopyData(mementoData);
                base.MementoData = cadLogicBase;
                base.Target = target;
            }

            /// <summary>
            /// ターゲットに思い出を反映させる
            /// </summary>
            /// <param name="mementoData"></param>
            public override void SetMemento(CadLogicBase mementoData)
            {
                base.MementoData = mementoData;
                base.Target.CopyData(mementoData);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // 定数
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 元に戻す操作を記憶するスタック数
        /// </summary>
        private const int MaxUndoStackCnt = 100;
        /// <summary>
        /// 編集中対象の描画色
        /// </summary>
        private readonly Color EditingColor = Color.Yellow;

        ////////////////////////////////////////////////////////////////////////
        // フィールド
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 変更通知イベント
        /// </summary>
        public event ChangeDeleagte Change = null;

        /// <summary>
        /// Cadパネル
        /// </summary>
        private Panel CadPanel = null;
        /// <summary>
        /// 方眼線の描画ペン
        /// </summary>
        private Pen GridPen = new Pen(Color.DarkGray, 1);
        /// <summary>
        /// 選択領域塗りつぶしブラシ
        /// </summary>
        private Brush SelectedBrush = (Brush)Brushes.Gray.Clone();
        /// <summary>
        /// 選択線分描画ペン
        /// </summary>
        private Pen SelectedPen = new Pen(Color.Black, 4);
        /// <summary>
        /// 入射ポート線分描画ペン
        /// </summary>
        private Pen IncidentPortPen = new Pen(Color.Cyan, 4);
        /// <summary>
        /// Cad領域サイズ
        /// </summary>
        private Size RegionSize;
        /// <summary>
        /// 方眼1マスのサイズ
        /// </summary>
        private Size Delta;
        /// <summary>
        /// 方眼左上オフセット
        /// </summary>
        private Size Ofs;
        /// <summary>
        /// マウス選択開始ポイント
        /// </summary>
        private Point StartPt;
        /// <summary>
        /// マウス選択終了ポイント
        /// </summary>
        private Point EndPt;
        /// <summary>
        /// ドラッグ中?
        /// </summary>
        private bool DragFlg = false;
        /// <summary>
        /// Cadモード
        /// </summary>
        public CadModeType CadMode
        {
            get { return _CadMode; }
            set 
            {
                CadModeType prevMode = _CadMode;
                if (prevMode != value)
                {
                    if (value == CadModeType.PortNumbering)
                    {
                        // ポート番号振りモードをセットされた場合、番号シーケンスを初期化する
                        PortNumberingSeq = 1;
                    }
                    if (value == CadModeType.Location)
                    {
                        // 領域移動モードをセットされた場合、移動領域をクリアする
                        LocationRect = new Rectangle();
                    }
                    _CadMode = value;
                }
            }
        }
        /// <summary>
        /// ポート番号付与用シーケンス
        /// </summary>
        private int PortNumberingSeq = 1;
        /// <summary>
        /// 選択中のセルタイプ
        /// </summary>
        public CellType SelectedCellType
        {
            get;
            set;
        }
        /// <summary>
        /// 格子1辺の分割数
        /// </summary>
        public int NdivForOneLattice
        {
            get { return _NdivForOneLattice; }
            set
            {
                _NdivForOneLattice = value;
                IsDirty = true;
            }
        }
        /// <summary>
        /// ロッドの半径割合
        /// </summary>
        public double RodRadiusRatio
        {
            get { return _RodRadiusRatio; }
            set
            {
                _RodRadiusRatio = value;
                IsDirty = true;
            }
        }
        /// <summary>
        /// ロッドの円周方向分割数
        /// </summary>
        public int RodCircleDiv
        {
            get { return _RodCircleDiv; }
            set
            {
                _RodCircleDiv = value;
                IsDirty = true;
            }
        }
        /// <summary>
        /// ロッドの半径方向分割数(1でもメッシュサイズが小さければ複数に分割される)
        /// </summary>
        public int RodRadiusDiv
        {
            get { return _RodRadiusDiv; }
            set
            {
                _RodRadiusDiv = value;
                IsDirty = true;
            }
        }

        /// <summary>
        /// 領域移動の移動領域
        /// </summary>
        private Rectangle LocationRect;
        /// <summary>
        /// コマンド管理
        /// </summary>
        private MyUtilLib.CommandManager CmdManager = null;
        /// <summary>
        /// CadロジックデータのMemento
        /// </summary>
        private CadLogicBaseMemento CadLogicBaseMmnt = null;
        /// <summary>
        /// Cad図面が変更された?
        /// </summary>
        private bool _IsDirty = false;
        /// <summary>
        /// Cad図面が変更された?
        /// </summary>
        public bool IsDirty
        {
            get { return _IsDirty; }
            private set { _IsDirty = value; }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public CadLogic(Panel panel)
        {
            CadPanel = panel;

            // 領域を決定する
            SetupRegionSize();
            // コマンド管理インスタンスを生成(Undo/Redo用)
            CmdManager = new MyUtilLib.CommandManager(MaxUndoStackCnt);

            // 初期化処理
            init();
        }

        /// <summary>
        /// 領域を決定する
        /// </summary>
        public void SetupRegionSize()
        {
            double deltaxx = CadPanel.Width / (double)(MaxDiv.Width + 2);
            int deltax = (int)deltaxx;
            double deltayy = CadPanel.Height / (double)(MaxDiv.Height + 2);
            int deltay = (int)deltayy;
            Ofs = new Size(deltax, deltay);
            Delta = new Size(deltax, deltay);
            RegionSize = new Size(Delta.Width * MaxDiv.Width, Delta.Height * MaxDiv.Height);
            //System.Diagnostics.Debug.WriteLine("{0},{1}", RegionSize.Width, RegionSize.Height);
            //System.Diagnostics.Debug.WriteLine("{0},{1}", Delta.Width, Delta.Height);
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected new void init()
        {
            base.init();

            CadMode = CadModeType.None;
            SelectedCellType = DefCellType;
            LocationRect = new Rectangle();
            
            // Memento初期化
            // 現在の状態をMementoに記憶させる
            setMemento();
            // コマンド管理初期化
            CmdManager.Refresh();
            _IsDirty = false;
        }

        /// <summary>
        /// データの初期化
        /// </summary>
        public void InitData()
        {
            init();
        }

        /// <summary>
        /// デストラクタ
        /// </summary>
        ~CadLogic()
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
                GridPen.Dispose();
                SelectedBrush.Dispose();
                SelectedPen.Dispose();
                IncidentPortPen.Dispose();
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
        /// マス目の描画(塗りつぶし)
        /// </summary>
        /// <param name="g"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="brush"></param>
        private void drawArea(Graphics g, int x, int y, CellType cellType, Brush vacumnBrush, Brush rodBrush)
        {
            // 背景を塗りつぶす
            g.FillRectangle(vacumnBrush, new Rectangle(new Point(x * Delta.Width, y * Delta.Height) + Ofs, Delta));
            if (cellType == CellType.Rod)
            {
                double centerX = x * Delta.Width + Ofs.Width + Delta.Width * 0.5;
                double centerY = y * Delta.Height + Ofs.Height + Delta.Height * 0.5;
                double radius = Delta.Height * _RodRadiusRatio;
                // ロッドを描画
                g.FillPie(rodBrush, (float)(centerX - radius), (float)(centerY - radius), (float)(2.0 * radius), (float)(2.0 * radius), 0.0f, 360.0f);
            }
        }

        /// <summary>
        /// 辺の描画
        /// </summary>
        /// <param name="g"></param>
        /// <param name="edge"></param>
        /// <param name="pen"></param>
        private void drawEdge(Graphics g, Edge edge, Pen pen)
        {
            if (edge.IsEmpty())
            {
                return;
            }
            Point[] pp = new Point[2];
            for (int i = 0; i < 2; i++)
            {
                pp[i] = new Point();
                pp[i].X = edge.Points[i].X * Delta.Width;
                pp[i].Y = edge.Points[i].Y * Delta.Height;
            }
            g.DrawLine(pen, pp[0] + Ofs, pp[1] + Ofs);
        }

        /// <summary>
        /// 辺の番号描画
        /// </summary>
        /// <param name="g"></param>
        /// <param name="edge"></param>
        /// <param name="font"></param>
        /// <param name="brush"></param>
        void drawEdgeNoText(Graphics g, Edge edge, Font font)
        {
            if (edge.IsEmpty())
            {
                return;
            }
            int x = edge.Points[0].X;
            int y = edge.Points[0].Y;
            Point pp = new Point();
            pp.X = x * Delta.Width;
            pp.Y = y * Delta.Height;

            Point drawPt = pp + Ofs;
            if (edge.Delta.Width == 0)
            {
                if (x == MaxDiv.Width)
                {
                    drawPt.X -= font.Height;
                }
                if (x >= 1 && x < MaxDiv.Width && y >= 0 && y < MaxDiv.Height && AreaSelection[y, x - 1] != CellType.Empty)
                {
                    drawPt.X -= font.Height;
                }
            }
            else if (edge.Delta.Height == 0)
            {
                if (y == MaxDiv.Height)
                {
                    drawPt.Y -= font.Height;
                }
                if (x >= 0 && x < MaxDiv.Width && y >= 1 && y < MaxDiv.Height && AreaSelection[y - 1, x] != CellType.Empty)
                {
                    drawPt.Y -= font.Height;
                }
            }
            // 描画位置の背景色を媒質インデックスを元に特定する
            Color backColor = Color.White;
            // テキストの色
            //Color textColor = Color.FromArgb(0xff - backColor.R, 0xff - backColor.G, 0xff - backColor.B);//反転
            Color textColor = Color.FromArgb( 0xff & (backColor.R + 0x40), 0xff & (backColor.G + 0x40), 0xff & (backColor.B + 0x40));
            using (Brush brush = new SolidBrush(textColor))
            {
                g.DrawString(string.Format("{0}", edge.No), font, brush, drawPt);
            }
        }

        /// <summary>
        /// 選択領域マス目の描画
        /// </summary>
        /// <param name="g"></param>
        private void DrawAreas(Graphics g)
        {
            System.Diagnostics.Debug.Assert(Medias.Length == 2);
            using(Brush vacumnBrush = new SolidBrush(Medias[0].BackColor))
            using (Brush rodBrush = new SolidBrush(Medias[1].BackColor))
            {
                for (int y = 0; y < MaxDiv.Height; y++)
                {
                    for (int x = 0; x < MaxDiv.Width; x++)
                    {
                        CellType cellType = AreaSelection[y, x];
                        if (cellType != CellType.Empty)
                        {
                            drawArea(g, x, y, cellType, vacumnBrush, rodBrush);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 方眼紙の線の描画
        /// </summary>
        /// <param name="g"></param>
        private void DrawGrid(Graphics g)
        {
            for (int y = 0; y < MaxDiv.Height + 1; y++)
            {
                if (y % 5 == 0 || y == MaxDiv.Height)
                {
                    GridPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                }
                else
                {
                    GridPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                }
                int pty = y * Delta.Height;
                g.DrawLine(GridPen, new Point(0, pty) + Ofs, new Point(RegionSize.Width, pty) + Ofs);
            }
            for (int x = 0; x < MaxDiv.Width + 1; x++)
            {
                if (x % 5 == 0 || x == MaxDiv.Width)
                {
                    GridPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                }
                else
                {
                    GridPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                }
                int ptx = x * Delta.Width;
                g.DrawLine(GridPen, new Point(ptx, 0) + Ofs, new Point(ptx, RegionSize.Height) + Ofs);
            }
        }

        /// <summary>
        /// 辺の描画
        /// </summary>
        /// <param name="g"></param>
        private void DrawEdges(Graphics g)
        {
            using (Font font = new Font("MS UI Gothic", 16, FontStyle.Bold))
            {
                foreach (Edge edge in EdgeList)
                {
                    Pen pen = edge.No == IncidentPortNo ? IncidentPortPen : SelectedPen;
                    drawEdge(g, edge, pen);
                    drawEdgeNoText(g, edge, font);
                }
            }
        }

        /// <summary>
        /// Cadパネル描画イベント処理
        /// </summary>
        /// <param name="g"></param>
        public void CadPanelPaint(Graphics g)
        {
            // 選択領域の描画
            DrawAreas(g);

            // 方眼紙の線の描画
            DrawGrid(g);

            // 辺の描画
            DrawEdges(g);
        }

        /// <summary>
        /// マウスクリックイベント処理
        /// </summary>
        /// <param name="e"></param>
        public void CadPanelMouseClick(MouseEventArgs e)
        {

        }

        /// <summary>
        /// マウスダウンイベント処理
        /// </summary>
        /// <param name="e"></param>
        public void CadPanelMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                DragFlg = true;
                StartPt = new Point(e.X, e.Y) - Ofs;
                EndPt = StartPt;

                if (CadMode == CadModeType.Location && !LocationRect.IsEmpty)
                {
                    if (LocationRect.X <= EndPt.X && LocationRect.X + LocationRect.Width >= EndPt.X
                        && LocationRect.Y <= EndPt.Y && LocationRect.Y + LocationRect.Height >= EndPt.Y)
                    {
                        // 領域移動開始
                        hitTesting();
                    }
                    else
                    {
                        // 移動領域をクリア
                        LocationRect = new Rectangle();
                        hitTesting();
                    }
                }
                else
                {
                    hitTesting();
                }

            }
        }

        /// <summary>
        /// マウス移動イベント処理
        /// </summary>
        /// <param name="e"></param>
        public void CadPanelMouseMove(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                EndPt = new Point(e.X, e.Y) - Ofs;
                if (DragFlg)
                {
                    hitTesting();
                }
            }
        }

        /// <summary>
        /// マウスアップイベント処理
        /// </summary>
        /// <param name="e"></param>
        public void CadPanelMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                CadPanelMouseLeftButtonUp(e);
            }
        }

        /// <summary>
        /// マウス左ボタンアップ
        /// </summary>
        /// <param name="e"></param>
        private void CadPanelMouseLeftButtonUp(MouseEventArgs e)
        {
            EndPt = new Point(e.X, e.Y) - Ofs;
            DragFlg = false;

            bool executed = false;
            Point minPt = new Point();
            Point maxPt = new Point();
            if (StartPt.X <= EndPt.X)
            {
                minPt.X = StartPt.X;
                maxPt.X = EndPt.X;
            }
            else
            {
                minPt.X = EndPt.X;
                maxPt.X = StartPt.X;
            }
            if (StartPt.Y <= EndPt.Y)
            {
                minPt.Y = StartPt.Y;
                maxPt.Y = EndPt.Y;
            }
            else
            {
                minPt.Y = EndPt.Y;
                maxPt.Y = StartPt.Y;
            }

            if (!executed && CadMode == CadModeType.Location)
            {
                bool workexecuted = false;
                // 領域移動処理
                // Note: 渡すポイントはmin, maxでなく実際の開始、終了ポイント
                bool IsSettingLocationRect = LocationRect.IsEmpty;
                workexecuted = doLocation(StartPt, EndPt);
                // 開始、終了位置をクリア
                StartPt = new Point();
                EndPt = new Point();
                if (IsSettingLocationRect)
                {
                    if (LocationRect.IsEmpty)
                    {
                        // パネルの再描画
                        CadPanel.Refresh();
                    }
                    else
                    {
                        // ラバーバンドを描画
                        hitTesting();
                    }

                    // 再描画を抑制
                    executed = false;
                }
                else
                {
                }
            }
            else
            {
                // ヒットテスト終了なので、ヒットテストで描画したものをクリア
                // パネルの再描画
                CadPanel.Refresh();

                if (!executed)
                {
                    executed = doSelectIncidentPort(EndPt);
                }
                if (!executed)
                {
                    executed = doNumberingPort(EndPt);
                }
                if (!executed)
                {
                    executed = doSelectWaveguidePort(minPt, maxPt);
                }
                if (!executed)
                {
                    executed = doSelectDisconArea(minPt, maxPt);
                }
            }

            if (executed)
            {
                invokeCadOperationCmd();
                if (Change != null)
                {
                    Change(this, CadMode);
                }
                // パネルの再描画
                CadPanel.Refresh();
            }
        }

        /// <summary>
        /// 2点を結ぶ線分上にあるエリアの点のリストを取得する
        /// </summary>
        /// <param name="startPt"></param>
        /// <param name="endPt"></param>
        /// <returns></returns>
        private IList<Point> getPointsInLineArea(Point in_startPt, Point in_endPt)
        {
            Point startPt;
            Point endPt;
            startPt = new Point(((int)(in_startPt.X / Delta.Width)) * Delta.Width, ((int)(in_startPt.Y / Delta.Height)) * Delta.Height);
            endPt = new Point(((int)(in_endPt.X / Delta.Width)) * Delta.Width, ((int)(in_endPt.Y / Delta.Height)) * Delta.Height);

            IList<Point> points = new List<Point>();
            if (startPt.X == endPt.X)
            {
                // Y軸に平行な直線
                int x0 = startPt.X;
                int leny = endPt.Y - startPt.Y;
                int minY = leny >= 0? startPt.Y : endPt.Y;
                int maxY = leny >= 0? endPt.Y : startPt.Y;
                for (int y = minY; y <= maxY; y++)
                {
                    // 直線状の点
                    points.Add(new Point(x0, y));
                }
            }
            else if (startPt.Y == endPt.Y)
            {
                // X軸に平行な直線
                int y0 = startPt.Y;
                int lenx = endPt.X - startPt.X;
                int minX = lenx >= 0? startPt.X : endPt.X;
                int maxX = lenx >= 0? endPt.X : startPt.X;
                for (int x = minX; x <= maxX; x++)
                {
                    // 直線状の点
                    points.Add(new Point(x, y0));
                }
            }
            else
            {
                int x0 = startPt.X;
                int y0 = startPt.Y;
                int x1 = endPt.X;
                int y1 = endPt.Y;
                double grad = (y1 - y0) / (double)(x1 - x0);
                int minX = (x0 <= x1)? x0 : x1;
                int maxX = (x0 <= x1)? x1 : x0;
                int prevY =(x0 <= x1)? y0 : y1;
                for (int x = minX; x <= maxX; x++)
                {
                    double curYDouble = (double)grad * (x - x0) + y0;
                    int curY = (int)((curYDouble - y0) / Delta.Height) * Delta.Height + y0;
                    // grad > 1.0のときの補間
                    if (x > minX)
                    {
                        for (int y = prevY; y < curY; y++)
                        {
                            points.Add(new Point(x - 1, y));
                        }
                    }
                    // 直線状の点
                    points.Add(new Point(x, curY));
                    prevY = curY;
                }
            }
            return points;
        }

        /// <summary>
        /// 楕円エリア内の点のリストを取得する
        /// </summary>
        /// <param name="in_startPt"></param>
        /// <param name="in_endPt"></param>
        /// <returns></returns>
        private IList<Point> getPointsInEllipseArea(Point in_startPt, Point in_endPt)
        {
            Point startPt;
            Point endPt;
            // マス目としてヒットするように始点、終点をマス目の中央にする
            startPt = new Point((int)((int)(in_startPt.X / Delta.Width) * Delta.Width + 0.5 * Delta.Width), (int)((int)(in_startPt.Y / Delta.Height) * Delta.Height + 0.5 * Delta.Height));
            endPt = new Point((int)((int)(in_endPt.X / Delta.Width) * Delta.Width + 0.5 * Delta.Width), (int)((int)(in_endPt.Y / Delta.Height) * Delta.Height + 0.5 * Delta.Height));
            Point zeroPt;
            zeroPt = new Point((startPt.X + endPt.X) / 2, (startPt.Y + endPt.Y) / 2);
            int a = Math.Abs(endPt.X - startPt.X) / 2;
            int b = Math.Abs(endPt.Y - startPt.Y) / 2;

            IList<Point> points = new List<Point>();
            if (a == 0)
            {
                int x0 = startPt.X;
                int leny = endPt.Y - startPt.Y;
                int minY = leny >= 0? startPt.Y : endPt.Y;
                int maxY = leny >= 0? endPt.Y : startPt.Y;
                for (int y = minY; y <= maxY; y++)
                {
                    points.Add(new Point(x0, y));
                }
            }
            else if (b == 0)
            {
                int y0 = startPt.Y;
                int lenx = endPt.X - startPt.X;
                int minX = lenx >= 0 ? startPt.X : endPt.X;
                int maxX = lenx >= 0 ? endPt.X : startPt.X;
                for (int x = minX; x <= maxX; x++)
                {
                    points.Add(new Point(x, y0));
                }
            }
            else
            {
                int lenx = endPt.X - startPt.X;
                int minX = lenx >= 0 ? startPt.X : endPt.X;
                int maxX = lenx >= 0 ? endPt.X : startPt.X;
                for (int x = minX; x <= maxX; x++)
                {
                    int maxY = (int)((double)b * Math.Sqrt(1.0 - (x - zeroPt.X) * (x - zeroPt.X) / ((double)a * a)) + zeroPt.Y);
                    int minY = -(maxY - zeroPt.Y) + zeroPt.Y;
                    minY = (int)(((int)(minY / Delta.Height)) * Delta.Height + 0.5 * Delta.Height);
                    maxY = (int)(((int)(maxY / Delta.Height)) * Delta.Height + 0.5 * Delta.Height);
                    for (int y = minY; y <= maxY; y++)
                    {
                        points.Add(new Point(x, y));
                    }
                }
            }
            return points;
        }

        /// <summary>
        /// 領域移動処理
        /// </summary>
        /// <param name="startPt"></param>
        /// <param name="endPt"></param>
        /// <returns></returns>
        private bool doLocation(Point startPt, Point endPt)
        {
            if (CadMode != CadModeType.Location)
            {
                return false;
            }
            bool executed = false;

            if (LocationRect.IsEmpty)
            {
                if (startPt.X != endPt.X || startPt.Y != endPt.Y)
                {
                    // 移動領域を設定する
                    Point minPt = new Point();
                    Point maxPt = new Point();
                    if (startPt.X <= endPt.X)
                    {
                        minPt.X = startPt.X;
                        maxPt.X = endPt.X;
                    }
                    else
                    {
                        minPt.X = endPt.X;
                        maxPt.X = startPt.X;
                    }
                    if (startPt.Y <= endPt.Y)
                    {
                        minPt.Y = startPt.Y;
                        maxPt.Y = endPt.Y;
                    }
                    else
                    {
                        minPt.Y = endPt.Y;
                        maxPt.Y = startPt.Y;
                    }
                    if (minPt.X < 0)
                    {
                        minPt.X = 0;
                    }
                    if (minPt.Y < 0)
                    {
                        minPt.Y = 0;
                    }
                    // BUGFIX : 後でDelta.Widthを足すのでマス目の最大値はMaxから１マス分引いた値
                    if (maxPt.X > (MaxDiv.Width - 1) * Delta.Width)
                    {
                        maxPt.X = (MaxDiv.Width - 1) * Delta.Width;
                    }
                    // BUGFIX : 後でDelta.Heightを足すのでマス目の最大値はMaxから１マス分引いた値
                    if (maxPt.Y > (MaxDiv.Height - 1) * Delta.Height)
                    {
                        maxPt.Y = (MaxDiv.Height - 1) * Delta.Height;
                    }
                    minPt.X = ((int)Math.Floor((double)minPt.X / Delta.Width)) * Delta.Width;
                    minPt.Y = ((int)Math.Floor((double)minPt.Y / Delta.Height)) * Delta.Height;
                    maxPt.X = ((int)Math.Floor((double)maxPt.X / Delta.Width)) * Delta.Width + Delta.Width;
                    maxPt.Y = ((int)Math.Floor((double)maxPt.Y / Delta.Height)) * Delta.Height + Delta.Height;
                    int width = maxPt.X - minPt.X;
                    int height = maxPt.Y - minPt.Y;
                    if (width > 0 && height > 0)
                    {
                        LocationRect = new Rectangle(minPt, new Size(width, height));
                        executed = true;
                    }
                }
                return executed;
            }

            // 領域を退避
            Rectangle saveLocationRect = LocationRect;
            // 新しい領域を設定
            LocationRect.X += (endPt.X - startPt.X);
            LocationRect.Y += (endPt.Y - startPt.Y);
            if (LocationRect.X < 0)
            {
                LocationRect.X = 0;
            }
            if (LocationRect.Y < 0)
            {
                LocationRect.Y = 0;
            }
            // BUGFIX
            if (LocationRect.Right > MaxDiv.Width * Delta.Width)
            {
                LocationRect.X -= (LocationRect.Right - MaxDiv.Width * Delta.Width);
            }
            // BUGFIX
            if (LocationRect.Bottom > MaxDiv.Height * Delta.Height)
            {
                LocationRect.Y -= (LocationRect.Bottom - MaxDiv.Height * Delta.Height);
            }
            
            if (LocationRect.X != saveLocationRect.X || LocationRect.Y != saveLocationRect.Y)
            {
                int saveWidth = saveLocationRect.Width / Delta.Width;
                int saveHeight = saveLocationRect.Height / Delta.Height;
                // 領域、辺の退避
                CellType[,] saveAreaSelection = new CellType[saveHeight, saveWidth];
                IList<Edge> saveEdgeList = new List<Edge>();
                for (int y = 0; y < saveHeight; y++)
                {
                    int saveY = y + saveLocationRect.Top / Delta.Height;
                    for (int x = 0; x < saveWidth; x++)
                    {
                        int saveX = x + saveLocationRect.Left / Delta.Width;
                        // マス目選択情報を退避
                        saveAreaSelection[y, x] = AreaSelection[saveY, saveX];

                        // ヒットする辺を退避
                        int hitIndex;
                        Edge hitEdge;
                        // Y方向境界
                        //YBoundarySelection[y, x]
                        hitEdge = hitTestEdge(new Point(saveX * Delta.Width, (int)((double)(saveY + 0.5) * Delta.Height)), out hitIndex);
                        if (hitEdge != null && !saveEdgeList.Contains(hitEdge))
                        {
                            saveEdgeList.Add(hitEdge);
                        }
                        // YBoundarySelection[y, x + 1]
                        hitEdge = hitTestEdge(new Point((saveX + 1)* Delta.Width, (int)((double)(saveY + 0.5) * Delta.Height)), out hitIndex);
                        if (hitEdge != null && !saveEdgeList.Contains(hitEdge))
                        {
                            saveEdgeList.Add(hitEdge);
                        }
                        // X方向境界
                        // XBoundarySelection[y, x]
                        hitEdge = hitTestEdge(new Point((int)((double)(saveX + 0.5) * Delta.Width), saveY * Delta.Height), out hitIndex);
                        if (hitEdge != null && !saveEdgeList.Contains(hitEdge))
                        {
                            saveEdgeList.Add(hitEdge);
                        }
                        // XBoundarySelection[y + 1, x]
                        hitEdge = hitTestEdge(new Point((int)((double)(saveX + 0.5) * Delta.Width), (saveY + 1) * Delta.Height), out hitIndex);
                        if (hitEdge != null && !saveEdgeList.Contains(hitEdge))
                        {
                            saveEdgeList.Add(hitEdge);
                        }
                    }
                }

                // 領域の移動処理
                //   元の領域を消去
                for (int y = 0; y < saveHeight; y++)
                {
                    int saveY = y + saveLocationRect.Top / Delta.Height;
                    for (int x = 0; x < saveWidth; x++)
                    {
                        int saveX = x + saveLocationRect.Left / Delta.Width;
                        AreaSelection[saveY, saveX] = CellType.Empty;
                    }
                }
                //   新たな領域に追加
                for (int y = 0; y < saveHeight; y++)
                {
                    int newY = y + LocationRect.Top / Delta.Height;
                    for (int x = 0; x < saveWidth; x++)
                    {
                        int newX = x + LocationRect.Left / Delta.Width;
                        AreaSelection[newY, newX] = saveAreaSelection[y, x];
                    }
                }

                // 辺の移動処理
                //   ヒットした辺は領域外にはみ出していれば削除する(分割するのは面倒なのでごめんなさい)
                foreach (Edge saveEdge in saveEdgeList)
                {
                    if (saveEdge.Delta.Equals(new Size(1, 0)))
                    {
                        int y0 = saveEdge.Points[0].Y;
                        if (saveEdge.Points[0].X < saveLocationRect.Left / Delta.Width)
                        {
                            for (int x = saveEdge.Points[0].X; x <= saveLocationRect.Left / Delta.Width - 1; x++)
                            {
                                XBoundarySelection[y0, x] = false;
                            }
                            saveEdge.Points[0].X = saveLocationRect.Left / Delta.Width;
                        }
                        if (saveEdge.Points[1].X > (saveLocationRect.Right / Delta.Width))
                        {
                            for (int x = (saveLocationRect.Right / Delta.Width); x <= saveEdge.Points[1].X - 1; x++)
                            {
                                XBoundarySelection[y0, x] = false;
                            }
                            saveEdge.Points[1].X = (saveLocationRect.Right / Delta.Width);
                        }
                    }
                    else if (saveEdge.Delta.Equals(new Size(0, 1)))
                    {
                        int x0 = saveEdge.Points[0].X;
                        if (saveEdge.Points[0].Y < saveLocationRect.Top / Delta.Height)
                        {
                            for (int y = saveEdge.Points[0].Y; y <= saveLocationRect.Top / Delta.Height - 1; y++)
                            {
                                YBoundarySelection[y, x0] = false;
                            }
                            saveEdge.Points[0].Y = saveLocationRect.Top / Delta.Height;
                        }
                        if (saveEdge.Points[1].Y > (saveLocationRect.Bottom / Delta.Height))
                        {
                            for (int y = (saveLocationRect.Bottom / Delta.Height) + 1; y <= saveEdge.Points[1].Y - 1; y++)
                            {
                                YBoundarySelection[y, x0] = false;
                            }
                            saveEdge.Points[1].Y = (saveLocationRect.Bottom / Delta.Height);
                        }
                    }
                    else
                    {
                        // logic error
                        System.Diagnostics.Debug.Assert(false);
                    }
                }
                // 辺を移動
                int ofsX = LocationRect.Left / Delta.Width -saveLocationRect.Left / Delta.Width;
                int ofsY = LocationRect.Top / Delta.Height - saveLocationRect.Top / Delta.Height;
                foreach (Edge saveEdge in saveEdgeList)
                {
                    // 以前の境界選択を消去
                    if (saveEdge.Delta.Equals(new Size(1, 0)))
                    {
                        int y0 = saveEdge.Points[0].Y;
                        for (int x = saveEdge.Points[0].X; x <= saveEdge.Points[1].X - 1; x++)
                        {
                            XBoundarySelection[y0, x] = false;
                        }
                    }
                    else if (saveEdge.Delta.Equals(new Size(0, 1)))
                    {
                        int x0 = saveEdge.Points[0].X;
                        for (int y = saveEdge.Points[0].Y; y <= saveEdge.Points[1].Y - 1; y++)
                        {
                            YBoundarySelection[y, x0] = false;
                        }
                    }
                    else
                    {
                        // logic error
                        System.Diagnostics.Debug.Assert(false);
                    }
                    // エッジ情報を更新
                    for (int i = 0; i < saveEdge.Points.Length; i++)
                    {
                        saveEdge.Points[i].X += ofsX;
                        saveEdge.Points[i].Y += ofsY;
                    }
                    // 新しい境界選択を追加
                    // 以前の境界選択を消去
                    if (saveEdge.Delta.Equals(new Size(1, 0)))
                    {
                        int y0 = saveEdge.Points[0].Y;
                        for (int x = saveEdge.Points[0].X; x <= saveEdge.Points[1].X - 1; x++)
                        {
                            XBoundarySelection[y0, x] = true;
                        }
                    }
                    else if (saveEdge.Delta.Equals(new Size(0, 1)))
                    {
                        int x0 = saveEdge.Points[0].X;
                        for (int y = saveEdge.Points[0].Y; y <= saveEdge.Points[1].Y - 1; y++)
                        {
                            YBoundarySelection[y, x0] = true;
                        }
                    }
                    else
                    {
                        // logic error
                        System.Diagnostics.Debug.Assert(false);
                    }
                }
                executed = true;
            }
            if (executed && !_IsDirty)
            {
                _IsDirty = true;
            }
            return executed;
        }

        /// <summary>
        /// ヒットテスト中処理
        /// </summary>
        /// <returns></returns>
        private bool hitTesting()
        {
            bool hit = false;
            Point minPt = new Point();
            Point maxPt = new Point();
            if (StartPt.X <= EndPt.X)
            {
                minPt.X = StartPt.X;
                maxPt.X = EndPt.X;
            }
            else
            {
                minPt.X = EndPt.X;
                maxPt.X = StartPt.X;
            }
            if (StartPt.Y <= EndPt.Y)
            {
                minPt.Y = StartPt.Y;
                maxPt.Y = EndPt.Y;
            }
            else
            {
                minPt.Y = EndPt.Y;
                maxPt.Y = StartPt.Y;
            }
            if (CadMode == CadModeType.Location && !LocationRect.IsEmpty)
            {
                // Note: maxPtは、セルの左上のポイントを指すので、LocationRectから見ると、Delta分だけ小さくなる
                minPt.X = LocationRect.Left + (EndPt.X - StartPt.X);
                minPt.Y = LocationRect.Top + (EndPt.Y - StartPt.Y);
                maxPt.X = LocationRect.Right + (EndPt.X - StartPt.X) - Delta.Width;
                maxPt.Y = LocationRect.Bottom + (EndPt.Y - StartPt.Y) - Delta.Height;
            }

            // 方眼紙、辺の描画
            using (Graphics screen_g = CadPanel.CreateGraphics())
            using(Bitmap bitmap = new Bitmap(CadPanel.ClientSize.Width, CadPanel.ClientSize.Height, screen_g))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                if (CadMode == CadModeType.IncidentPort || CadMode == CadModeType.PortNumbering
                    || CadMode == CadModeType.Erase)
                {
                    // ヒットするポート境界線分の描画
                    hit = hitTestingPort(EndPt, g);
                }
                if (CadMode == CadModeType.Location
                       || CadMode == CadModeType.Area
                       || CadMode == CadModeType.Port
                       || CadMode == CadModeType.Erase)
                {
                    if (DragFlg)
                    {
                        // ヒットする線があるか
                        hit = hitTestingLines(minPt, maxPt, false, g);
                    }

                    // 背景クリア
                    using (Brush brush = new SolidBrush(CadPanel.BackColor))
                    {
                        g.FillRectangle(brush, CadPanel.ClientRectangle);
                    }
                    DrawAreas(g);

                    if (!hit)
                    {
                        // ヒットするエリアの描画
                        bool fillFlg = true;
                        if (CadMode == CadModeType.Location)
                        {
                            fillFlg = false;
                        }
                        hit = hittingTestDisconArea(minPt, maxPt, g, fillFlg);
                    }
                    DrawGrid(g);
                    DrawEdges(g);
                    if (DragFlg)
                    {
                        // ヒットする線を描画
                        bool workhit;
                        workhit = hitTestingLines(minPt, maxPt, true, g);
                    }
                }
                screen_g.DrawImage(bitmap, new Point(0, 0));
            }
            return hit;
        }

        /// <summary>
        /// 線分のヒットテスト
        ///   hitx != -1 Y方向の辺がヒット (hitx, edy)が辺の始点
        ///   hity != -1 X方向の辺がヒット (edx, hity)が辺の始点
        /// </summary>
        /// <param name="endPt"></param>
        /// <param name="hitx"></param>
        /// <param name="hity"></param>
        /// <param name="edx"></param>
        /// <param name="edy"></param>
        /// <returns></returns>
        private bool hitTestLine(Point endPt, out int hitx, out int hity, out int edx, out int edy)
        {
            bool hit = false;
            // 線分付近判定のスレッショルド
            double th = 0.3;

            // 終了ポイントのインデックス変換
            double edxx = (double)endPt.X / (double)Delta.Width;
            edx = (int)edxx;
            double edyy = (double)endPt.Y / (double)Delta.Height;
            edy = (int)edyy;

            hitx = -1;
            hity = -1;

            double eddx = edxx - edx;
            double eddy = edyy - edy;

            // Y方向境界
            if (eddx >= -th && eddx <= th)
            {
                if (edx >= 0 && edx < MaxDiv.Width + 1)
                {
                    hitx = edx;
                }
            }
            if (eddx < 1.0 && eddx >= (1.0 - th))
            {
                if (edx >= 0 && edx < MaxDiv.Width)
                {
                    hitx = edx + 1;
                }
            }
            // X方向境界
            if (eddy >= -th && eddy <= th)
            {
                if (edy >= 0 && edy < MaxDiv.Height + 1)
                {
                    hity = edy;
                }
            }
            if (eddy < 1.0 && eddy >= (1.0 - th))
            {
                if (edy >= 0 && edy < MaxDiv.Height)
                {
                    hity = edy + 1;
                }
            }

            if (hitx == -1 && hity == -1)
            {
                // ヒットなし
                return hit;
            }
            // hitx != -1 Y方向の辺がヒット (hitx, edy)が辺の始点
            // hity != -1 X方向の辺がヒット (edx, hity)が辺の始点

            // 改善:X,Y方向両方がヒットしないようにする
            if (hitx != -1 && hity != -1)
            {
                // X方向,Y方向両方の辺がヒットする場合
                double eddx2 = edxx - hitx;
                double eddy2 = edyy - hity;
                // >= (1.0 - th)の場合は+1したのでここでは、±thになっているはず
                System.Diagnostics.Debug.Assert(Math.Abs(eddx2) <= th + Constants.PrecisionLowerLimit);
                System.Diagnostics.Debug.Assert(Math.Abs(eddy2) <= th + Constants.PrecisionLowerLimit);
                if (Math.Abs(eddx2) < Math.Abs(eddy2))
                {
                    // Y方向の辺(hitx != -1)の方がより近い
                    // X方向の辺はヒットから外す
                    hity = -1;
                }
                else
                {
                    // X方向の辺(hity != -1)の方がより近い
                    hitx = -1;
                }
            }

            // 改善:範囲チェック
            if (hitx != -1)
            {
                // Y方向の辺
                if (edy < 0 || edy > MaxDiv.Height - 1)
                {
                    // 範囲外なのでヒットから外す
                    hitx = -1;
                }
            }
            if (hity != -1)
            {
                // X方向の辺
                if (edx < 0 || edx > MaxDiv.Width - 1)
                {
                    // 範囲外なのでヒットから外す
                    hity = -1;
                }
            }

            // 最終チェック
            if (hitx == -1 && hity == -1)
            {
                // ヒットなし
                return hit;
            }
            // ヒットした
            hit = true;

            return hit;
        }

        /// <summary>
        /// 辺のヒットテスト
        /// </summary>
        /// <param name="endPt"></param>
        /// <returns></returns>
        private Edge hitTestEdge(Point endPt, out int hitIndex)
        {
            Edge hitEdge = null;
            hitIndex = -1;

            int hitx;
            int hity;
            int edx;
            int edy;
            bool hit = hitTestLine(endPt, out hitx, out hity, out edx, out edy);
            if (!hit)
            {
                return hitEdge;
            }
            if (hitx != -1)
            {
                // Y方向辺 始点(hitx, edy)
                Size edgeDelta = new Size(0, 1);
                int counter = 0;
                foreach (Edge edge in EdgeList)
                {
                    // Y方向辺に関してヒットテスト
                    if (edge.HitTest(new Point(hitx, edy), edgeDelta))
                    {
                        hitEdge = edge;
                        hitIndex = counter;
                        break;
                    }
                    counter++;
                }
            }
            if (hitEdge == null && hity != -1)
            {
                // X方向辺 始点(edx, hity)
                Size edgeDelta = new Size(1, 0);
                int counter = 0;
                foreach (Edge edge in EdgeList)
                {
                    // X方向辺に関してヒットテスト
                    if (edge.HitTest(new Point(edx, hity), edgeDelta))
                    {
                        hitEdge = edge;
                        hitIndex = counter;
                        break;
                    }
                    counter++;
                }
            }
            return hitEdge;
        }

        /// <summary>
        /// 入射ポートの選択処理
        /// </summary>
        /// <returns></returns>
        private bool doSelectIncidentPort(Point endPt)
        {
            if (CadMode != CadModeType.IncidentPort)
            {
                return false;
            }
            bool executed = false;

            // ヒットする辺を取得する
            int hitIndex;
            Edge hitEdge = hitTestEdge(endPt, out hitIndex);
            if (hitEdge != null)
            {
                // ヒットした辺の辺番号を入射ポート番号に設定する
                IncidentPortNo = hitEdge.No;
                executed = true;
            }

            if (executed && !_IsDirty)
            {
                _IsDirty = true;
            }

            return executed;
        }

        /// <summary>
        /// ポートの番号付与
        /// </summary>
        /// <returns></returns>
        private bool doNumberingPort(Point endPt)
        {
            if (CadMode != CadModeType.PortNumbering)
            {
                return false;
            }
            bool executed = false;

            // ヒットする辺を取得する
            int hitIndex;
            Edge hitEdge = hitTestEdge(endPt, out hitIndex);
            if (hitEdge != null)
            {
                int hitPortNo = hitEdge.No;
                int newNumber = PortNumberingSeq;
                PortNumberingSeq++;
                if (PortNumberingSeq > EdgeList.Count)
                {
                    PortNumberingSeq = 1;
                }

                // 以前の入射ポート番号を退避
                int saveIncidentPortNo = IncidentPortNo;
                // 辺番号をリナンバリングする
                EdgeList[hitIndex].No = newNumber;
                if (hitPortNo == saveIncidentPortNo)
                {
                    // リナンバリングした辺が入射ポートだった場合、入射ポート番号も変更する
                    IncidentPortNo = newNumber;
                }
                // 他の辺をリナンバリングする（重複があるはずなので)
                for (int i = 0; i < EdgeList.Count; i++)
                {
                    if (i == hitIndex) continue;
                    Edge edge = EdgeList[i];
                    int oldEdgeNo = edge.No;
                    if (hitPortNo > newNumber)
                    {
                        if (edge.No >= newNumber && edge.No < hitPortNo)
                        {
                            // リナンバリング
                            edge.No = edge.No + 1;
                            if (oldEdgeNo == saveIncidentPortNo)
                            {
                                // リナンバリングした辺が入射ポートだった場合、入射ポート番号も変更する
                                IncidentPortNo = edge.No;
                            }
                        }
                    }
                    if (hitPortNo < newNumber)
                    {
                        if (edge.No > hitPortNo && edge.No <= newNumber)
                        {
                            // リナンバリング
                            edge.No = edge.No - 1;
                            if (oldEdgeNo == saveIncidentPortNo)
                            {
                                // リナンバリングした辺が入射ポートだった場合、入射ポート番号も変更する
                                IncidentPortNo = edge.No;
                            }
                        }
                    }
                    //System.Diagnostics.Debug.Write("{0},", edge.No);
                }
                //System.Diagnostics.Debug.WriteLine(" ");
                // 番号順に並び替え
                ((List<Edge>)EdgeList).Sort();
                executed = true;
            }

            if (executed && !_IsDirty)
            {
                _IsDirty = true;
            }

            return executed;
        }

        /// <summary>
        /// ポートのヒットテスト処理中
        ///   入射ポート選択処理中、ポート番号振り処理中の場合に限る
        ///   ドラッグしてなくても処理可
        /// </summary>
        /// <returns></returns>
        private bool hitTestingPort(Point endPt, Graphics g)
        {
            if (CadMode != CadModeType.IncidentPort && CadMode != CadModeType.PortNumbering)
            {
                return false;
            }
            bool hit = false;

            // ヒットする辺を取得する
            int hitIndex;
            Edge hitEdge = hitTestEdge(endPt, out hitIndex);

            // 描画はヒットした、しないに関わらず実施する
            using (Pen hitPen = new Pen(EditingColor, 4))
            {
                foreach (Edge edge in EdgeList)
                {
                    Pen pen = (edge.No == IncidentPortNo) ? IncidentPortPen : SelectedPen;
                    if (hitEdge != null && hitEdge == edge)
                    {
                        pen = hitPen;
                    }
                    drawEdge(g, edge, pen);
                }
            }

            hit = hitEdge != null;

            return hit;
        }

        /// <summary>
        /// 複数の線分のヒットテスト
        // Y方向の辺がヒット hitx != -1 始点(hitx, sty) 最後の線分の始点(hitx, edy)
        // X方向の辺がヒット hity != -1 始点(stx, hity) 最後の線分の始点(edx, hity) 
        /// </summary>
        /// <param name="startPt"></param>
        /// <param name="endPt"></param>
        /// <param name="hitx"></param>
        /// <param name="hity"></param>
        /// <param name="stx"></param>
        /// <param name="sty"></param>
        /// <param name="edx"></param>
        /// <param name="edy"></param>
        /// <returns></returns>
        private bool hitTestLineRange(
            Point startPt, Point endPt,
            out int hitx, out int hity,
            out int stx, out int sty,
            out int edx, out int edy)
        {
            bool hit = false;
            int hitstx;
            int hitsty;
            int hitedx;
            int hitedy;


            // 線分付近判定のスレッショルド
            double th = 0.3;
            // 開始ポイント、終了ポイントのインデックス変換
            double stxx = (double)startPt.X / (double)Delta.Width;
            stx = (int)stxx;
            double styy = (double)startPt.Y / (double)Delta.Height;
            sty = (int)styy;
            double edxx = (double)endPt.X / (double)Delta.Width;
            edx = (int)edxx;
            double edyy = (double)endPt.Y / (double)Delta.Height;
            edy = (int)edyy;

            hitx = -1;
            hity = -1;

            hitstx = -1;
            hitsty = -1;
            hitedx = -1;
            hitedy = -1;

            double stdx = stxx - stx;
            double stdy = styy - sty;
            double eddx = edxx - edx;
            double eddy = edyy - edy;

            // Y方向境界
            if (stdx >= - th &&  stdx <= th)
            {
                if (stx >= 0 && stx < MaxDiv.Width + 1)
                {
                    hitstx = stx;
                }
            }
            if (stdx < 1.0 && stdx >= (1.0 - th))
            {
                if (stx >= 0 && stx < MaxDiv.Width)
                {
                    hitstx = stx + 1;
                }
            }
            // X方向境界
            if (stdy >= - th && stdy <= th)
            {
                if (sty >= 0 && sty < MaxDiv.Height + 1)
                {
                    hitsty = sty;
                }
            }
            if (stdy < 1.0 && stdy >= (1.0 - th))
            {
                if (sty >= 0 && sty < MaxDiv.Height)
                {
                    hitsty = sty + 1;
                }
            }
            if (hitstx == -1 && hitsty == -1)
            {
                // ヒットなし
                return hit;
            }
            // Y方向境界
            if (eddx >= -th && eddx <= th)
            {
                if (edx >= 0 && edx < MaxDiv.Width + 1)
                {
                    hitedx = edx;
                }
            }
            if (eddx < 1.0 && eddx >= (1.0 - th))
            {
                if (edx >= 0 && edx < MaxDiv.Width)
                {
                    hitedx = edx + 1;
                }
            }
            // X方向境界
            if (eddy >= - th && eddy <= th)
            {
                if (edy >= 0 && edy < MaxDiv.Height + 1)
                {
                    hitedy = edy;
                }
            }
            if (eddy < 1.0 && eddy >= (1.0 - th))
            {
                if (edy >= 0 && edy < MaxDiv.Height)
                {
                    hitedy = edy + 1;
                }
            }
            if (hitedx == -1 && hitedy == -1)
            {
                // ヒットなし
                return hit;
            }

            // Y方向の辺がヒット hitstx != -1 && hitstx == hitedx  始点(hitx, sty) 最後の線分の始点(hitx, edy)
            // X方向の辺がヒット hitsty != -1 && hitsty == hitedy  始点(stx, hity) 最後の線分の始点(edx, hity) 
            if (hitstx != -1 && hitstx == hitedx)
            {
                // Y方向の辺がヒット
                hitx = hitstx;
            }
            if (hitsty != -1 && hitsty == hitedy)
            {
                // X方向の辺がヒット
                hity = hitsty;
            }

            if (hitx == -1 && hity == -1)
            {
                // ヒットなし
                return hit;
            }

            // 改善:X,Y方向両方がヒットしないようにする
            if (hitx != -1 && hity != -1)
            {
                // X方向,Y方向両方の辺がヒットする場合
                double eddx2 = edxx - hitx;
                double eddy2 = edyy - hity;
                // >= (1.0 - th)の場合は+1したのでここでは、±thになっているはず
                System.Diagnostics.Debug.Assert(Math.Abs(eddx2) <= th + Constants.PrecisionLowerLimit);
                System.Diagnostics.Debug.Assert(Math.Abs(eddy2) <= th + Constants.PrecisionLowerLimit);
                if (Math.Abs(eddx2) < Math.Abs(eddy2))
                {
                    // Y方向の辺(hitx != -1)の方がより近い
                    // X方向の辺はヒットから外す
                    hity = -1;
                }
                else
                {
                    // X方向の辺(hity != -1)の方がより近い
                    hitx = -1;
                }
            }

            // 改善:範囲チェック
            if (hitx != -1)
            {
                // Y方向の辺
                if (sty == edy)  // 1辺だけ選択されたとき
                {
                    if (sty < 0 || sty > MaxDiv.Height - 1)
                    {
                        // 範囲外なのでヒットから外す
                        hitx = -1;
                    }
                    if (edy < 0 || edy > MaxDiv.Height - 1)
                    {
                        // 範囲外なのでヒットから外す
                        hitx = -1;
                    }
                }
                else
                {
                    if (sty < 0)
                    {
                        sty = 0;
                    }
                    if (sty > MaxDiv.Height - 1)
                    {
                        sty = MaxDiv.Height - 1;
                    }
                    if (edy < 0)
                    {
                        edy = 0;
                    }
                    if (edy > MaxDiv.Height - 1)
                    {
                        edy = MaxDiv.Height - 1;
                    }
                }
            }
            if (hity != -1)
            {
                // X方向の辺
                if (stx == edx) // 1辺だけ選択されたとき
                {
                    if (stx < 0 || stx > MaxDiv.Width - 1)
                    {
                        // 範囲外なのでヒットから外す
                        hity = -1;
                    }
                    if (edx < 0 || edx > MaxDiv.Width - 1)
                    {
                        // 範囲外なのでヒットから外す
                        hity = -1;
                    }
                }
                else
                {
                    if (stx < 0)
                    {
                        stx = 0;
                    }
                    if (stx > MaxDiv.Width - 1)
                    {
                        stx = MaxDiv.Width - 1;
                    }
                    if (edx < 0)
                    {
                        edx = 0;
                    }
                    if (edx > MaxDiv.Width - 1)
                    {
                        edx = MaxDiv.Width - 1;
                    }
                }
            }

            // 最終チェック
            if (hitx == -1 && hity == -1)
            {
                // ヒットなし
                return hit;
            }
            // ヒットした
            hit = true;
            return hit;
        }

            /// <summary>
        /// ポート境界の選択処理
        /// </summary>
        /// <returns></returns>
        private bool doSelectWaveguidePort(Point startPt, Point endPt)
        {
            if (CadMode != CadModeType.Port
                && CadMode != CadModeType.Erase)
            {
                return false;
            }
            bool executed = false;
            bool valueToSet = ((CadMode == CadModeType.Erase)) ? false : true;

            // 複数の線分のヒットテスト
            int stx;
            int sty;
            int edx;
            int edy;
            int hitx;
            int hity;
            bool hit = hitTestLineRange(
                startPt, endPt,
                out hitx, out hity,
                out stx, out sty,
                out edx, out edy);
            if (!hit)
            {
                return executed;
            }
            Edge edge = null;
            if (hitx != -1)
            {
                // Y方向境界
                //System.Diagnostics.Debug.WriteLine("YBoundary:x={0},y={1},{2}", hitx, sty, edy);
                for (int y = sty; y < (edy + 1); y++)
                {
                    if (y < 0 || y >= MaxDiv.Height) continue;
                    if (valueToSet)
                    {
                        // 境界選択処理(valueToSet:true)で領域が選択されていない場合スキップ
                        if (hitx == 0)
                        {
                            if (AreaSelection[y, hitx] == CellType.Empty) continue;
                        }
                        else if (hitx == MaxDiv.Width)
                        {
                            if (AreaSelection[y, hitx - 1] == CellType.Empty) continue;
                        }
                        else
                        {
                            if (!((AreaSelection[y, hitx - 1] == CellType.Empty && AreaSelection[y, hitx] != CellType.Empty)
                                        || (AreaSelection[y, hitx - 1] != CellType.Empty && AreaSelection[y, hitx] == CellType.Empty)
                                        )) continue;
                        }
                    }
                    if (YBoundarySelection[y, hitx] != valueToSet)
                    {
                        YBoundarySelection[y, hitx] = valueToSet;
                        executed = true;
                    }
                }
                if (executed)
                {
                    edge = new Edge(new Size(0, 1));
                    edge.No = 0;
                    int minY = int.MaxValue;
                    int maxY = 0;
                    for (int y = sty; y < (edy + 1); y++)
                    {
                        if (y < 0 || y >= MaxDiv.Height) continue;
                        if (YBoundarySelection[y, hitx] == valueToSet)
                        {
                            if (minY > y)
                            {
                                minY = y;
                            }
                            if (maxY < y )
                            {
                                maxY = y;
                            }
                        }
                    }
                    maxY = maxY + 1;  // 終点は最後の点に+1したもの
                    edge.Set(new Point(hitx, minY), new Point(hitx, maxY));
                }
            }
            if (!executed && hity != -1)
            {
                // X方向境界
                //System.Diagnostics.Debug.WriteLine("XBoundary:y={0},x={1},{2}", hity, stx, edx);
                for (int x = stx; x < (edx + 1); x++)
                {
                    if (x < 0 || x >= MaxDiv.Width) continue;
                    if (valueToSet)
                    {
                        // 境界選択処理(valueToSet:true)で領域が選択されていない場合スキップ
                        if (hity == 0)
                        {
                            if (AreaSelection[hity, x] == CellType.Empty) continue;
                        }
                        else if (hity == MaxDiv.Height)
                        {
                            if (AreaSelection[hity - 1, x] == CellType.Empty) continue;
                        }
                        else
                        {
                            if (!((AreaSelection[hity - 1, x] == CellType.Empty && AreaSelection[hity, x] != CellType.Empty)
                                        || (AreaSelection[hity - 1, x] != CellType.Empty && AreaSelection[hity, x] == CellType.Empty)
                                        )) continue;
                        }
                    }
                    if (XBoundarySelection[hity, x] != valueToSet)
                    {
                        XBoundarySelection[hity, x] = valueToSet;
                        executed = true;
                    }
                }
                if (executed)
                {
                    edge = new Edge(new Size(1, 0));
                    edge.No = 0;
                    int minX = int.MaxValue;
                    int maxX = 0;
                    for (int x = stx; x < (edx + 1); x++)
                    {
                        if (x < 0 || x >= MaxDiv.Width) continue;
                        if (XBoundarySelection[hity, x] == valueToSet)
                        {
                            if (minX > x)
                            {
                                minX = x;
                            }
                            if (maxX < x )
                            {
                                maxX = x;
                            }
                        }
                    }
                    maxX = maxX + 1;  // 終点は最後の点に+1したもの
                    edge.Set(new Point(minX, hity), new Point(maxX, hity));
                }
            }
            if (edge != null)
            {
                System.Diagnostics.Debug.Assert(!edge.IsEmpty());
                //System.Diagnostics.Debug.WriteLine("target:({0},{1}),({2},{3})", edge.Points[0].X, edge.Points[0].Y, edge.Points[1].X, edge.Points[1].Y);
                // 辺リストの最大番号を取得
                int maxNo = 0;
                foreach (Edge work in EdgeList)
                {
                    if (work.No > maxNo)
                    {
                        maxNo = work.No;
                    }
                }
                // 重なりチェック & 辺の番号取得
                IList<Edge> containEdges = new List<Edge>();
                foreach (Edge work in EdgeList)
                {
                    if (work.ContainsEdge(edge))
                    {
                        if (edge.No == 0 || edge.No > work.No)
                        {
                            edge.No = work.No;
                        }
                        containEdges.Add(work);
                    }
                }
                if (containEdges.Count > 0)
                {
                    // 番号は決定済み
                    System.Diagnostics.Debug.Assert(edge.No != 0);
                    
                    // 重なりがある場合は、合体させる
                    foreach (Edge work in containEdges)
                    {
                        if (valueToSet)
                        {
                            // 合体
                            edge.MergeEdge(work);
                            //System.Diagnostics.Debug.WriteLine("merge:({0},{1}),({2},{3})", edge.Points[0].X, edge.Points[0].Y, edge.Points[1].X, edge.Points[1].Y);

                            // 合体前の辺は削除
                            //System.Diagnostics.Debug.WriteLine("removed:({0},{1}),({2},{3})", work.Points[0].X, work.Points[0].Y, work.Points[1].X, work.Points[1].Y);
                            EdgeList.Remove(work);
                        }
                        else
                        {
                            // 分解
                            IList<Edge> splitEdges = work.SplitEdge(edge);
                            if (splitEdges.Count > 0)
                            {
                                // 分解前の辺は削除
                                //System.Diagnostics.Debug.WriteLine("removed:({0},{1}),({2},{3})", work.Points[0].X, work.Points[0].Y, work.Points[1].X, work.Points[1].Y);
                                EdgeList.Remove(work);
                                // 分解した辺を追加
                                foreach (Edge splitEdge in splitEdges)
                                {
                                    if (!splitEdge.IsEmpty())
                                    {
                                        if (splitEdge.No == 0)
                                        {
                                            // 新規
                                            maxNo++;
                                            splitEdge.No = maxNo;
                                        }
                                        //System.Diagnostics.Debug.WriteLine("addNo:{0}", splitEdge.No);
                                        //System.Diagnostics.Debug.WriteLine("added:({0},{1}),({2},{3})", splitEdge.Points[0].X, splitEdge.Points[0].Y, splitEdge.Points[1].X, splitEdge.Points[1].Y);
                                        EdgeList.Add(splitEdge);
                                    }
                                }
                            }
                        }
                    }
                    if (valueToSet)
                    {
                        // 追加
                        //System.Diagnostics.Debug.WriteLine("addNo:{0}", edge.No);
                        //System.Diagnostics.Debug.WriteLine("added:({0},{1}),({2},{3})", edge.Points[0].X, edge.Points[0].Y, edge.Points[1].X, edge.Points[1].Y);
                        EdgeList.Add(edge);
                    }
                }
                else
                {
                    if (valueToSet)
                    {
                        // 新規
                        maxNo++;
                        edge.No = maxNo;
                        // 追加
                        //System.Diagnostics.Debug.WriteLine("addNo:{0}", edge.No);
                        //System.Diagnostics.Debug.WriteLine("added:({0},{1}),({2},{3})", edge.Points[0].X, edge.Points[0].Y, edge.Points[1].X, edge.Points[1].Y);
                        EdgeList.Add(edge);
                    }
                }
                // 番号順に並び替え
                ((List<Edge>)EdgeList).Sort();
                // 欠番を無くす
                int portCounter = 0;
                int newIncidentPortNo = -1;
                foreach (Edge work in EdgeList)
                {
                    //System.Diagnostics.Debug.Write("{0}", work.No);
                    int saveNo = work.No;
                    work.No = ++portCounter;
                    //System.Diagnostics.Debug.WriteLine("  --> {0}", work.No);
                    if (saveNo == IncidentPortNo)
                    {
                        //BUGFIX 判定に用いているIncdentPortNoを書き換えない!!!
                        //IncidentPortNo = work.No;
                        newIncidentPortNo = work.No;
                    }
                }
                if (newIncidentPortNo != -1)
                {
                    IncidentPortNo = newIncidentPortNo;
                }
            }
            if (executed && !_IsDirty)
            {
                _IsDirty = true;
            }

            return executed;
        }

        /// <summary>
        /// ポートのヒットテスト処理中
        ///   ポート選択処理中、消しゴム処理中の場合に限る
        ///   ドラッグ中のみ処理する
        /// </summary>
        /// <returns></returns>
        private bool hitTestingLines(Point startPt, Point endPt, bool drawFlg, Graphics g)
        {
            /*
            if (!DragFlg)
            {
                return false;
            }
             */
            if (CadMode != CadModeType.Port 
                && CadMode != CadModeType.Location
                && CadMode != CadModeType.Erase)
            {
                return false;
            }
            bool hit = false;

            // 複数の線分のヒットテスト
            int stx;
            int sty;
            int edx;
            int edy;
            int hitx;
            int hity;
            hit = hitTestLineRange(
                startPt, endPt,
                out hitx, out hity,
                out stx, out sty,
                out edx, out edy);

            if (!drawFlg)
            {
                return hit;
            }

            // 描画はヒットした、しないに関わらず実施する
            using (Pen hitPen = new Pen(EditingColor, 4))
            {
                //hitPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                Size edgeDelta;
                // Y方向辺
                edgeDelta = new Size(0, 1);
                if (hitx != -1)
                {
                    // 描画用にテンポラリの辺を作成
                    Edge edge = new Edge(edgeDelta); // Y方向辺
                    edge.Set(new Point(hitx, sty), new Point(hitx, edy + 1)); // 終点は、最後の点(hitx, edy)にU方向へ+1したもの
                    drawEdge(g, edge, hitPen);
                }
                // X方向辺
                edgeDelta = new Size(1, 0);
                if (hity != -1)
                {
                    // 描画用にテンポラリの辺を作成
                    Edge edge = new Edge(edgeDelta); // Y方向辺
                    edge.Set(new Point(stx, hity), new Point(edx + 1, hity)); // 終点は、最後の点(edx, hity)にX方向へ+1したもの
                    drawEdge(g, edge, hitPen);
                }
            }
            return hit;
        }

        /// <summary>
        /// 不連続領域の領域選択処理
        /// </summary>
        /// <returns></returns>
        private bool doSelectDisconArea(Point startPt, Point endPt)
        {
            if (CadMode != CadModeType.Area
                && CadMode != CadModeType.Erase)
            {
                return false;
            }
            bool executed = false;
            double stxx = (double)startPt.X / (double)Delta.Width;
            int stx = (int)stxx;
            double styy = (double)startPt.Y / (double)Delta.Height;
            int sty = (int)styy;
            double edxx = (double)endPt.X / (double)Delta.Width;
            int edx = (int)edxx;
            double edyy = (double)endPt.Y / (double)Delta.Height;
            int edy = (int)edyy;

            CellType valueToSet = ((CadMode == CadModeType.Erase)) ? CellType.Empty : SelectedCellType;
            for (int y = sty; y < edy + 1; y++)
            {
                if (y < 0 || y >= MaxDiv.Height) continue;
                for (int x = stx; x < edx + 1; x++)
                {
                    if (x < 0 || x >= MaxDiv.Width) continue;
                    if (AreaSelection[y, x] != valueToSet)
                    {
                        AreaSelection[y, x] = valueToSet;
                        if (valueToSet == CellType.Empty)
                        {
                            //System.Diagnostics.Debug.WriteLine("Delte Boundary for Area x ={0}, y ={1}", x, y);
                            // 消去処理で境界に面している場合は、境界も消去
                            // Y方向境界
                            // YBoundarySelection[y, x] = false;
                            doSelectWaveguidePort(new Point(x * Delta.Width,       (int)((double)(y + 0.5) * Delta.Height)), new Point(x * Delta.Width,       (int)((double)(y + 0.5) * Delta.Height)));
                            // YBoundarySelection[y, x + 1] = false;
                            doSelectWaveguidePort(new Point((x + 1) * Delta.Width, (int)((double)(y + 0.5) * Delta.Height)), new Point((x + 1) * Delta.Width, (int)((double)(y + 0.5) * Delta.Height)));
                            // X方向境界
                            // XBoundarySelection[y, x] = false;
                            doSelectWaveguidePort(new Point((int)((double)(x + 0.5) * Delta.Width), y * Delta.Height),       new Point((int)((double)(x + 0.5) * Delta.Width), y * Delta.Height));
                            // XBoundarySelection[y + 1, x] = false;
                            doSelectWaveguidePort(new Point((int)((double)(x + 0.5) * Delta.Width), (y + 1) * Delta.Height), new Point((int)((double)(x + 0.5) * Delta.Width), (y + 1) * Delta.Height));
                        }
                        executed = true;
                    }
                }
            }
            if (executed && !_IsDirty)
            {
                _IsDirty = true;
            }
            return executed;
        }

        /// <summary>
        /// 不連続部領域のヒットテスト中処理
        /// </summary>
        /// <param name="startPt"></param>
        /// <param name="endPt"></param>
        /// <returns></returns>
        private bool hittingTestDisconArea(Point startPt, Point endPt, Graphics g, bool fillFlg = true)
        {
            /*
            if (!DragFlg)
            {
                return false;
            }
             */
            if (CadMode != CadModeType.Area
                && CadMode != CadModeType.Location
                && CadMode != CadModeType.Erase)
            {
                return false;
            }
            bool hit = false;
            double stxx = (double)startPt.X / (double)Delta.Width;
            int stx = (int)stxx;
            double styy = (double)startPt.Y / (double)Delta.Height;
            int sty = (int)styy;
            double edxx = (double)endPt.X / (double)Delta.Width;
            int edx = (int)edxx;
            double edyy = (double)endPt.Y / (double)Delta.Height;
            int edy = (int)edyy;

            Point minPt = new Point(MaxDiv.Width * Delta.Width, MaxDiv.Height * Delta.Height);
            Point maxPt = new Point(0, 0);
            for (int y = sty; y < edy + 1; y++)
            {
                if (y < 0 || y >= MaxDiv.Height) continue;
                for (int x = stx; x < edx + 1; x++)
                {
                    if (x < 0 || x >= MaxDiv.Width) continue;
                    hit = true;
                    int xx = x * Delta.Width;
                    int yy = y * Delta.Height;
                    if (minPt.X > xx)
                    {
                        minPt.X = xx;
                    }
                    if (minPt.Y > yy)
                    {
                        minPt.Y = yy;
                    }
                    if (maxPt.X < xx)
                    {
                        maxPt.X = xx;
                    }
                    if (maxPt.Y < yy)
                    {
                        maxPt.Y = yy;
                    }
                }
            }
            Rectangle hitRect = new Rectangle(minPt + Ofs, new Size(maxPt.X - minPt.X, maxPt.Y - minPt.Y) + Delta);
            if (fillFlg)
            {
                using (Brush brush = new SolidBrush(EditingColor))
                {
                    g.FillRectangle(brush, hitRect);
                }
                /*
                using (Brush brush = new SolidBrush(EditingColor))
                {
                    for (int y = sty; y < edy + 1; y++)
                    {
                        if (y < 0 || y >= MaxDiv.Height) continue;
                        for (int x = stx; x < edx + 1; x++)
                        {
                            if (x < 0 || x >= MaxDiv.Width) continue;
                            drawArea(g, x, y, brush);
                            hit = true;
                        }
                    }
                }
                 */
            }
            else
            {
                using (Pen pen = new Pen(EditingColor, 5))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                    g.DrawRectangle(pen, hitRect);
                }
            }

            return hit;
        }

        /// <summary>
        /// Cadデータの書き込み
        /// </summary>
        public void SerializeCadData(string filename)
        {
            _IsDirty = false;

            // ファイルへ書き込む
            CadDatFile.SaveToFile(
                filename,
                AreaSelection,
                EdgeList,
                IncidentPortNo,
                Medias,
                _NdivForOneLattice,
                _RodRadiusRatio,
                _RodCircleDiv,
                _RodRadiusDiv
            );
        }

        /// <summary>
        /// Cadデータの読み込み
        /// </summary>
        public bool DeserializeCadData(string filename)
        {
            bool success = false;

            init();
            _IsDirty = false;

            success = CadDatFile.LoadFromFile(
                filename,
                ref AreaSelection,
                ref EdgeList, ref YBoundarySelection, ref XBoundarySelection,
                ref IncidentPortNo,
                ref Medias,
                ref _NdivForOneLattice,
                ref _RodRadiusRatio,
                ref _RodCircleDiv,
                ref _RodRadiusDiv
            );

            // Mementoの初期化
            // 現在の状態をMementoに記憶させる
            setMemento();
            // コマンド管理初期化
            CmdManager.Refresh();
            return success;
        }

        /// <summary>
        /// FEM入力データ作成
        /// </summary>
        /// <param name="filename">ファイル名(*.cad)</param>
        /// <param name="elemShapeDv">要素形状</param>
        /// <param name="order">補間次数</param>
        /// <param name="normalizedFreq1">計算開始規格化周波数</param>
        /// <param name="normalizedFreq2">計算終了規格化周波数</param>
        /// <param name="calcCnt">計算する周波数の数</param>
        /// <param name="wgStructureDv">導波路構造区分</param>
        /// <param name="waveModeDv">計算モード区分</param>
        public void MkFemInputData(
            string filename,
            Constants.FemElementShapeDV elemShapeDv, int order,
            double normalizedFreq1, double normalizedFreq2, int calcCnt,
            FemSolver.WaveModeDV waveModeDv)
        {
            IList<double[]> doubleCoords = null;
            IList<int[]> elements = null;
            IList<IList<int>> portList = null;
            int[] forceBCNodeNumbers = null;
            // 周期構造領域
            IList<IList<uint>> elemNoPeriodicList = null;
            IList<IList<IList<int>>> nodePeriodicBList = null;
            IList<IList<int>> defectNodePeriodicList = null;
            bool ret;

            if (elemShapeDv == Constants.FemElementShapeDV.Triangle && order == Constants.SecondOrder)
            {
                // ２次三角形要素メッシュ作成
                ret = FemMeshLogic.MkTriMeshSecondOrder(
                    NdivForOneLattice,
                    RodRadiusRatio,
                    RodCircleDiv,
                    RodRadiusDiv,
                    MaxDiv,
                    AreaSelection,
                    EdgeList,
                    out doubleCoords,
                    out elements,
                    out portList,
                    out forceBCNodeNumbers,
                    out elemNoPeriodicList,
                    out nodePeriodicBList,
                    out defectNodePeriodicList
                );
            }
            else if (elemShapeDv == Constants.FemElementShapeDV.Triangle && order == Constants.FirstOrder)
            {
                // １次三角形要素メッシュ作成
                ret = FemMeshLogic.MkTriMeshFirstOrder(
                    NdivForOneLattice,
                    RodRadiusRatio,
                    RodCircleDiv,
                    RodRadiusDiv,
                    MaxDiv,
                    AreaSelection,
                    EdgeList,
                    out doubleCoords,
                    out elements,
                    out portList,
                    out forceBCNodeNumbers,
                    out elemNoPeriodicList,
                    out nodePeriodicBList,
                    out defectNodePeriodicList
                );
            }
            else
            {
                ret = false;
            }
            if (!ret)
            {
                MessageBox.Show("メッシュ作成に失敗しました");
                return;
            }

            // 導波管幅の計算
            double waveguideWidth = FemSolver.DefWaveguideWidth;
            if (portList.Count > 0 && portList[0].Count >= 2)
            {
                IList<int> portNodes = portList[0];
                double[] pp1 = doubleCoords[portNodes[0] - 1];
                double[] pp2 = doubleCoords[portNodes[portNodes.Count - 1] - 1];
                waveguideWidth = FemMeshLogic.GetDistance(pp1, pp2);
            }
            System.Diagnostics.Debug.WriteLine("(MkFemInputData) waveguideWidth:{0}", waveguideWidth);
            double latticeA = 1.0;
            System.Diagnostics.Debug.WriteLine("(MkFemInputData) latticeA:{0}", latticeA);
            // 計算開始、終了波長の計算
            double firstWaveLength = FemSolver.GetWaveLengthFromNormalizedFreq(normalizedFreq1, waveguideWidth, latticeA);
            double lastWaveLength = FemSolver.GetWaveLengthFromNormalizedFreq(normalizedFreq2, waveguideWidth, latticeA);

            // Fem入力データファイルへ保存
            int nodeCnt = doubleCoords.Count;
            int elemCnt = elements.Count;
            int portCnt = portList.Count;
            FemInputDatFile.SaveToFileFromCad(
                filename,
                nodeCnt, doubleCoords,
                elemCnt, elements,
                portCnt, portList,
                forceBCNodeNumbers,
                elemNoPeriodicList,
                nodePeriodicBList,
                defectNodePeriodicList,
                IncidentPortNo,
                Medias,
                firstWaveLength,
                lastWaveLength,
                calcCnt,
                waveModeDv);
        }

        /// <summary>
        /// 媒質リストの取得
        /// </summary>
        /// <returns></returns>
        public MediaInfo[] GetMediaList()
        {
            MediaInfo[] medias = new MediaInfo[Medias.Length];
            for (int i = 0; i < Medias.Length; i++)
            {
                medias[i] = Medias[i].Clone() as MediaInfo;
            }
            return medias;
        }

        /// <summary>
        /// 指定されたインデックスの媒質を取得
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public MediaInfo GetMediaInfo(int index)
        {
            if (index < 0 || index >= Medias.Length)
            {
                return null;
            }
            return (MediaInfo)Medias[index].Clone();
        }

        /// <summary>
        /// 指定されたインデックスの媒質情報を更新
        /// </summary>
        /// <param name="index"></param>
        /// <param name="media"></param>
        public void SetMediaInfo(int index, MediaInfo media)
        {
            if (index < 0 || index >= Medias.Length)
            {
                return;
            }
            Medias[index] = media.Clone() as MediaInfo;
        }

        /// <summary>
        /// Cad操作コマンドを実行する
        /// </summary>
        private void invokeCadOperationCmd()
        {
            // 現在のMementoを生成する
            CadLogicBaseMemento curMmnt = new CadLogicBaseMemento(this, this);
            var cmd = new MyUtilLib.MementoCommand<CadLogicBase, CadLogicBase>(CadLogicBaseMmnt, curMmnt);
            // Note: 第１引数のMementoはコマンドインスタンス内に格納される。第２引数のMementoはMementoのデータが参照されるだけで格納されない
            //       よって、第１引数の破棄の責任はMementoCommandへ移行するが、第２引数は依然こちらの責任となる

            // ここで、再度Cadデータが自分自身にセットされる（mementoでデータ更新するのが本来の使用方法なので)
            bool ret = CmdManager.Invoke(cmd);
            if (!ret)
            {
                MessageBox.Show("状態の最大保存数を超えました。");
            }
            CadLogicBaseMmnt = curMmnt;
            // Note: ここでCadLogicBaseMntが変更されるが、変更される前のインスタンスの破棄責任はMementoCommandへ移行したので破棄処理は必要ない
        }
        
        /// <summary>
        /// 元に戻す
        /// </summary>
        public void Undo()
        {
            CadModeType prevCadMode = CadMode;

            // CadLogicBaseのUndoを実行
            CmdManager.Undo();
            // BUGFIX 
            // 現在の状態をMementoに記憶させる
            setMemento();

            _IsDirty = true;

            if (Change != null)
            {
                Change(this, prevCadMode);
            }
            CadPanel.Refresh();
        }

        /// <summary>
        /// やり直す
        /// </summary>
        public void Redo()
        {
            CadModeType prevCadMode = CadMode;

            // CadLogicBaseのRedoを実行
            CmdManager.Redo();
            // BUGFIX 
            // 現在の状態をMementoに記憶させる
            setMemento();

            _IsDirty = true;

            if (Change != null)
            {
                Change(this, prevCadMode);
            }
            CadPanel.Refresh();
        }

        /// <summary>
        /// 現在の状態をMementoに記憶させる
        /// </summary>
        private void setMemento()
        {
            //   Mementoを生成
            CadLogicBaseMmnt = new CadLogicBaseMemento(this, this);
        }

        /// <summary>
        /// 元に戻す操作が可能か
        /// </summary>
        /// <returns></returns>
        public bool CanUndo()
        {
            return CmdManager.CanUndo();
        }

        /// <summary>
        /// やり直し操作が可能か
        /// </summary>
        /// <returns></returns>
        public bool CanRedo()
        {
            return CmdManager.CanRedo();
        }
    }
}
