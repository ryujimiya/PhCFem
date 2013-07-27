using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Diagnostics;
//using System.Numerics;
using KrdLab.clapack;  // KrdLab.clapack.Complex

namespace MyUtilLib.Matrix
{
    /// <summary>
    /// 対称バンド行列クラス(double)
    ///    KrdLab.Lisysをベースに変更
    ///    C#２次元配列とclapackの配列の変換オーバヘッドを無くすため + メモリ節約のために導入
    ///
    ///    LisysのMatrixのデータ構造と同じで1次元配列として行列データを保持します。
    ///    1次元配列は、clapackの配列数値格納順序と同じ（行データを先に格納する)
    /// </summary>
    public class MyDoubleSymmetricBandMatrix : MyDoubleMatrix
    {
        internal int _rowcolSize = 0;
        //internal int _subdiaSize = 0;  // 上三角の未格納するので常に0
        internal int _superdiaSize = 0;

        /// <summary>
        /// 内部バッファのインデックスを取得する
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        internal override int GetBufferIndex(int row, int col)
        {
            System.Diagnostics.Debug.Assert(row >= 0 && row < RowSize && col >= 0 && col < ColumnSize);
            // 上三角バンド行列
            if (!(row >= col - this._superdiaSize && row <= col))
            {
                System.Diagnostics.Debug.Assert(false);
                return -1;
            }
            return ((row - col) + this._superdiaSize + col * this._rsize);
        }

        /// <summary>
        /// 空のオブジェクトを作成する．
        /// </summary>
        internal MyDoubleSymmetricBandMatrix()
        {
            Clear();
        }

        /// <summary>
        /// 指定された配列をコピーして，新しい行列を作成する．
        /// </summary>
        /// <param name="body">コピーされる配列</param>
        /// <param name="rowSize">新しい行数=新しい列数</param>
        /// <param name="columnSize">subdiagonalのサイズ</param>
        /// <param name="columnSize">superdiagonalのサイズ</param>
        internal MyDoubleSymmetricBandMatrix(double[] body, int rowcolSize, int subdiaSize, int superdiaSize)
        {
            CopyFrom(body, rowcolSize, subdiaSize, superdiaSize);
        }

        /// <summary>
        /// 指定されたサイズの行列を作成する．
        /// 各要素は0に初期化される．--> 一旦削除 メモリ節約の為
        /// </summary>
        /// <param name="rowcolSize">行数=列数</param>
        /// <param name="subdiaSize">subdiagonalのサイズ</param>
        /// <param name="superdiaSize">superdiagonalのサイズ</param>
        public MyDoubleSymmetricBandMatrix(int rowcolSize, int subdiaSize, int superdiaSize)
        {
            //Resize(rowSize, columnSize, 0.0); // 一旦削除 メモリ節約の為
            Resize(rowcolSize, subdiaSize, superdiaSize);
        }

        /// <summary>
        /// ベースクラスのコンストラクタと同じ引数
        /// </summary>
        /// <param name="rowSize"></param>
        /// <param name="colSize"></param>
        private MyDoubleSymmetricBandMatrix(int rowSize, int colSize)
            //: base(rowSize, colSize)
        {
            System.Diagnostics.Debug.Assert(false);
            Clear();
        }

        /// <summary>
        /// 指定された行列をコピーして，新しい行列を作成する．
        /// </summary>
        /// <param name="m">コピーされる行列</param>
        public MyDoubleSymmetricBandMatrix(MyDoubleSymmetricBandMatrix m)
        {
            CopyFrom(m);
        }

