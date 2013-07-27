using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyUtilLib
{
    /// <summary>
    /// ICommandインターフェイス
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// 呼び出し
        /// </summary>
        void Invoke();
        /// <summary>
        /// 元に戻す
        /// </summary>
        void Undo();
        /// <summary>
        /// やり直し
        /// </summary>
        void Redo();
    }
}
