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
    /// Fem計算結果データ(周期構造導波路固有モード)ファイルの読み書き
    /// </summary>
    class FemOutputPeriodicDatFile
    {
        /// <summary>
        /// 出力ファイル名から周期構造導波路固有モード用出力ファイル名を取得する
        /// </summary>
        /// <param name="outputDatFilename"></param>
        /// <returns></returns>
        public static string GetOutputPeriodicDatFilename(string outputDatFilename)
        {
            if (outputDatFilename == "")
            {
                return "";
            }
            return (Path.GetDirectoryName(outputDatFilename) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(outputDatFilename) + Constants.FemOutputPeriodicExt);
        }

        /// <summary>
        /// 計算結果をファイルに出力(追記モード)
        /// 伝搬モードのみを出力
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="freqNo"></param>
        /// <param name="portNo"></param>
        /// <param name="waveLength"></param>
        /// <param name="nodePeriodic"></param>
        /// <param name="toNodePeriodic"></param>
        /// <param name="coordsPeriodic"></param>
        /// <param name="eigenValues"></param>
        /// <param name="eigenVecsPeriodic"></param>
        public static void AppendToFile(
            string filename,
            int freqNo,
            int portNo,
            double waveLength,
            IList<int> nodePeriodic,
            Dictionary<int, int> toNodePeriodic,
            IList<double[]> coordsPeriodic,
            KrdLab.clapack.Complex[] eigenValues,
            KrdLab.clapack.Complex[][] eigenVecsPeriodic
            )
        {
            // 波数
            double k0 = 2.0 * Constants.pi / waveLength;

            if (eigenValues == null)
            {
                return;
            }
            int modeCnt = 0; // 伝搬モードの数
            for (int imode = 0; imode < eigenValues.Length; imode++)
            {
                KrdLab.clapack.Complex beta = eigenValues[imode];
                if (Math.Abs(beta.Imaginary / k0) >= Constants.PrecisionLowerLimit)
                {
                    // 減衰モード
                    break;
                }
                modeCnt++;
            }
            if (modeCnt == 0)
            {
                return;
            }
            int nodeCntB = nodePeriodic.Count;

            long writeFileOfs = 0;  // 書き込み開始位置
            try
            {
                // 追記モードで書き込み
                using (StreamWriter sw = new StreamWriter(filename, true))
                {
                    Stream stream = sw.BaseStream;

                    // 書き込み開始位置を記憶
                    writeFileOfs = stream.Position;
                    
                    // 開始シーケンスの書き込み
                    sw.WriteLine("S");
                    
                    /////////////////////////////////////
                    {
                        // 節点数
                        sw.WriteLine("nodeCntB,{0}", nodeCntB);
                        // 節点番号
                        sw.WriteLine("nodePeriodic");
                        for (int ino = 0; ino < nodeCntB; ino++)
                        {
                            sw.WriteLine("{0}", nodePeriodic[ino]);
                        }
                        // 座標
                        sw.WriteLine("coordsPeriodic");
                        for (int ino = 0; ino < nodeCntB; ino++)
                        {
                            sw.WriteLine("{0},{1}", coordsPeriodic[ino][0], coordsPeriodic[ino][1]);
                        }
                    }
                    /////////////////////////////////////

                    // 周波数番号
                    sw.WriteLine("freqNo,{0}", freqNo);
                    // ポート番号
                    sw.WriteLine("portNo,{0}", portNo);
                    // 波数
                    sw.WriteLine("waveLength,{0}", waveLength);
                    // 伝搬モード数
                    sw.WriteLine("modeCnt,{0}", modeCnt);
                    for (int imode = 0; imode < modeCnt; imode++)
                    {
                        KrdLab.clapack.Complex beta = eigenValues[imode];
                        KrdLab.clapack.Complex[] eigenVecPeriodic = eigenVecsPeriodic[imode];
                        sw.WriteLine("mode,{0}", imode);
                        // 伝搬定数
                        sw.WriteLine("beta,{0}+{1}i", beta.Real, beta.Imaginary);
                        // 固有モード分布
                        System.Diagnostics.Debug.Assert(nodeCntB == eigenVecPeriodic.Length);
                        for (int ino = 0; ino < nodeCntB; ino++)
                        {
                            KrdLab.clapack.Complex fVal = eigenVecPeriodic[ino];
                            sw.WriteLine("{0}+{1}i", fVal.Real, fVal.Imaginary);
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
                    line = string.Format("{0},{1},{2}", freqNo, portNo, writeFileOfs);
                    sw.WriteLine(line);
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
                MessageBox.Show(exception.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /*
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
                IList<int> freqNoList = new List<int>();
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
                        int tmpFreqNo = int.Parse(tokens[0]);
                        int tmpPortNo = int.Parse(tokens[1]);
                        long tmpFOfs = long.Parse(tokens[2]);
                        if (tmpFreqNo < 1)
                        {
                            MessageBox.Show("周波数インデックスが不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return freqCnt;
                        }
                        if (tmpPortNo < 1)
                        {
                            MessageBox.Show("ポート番号が不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return freqCnt;
                        }
                        if (freqNoList.Contains(tmpFreqNo))
                        {
                            // ポート番号違いの同じ周波数
                            continue;
                        }
                        else
                        {
                            freqNoList.Add(tmpFreqNo);
                        }

                        // 周波数が順番に並んでいない場合を考慮
                        if (minFreq > tmpFreqNo)
                        {
                            minFreq = tmpFreqNo;
                        }
                        if (maxFreq < tmpFreqNo)
                        {
                            maxFreq = tmpFreqNo;
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
         */

        public static bool LoadFromFile(
            string filename,
            int freqNo,
            int portNo,
            out double waveLength,
            out IList<int> nodePeriodic,
            out Dictionary<int, int> toNodePeriodic,
            out IList<double[]> coordsPeriodic,
            out KrdLab.clapack.Complex[] eigenValues,
            out KrdLab.clapack.Complex[][] eigenVecsPeriodic
            )
        {
            const char delimiter = ',';

            waveLength = 0.0;
            nodePeriodic = null;
            toNodePeriodic = null;
            coordsPeriodic = null;
            eigenValues = null;
            eigenVecsPeriodic = null;

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
                        int tmpFreqNo = int.Parse(tokens[0]);
                        int tmpPortNo = int.Parse(tokens[1]);
                        long tmpFOfs = long.Parse(tokens[2]);
                        if (tmpFreqNo < 1)
                        {
                            MessageBox.Show("周波数インデックスが不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                        if (tmpPortNo < 1)
                        {
                            MessageBox.Show("ポート番号が不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                        if (freqNo == -1 && portNo == tmpPortNo)
                        {
                            // 最後の結果データの読み込み開始位置を更新
                            readFileOfs = tmpFOfs;
                            findFlg = true;
                        }
                        else if (freqNo == tmpFreqNo && portNo == tmpPortNo)
                        {
                            // 指定周波数、ポート番号の場合
                            readFileOfs = tmpFOfs;
                            findFlg = true;
                            break;
                        }
                        else
                        {
                            // 該当しない
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

                    /////////////////////////////////////
                    {
                        // 節点数
                        line = sr.ReadLine();
                        tokens = line.Split(delimiter);
                        if (tokens.Length != 2 || tokens[0] != "nodeCntB")
                        {
                            MessageBox.Show("節点数がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                        int nodeCntB = int.Parse(tokens[1]);
                        // 節点番号
                        line = sr.ReadLine();
                        if (line != "nodePeriodic")
                        {
                            MessageBox.Show("節点番号がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                        nodePeriodic = new List<int>();
                        toNodePeriodic = new Dictionary<int, int>();
                        for (int ino = 0; ino < nodeCntB; ino++)
                        {
                            line = sr.ReadLine();
                            tokens = line.Split(delimiter);
                            if (tokens.Length != 1)
                            {
                                MessageBox.Show("節点番号がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return false;
                            }
                            int nodeNumber = int.Parse(tokens[0]);
                            nodePeriodic.Add(nodeNumber);
                            toNodePeriodic.Add(nodeNumber, ino);
                        }
                        // 座標
                        line = sr.ReadLine();
                        if (line != "coordsPeriodic")
                        {
                            MessageBox.Show("節点座標がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                        coordsPeriodic = new List<double[]>();
                        for (int ino = 0; ino < nodeCntB; ino++)
                        {
                            line = sr.ReadLine();
                            tokens = line.Split(delimiter);
                            if (tokens.Length != 2)
                            {
                                MessageBox.Show("節点座標がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return false;
                            }
                            double xx = double.Parse(tokens[0]);
                            double yy = double.Parse(tokens[1]);
                            double[] pp = new double[] { xx, yy };
                            coordsPeriodic.Add(pp);
                        }
                    }

                    /////////////////////////////////////

                    // 周波数番号
                    line = sr.ReadLine();
                    tokens = line.Split(delimiter);
                    if (tokens.Length != 2 || tokens[0] != "freqNo")
                    {
                        MessageBox.Show("周波数番号がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    int tmpFreqNo = int.Parse(tokens[1]);
                    if (freqNo != -1 && tmpFreqNo != freqNo)
                    {
                        MessageBox.Show("周波数番号が一致しません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    // ポート番号
                    line = sr.ReadLine();
                    tokens = line.Split(delimiter);
                    if (tokens.Length != 2 || tokens[0] != "portNo")
                    {
                        MessageBox.Show("ポート番号がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    int tmpPortNo = int.Parse(tokens[1]);
                    if (tmpPortNo != portNo)
                    {
                        MessageBox.Show("ポート番号が一致しません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    // 波数
                    line = sr.ReadLine();
                    tokens = line.Split(delimiter);
                    if (tokens.Length != 2 || tokens[0] != "waveLength")
                    {
                        MessageBox.Show("波長がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    waveLength = double.Parse(tokens[1]);

                    // 伝搬モード数
                    line = sr.ReadLine();
                    tokens = line.Split(delimiter);
                    if (tokens.Length != 2 || tokens[0] != "modeCnt")
                    {
                        MessageBox.Show("モード数がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    int modeCnt = int.Parse(tokens[1]);
                    eigenValues = new KrdLab.clapack.Complex[modeCnt];
                    eigenVecsPeriodic = new KrdLab.clapack.Complex[modeCnt][];
                    for (int imode = 0; imode < modeCnt; imode++)
                    {
                        line = sr.ReadLine();
                        tokens = line.Split(delimiter);
                        if (tokens.Length != 2 || tokens[0] != "mode")
                        {
                            MessageBox.Show("モードがありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                        int tmpModeIndex = int.Parse(tokens[1]);
                        if (tmpModeIndex != imode)
                        {
                            MessageBox.Show("モードインデックスが不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }

                        // 伝搬定数
                        line = sr.ReadLine();
                        tokens = line.Split(delimiter);
                        if (tokens.Length != 2 || tokens[0] != "beta")
                        {
                            MessageBox.Show("伝搬定数がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                        KrdLab.clapack.Complex beta = MyUtilLib.MyUtil.ComplexParse(tokens[1]);

                        // 固有モード分布
                        int nodeCntB = nodePeriodic.Count;
                        eigenVecsPeriodic[imode] = new KrdLab.clapack.Complex[nodeCntB];
                        for (int ino = 0; ino < nodeCntB; ino++)
                        {
                            line = sr.ReadLine();
                            tokens = line.Split(delimiter);
                            if (tokens.Length != 1)
                            {
                                MessageBox.Show("固有モード分布がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return false;
                            }
                            KrdLab.clapack.Complex fVal = MyUtilLib.MyUtil.ComplexParse(tokens[0]);
                            eigenVecsPeriodic[imode][ino] = fVal;
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

    }
}