        /// <summary>
        /// 2次元配列から新しい行列を作成する．
        /// </summary>
        /// <param name="arr">行列の要素を格納した2次元配列</param>
        public MyDoubleSymmetricBandMatrix(double[,] arr)
        {
            System.Diagnostics.Debug.Assert(arr.GetLength(0) == arr.GetLength(1));
            if (arr.GetLength(0) != arr.GetLength(1))
            {
                Clear();
                return;
            }
            int rowcolSize = arr.GetLength(0);

            // superdiaサイズを取得する
            int superdiaSize = 0;
            for (int c = 0; c < rowcolSize; c++)
            {
                if (c > 0)
                {
                    int cnt = 0;
                    for (int r = 0; r <= c - 1; r++)
                    {
                        // 非０要素が見つかったら抜ける
                        if (Math.Abs(arr[r, c]) >= Constants.PrecisionLowerLimit)
                        {
                            cnt = c - r;
                            break;
                        }
                    }
                    if (cnt > superdiaSize)
                    {
                        superdiaSize = cnt;
                    }
                }
            }

            // バッファの確保
            Resize(rowcolSize, superdiaSize, superdiaSize);
            // 値をコピーする
            for (int c = 0; c < rowcolSize; ++c)
            {
                // 対角成分
                this[c, c] = arr[c, c];

                // superdiagonals成分
                if (c > 0)
                {
                    for (int r = c - 1; r >= c - superdiaSize && r >= 0; r--)
                    {
                        this[r, c] = arr[r, c];
                    }
                }
            }
        }

        /// <summary>
        /// この行列の各要素を設定，取得する．(ベースクラスのI/Fのオーバーライド)
        /// </summary>
        /// <param name="row">行index（範囲：[0, <see cref="RowSize"/>) ）</param>
        /// <param name="col">列index（範囲：[0, <see cref="ColumnSize"/>) ）</param>
        /// <returns>要素の値</returns>
        public override double this[int row, int col]
        {
            get
            {
                if (row < 0 || this.RowSize <= row || col < 0 || this.ColumnSize <= col)
                {
                    throw new IndexOutOfRangeException();
                }
                if (!(row >= col - this._superdiaSize && row <= col + this._superdiaSize))
                {
                    return 0.0;
                }
                // 参照する場合は下三角も参照可とする
                int idx = (row <= col) ? this.GetBufferIndex(row, col) : this.GetBufferIndex(col, row);
                return this._body[idx];
            }
            set
            {
                if (row < 0 || this.RowSize <= row || col < 0 || this.ColumnSize <= col)
                {
                    throw new IndexOutOfRangeException();
                }
                if (!(row >= col - this._superdiaSize && row <= col + this._superdiaSize))
                {
                    return;
                }
                // 設定する場合は、上三角に限定する
                if (row > col)
                {
                    return;
                }
                int idx = this.GetBufferIndex(row, col);
                this._body[idx] = value;
            }
        }

        /// <summary>
        /// このオブジェクトの行数を取得する．
        /// </summary>
        public override int RowSize
        {
            get { return this._rowcolSize; }
        }

        /// <summary>
        /// このオブジェクトの列数を取得する．
        /// </summary>
        public override int ColumnSize
        {
            get { return this._rowcolSize; }
        }

        /// <summary>
        /// subdiagonalのサイズを取得する(対称行列なので==SuperdiaSize)
        /// </summary>
        public int SubdiaSize
        {
            get { return this._superdiaSize; }
        }

        /// <summary>
        /// superdiaginalのサイズを取得する
        /// </summary>
        public int SuperdiaSize
        {
            get { return this._superdiaSize; }
        }

        /// <summary>
        /// このオブジェクトをクリアする（<c>RowSize == 0 and ColumnSize == 0</c> になる）．(ベースクラスのI/Fのオーバーライド)
        /// </summary>
        public override void Clear()
        {
            // ベースクラスのクリアを実行する
            base.Clear();

            //this._body = new Complex[0];
            //this._rsize = 0;
            //this._csize = 0;
            this._rowcolSize = 0;
            this._superdiaSize = 0;
        }

