using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
namespace PhCFem
{
    /// <summary>
    /// Cadデータファイルの読み書き
    /// </summary>
    class CadDatFile
    {
        /////////////////////////////////////////////////////////////////////////////
        // 定数
        /////////////////////////////////////////////////////////////////////////////

         /////////////////////////////////////////////////////////////////////////////
         // 型
         /////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// 図面情報を保存する
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="AreaSelection"></param>
        /// <param name="EdgeList"></param>
        /// <param name="IncidentPortNo"></param>
        /// <param name="Medias"></param>
        /// <param name="ndivForOneLattice"></param>
        /// <param name="rodRadiusRatio"></param>
        /// <param name="rodCircleDiv"></param>
        /// <param name="rodRadiusDiv"></param>
        public static void SaveToFile(
            string filename,
            CadLogic.CellType[,] AreaSelection,
            IList<Edge> EdgeList,
            int IncidentPortNo,
            MediaInfo[] Medias,
            int ndivForOneLattice,
            double rodRadiusRatio,
            int rodCircleDiv,
            int rodRadiusDiv
            )
        {
            Size MaxDiv = Constants.MaxDiv;
            try
            {
                using (StreamWriter sw = new StreamWriter(filename))
                {
                    int counter;
                    string line;
                    
                    // 領域: 書き込む個数の計算
                    counter = 0;
                    for (int y = 0; y < MaxDiv.Height; y++)
                    {
                        for (int x = 0; x < MaxDiv.Width; x++)
                        {
                            if (AreaSelection[y, x] != CadLogic.CellType.Empty)
                            {
                                counter++;
                            }
                        }
                    }
                    // 領域: 書き込み
                    sw.WriteLine("AreaSelection,{0}", counter);
                    for (int y = 0; y < MaxDiv.Height; y++)
                    {
                        for (int x = 0; x < MaxDiv.Width; x++)
                        {
                            CadLogic.CellType cellType = AreaSelection[y, x];
                            if (cellType != CadLogic.CellType.Empty)
                            {
                                string cellTypeStr = CadLogic.GetCellTypeStr(cellType);
                                sw.WriteLine("{0},{1},{2}", x, y, cellTypeStr);
                            }
                        }
                    }
                    // ポート境界: 書き込み個数の計算
                    sw.WriteLine("EdgeList,{0}", EdgeList.Count);
                    // ポート境界: 書き込み
                    foreach (Edge edge in EdgeList)
                    {
                        sw.WriteLine("{0},{1},{2},{3},{4}", edge.No, edge.Points[0].X, edge.Points[0].Y, edge.Points[1].X, edge.Points[1].Y);
                    }
                    // 入射ポート番号
                    sw.WriteLine("IncidentPortNo,{0}", IncidentPortNo);
                    //////////////////////////////////////////
                    //// Ver1.1.0.0からの追加情報
                    //////////////////////////////////////////
                    // 媒質情報の個数
                    sw.WriteLine("Medias,{0}", Medias.Length);
                    // 媒質情報の書き込み
                    for(int i = 0; i < Medias.Length; i++)
                    {
                        MediaInfo media = Medias[i];
                        line = string.Format("{0},", i);
                        double[,] p = media.P;
                        for (int m = 0; m < p.GetLength(0); m++)
                        {
                            for (int n = 0; n < p.GetLength(1); n++)
                            {
                                line += string.Format("{0},", p[m, n]);
                            }
                        }
                        double[,] q = media.Q;
                        for (int m = 0; m < q.GetLength(0); m++)
                        {
                            for (int n = 0; n < q.GetLength(1); n++)
                            {
                                line += string.Format("{0},", q[m, n]);
                            }
                        }
                        line = line.Remove(line.Length - 1); // 最後の,を削除
                        sw.WriteLine(line);
                    }
                    // 格子１辺の分割数
                    sw.WriteLine("ndivForOneLattice,{0}", ndivForOneLattice);
                    // ロッドの半径の割合
                    sw.WriteLine("rodRadiusRatio,{0}", rodRadiusRatio);
                    // ロッドの円周方向分割数
                    sw.WriteLine("rodCircleDiv,{0}", rodCircleDiv);
                    // ロッドの半径方向分割数
                    sw.WriteLine("rodRadiusDiv,{0}", rodRadiusDiv);
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
                MessageBox.Show(exception.Message);
            }
        }

        /// <summary>
        /// 図面情報を読み込む
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="areaSelection"></param>
        /// <param name="edgeList"></param>
        /// <param name="yBoundarySelection">２次的な情報(edgeListから生成される)</param>
        /// <param name="xBoundarySelection">２次的な情報(edgeListから生成される)</param>
        /// <param name="incidentPortNo"></param>
        /// <param name="medias"></param>
        /// <param name="ndivForOneLattice"></param>
        /// <param name="rodRadiusRatio"></param>
        /// <param name="rodCircleDiv"></param>
        /// <param name="rodRadiusDiv"></param>
        /// <returns></returns>
        public static bool LoadFromFile(
            string filename,
            ref CadLogic.CellType[,] AreaSelection,
            ref IList<Edge> EdgeList,
            ref bool[,] YBoundarySelection,
            ref bool[,] XBoundarySelection,
            ref int IncidentPortNo,
            ref MediaInfo[] Medias,
            ref int ndivForOneLattice,
            ref double rodRadiusRatio,
            ref int rodCircleDiv,
            ref int rodRadiusDiv
            )
        {
            bool success = false;

            Size MaxDiv = Constants.MaxDiv;
            int MaxMediaCount = Medias.Length;

            for (int y = 0; y < AreaSelection.GetLength(0); y++)
            {
                for (int x = 0; x < AreaSelection.GetLength(1); x++)
                {
                    AreaSelection[y, x] = CadLogic.CellType.Empty;
                }
            }

            try
            {
                using (StreamReader sr = new StreamReader(filename))
                {
                    string line;
                    string[] tokens;
                    const char delimiter = ',';
                    int cnt = 0;

                    // 領域選択
                    line = sr.ReadLine();
                    tokens = line.Split(delimiter);
                    if (tokens[0] != "AreaSelection")
                    {
                        MessageBox.Show("領域選択情報がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return success;
                    }
                    cnt = int.Parse(tokens[1]);
                    for (int i = 0; i < cnt; i++)
                    {
                        line = sr.ReadLine();
                        tokens = line.Split(delimiter);
                        if (tokens.Length != 3)
                        {
                            MessageBox.Show("領域選択情報が不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return success;
                        }
                        int x = int.Parse(tokens[0]);
                        int y = int.Parse(tokens[1]);
                        CadLogic.CellType cellType = CadLogic.GetCellTypeFromStr(tokens[2]);
                        if ((x >= 0 && x < MaxDiv.Width) && (y >= 0 && y < MaxDiv.Height))
                        {
                            AreaSelection[y, x] = cellType;
                        }
                        else
                        {
                            MessageBox.Show("領域選択座標値が不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return success;
                        }
                    }

                    // ポート境界
                    line = sr.ReadLine();
                    tokens = line.Split(delimiter);
                    if (tokens[0] != "EdgeList")
                    {
                        MessageBox.Show("境界選択情報がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return success;
                    }
                    cnt = int.Parse(tokens[1]);
                    for (int i = 0; i < cnt; i++)
                    {
                        line = sr.ReadLine();
                        tokens = line.Split(delimiter);
                        if (tokens.Length != 5)
                        {
                            MessageBox.Show("境界選択情報が不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return success;
                        }
                        int edgeNo = int.Parse(tokens[0]);
                        Point[] p = new Point[2];
                        for (int k = 0; k < p.Length; k++)
                        {
                            p[k] = new Point();
                            p[k].X = int.Parse(tokens[1 + k * 2]);
                            p[k].Y = int.Parse(tokens[1 + k * 2 + 1]);

                        }
                        Size delta = new Size(0, 0);
                        if (p[0].X == p[1].X)
                        {
                            // Y方向境界
                            delta = new Size(0, 1);
                        }
                        else if (p[0].Y == p[1].Y)
                        {
                            // X方向境界
                            delta = new Size(1, 0);
                        }
                        else
                        {
                            MessageBox.Show("境界選択情報が不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return success;
                        }
                        Edge edge = new Edge(delta);
                        edge.No = edgeNo;
                        edge.Set(p[0], p[1]);
                        EdgeList.Add(edge);
                    }

                    foreach (Edge edge in EdgeList)
                    {
                        if (edge.Delta.Width == 0)
                        {
                            // Y方向境界
                            int x = edge.Points[0].X;
                            int sty = edge.Points[0].Y;
                            int edy = edge.Points[1].Y;
                            for (int y = sty; y < edy; y++)
                            {
                                YBoundarySelection[y, x] = true;
                            }
                        }
                        else if (edge.Delta.Height == 0)
                        {
                            // X方向境界
                            int y = edge.Points[0].Y;
                            int stx = edge.Points[0].X;
                            int edx = edge.Points[1].X;
                            for (int x = stx; x < edx; x++)
                            {
                                XBoundarySelection[y, x] = true;
                            }
                        }
                        else
                        {
                            MessageBox.Show("Not implemented");
                        }
                    }

                    line = sr.ReadLine();
                    if (line.Length == 0)
                    {
                        MessageBox.Show("入射ポート番号がありません");
                        return success;
                    }
                    tokens = line.Split(delimiter);
                    if (tokens[0] != "IncidentPortNo")
                    {
                        MessageBox.Show("入射ポート番号がありません");
                        return success;
                    }
                    IncidentPortNo = int.Parse(tokens[1]);

                    line = sr.ReadLine();
                    // 媒質情報？
                    tokens = line.Split(delimiter);
                    if (tokens[0] != "Medias")
                    {
                        MessageBox.Show("媒質情報がありません");
                        return success;
                    }
                    cnt = int.Parse(tokens[1]);
                    if (cnt > MaxMediaCount)
                    {
                        MessageBox.Show("媒質情報の個数が不正です");
                        return success;
                    }
                    for (int i = 0; i < cnt; i++)
                    {
                        line = sr.ReadLine();
                        if (line.Length == 0)
                        {
                            MessageBox.Show("媒質情報が不正です");
                            return success;
                        }
                        tokens = line.Split(delimiter);
                        if (tokens.Length != 1 + 9 + 9)
                        {
                            MessageBox.Show("媒質情報が不正です");
                            return success;
                        }
                        int mediaIndex = int.Parse(tokens[0]);
                        System.Diagnostics.Debug.Assert(mediaIndex == i);

                        double[,] p = new double[3, 3];
                        for (int m = 0; m < p.GetLength(0); m++)
                        {
                            for (int n = 0; n < p.GetLength(1); n++)
                            {
                                p[m, n] = double.Parse(tokens[1 + m * p.GetLength(1) + n]);
                            }
                        }
                        Medias[i].SetP(p);

                        double[,] q = new double[3, 3];
                        for (int m = 0; m < q.GetLength(0); m++)
                        {
                            for (int n = 0; n < q.GetLength(1); n++)
                            {
                                q[m, n] = double.Parse(tokens[1 + 9 + m * q.GetLength(1) + n]);
                            }
                        }
                        Medias[i].SetQ(q);
                    }
                    line = sr.ReadLine();
                    if (line == null)
                    {
                        MessageBox.Show("格子１辺の分割数がありません");
                        return success;
                    }
                    tokens = line.Split(delimiter);
                    if (tokens.Length != 2 || tokens[0] != "ndivForOneLattice")
                    {
                        MessageBox.Show("格子１辺の分割数がありません");
                        return success;
                    }
                    ndivForOneLattice = int.Parse(tokens[1]);
                    
                    line = sr.ReadLine();
                    if (line == null)
                    {
                        MessageBox.Show("ロッドの半径がありません");
                        return success;
                    }
                    tokens = line.Split(delimiter);
                    if (tokens.Length != 2 || tokens[0] != "rodRadiusRatio")
                    {
                        MessageBox.Show("ロッドの半径がありません");
                        return success;
                    }
                    rodRadiusRatio = double.Parse(tokens[1]);

                    line = sr.ReadLine();
                    if (line == null)
                    {
                        MessageBox.Show("ロッドの円周方向の分割数がありません");
                        return success;
                    }
                    tokens = line.Split(delimiter);
                    if (tokens.Length != 2 || tokens[0] != "rodCircleDiv")
                    {
                        MessageBox.Show("ロッドの円周方向の分割数がありません");
                        return success;
                    }
                    rodCircleDiv = int.Parse(tokens[1]);

                    line = sr.ReadLine();
                    tokens = line.Split(delimiter);
                    if (tokens.Length != 2 || tokens[0] != "rodRadiusDiv")
                    {
                        MessageBox.Show("ロッドの半径方向の分割数がありません");
                        return success;
                    }
                    rodRadiusDiv = int.Parse(tokens[1]);

                }
                success = true;
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
                MessageBox.Show(exception.Message);
            }

            return success;
        }
    }
}
