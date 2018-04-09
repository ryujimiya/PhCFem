using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Forms; // MessageBox
using DelFEM4NetCad;
using DelFEM4NetCom;
using DelFEM4NetCad.View;
using DelFEM4NetCom.View;
using DelFEM4NetFem;
using DelFEM4NetFem.Field;
using DelFEM4NetFem.Field.View;
using DelFEM4NetFem.Eqn;
using DelFEM4NetFem.Ls;
using DelFEM4NetMsh;
using DelFEM4NetMsh.View;
using DelFEM4NetMatVec;
using DelFEM4NetLsSol;
using MyUtilLib.Matrix;

namespace wg2d
{
    ///////////////////////////////////////////////////////////////
    //  DelFEM4Net サンプル
    //  導波管伝達問題 2D
    //
    //      Copyright (C) 2012-2013 ryujimiya
    //
    //      e-mail: ryujimiya@mail.goo.ne.jp
    ///////////////////////////////////////////////////////////////
    /// <summary>
    /// 導波管解析のユーティリティ
    /// </summary>
    class WgUtil
    {
        ////////////////////////////////////////////////////////////////////////
        // 型
        ////////////////////////////////////////////////////////////////////////

        /////////////////////////////////////////////////
        // 定数
        /////////////////////////////////////////////////
        /// <summary>
        /// π
        /// </summary>
        public const double pi = Math.PI;//3.1416;
        /// <summary>
        /// 真空中の光速
        /// </summary>
        public const double c0 = 2.99792458e+8;
        /// <summary>
        /// 真空中の透磁率
        /// </summary>
        public const double mu0 = 4.0e-7 * pi;
        /// <summary>
        /// 真空中の誘電率
        /// </summary>1
        public const double eps0 = 8.85418782e-12;//1.0 / (mu0 * c0 * c0);

        /// <summary>
        /// 境界上の節点番号の取得
        /// </summary>
        /// <param name="world">ワールド座標系</param>
        /// <param name="fieldValId">フィールド値ID</param>
        /// <param name="no_c_all">全体節点番号配列</param>
        /// <param name="to_no_boundary">全体節点番号→境界上の節点番号のマップ</param>
        /// <returns></returns>
        public static bool GetBoundaryNodeList(CFieldWorld world, uint fieldValId, out uint[] no_c_all, out Dictionary<uint, uint> to_no_boundary)
        {
            no_c_all = null;
            to_no_boundary = null;

            // フィールドを取得
            CField valField = world.GetField(fieldValId);
            if (valField.GetFieldType() != FIELD_TYPE.ZSCALAR)
            {
                return false;
            }
            IList<uint> aIdEA = valField.GetAryIdEA();

            // 全体節点番号→境界節点番号変換テーブル(to_no_boundary)作成
            to_no_boundary = new Dictionary<uint, uint>();

            foreach (uint eaId in aIdEA)
            {
                bool res = getBoundaryNodeList_EachElementAry(world, fieldValId, eaId, to_no_boundary);
                if (!res)
                {
                    return false;
                }
            }
            //境界節点番号→全体節点番号変換テーブル(no_c_all)作成 
            no_c_all = new uint[to_no_boundary.Count];
            foreach (KeyValuePair<uint, uint> kvp in to_no_boundary)
            {
                uint ino_boundary = kvp.Value;
                uint ino = kvp.Key;
                no_c_all[ino_boundary] = ino;
            }

            return true;
        }

