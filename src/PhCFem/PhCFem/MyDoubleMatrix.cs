using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Numerics;

namespace MyUtilLib.Matrix
{
    /// <summary>
    /// 行列クラス(double)
    ///    KrdLab.Lisysをベースに変更
    ///    C#２次元配列とclapackの配列の変換オーバヘッドを無くすため + メモリ節約のために導入
    ///
    ///    LisysのMatrixのデータ構造と同じで1次元配列として行列データを保持します。
    ///    1次元配列は、clapackの配列数値格納順序と同じ（行データを先に格納する)
    ///    既存のdouble[,]からの置き換えポイント
    ///       double[,] --> MyDoubleMatrix
    ///       GetLength(0) --> RowSize
    ///       GetLength(1) --> ColumnSize
    /// </summary>
    public class MyDoubleMatrix
    {
        internal double[] _body = null;
        internal int _rsize = 0;
        internal int _csize = 0;

        /// <summary>
        /// 内部バッファのインデックスを取得する
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        internal virtual int GetBufferIndex(int row, int col)
        {
            System.Diagnostics.Debug.Assert(row >= 0 && row < RowSize && col >= 0 && col < ColumnSize);
            return (row + col * _rsize);
        }

        /// <summary>
        /// 空のオブジェクトを作成する．
        /// </summary>
        internal MyDoubleMatrix()
        {
            Clear();
        }

        /// <summary>
        /// 指定された配列をコピーして，新しい行列を作成する．
        /// </summary>
        /// <param name="body">コピーされる配列</param>
        /// <param name="rowSize">新しい行数</param>
        /// <param name="columnSize">新しい列数</param>
        internal MyDoubleMatrix(Double[] body, int rowSize, int columnSize)
        {
            CopyFrom(body, rowSize, columnSize);
        }

        /// <summary>
        /// 指定されたサイズの行列を作成する．
        /// 各要素は0に初期化される． ---> 一旦削除 メモリ節約の為
        /// </summary>
        /// <param name="rowSize">行数</param>
        /// <param name="columnSize">列数</param>
        public MyDoubleMatrix(int rowSize, int columnSize)
        {
            //Resize(rowSize, columnSize, 0.0); // 一旦削除 メモリ節約の為
            Resize(rowSize, columnSize);
        }

        /// <summary>
        /// 指定された行列をコピーして，新しい行列を作成する．
        /// </summary>
        /// <param name="m">コピーされる行列</param>
        public MyDoubleMatrix(MyDoubleMatrix m)
        {
            CopyFrom(m);
        }

        /// <summary>
        /// 2次元配列から新しい行列を作成する．
        /// </summary>
        /// <param name="arr">行列の要素を格納した2次元配列</param>
        public MyDoubleMatrix(double[,] arr)
        {
            int rsize = arr.GetLength(0);
            int csize = arr.GetLength(1);

            Resize(rsize, csize);

            for (int r = 0; r < rsize; ++r)
            {
                for (int c = 0; c < csize; ++c)
                {
                    this[r, c] = arr[r, c];
                }
            }
        }

        /// <summary>
        /// この行列の各要素を設定，取得する．
        /// </summary>
        /// <param name="row">行index（範囲：[0, <see cref="RowSize"/>) ）</param>
        /// <param name="col">列index（範囲：[0, <see cref="ColumnSize"/>) ）</param>
        /// <returns>要素の値</returns>
        public virtual double this[int row, int col]
        {
            get
            {
                if (row < 0 || this.RowSize <= row || col < 0 || this.ColumnSize <= col)
                {
                    throw new IndexOutOfRangeException();
                }
                return this._body[row + col * this._rsize];
            }
            set
            {
                if (row < 0 || this.RowSize <= row || col < 0 || this.ColumnSize <= col)
                {
                    throw new IndexOutOfRangeException();
                }
                this._body[row + col * this._rsize] = value;
            }
        }

        /// <summary>
        /// このオブジェクトの行数を取得する．
        /// </summary>
        public virtual int RowSize
        {
            get { return this._rsize; }
        }

        /// <summary>
        /// このオブジェクトの列数を取得する．
        /// </summary>
        public virtual int ColumnSize
        {
            get { return this._csize; }
        }

        /// <summary>
        /// このオブジェクトをクリアする（<c>RowSize == 0 and ColumnSize == 0</c> になる）．
        /// </summary>
        public virtual void Clear()
        {
            this._body = new double[0];
            this._rsize = 0;
            this._csize = 0;
        }

