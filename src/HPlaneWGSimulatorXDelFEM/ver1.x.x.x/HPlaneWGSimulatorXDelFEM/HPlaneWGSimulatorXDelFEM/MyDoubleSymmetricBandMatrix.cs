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
    /// �Ώ̃o���h�s��N���X(double)
    ///    KrdLab.Lisys���x�[�X�ɕύX
    ///    C#�Q�����z���clapack�̔z��̕ϊ��I�[�o�w�b�h�𖳂������� + �������ߖ�̂��߂ɓ���
    ///
    ///    Lisys��Matrix�̃f�[�^�\���Ɠ�����1�����z��Ƃ��čs��f�[�^��ێ����܂��B
    ///    1�����z��́Aclapack�̔z�񐔒l�i�[�����Ɠ����i�s�f�[�^���Ɋi�[����)
    /// </summary>
    public class MyDoubleSymmetricBandMatrix : MyDoubleMatrix
    {
        internal int _rowcolSize = 0;
        //internal int _subdiaSize = 0;  // ��O�p�̖��i�[����̂ŏ��0
        internal int _superdiaSize = 0;

        /// <summary>
        /// �����o�b�t�@�̃C���f�b�N�X���擾����
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        internal override int GetBufferIndex(int row, int col)
        {
            System.Diagnostics.Debug.Assert(row >= 0 && row < RowSize && col >= 0 && col < ColumnSize);
            // ��O�p�o���h�s��
            if (!(row >= col - this._superdiaSize && row <= col))
            {
                System.Diagnostics.Debug.Assert(false);
                return -1;
            }
            return ((row - col) + this._superdiaSize + col * this._rsize);
        }

        /// <summary>
        /// ��̃I�u�W�F�N�g���쐬����D
        /// </summary>
        internal MyDoubleSymmetricBandMatrix()
        {
            Clear();
        }

        /// <summary>
        /// �w�肳�ꂽ�z����R�s�[���āC�V�����s����쐬����D
        /// </summary>
        /// <param name="body">�R�s�[�����z��</param>
        /// <param name="rowSize">�V�����s��=�V������</param>
        /// <param name="columnSize">subdiagonal�̃T�C�Y</param>
        /// <param name="columnSize">superdiagonal�̃T�C�Y</param>
        internal MyDoubleSymmetricBandMatrix(double[] body, int rowcolSize, int subdiaSize, int superdiaSize)
        {
            CopyFrom(body, rowcolSize, subdiaSize, superdiaSize);
        }

        /// <summary>
        /// �w�肳�ꂽ�T�C�Y�̍s����쐬����D
        /// �e�v�f��0�ɏ����������D--> ��U�폜 �������ߖ�̈�
        /// </summary>
        /// <param name="rowcolSize">�s��=��</param>
        /// <param name="subdiaSize">subdiagonal�̃T�C�Y</param>
        /// <param name="superdiaSize">superdiagonal�̃T�C�Y</param>
        public MyDoubleSymmetricBandMatrix(int rowcolSize, int subdiaSize, int superdiaSize)
        {
            //Resize(rowSize, columnSize, 0.0); // ��U�폜 �������ߖ�̈�
            Resize(rowcolSize, subdiaSize, superdiaSize);
        }

        /// <summary>
        /// �x�[�X�N���X�̃R���X�g���N�^�Ɠ�������
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
        /// �w�肳�ꂽ�s����R�s�[���āC�V�����s����쐬����D
        /// </summary>
        /// <param name="m">�R�s�[�����s��</param>
        public MyDoubleSymmetricBandMatrix(MyDoubleSymmetricBandMatrix m)
        {
            CopyFrom(m);
        }

        /// <summary>
        /// 2�����z�񂩂�V�����s����쐬����D
        /// </summary>
        /// <param name="arr">�s��̗v�f���i�[����2�����z��</param>
        public MyDoubleSymmetricBandMatrix(double[,] arr)
        {
            System.Diagnostics.Debug.Assert(arr.GetLength(0) == arr.GetLength(1));
            if (arr.GetLength(0) != arr.GetLength(1))
            {
                Clear();
                return;
            }
            int rowcolSize = arr.GetLength(0);

            // superdia�T�C�Y���擾����
            int superdiaSize = 0;
            for (int c = 0; c < rowcolSize; c++)
            {
                if (c > 0)
                {
                    int cnt = 0;
                    for (int r = 0; r <= c - 1; r++)
                    {
                        // ��O�v�f�����������甲����
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

            // �o�b�t�@�̊m��
            Resize(rowcolSize, superdiaSize, superdiaSize);
            // �l���R�s�[����
            for (int c = 0; c < rowcolSize; ++c)
            {
                // �Ίp����
                this[c, c] = arr[c, c];

                // superdiagonals����
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
        /// ���̍s��̊e�v�f��ݒ�C�擾����D(�x�[�X�N���X��I/F�̃I�[�o�[���C�h)
        /// </summary>
        /// <param name="row">�sindex�i�͈́F[0, <see cref="RowSize"/>) �j</param>
        /// <param name="col">��index�i�͈́F[0, <see cref="ColumnSize"/>) �j</param>
        /// <returns>�v�f�̒l</returns>
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
                // �Q�Ƃ���ꍇ�͉��O�p���Q�ƉƂ���
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
                // �ݒ肷��ꍇ�́A��O�p�Ɍ��肷��
                if (row > col)
                {
                    return;
                }
                int idx = this.GetBufferIndex(row, col);
                this._body[idx] = value;
            }
        }

        /// <summary>
        /// ���̃I�u�W�F�N�g�̍s�����擾����D
        /// </summary>
        public override int RowSize
        {
            get { return this._rowcolSize; }
        }

        /// <summary>
        /// ���̃I�u�W�F�N�g�̗񐔂��擾����D
        /// </summary>
        public override int ColumnSize
        {
            get { return this._rowcolSize; }
        }

        /// <summary>
        /// subdiagonal�̃T�C�Y���擾����(�Ώ̍s��Ȃ̂�==SuperdiaSize)
        /// </summary>
        public int SubdiaSize
        {
            get { return this._superdiaSize; }
        }

        /// <summary>
        /// superdiaginal�̃T�C�Y���擾����
        /// </summary>
        public int SuperdiaSize
        {
            get { return this._superdiaSize; }
        }

        /// <summary>
        /// ���̃I�u�W�F�N�g���N���A����i<c>RowSize == 0 and ColumnSize == 0</c> �ɂȂ�j�D(�x�[�X�N���X��I/F�̃I�[�o�[���C�h)
        /// </summary>
        public override void Clear()
        {
            // �x�[�X�N���X�̃N���A�����s����
            base.Clear();

            //this._body = new Complex[0];
            //this._rsize = 0;
            //this._csize = 0;
            this._rowcolSize = 0;
            this._superdiaSize = 0;
        }

        /// <summary>
        /// ���T�C�Y����D���T�C�Y��̊e�v�f�l��0�ɂȂ�D
        /// </summary>
        /// <param name="rowcolSize">�V�����s��=�V������</param>
        /// <param name="subdiaSize">subdiagonal�̃T�C�Y</param>
        /// <param name="superdiaSize">subdiagonal�̃T�C�Y</param>
        /// <returns>���T�C�Y��̎��g�ւ̎Q��</returns>
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
        /// �x�[�X�N���X�̃��T�C�YI/F (����)
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
        /// �w�肳�ꂽ�s����R�s�[����D
        /// </summary>
        /// <param name="m">�R�s�[�����s��</param>
        /// <returns>�R�s�[��̎��g�ւ̎Q��</returns>
        public virtual MyDoubleSymmetricBandMatrix CopyFrom(MyDoubleSymmetricBandMatrix m)
        {
            return CopyFrom(m._body, m._rowcolSize, m._superdiaSize, m._superdiaSize);
        }

        /// <summary>
        /// �x�[�X�N���X�̃R�s�[I/F (����)
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
        /// <para>�w�肳�ꂽ1�����z����C�w�肳�ꂽ�s��`���ŃR�s�[����D</para>
        /// <para>�z��̃T�C�Y�ƁurowSize * columnSize�v�͈�v���Ȃ���΂Ȃ�Ȃ��D</para>
        /// </summary>
        /// <param name="body">�R�s�[�����z��</param>
        /// <param name="rowcolSize">�s��=��</param>
        /// <param name="subdiaSize">subdiagonal�̃T�C�Y</param>
        /// <param name="superdiaSize">superdiagonal�̃T�C�Y</param>
        /// <returns>�R�s�[��̎��g�ւ̎Q��</returns>
        internal virtual MyDoubleSymmetricBandMatrix CopyFrom(double[] body, int rowcolSize, int subdiaSize, int superdiaSize)
        {
            //System.Diagnostics.Debug.Assert(subdiaSize == superdiaSize);
            int rsize = superdiaSize + 1;
            int csize = rowcolSize;

            // ���͂̌���
            System.Diagnostics.Debug.Assert(body.Length == rsize * csize);
            if (body.Length != rsize * csize)
            {
                return this;
            }

            // �o�b�t�@�m��
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

            // �R�s�[
            body.CopyTo(this._body, 0);
            return this;
        }

        /// <summary>
        /// �x�[�X�N���X�̃R�s�[I/F (����)
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
        /// �]�u����D(�x�[�X�N���X��I/F�̃I�[�o�[���C�h)
        /// </summary>
        /// <returns>�]�u��̎��g�ւ̎Q��</returns>
        public override MyDoubleMatrix Transpose()
        {
            //return base.Transpose();
            // �Ώ̂Ȃ̂œ]�u���Ă������}�g���N�X
            return this;
        }

    }
}
