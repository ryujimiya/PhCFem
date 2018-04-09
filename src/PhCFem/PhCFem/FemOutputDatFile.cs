using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
//using System.Numerics; // Complex
using KrdLab.clapack; //KrdLab.clapack.Complex
using System.Text.RegularExpressions;

namespace PhCFem
{
    /// <summary>
    /// Fem計算結果データファイルの読み書き
    /// </summary>
    class FemOutputDatFile
    {
        /// <summary>
        /// 計算結果をファイルに出力(追記モード)
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="freqNo"></param>
        /// <param name="waveLength"></param>
        /// <param name="maxMode"></param>
        /// <param name="portCnt"></param>
        /// <param name="incidentPortNo"></param>
        /// <param name="nodesBoundaryList"></param>
        /// <param name="eigenValuesList"></param>
        /// <param name="eigenVecsList"></param>
        /// <param name="nodesRegion"></param>
        /// <param name="valuesAll"></param>
        /// <param name="scatterVecList"></param>
        public static void AppendToFile(
            string filename, int freqNo, double waveLength, int maxMode,
            int portCnt, int incidentPortNo,
            IList<int[]> nodesBoundaryList, IList<Complex[]> eigenValuesList, IList<Complex[,]> eigenVecsList,
            int[] nodesRegion, Complex[] valuesAll,
            IList<Complex[]> scatterVecList)
        {
            System.Diagnostics.Debug.Assert(portCnt == nodesBoundaryList.Count);
            System.Diagnostics.Debug.Assert(portCnt == eigenValuesList.Count);
            System.Diagnostics.Debug.Assert(portCnt == eigenVecsList.Count);

            long writeFileOfs = 0;  // 書き込み開始位置
            try
            {
                // 追記モードで書き込み
                using (StreamWriter sw = new StreamWriter(filename, true))
                {
                    Stream stream = sw.BaseStream;
                    string line;

                    // 書き込み開始位置を記憶
                    writeFileOfs = stream.Position;

                    // 開始シーケンスの書き込み
                    sw.WriteLine("S");

                    // 計算条件
                    line = string.Format("waveLength,{0}", waveLength);
                    sw.WriteLine(line);
                    line = string.Format("maxMode,{0}", maxMode);
                    sw.WriteLine(line);
                    // ポート解析結果
                    line = string.Format("ports,{0}", portCnt);
                    sw.WriteLine(line);
                    for (int portIndex = 0; portIndex < portCnt; portIndex++)
                    {
                        int[] nodesBoundary = nodesBoundaryList[portIndex];
                        Complex[] eigenValues = eigenValuesList[portIndex];
                        Complex[,] eigenVecs = eigenVecsList[portIndex];

                        // ポート節点番号リスト
                        line = string.Format("portNodes,{0}", nodesBoundary.Length);
                        sw.WriteLine(line);
                        foreach (int nodeNumber in nodesBoundary)
                        {
                            line = string.Format("{0}", nodeNumber);
                            sw.WriteLine(line);
                        }
                        if (eigenValues == null)
                        {
                            // モード解析結果
                            line = string.Format("modes,{0}", 0);
                            sw.WriteLine(line);
                        }
                        else
                        {
                            // モード解析結果
                            line = string.Format("modes,{0}", eigenValues.Length);
                            sw.WriteLine(line);
                            for (int modeIndex = 0; modeIndex < eigenValues.Length; modeIndex++)
                            {
                                // 固有値
                                Complex bm = eigenValues[modeIndex];
                                line = string.Format("eigenValue,{0},{1}+{2}i", modeIndex, bm.Real, bm.Imaginary);
                                sw.WriteLine(line);
                                System.Diagnostics.Debug.Assert(nodesBoundary.Length == eigenVecs.GetLength(1));
                                line = string.Format("eigenVec,{0},{1}", modeIndex, eigenVecs.GetLength(1));
                                sw.WriteLine(line);
                                // 固有ベクトル
                                for (int ino = 0; ino < eigenVecs.GetLength(1); ino++)
                                {
                                    Complex fieldValue = eigenVecs[modeIndex, ino];
                                    line = string.Format("{0}+{1}i", fieldValue.Real, fieldValue.Imaginary);
                                    sw.WriteLine(line);
                                }
                            }
                        }
                    }
                    // 領域解析結果
                    // 節点番号
                    line = string.Format("regionNodes,{0}", nodesRegion.Length);
                    sw.WriteLine(line);
                    foreach (int nodeNumber in nodesRegion)
                    {
                        line = string.Format("{0}", nodeNumber);
                        sw.WriteLine(line);
                    }
                    // 界
                    line = string.Format("fieldValues,{0}", valuesAll.Length);
                    sw.WriteLine(line);
                    foreach (Complex fieldValue in valuesAll)
                    {
                        line = string.Format("{0}+{1}i", fieldValue.Real, fieldValue.Imaginary);
                        sw.WriteLine(line);
                    }
                    // 互換性の為に基本モードのみの散乱行列出力を残す
                    // 散乱行列(Sij j = IncidentPortNoのみ)
                    line = string.Format("Si{0}", incidentPortNo);
                    sw.WriteLine(line);
                    foreach (Complex[] portScatterVec in scatterVecList)
                    {
                        // ポートの基本モードの散乱パラメータ―
                        Complex si1 = portScatterVec[0];
                        line = string.Format("{0}+{1}i", si1.Real, si1.Imaginary);
                        sw.WriteLine(line);
                    }
                    // 拡張散乱行列 Simjn
                    //   出力ポートi モードmの散乱係数
                    //   入射ポートj = IncindentPortNo n = 0(基本モード)のみ対応
                    const int incidentModeIndex = 0;
                    line = string.Format("scatterVecList,{0},{1}", incidentPortNo, incidentModeIndex);
                    sw.WriteLine(line);
                    for (int portIndex = 0; portIndex < scatterVecList.Count; portIndex++)
                    {
                        Complex[] portScatterVec = scatterVecList[portIndex];
                        int modeCnt = portScatterVec.Length;
                        line = string.Format("portScatterVec,{0},{1}", portIndex, modeCnt);
                        sw.WriteLine(line);
                        for (int iMode = 0; iMode < modeCnt; iMode++)
                        {
                            Complex simjn = portScatterVec[iMode];
                            line = string.Format("{0}+{1}i", simjn.Real, simjn.Imaginary);
                            sw.WriteLine(line);
                        }
                    }

                    // 終了シーケンスの書き込み
                    sw.WriteLine("E");

                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
                MessageBox.Show(exception.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // 書き込み開始位置をインデックスファイルに記録する
            string indexfilename = filename + Constants.FemOutputIndexExt;
            try
            {
                // 追記モードで書き込み
                using (StreamWriter sw = new StreamWriter(indexfilename, true))
                {
                    string line;
                    line = string.Format("{0},{1}", freqNo, writeFileOfs);
                    sw.WriteLine(line);
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
                MessageBox.Show(exception.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        /// <summary>
        /// 計算済み周波数の件数を取得
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static int GetCalculatedFreqCnt(string filename, out int firstFreq, out int lastFreq)
        {
            int freqCnt = 0;

            firstFreq = 1;
            lastFreq = 1;
            // ファイル本体の存在確認
            if (!File.Exists(filename))
            {
                return freqCnt;
            }
            // インデックスファイルから件数を取得する
            string indexfilename = filename + Constants.FemOutputIndexExt;
            // インデックスファイルの存在確認
            if (!File.Exists(indexfilename))
            {
                return freqCnt;
            }
            try
            {
                // 周波数が順番に並んでいない場合を考慮
                int minFreq = int.MaxValue;
                int maxFreq = int.MinValue;
                using (StreamReader sr = new StreamReader(indexfilename))
                {
                    string line;
                    string[] tokens;
                    char delimiter = ',';

                    while (!sr.EndOfStream)
                    {
                        line = sr.ReadLine();
                        if (line.Length == 0) break;
                        tokens = line.Split(delimiter);
                        int tmpfreqNo = int.Parse(tokens[0]);
                        if (tmpfreqNo < 1)
                        {
                            MessageBox.Show("周波数インデックスが不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return freqCnt;
                        }
                        long tmpFOfs = long.Parse(tokens[1]);
                        //if (freqCnt == 0)
                        //{
                        //    firstFreq = tmpfreqNo;
                        //}
                        //lastFreq = tmpfreqNo;
                        // 周波数が順番に並んでいない場合を考慮
                        if (minFreq > tmpfreqNo)
                        {
                            minFreq = tmpfreqNo;
                        }
                        if (maxFreq < tmpfreqNo)
                        {
                            maxFreq = tmpfreqNo;
                        }

                        freqCnt++;
                    }
                }
                // 周波数が順番に並んでいない場合を考慮
                if (freqCnt > 0)
                {
                    firstFreq = minFreq;
                    lastFreq = maxFreq;
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
                MessageBox.Show(exception.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return freqCnt;
            }

            return freqCnt;
        }

        /// <summary>
        /// ファイルから読み込み
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="freqNo"></param>
        /// <param name="waveLength"></param>
        /// <param name="maxMode"></param>
        /// <param name="incidentPortNo"></param>
        /// <param name="nodesBoundaryList"></param>
        /// <param name="eigenValuesList"></param>
        /// <param name="eigenVecsList"></param>
        /// <param name="nodesRegion"></param>
        /// <param name="valuesAll"></param>
        /// <param name="scatterVecList"></param>
        /// <returns></returns>
        public static bool LoadFromFile(
            string filename, int freqNo,
            out double waveLength, out int maxMode, out int incidentPortNo,
            out IList<int[]> nodesBoundaryList, out IList<Complex[]> eigenValuesList, out IList<Complex[,]> eigenVecsList,
            out int[] nodesRegion, out Complex[] valuesAll,
            out IList<Complex[]> scatterVecList)
        {
            const char delimiter = ',';

            waveLength = 0.0;
            maxMode = 0;
            incidentPortNo = 1;
            nodesBoundaryList = new List<int[]>();
            eigenValuesList = new List<Complex[]>();
            eigenVecsList = new List<Complex[,]>();
            nodesRegion = null;
            valuesAll = null;
            scatterVecList = new List<Complex[]>();

            if (!File.Exists(filename))
            {
                return false;
            }

            long readFileOfs = 0;
            bool findFlg = false;
            // 読み込み開始位置をインデックスファイルから読み出す
            string indexfilename = filename + Constants.FemOutputIndexExt;
            try
            {
                using (StreamReader sr = new StreamReader(indexfilename))
                {
                    string line;
                    string[] tokens;

                    while (!sr.EndOfStream)
                    {
                        line = sr.ReadLine();
                        if (line.Length == 0) break;
                        tokens = line.Split(delimiter);
                        int tmpfreqNo = int.Parse(tokens[0]);
                        if (tmpfreqNo < 1)
                        {
                            MessageBox.Show("周波数インデックスが不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                        long tmpFOfs = long.Parse(tokens[1]);
                        if (freqNo == -1)
                        {
                            // 最後の結果データの読み込み開始位置を更新
                            readFileOfs = tmpFOfs;
                            findFlg = true;
                        }
                        else
                        {
                            if (freqNo == tmpfreqNo)
                            {
                                readFileOfs = tmpFOfs;
                                findFlg = true;
                                break;
                            }
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

            if (!findFlg)
            {
                return false;
            }

            try
            {
                using (StreamReader sr = new StreamReader(filename))
                {
                    Stream stream = sr.BaseStream;
                    string line;
                    string[] tokens;

                    stream.Seek(readFileOfs, SeekOrigin.Begin);

                    line = sr.ReadLine();
                    if (line != "S")
                    {
                        MessageBox.Show("開始シーケンスがありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    line = sr.ReadLine();
                    tokens = line.Split(delimiter);
                    if (tokens[0] != "waveLength")
                    {
                        MessageBox.Show("波長がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    waveLength = double.Parse(tokens[1]);
                    line = sr.ReadLine();
                    tokens = line.Split(delimiter);
                    if (tokens[0] != "maxMode")
                    {
                        MessageBox.Show("考慮モード数がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    maxMode = int.Parse(tokens[1]);

                    line = sr.ReadLine();
                    tokens = line.Split(delimiter);
                    if (tokens[0] != "ports")
                    {
                        MessageBox.Show("ポート情報がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    int portCnt = int.Parse(tokens[1]);
                    for (int portIndex = 0; portIndex < portCnt; portIndex++)
                    {
                        line = sr.ReadLine();
                        tokens = line.Split(delimiter);
                        if (tokens[0] != "portNodes")
                        {
                            MessageBox.Show("ポートの節点情報がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                        int portNodeCnt = int.Parse(tokens[1]);
                        int[] nodesBoundary = new int[portNodeCnt];
                        for (int inoB = 0; inoB < portNodeCnt; inoB++)
                        {
                            line = sr.ReadLine();
                            int nodeNumber = int.Parse(line);
                            nodesBoundary[inoB] = nodeNumber;
                        }
                        nodesBoundaryList.Add(nodesBoundary);

                        line = sr.ReadLine();
                        tokens = line.Split(delimiter);
                        if (tokens[0] != "modes")
                        {
                            MessageBox.Show("ポートのモード解析結果がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                        int modeCnt = int.Parse(tokens[1]);
                        maxMode = modeCnt;
                        Complex[] eigenValues = new Complex[modeCnt];
                        Complex[,] eigenVecs = new Complex[modeCnt, portNodeCnt];
                        for (int modeIndex = 0; modeIndex < modeCnt; modeIndex++)
                        {
                            line = sr.ReadLine();
                            tokens = line.Split(delimiter);
                            if (tokens[0] != "eigenValue")
                            {
                                MessageBox.Show("ポートの固有値がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return false;
                            }
                            int mindex = int.Parse(tokens[1]);
                            if (mindex != modeIndex)
                            {
                                MessageBox.Show("ポートのモードインデックスが不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return false;
                            }
                            eigenValues[modeIndex] = MyUtilLib.MyUtil.ComplexParse(tokens[2]);

                            line = sr.ReadLine();
                            tokens = line.Split(delimiter);
                            if (tokens[0] != "eigenVec")
                            {
                                MessageBox.Show("ポートの固有ベクトルがありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return false;
                            }
                            mindex = int.Parse(tokens[1]);
                            if (mindex != modeIndex)
                            {
                                MessageBox.Show("ポートのモードインデックスが不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return false;
                            }
                            int bnodeCnt = int.Parse(tokens[2]);
                            if (bnodeCnt != portNodeCnt)
                            {
                                MessageBox.Show("ポートの固有ベクトルの節点数が不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return false;
                            }
                            for (int inoB = 0; inoB < portNodeCnt; inoB++)
                            {
                                line = sr.ReadLine();
                                eigenVecs[modeIndex, inoB] = MyUtilLib.MyUtil.ComplexParse(line);
                            }
                        }
                        eigenValuesList.Add(eigenValues);
                        eigenVecsList.Add(eigenVecs);
                    }

                    // 領域内節点番号
                    line = sr.ReadLine();
                    tokens = line.Split(delimiter);
                    if (tokens[0] != "regionNodes")
                    {
                        MessageBox.Show("領域内節点情報がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    int nodeCnt = int.Parse(tokens[1]);
                    nodesRegion = new int[nodeCnt];
                    for (int ino = 0; ino < nodeCnt; ino++)
                    {
                        line = sr.ReadLine();
                        int nodeNumber = int.Parse(line);
                        nodesRegion[ino] = nodeNumber;
                    }

                    // フィールド値
                    line = sr.ReadLine();
                    tokens = line.Split(delimiter);
                    if (tokens[0] != "fieldValues")
                    {
                        MessageBox.Show("領域内フィールド値がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    int fValueCnt = int.Parse(tokens[1]);
                    System.Diagnostics.Debug.Assert(fValueCnt == nodeCnt);
                    valuesAll = new Complex[fValueCnt];
                    for (int ino = 0; ino < fValueCnt; ino++)
                    {
                        line = sr.ReadLine();
                        valuesAll[ino] = MyUtilLib.MyUtil.ComplexParse(line);
                    }

                    // 基本モードの散乱行列(読み捨てる)
                    // Si1
                    line = sr.ReadLine();
                    if (line.IndexOf("Si") != 0)  // Sij j=入射ポート
                    {
                        MessageBox.Show("散乱行列がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    string incidentPortNoStr = line.Substring(2);  // "Si"を削除
                    int dummyIncidentPortNo = int.Parse(incidentPortNoStr);
                    Complex[] dummyScatterVec = new Complex[portCnt];
                    for (int iportno = 0; iportno < portCnt; iportno++)
                    {
                        line = sr.ReadLine();
                        dummyScatterVec[iportno] = MyUtilLib.MyUtil.ComplexParse(line);
                    }

                    // 拡張散乱行列
                    // scatterVecList
                    line = sr.ReadLine();
                    if (line != null && line != "E")
                    {
                        tokens = line.Split(delimiter);
                        if (tokens.Length != 3 || tokens[0] != "scatterVecList")
                        {
                            MessageBox.Show("拡張散乱行列がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                        incidentPortNo = int.Parse(tokens[1]);
                        int incidentModeIndex = int.Parse(tokens[2]);
                        for (int portIndex = 0; portIndex < portCnt; portIndex++)
                        {
                            line = sr.ReadLine();
                            tokens = line.Split(delimiter);
                            if (tokens.Length != 3 || tokens[0] != "portScatterVec")
                            {
                                MessageBox.Show("拡張散乱行列（ポート）がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return false;
                            }
                            int workPortIndex = int.Parse(tokens[1]);
                            int modeCnt = int.Parse(tokens[2]);
                            if (modeCnt <= 0)
                            {
                                MessageBox.Show("拡張散乱行列（ポート）が不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return false;
                            }
                            Complex[] portScatterVec = new Complex[modeCnt];
                            scatterVecList.Add(portScatterVec);
                            for (int iMode = 0; iMode < modeCnt; iMode++)
                            {
                                line = sr.ReadLine();
                                Complex simjn = MyUtilLib.MyUtil.ComplexParse(line);
                                portScatterVec[iMode] = simjn;
                            }
                        }
                    }
                    else
                    {
                        // 拡張散乱行列がない場合は、旧型式のデータを採用する
                        incidentPortNo = dummyIncidentPortNo;
                        foreach (Complex si1 in dummyScatterVec)
                        {
                            Complex[] portScatterVec = new Complex[1];
                            portScatterVec[0] = si1;
                            scatterVecList.Add(portScatterVec);
                        }
                    }

                    //line = sr.ReadLine();
                    //System.Diagnostics.Debug.Assert(line == "E");
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
        /// フィールド値の読み込み
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="waveLengthList"></param>
        /// <param name="valuesAllList"></param>
        /// <returns></returns>
        public static bool LoadValuesAllListFromFile(string filename, out IList<double> waveLengthList, out IList<Complex[]> valuesAllList)
        {
            string line;
            string[] tokens;
            char delimiter = ',';
            // 結果ファイルのインデックスファイル名
            string indexfilename = filename + Constants.FemOutputIndexExt;
            // 周波数個数
            int freqCnt = 0;
            // 波長リスト
            waveLengthList = new List<double>();
            // フィールド値リスト
            valuesAllList = new List<Complex[]>();

            // ファイル読み込み処理
            try
            {
                // 結果ファイルの読み込みオフセットリスト
                IList<long> datReadFileOfsList = new List<long>();

                using (StreamReader sr = new StreamReader(indexfilename))
                {
                    while (!sr.EndOfStream)
                    {
                        line = sr.ReadLine();
                        tokens = line.Split(delimiter);
                        if (tokens.Length != 2)
                        {
                            MessageBox.Show("結果データのインデックス情報が不正です");
                            return false;
                        }
                        int freqNo = int.Parse(tokens[0]);
                        long fileOfs = long.Parse(tokens[1]);
                        datReadFileOfsList.Add(fileOfs);
                    }
                }
                freqCnt = datReadFileOfsList.Count;
                for (int freqIndex = 0; freqIndex < freqCnt; freqIndex++)
                {
                    Complex[] valuesAll = null;
                    double waveLength;

                    using (StreamReader sr = new StreamReader(filename))
                    {
                        Stream stream = sr.BaseStream;
                        stream.Position = datReadFileOfsList[freqIndex];

                        line = sr.ReadLine();
                        if (line != "S")
                        {
                            MessageBox.Show("開始シーケンスがありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                        line = sr.ReadLine();
                        tokens = line.Split(delimiter);
                        if (tokens[0] != "waveLength")
                        {
                            MessageBox.Show("波長がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                        waveLength = double.Parse(tokens[1]);
                        // 波長を格納
                        waveLengthList.Add(waveLength);

                        // フィールド値の格納場所までスキップ
                        bool findFlg = false;
                        while (!sr.EndOfStream)
                        {
                            line = sr.ReadLine();
                            tokens = line.Split(delimiter);
                            if (tokens.Length == 2 && tokens[0] == "fieldValues")
                            {
                                findFlg = true;
                                break;
                            }
                        }
                        if (!findFlg)
                        {
                            MessageBox.Show("フィールド値がありません");
                            return false;
                        }
                        System.Diagnostics.Debug.Assert(tokens[0] == "fieldValues");
                        int fValueCnt = int.Parse(tokens[1]);
                        valuesAll = new Complex[fValueCnt];
                        for (int ino = 0; ino < fValueCnt; ino++)
                        {
                            line = sr.ReadLine();
                            valuesAll[ino] = MyUtilLib.MyUtil.ComplexParse(line);
                        }
                        // フィールド値ベクトルを格納
                        valuesAllList.Add(valuesAll);
                    }
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
                MessageBox.Show(exception.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return true;
        }
    
    }
}