        /// <summary>
        /// リサイズする．リサイズ後の各要素値は0になる．
        /// </summary>
        /// <param name="rowcolSize">新しい行数=新しい列数</param>
        /// <param name="subdiaSize">subdiagonalのサイズ</param>
        /// <param name="superdiaSize">subdiagonalのサイズ</param>
        /// <returns>リサイズ後の自身への参照</returns>
        public virtual MyDoubleSymmetricBandMatrix Resize(int rowcolSize, int subdiaSize, int superdiaSize)
        {
            //System.Diagnostics.Debug.Assert(subdiaSize == superdiaSize);

            int rsize = superdiaSize + 1;
            int csize = rowcolSize;
            base.Resize(rsize, csize);
            //this._body = new Complex[rsize * csize];
            //this._rsize = rsize;
            //this._csize = csize;
            this._rowcolSize = rowcolSize;
            this._superdiaSize = superdiaSize;
            return this;
        }

        /// <summary>
        /// ベースクラスのリサイズI/F (無効)
        /// </summary>
        /// <param name="rowSize"></param>
        /// <param name="columnSize"></param>
        /// <returns></returns>
        public override sealed MyDoubleMatrix Resize(int rowSize, int columnSize)
        {
            System.Diagnostics.Debug.Assert(false);
            //return base.Resize(rowSize, columnSize);
            return this;
        }

        /// <summary>
        /// 指定された行列をコピーする．
        /// </summary>
        /// <param name="m">コピーされる行列</param>
        /// <returns>コピー後の自身への参照</returns>
        public virtual MyDoubleSymmetricBandMatrix CopyFrom(MyDoubleSymmetricBandMatrix m)
        {
            return CopyFrom(m._body, m._rowcolSize, m._superdiaSize, m._superdiaSize);
        }

        /// <summary>
        /// ベースクラスのコピーI/F (無効)
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public override sealed MyDoubleMatrix CopyFrom(MyDoubleMatrix m)
        {
            System.Diagnostics.Debug.Assert(false);
            //return base.CopyFrom(m);
            return this;
        }

        /// <summary>
        /// <para>指定された1次元配列を，指定された行列形式でコピーする．</para>
        /// <para>配列のサイズと「rowSize * columnSize」は一致しなければならない．</para>
        /// </summary>
        /// <param name="body">コピーされる配列</param>
        /// <param name="rowcolSize">行数=列数</param>
        /// <param name="subdiaSize">subdiagonalのサイズ</param>
        /// <param name="superdiaSize">superdiagonalのサイズ</param>
        /// <returns>コピー後の自身への参照</returns>
        internal virtual MyDoubleSymmetricBandMatrix CopyFrom(double[] body, int rowcolSize, int subdiaSize, int superdiaSize)
        {
            //System.Diagnostics.Debug.Assert(subdiaSize == superdiaSize);
            int rsize = superdiaSize + 1;
            int csize = rowcolSize;

            // 入力の検証
            System.Diagnostics.Debug.Assert(body.Length == rsize * csize);
            if (body.Length != rsize * csize)
            {
                return this;
            }

            // バッファ確保
            if (this._rsize == rsize && this._csize == csize)
            {
            }
            else if (this._body != null && this._body.Length == rsize * csize)
            {
                this._rsize = rsize;
                this._csize = csize;
            }
            else
            {
                base.Resize(rsize, csize);
            }
            this._rowcolSize = rowcolSize;
            this._superdiaSize = superdiaSize;

            // コピー
            body.CopyTo(this._body, 0);
            return this;
        }

        /// <summary>
        /// ベースクラスのコピーI/F (無効)
        /// </summary>
        /// <param name="body"></param>
        /// <param name="rowSize"></param>
        /// <param name="columnSize"></param>
        /// <returns></returns>
        internal override sealed MyDoubleMatrix CopyFrom(double[] body, int rowSize, int columnSize)
        {
            System.Diagnostics.Debug.Assert(false);
            //return base.CopyFrom(body, rowSize, columnSize);
            return this;
        } 

        /// <summary>
        /// 転置する．(ベースクラスのI/Fのオーバーライド)
        /// </summary>
        /// <returns>転置後の自身への参照</returns>
        public override MyDoubleMatrix Transpose()
        {
            //return base.Transpose();
            // 対称なので転置しても同じマトリクス
            return this;
        }

    }
}
