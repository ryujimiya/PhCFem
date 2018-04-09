using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace PhCFem
{
    /// <summary>
    /// 要素分割のロジックをここにまとめます
    /// </summary>
    class FemMeshLogic
    {
        /// <summary>
        /// ２次三角形要素メッシュを作成する
        /// </summary>
        /// <param name="ndivForOneLattice">格子1辺の分割数</param>
        /// <param name="rodRadiusRatio">ロッドの半径割合</param>
        /// <param name="rodCircleDiv">ロッドの円周方向分割数</param>
        /// <param name="rodRadiusDiv">ロッドの半径方向分割数(1でもメッシュサイズが小さければ複数に分割される)</param>
        /// <param name="maxDiv">図面の領域サイズ</param>
        /// <param name="areaSelection">マス目選択フラグ配列</param>
        /// <param name="edgeList">ポート境界リスト</param>
        /// <param name="doubleCoords">[OUT]座標リスト（このリストのインデックス+1が節点番号として扱われます)</param>
        /// <param name="elements">[OUT]要素データリスト 要素データはint[] = { 要素番号, 媒質インデックス, 節点番号1, 節点番号2, ...}</param>
        /// <param name="portList">[OUT]ポート節点リストのリスト</param>
        /// <param name="forceBCNodeNumbers">強制境界節点配列</param>
        /// <param name="elemNoPeriodicList">周期領域の要素番号リスト（ポート - 要素番号)</param>
        /// <param name="nodePeriodicBList">周期領域境界の節点番号リスト(ポート - 周期領域境界 - 節点番号)</param>
        /// <param name="defectNodePeriodicList">周期領域の欠陥部の節点番号リスト(ポート- 節点番号)</param>
        /// <returns></returns>
        public static bool MkTriMeshSecondOrder(
            int ndivForOneLattice,
            double rodRadiusRatio,
            int rodCircleDiv,
            int rodRadiusDiv,
            Size maxDiv,
            CadLogic.CellType[,] areaSelection,
            IList<Edge> edgeList,
            out IList<double[]> doubleCoords,
            out IList<int[]> elements,
            out IList<IList<int>> portList,
            out int[] forceBCNodeNumbers,
            out IList<IList<uint>> elemNoPeriodicList,
            out IList<IList<IList<int>>> nodePeriodicBList,
            out IList<IList<int>> defectNodePeriodicList
            )
        {
            // セルのメッシュを取得する
            //  欠陥部セル
            double[,] ptsInCell_Defect = null;
            uint[,] nodeBInCell_Defect = null;
            uint[,] elemNodeInCell_Defect = null;
            WgMesh.GetCellMesh_Defect_TriSecondOrder(
                ndivForOneLattice,
                out ptsInCell_Defect,
                out nodeBInCell_Defect,
                out elemNodeInCell_Defect
                );
            // 誘電体ロッドセル
            double[,] ptsInCell_Rod = null;
            uint[,] nodeBInCell_Rod = null;
            uint[] elemLoopIdInCell_Rod = null;
            uint rodLoopId = 0;
            uint[,] elemNodeInCell_Rod = null;
            WgMesh.GetCellMesh_Rod_TriSecondOrder(
                ndivForOneLattice,
                rodRadiusRatio,
                rodCircleDiv,
                rodRadiusDiv,
                out ptsInCell_Rod,
                out nodeBInCell_Rod,
                out elemLoopIdInCell_Rod,
                out rodLoopId,
                out elemNodeInCell_Rod
                );

            elements = new List<int[]>(); // 要素リスト

            // 座標 - 節点番号対応マップ
            Dictionary<string, uint> coordToNo = new Dictionary<string, uint>();
            // セル→節点番号リストマップ
            uint[,][] nodeNumbersCellList = new uint[maxDiv.Height, maxDiv.Width][];
            // セル→要素番号リストマップ
            uint[,][] elemNoCellList = new uint[maxDiv.Height, maxDiv.Width][];
            // 節点座標
            IList<double[]> coords = new List<double[]>();
            // 強制境界
            Dictionary<int, bool> forceBCNodeNumberDic = new Dictionary<int, bool>();

            int nodeCounter = 0; // 節点番号カウンター
            int elementCounter = 0; // 要素番号カウンター

            for (int x = 0; x < maxDiv.Width; x++)
            {
                for (int y = 0; y < maxDiv.Height; y++)
                {
                    if (areaSelection[y, x] != CadLogic.CellType.Empty)
                    {
                        CadLogic.CellType workCellType = areaSelection[y, x];
                        double[,] work_ptsInCell = null;
                        uint[,] work_nodeBInCell = null;
                        uint[] work_elemLoopIdInCell = null;
                        uint work_rodLoopId = 0;
                        uint[,] work_elemNodeInCell = null;
                        if (workCellType == CadLogic.CellType.Rod)
                        {
                            work_ptsInCell = ptsInCell_Rod;
                            work_nodeBInCell = nodeBInCell_Rod;
                            work_elemLoopIdInCell = elemLoopIdInCell_Rod;
                            work_rodLoopId = rodLoopId;
                            work_elemNodeInCell = elemNodeInCell_Rod;
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(workCellType == CadLogic.CellType.Defect);
                            work_ptsInCell = ptsInCell_Defect;
                            work_nodeBInCell = nodeBInCell_Defect;
                            work_elemLoopIdInCell = null;
                            work_rodLoopId = 0;
                            work_elemNodeInCell = elemNodeInCell_Defect;
                        }

                        // 全体節点番号
                        int nodeCntInCell = work_ptsInCell.GetLength(0);
                        // セル内の節点の座標
                        double[][] pps = new double[nodeCntInCell][];
                        for (int ino = 0; ino < pps.Length; ino++)
                        {
                            pps[ino] = new double[2];
                            pps[ino][0] = work_ptsInCell[ino, 0] + x;
                            pps[ino][1] = work_ptsInCell[ino, 1] + y;
                        }
                        // セル内の節点の節点番号
                        uint[] nodeNumbers = new uint[nodeCntInCell];
                        for (int ino = 0; ino < nodeCntInCell; ino++)
                        {
                            double[] pp = pps[ino];
                            double xx = pp[0];
                            double yy = pp[1];
                            string coordStr = string.Format("{0:F06}_{1:F06}", xx, yy);
                            uint nodeNumber = 0;
                            if (coordToNo.ContainsKey(coordStr))
                            {
                                // 追加済み
                                nodeNumber = coordToNo[coordStr];
                            }
                            else
                            {
                                nodeNumber = (uint)(++nodeCounter);
                                coordToNo.Add(coordStr, nodeNumber);
                                coords.Add(new double[2] { xx, yy });
                                System.Diagnostics.Debug.Assert(coords.Count == nodeNumber);
                            }
                            System.Diagnostics.Debug.Assert(nodeNumber != 0);
                            // セル内の節点の全体節点番号
                            nodeNumbers[ino] = nodeNumber;
                        }
                        nodeNumbersCellList[y, x] = nodeNumbers;

                        // 強制境界判定
                        {
                            int boundaryIndex = -1;
                            if (x == 0 || (x >= 1 && areaSelection[y, x - 1] == CadLogic.CellType.Empty))
                            {
                                // 左の境界
                                boundaryIndex = 0;
                            }
                            else if (x == maxDiv.Width - 1 || (x <= maxDiv.Width - 2 && areaSelection[y, x + 1] == CadLogic.CellType.Empty))
                            {
                                // 右の境界
                                boundaryIndex = 1;
                            }
                            else if (y == 0 || (y >= 1 && areaSelection[y - 1, x] == CadLogic.CellType.Empty))
                            {
                                // 下の境界
                                boundaryIndex = 2;
                            }
                            else if (y == maxDiv.Height - 1 || (y <= maxDiv.Height - 2 && areaSelection[y + 1, x] == CadLogic.CellType.Empty))
                            {
                                // 上の境界
                                boundaryIndex = 3;
                            }
                            if (boundaryIndex != -1)
                            {
                                int nodeCntB = work_nodeBInCell.GetLength(1);
                                uint[] work_no_B = new uint[nodeCntB];
                                for (int ino = 0; ino < nodeCntB; ino++)
                                {
                                    work_no_B[ino] = work_nodeBInCell[boundaryIndex, ino];
                                }
                                for (int ino = 0; ino < nodeCntB; ino++)
                                {
                                    uint noCell = work_no_B[ino];
                                    uint nodeNumber = nodeNumbers[noCell];
                                    if (!forceBCNodeNumberDic.ContainsKey((int)nodeNumber))
                                    {
                                        forceBCNodeNumberDic.Add((int)nodeNumber, true);
                                        double[] coord = coords[(int)nodeNumber - 1];
                                        //System.Diagnostics.Debug.WriteLine("Force: (B{0}) {1} : {2}, {3}", boundaryIndex, nodeNumber, coord[0], coord[1]);
                                    }
                                }
                            }
                        }
                        int elemCntInCell = work_elemNodeInCell.GetLength(0);
                        elemNoCellList[y, x] = new uint[elemCntInCell];
                        for (int ie = 0; ie < elemCntInCell; ie++)
                        {
                            uint[] workNodeElem = new uint[Constants.TriNodeCnt_SecondOrder];
                            for (int ino = 0; ino < Constants.TriNodeCnt_SecondOrder; ino++)
                            {
                                uint noCell = work_elemNodeInCell[ie, ino];
                                uint nodeNumber = nodeNumbers[noCell];
                                workNodeElem[ino] = nodeNumber;
                            }
                            // 媒質
                            int mediaIndex = 0;
                            if (workCellType == CadLogic.CellType.Rod)
                            {
                                uint workLoopId = work_elemLoopIdInCell[ie];
                                if (workLoopId == rodLoopId)
                                {
                                    mediaIndex = 1;
                                }
                            }
                            // 要素追加
                            int elemNo = ++elementCounter;
                            elements.Add(new int[] 
                                {
                                    elemNo,
                                    mediaIndex,
                                    (int)workNodeElem[0], (int)workNodeElem[1], (int)workNodeElem[2],
                                    (int)workNodeElem[3], (int)workNodeElem[4], (int)workNodeElem[5]
                                });
                            elemNoCellList[y, x][ie] = (uint)elemNo;
                        }
                    }
                }
            }

            // ポート境界
            int portCounter = 0;
            portList = new List<IList<int>>();
            elemNoPeriodicList = new List<IList<uint>>();
            nodePeriodicBList = new List<IList<IList<int>>>();
            defectNodePeriodicList = new List<IList<int>>();

            foreach (Edge edge in edgeList)
            {
                //System.Diagnostics.Debug.WriteLine("--------------");
                portCounter++;
                System.Diagnostics.Debug.Assert(edge.No == portCounter);
                // ポート境界
                IList<int> portNodes = null;
                // 周期構造領域
                IList<uint> elemNoPeriodic = new List<uint>();
                IList<IList<int>> nodePeriodicB = new List<IList<int>>();
                IList<int> defectNodePeriodic = new List<int>();

                if (edge.Delta.Width == 0)
                {
                    // 1次線要素
                    int xx = edge.Points[0].X;
                    int sty = edge.Points[0].Y;
                    int edy = edge.Points[1].Y;
                    int nodeCnt = coords.Count;
                    IList<uint> workPortNodes = new List<uint>();
                    IList<double> workYs = new List<double>();
                    for (int ino = 0; ino < nodeCnt; ino++)
                    {
                        double[] coord = coords[ino];
                        if (Math.Abs(coord[0] - xx) < Constants.PrecisionLowerLimit
                            && coord[1] >= sty - Constants.PrecisionLowerLimit && coord[1] <= edy + Constants.PrecisionLowerLimit)
                        {
                            uint nodeNumber = (uint)ino + 1;
                            workPortNodes.Add(nodeNumber);
                            workYs.Add(coord[1]);
                        }
                    }
                    int portNodeCnt = workPortNodes.Count;
                    double[] workYAry = workYs.ToArray();
                    Array.Sort(workYAry);
                    int[] portNodeAry = new int[portNodeCnt];
                    for (int i = 0; i < portNodeCnt; i++)
                    {
                        double y = workYAry[i];
                        int orgIndex = workYs.IndexOf(y);
                        uint nodeNumber = workPortNodes[orgIndex];
                        portNodeAry[i] = (int)nodeNumber;

                        double[] coord = coords[(int)nodeNumber - 1];
                        //System.Diagnostics.Debug.WriteLine("portNode: {0}: {1}, {2}", nodeNumber, coord[0], coord[1]);
                    }
                    portNodes = portNodeAry.ToList();

                    // 周期構造1周期領域
                    bool isLeftOuterBoundary = true; // 左側が外部境界
                    int xxPeriodic = xx; // 左境界の場合
                    if (xx == (maxDiv.Width + 1) || (xx >= 1 && areaSelection[sty, xx - 1] != CadLogic.CellType.Empty))
                    {
                        // 右側が外部境界の場合
                        xxPeriodic = xx - 1;
                        isLeftOuterBoundary = false;
                    }
                    // 周期構造領域の要素番号
                    for (int y = sty; y < edy; y++)
                    {
                        uint[] workElemNoList = elemNoCellList[y, xxPeriodic];
                        if (workElemNoList == null)
                        {
                            continue; // 全体領域でセルが空の場所
                        }
                        foreach (uint elemNo in workElemNoList)
                        {
                            elemNoPeriodic.Add(elemNo);
                        }
                    }

                    // 周期構造領域境界の節点番号
                    //   外部境界
                    IList<int> workNodesB_Outer = portNodes;
                    //   内部境界
                    IList<int> workNodesB_Inner = null;
                    {
                        int tagtxx = xx + 1; // 右側境界
                        if (!isLeftOuterBoundary)
                        {
                            // 右側が外部境界
                            tagtxx = xx - 1; // 左側境界
                        }
                        IList<int> workNodesB = null;
                        IList<uint> workPeriodicBNodes = new List<uint>();
                        IList<double> workPeriodicBYs = new List<double>();
                        for (int ino = 0; ino < nodeCnt; ino++)
                        {
                            double[] coord = coords[ino];
                            if (Math.Abs(coord[0] - tagtxx) < Constants.PrecisionLowerLimit
                                && coord[1] >= sty - Constants.PrecisionLowerLimit && coord[1] <= edy + Constants.PrecisionLowerLimit)
                            {
                                uint nodeNumber = (uint)ino + 1;
                                workPeriodicBNodes.Add(nodeNumber);
                                workPeriodicBYs.Add(coord[1]);
                            }
                        }
                        int periodicBNodeCnt = workPeriodicBNodes.Count;
                        double[] workPeriodicBYAry = workPeriodicBYs.ToArray();
                        Array.Sort(workPeriodicBYAry);
                        int[] periodicBNodeAry = new int[periodicBNodeCnt];
                        for (int i = 0; i < periodicBNodeCnt; i++)
                        {
                            double y = workPeriodicBYAry[i];
                            int orgIndex = workPeriodicBYs.IndexOf(y);
                            uint nodeNumber = workPeriodicBNodes[orgIndex];
                            periodicBNodeAry[i] = (int)nodeNumber;

                            int portIndex = portCounter - 1;
                            double[] coord = coords[(int)nodeNumber - 1];
                            //System.Diagnostics.Debug.WriteLine("nodeB_Inner: {0}: {1}: {2}, {3}", portIndex, nodeNumber, coord[0], coord[1]);
                        }
                        workNodesB = periodicBNodeAry.ToList();
                        // 格納
                        workNodesB_Inner = workNodesB;
                    }
                    // 格納
                    nodePeriodicB.Add(workNodesB_Outer);
                    nodePeriodicB.Add(workNodesB_Inner);

                    // 周期構造領域内欠陥部の節点番号
                    double periodicDistance = 1.0;
                    for (int ino = 0; ino < nodeCnt; ino++)
                    {
                        double[] coord = coords[ino];
                        if (coord[0] >= xxPeriodic - Constants.PrecisionLowerLimit && coord[0] <= xxPeriodic + periodicDistance + Constants.PrecisionLowerLimit
                            && coord[1] >= sty - Constants.PrecisionLowerLimit && coord[1] <= edy + Constants.PrecisionLowerLimit)
                        {
                            uint nodeNumber = (uint)ino + 1;
                            {
                                // セルのx, yインデックス
                                int work_intX = (int)Math.Round(coord[0]);
                                int work_intY1 = (int)Math.Round(coord[1]);
                                if (work_intX >= maxDiv.Width)
                                {
                                    // 解析領域の右境界のとき
                                    work_intX--;
                                }
                                if (work_intY1 >= maxDiv.Height)
                                {
                                    // 解析領域の上境界のとき
                                    work_intY1--;
                                }
                                int work_intY2 = work_intY1 - 1;
                                if (work_intY2 < 0)
                                {
                                    work_intY2 = 0;
                                }
                                CadLogic.CellType workCellType1 = areaSelection[work_intY1, work_intX];
                                if (workCellType1 == CadLogic.CellType.Empty
                                    && (work_intX - 1) >= 0 && areaSelection[work_intY1, work_intX - 1] != CadLogic.CellType.Empty)
                                {
                                    // 不連続領域の右境界のとき
                                    work_intX--;
                                    workCellType1 = areaSelection[work_intY1, work_intX];
                                }
                                CadLogic.CellType workCellType2 = areaSelection[work_intY2, work_intX];
                                if (workCellType1 == CadLogic.CellType.Defect || workCellType2 == CadLogic.CellType.Defect)
                                {
                                    defectNodePeriodic.Add((int)nodeNumber);
                                }
                            }
                        }
                    }

                }
                else if (edge.Delta.Height == 0)
                {
                    // 1次線要素
                    int yy = edge.Points[0].Y;
                    int stx = edge.Points[0].X;
                    int edx = edge.Points[1].X;
                    int nodeCnt = coords.Count;
                    IList<uint> workPortNodes = new List<uint>();
                    IList<double> workXs = new List<double>();
                    for (int ino = 0; ino < nodeCnt; ino++)
                    {
                        double[] coord = coords[ino];
                        if (Math.Abs(coord[1] - yy) < Constants.PrecisionLowerLimit
                            && coord[0] >= stx - Constants.PrecisionLowerLimit && coord[0] <= edx + Constants.PrecisionLowerLimit)
                        {
                            uint nodeNumber = (uint)ino + 1;
                            workPortNodes.Add(nodeNumber);
                            workXs.Add(coord[0]);
                        }
                    }
                    int portNodeCnt = workPortNodes.Count;
                    double[] workXAry = workXs.ToArray();
                    Array.Sort(workXAry);
                    int[] portNodeAry = new int[portNodeCnt];
                    for (int i = 0; i < portNodeCnt; i++)
                    {
                        double x = workXAry[i];
                        int orgIndex = workXs.IndexOf(x);
                        uint nodeNumber = workPortNodes[orgIndex];
                        portNodeAry[i] = (int)nodeNumber;

                        double[] coord = coords[(int)nodeNumber - 1];
                        //System.Diagnostics.Debug.WriteLine("portNode: {0}: {1}, {2}", nodeNumber, coord[0], coord[1]);
                    }
                    portNodes = portNodeAry.ToList();

                    // 周期構造1周期領域
                    bool isBottomOuterBoundary = true; // 下側が外側境界?
                    int yyPeriodic = yy; // 下境界の場合
                    if (yy == (maxDiv.Height + 1) || (yy >= 1 && areaSelection[yy - 1, stx] != CadLogic.CellType.Empty))
                    {
                        // 上境界の場合
                        yyPeriodic = yy - 1;
                        isBottomOuterBoundary = false;
                    }
                    // 周期構造領域の要素番号
                    for (int x = stx; x < edx; x++)
                    {
                        uint[] workElemNoList = elemNoCellList[yyPeriodic, x];
                        if (workElemNoList == null)
                        {
                            continue; // 全体領域でセルが空の場所
                        }
                        foreach (uint elemNo in workElemNoList)
                        {
                            elemNoPeriodic.Add(elemNo);
                        }
                    }

                    // 周期構造領域境界の節点番号
                    //  外部境界
                    IList<int> workNodesB_Outer = portNodes;
                    //  内部境界
                    IList<int> workNodesB_Inner = null;
                    {
                        int tagtyy = yy + 1; // 上側境界
                        if (!isBottomOuterBoundary)
                        {
                            // 上側が外部境界のとき
                            tagtyy = yy - 1; // 下側境界
                        }
                        IList<int> workNodesB = null;
                        IList<uint> workPeriodicBNodes = new List<uint>();
                        IList<double> workPeriodicBXs = new List<double>();
                        for (int ino = 0; ino < nodeCnt; ino++)
                        {
                            double[] coord = coords[ino];
                            if (Math.Abs(coord[1] - tagtyy) < Constants.PrecisionLowerLimit
                                && coord[0] >= stx - Constants.PrecisionLowerLimit && coord[0] <= edx + Constants.PrecisionLowerLimit)
                            {
                                uint nodeNumber = (uint)ino + 1;
                                workPeriodicBNodes.Add(nodeNumber);
                                workPeriodicBXs.Add(coord[0]);
                            }
                        }
                        int periodicBNodeCnt = workPeriodicBNodes.Count;
                        double[] workPeriodicBXAry = workPeriodicBXs.ToArray();
                        Array.Sort(workPeriodicBXAry);
                        int[] periodicBNodeAry = new int[periodicBNodeCnt];
                        for (int i = 0; i < periodicBNodeCnt; i++)
                        {
                            double x = workPeriodicBXAry[i];
                            int orgIndex = workPeriodicBXs.IndexOf(x);
                            uint nodeNumber = workPeriodicBNodes[orgIndex];
                            periodicBNodeAry[i] = (int)nodeNumber;

                            int portIndex = portCounter - 1;
                            double[] coord = coords[(int)nodeNumber - 1];
                            //System.Diagnostics.Debug.WriteLine("nodeB_Inner: {0}: {1}: {2}, {3}", portIndex, nodeNumber, coord[0], coord[1]);
                        }
                        workNodesB = periodicBNodeAry.ToList();
                        // 格納
                        workNodesB_Inner = workNodesB;
                    }
                    // 格納
                    nodePeriodicB.Add(workNodesB_Outer);
                    nodePeriodicB.Add(workNodesB_Inner);

                    // 周期構造領域内欠陥部の節点番号
                    double periodicDistance = 1.0;
                    for (int ino = 0; ino < nodeCnt; ino++)
                    {
                        double[] coord = coords[ino];
                        if (coord[1] >= yyPeriodic - Constants.PrecisionLowerLimit && coord[1] <= yyPeriodic + periodicDistance + Constants.PrecisionLowerLimit
                            && coord[0] >= stx - Constants.PrecisionLowerLimit && coord[0] <= edx + Constants.PrecisionLowerLimit)
                        {
                            uint nodeNumber = (uint)ino + 1;
                            {
                                // セルのx, yインデックス
                                int work_intY = (int)Math.Round(coord[1]);
                                int work_intX1 = (int)Math.Round(coord[0]);
                                if (work_intY >= maxDiv.Height)
                                {
                                    // 解析領域の上境界のとき
                                    work_intY--;
                                }
                                if (work_intX1 >= maxDiv.Width)
                                {
                                    // 解析領域の右境界のとき
                                    work_intX1--;
                                }
                                int work_intX2 = work_intX1 - 1;
                                if (work_intX2 < 0)
                                {
                                    work_intX2 = 0;
                                }
                                CadLogic.CellType workCellType1 = areaSelection[work_intY, work_intX1];
                                if (workCellType1 == CadLogic.CellType.Empty
                                    && (work_intY - 1) >= 0 && areaSelection[work_intY - 1, work_intX1] != CadLogic.CellType.Empty)
                                {
                                    // 不連続領域の上境界のとき
                                    work_intY--;
                                    workCellType1 = areaSelection[work_intY, work_intX1];
                                }
                                CadLogic.CellType workCellType2 = areaSelection[work_intY, work_intX2];
                                if (workCellType1 == CadLogic.CellType.Defect || workCellType2 == CadLogic.CellType.Defect)
                                {
                                    defectNodePeriodic.Add((int)nodeNumber);
                                }
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Not implemented");
                }
                /*
                // check
                for (int i = 0; i < portNodes.Count; i++)
                {
                    System.Diagnostics.Debug.Assert(nodePeriodicB[0].Contains(portNodes[i]));
                }
                 */

                portList.Add(portNodes);
                elemNoPeriodicList.Add(elemNoPeriodic);
                nodePeriodicBList.Add(nodePeriodicB);
                defectNodePeriodicList.Add(defectNodePeriodic);
            }

            // 強制境界からポート境界の節点を取り除く
            // ただし、始点と終点は強制境界なので残す
            foreach (IList<int> nodes in portList)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (i != 0 && i != nodes.Count - 1)
                    {
                        int nodeNumber = nodes[i];
                        if (forceBCNodeNumberDic.ContainsKey(nodeNumber))
                        {
                            forceBCNodeNumberDic.Remove(nodeNumber);
                        }
                    }
                }
            }

            // 座標値に変換
            doubleCoords = new List<double[]>();
            for (int i = 0; i < coords.Count; i++)
            {
                double[] coord = coords[i];
                doubleCoords.Add(coord);
            }
            forceBCNodeNumbers = forceBCNodeNumberDic.Keys.ToArray();

            return true;
        }


        /// <summary>
        /// 1次三角形要素メッシュを作成する
        /// </summary>
        /// <param name="ndivForOneLattice">格子1辺の分割数</param>
        /// <param name="rodRadiusRatio">ロッドの半径割合</param>
        /// <param name="rodCircleDiv">ロッドの円周方向分割数</param>
        /// <param name="rodRadiusDiv">ロッドの半径方向分割数(1でもメッシュサイズが小さければ複数に分割される)</param>
        /// <param name="maxDiv">図面の領域サイズ</param>
        /// <param name="areaSelection">マス目選択フラグ配列</param>
        /// <param name="edgeList">ポート境界リスト</param>
        /// <param name="doubleCoords">[OUT]座標リスト（このリストのインデックス+1が節点番号として扱われます)</param>
        /// <param name="elements">[OUT]要素データリスト 要素データはint[] = { 要素番号, 媒質インデックス, 節点番号1, 節点番号2, ...}</param>
        /// <param name="portList">[OUT]ポート節点リストのリスト</param>
        /// <param name="forceBCNodeNumbers">強制境界節点配列</param>
        /// <param name="elemNoPeriodicList">周期領域の要素番号リスト（ポート - 要素番号)</param>
        /// <param name="nodePeriodicBList">周期領域境界の節点番号リスト(ポート - 周期領域境界 - 節点番号)</param>
        /// <param name="defectNodePeriodicList">周期領域の欠陥部の節点番号リスト(ポート- 節点番号)</param>
        /// <returns></returns>
        public static bool MkTriMeshFirstOrder(
            int ndivForOneLattice,
            double rodRadiusRatio,
            int rodCircleDiv,
            int rodRadiusDiv,
            Size maxDiv,
            CadLogic.CellType[,] areaSelection,
            IList<Edge> edgeList,
            out IList<double[]> doubleCoords,
            out IList<int[]> elements,
            out IList<IList<int>> portList,
            out int[] forceBCNodeNumbers,
            out IList<IList<uint>> elemNoPeriodicList,
            out IList<IList<IList<int>>> nodePeriodicBList,
            out IList<IList<int>> defectNodePeriodicList
            )
        {
            // セルのメッシュを取得する
            //  欠陥部セル
            double[,] ptsInCell_Defect = null;
            uint[,] nodeBInCell_Defect = null;
            uint[,] elemNodeInCell_Defect = null;
            WgMesh.GetCellMesh_Defect_TriFirstOrder(
                ndivForOneLattice,
                out ptsInCell_Defect,
                out nodeBInCell_Defect,
                out elemNodeInCell_Defect
                );
            // 誘電体ロッドセル
            double[,] ptsInCell_Rod = null;
            uint[,] nodeBInCell_Rod = null;
            uint[] elemLoopIdInCell_Rod = null;
            uint rodLoopId = 0;
            uint[,] elemNodeInCell_Rod = null;
            WgMesh.GetCellMesh_Rod_TriFirstOrder(
                ndivForOneLattice,
                rodRadiusRatio,
                rodCircleDiv,
                rodRadiusDiv,
                out ptsInCell_Rod,
                out nodeBInCell_Rod,
                out elemLoopIdInCell_Rod,
                out rodLoopId,
                out elemNodeInCell_Rod
                );

            elements = new List<int[]>(); // 要素リスト

            // 座標 - 節点番号対応マップ
            Dictionary<string, uint> coordToNo = new Dictionary<string, uint>();
            // セル→節点番号リストマップ
            uint[,][] nodeNumbersCellList = new uint[maxDiv.Height, maxDiv.Width][];
            // セル→要素番号リストマップ
            uint[,][] elemNoCellList = new uint[maxDiv.Height, maxDiv.Width][];
            // 節点座標
            IList<double[]> coords = new List<double[]>();
            // 強制境界
            Dictionary<int, bool> forceBCNodeNumberDic = new Dictionary<int, bool>();
            
            int nodeCounter = 0; // 節点番号カウンター
            int elementCounter = 0; // 要素番号カウンター

            for (int x = 0; x < maxDiv.Width; x++)
            {
                for (int y = 0; y < maxDiv.Height; y++)
                {
                    if (areaSelection[y, x] != CadLogic.CellType.Empty)
                    {
                        CadLogic.CellType workCellType = areaSelection[y, x];
                        double[,] work_ptsInCell = null;
                        uint[,] work_nodeBInCell = null;
                        uint[] work_elemLoopIdInCell = null;
                        uint work_rodLoopId = 0;
                        uint[,] work_elemNodeInCell = null;
                        if (workCellType == CadLogic.CellType.Rod)
                        {
                            work_ptsInCell = ptsInCell_Rod;
                            work_nodeBInCell =nodeBInCell_Rod;
                            work_elemLoopIdInCell = elemLoopIdInCell_Rod;
                            work_rodLoopId = rodLoopId;
                            work_elemNodeInCell = elemNodeInCell_Rod;
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(workCellType == CadLogic.CellType.Defect);
                            work_ptsInCell = ptsInCell_Defect;
                            work_nodeBInCell =nodeBInCell_Defect;
                            work_elemLoopIdInCell = null;
                            work_rodLoopId = 0;
                            work_elemNodeInCell = elemNodeInCell_Defect;
                        }

                        // 全体節点番号
                        int nodeCntInCell = work_ptsInCell.GetLength(0);
                        // セル内の節点の座標
                        double[][] pps = new double[nodeCntInCell][];
                        for (int ino = 0; ino < pps.Length; ino++)
                        {
                            pps[ino] = new double[2];
                            pps[ino][0] = work_ptsInCell[ino, 0] + x;
                            pps[ino][1] = work_ptsInCell[ino, 1] + y;
                        }
                        // セル内の節点の節点番号
                        uint[] nodeNumbers = new uint[nodeCntInCell];
                        for (int ino = 0; ino < nodeCntInCell; ino++)
                        {
                            double[] pp = pps[ino];
                            double xx = pp[0];
                            double yy = pp[1];
                            string coordStr = string.Format("{0:F06}_{1:F06}", xx, yy);
                            uint nodeNumber = 0;
                            if (coordToNo.ContainsKey(coordStr))
                            {
                                // 追加済み
                                nodeNumber = coordToNo[coordStr];
                            }
                            else
                            {
                                nodeNumber = (uint)(++nodeCounter);
                                coordToNo.Add(coordStr, nodeNumber);
                                coords.Add(new double[2] { xx, yy });
                                System.Diagnostics.Debug.Assert(coords.Count == nodeNumber);
                            }
                            System.Diagnostics.Debug.Assert(nodeNumber != 0);
                            // セル内の節点の全体節点番号
                            nodeNumbers[ino] = nodeNumber;
                        }
                        nodeNumbersCellList[y, x] = nodeNumbers;

                        // 強制境界判定
                        {
                            int boundaryIndex = -1;
                            if (x == 0 || (x >= 1 && areaSelection[y, x - 1] == CadLogic.CellType.Empty))
                            {
                                // 左の境界
                                boundaryIndex = 0;
                            }
                            else if (x == maxDiv.Width - 1 || (x <= maxDiv.Width - 2 && areaSelection[y, x + 1] == CadLogic.CellType.Empty))
                            {
                                // 右の境界
                                boundaryIndex = 1;
                            }
                            else if (y == 0 || (y >= 1 && areaSelection[y - 1, x] == CadLogic.CellType.Empty))
                            {
                                // 下の境界
                                boundaryIndex = 2;
                            }
                            else if (y == maxDiv.Height - 1 || (y <= maxDiv.Height - 2 && areaSelection[y + 1, x] == CadLogic.CellType.Empty))
                            {
                                // 上の境界
                                boundaryIndex = 3;
                            }
                            if (boundaryIndex != -1)
                            {
                                int nodeCntB = work_nodeBInCell.GetLength(1);
                                uint[] work_no_B = new uint[nodeCntB];
                                for (int ino = 0; ino < nodeCntB; ino++)
                                {
                                    work_no_B[ino] = work_nodeBInCell[boundaryIndex, ino];
                                }
                                for (int ino = 0; ino < nodeCntB; ino++)
                                {
                                    uint noCell = work_no_B[ino];
                                    uint nodeNumber = nodeNumbers[noCell];
                                    if (!forceBCNodeNumberDic.ContainsKey((int)nodeNumber))
                                    {
                                        forceBCNodeNumberDic.Add((int)nodeNumber, true);
                                        double[] coord = coords[(int)nodeNumber - 1];
                                        //System.Diagnostics.Debug.WriteLine("Force: (B{0}) {1} : {2}, {3}", boundaryIndex, nodeNumber, coord[0], coord[1]);
                                    }
                                }
                            }
                        }
                        int elemCntInCell = work_elemNodeInCell.GetLength(0);
                        elemNoCellList[y, x] = new uint[elemCntInCell];
                        for (int ie = 0; ie < elemCntInCell; ie++)
                        {
                            uint[] workNodeElem = new uint[Constants.TriVertexCnt];
                            for (int ino = 0; ino < Constants.TriVertexCnt; ino++)
                            {
                                uint noCell = work_elemNodeInCell[ie, ino];
                                uint nodeNumber = nodeNumbers[noCell];
                                workNodeElem[ino] = nodeNumber;
                            }
                            // 媒質
                            int mediaIndex = 0;
                            if (workCellType == CadLogic.CellType.Rod)
                            {
                                uint workLoopId = work_elemLoopIdInCell[ie];
                                if (workLoopId == rodLoopId)
                                {
                                    mediaIndex = 1;
                                }
                            }
                            // 要素追加
                            int elemNo = ++elementCounter;
                            elements.Add(new int[] 
                                {
                                    elemNo,
                                    mediaIndex,
                                    (int)workNodeElem[0], (int)workNodeElem[1], (int)workNodeElem[2]
                                });
                            elemNoCellList[y, x][ie] = (uint)elemNo;
                        }
                    }
                }
            }

            // ポート境界
            int portCounter = 0;
            portList = new List<IList<int>>();
            elemNoPeriodicList = new List<IList<uint>>();
            nodePeriodicBList = new List<IList<IList<int>>>();
            defectNodePeriodicList = new List<IList<int>>();

            foreach (Edge edge in edgeList)
            {
                //System.Diagnostics.Debug.WriteLine("--------------");
                portCounter++;
                System.Diagnostics.Debug.Assert(edge.No == portCounter);
                // ポート境界
                IList<int> portNodes = null;
                // 周期構造領域
                IList<uint> elemNoPeriodic = new List<uint>();
                IList<IList<int>> nodePeriodicB = new List<IList<int>>();
                IList<int> defectNodePeriodic = new List<int>();

                if (edge.Delta.Width == 0)
                {
                    // 1次線要素
                    int xx = edge.Points[0].X;
                    int sty = edge.Points[0].Y;
                    int edy = edge.Points[1].Y;
                    int nodeCnt = coords.Count;
                    IList<uint> workPortNodes = new List<uint>();
                    IList<double> workYs = new List<double>();
                    for (int ino = 0; ino < nodeCnt; ino++)
                    {
                        double[] coord = coords[ino];
                        if (Math.Abs(coord[0] - xx) < Constants.PrecisionLowerLimit
                            && coord[1] >= sty - Constants.PrecisionLowerLimit && coord[1] <= edy + Constants.PrecisionLowerLimit)
                        {
                            uint nodeNumber = (uint)ino + 1;
                            workPortNodes.Add(nodeNumber);
                            workYs.Add(coord[1]);
                        }
                    }
                    int portNodeCnt = workPortNodes.Count;
                    double[] workYAry = workYs.ToArray();
                    Array.Sort(workYAry);
                    int[] portNodeAry = new int[portNodeCnt];
                    for (int i = 0; i < portNodeCnt; i++)
                    {
                        double y = workYAry[i];
                        int orgIndex = workYs.IndexOf(y);
                        uint nodeNumber = workPortNodes[orgIndex];
                        portNodeAry[i] = (int)nodeNumber;

                        double[] coord = coords[(int)nodeNumber - 1];
                        //System.Diagnostics.Debug.WriteLine("portNode: {0}: {1}, {2}", nodeNumber, coord[0], coord[1]);
                    }
                    portNodes = portNodeAry.ToList();

                    // 周期構造1周期領域
                    bool isLeftOuterBoundary = true; // 左側が外部境界
                    int xxPeriodic = xx; // 左境界の場合
                    if (xx == (maxDiv.Width + 1) || (xx >= 1 && areaSelection[sty, xx - 1] != CadLogic.CellType.Empty))
                    {
                        // 右側が外部境界の場合
                        xxPeriodic = xx - 1;
                        isLeftOuterBoundary = false;
                    }
                    // 周期構造領域の要素番号
                    for (int y = sty; y < edy; y++)
                    {
                        uint[] workElemNoList = elemNoCellList[y, xxPeriodic];
                        if (workElemNoList == null)
                        {
                            continue; // 全体領域でセルが空の場所
                        }
                        foreach (uint elemNo in workElemNoList)
                        {
                            elemNoPeriodic.Add(elemNo);
                        }
                    }
                    // 周期構造領域境界の節点番号
                    //   外部境界
                    IList<int> workNodesB_Outer = portNodes;
                    //   内部境界
                    IList<int> workNodesB_Inner = null;
                    {
                        int tagtxx = xx + 1; // 右側境界
                        if (!isLeftOuterBoundary)
                        {
                            // 右側が外部境界
                            tagtxx = xx - 1; // 左側境界
                        }
                        IList<int> workNodesB = null;
                        IList<uint> workPeriodicBNodes = new List<uint>();
                        IList<double> workPeriodicBYs = new List<double>();
                        for (int ino = 0; ino < nodeCnt; ino++)
                        {
                            double[] coord = coords[ino];
                            if (Math.Abs(coord[0] - tagtxx) < Constants.PrecisionLowerLimit
                                && coord[1] >= sty - Constants.PrecisionLowerLimit && coord[1] <= edy + Constants.PrecisionLowerLimit)
                            {
                                uint nodeNumber = (uint)ino + 1;
                                workPeriodicBNodes.Add(nodeNumber);
                                workPeriodicBYs.Add(coord[1]);
                            }
                        }
                        int periodicBNodeCnt = workPeriodicBNodes.Count;
                        double[] workPeriodicBYAry = workPeriodicBYs.ToArray();
                        Array.Sort(workPeriodicBYAry);
                        int[] periodicBNodeAry = new int[periodicBNodeCnt];
                        for (int i = 0; i < periodicBNodeCnt; i++)
                        {
                            double y = workPeriodicBYAry[i];
                            int orgIndex = workPeriodicBYs.IndexOf(y);
                            uint nodeNumber = workPeriodicBNodes[orgIndex];
                            periodicBNodeAry[i] = (int)nodeNumber;

                            int portIndex = portCounter - 1;
                            double[] coord = coords[(int)nodeNumber - 1];
                            //System.Diagnostics.Debug.WriteLine("nodeB_Inner: {0}: {1}: {2}, {3}", portIndex, nodeNumber, coord[0], coord[1]);
                        }
                        workNodesB = periodicBNodeAry.ToList();
                        // 格納
                        workNodesB_Inner = workNodesB;
                    }
                    // 格納
                    nodePeriodicB.Add(workNodesB_Outer);
                    nodePeriodicB.Add(workNodesB_Inner);

                    // 周期構造領域内欠陥部の節点番号
                    double periodicDistance = 1.0;
                    for (int ino = 0; ino < nodeCnt; ino++)
                    {
                        double[] coord = coords[ino];
                        if (coord[0] >= xxPeriodic - Constants.PrecisionLowerLimit && coord[0] <= xxPeriodic + periodicDistance + Constants.PrecisionLowerLimit
                            && coord[1] >= sty - Constants.PrecisionLowerLimit && coord[1] <= edy + Constants.PrecisionLowerLimit)
                        {
                            uint nodeNumber = (uint)ino + 1;
                            {
                                // セルのx, yインデックス
                                int work_intX = (int)Math.Round(coord[0]);
                                int work_intY1 = (int)Math.Round(coord[1]);
                                if (work_intX >= maxDiv.Width)
                                {
                                    // 解析領域の右境界のとき
                                    work_intX--;
                                }
                                if (work_intY1 >= maxDiv.Height)
                                {
                                    // 解析領域の上境界のとき
                                    work_intY1--;
                                }
                                int work_intY2 = work_intY1 - 1;
                                if (work_intY2 < 0)
                                {
                                    work_intY2 = 0;
                                }
                                CadLogic.CellType workCellType1 = areaSelection[work_intY1, work_intX];
                                if (workCellType1 == CadLogic.CellType.Empty
                                    && (work_intX - 1) >= 0 && areaSelection[work_intY1, work_intX - 1] != CadLogic.CellType.Empty)
                                {
                                    // 不連続領域の右境界のとき
                                    work_intX--;
                                    workCellType1 = areaSelection[work_intY1, work_intX];
                                }
                                CadLogic.CellType workCellType2 = areaSelection[work_intY2, work_intX];
                                if (workCellType1 == CadLogic.CellType.Defect || workCellType2 == CadLogic.CellType.Defect)
                                {
                                    defectNodePeriodic.Add((int)nodeNumber);
                                }
                            }
                        }
                    }

                }
                else if (edge.Delta.Height == 0)
                {
                    // 1次線要素
                    int yy = edge.Points[0].Y;
                    int stx = edge.Points[0].X;
                    int edx = edge.Points[1].X;
                    int nodeCnt = coords.Count;
                    IList<uint> workPortNodes = new List<uint>();
                    IList<double> workXs = new List<double>();
                    for (int ino = 0; ino < nodeCnt; ino++)
                    {
                        double[] coord = coords[ino];
                        if (Math.Abs(coord[1] - yy) < Constants.PrecisionLowerLimit
                            && coord[0] >= stx - Constants.PrecisionLowerLimit && coord[0] <= edx + Constants.PrecisionLowerLimit)
                        {
                            uint nodeNumber = (uint)ino + 1;
                            workPortNodes.Add(nodeNumber);
                            workXs.Add(coord[0]);
                        }
                    }
                    int portNodeCnt = workPortNodes.Count;
                    double[] workXAry = workXs.ToArray();
                    Array.Sort(workXAry);
                    int[] portNodeAry = new int[portNodeCnt];
                    for (int i = 0; i < portNodeCnt; i++)
                    {
                        double x = workXAry[i];
                        int orgIndex = workXs.IndexOf(x);
                        uint nodeNumber = workPortNodes[orgIndex];
                        portNodeAry[i] = (int)nodeNumber;

                        double[] coord = coords[(int)nodeNumber - 1];
                        //System.Diagnostics.Debug.WriteLine("portNode: {0}: {1}, {2}", nodeNumber, coord[0], coord[1]);
                    }
                    portNodes = portNodeAry.ToList();

                    // 周期構造1周期領域
                    bool isBottomOuterBoundary = true; // 下側が外側境界?
                    int yyPeriodic = yy; // 下境界の場合
                    if (yy == (maxDiv.Height + 1) || (yy >= 1 && areaSelection[yy - 1, stx] != CadLogic.CellType.Empty))
                    {
                        // 上境界の場合
                        yyPeriodic = yy - 1;
                        isBottomOuterBoundary = false;
                    }
                    // 周期構造領域の要素番号
                    for (int x = stx; x < edx; x++)
                    {
                        uint[] workElemNoList = elemNoCellList[yyPeriodic, x];
                        if (workElemNoList == null)
                        {
                            continue; // 全体領域でセルが空の場所
                        }
                        foreach (uint elemNo in workElemNoList)
                        {
                            elemNoPeriodic.Add(elemNo);
                        }
                    }

                    // 周期構造領域境界の節点番号
                    //  外部境界
                    IList<int> workNodesB_Outer = portNodes;
                    //  内部境界
                    IList<int> workNodesB_Inner = null;
                    {
                        int tagtyy = yy + 1; // 上側境界
                        if (!isBottomOuterBoundary)
                        {
                            // 上側が外部境界のとき
                            tagtyy = yy - 1; // 下側境界
                        }
                        IList<int> workNodesB = null;
                        IList<uint> workPeriodicBNodes = new List<uint>();
                        IList<double> workPeriodicBXs = new List<double>();
                        for (int ino = 0; ino < nodeCnt; ino++)
                        {
                            double[] coord = coords[ino];
                            if (Math.Abs(coord[1] - tagtyy) < Constants.PrecisionLowerLimit
                                && coord[0] >= stx - Constants.PrecisionLowerLimit && coord[0] <= edx + Constants.PrecisionLowerLimit)
                            {
                                uint nodeNumber = (uint)ino + 1;
                                workPeriodicBNodes.Add(nodeNumber);
                                workPeriodicBXs.Add(coord[0]);
                            }
                        }
                        int periodicBNodeCnt = workPeriodicBNodes.Count;
                        double[] workPeriodicBXAry = workPeriodicBXs.ToArray();
                        Array.Sort(workPeriodicBXAry);
                        int[] periodicBNodeAry = new int[periodicBNodeCnt];
                        for (int i = 0; i < periodicBNodeCnt; i++)
                        {
                            double x = workPeriodicBXAry[i];
                            int orgIndex = workPeriodicBXs.IndexOf(x);
                            uint nodeNumber = workPeriodicBNodes[orgIndex];
                            periodicBNodeAry[i] = (int)nodeNumber;

                            int portIndex = portCounter - 1;
                            double[] coord = coords[(int)nodeNumber - 1];
                            //System.Diagnostics.Debug.WriteLine("nodeB_Inner: {0}: {1}: {2}, {3}", portIndex, nodeNumber, coord[0], coord[1]);
                        }
                        workNodesB = periodicBNodeAry.ToList();
                        // 格納
                        workNodesB_Inner = workNodesB;
                    }
                    // 格納
                    nodePeriodicB.Add(workNodesB_Outer);
                    nodePeriodicB.Add(workNodesB_Inner);

                    // 周期構造領域内欠陥部の節点番号
                    double periodicDistance = 1.0;
                    for (int ino = 0; ino < nodeCnt; ino++)
                    {
                        double[] coord = coords[ino];
                        if (coord[1] >= yyPeriodic - Constants.PrecisionLowerLimit && coord[1] <= yyPeriodic + periodicDistance + Constants.PrecisionLowerLimit
                            && coord[0] >= stx - Constants.PrecisionLowerLimit && coord[0] <= edx + Constants.PrecisionLowerLimit)
                        {
                            uint nodeNumber = (uint)ino + 1;
                            {
                                // セルのx, yインデックス
                                int work_intY = (int)Math.Round(coord[1]);
                                int work_intX1 = (int)Math.Round(coord[0]);
                                if (work_intY >= maxDiv.Height)
                                {
                                    // 解析領域の上境界のとき
                                    work_intY--;
                                }
                                if (work_intX1 >= maxDiv.Width)
                                {
                                    // 解析領域の右境界のとき
                                    work_intX1--;
                                }
                                int work_intX2 = work_intX1 - 1;
                                if (work_intX2 < 0)
                                {
                                    work_intX2 = 0;
                                }
                                CadLogic.CellType workCellType1 = areaSelection[work_intY, work_intX1];
                                if (workCellType1 == CadLogic.CellType.Empty
                                     && (work_intY - 1) >= 0 && areaSelection[work_intY - 1, work_intX1] != CadLogic.CellType.Empty)
                               {
                                    // 不連続領域の上境界のとき
                                    work_intY--;
                                    workCellType1 = areaSelection[work_intY, work_intX1];
                                }
                                CadLogic.CellType workCellType2 = areaSelection[work_intY, work_intX2];
                                if (workCellType1 == CadLogic.CellType.Defect || workCellType2 == CadLogic.CellType.Defect)
                                {
                                    defectNodePeriodic.Add((int)nodeNumber);
                                }
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Not implemented");
                }
                /*
                // check
                for (int i = 0; i < portNodes.Count; i++)
                {
                    System.Diagnostics.Debug.Assert(nodePeriodicB[0].Contains(portNodes[i]));
                }
                 */

                portList.Add(portNodes);
                elemNoPeriodicList.Add(elemNoPeriodic);
                nodePeriodicBList.Add(nodePeriodicB);
                defectNodePeriodicList.Add(defectNodePeriodic);
            }

            // 強制境界からポート境界の節点を取り除く
            // ただし、始点と終点は強制境界なので残す
            foreach (IList<int> nodes in portList)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (i != 0 && i != nodes.Count - 1)
                    {
                        int nodeNumber = nodes[i];
                        if (forceBCNodeNumberDic.ContainsKey(nodeNumber))
                        {
                            forceBCNodeNumberDic.Remove(nodeNumber);
                        }
                    }
                }
            }

            // 座標値に変換
            doubleCoords = new List<double[]>();
            for (int i = 0; i < coords.Count; i++)
            {
                double[] coord = coords[i];
                doubleCoords.Add(coord);
            }
            forceBCNodeNumbers = forceBCNodeNumberDic.Keys.ToArray();

            return true;
        }
        
        /// <summary>
        /// 要素の節点数から要素形状区分と補間次数を取得する
        /// </summary>
        /// <param name="eNodeCnt">要素の節点数</param>
        /// <param name="elemShapeDv">要素形状区分</param>
        /// <param name="order">補間次数</param>
        /// <param name="vertexCnt">頂点数</param>
        public static void GetElementShapeDvAndOrderByElemNodeCnt(int eNodeCnt, out Constants.FemElementShapeDV elemShapeDv, out int order, out int vertexCnt)
        {
            elemShapeDv = Constants.FemElementShapeDV.Triangle;
            order = Constants.SecondOrder;
            vertexCnt = Constants.TriVertexCnt;
            if (eNodeCnt == Constants.TriNodeCnt_SecondOrder)
            {
                // ２次三角形
                elemShapeDv = Constants.FemElementShapeDV.Triangle;
                order = Constants.SecondOrder;
                vertexCnt = Constants.TriVertexCnt;
            }
            else if (eNodeCnt == Constants.TriNodeCnt_FirstOrder)
            {
                // １次三角形
                elemShapeDv = Constants.FemElementShapeDV.Triangle;
                order = Constants.FirstOrder;
                vertexCnt = Constants.TriVertexCnt;
            }
            else
            {
                // 未対応
                System.Diagnostics.Debug.Assert(false);
            }
        }

        /// <summary>
        /// 辺と要素の対応マップ作成
        /// </summary>
        /// <param name="in_Elements"></param>
        /// <param name="out_EdgeToElementNoH"></param>
        public static void MkEdgeToElementNoH(IList<FemElement> in_Elements, ref Dictionary<string, IList<int>> out_EdgeToElementNoH)
        {
            if (in_Elements.Count == 0)
            {
                return;
            }
            int eNodeCnt = in_Elements[0].NodeNumbers.Length;
            Constants.FemElementShapeDV elemShapeDv;
            int order;
            int vertexCnt;
            GetElementShapeDvAndOrderByElemNodeCnt(eNodeCnt, out elemShapeDv, out order, out vertexCnt);

            if (elemShapeDv == Constants.FemElementShapeDV.Triangle && order == Constants.SecondOrder)
            {
                // ２次三角形要素
                TriSecondOrderElements_MkEdgeToElementNoH(in_Elements, ref out_EdgeToElementNoH);
            }
            else if (elemShapeDv == Constants.FemElementShapeDV.Triangle && order == Constants.FirstOrder)
            {
                // １次三角形要素
                TriFirstOrderElements_MkEdgeToElementNoH(in_Elements, ref out_EdgeToElementNoH);
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
        }

        /// <summary>
        /// ２次三角形要素：辺と要素の対応マップ作成
        /// </summary>
        /// <param name="in_Elements"></param>
        /// <param name="out_EdgeToElementNoH"></param>
        public static void TriSecondOrderElements_MkEdgeToElementNoH(IList<FemElement> in_Elements, ref Dictionary<string, IList<int>> out_EdgeToElementNoH)
        {
            // 辺と要素の対応マップ作成
            // 2次三角形要素の辺の始点、終点ローカル番号
            int[][] edgeStEdNoList = new int[Constants.TriEdgeCnt_SecondOrder][]
                        {
                            new int[]{1, 4},
                            new int[]{4, 2},
                            new int[]{2, 5},
                            new int[]{5, 3},
                            new int[]{3, 6},
                            new int[]{6, 1}
                        };
            out_EdgeToElementNoH.Clear();
            foreach (FemElement element in in_Elements)
            {
                foreach (int[] edgeStEdNo in edgeStEdNoList)
                {
                    int stNodeNumber = element.NodeNumbers[edgeStEdNo[0] - 1];
                    int edNodeNumber = element.NodeNumbers[edgeStEdNo[1] - 1];
                    string edgeKey = "";
                    if (stNodeNumber < edNodeNumber)
                    {
                        edgeKey = string.Format("{0}_{1}", stNodeNumber, edNodeNumber);
                    }
                    else
                    {
                        edgeKey = string.Format("{0}_{1}", edNodeNumber, stNodeNumber);
                    }
                    if (out_EdgeToElementNoH.ContainsKey(edgeKey))
                    {
                        out_EdgeToElementNoH[edgeKey].Add(element.No);
                    }
                    else
                    {
                        out_EdgeToElementNoH[edgeKey] = new List<int>();
                        out_EdgeToElementNoH[edgeKey].Add(element.No);
                    }
                }
            }
        }

        /// <summary>
        /// １次三角形要素：辺と要素の対応マップ作成
        /// </summary>
        /// <param name="in_Elements"></param>
        /// <param name="out_EdgeToElementNoH"></param>
        public static void TriFirstOrderElements_MkEdgeToElementNoH(IList<FemElement> in_Elements, ref Dictionary<string, IList<int>> out_EdgeToElementNoH)
        {
            // 辺と要素の対応マップ作成
            // 1次三角形要素の辺の始点、終点ローカル番号
            int[][] edgeStEdNoList = new int[Constants.TriEdgeCnt_FirstOrder][]
                        {
                            new int[]{1, 2},
                            new int[]{2, 3},
                            new int[]{3, 1},
                        };
            out_EdgeToElementNoH.Clear();
            foreach (FemElement element in in_Elements)
            {
                foreach (int[] edgeStEdNo in edgeStEdNoList)
                {
                    int stNodeNumber = element.NodeNumbers[edgeStEdNo[0] - 1];
                    int edNodeNumber = element.NodeNumbers[edgeStEdNo[1] - 1];
                    string edgeKey = "";
                    if (stNodeNumber < edNodeNumber)
                    {
                        edgeKey = string.Format("{0}_{1}", stNodeNumber, edNodeNumber);
                    }
                    else
                    {
                        edgeKey = string.Format("{0}_{1}", edNodeNumber, stNodeNumber);
                    }
                    if (out_EdgeToElementNoH.ContainsKey(edgeKey))
                    {
                        out_EdgeToElementNoH[edgeKey].Add(element.No);
                    }
                    else
                    {
                        out_EdgeToElementNoH[edgeKey] = new List<int>();
                        out_EdgeToElementNoH[edgeKey].Add(element.No);
                    }
                }
            }
        }

        /// <summary>
        /// 節点と要素番号のマップ作成
        /// </summary>
        /// <param name="in_Elements"></param>
        /// <param name="out_NodeToElementNoH"></param>
        public static void MkNodeToElementNoH(IList<FemElement> in_Elements, ref Dictionary<int, IList<int>> out_NodeToElementNoH)
        {
            //check
            for (int ieleno = 0; ieleno < in_Elements.Count; ieleno++)
            {
                System.Diagnostics.Debug.Assert(in_Elements[ieleno].No == ieleno + 1);
            }
            // 節点と要素番号のマップ作成
            foreach (FemElement element in in_Elements)
            {
                foreach (int nodeNumber in element.NodeNumbers)
                {
                    if (out_NodeToElementNoH.ContainsKey(nodeNumber))
                    {
                        out_NodeToElementNoH[nodeNumber].Add(element.No);
                    }
                    else
                    {
                        out_NodeToElementNoH[nodeNumber] = new List<int>();
                        out_NodeToElementNoH[nodeNumber].Add(element.No);
                    }
                }
            }
        }

        /// <summary>
        /// 点が要素内に含まれる？
        /// </summary>
        /// <param name="element">要素</param>
        /// <param name="test_pp">テストする点</param>
        /// <param name="Nodes">節点リスト（要素は座標を持たないので節点リストが必要）</param>
        /// <returns></returns>
        public static bool IsPointInElement(FemElement element, double[] test_pp, IList<FemNode> nodes)
        {
            bool hit = false;
            int eNodeCnt = element.NodeNumbers.Length;
            Constants.FemElementShapeDV elemShapeDv;
            int order;
            int vertexCnt;
            GetElementShapeDvAndOrderByElemNodeCnt(eNodeCnt, out elemShapeDv, out order, out vertexCnt);

            if (vertexCnt == Constants.TriVertexCnt)
            {
                // ２次/１次三角形要素
                hit = TriElement_IsPointInElement(element, test_pp, nodes);
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            return hit;
        }

        /// <summary>
        /// 三角形要素：点が要素内に含まれる？
        /// </summary>
        /// <param name="element">要素</param>
        /// <param name="test_pp">テストする点</param>
        /// <param name="Nodes">節点リスト（要素は座標を持たないので節点リストが必要）</param>
        /// <returns></returns>
        public static bool TriElement_IsPointInElement(FemElement element, double[] test_pp, IList<FemNode> nodes)
        {
            bool hit = false;

            // 三角形の頂点数
            const int vertexCnt = Constants.TriVertexCnt;
            double[][] pps = new double[vertexCnt][];
            // 2次三角形要素の最初の３点＝頂点の座標を取得
            for (int ino = 0; ino < vertexCnt; ino++)
            {
                int nodeNumber = element.NodeNumbers[ino];
                FemNode node = nodes[nodeNumber - 1];
                System.Diagnostics.Debug.Assert(node.No == nodeNumber);
                pps[ino] = node.Coord;
            }
            // バウンディングボックス取得
            double minX = double.MaxValue;
            double maxX = double.MinValue;
            double minY = double.MaxValue;
            double maxY = double.MinValue;
            foreach (double[] pp in pps)
            {
                double xx = pp[0];
                double yy = pp[1];
                if (minX > xx)
                {
                    minX = xx;
                }
                if (maxX < xx)
                {
                    maxX = xx;
                }
                if (minY > yy)
                {
                    minY = yy;
                }
                if (maxY < yy)
                {
                    maxY = yy;
                }
            }
            // バウンディングボックスでチェック
            if (test_pp[0] < minX || test_pp[0] > maxX)
            {
                return hit;
            }
            if (test_pp[1] < minY || test_pp[1] > maxY)
            {
                return hit;
            }

            // 頂点？
            foreach (double[] pp in pps)
            {
                if (Math.Abs(pp[0] - test_pp[0]) < Constants.PrecisionLowerLimit && Math.Abs(pp[1] - test_pp[1]) < Constants.PrecisionLowerLimit)
                {
                    hit = true;
                    break;
                }
            }
            if (!hit)
            {
                // 面積から内部判定する
                double area = KerEMatTri.TriArea(pps[0], pps[1], pps[2]);
                double sumOfSubArea = 0.0;
                for (int ino = 0; ino < vertexCnt; ino++)
                {
                    double[][] subArea_pp = new double[vertexCnt][];
                    subArea_pp[0] = pps[ino];
                    subArea_pp[1] = pps[(ino + 1) % vertexCnt];
                    subArea_pp[2] = test_pp;
                    //foreach (double[] work_pp in subArea_pp)
                    //{
                    //    System.Diagnostics.Debug.Write("{0},{1}  ", work_pp[0], work_pp[1]);
                    //}
                    double subArea = KerEMatTri.TriArea(subArea_pp[0], subArea_pp[1], subArea_pp[2]);
                    //System.Diagnostics.Debug.Write("  subArea = {0}", subArea);
                    //System.Diagnostics.Debug.WriteLine();
                    //BUGFIX
                    //if (subArea <= 0.0)
                    // 丁度辺上の場合は、サブエリアの１つが０になるのでこれは許可しないといけない
                    if (subArea < -1.0 * Constants.PrecisionLowerLimit)  // 0未満
                    {
                        sumOfSubArea = 0.0;
                        break;
                        // 外側？
                    }
                    sumOfSubArea += Math.Abs(subArea);
                }
                if (Math.Abs(area - sumOfSubArea) < Constants.PrecisionLowerLimit)
                {
                    hit = true;
                }
            }
            return hit;
        }

        /// <summary>
        /// 2点間距離の計算
        /// </summary>
        /// <param name="p"></param>
        /// <param name="p0"></param>
        /// <returns></returns>
        public static double GetDistance(double[] p, double[] p0)
        {
            return Math.Sqrt((p[0] - p0[0]) * (p[0] - p0[0]) + (p[1] - p0[1]) * (p[1] - p0[1]));
        }

        /// <summary>
        /// 要素の節点数から該当するFemElementインスタンスを作成する
        /// </summary>
        /// <param name="eNodeCnt"></param>
        /// <returns></returns>
        public static FemElement CreateFemElementByElementNodeCnt(int eNodeCnt)
        {
            FemElement femElement = null;
            Constants.FemElementShapeDV elemShapeDv;
            int order;
            int vertexCnt;
            GetElementShapeDvAndOrderByElemNodeCnt(eNodeCnt, out elemShapeDv, out order, out vertexCnt);

            if (vertexCnt == Constants.TriVertexCnt)
            {
                femElement = new FemTriElement();
            }
            else
            {
                femElement = new FemElement();
            }
            return femElement;
        }


    }
}
