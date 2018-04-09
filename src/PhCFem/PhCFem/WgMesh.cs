using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;
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
using Tao.OpenGl;
using wg2d;

namespace PhCFem
{
    /// <summary>
    /// 三角形メッシュ作成(DelFEM)
    /// </summary>
    class WgMesh
    {
        /// <summary>
        ///  欠陥部セルのメッシュ取得
        /// </summary>
        /// <param name="ndivForOneLattice">格子1辺の分割数</param>
        /// <param name="ptsInCell">セル内節点の座標リスト</param>
        /// <param name="nodeBInCell">セル境界節点の節点番号リスト</param>
        /// <param name="elemNodeInCell">要素内節点の節点番号リスト</param>
        public static void GetCellMesh_Defect_TriFirstOrder(
            int ndivForOneLattice,
            out double[,] ptsInCell,
            out uint[,] nodeBInCell,
            out uint[,] elemNodeInCell
            )
        {
            uint[] dummy_elemLoopIdInCell = null;
            uint dummy_RodLoopId = 0;
            double dummy_rodRadiusRatio = 0.0;
            int dummy_rodCircleDiv = 0;
            int dummy_rodRadiusDiv = 0;

            uint FieldValId = 0;
            uint FieldLoopId = 0;
            uint FieldForceBcId = 0;
            IList<uint> FieldPortBcIds = new List<uint>();
            CFieldWorld World = new CFieldWorld();
            mkMeshCell_Rod_Tri_FirstOrder(
                false, /* isRodCell : fase 欠陥部 true ロッド部 */
                ndivForOneLattice,
                dummy_rodRadiusRatio,
                dummy_rodCircleDiv,
                dummy_rodRadiusDiv,
                ref World,
                ref FieldValId,
                ref FieldLoopId,
                ref FieldForceBcId,
                ref FieldPortBcIds,
                out dummy_RodLoopId);
            getCoordListFromMeshCell_Tri_FirstOrder(
                World,
                FieldLoopId,
                FieldForceBcId,
                FieldPortBcIds,
                out ptsInCell,
                out nodeBInCell,
                out dummy_elemLoopIdInCell,
                out elemNodeInCell
                );

            World.Dispose();
            World = null;
        }

        /// <summary>
        /// ロッド部セルメッシュの取得
        /// </summary>
        /// <param name="ndivForOneLattice">格子1辺の分割数</param>
        /// <param name="rodRadiusRatio">ロッドの半径割合</param>
        /// <param name="rodCircleDiv">ロッドの円周方向分割数</param>
        /// <param name="rodRadiusDiv">ロッドの半径方向分割数(1でもメッシュサイズが小さければ複数に分割される)</param>
        /// <param name="ptsInCell">セル内節点の座標リスト</param>
        /// <param name="nodeBInCell">セル境界節点の節点番号リスト</param>
        /// <param name="elemLoopIdInCell">セル内要素のワールド座標系ループIDリスト</param>
        /// <param name="RodLoopId">ロッドのワールド座標系ループID</param>
        /// <param name="elemNodeInCell">要素内節点の節点番号リスト</param>
        public static void GetCellMesh_Rod_TriFirstOrder(
            int ndivForOneLattice,
            double rodRadiusRatio,
            int rodCircleDiv,
            int rodRadiusDiv,
            out double[,] ptsInCell,
            out uint[,] nodeBInCell,
            out uint[] elemLoopIdInCell,
            out uint RodLoopId,
            out uint[,] elemNodeInCell
            )
        {
            uint FieldValId = 0;
            uint FieldLoopId = 0;
            uint FieldForceBcId = 0;
            IList<uint> FieldPortBcIds = new List<uint>();
            CFieldWorld World = new CFieldWorld();
            mkMeshCell_Rod_Tri_FirstOrder(
                true, /* isRodCell */
                ndivForOneLattice,
                rodRadiusRatio,
                rodCircleDiv,
                rodRadiusDiv,
                ref World,
                ref FieldValId,
                ref FieldLoopId,
                ref FieldForceBcId,
                ref FieldPortBcIds,
                out RodLoopId);
            getCoordListFromMeshCell_Tri_FirstOrder(
                World,
                FieldLoopId,
                FieldForceBcId,
                FieldPortBcIds,
                out ptsInCell,
                out nodeBInCell,
                out elemLoopIdInCell,
                out elemNodeInCell
                );

            World.Dispose();
            World = null;

        }

