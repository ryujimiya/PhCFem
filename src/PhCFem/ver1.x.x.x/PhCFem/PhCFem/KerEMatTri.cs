/*
参照元の著作権表示
*/
/*
DelFEM (Finite Element Analysis)
Copyright (C) 2009  Nobuyuki Umetani    n.umetani@gmail.com

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PhCFem
{
    /// <summary>
    /// 三角形要素カーネルマトリクス計算用ユーティリティ
    /// 　参照元：DelFEM (Nobuyuki Umetani) ker_emat_tri.h
    /// </summary>
    class KerEMatTri
    {
        /// <summary>
        /// 座標の次元
        /// </summary>
        public static readonly int VecDim = 2;
        /// <summary>
        /// 節点の数
        /// </summary>
        public static readonly int NodeCnt = 3;

        /// <summary>
        /// calculate Area of Triangle
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double TriArea(double[] p0, double[] p1, double[] p2)
        {
            return 0.5 * ( (p1[0] - p0[0])*(p2[1] - p0[1]) - (p2[0] - p0[0])*(p1[1] - p0[1]) );
        }        
        /// <summary>
        /// calculate AreaCoord of Triangle
        /// </summary>
        /// <param name="vc_p"></param>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="pb"></param>
        public static void TriAreaCoord(out double[] vc_p,
                                        double[] p0, double[] p1, double[] p2, double[] pb)
        {
            vc_p = new double[NodeCnt];
            vc_p[0] = TriArea(pb, p1, p2);
            vc_p[1] = TriArea(p0, pb, p2);
            vc_p[2] = TriArea(p0, p1, pb);

            double area = TriArea(p0, p1, p2);
            double inv_area = 1.0 / area;

            vc_p[0] = vc_p[0] * inv_area;
            vc_p[1] = vc_p[1] * inv_area;
            vc_p[2] = vc_p[2] * inv_area;

            System.Diagnostics.Debug.Assert(Math.Abs(vc_p[0] + vc_p[1] + vc_p[2] - 1.0) < 1.0e-15);
        }
        /// <summary>
        /// caluculate Derivative of Area Coord
        /// </summary>
        /// <param name="dldx"></param>
        /// <param name="const_term"></param>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        public static void TriDlDx(out double[,] dldx, out double[] const_term,
                                   double[] p0, double[] p1, double[] p2)
        {
            double area = TriArea(p0, p1, p2);
            double tmp1 = 0.5 / area;

            const_term = new double[NodeCnt];
            const_term[0] = tmp1 * (p1[0] * p2[1] - p2[0] * p1[1]);
            const_term[1] = tmp1 * (p2[0] * p0[1] - p0[0] * p2[1]);
            const_term[2] = tmp1 * (p0[0] * p1[1] - p1[0] * p0[1]);

            dldx = new double[NodeCnt, VecDim];
            dldx[0, 0] = tmp1 * (p1[1] - p2[1]);
            dldx[1, 0] = tmp1 * (p2[1] - p0[1]);
            dldx[2, 0] = tmp1 * (p0[1] - p1[1]);

            dldx[0, 1] = tmp1 * (p2[0] - p1[0]);
            dldx[1, 1] = tmp1 * (p0[0] - p2[0]);
            dldx[2, 1] = tmp1 * (p1[0] - p0[0]);
        }
        /// <summary>
        /// 積分点の数
        /// </summary>
        public static readonly int[] NIntTriGauss = new int[3]
        { 
            1, 3, 7
        };
        /// <summary>
        /// 積分点の位置(r1,r2)と重みの配列
        /// </summary>
        public static readonly double[,,] TriGauss = new double[3,7,3]
        {
            {
                { 0.3333333333, 0.3333333333, 1.0 },
                { 0.0, 0.0, 0.0 },
                { 0.0, 0.0, 0.0 },
                { 0.0, 0.0, 0.0 },
                { 0.0, 0.0, 0.0 },
                { 0.0, 0.0, 0.0 },
                { 0.0, 0.0, 0.0}/*無効データ*/
            },
            {
                { 0.1666666667, 0.1666666667, 0.3333333333 },
                { 0.6666666667, 0.1666666667, 0.3333333333 },
                { 0.1666666667, 0.6666666667, 0.3333333333 },
                { 0.0, 0.0, 0.0 },
                { 0.0, 0.0, 0.0 },
                { 0.0, 0.0, 0.0 },
                { 0.0, 0.0, 0.0}/*無効データ*/
            },
            {    
                { 0.1012865073, 0.1012865073, 0.1259391805 },
                { 0.7974269854, 0.1012865073, 0.1259391805 },
                { 0.1012865073, 0.7974269854, 0.1259391805 },
                { 0.4701420641, 0.0597158718, 0.1323941527 },
                { 0.4701420641, 0.4701420641, 0.1323941527 },
                { 0.0597158718, 0.4701420641, 0.1323941527 },
                { 0.3333333333, 0.3333333333, 0.225        },
            }
        };
    }
}
