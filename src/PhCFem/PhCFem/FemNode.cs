using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PhCFem
{
    /// <summary>
    /// Fem節点クラス
    /// </summary>
    class FemNode
    {
        /// <summary>
        /// 全体節点番号
        /// </summary>
        public int No;
        /// <summary>
        /// 座標(2次元)配列
        /// </summary>
        public double[] Coord;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FemNode()
        {
            No = 0;
            Coord = null;
        }

        /// <summary>
        /// コピー
        /// </summary>
        /// <param name="src"></param>
        public void CP(FemNode src)
        {
            No = src.No;
            Coord = null;
            if (src.Coord != null)
            {
                Coord = new double[src.Coord.Length];
                for (int i = 0; i < src.Coord.Length; i++)
                {
                    Coord[i] = src.Coord[i];
                }
            }
        }
    }
}