        /// <summary>
        /// セルのメッシュ座標一覧取得
        /// </summary>
        /// <param name="World"></param>
        /// <param name="FieldLoopId"></param>
        /// <param name="FieldForceBcId"></param>
        /// <param name="FieldPortBcIds"></param>
        /// <param name="ptsInCell"></param>
        /// <param name="nodeBInCell"></param>
        /// <param name="elemLoopIdInCell"></param>
        /// <param name="elemNodeInCell"></param>
        private static void getCoordListFromMeshCell_Tri_FirstOrder(
            CFieldWorld World,
            uint FieldLoopId,
            uint FieldForceBcId,
            IList<uint> FieldPortBcIds,
            out double[,] ptsInCell,
            out uint[,] nodeBInCell,
            out uint[] elemLoopIdInCell,
            out uint[,] elemNodeInCell
            )
        {
            // 全節点数を取得する
            uint node_cnt = 0;
            //node_cnt = WgUtilForPeriodicEigenBetaSpecified.GetNodeCnt(World, FieldLoopId);
            double[][] coord_c_all = null;
            uint[][] elem_no_c = null;
            uint[] elem_loopId = null;
            {
                uint[] no_c_all_tmp = null;
                Dictionary<uint, uint> to_no_all_tmp = null;
                double[][] coord_c_all_tmp = null;
                WgUtil.GetLoopCoordList(World, FieldLoopId, out no_c_all_tmp, out to_no_all_tmp, out coord_c_all_tmp, out elem_no_c, out elem_loopId);
                node_cnt = (uint)no_c_all_tmp.Length;

                // 座標リストを節点番号順に並び替えて格納
                coord_c_all = new double[node_cnt][];
                for (int ino = 0; ino < node_cnt; ino++)
                {
                    uint nodeNumber = no_c_all_tmp[ino];
                    double[] coord = coord_c_all_tmp[ino];
                    coord_c_all[nodeNumber] = coord;
                }
            }

            System.Diagnostics.Debug.WriteLine("node_cnt: {0}", node_cnt);

            // 境界の節点リストを取得する
            uint[] no_c_all_fieldForceBcId = null;
            Dictionary<uint, uint> to_no_boundary_fieldForceBcId = null;
            if (FieldForceBcId != 0)
            {
                WgUtil.GetBoundaryNodeList(World, FieldForceBcId, out no_c_all_fieldForceBcId, out to_no_boundary_fieldForceBcId);
            }
            int boundaryCnt = FieldPortBcIds.Count;
            IList<uint[]> no_c_all_fieldPortBcId_list = new List<uint[]>();
            IList<Dictionary<uint, uint>> to_no_boundary_fieldPortBcId_list = new List<Dictionary<uint, uint>>();
            for (int i = 0; i < boundaryCnt; i++)
            {
                uint[] work_no_c_all_fieldPortBcId = null;
                Dictionary<uint, uint> work_to_no_boundary_fieldPortBcId = null;
                uint work_fieldPortBcId = FieldPortBcIds[i];
                WgUtil.GetBoundaryNodeList(World, work_fieldPortBcId, out work_no_c_all_fieldPortBcId, out work_to_no_boundary_fieldPortBcId);
                no_c_all_fieldPortBcId_list.Add(work_no_c_all_fieldPortBcId);
                to_no_boundary_fieldPortBcId_list.Add(work_to_no_boundary_fieldPortBcId);
            }

            {
                ptsInCell = new double[node_cnt, 2];
                for (int ino = 0; ino < node_cnt; ino++)
                {
                    for (int idim = 0; idim < 2; idim++)
                    {
                        ptsInCell[ino, idim] = coord_c_all[ino][idim];
                    }
                }
                int nodeCntB1 = no_c_all_fieldPortBcId_list[0].Length;
                nodeBInCell = new uint[boundaryCnt, nodeCntB1]; // 正方格子、上下左右境界の節点数は同じ
                for (int boundaryIndex = 0; boundaryIndex < boundaryCnt; boundaryIndex++)
                {
                    uint[] work_no_c_all_B = no_c_all_fieldPortBcId_list[boundaryIndex];
                    System.Diagnostics.Debug.Assert(nodeCntB1 == work_no_c_all_B.Length); // 正方格子、上下左右境界の節点数は同じ
                    for (int inoB = 0; inoB < work_no_c_all_B.Length; inoB++)
                    {
                        nodeBInCell[boundaryIndex, inoB] = work_no_c_all_B[inoB];
                    }
                }
                int elemCnt = elem_loopId.Length;
                elemLoopIdInCell = new uint[elemCnt];
                for (int ie = 0; ie < elemCnt; ie++)
                {
                    elemLoopIdInCell[ie] = elem_loopId[ie];
                }
                elemNodeInCell = new uint[elemCnt, 3];
                for (int ie = 0; ie < elemCnt; ie++)
                {
                    for (int inoe = 0; inoe < 3; inoe++)
                    {
                        elemNodeInCell[ie, inoe] = elem_no_c[ie][inoe];
                    }
                }
            }

            /*
            using (StreamWriter sw = new StreamWriter("tmp.txt"))
            {
                sw.WriteLine("//// {0}", node_cnt);
                for (int ino = 0; ino < node_cnt; ino++)
                {
                    sw.WriteLine("{{{0}, {1}}},", coord_c_all[ino][0], coord_c_all[ino][1]);
                }
                sw.WriteLine("//// {0}", boundaryCnt);
                for (int boundaryIndex = 0; boundaryIndex < boundaryCnt; boundaryIndex++)
                {
                    uint[] work_no_c_all_B = no_c_all_fieldPortBcId_list[boundaryIndex];
                    sw.WriteLine("//// {0}", work_no_c_all_B.Length);
                    for (int inoB = 0; inoB < work_no_c_all_B.Length; inoB++)
                    {
                        sw.WriteLine("{0},", work_no_c_all_B[inoB]);
                    }
                }
                int elemCnt = elem_loopId.Length;
                sw.WriteLine("//// {0}", elemCnt);
                for (int ie = 0; ie < elemCnt; ie++)
                {
                    sw.WriteLine("{0},", elem_loopId[ie]);
                }
                sw.WriteLine("//// {0}", elemCnt);
                for (int ie = 0; ie < elemCnt; ie++)
                {
                    sw.WriteLine("{{{0}, {1}, {2}}},", elem_no_c[ie][0], elem_no_c[ie][1], elem_no_c[ie][2]);
                }
            }
             */
        }