        /// <summary>
        /// リサイズする．リサイズ後の各要素値は0になる．
        /// </summary>
        /// <param name="rowSize">新しい行数</param>
        /// <param name="columnSize">新しい列数</param>
        /// <returns>リサイズ後の自身への参照</returns>
        public virtual MyDoubleMatrix Resize(int rowSize, int columnSize)
        {
            this._body = new double[rowSize * columnSize];
            this._rsize = rowSize;
            this._csize = columnSize;
            return this;
        }

        /*
        /// <summary>
        /// リサイズする．
        /// </summary>
        /// <param name="rowSize">新しい行数</param>
        /// <param name="columnSize">新しい列数</param>
        /// <param name="val">各要素の値</param>
        /// <returns>リサイズ後の自身への参照</returns>
        public MyDoubleMatrix Resize(int rowSize, int columnSize, double val)
        {
            Resize(rowSize, columnSize);
            for (int i = 0; i < this._body.Length; ++i)
            {
                this._body[i] = val;
            }
            return this;
        }
         */

        /// <summary>
        /// 指定された行列をコピーする．
        /// </summary>
        /// <param name="m">コピーされる行列</param>
        /// <returns>コピー後の自身への参照</returns>
        public virtual MyDoubleMatrix CopyFrom(MyDoubleMatrix m)
        {
            return CopyFrom(m._body, m._rsize, m._csize);
        }

        /// <summary>
        /// <para>指定された1次元配列を，指定された行列形式でコピーする．</para>
        /// <para>配列のサイズと「rowSize * columnSize」は一致しなければならない．</para>
        /// </summary>
        /// <param name="body">コピーされる配列</param>
        /// <param name="rowSize">行数</param>
        /// <param name="columnSize">列数</param>
        /// <returns>コピー後の自身への参照</returns>
        internal virtual MyDoubleMatrix CopyFrom(Double[] body, int rowSize, int columnSize)
        {
            // 入力の検証
            System.Diagnostics.Debug.Assert(body.Length == rowSize * columnSize);
            if (body.Length != rowSize * columnSize)
            {
                return this;
            }

            // バッファ確保
            if (this._rsize == rowSize && this._csize == columnSize)
            {
                // 何もしない
            }
            else if (this._body != null && this._body.Length == rowSize * columnSize)
            {
                this._rsize = rowSize;
                this._csize = columnSize;
            }
            else
            {
                Resize(rowSize, columnSize);
            }

            // コピー
            body.CopyTo(this._body, 0);
            return this;
        }

        /// <summary>
        /// 行列を 2次元配列として出力する．
        /// </summary>
        /// <returns>2次元配列（<c>array[r, c] == matrix[r, c]</c>）</returns>
        public double[,] ToArray()
        {
            double[,] ret = new double[this.RowSize, this.ColumnSize];
            for (int r = 0; r < this.RowSize; ++r)
            {
                for (int c = 0; c < this.ColumnSize; ++c)
                {
                    ret[r, c] = this[r, c];
                }
            }
            return ret;
        }

        /// <summary>
        /// この行列をゼロ行列にする．
        /// </summary>
        /// <returns></returns>
        public MyDoubleMatrix Zero()
        {
            int size = this._body.Length;
            for (int i = 0; i < size; ++i)
            {
                this._body[i] = 0.0;
            }
            return this;
        }

        /// <summary>
        /// この行列を単位行列（I = diag(1,1,...,1)）にする．
        /// <para>Unitは，全ての要素が1である行列のことをいう．Identifyとは異なることに注意せよ．</para>
        /// </summary>
        /// <returns></returns>
        public MyDoubleMatrix Identity()
        {
            //MyDoubleMatrixChecker.IsSquare(this);

            this.Zero();
            for (int i = 0; i < this.RowSize; ++i)
            {
                this[i, i] = 1;
            }
            return this;
        }

        /// <summary>
        /// 全ての要素の符号を反転する．
        /// </summary>
        /// <returns>自身への参照</returns>
        public MyDoubleMatrix Flip()
        {
            int size = this._body.Length;
            for (int i = 0; i < size; ++i)
            {
                this._body[i] = -this._body[i];
            }
            return this;
        }

        /// <summary>
        /// 転置する．
        /// </summary>
        /// <returns>転置後の自身への参照</returns>
        public virtual MyDoubleMatrix Transpose()
        {
            MyDoubleMatrix t = new MyDoubleMatrix(this._csize, this._rsize);

            for (int r = 0; r < this._rsize; ++r)
            {
                for (int c = 0; c < this._csize; ++c)
                {
                    t[c, r] = this[r, c];
                }
            }

            this.Clear();
            this._body = t._body;
            this._rsize = t._rsize;
            this._csize = t._csize;

            return this;
        }
    }
}
