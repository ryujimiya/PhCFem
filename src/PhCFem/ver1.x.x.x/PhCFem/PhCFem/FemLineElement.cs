using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Numerics; // Complex

namespace PhCFem
{
    /// <summary>
    /// 線要素
    /// </summary>
    class FemLineElement : FemElement
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FemLineElement()
            : base()
        {
        }

        /// <summary>
        /// コピー
        /// </summary>
        /// <param name="src"></param>
        public override void CP(FemElement src)
        {
            if (src == this)
            {
                return;
            }
            // 基本クラスのコピー
            base.CP(src);
        }
    }
}