        /// <summary>
        /// 境界上の節点番号の取得(要素アレイ単位)
        /// </summary>
        /// <param name="world">ワールド座標系</param>
        /// <param name="fieldValId">フィールド値ID</param>
        /// <param name="eaId">要素アレイID</param>
        /// <param name="to_no_boundary">全体節点番号→境界上節点番号マップ</param>
        /// <returns></returns>
        private static bool getBoundaryNodeList_EachElementAry(CFieldWorld world, uint fieldValId, uint eaId, Dictionary<uint, uint> to_no_boundary)
        {
            // 要素アレイを取得する
            CElemAry ea = world.GetEA(eaId);
            System.Diagnostics.Debug.Assert(ea.ElemType() == ELEM_TYPE.LINE);
            if (ea.ElemType() != ELEM_TYPE.LINE)
            {
                return false;
            }

            // フィールドを取得
            CField valField = world.GetField(fieldValId);
            // 座標セグメントを取得
            CElemAry.CElemSeg es_c_co = valField.GetElemSeg(eaId, ELSEG_TYPE.CORNER, false, world);

            //境界節点番号→全体節点番号変換テーブル(no_c_all)作成
            // 全体節点番号→境界節点番号変換テーブル(to_no_boundary)作成
            uint node_cnt = ea.Size() + 1;  // 全節点数

            // 線要素の節点数
            uint nno = 2;
            uint[] no_c = new uint[nno]; // 要素節点の全体節点番号

            for (uint ielem = 0; ielem < ea.Size(); ielem++)
            {
                // 要素配列から要素セグメントの節点番号を取り出す
                es_c_co.GetNodes(ielem, no_c);

                for (uint ino = 0; ino < nno; ino++)
                {
                    if (!to_no_boundary.ContainsKey(no_c[ino]))
                    {
                        uint ino_boundary_tmp = (uint)to_no_boundary.Count;
                        to_no_boundary[no_c[ino]] = ino_boundary_tmp;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 境界上の節点番号と座標の取得
        /// </summary>
        /// <param name="world">ワールド座標系</param>
        /// <param name="fieldValId">フィールド値ID</param>
        /// <param name="no_c_all">全体節点番号配列</param>
        /// <param name="to_no_boundary">全体節点番号→境界上の節点番号のマップ</param>
        /// <param name="coord_c_all">座標リスト</param>
        /// <returns></returns>
        public static bool GetBoundaryCoordList(
            CFieldWorld world, uint fieldValId,
            out uint[] no_c_all, out Dictionary<uint, uint> to_no_boundary, out double[][] coord_c_all)
        {
            return GetBoundaryCoordList(
                world, fieldValId,
                0.0, null,
                out no_c_all, out to_no_boundary, out coord_c_all);
        }

        /// <summary>
        /// 境界上の節点番号と座標の取得
        /// </summary>
        /// <param name="world">ワールド座標系</param>
        /// <param name="fieldValId">フィールド値ID</param>
        /// <param name="no_c_all">全体節点番号配列</param>
        /// <param name="to_no_boundary">全体節点番号→境界上の節点番号のマップ</param>
        /// <param name="coord_c_all">座標リスト</param>
        /// <returns></returns>
        public static bool GetBoundaryCoordList(
            CFieldWorld world, uint fieldValId,
            double rotAngle, double[] rotOrigin,
            out uint[] no_c_all, out Dictionary<uint, uint> to_no_boundary, out double[][] coord_c_all)
        {
            no_c_all = null;
            to_no_boundary = null;
            coord_c_all = null;

            // フィールドを取得
            CField valField = world.GetField(fieldValId);
            if (valField.GetFieldType() != FIELD_TYPE.ZSCALAR)
            {
                return false;
            }
            IList<uint> aIdEA = valField.GetAryIdEA();

            // 全体節点番号→境界節点番号変換テーブル(to_no_boundary)作成
            to_no_boundary = new Dictionary<uint, uint>();
            IList<double[]> coord_c_list = new List<double[]>();

            foreach (uint eaId in aIdEA)
            {
                bool res = getBoundaryCoordList_EachElementAry(
                    world, fieldValId, eaId,
                    rotAngle, rotOrigin,
                    ref to_no_boundary, ref coord_c_list);
                if (!res)
                {
                    return false;
                }
            }
            //境界節点番号→全体節点番号変換テーブル(no_c_all)作成 
            no_c_all = new uint[to_no_boundary.Count];
            foreach (KeyValuePair<uint, uint> kvp in to_no_boundary)
            {
                uint ino_boundary = kvp.Value;
                uint ino = kvp.Key;
                no_c_all[ino_boundary] = ino;
            }
            coord_c_all = coord_c_list.ToArray();

            return true;
        }

        /// <summary>
        /// 境界上の節点番号と座標の取得(要素アレイ単位)
        /// </summary>
        /// <param name="world">ワールド座標系</param>
        /// <param name="fieldValId">フィールド値ID</param>
        /// <param name="eaId">要素アレイID</param>
        /// <param name="to_no_boundary">全体節点番号→境界上節点番号マップ</param>
        /// <returns></returns>
        private static bool getBoundaryCoordList_EachElementAry(
            CFieldWorld world, uint fieldValId, uint eaId,
            double rotAngle, double[] rotOrigin,
            ref Dictionary<uint, uint> to_no_boundary, ref IList<double[]> coord_c_list)
        {
            // 要素アレイを取得する
            CElemAry ea = world.GetEA(eaId);
            System.Diagnostics.Debug.Assert(ea.ElemType() == ELEM_TYPE.LINE);
            if (ea.ElemType() != ELEM_TYPE.LINE)
            {
                return false;
            }

            // フィールドを取得
            CField valField = world.GetField(fieldValId);
            // 座標セグメントを取得
            CElemAry.CElemSeg es_c_co = valField.GetElemSeg(eaId, ELSEG_TYPE.CORNER, false, world);
            // 座標のノードセグメントを取得
            CNodeAry.CNodeSeg ns_c_co = valField.GetNodeSeg(ELSEG_TYPE.CORNER, false, world);

            //境界節点番号→全体節点番号変換テーブル(no_c_all)作成
            // 全体節点番号→境界節点番号変換テーブル(to_no_boundary)作成
            uint node_cnt = ea.Size() + 1;  // 全節点数

            // 線要素の節点数
            uint nno = 2;
            // 座標の次元
            uint ndim = 2;
            // 要素節点の全体節点番号
            uint[] no_c = new uint[nno];
            // 要素節点の座標
            double[][] coord_c = new double[nno][];
            for (int inoes = 0; inoes < nno; inoes++)
            {
                coord_c[inoes] = new double[ndim];
            }

            for (uint ielem = 0; ielem < ea.Size(); ielem++)
            {
                // 要素配列から要素セグメントの節点番号を取り出す
                es_c_co.GetNodes(ielem, no_c);
                // 座標を取得
                for (uint inoes = 0; inoes < nno; inoes++)
                {
                    double[] tmpval = null;
                    ns_c_co.GetValue(no_c[inoes], out tmpval);
                    System.Diagnostics.Debug.Assert(tmpval.Length == ndim);
                    for (int i = 0; i < tmpval.Length; i++)
                    {
                        coord_c[inoes][i] = tmpval[i];
                    }
                }
                if (Math.Abs(rotAngle) >= Constants.PrecisionLowerLimit)
                {
                    // 座標を回転移動する
                    for (uint inoes = 0; inoes < nno; inoes++)
                    {
                        double[] srcPt = coord_c[inoes];
                        double[] destPt = GetRotCoord(srcPt, rotAngle, rotOrigin);
                        for (int i = 0; i < ndim; i++)
                        {
                            coord_c[inoes][i] = destPt[i];
                        }
                    }
                }

                for (uint ino = 0; ino < nno; ino++)
                {
                    if (!to_no_boundary.ContainsKey(no_c[ino]))
                    {
                        uint ino_boundary_tmp = (uint)to_no_boundary.Count;
                        to_no_boundary[no_c[ino]] = ino_boundary_tmp;
                        coord_c_list.Add(new double[] { coord_c[ino][0], coord_c[ino][1] });
                        System.Diagnostics.Debug.Assert(coord_c_list.Count == (ino_boundary_tmp + 1));
                    }
                }

            }
            return true;
        }

        /// <summary>
        /// 回転移動する
        /// </summary>
        /// <param name="srcPt"></param>
        /// <param name="rotAngle"></param>
        /// <param name="rotOrigin"></param>
        /// <returns></returns>
        public static double[] GetRotCoord(double[] srcPt, double rotAngle, double[] rotOrigin = null)
        {
            double[] destPt = new double[2];
            double x0 = 0;
            double y0 = 0;
            if (rotOrigin != null)
            {
                x0 = rotOrigin[0];
                y0 = rotOrigin[1];
            }
            double cosA = Math.Cos(rotAngle);
            double sinA = Math.Sin(rotAngle);
            destPt[0] = cosA * (srcPt[0] - x0) + sinA * (srcPt[1] - y0);
            destPt[1] = -sinA * (srcPt[0] - x0) + cosA * (srcPt[1] - y0);
            return destPt;
        }

        /// <summary>
        /// GCを実行する
        /// </summary>
        public static void GC_Collect()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("TotalMemory: {0}", GC.GetTotalMemory(false));
                // GC.Collect 呼び出し後に GC.WaitForPendingFinalizers を呼び出します。これにより、すべてのオブジェクトに対するファイナライザが呼び出されるまで、現在のスレッドは待機します。
                // ファイナライザ作動後は、回収すべき、(ファイナライズされたばかりの) アクセス不可能なオブジェクトが増えます。もう1度 GC.Collect を呼び出し、それらを回収します。
                GC.Collect(); // アクセス不可能なオブジェクトを除去
                GC.WaitForPendingFinalizers(); // ファイナライゼーションが終わるまでスレッド待機
                GC.Collect(0); // ファイナライズされたばかりのオブジェクトに関連するメモリを開放
                System.Diagnostics.Debug.WriteLine("TotalMemory: {0}", GC.GetTotalMemory(false));
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
                MessageBox.Show(exception.Message);
            }
        }

        /// <summary>
        /// ループ内の節点番号と座標の取得
        /// </summary>
        /// <param name="world">ワールド座標系</param>
        /// <param name="fieldValId">フィールド値ID</param>
        /// <param name="no_c_all">全体節点番号配列</param>
        /// <param name="to_no_boundary">全体節点番号→境界上の節点番号のマップ</param>
        /// <param name="coord_c_all">座標リスト</param>
        /// <returns></returns>
        public static bool GetLoopCoordList(
            CFieldWorld world, uint fieldValId,
            out uint[] no_c_all, out Dictionary<uint, uint> to_no_loop, out double[][] coord_c_all, out uint[][] elem_no_c, out uint[] elem_loopId)
        {
            return GetLoopCoordList(
                world, fieldValId,
                0.0, null,
                out no_c_all, out to_no_loop, out coord_c_all, out elem_no_c, out elem_loopId);
        }

        /// <summary>
        /// ループ内の節点番号と座標の取得
        /// </summary>
        /// <param name="world">ワールド座標系</param>
        /// <param name="fieldValId">フィールド値ID</param>
        /// <param name="no_c_all">全体節点番号配列</param>
        /// <param name="to_no_boundary">全体節点番号→境界上の節点番号のマップ</param>
        /// <param name="coord_c_all">座標リスト</param>
        /// <returns></returns>
        public static bool GetLoopCoordList(CFieldWorld world, uint fieldValId,
            double rotAngle, double[] rotOrigin,
            out uint[] no_c_all, out Dictionary<uint, uint> to_no_loop, out double[][] coord_c_all, out uint[][] elem_no_c, out uint[] elem_loopId)
        {
            no_c_all = null;
            to_no_loop = null;
            coord_c_all = null;
            elem_no_c = null;
            elem_loopId = null;

            // フィールドを取得
            CField valField = world.GetField(fieldValId);
            if (valField.GetFieldType() != FIELD_TYPE.ZSCALAR)
            {
                return false;
            }
            IList<uint> aIdEA = valField.GetAryIdEA();

            // 全体節点番号→ループ内節点番号変換テーブル作成
            to_no_loop = new Dictionary<uint, uint>();
            IList<double[]> coord_c_list = new List<double[]>();

            // 要素の節点番号リスト
            IList<uint[]> elem_no_c_list = new List<uint[]>();
            // 要素のループIDリスト
            IList<uint> elem_loopId_list = new List<uint>();

            foreach (uint eaId in aIdEA)
            {
                bool res = getLoopCoordList_EachElementAry(
                    world, fieldValId, eaId,
                    rotAngle, rotOrigin,
                    ref to_no_loop, ref coord_c_list, ref elem_no_c_list, ref elem_loopId_list);
                if (!res)
                {
                    return false;
                }
            }
            //境界節点番号→全体節点番号変換テーブル(no_c_all)作成 
            no_c_all = new uint[to_no_loop.Count];
            foreach (KeyValuePair<uint, uint> kvp in to_no_loop)
            {
                uint ino_boundary = kvp.Value;
                uint ino = kvp.Key;
                no_c_all[ino_boundary] = ino;
            }
            coord_c_all = coord_c_list.ToArray();

            int elemCnt = elem_no_c_list.Count;
            elem_no_c = new uint[elemCnt][];
            elem_loopId = new uint[elemCnt];
            for (int ie = 0; ie < elemCnt; ie++)
            {
                elem_no_c[ie] = new uint[3];
                for (int i = 0; i < 3; i++)
                {
                    elem_no_c[ie][i] = elem_no_c_list[ie][i];
                    elem_loopId[ie] = elem_loopId_list[ie];
                }
            }

            return true;
        }

        /// <summary>
        /// ループ内の節点番号と座標の取得(要素アレイ単位)
        /// </summary>
        /// <param name="world">ワールド座標系</param>
        /// <param name="fieldValId">フィールド値ID</param>
        /// <param name="eaId">要素アレイID</param>
        /// <param name="to_no_boundary">全体節点番号→境界上節点番号マップ</param>
        /// <returns></returns>
        private static bool getLoopCoordList_EachElementAry(
            CFieldWorld world, uint fieldValId, uint eaId,
            double rotAngle, double[] rotOrigin,
            ref Dictionary<uint, uint> to_no_loop, ref IList<double[]> coord_c_list, ref IList<uint[]> elem_no_c_list, ref IList<uint> elem_loopId_list)
        {
            // 要素アレイを取得する
            CElemAry ea = world.GetEA(eaId);
            System.Diagnostics.Debug.Assert(ea.ElemType() == ELEM_TYPE.TRI);
            if (ea.ElemType() != ELEM_TYPE.TRI)
            {
                return false;
            }

            // フィールドを取得
            CField valField = world.GetField(fieldValId);
            // 座標セグメントを取得
            CElemAry.CElemSeg es_c_co = valField.GetElemSeg(eaId, ELSEG_TYPE.CORNER, false, world);
            // 座標のノードセグメントを取得
            CNodeAry.CNodeSeg ns_c_co = valField.GetNodeSeg(ELSEG_TYPE.CORNER, false, world);

            uint node_cnt = ea.Size() + 1;  // 全節点数

            // 三角形要素の節点数
            uint nno = 3;
            // 座標の次元
            uint ndim = 2;
            // 要素節点の全体節点番号
            uint[] no_c = new uint[nno];
            // 要素節点の座標
            double[][] coord_c = new double[nno][];
            for (int inoes = 0; inoes < nno; inoes++)
            {
                coord_c[inoes] = new double[ndim];
            }

            for (uint ielem = 0; ielem < ea.Size(); ielem++)
            {
                // 要素配列から要素セグメントの節点番号を取り出す
                es_c_co.GetNodes(ielem, no_c);

                // ループID
                elem_loopId_list.Add(eaId);
                // 要素内節点番号
                {
                    uint[] work_no_c = new uint[nno];
                    no_c.CopyTo(work_no_c, 0);
                    elem_no_c_list.Add(work_no_c);
                }

                // 座標を取得
                for (uint inoes = 0; inoes < nno; inoes++)
                {
                    double[] tmpval = null;
                    ns_c_co.GetValue(no_c[inoes], out tmpval);
                    System.Diagnostics.Debug.Assert(tmpval.Length == ndim);
                    for (int i = 0; i < tmpval.Length; i++)
                    {
                        coord_c[inoes][i] = tmpval[i];
                    }
                }
                if (Math.Abs(rotAngle) >= Constants.PrecisionLowerLimit)
                {
                    // 座標を回転移動する
                    for (uint inoes = 0; inoes < nno; inoes++)
                    {
                        double[] srcPt = coord_c[inoes];
                        double[] destPt = GetRotCoord(srcPt, rotAngle, rotOrigin);
                        for (int i = 0; i < ndim; i++)
                        {
                            coord_c[inoes][i] = destPt[i];
                        }
                    }
                }

                for (uint ino = 0; ino < nno; ino++)
                {
                    if (!to_no_loop.ContainsKey(no_c[ino]))
                    {
                        uint ino_loop_tmp = (uint)to_no_loop.Count;
                        to_no_loop[no_c[ino]] = ino_loop_tmp;
                        coord_c_list.Add(new double[] { coord_c[ino][0], coord_c[ino][1] });
                        System.Diagnostics.Debug.Assert(coord_c_list.Count == (ino_loop_tmp + 1));
                    }
                }

            }
            return true;
        }


    }
}
