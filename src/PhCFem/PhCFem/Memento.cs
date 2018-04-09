using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyUtilLib
{
    /// <summary>
    /// 記念品抽象クラス
    /// </summary>
    /// <typeparam name="T1">思い出データの型</typeparam>
    /// <typeparam name="T2">データ反映対象オブジェクトの型</typeparam>
    public abstract class Memento<T1, T2>
    {
        /// <summary>
        /// 思い出データを取得または設定します。
        /// </summary>
        public T1 MementoData { get; protected set; }

        /// <summary>
        /// データ反映対象オブジェクトを取得または設定します。
        /// </summary>
        protected T2 Target { get; set; }

        /// <summary>
        /// 思い出を反映させます。
        /// </summary>
        public abstract void SetMemento(T1 _mementoData);
    }
}
