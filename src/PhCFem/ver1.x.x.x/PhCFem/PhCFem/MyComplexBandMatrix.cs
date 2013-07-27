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
    /// バンド行列クラス(Complex)
    ///    KrdLab.Lisysをベースに変更
    ///    C#２次元配列とclapackの配列の変換オーバヘッドを無くすため + メモリ節約のために導入
    ///
    ///    LisysのMatrixのデータ構造と同じで1次元配列として行列データを保持します。
    ///    1次元配列は、clapackの配列数値格納順序と同じ（行データを先に格納する)
    /// </summary>
    public class MyComplexBandMatrix : MyComplexMatrix
    {
        internal int _rowcolSize = 0;
        internal int _subdiaSize = 0;
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
            //BUGFIX
            //if (!(col >= row - this._superdiaSize && col <= row + this._subdiaSize))
            if (!(row >= col - this._superdiaSize && row <= col + this._subdiaSize))
            {
                System.Diagnostics.Debug.Assert(false);
                return -1;
            }

            return ((row - col) + this._subdiaSize + this._subdiaSize + col * this._rsize);
        }

        /// <summary>
        /// 空のオブジェクトを作成する．
        /// </summary>
        internal MyComplexBandMatrix()
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
        internal MyComplexBandMatrix(Complex[] body, int rowcolSize, int subdiaSize, int superdiaSize)
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
        public MyComplexBandMatrix(int rowcolSize, int subdiaSize, int superdiaSize)
        {
            //Resize(rowSize, columnSize, 0.0); // 一旦削除 メモリ節約の為
            Resize(rowcolSize, subdiaSize, superdiaSize);
        }

        /// <summary>
        /// ベースクラスのコンストラクタと同じ引数
        /// </summary>
        /// <param name="rowSize"></param>
        /// <param name="colSize"></param>
        private MyComplexBandMatrix(int rowSize, int colSize)
            //: base(rowSize, colSize)
        {
            System.Diagnostics.Debug.Assert(false);
            Clear();
        }

        /// <summary>
        /// 指定された行列をコピーして，新しい行列を作成する．
        /// </summary>
        /// <param name="m">コピーされる行列</param>
        public MyComplexBandMatrix(MyComplexBandMatrix m)
        {
            CopyFrom(m);
        }

        /// <summary>
        /// 2次元配列から新しい行列を作成する．
        /// </summary>
        /// <param name="arr">行列の要素を格納した2次元配列</param>
        public MyComplexBandMatrix(Complex[,] arr)
        {
            System.Diagnostics.Debug.Assert(arr.GetLength(0) == arr.GetLength(1));
            if (arr.GetLength(0) != arr.GetLength(1))
            {
                Clear();
                return;
            }
            int rowcolSize = arr.GetLength(0);

            // subdiaサイズ、superdiaサイズを取得する
            int subdiaSize = 0;
            int superdiaSize = 0;
            for (int c = 0; c < rowcolSize; c++)
            {
                if (c < rowcolSize - 1)
                {
                    int cnt = 0;
                    for (int r = rowcolSize - 1; r >= c + 1; r--)
                    {
                        // 非０要素が見つかったら抜ける
                        if (Math.Abs(arr[r, c].Real) >= Constants.PrecisionLowerLimit || Math.Abs(arr[r, c].Imaginary) >= Constants.PrecisionLowerLimit)
                        {
                            cnt = r - c;
                            break;
                        }
                    }
                    if (cnt > subdiaSize)
                    {
                        subdiaSize = cnt;
                    }
                }
                if (c > 0)
                {
                    int cnt = 0;
                    for (int r = 0; r <= c - 1; r++)
                    {
                        // 非０要素が見つかったら抜ける
                        if (Math.Abs(arr[r, c].Real) >= Constants.PrecisionLowerLimit || Math.Abs(arr[r, c].Imaginary) >= Constants.PrecisionLowerLimit)
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
            Resize(rowcolSize, subdiaSize, superdiaSize);
            // 値をコピーする
            for (int c = 0; c < rowcolSize; ++c)
            {
                // 対角成分
                this[c, c] = arr[c, c];

                // subdiagonal成分
                if (c < rowcolSize - 1)
                {
                    //BUGFIX
                    //for (int r = c + 1; r < c + subdiaSize && c < rowcolSize; r++)
                    for (int r = c + 1; r <= c + subdiaSize && r < rowcolSize; r++)
                    {
                        this[r, c] = arr[r, c];
                    }
                }
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
        public override Complex this[int row, int col]
        {
            get
            {
                if (row < 0 || this.RowSize <= row || col < 0 || this.ColumnSize <= col)
                {
                    throw new IndexOutOfRangeException();
                }
                //BUGFIX
                //if (!(col >= row - this._superdiaSize && col <= row + this._subdiaSize))
                if (!(row >= col - this._superdiaSize && row <= col + this._subdiaSize))
                {
                    return new Complex();
                }
                return this._body[(row - col) +this._subdiaSize + this._superdiaSize + col * this._rsize];
            }
            set
            {
                if (row < 0 || this.RowSize <= row || col < 0 || this.ColumnSize <= col)
                {
                    throw new IndexOutOfRangeException();
                }
                //BUGFIX
                //if (!(col >= row - this._superdiaSize && col <= row + this._subdiaSize))
                if (!(row >= col - this._superdiaSize && row <= col + this._subdiaSize))
                {
                    return;
                }
                this._body[(row - col) + this._subdiaSize + this._superdiaSize + col * this._rsize] = value;
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
        /// subdiagonalのサイズを取得する
        /// </summary>
        public int SubdiaSize
        {
            get { return this._subdiaSize; }
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
            this._subdiaSize = 0;
            this._superdiaSize = 0;
        }

        /// <summary>
        /// リサイズする．リサイズ後の各要素値は0になる．
        /// </summary>
        /// <param name="rowcolSize">新しい行数=新しい列数</param>
        /// <param name="subdiaSize">subdiagonalのサイズ</param>
        /// <param name="superdiaSize">subdiagonalのサイズ</param>
        /// <returns>リサイズ後の自身への参照</returns>
        public virtual MyComplexBandMatrix Resize(int rowcolSize, int subdiaSize, int superdiaSize)
        {
            int rsize = subdiaSize * 2 + superdiaSize + 1;
            int csize = rowcolSize;
            base.Resize(rsize, csize);
            //this._body = new Complex[rsize * csize];
            //this._rsize = rsize;
            //this._csize = csize;
            this._rowcolSize = rowcolSize;
            this._subdiaSize = subdiaSize;
            this._superdiaSize = superdiaSize;
            return this;
        }

        /// <summary>
        /// ベースクラスのリサイズI/F (無効)
        /// </summary>
        /// <param name="rowSize"></param>
        /// <param name="columnSize"></param>
        /// <returns></returns>
        public override sealed MyComplexMatrix Resize(int rowSize, int columnSize)
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
        public virtual MyComplexBandMatrix CopyFrom(MyComplexBandMatrix m)
        {
            return CopyFrom(m._body, m._rowcolSize, m._subdiaSize, m._superdiaSize);
        }

        /// <summary>
        /// ベースクラスのコピーI/F (無効)
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public override sealed MyComplexMatrix CopyFrom(MyComplexMatrix m)
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
        internal virtual MyComplexBandMatrix CopyFrom(Complex[] body, int rowcolSize, int subdiaSize, int superdiaSize)
        {
            int rsize = subdiaSize * 2 + superdiaSize + 1;
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
            this._subdiaSize = subdiaSize;
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
        internal override sealed MyComplexMatrix CopyFrom(Complex[] body, int rowSize, int columnSize)
        {
            System.Diagnostics.Debug.Assert(false);
            //return base.CopyFrom(body, rowSize, columnSize);
            return this;
        } 

        /// <summary>
        /// 転置する．(ベースクラスのI/Fのオーバーライド)
        /// </summary>
        /// <returns>転置後の自身への参照</returns>
        public override MyComplexMatrix Transpose()
        {
            //return base.Transpose();

            int rowcolSize = this._rowcolSize;
            int subdiaSize = 0;
            int superdiaSize = 0;
            // 転置後のsubdiaSize, superdiaSizeを取得する
            // rとcが入れ替わっていることに注意
            for (int r = 0; r < rowcolSize; r++)
            {
                if (r < rowcolSize - 1)
                {
                    int cnt = 0;
                    for (int c = rowcolSize - 1; c >= r + 1; c--)
                    {
                        // 非０要素が見つかったら抜ける
                        if (Math.Abs(this[c, r].Real) >= Constants.PrecisionLowerLimit || Math.Abs(this[c, r].Imaginary) >= Constants.PrecisionLowerLimit)
                        {
                            cnt = c - r;
                            break;
                        }
                    }
                    if (cnt > subdiaSize)
                    {
                        subdiaSize = cnt;
                    }
                }
                if (r > 0)
                {
                    int cnt = 0;
                    for (int c = 0; c <= r - 1; c++)
                    {
                        // 非０要素が見つかったら抜ける
                        if (Math.Abs(this[c, r].Real) >= Constants.PrecisionLowerLimit || Math.Abs(this[c, r].Imaginary) >= Constants.PrecisionLowerLimit)
                        {
                            cnt = r - c;
                            break;
                        }
                    }
                    if (cnt > superdiaSize)
                    {
                        superdiaSize = cnt;
                    }
                }
            }

            MyComplexBandMatrix t = new MyComplexBandMatrix(rowcolSize, subdiaSize, superdiaSize);
            for (int r = 0; r < this._rsize; ++r)
            {
                for (int c = 0; c < this._csize; ++c)
                {
                    //t[c, r] = this[r, c];
                    //t._body[c + r * this._csize] = this._body[r + c * this._rsize];
                    //t._body[(c - r) + t._subdiaSize + t._superdiaSize + r * t._rsize] = this._body[(r - c) + this._subdiaSize + this._superdiaSize + c * this._rsize];
                    t._body[(c - r) + t._subdiaSize + t._superdiaSize + r * t._rsize].Real = this._body[(r - c) + this._subdiaSize + this._superdiaSize + c * this._rsize].Real;
                    t._body[(c - r) + t._subdiaSize + t._superdiaSize + r * t._rsize].Imaginary = this._body[(r - c) + this._subdiaSize + this._superdiaSize + c * this._rsize].Imaginary;
                }
            }

            this.Clear();
            this._body = t._body;
            this._rsize = t._rsize;
            this._csize = t._csize;
            this._rowcolSize = t._rowcolSize;
            this._subdiaSize = t._subdiaSize;
            this._superdiaSize = t._superdiaSize;

            return this;
        }

        /// <summary>
        /// 複素共役転置する．(ベースクラスのI/Fのオーバーライド)
        /// </summary>
        /// <returns>転置後の自身への参照</returns>
        public override MyComplexMatrix ConjugateTranspose()
        {
            //return base.ConjugateTranspose();

            int rowcolSize = this._rowcolSize;
            int subdiaSize = 0;
            int superdiaSize = 0;
            // 転置後のsubdiaSize, superdiaSizeを取得する
            // rとcが入れ替わっていることに注意
            for (int r = 0; r < rowcolSize; r++)
            {
                if (r < rowcolSize - 1)
                {
                    int cnt = 0;
                    for (int c = rowcolSize - 1; c >= r + 1; c--)
                    {
                        // 非０要素が見つかったら抜ける
                        if (Math.Abs(this[c, r].Real) >= Constants.PrecisionLowerLimit || Math.Abs(this[c, r].Imaginary) >= Constants.PrecisionLowerLimit)
                        {
                            cnt = c - r;
                            break;
                        }
                    }
                    if (cnt > subdiaSize)
                    {
                        subdiaSize = cnt;
                    }
                }
                if (r > 0)
                {
                    int cnt = 0;
                    for (int c = 0; c <= r - 1; c++)
                    {
                        // 非０要素が見つかったら抜ける
                        if (Math.Abs(this[c, r].Real) >= Constants.PrecisionLowerLimit || Math.Abs(this[c, r].Imaginary) >= Constants.PrecisionLowerLimit)
                        {
                            cnt = r - c;
                            break;
                        }
                    }
                    if (cnt > superdiaSize)
                    {
                        superdiaSize = cnt;
                    }
                }
            }

            MyComplexBandMatrix t = new MyComplexBandMatrix(rowcolSize, subdiaSize, superdiaSize);
            for (int r = 0; r < this._rsize; ++r)
            {
                for (int c = 0; c < this._csize; ++c)
                {
                    //t[c, r] = Complex.Conjugate(this[r, c]);
                    //t._body[(c - r) + t._subdiaSize + t._superdiaSize + r * t._rsize] = Complex.Conjugate(this._body[(r - c + this._subdiaSize + this._superdiaSize + c * this._rsize]);
                    t._body[(c - r) + t._subdiaSize + t._superdiaSize + r * t._rsize].Real = this._body[(r - c) + this._subdiaSize + this._superdiaSize + c * this._rsize].Real;
                    t._body[(c - r) + t._subdiaSize + t._superdiaSize + r * t._rsize].Imaginary = -this._body[(r - c) + this._subdiaSize + this._superdiaSize + c * this._rsize].Imaginary;
                }
            }

            this.Clear();
            this._body = t._body;
            this._rsize = t._rsize;
            this._csize = t._csize;
            this._rowcolSize = t._rowcolSize;
            this._subdiaSize = t._subdiaSize;
            this._superdiaSize = t._superdiaSize;

            return this;
        }
 
    }
}
