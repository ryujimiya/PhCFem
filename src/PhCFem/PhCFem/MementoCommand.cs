using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyUtilLib
{
    /// <summary>
    /// 思い出更新コマンド
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class MementoCommand<T1, T2> : ICommand
    {
        private Memento<T1, T2> _memento;
        private T1 _prev;
        private T1 _next;

        public MementoCommand(Memento<T1, T2> prev, Memento<T1, T2> next)
        {
            _memento = prev;
            _prev = prev.MementoData;
            _next = next.MementoData;
        }

        #region ICommand メンバ

        /// <summary>
        /// 呼び出し
        /// </summary>
        void ICommand.Invoke()
        {
            _prev = _memento.MementoData;
            _memento.SetMemento(_next);
        }

        /// <summary>
        /// 元に戻す
        /// </summary>
        void ICommand.Undo()
        {
            _memento.SetMemento(_prev);
        }

        /// <summary>
        /// やり直し
        /// </summary>
        void ICommand.Redo()
        {
            _memento.SetMemento(_next);
        }

        #endregion
    }
}