        /// <summary>
        /// ロッド部セルのメッシュ作成
        /// </summary>
        /// <param name="isRodCell">セルの種類がロッド？</param>
        /// <param name="ndivForOneLattice">格子1辺の分割数</param>
        /// <param name="rodRadiusRatio">ロッドの半径割合</param>
        /// <param name="rodCircleDiv">ロッドの円周方向分割数</param>
        /// <param name="rodRadiusDiv">ロッドの半径方向分割数(1でもメッシュサイズが小さければ複数に分割される)</param>
        /// <param name="World">DelFEMワールド座標系</param>
        /// <param name="FieldValId">DelFEM値のフィールドID</param>
        /// <param name="FieldLoopId">DelFEMループのフィールドID</param>
        /// <param name="FieldForceBcId">DelFEM強制境界のフィールドID</param>
        /// <param name="FieldPortBcIds">DelFEM境界のフィールドID</param>
        /// <param name="RodLoopId">ロッドのワールド座標系ループID</param>
        private static void mkMeshCell_Rod_Tri_FirstOrder(
            bool isRodCell,
            int ndivForOneLattice,
            double rodRadiusRatio,
            int rodCircleDiv,
            int rodRadiusDiv,
            ref CFieldWorld World,
            ref uint FieldValId,
            ref uint FieldLoopId,
            ref uint FieldForceBcId,
            ref IList<uint> FieldPortBcIds,
            out uint RodLoopId
            )
        {
            // Y方向周期構造距離
            //  セルの領域は1.0 x 1.0に規格化されてるものとする
            double periodicDistanceY = 1.0;
            // 格子定数
            double latticeA = periodicDistanceY;
            // 周期構造距離
            double periodicDistanceX = latticeA;
            // ロッドの半径
            double rodRadius = rodRadiusRatio * latticeA;
            // ロッドの比誘電率
            //double rodEps = 3.4 * 3.4;
            // 格子１辺の分割数
            //int ndivForOneLattice = 8;
            // 境界上の総分割数
            int ndiv = ndivForOneLattice;
            // ロッドの円周分割数
            ////int rodCircleDiv = 12;
            //int rodCircleDiv = 8;
            // ロッドの半径の分割数
            ////int rodRadiusDiv = 4;
            //int rodRadiusDiv = 1;
            // メッシュの長さ
            double meshL = 1.05 * periodicDistanceY / ndiv;

            // Cad
            uint baseLoopId = 0;
            IList<uint> rodLoopIds = new List<uint>();
            // ワールド座標系
            uint baseId = 0;
            CIDConvEAMshCad conv = null;
            using (CCadObj2D cad2d = new CCadObj2D())
            {
                //------------------------------------------------------------------
                // 図面作成
                //------------------------------------------------------------------
                // 領域を追加
                {
                    List<CVector2D> pts = new List<CVector2D>();
                    pts.Add(new CVector2D(0.0, periodicDistanceY));
                    pts.Add(new CVector2D(0.0, 0.0));
                    pts.Add(new CVector2D(periodicDistanceX, 0.0));
                    pts.Add(new CVector2D(periodicDistanceX, periodicDistanceY));
                    // 多角形追加
                    uint lId = cad2d.AddPolygon(pts).id_l_add;
                    System.Diagnostics.Debug.Assert(lId != 0);
                    baseLoopId = lId;
                }
                // 周期構造境界上の頂点を追加
                //  逆から追加しているのは、頂点によって新たに生成される辺に頂点を追加しないようにするため
                // 境界1：左
                {
                    uint id_e = 1;
                    double x1 = 0.0;
                    double y1 = periodicDistanceY;
                    double x2 = x1;
                    double y2 = 0.0;
                    WgCadUtil.DivideBoundary(cad2d, id_e, ndiv, x1, y1, x2, y2);
                }
                // 境界2：右
                {
                    uint id_e = 3;
                    double x1 = periodicDistanceX;
                    double y1 = 0.0;
                    double x2 = x1;
                    double y2 = periodicDistanceY;
                    WgCadUtil.DivideBoundary(cad2d, id_e, ndiv, x1, y1, x2, y2);
                }
                // 境界3：下
                {
                    uint id_e = 2;
                    double x1 = 0.0;
                    double y1 = 0.0;
                    double x2 = periodicDistanceX;
                    double y2 = y1;
                    WgCadUtil.DivideBoundary(cad2d, id_e, ndiv, x1, y1, x2, y2);
                }
                // 境界4：上
                {
                    uint id_e = 4;
                    double x1 = periodicDistanceX;
                    double y1 = periodicDistanceY;
                    double x2 = 0.0;
                    double y2 = y1;
                    WgCadUtil.DivideBoundary(cad2d, id_e, ndiv, x1, y1, x2, y2);
                }

                // ロッドを追加
                if (isRodCell)
                {
                    double x0 = periodicDistanceX * 0.5;
                    double y0 = periodicDistanceY * 0.5;
                    uint lId = WgCadUtil.AddRod(cad2d, baseLoopId, x0, y0, rodRadius, rodCircleDiv, rodRadiusDiv);
                    rodLoopIds.Add(lId);
                }
                //------------------------------------------------------------------
                // メッシュ作成
                //------------------------------------------------------------------
                // メッシュを作成し、ワールド座標系にセットする
                World.Clear();
                using (CMesher2D mesher2d = new CMesher2D(cad2d, meshL))
                {
                    baseId = World.AddMesh(mesher2d);
                    conv = World.GetIDConverter(baseId);
                }
            }
            // 界の値を扱うバッファ？を生成する。
            // フィールド値IDが返却される。
            //    要素の次元: 2次元 界: 複素数スカラー 微分タイプ: 値 要素セグメント: 角節点
            FieldValId = World.MakeField_FieldElemDim(baseId, 2,
                FIELD_TYPE.ZSCALAR, FIELD_DERIVATION_TYPE.VALUE, ELSEG_TYPE.CORNER);

            // 領域
            //   ワールド座標系のループIDを取得
            //   媒質をループ単位で指定する
            FieldLoopId = 0;
            RodLoopId = 0;
            const int mediaIndexCladding = 0;
            const int mediaIndexCore = 1;
            {
                // 領域 + ロッド
                uint[] loopId_cad_list = new uint[1 + rodLoopIds.Count];
                int[] mediaIndex_list = new int[loopId_cad_list.Length];

                // 領域
                loopId_cad_list[0] = baseLoopId;
                mediaIndex_list[0] = mediaIndexCladding;

                // ロッド
                int offset = 1;
                rodLoopIds.ToArray().CopyTo(loopId_cad_list, offset);
                for (int i = offset; i < mediaIndex_list.Length; i++)
                {
                    mediaIndex_list[i] = mediaIndexCore;
                }
                // 要素アレイのリスト
                IList<uint> aEA = new List<uint>();
                for (int i = 0; i < loopId_cad_list.Length; i++)
                {
                    uint loopId_cad = loopId_cad_list[i];
                    int mediaIndex = mediaIndex_list[i];
                    uint lId1 = conv.GetIdEA_fromCad(loopId_cad, CAD_ELEM_TYPE.LOOP);
                    aEA.Add(lId1);
                    System.Diagnostics.Debug.Assert(lId1 != 0);
                    if (mediaIndex == mediaIndexCore)
                    {
                        System.Diagnostics.Debug.Assert(RodLoopId == 0); // 1セル内にロッドは１つだけなので
                        RodLoopId = lId1;
                    }
                }
                //System.Diagnostics.Debug.WriteLine("lId:" + lId1);
                FieldLoopId = World.GetPartialField(FieldValId, aEA);
                System.Diagnostics.Debug.Assert(FieldLoopId != 0);
            }

            // 境界条件を設定する
            // 固定境界条件（強制境界)
            FieldForceBcId = 0; // なし

            // 開口条件
            int boundaryCnt = 4;
            for (int boundaryIndex = 0; boundaryIndex < boundaryCnt; boundaryIndex++)
            {
                uint work_fieldPortBcId = 0;
                uint[] eId_cad_list = new uint[ndiv];
                int[] mediaIndex_list = new int[eId_cad_list.Length];
                if (boundaryIndex == 0)
                {
                    eId_cad_list[0] = 1;
                }
                else if (boundaryIndex == 1)
                {
                    eId_cad_list[0] = 3;
                }
                else if (boundaryIndex == 2)
                {
                    eId_cad_list[0] = 2;
                }
                else if (boundaryIndex == 3)
                {
                    eId_cad_list[0] = 4;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                mediaIndex_list[0] = mediaIndexCladding;
                for (int i = 1; i <= ndiv - 1; i++)
                {
                    if (boundaryIndex == 0)
                    {
                        eId_cad_list[i] = (uint)(4 + (ndiv - 1) - (i - 1));
                    }
                    else if (boundaryIndex == 1)
                    {
                        eId_cad_list[i] = (uint)(4 + (ndiv - 1) * 2 - (i - 1));
                    }
                    else if (boundaryIndex == 2)
                    {
                        eId_cad_list[i] = (uint)(4 + (ndiv - 1) * 3 - (i - 1));
                    }
                    else if (boundaryIndex == 3)
                    {
                        eId_cad_list[i] = (uint)(4 + (ndiv - 1) * 4 - (i - 1));
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }
                    mediaIndex_list[i] = mediaIndexCladding;
                }
                // 要素アレイのリスト
                IList<uint> aEA = new List<uint>();
                for (int i = 0; i < eId_cad_list.Length; i++)
                {
                    uint eId_cad = eId_cad_list[i];
                    int mediaIndex = mediaIndex_list[i];

                    uint eId = conv.GetIdEA_fromCad(eId_cad, CAD_ELEM_TYPE.EDGE);
                    System.Diagnostics.Debug.Assert(eId != 0);
                    aEA.Add(eId);
                }
                work_fieldPortBcId = World.GetPartialField(FieldValId, aEA);
                System.Diagnostics.Debug.Assert(work_fieldPortBcId != 0);
                FieldPortBcIds.Add(work_fieldPortBcId);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////
        // 2次三角形要素
        //////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///  欠陥部セルのメッシュ取得
        /// </summary>
        /// <param name="ndivForOneLattice">格子1辺の分割数</param>
        /// <param name="ptsInCell">セル内節点の座標リスト</param>
        /// <param name="nodeBInCell">セル境界節点の節点番号リスト</param>
        /// <param name="elemNodeInCell">要素内節点の節点番号リスト</param>
        public static void GetCellMesh_Defect_TriSecondOrder(
            int ndivForOneLattice,
            out double[,] ptsInCell,
            out uint[,] nodeBInCell,
            out uint[,] elemNodeInCell
            )
        {
            ptsInCell = null;
            nodeBInCell = null;
            elemNodeInCell = null;

            // 先ず、1次三角形要素のメッシュを作成
            double[,] ptsInCell_FirstOrder = null;
            uint[,] nodeBInCell_FirstOrder = null;
            uint[,] elemNodeInCell_FirstOrder = null;
            GetCellMesh_Defect_TriFirstOrder(
                ndivForOneLattice,
                out ptsInCell_FirstOrder,
                out nodeBInCell_FirstOrder,
                out elemNodeInCell_FirstOrder
                );

            // 2次三角形要素の節点を作成する
            getNodeInCell_Tri_SecondOrder(
                ptsInCell_FirstOrder,
                nodeBInCell_FirstOrder,
                elemNodeInCell_FirstOrder,
                out ptsInCell,
                out nodeBInCell,
                out elemNodeInCell
                );
        }

        /// <summary>
        /// ロッド部セルメッシュの取得
        /// </summary>
        /// <param name="ndivForOneLattice">格子1辺の分割数</param>
        /// <param name="rodRadiusRatio">ロッドの半径割合</param>
        /// <param name="rodCircleDiv">ロッドの円周方向分割数</param>
        /// <param name="rodRadiusDiv">ロッドの半径方向分割数(1でもメッシュサイズが小さければ複数に分割される)</param>
        /// <param name="ptsInCell">セル内節点の座標リスト</param>
        /// <param name="nodeBInCell">セル境界節点の節点番号リスト</param>
        /// <param name="elemLoopIdInCell">セル内要素のワールド座標系ループIDリスト</param>
        /// <param name="RodLoopId">ロッドのワールド座標系ループID</param>
        /// <param name="elemNodeInCell">要素内節点の節点番号リスト</param>
        public static void GetCellMesh_Rod_TriSecondOrder(
            int ndivForOneLattice,
            double rodRadiusRatio,
            int rodCircleDiv,
            int rodRadiusDiv,
            out double[,] ptsInCell,
            out uint[,] nodeBInCell,
            out uint[] elemLoopIdInCell,
            out uint RodLoopId,
            out uint[,] elemNodeInCell
            )
        {
            ptsInCell = null;
            nodeBInCell = null;
            elemLoopIdInCell = null;
            RodLoopId = 0;
            elemNodeInCell = null;
            
            // 先ず、1次三角形要素のメッシュを作成
            double[,] ptsInCell_FirstOrder = null;
            uint[,] nodeBInCell_FirstOrder = null;
            uint[,] elemNodeInCell_FirstOrder = null;
            GetCellMesh_Rod_TriFirstOrder(
                ndivForOneLattice,
                rodRadiusRatio,
                rodCircleDiv,
                rodRadiusDiv,
                out ptsInCell_FirstOrder,
                out nodeBInCell_FirstOrder,
                out elemLoopIdInCell,
                out RodLoopId,
                out elemNodeInCell_FirstOrder
                );

            // 2次三角形要素の節点を作成する
            getNodeInCell_Tri_SecondOrder(
                ptsInCell_FirstOrder,
                nodeBInCell_FirstOrder,
                elemNodeInCell_FirstOrder,
                out ptsInCell,
                out nodeBInCell,
                out elemNodeInCell
                );
        }

        /// <summary>
        /// ２次三角形要素の節点を作成する
        /// </summary>
        /// <param name="ptsInCell_FirstOrder"></param>
        /// <param name="nodeBInCell_FirstOrder"></param>
        /// <param name="elemNodeInCell_FirstOrder"></param>
        /// <param name="ptsInCell"></param>
        /// <param name="nodeBInCell"></param>
        /// <param name="elemNodeInCell"></param>
        private static void getNodeInCell_Tri_SecondOrder(
            double[,] ptsInCell_FirstOrder,
            uint[,] nodeBInCell_FirstOrder,
            uint[,] elemNodeInCell_FirstOrder,
            out double[,] ptsInCell,
            out uint[,] nodeBInCell,
            out uint[,] elemNodeInCell
            )
        {
            // 1次三角形要素の節点数
            int nodeCntInCell_FirstOrder = ptsInCell_FirstOrder.GetLength(0);
            // 要素数
            int elemCntInCell = elemNodeInCell_FirstOrder.GetLength(0);
            // 2次三角形要素の要素内節点の節点番号
            elemNodeInCell = new uint[elemCntInCell, Constants.TriNodeCnt_SecondOrder];
            // 節点座標リスト
            ptsInCell = null;

            // 座標
            IList<double[]> ptsList = new List<double[]>();
            for (int ino = 0; ino < nodeCntInCell_FirstOrder; ino++)
            {
                double[] work_pp = new double[2] { ptsInCell_FirstOrder[ino, 0], ptsInCell_FirstOrder[ino, 1] };
                ptsList.Add(work_pp);
            }

            // 辺の頂点の節点番号→中点の節点番号マップ
            Dictionary<string, uint> edgeToNoH = new Dictionary<string, uint>();

            uint nodeCounter = (uint)(nodeCntInCell_FirstOrder - 1);
            for (int ie = 0; ie < elemCntInCell; ie++)
            {
                // 要素の節点番号、節点座標
                uint[] nodeNumbers = new uint[Constants.TriNodeCnt_SecondOrder];
                double[,] ppe = new double[Constants.TriNodeCnt_SecondOrder, 2];
                for (int ino = 0; ino < Constants.TriNodeCnt_FirstOrder; ino++)
                {
                    // １次三角形要素の節点番号
                    uint noCell_FirstOrder = elemNodeInCell_FirstOrder[ie, ino];
                    nodeNumbers[ino] = noCell_FirstOrder;
                    for (int idim = 0; idim < 2; idim++)
                    {
                        ppe[ino, idim] = ptsInCell_FirstOrder[noCell_FirstOrder, idim];
                    }
                }
                // 2次要素の節点
                int[,] edgeVertex = {
                                        {0, 1},
                                        {1, 2},
                                        {2, 0}
                                    };
                for (int ino = Constants.TriNodeCnt_FirstOrder; ino < Constants.TriNodeCnt_SecondOrder; ino++)
                {
                    int v1 = edgeVertex[ino - Constants.TriNodeCnt_FirstOrder, 0];
                    int v2 = edgeVertex[ino - Constants.TriNodeCnt_FirstOrder, 1];
                    uint no1 = nodeNumbers[v1];
                    uint no2 = nodeNumbers[v2];
                    uint no1key = Math.Min(no1, no2);
                    uint no2key = Math.Max(no1, no2);
                    string edgeKey = string.Format("{0}_{1}", no1key, no2key);
                    uint midNodeNumber = 0;
                    bool isAdded = false;
                    if (edgeToNoH.ContainsKey(edgeKey))
                    {
                        midNodeNumber = edgeToNoH[edgeKey];
                    }
                    else
                    {
                        isAdded = true;
                        midNodeNumber = ++nodeCounter;
                        edgeToNoH[edgeKey] = midNodeNumber;
                    }
                    nodeNumbers[ino] = midNodeNumber;
                    for (int idim = 0; idim < 2; idim++)
                    {
                        if (isAdded)
                        {
                            ppe[ino, idim] = (ppe[v1, idim] + ppe[v2, idim]) * 0.5;
                        }
                        else
                        {
                            ppe[ino, idim] = ptsList[(int)midNodeNumber - 1][idim];
                        }
                    }
                    if (isAdded)
                    {
                        ptsList.Add(new double[] { ppe[ino, 0], ppe[ino, 1] });
                        System.Diagnostics.Debug.Assert((ptsList.Count - 1) == midNodeNumber);
                    }
                }

                // ２次三角形要素の要素内節点番号リストの格納
                for (int ino = 0; ino < Constants.TriNodeCnt_SecondOrder; ino++)
                {
                    elemNodeInCell[ie, ino] = nodeNumbers[ino];
                }
            }

            // ２次三角形要素メッシュの節点座標リストの格納
            int nodeCntInCell_SecondOrder = ptsList.Count;
            ptsInCell = new double[nodeCntInCell_SecondOrder, 2];
            for (int ino = 0; ino < nodeCntInCell_SecondOrder; ino++)
            {
                for (int idim = 0; idim < 2; idim++)
                {
                    ptsInCell[ino, idim] = ptsList[ino][idim];
                }
            }

            int boundaryCnt = nodeBInCell_FirstOrder.GetLength(0);
            int nodeCntB_FirstOrder = nodeBInCell_FirstOrder.GetLength(1);
            int nodeCntB = (nodeCntB_FirstOrder - 1) * 2 + 1;
            nodeBInCell = new uint[boundaryCnt, nodeCntB];
            for (int boundaryIndex = 0; boundaryIndex < boundaryCnt; boundaryIndex++)
            {
                for (int ino = 0; ino < nodeCntB_FirstOrder; ino++)
                {
                    nodeBInCell[boundaryIndex, ino * 2] = nodeBInCell_FirstOrder[boundaryIndex, ino];
                }
                for (int ino = 0; ino < (nodeCntB_FirstOrder - 1); ino++)
                {
                    uint no1 = nodeBInCell[boundaryIndex, ino * 2];
                    uint no2 = nodeBInCell[boundaryIndex, ino * 2 + 2];
                    uint no1key = Math.Min(no1, no2);
                    uint no2key = Math.Max(no1, no2);
                    string edgeKey = string.Format("{0}_{1}", no1key, no2key);
                    System.Diagnostics.Debug.Assert(edgeToNoH.ContainsKey(edgeKey));
                    uint midNodeNumber = edgeToNoH[edgeKey];
                    nodeBInCell[boundaryIndex, ino * 2 + 1] = midNodeNumber;
                }
            }
        }
    
    }
}
