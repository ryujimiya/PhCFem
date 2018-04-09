using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
//using System.Numerics; // Complex
using KrdLab.clapack; // KrdLab.clapack.Complex
using System.Text.RegularExpressions;

namespace PhCFem
{
    /// <summary>
    /// Fem入力データファイルの読み書き
    /// </summary>
    class FemInputDatFile
    {
        /// <summary>
        ///  Fem入力データをファイルから読み込み
        /// </summary>
        /// <param name="filename">ファイル名(*.fem)</param>
        /// <param name="nodes">節点リスト</param>
        /// <param name="elements">要素リスト</param>
        /// <param name="ports">ポートの節点番号リストのリスト</param>
        /// <param name="forceBCNodes">強制境界節点番号リスト</param>
        /// <param name="incidentPortNo">入射ポート番号</param>
        /// <param name="medias">媒質情報リスト</param>
        /// <param name="firstWaveLength">計算開始波長</param>
        /// <param name="lastWaveLength">計算終了波長</param>
        /// <param name="calcCnt">計算件数</param>
        /// <param name="wgStructureDv">導波路構造区分</param>
        /// <param name="waveModeDv">波のモード区分</param>
        /// <returns></returns>
        public static bool LoadFromFile(
            string filename,
            out IList<FemNode> nodes,
            out IList<FemElement> elements,
            out IList<IList<int>> ports,
            out IList<int> forceBCNodes,
            out IList<IList<uint>> elemNoPeriodicList,
            out IList<IList<IList<int>>> nodePeriodicBList,
            out IList<IList<int>> defectNodePeriodicList,
            out int incidentPortNo,
            out MediaInfo[] medias,
            out double firstWaveLength,
            out double lastWaveLength,
            out int calcCnt,
            out FemSolver.WaveModeDV waveModeDv
            )
        {
            int eNodeCnt = 0;

            nodes = new List<FemNode>();
            elements = new List<FemElement>();
            ports = new List<IList<int>>();
            forceBCNodes = new List<int>();
            elemNoPeriodicList = new List<IList<uint>>();
            nodePeriodicBList = new List<IList<IList<int>>>();
            defectNodePeriodicList = new List<IList<int>>();
            incidentPortNo = 1;
            medias = new MediaInfo[Constants.MaxMediaCount];
            Color[] workColorList = { CadLogic.VacumnBackColor, CadLogic.RodBackColor };
            for (int i = 0; i < medias.Length; i++)
            {
                MediaInfo media = new MediaInfo();
                media.BackColor = workColorList[i];
                medias[i] = media;
            }
            firstWaveLength = 0.0;
            lastWaveLength = 0.0;
            calcCnt = 0;
            waveModeDv = Constants.DefWaveModeDv;

            if (!File.Exists(filename))
            {
                return false;
            }

            // 入力データ読み込み
            try
            {
                using (StreamReader sr = new StreamReader(filename))
                {
                    const char delimiter = ',';
                    string line;
                    string[] tokens;

                    line = sr.ReadLine();
                    tokens = line.Split(delimiter);
                    if (tokens.Length != 2 || tokens[0] != "Nodes")
                    {
                        MessageBox.Show("節点情報がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    int nodeCnt = int.Parse(tokens[1]);
                    for (int i = 0; i < nodeCnt; i++)
                    {
                        line = sr.ReadLine();
                        tokens = line.Split(delimiter);
                        if (tokens.Length != 3)
                        {
                            MessageBox.Show("節点情報が不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                        int no = int.Parse(tokens[0]);
                        if (no != i + 1)
                        {
                            MessageBox.Show("節点番号が不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                        FemNode femNode = new FemNode();
                        femNode.No = no;
                        femNode.Coord = new double[2];
                        femNode.Coord[0] = double.Parse(tokens[1]);
                        femNode.Coord[1] = double.Parse(tokens[2]);
                        nodes.Add(femNode);
                    }

                    line = sr.ReadLine();
                    tokens = line.Split(delimiter);
                    if (tokens.Length != 2 || tokens[0] != "Elements")
                    {
                        MessageBox.Show("要素情報がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    int elementCnt = int.Parse(tokens[1]);
                    for (int i = 0; i < elementCnt; i++)
                    {
                        line = sr.ReadLine();
                        tokens = line.Split(delimiter);
                        if ((tokens.Length != 1 + Constants.TriNodeCnt_SecondOrder)
                            && (tokens.Length != 2 + Constants.TriNodeCnt_SecondOrder)  // ver1.1.0.0で媒質インデックスを番号の後に挿入
                            && (tokens.Length != 2 + Constants.TriNodeCnt_FirstOrder)
                            )
                        {
                            MessageBox.Show("要素情報が不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                        int elemNo = int.Parse(tokens[0]);
                        int mediaIndex = 0;
                        int indexOffset = 1; // ver1.0.0.0
                        int workENodeCnt = Constants.TriNodeCnt_SecondOrder;
                        if (tokens.Length == 1 + Constants.TriNodeCnt_SecondOrder)
                        {
                            // 媒質インデックスのない古い形式(ver1.0.0.0)
                        }
                        else
                        {
                            // ver1.1.0.0で媒質インデックスを追加
                            mediaIndex = int.Parse(tokens[1]);
                            indexOffset = 2;

                            workENodeCnt = tokens.Length - 2;
                        }
                        if (workENodeCnt <= 0)
                        {
                            MessageBox.Show("要素節点数が不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                        if (eNodeCnt == 0)
                        {
                            // 最初の要素の節点数を格納（チェックに利用)
                            eNodeCnt = workENodeCnt;
                        }
                        else
                        {
                            // 要素の節点数が変わった？
                            if (workENodeCnt != eNodeCnt)
                            {
                                MessageBox.Show("要素節点数が不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return false;
                            }
                        }
                        //FemElement femElement = new FemElement();
                        FemElement femElement = FemMeshLogic.CreateFemElementByElementNodeCnt(eNodeCnt);
                        femElement.No = elemNo;
                        femElement.MediaIndex = mediaIndex;
                        femElement.NodeNumbers = new int[eNodeCnt];
                        for (int n = 0; n < femElement.NodeNumbers.Length; n++)
                        {
                            femElement.NodeNumbers[n] = int.Parse(tokens[n + indexOffset]);
                        }
                        elements.Add(femElement);
                    }

                    line = sr.ReadLine();
                    tokens = line.Split(delimiter);
                    if (tokens.Length != 2 || tokens[0] != "Ports")
                    {
                        MessageBox.Show("入出力ポート情報がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    int portCnt = int.Parse(tokens[1]);
                    for (int i = 0; i < portCnt; i++)
                    {
                        line = sr.ReadLine();
                        tokens = line.Split(delimiter);
                        if (tokens.Length != 2)
                        {
                            MessageBox.Show("入出力ポート情報が不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                        int portNo = int.Parse(tokens[0]);
                        int portNodeCnt = int.Parse(tokens[1]);
                        if (portNo != i + 1)
                        {
                            MessageBox.Show("ポート番号が不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                        IList<int> portNodes = new List<int>();
                        for (int n = 0; n < portNodeCnt; n++)
                        {
                            line = sr.ReadLine();
                            tokens = line.Split(delimiter);
                            if (tokens.Length != 2)
                            {
                                MessageBox.Show("ポートの節点情報が不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return false;
                            }
                            int portNodeNumber = int.Parse(tokens[0]);
                            int nodeNumber = int.Parse(tokens[1]);
                            if (portNodeNumber != n + 1)
                            {
                                MessageBox.Show("ポートの節点番号が不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return false;
                            }
                            portNodes.Add(nodeNumber);
                        }
                        ports.Add(portNodes);
                    }

                    line = sr.ReadLine();
                    tokens = line.Split(delimiter);
                    if (tokens.Length != 2 || tokens[0] != "Force")
                    {
                        MessageBox.Show("強制境界情報がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    int forceNodeCnt = int.Parse(tokens[1]);
                    for (int i = 0; i < forceNodeCnt; i++)
                    {
                        line = sr.ReadLine();
                        int nodeNumber = int.Parse(line);
                        forceBCNodes.Add(nodeNumber);
                    }

                    // 周期構造領域内要素番号
                    for (int portIndex = 0; portIndex < portCnt; portIndex++)
                    {
                        line = sr.ReadLine();
                        tokens = line.Split(delimiter);
                        if (tokens.Length != 3 || tokens[0] != "elemNoPeriodic")
                        {
                            MessageBox.Show("周期構造領域要素情報がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                        int tmpIndex = int.Parse(tokens[1]);
                        System.Diagnostics.Debug.Assert(tmpIndex == portIndex);
                        int cnt = int.Parse(tokens[2]);
                        IList<uint> elemNoPeriodic = new List<uint>();
                        for (int i = 0; i < cnt; i++)
                        {
                            line = sr.ReadLine();
                            tokens = line.Split(delimiter);
                            if (tokens.Length != 1)
                            {
                                MessageBox.Show("周期構造領域要素番号情報がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return false;
                            }
                            uint elemNo = uint.Parse(tokens[0]);
                            elemNoPeriodic.Add(elemNo);
                        }
                        elemNoPeriodicList.Add(elemNoPeriodic);
                    }

                    // 周期構造境界節点番号
                    for (int portIndex = 0; portIndex < portCnt; portIndex++)
                    {
                        IList<IList<int>> nodePeriodicB = new List<IList<int>>();
                        for (int boundaryIndex = 0; boundaryIndex < 2; boundaryIndex++)
                        {
                            line = sr.ReadLine();
                            tokens = line.Split(delimiter);
                            if (tokens.Length != 4 || tokens[0] != "nodePeriodicB")
                            {
                                MessageBox.Show("周期構造境界節点番号情報がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return false;
                            }
                            int tmpIndex1 = int.Parse(tokens[1]);
                            System.Diagnostics.Debug.Assert(tmpIndex1 == portIndex);
                            int tmpIndex2 = int.Parse(tokens[2]);
                            System.Diagnostics.Debug.Assert(tmpIndex2 == boundaryIndex);
                            int cnt = int.Parse(tokens[3]);
                            IList<int> workNodesB = new List<int>();
                            for (int i = 0; i < cnt; i++)
                            {
                                line = sr.ReadLine();
                                tokens = line.Split(delimiter);
                                if (tokens.Length != 1)
                                {
                                    MessageBox.Show("周期構造境界節点番号情報がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return false;
                                }
                                int nodeNumber = int.Parse(tokens[0]);
                                workNodesB.Add(nodeNumber);
                            }
                            nodePeriodicB.Add(workNodesB);
                        }
                        nodePeriodicBList.Add(nodePeriodicB);
                    }

                    
                    // 周期構造領域内要素番号
                    for (int portIndex = 0; portIndex < portCnt; portIndex++)
                    {
                        line = sr.ReadLine();
                        tokens = line.Split(delimiter);
                        if (tokens.Length != 3 || tokens[0] != "defectNodePeriodic")
                        {
                            MessageBox.Show("周期構造領域欠陥部節点情報がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                        int tmpIndex = int.Parse(tokens[1]);
                        System.Diagnostics.Debug.Assert(tmpIndex == portIndex);
                        int cnt = int.Parse(tokens[2]);
                        IList<int> defectNodePeriodic = new List<int>();
                        for (int i = 0; i < cnt; i++)
                        {
                            line = sr.ReadLine();
                            tokens = line.Split(delimiter);
                            if (tokens.Length != 1)
                            {
                                MessageBox.Show("周期構造領域欠陥部節点番号情報がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return false;
                            }
                            int nodeNumber = int.Parse(tokens[0]);
                            defectNodePeriodic.Add(nodeNumber);
                        }
                        defectNodePeriodicList.Add(defectNodePeriodic);
                    }
                     
                    line = sr.ReadLine();
                    tokens = line.Split(delimiter);
                    if (tokens.Length != 2 || tokens[0] != "IncidentPortNo")
                    {
                        MessageBox.Show("入射ポート番号がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    incidentPortNo = int.Parse(tokens[1]);

                    line = sr.ReadLine();
                    if (line == null || line.Length == 0)
                    {
                        // 媒質情報なし
                    }
                    else
                    {
                        // 媒質情報？
                        tokens = line.Split(delimiter);
                        if (tokens[0] != "Medias")
                        {
                            MessageBox.Show("媒質情報がありません");
                            return false;
                        }
                        int cnt = int.Parse(tokens[1]);
                        if (cnt > Constants.MaxMediaCount)
                        {
                            MessageBox.Show("媒質情報の個数が不正です");
                            return false;
                        }
                        for (int i = 0; i < cnt; i++)
                        {
                            line = sr.ReadLine();
                            if (line.Length == 0)
                            {
                                MessageBox.Show("媒質情報が不正です");
                                return false;
                            }
                            tokens = line.Split(delimiter);
                            if (tokens.Length != 1 + 9 + 9)
                            {
                                MessageBox.Show("媒質情報が不正です");
                                return false;
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
                            medias[i].SetP(p);

                            double[,] q = new double[3, 3];
                            for (int m = 0; m < q.GetLength(0); m++)
                            {
                                for (int n = 0; n < q.GetLength(1); n++)
                                {
                                    q[m, n] = double.Parse(tokens[1 + 9 + m * q.GetLength(1) + n]);
                                }
                            }
                            medias[i].SetQ(q);
                        }
                    }
                    line = sr.ReadLine();
                    if (line == null || line.Length == 0)
                    {
                    }
                    else
                    {
                        tokens = line.Split(delimiter);
                        if (tokens.Length != 4 || tokens[0] != "WaveLengthRange")
                        {
                            MessageBox.Show("計算対象周波数情報がありません");
                            return false;
                        }
                        firstWaveLength = double.Parse(tokens[1]);
                        lastWaveLength = double.Parse(tokens[2]);
                        calcCnt = int.Parse(tokens[3]);
                    }
                    line = sr.ReadLine();
                    if (line == null || line.Length == 0)
                    {
                    }
                    else
                    {
                        tokens = line.Split(delimiter);
                        if (tokens.Length != 2 || tokens[0] != "WaveModeDv")
                        {
                            MessageBox.Show("計算対象モード区分情報がありません");
                            return false;
                        }
                        if (tokens[1] == "TE")
                        {
                            waveModeDv = FemSolver.WaveModeDV.TE;
                        }
                        else if (tokens[1] == "TM")
                        {
                            waveModeDv = FemSolver.WaveModeDV.TM;
                        }
                        else
                        {
                            MessageBox.Show("計算対象モード区分情報が不正です");
                            return false;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
                MessageBox.Show(exception.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Fem入力データファイルへ保存
        ///   I/FがCadの内部データ寄りになっているので、変更したいが後回し
        /// </summary>
        /// <param name="filename">ファイル名(*.fem)</param>
        /// <param name="nodeCnt">節点数</param>
        /// <param name="doubleCoords">節点座標リスト</param>
        /// <param name="elementCnt">要素数</param>
        /// <param name="elements">要素リスト</param>
        /// <param name="portCnt">ポート数</param>
        /// <param name="portList">ポートの節点番号リストのリスト</param>
        /// <param name="forceBCNodeNumbers">強制境界節点番号のリスト</param>
        /// <param name="incidentPortNo">入射ポート番号</param>
        /// <param name="medias">媒質情報リスト</param>
        /// <param name="firstWaveLength">計算開始波長</param>
        /// <param name="lastWaveLength">計算終了波長</param>
        /// <param name="calcCnt">計算周波数件数</param>
        /// <param name="waveModeDv">波のモード区分</param>
        public static void SaveToFileFromCad
            (string filename,
            int nodeCnt, IList<double[]> doubleCoords,
            int elementCnt, IList<int[]> elements,
            int portCnt, IList<IList<int>> portList,
            int[] forceBCNodeNumbers,
            IList<IList<uint>> elemNoPeriodicList,
            IList<IList<IList<int>>> nodePeriodicBList,
            IList<IList<int>> defectNodePeriodicList,
            int incidentPortNo,
            MediaInfo[] medias,
            double firstWaveLength,
            double lastWaveLength,
            int calcCnt,
            FemSolver.WaveModeDV waveModeDv)
        {
            //////////////////////////////////////////
            // ファイル出力
            //////////////////////////////////////////
            try
            {
                using (StreamWriter sw = new StreamWriter(filename))
                {
                    string line;

                    // 節点番号と座標の出力
                    line = string.Format("Nodes,{0}", nodeCnt);
                    sw.WriteLine(line);
                    for (int i = 0; i < doubleCoords.Count; i++)
                    {
                        double[] doubleCoord = doubleCoords[i];
                        int nodeNumber = i + 1;
                        line = string.Format("{0},{1},{2}", nodeNumber, doubleCoord[0], doubleCoord[1]);
                        sw.WriteLine(line);
                    }
                    // 要素番号と要素を構成する節点の全体節点番号の出力
                    line = string.Format("Elements,{0}", elementCnt);
                    sw.WriteLine(line);
                    foreach (int[] element in elements)
                    {
                        line = "";
                        foreach (int k in element)
                        {
                            line += string.Format("{0},", k);
                        }
                        line = line.Substring(0, line.Length - 1); // 最後の,を削除
                        sw.WriteLine(line);
                    }
                    // ポート境界条件節点
                    int portCounter = 0;
                    line = string.Format("Ports,{0}", portList.Count);
                    sw.WriteLine(line);
                    foreach (IList<int> nodes in portList)
                    {
                        line = string.Format("{0},{1}", ++portCounter, nodes.Count);
                        sw.WriteLine(line);
                        int portNodeNumber = 0;
                        foreach (int nodeNumber in nodes)
                        {
                            line = string.Format("{0},{1}", ++portNodeNumber, nodeNumber);
                            sw.WriteLine(line);
                        }
                    }
                    // 強制境界節点
                    line = string.Format("Force,{0}", forceBCNodeNumbers.Length);
                    sw.WriteLine(line);
                    foreach (int nodeNumber in forceBCNodeNumbers)
                    {
                        line = string.Format("{0}", nodeNumber);
                        sw.WriteLine(line);
                    }

                    // 周期構造領域内要素番号
                    System.Diagnostics.Debug.Assert(portCnt == elemNoPeriodicList.Count);
                    for (int portIndex = 0; portIndex < portCnt; portIndex++)
                    {
                        IList<uint> elemNoPeriodic = elemNoPeriodicList[portIndex];
                        line = string.Format("elemNoPeriodic,{0},{1}", portIndex, elemNoPeriodic.Count);
                        sw.WriteLine(line);
                        for (int i = 0; i < elemNoPeriodic.Count; i++)
                        {
                            line = string.Format("{0}", elemNoPeriodic[i]);
                            sw.WriteLine(line);
                        }
                    }
                    // 周期構造境界節点番号
                    System.Diagnostics.Debug.Assert(portCnt == nodePeriodicBList.Count);
                    for (int portIndex = 0; portIndex < portCnt; portIndex++)
                    {
                        IList<IList<int>> nodePeriodicB = nodePeriodicBList[portIndex];
                        for (int boundaryIndex = 0; boundaryIndex < 2; boundaryIndex++)
                        {
                            IList<int> workNodesB = nodePeriodicB[boundaryIndex];
                            line = string.Format("nodePeriodicB,{0},{1},{2}", portIndex, boundaryIndex, workNodesB.Count);
                            sw.WriteLine(line);
                            for (int i = 0; i < workNodesB.Count; i++)
                            {
                                line = string.Format("{0}", workNodesB[i]);
                                sw.WriteLine(line);
                            }
                        }
                    }
                    
                    // 周期構造領域内欠陥部の節点番号
                    System.Diagnostics.Debug.Assert(portCnt == defectNodePeriodicList.Count);
                    for (int portIndex = 0; portIndex < portCnt; portIndex++)
                    {
                        IList<int> defectNodePeriodic = defectNodePeriodicList[portIndex];
                        line = string.Format("defectNodePeriodic,{0},{1}", portIndex, defectNodePeriodic.Count);
                        sw.WriteLine(line);
                        for (int i = 0; i < defectNodePeriodic.Count; i++)
                        {
                            line = string.Format("{0}", defectNodePeriodic[i]);
                            sw.WriteLine(line);
                        }
                    }
                     

                    // 入射ポート番号
                    line = string.Format("IncidentPortNo,{0}", incidentPortNo);
                    sw.WriteLine(line);
                    // 媒質情報の個数
                    sw.WriteLine("Medias,{0}", medias.Length);
                    // 媒質情報の書き込み
                    for (int i = 0; i < medias.Length; i++)
                    {
                        MediaInfo media = medias[i];
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
                    // 計算対象周波数
                    sw.WriteLine("WaveLengthRange,{0},{1},{2}", firstWaveLength, lastWaveLength, calcCnt);
                    // 計算対象モード区分
                    sw.WriteLine("WaveModeDv,{0}", ((waveModeDv == FemSolver.WaveModeDV.TM) ? "TM" : "TE"));
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
                MessageBox.Show(exception.Message);
            }
        }

        /// <summary>
        ///  Fem入力データをファイルへ書き込み
        /// </summary>
        /// <param name="filename">ファイル名(*.fem)</param>
        /// <param name="nodes">節点リスト</param>
        /// <param name="elements">要素リスト</param>
        /// <param name="ports">ポートの節点番号リストのリスト</param>
        /// <param name="forceBCNodes">強制境界節点番号リスト</param>
        /// <param name="incidentPortNo">入射ポート番号</param>
        /// <param name="medias">媒質情報リスト</param>
        /// <param name="firstWaveLength">計算開始波長</param>
        /// <param name="lastWaveLength">計算終了波長</param>
        /// <param name="calcCnt">計算件数</param>
        /// <param name="waveModeDv">波のモード区分</param>
        /// <returns></returns>
        public static void SaveToFile
            (string filename,
            IList<FemNode> nodes,
            IList<FemElement> elements,
            IList<IList<int>> ports,
            IList<int> forceBCNodes,
            IList<IList<uint>> elemNoPeriodicList,
            IList<IList<IList<int>>> nodePeriodicBList,
            IList<IList<int>> defectNodePeriodicList,
            int incidentPortNo,
            MediaInfo[] medias,
            double firstWaveLength,
            double lastWaveLength,
            int calcCnt,
            FemSolver.WaveModeDV waveModeDv
            )
        {
            int nodeCnt = nodes.Count;
            IList<double[]> doubleCoords = new List<double[]>();
            foreach (FemNode femNode in nodes)
            {
                doubleCoords.Add(femNode.Coord);
            }
            int elementCnt = elements.Count;
            IList<int[]> in_elements = new List<int[]>();
            foreach (FemElement femElement in elements)
            {
                int cnt = 2 + femElement.NodeNumbers.Length;
                int[] in_element = new int[cnt];
                in_element[0] = femElement.No;
                in_element[1] = femElement.MediaIndex;
                for (int ino = 0; ino < femElement.NodeNumbers.Length; ino++)
                {
                    in_element[2 + ino] = femElement.NodeNumbers[ino];
                }
                in_elements.Add(in_element);
            }
            int portCnt = ports.Count;
            int[] forceBCNodeNumbers = forceBCNodes.ToArray();

            SaveToFileFromCad(
                filename,
                nodeCnt, doubleCoords,
                elementCnt, in_elements,
                portCnt, ports,
                forceBCNodeNumbers,
                elemNoPeriodicList,
                nodePeriodicBList,
                defectNodePeriodicList,
                incidentPortNo,
                medias,
                firstWaveLength,
                lastWaveLength,
                calcCnt,
                waveModeDv);
        }
    }
}
