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
    //  図面作成ユーティリティ
    //
    //      Copyright (C) 2012-2013 ryujimiya
    //
    //      e-mail: ryujimiya@mail.goo.ne.jp
    ///////////////////////////////////////////////////////////////
    /// <summary>
    /// 図面作成ユーティリティ
    /// </summary>
    class WgCadUtil
    {
        /////////////////////////////////////////////////
        // 定数
        /////////////////////////////////////////////////
        private const double pi = WgUtil.pi;
        private const double c0 = WgUtil.c0;
        private const double mu0 = WgUtil.mu0;
        private const double eps0 = WgUtil.eps0;

        ////////////////////////////////////////////////////////////////////////
        // 型
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// 境界を分割する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="id_e"></param>
        /// <param name="ndiv"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        public static void DivideBoundary(CCadObj2D cad2d, uint id_e, int ndiv, double x1, double y1, double x2, double y2)
        {
            double signedWidthX = x2 - x1;
            double signedWidthY = y2 - y1;
            for (int i = ndiv - 1; i >= 1; i--)
            {
                double x = x1 + i * signedWidthX / ndiv;
                double y = y1 + i * signedWidthY / ndiv;
                CCadObj2D.CResAddVertex resAddVertex = cad2d.AddVertex(CAD_ELEM_TYPE.EDGE, id_e, new CVector2D(x, y));
                uint id_v_add = resAddVertex.id_v_add;
                uint id_e_add = resAddVertex.id_e_add;
                System.Diagnostics.Debug.Assert(id_v_add != 0);
            }
        }

        /// <summary>
        /// ロッドを追加する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="baseLoopId"></param>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="rodRadius"></param>
        /// <param name="rodCircleDiv"></param>
        /// <param name="rodRadiusDiv"></param>
        /// <returns></returns>
        public static uint AddRod(CCadObj2D cad2d, uint baseLoopId, double x0, double y0, double rodRadius, int rodCircleDiv, int rodRadiusDiv)
        {
            List<CVector2D> pts = new List<CVector2D>();
            {
                // メッシュ形状を整えるためにロッドの中心に頂点を追加
                uint id_v_center = cad2d.AddVertex(CAD_ELEM_TYPE.LOOP, baseLoopId, new CVector2D(x0, y0)).id_v_add;
                System.Diagnostics.Debug.Assert(id_v_center != 0);
            }
            // ロッドの分割数調整
            for (int k = 1; k < rodRadiusDiv; k++)
            {
                for (int itheta = 0; itheta < rodCircleDiv; itheta++)
                {
                    double theta = itheta * 2.0 * pi / rodCircleDiv;
                    double x = x0 + (k * rodRadius / rodRadiusDiv) * Math.Cos(theta);
                    double y = y0 + (k * rodRadius / rodRadiusDiv) * Math.Sin(theta);
                    uint id_v_add = cad2d.AddVertex(CAD_ELEM_TYPE.LOOP, baseLoopId, new CVector2D(x, y)).id_v_add;
                    System.Diagnostics.Debug.Assert(id_v_add != 0);
                }
            }
            // ロッド
            for (int itheta = 0; itheta < rodCircleDiv; itheta++)
            {
                double theta = itheta * 2.0 * pi / rodCircleDiv;
                double x = x0 + rodRadius * Math.Cos(theta);
                double y = y0 + rodRadius * Math.Sin(theta);
                pts.Add(new CVector2D(x, y));
            }
            uint lId = cad2d.AddPolygon(pts, baseLoopId).id_l_add;
            System.Diagnostics.Debug.Assert(lId != 0);
            return lId;
        }

        /// <summary>
        /// ロッド(半分)を追加する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="baseLoopId"></param>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="rodRadius"></param>
        /// <param name="rodCircleDiv"></param>
        /// <param name="rodRadiusDiv"></param>
        /// <returns></returns>
        public static uint AddHalfRod(CCadObj2D cad2d, uint baseLoopId, double x0, double y0, double rodRadius, int rodCircleDiv, int rodRadiusDiv,
            double startAngle, double additionalAngle = 0.0, bool isReverseAddVertex = false, uint id_v_st = uint.MaxValue, uint id_v_ed = uint.MaxValue)
        {
            System.Diagnostics.Debug.Assert(additionalAngle < 360.0 / rodCircleDiv);
            List<CVector2D> pts = new List<CVector2D>();
            System.Diagnostics.Debug.Assert((startAngle == 0.0) || (startAngle == 90.0) || (startAngle == 180.0) || (startAngle == 270.0));
            if (Math.Abs(additionalAngle) >= MyUtilLib.Matrix.Constants.PrecisionLowerLimit)
            {
                // メッシュ形状を整えるためにロッドの中心に頂点を追加
                uint id_v_center = cad2d.AddVertex(CAD_ELEM_TYPE.LOOP, baseLoopId, new CVector2D(x0, y0)).id_v_add;
                System.Diagnostics.Debug.Assert(id_v_center != 0);
            }
            // ロッドの分割数調整
            for (int k = 1; k < rodRadiusDiv; k++)
            {
                for (int itheta = 0; itheta <= (rodCircleDiv / 2); itheta++)
                {
                    if (Math.Abs(additionalAngle) < MyUtilLib.Matrix.Constants.PrecisionLowerLimit && (itheta == 0 || itheta == (rodCircleDiv / 2)))
                    {
                        continue;
                    }
                    double theta = 0;
                    if (isReverseAddVertex)
                    {
                        theta = startAngle * pi / 180.0 - itheta * 2.0 * pi / rodCircleDiv;
                    }
                    else
                    {
                        theta = startAngle * pi / 180.0 + itheta * 2.0 * pi / rodCircleDiv;
                    }
                    double x = x0 + (k * rodRadius / rodRadiusDiv) * Math.Cos(theta);
                    double y = y0 + (k * rodRadius / rodRadiusDiv) * Math.Sin(theta);
                    uint id_v_add = cad2d.AddVertex(CAD_ELEM_TYPE.LOOP, baseLoopId, new CVector2D(x, y)).id_v_add;
                    System.Diagnostics.Debug.Assert(id_v_add != 0);
                }
            }
            // ロッドの分割数調整: ロッド1/4円から超えた部分
            if (Math.Abs(additionalAngle) >= MyUtilLib.Matrix.Constants.PrecisionLowerLimit)
            {
                double theta = 0;
                if (isReverseAddVertex)
                {
                    theta = (startAngle + additionalAngle) * pi / 180.0;
                }
                else
                {
                    theta = (startAngle - additionalAngle) * pi / 180.0;
                }
                for (int k = 1; k < rodRadiusDiv; k++)
                {
                    double x = x0 + (k * rodRadius / rodRadiusDiv) * Math.Cos(theta);
                    double y = y0 + (k * rodRadius / rodRadiusDiv) * Math.Sin(theta);
                    uint id_v_add = cad2d.AddVertex(CAD_ELEM_TYPE.LOOP, baseLoopId, new CVector2D(x, y)).id_v_add;
                    System.Diagnostics.Debug.Assert(id_v_add != 0);
                }
            }
            // ロッドの分割数調整: ロッド1/4円から超えた部分
            if (Math.Abs(additionalAngle) >= MyUtilLib.Matrix.Constants.PrecisionLowerLimit)
            {
                double theta = 0;
                if (isReverseAddVertex)
                {
                    theta = (startAngle - 180.0 - additionalAngle) * pi / 180.0;
                }
                else
                {
                    theta = (startAngle + 180.0 + additionalAngle) * pi / 180.0;
                }
                for (int k = 1; k < rodRadiusDiv; k++)
                {
                    double x = x0 + (k * rodRadius / rodRadiusDiv) * Math.Cos(theta);
                    double y = y0 + (k * rodRadius / rodRadiusDiv) * Math.Sin(theta);
                    uint id_v_add = cad2d.AddVertex(CAD_ELEM_TYPE.LOOP, baseLoopId, new CVector2D(x, y)).id_v_add;
                    System.Diagnostics.Debug.Assert(id_v_add != 0);
                }
            }

            uint retLoopId = 0;
            if (id_v_st != uint.MaxValue && id_v_ed != uint.MaxValue)
            {
                uint prev_id_v = id_v_st;

                // ロッド半円から超えた部分
                if (Math.Abs(additionalAngle) >= MyUtilLib.Matrix.Constants.PrecisionLowerLimit)
                {
                    double theta = 0;
                    if (isReverseAddVertex)
                    {
                        theta = (startAngle + additionalAngle) * pi / 180.0;
                    }
                    else
                    {
                        theta = (startAngle - additionalAngle) * pi / 180.0;
                    }
                    double x = x0 + rodRadius * Math.Cos(theta);
                    double y = y0 + rodRadius * Math.Sin(theta);
                    uint id_v_add = cad2d.AddVertex(CAD_ELEM_TYPE.LOOP, baseLoopId, new CVector2D(x, y)).id_v_add;
                    System.Diagnostics.Debug.Assert(id_v_add != 0);
                    CBRepSurface.CResConnectVertex resConnectVertex = cad2d.ConnectVertex_Line(prev_id_v, id_v_add);
                    uint id_e_add = resConnectVertex.id_e_add;
                    System.Diagnostics.Debug.Assert(id_e_add != 0);
                    prev_id_v = id_v_add;
                }

                // ロッド半円
                for (int itheta = 0; itheta <= (rodCircleDiv / 2); itheta++)
                {
                    double theta = 0;
                    if (isReverseAddVertex)
                    {
                        theta = startAngle * pi / 180.0 - itheta * 2.0 * pi / rodCircleDiv;
                    }
                    else
                    {
                        theta = startAngle * pi / 180.0 + itheta * 2.0 * pi / rodCircleDiv;
                    }
                    double x = x0 + rodRadius * Math.Cos(theta);
                    double y = y0 + rodRadius * Math.Sin(theta);
                    uint id_v_add = cad2d.AddVertex(CAD_ELEM_TYPE.LOOP, baseLoopId, new CVector2D(x, y)).id_v_add;
                    System.Diagnostics.Debug.Assert(id_v_add != 0);
                    CBRepSurface.CResConnectVertex resConnectVertex = cad2d.ConnectVertex_Line(prev_id_v, id_v_add);
                    uint id_e_add = resConnectVertex.id_e_add;
                    System.Diagnostics.Debug.Assert(id_e_add != 0);
                    prev_id_v = id_v_add;
                }
                // ロッド半円から超えた部分
                if (Math.Abs(additionalAngle) >= MyUtilLib.Matrix.Constants.PrecisionLowerLimit)
                {
                    double theta = 0;
                    if (isReverseAddVertex)
                    {
                        theta = (startAngle - 180.0 - additionalAngle) * pi / 180.0;
                    }
                    else
                    {
                        theta = (startAngle + 180.0 + additionalAngle) * pi / 180.0;
                    }
                    double x = x0 + rodRadius * Math.Cos(theta);
                    double y = y0 + rodRadius * Math.Sin(theta);
                    uint id_v_add = cad2d.AddVertex(CAD_ELEM_TYPE.LOOP, baseLoopId, new CVector2D(x, y)).id_v_add;
                    System.Diagnostics.Debug.Assert(id_v_add != 0);
                    CBRepSurface.CResConnectVertex resConnectVertex = cad2d.ConnectVertex_Line(prev_id_v, id_v_add);
                    uint id_e_add = resConnectVertex.id_e_add;
                    System.Diagnostics.Debug.Assert(id_e_add != 0);
                    prev_id_v = id_v_add; //!!!!!!!!!!!!!!!
                }
                uint last_id_v = id_v_ed;
                {
                    CBRepSurface.CResConnectVertex resConnectVertex = cad2d.ConnectVertex_Line(prev_id_v, last_id_v);
                    uint id_e_add = resConnectVertex.id_e_add;
                    uint lId = resConnectVertex.id_l_add;
                    System.Diagnostics.Debug.Assert(id_e_add != 0);
                    System.Diagnostics.Debug.Assert(lId != 0);
                    retLoopId = lId;
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(isReverseAddVertex == false); // 逆順未対応
                // ロッド半円から超えた部分
                if (Math.Abs(additionalAngle) >= MyUtilLib.Matrix.Constants.PrecisionLowerLimit)
                {
                    double theta = (startAngle - additionalAngle) * pi / 180.0;
                    double x = x0 + rodRadius * Math.Cos(theta);
                    double y = y0 + rodRadius * Math.Sin(theta);
                    pts.Add(new CVector2D(x, y));
                }
                // ロッド半円
                for (int itheta = 0; itheta <= (rodCircleDiv / 2); itheta++)
                {
                    double theta = startAngle * pi / 180.0 + itheta * 2.0 * pi / rodCircleDiv;
                    double x = x0 + rodRadius * Math.Cos(theta);
                    double y = y0 + rodRadius * Math.Sin(theta);
                    pts.Add(new CVector2D(x, y));
                }
                // ロッド半円から超えた部分
                if (Math.Abs(additionalAngle) >= MyUtilLib.Matrix.Constants.PrecisionLowerLimit)
                {
                    double theta = (startAngle + 180.0 + additionalAngle) * pi / 180.0;
                    double x = x0 + rodRadius * Math.Cos(theta);
                    double y = y0 + rodRadius * Math.Sin(theta);
                    pts.Add(new CVector2D(x, y));
                }
                uint lId = cad2d.AddPolygon(pts, baseLoopId).id_l_add;
                retLoopId = lId;
            }
            return retLoopId;
        }

        /// <summary>
        /// 左のロッド
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="baseLoopId"></param>
        /// <param name="id_v0"></param>
        /// <param name="id_v1"></param>
        /// <param name="id_v2"></param>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="rodRadius"></param>
        /// <param name="rodCircleDiv"></param>
        /// <param name="rodRadiusDiv"></param>
        /// <returns></returns>
        public static uint AddLeftRod(CCadObj2D cad2d, uint baseLoopId,
            uint id_v0, uint id_v1, uint id_v2,
            double x0, double y0, double rodRadius, int rodCircleDiv, int rodRadiusDiv)
        {
            return AddExactlyHalfRod(cad2d, baseLoopId,
                id_v0, id_v1, id_v2,
                x0, y0, rodRadius, rodCircleDiv, rodRadiusDiv,
                90.0, true);
        }

        /// <summary>
        /// 半円（余剰角度なし)ロッドの追加
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="baseLoopId"></param>
        /// <param name="id_v0"></param>
        /// <param name="id_v1"></param>
        /// <param name="id_v2"></param>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="rodRadius"></param>
        /// <param name="rodCircleDiv"></param>
        /// <param name="rodRadiusDiv"></param>
        /// <param name="startAngle"></param>
        /// <param name="isReverseAddVertex"></param>
        /// <returns></returns>
        public static uint AddExactlyHalfRod(CCadObj2D cad2d, uint baseLoopId,
            uint id_v0, uint id_v1, uint id_v2,
            double x0, double y0, double rodRadius, int rodCircleDiv, int rodRadiusDiv,
            double startAngle, bool isReverseAddVertex)
        {
            uint retLoopId = 0;

            CVector2D pt_center = cad2d.GetVertexCoord(id_v1);
            System.Diagnostics.Debug.Assert(Math.Abs(x0 - pt_center.x) < MyUtilLib.Matrix.Constants.PrecisionLowerLimit);
            System.Diagnostics.Debug.Assert(Math.Abs(y0 - pt_center.y) < MyUtilLib.Matrix.Constants.PrecisionLowerLimit);
            // check
            //CVector2D pt0 = cad2d.GetVertexCoord(id_v0);
            //double x_pt0 = pt0.x;
            //double y_pt0 = pt0.y;
            //CVector2D pt2 = cad2d.GetVertexCoord(id_v2);
            //double x_pt2 = pt2.x;
            //double y_pt2 = pt2.y;

            // ロッドの分割数調整
            for (int k = 1; k < rodRadiusDiv; k++)
            {
                for (int itheta = 1; itheta < (rodCircleDiv / 2); itheta++)
                {
                    double theta = 0.0;
                    if (isReverseAddVertex)
                    {
                        theta = startAngle * pi / 180.0 - itheta * 2.0 * pi / rodCircleDiv;
                    }
                    else
                    {
                        theta = startAngle * pi / 180.0 + itheta * 2.0 * pi / rodCircleDiv;
                    }
                    double x = x0 + (k * rodRadius / rodRadiusDiv) * Math.Cos(theta);
                    double y = y0 + (k * rodRadius / rodRadiusDiv) * Math.Sin(theta);
                    uint id_v_add = cad2d.AddVertex(CAD_ELEM_TYPE.LOOP, baseLoopId, new CVector2D(x, y)).id_v_add;
                    System.Diagnostics.Debug.Assert(id_v_add != 0);
                }
            }
            // ロッド
            uint prev_id_v = id_v2;
            for (int itheta = 1; itheta < (rodCircleDiv / 2); itheta++)
            {
                double theta = 0.0;
                if (isReverseAddVertex)
                {
                    theta = startAngle * pi / 180.0 - itheta * 2.0 * pi / rodCircleDiv;
                }
                else
                {
                    theta = startAngle * pi / 180.0 + itheta * 2.0 * pi / rodCircleDiv;
                }
                double x = x0 + rodRadius * Math.Cos(theta);
                double y = y0 + rodRadius * Math.Sin(theta);
                uint id_v_add = cad2d.AddVertex(CAD_ELEM_TYPE.LOOP, baseLoopId, new CVector2D(x, y)).id_v_add;
                System.Diagnostics.Debug.Assert(id_v_add != 0);
                CBRepSurface.CResConnectVertex resConnectVertex = cad2d.ConnectVertex_Line(prev_id_v, id_v_add);
                uint id_e_add = resConnectVertex.id_e_add;
                System.Diagnostics.Debug.Assert(id_e_add != 0);
                prev_id_v = id_v_add;
            }
            uint last_id_v = id_v0;
            {
                CBRepSurface.CResConnectVertex resConnectVertex = cad2d.ConnectVertex_Line(prev_id_v, last_id_v);
                uint id_e_add = resConnectVertex.id_e_add;
                uint lId = resConnectVertex.id_l_add;
                System.Diagnostics.Debug.Assert(id_e_add != 0);
                System.Diagnostics.Debug.Assert(lId != 0);
                retLoopId = lId;
            }
            return retLoopId;
        }

        /// <summary>
        /// 右のロッド
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="baseLoopId"></param>
        /// <param name="id_v0"></param>
        /// <param name="id_v1"></param>
        /// <param name="id_v2"></param>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="rodRadius"></param>
        /// <param name="rodCircleDiv"></param>
        /// <param name="rodRadiusDiv"></param>
        /// <returns></returns>
        public static uint AddRightRod(CCadObj2D cad2d, uint baseLoopId,
            uint id_v0, uint id_v1, uint id_v2,
            double x0, double y0, double rodRadius, int rodCircleDiv, int rodRadiusDiv)
        {
            return AddExactlyHalfRod(cad2d, baseLoopId,
                id_v0, id_v1, id_v2,
                x0, y0, rodRadius, rodCircleDiv, rodRadiusDiv,
                270.0, true);
        }

        /// <summary>
        /// 上のロッド
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="baseLoopId"></param>
        /// <param name="id_v0"></param>
        /// <param name="id_v1"></param>
        /// <param name="id_v2"></param>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="rodRadius"></param>
        /// <param name="rodCircleDiv"></param>
        /// <param name="rodRadiusDiv"></param>
        /// <returns></returns>
        public static uint AddTopRod(CCadObj2D cad2d, uint baseLoopId,
            uint id_v0, uint id_v1, uint id_v2,
            double x0, double y0, double rodRadius, int rodCircleDiv, int rodRadiusDiv)
        {
            return AddExactlyHalfRod(cad2d, baseLoopId,
                id_v0, id_v1, id_v2,
                x0, y0, rodRadius, rodCircleDiv, rodRadiusDiv,
                0.0, true);
        }

        /// <summary>
        /// 下のロッド
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="baseLoopId"></param>
        /// <param name="id_v0"></param>
        /// <param name="id_v1"></param>
        /// <param name="id_v2"></param>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="rodRadius"></param>
        /// <param name="rodCircleDiv"></param>
        /// <param name="rodRadiusDiv"></param>
        /// <returns></returns>
        public static uint AddBottomRod(CCadObj2D cad2d, uint baseLoopId,
            uint id_v0, uint id_v1, uint id_v2,
            double x0, double y0, double rodRadius, int rodCircleDiv, int rodRadiusDiv)
        {
            // 注意：id_v0とid_v2が逆になる
            return AddExactlyHalfRod(cad2d, baseLoopId,
                id_v2, id_v1, id_v0,
                x0, y0, rodRadius, rodCircleDiv, rodRadiusDiv,
                180.0, true);
        }


        /// <summary>
        /// ロッド1/4円を追加する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="baseLoopId"></param>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="rodRadius"></param>
        /// <param name="rodCircleDiv"></param>
        /// <param name="rodRadiusDiv"></param>
        /// <returns></returns>
        public static uint AddQuarterRod(CCadObj2D cad2d, uint baseLoopId,
            uint id_v0, uint id_v1, uint id_v2,
            double x0, double y0, double rodRadius, int rodCircleDiv, int rodRadiusDiv,
            double startAngle, double endAngle, bool isReverseAddVertex = false)
        {
            CVector2D pt_center = cad2d.GetVertexCoord(id_v1);
            System.Diagnostics.Debug.Assert(Math.Abs(x0 - pt_center.x) < MyUtilLib.Matrix.Constants.PrecisionLowerLimit);
            System.Diagnostics.Debug.Assert(Math.Abs(y0 - pt_center.y) < MyUtilLib.Matrix.Constants.PrecisionLowerLimit);
            List<CVector2D> pts = new List<CVector2D>();

            // ロッドの分割数調整
            for (int k = 1; k < rodRadiusDiv; k++)
            {
                for (int itheta = 0; itheta <= rodCircleDiv; itheta++)
                {
                    double workAngle = 0.0;
                    if (isReverseAddVertex)
                    {
                        workAngle = 360.0 - itheta * 360.0 / rodCircleDiv;
                        if (workAngle <= endAngle || workAngle >= startAngle)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        workAngle = itheta * 360.0 / rodCircleDiv;
                        if (workAngle <= startAngle || workAngle >= endAngle)
                        {
                            continue;
                        }
                    }
                    double theta = workAngle * pi / 180.0;
                    double x = x0 + (k * rodRadius / rodRadiusDiv) * Math.Cos(theta);
                    double y = y0 + (k * rodRadius / rodRadiusDiv) * Math.Sin(theta);
                    uint id_v_add = cad2d.AddVertex(CAD_ELEM_TYPE.LOOP, baseLoopId, new CVector2D(x, y)).id_v_add;
                    System.Diagnostics.Debug.Assert(id_v_add != 0);
                }
            }
            uint retLoopId = 0;
            uint prev_id_v = id_v0;

            // ロッド1/4円
            for (int itheta = 0; itheta <= rodCircleDiv; itheta++)
            {
                double workAngle = 0.0;
                if (isReverseAddVertex)
                {
                    workAngle = 360.0 - itheta * 360.0 / rodCircleDiv;
                    if (workAngle <= endAngle || workAngle >= startAngle)
                    {
                        continue;
                    }
                }
                else
                {
                    workAngle = itheta * 360.0 / rodCircleDiv;
                    if (workAngle <= startAngle || workAngle >= endAngle)
                    {
                        continue;
                    }
                }

                double theta = workAngle * pi / 180.0;
                double x = x0 + rodRadius * Math.Cos(theta);
                double y = y0 + rodRadius * Math.Sin(theta);
                uint id_v_add = cad2d.AddVertex(CAD_ELEM_TYPE.LOOP, baseLoopId, new CVector2D(x, y)).id_v_add;
                System.Diagnostics.Debug.Assert(id_v_add != 0);
                CBRepSurface.CResConnectVertex resConnectVertex = cad2d.ConnectVertex_Line(prev_id_v, id_v_add);
                uint id_e_add = resConnectVertex.id_e_add;
                System.Diagnostics.Debug.Assert(id_e_add != 0);
                prev_id_v = id_v_add;
            }
            uint last_id_v = id_v2;
            {
                CBRepSurface.CResConnectVertex resConnectVertex = cad2d.ConnectVertex_Line(prev_id_v, last_id_v);
                uint id_e_add = resConnectVertex.id_e_add;
                uint lId = resConnectVertex.id_l_add;
                System.Diagnostics.Debug.Assert(id_e_add != 0);
                System.Diagnostics.Debug.Assert(lId != 0);
                retLoopId = lId;
            }

            return retLoopId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="baseLoopId"></param>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="rodRadius"></param>
        /// <param name="rodCircleDiv"></param>
        /// <param name="rodRadiusDiv"></param>
        /// <param name="id_v0"></param>
        /// <param name="id_v1"></param>
        /// <param name="id_v2"></param>
        /// <param name="startAngle"></param>
        /// <param name="isReverseAddVertex"></param>
        /// <returns></returns>
        public static uint AddExactlyQuarterRod(CCadObj2D cad2d, uint baseLoopId, double x0, double y0, double rodRadius, int rodCircleDiv, int rodRadiusDiv,
            uint id_v0, uint id_v1, uint id_v2, double startAngle, bool isReverseAddVertex)
        {
            CVector2D pt_center = cad2d.GetVertexCoord(id_v1);
            System.Diagnostics.Debug.Assert(Math.Abs(x0 - pt_center.x) < MyUtilLib.Matrix.Constants.PrecisionLowerLimit);
            System.Diagnostics.Debug.Assert(Math.Abs(y0 - pt_center.y) < MyUtilLib.Matrix.Constants.PrecisionLowerLimit);
            List<CVector2D> pts = new List<CVector2D>();
            System.Diagnostics.Debug.Assert((startAngle == 0.0) || (startAngle == 90.0) || (startAngle == 180.0) || (startAngle == 270.0));

            // ロッドの分割数調整
            for (int k = 1; k < rodRadiusDiv; k++)
            {
                for (int itheta = 1; itheta < (rodCircleDiv / 4); itheta++)
                {
                    double theta = 0;
                    if (isReverseAddVertex)
                    {
                        theta = startAngle * pi / 180.0 - itheta * 2.0 * pi / rodCircleDiv;
                    }
                    else
                    {
                        theta = startAngle * pi / 180.0 + itheta * 2.0 * pi / rodCircleDiv;
                    }
                    double x = x0 + (k * rodRadius / rodRadiusDiv) * Math.Cos(theta);
                    double y = y0 + (k * rodRadius / rodRadiusDiv) * Math.Sin(theta);
                    uint id_v_add = cad2d.AddVertex(CAD_ELEM_TYPE.LOOP, baseLoopId, new CVector2D(x, y)).id_v_add;
                    System.Diagnostics.Debug.Assert(id_v_add != 0);
                }
            }
            uint retLoopId = 0;
            uint prev_id_v = id_v0;

            // ロッド1/4円
            for (int itheta = 1; itheta < (rodCircleDiv / 4); itheta++)
            {
                double theta = 0;
                if (isReverseAddVertex)
                {
                    theta = startAngle * pi / 180.0 - itheta * 2.0 * pi / rodCircleDiv;
                }
                else
                {
                    theta = startAngle * pi / 180.0 + itheta * 2.0 * pi / rodCircleDiv;
                }
                double x = x0 + rodRadius * Math.Cos(theta);
                double y = y0 + rodRadius * Math.Sin(theta);
                uint id_v_add = cad2d.AddVertex(CAD_ELEM_TYPE.LOOP, baseLoopId, new CVector2D(x, y)).id_v_add;
                System.Diagnostics.Debug.Assert(id_v_add != 0);
                CBRepSurface.CResConnectVertex resConnectVertex = cad2d.ConnectVertex_Line(prev_id_v, id_v_add);
                uint id_e_add = resConnectVertex.id_e_add;
                System.Diagnostics.Debug.Assert(id_e_add != 0);
                prev_id_v = id_v_add;
            }
            uint last_id_v = id_v2;
            {
                CBRepSurface.CResConnectVertex resConnectVertex = cad2d.ConnectVertex_Line(prev_id_v, last_id_v);
                uint id_e_add = resConnectVertex.id_e_add;
                uint lId = resConnectVertex.id_l_add;
                System.Diagnostics.Debug.Assert(id_e_add != 0);
                System.Diagnostics.Debug.Assert(lId != 0);
                retLoopId = lId;
            }

            return retLoopId;
        }

    }
}
