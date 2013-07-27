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
using System.Drawing;

namespace PhCFem
{
    /// <summary>
    /// フィールド値カラーマップ
    /// </summary>
    class ColorMap
    {
        /// <summary>
        /// フィールドの最小値
        /// </summary>
        public double Min
        {
            get;
            set;
        }
        /// <summary>
        /// フィールドの最大値
        /// </summary>
        public double Max
        {
            get;
            set;
        }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ColorMap()
        {
            Min = 0.0;
            Max = 0.0;
        }
        /// <summary>
        /// 指定フィールド値に対する色を取得
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Color GetColor(double value)
        {
            double r = 0.0;
            if (Math.Abs(Max - Min) >= Constants.PrecisionLowerLimit)
            {
                r = (value - Min) / (Max - Min);
            }
            double d = 2.0 * r - 1;
            int[] color = new int[3]; // rgb
            if (r > 0.75)
            {
                color[0] = 255;
                color[1] = (int)((double)(2 - 2 * d) * 255);
                color[2] = 0;
            }
            else if (r > 0.50)
            {
                color[0] = (int)((double)(-4 * d * d + 4 * d) * 255);
                color[1] = 255;
                color[2] = 0;
            }
            else if (r > 0.25)
            {
                color[0] = 0;
                color[1] = 255;
                color[2] = (int)((double)(-4 * d * d - 4 * d) * 255);
            }
            else
            {
                color[0] = 0;
                color[1] = (int)((double)(2 + 2 * d) * 255);
                color[2] = 255;
            }
            for (int i = 0; i < color.Length; i++)
            {
                int colorVal = color[i];
                if (colorVal < 0) colorVal = 0;
                if (colorVal > 255) colorVal = 255;
                color[i] = colorVal;
            }
            
            Color c = Color.FromArgb(255, color[0], color[1], color[2]);
            return c;
        }
    }
}
