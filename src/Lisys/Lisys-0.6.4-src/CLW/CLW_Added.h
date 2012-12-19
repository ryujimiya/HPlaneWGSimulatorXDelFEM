#pragma once
/*
 * CLW.h (clapack Wrapper)
 *
 * @author KrdLab
 *
 */

using namespace System;

namespace KrdLab {
    namespace clapack {

        using namespace exception;

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // �ȉ�  ryujimiya�ǉ���
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Complex���g�p����I/F
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// ���f���\����
        ///   clapack��doublecomplex�ɑΉ����܂�
        ///   pin_ptr�����doublecomplex�^�ŃA�N�Z�X����̂����̍\���̂̎�v�p�r�ł�
        /// </summary>
        public value class Complex
        {
        private:
            /// <summary>
            /// ������
            /// </summary>
            double r;
            /// <summary>
            /// ������
            /// </summary>
            double i;
        public:
            /// <summary>
            /// �R���X�g���N�^
            /// </summary>
            /// <param name="r_">������</param>
            /// <param name="i_">������</param>
            Complex(double r_, double i_)
            {
                r = r_;
                i = i_;
            }
            /// <summary>
            /// ������
            /// </summary>
            property double Real
            {
                double get() { return r; }
                void set(double value) { r = value; }
            }
            /// <summary>
            /// ������
            /// </summary>
            property double Imaginary
            {
                double get() { return i; }
                void set(double value) { i = value; }
            }
            /// <summary>
            /// �傫���i��Βl)
            /// </summary>
            property double Magnitude
            {
                double get() { return System::Math::Sqrt(r * r + i * i); }
            }
            /// <summary>
            /// �t�F�[�Y
            /// </summary>
            property double Phase
            {
                double get() { return System::Math::Atan2(r, i); }
            }
            /// <summary>
            /// �������H
            /// </summary>
            /// <param name="value">�I�u�W�F�N�g</param>
            /// <returns></returns>
            virtual bool Equals(Object^ value) override
            {
                return Complex::Equals((Complex)value);
            }
            /// <summary>
            /// �������H
            /// </summary>
            /// <param name="value">���f��</param>
            /// <returns></returns>
            virtual bool Equals(Complex value) sealed
            {
                return (r == value.r && i == value.i);
            }

            /*
            inline Complex operator +=(Complex value)
            {
                r += value.r;
                i += value.i;
                return *this;
            }

            inline Complex operator -=(Complex value)
            {
                r -= value.r;
                i -= value.i;
                return *this;
            }

            inline Complex operator *=(Complex value)
            {
                double lhs_r = r;
                double lhs_i = i;
                r = lhs_r * value.r - lhs_i * value.i;
                i = lhs_r * value.i + lhs_i * value.r;
                return *this;
            }

            inline Complex operator /=(Complex value)
            {
                double lhs_r = r;
                double lhs_i = i;
                double valueSquare = value.r * value.r + value.i * value.i;
                r = (lhs_r * value.r + lhs_i * value.i) / valueSquare;
                i = (-lhs_r * value.i + lhs_i * value.r) / valueSquare;
                
                return *this;
            }
            */
        public:
            /// <summary>
            /// ���f�� 0
            /// </summary>
            static const Complex Zero;
            /// <summary>
            /// �����P��
            /// </summary>
            static const Complex ImaginaryOne = Complex(0.0, 1.0);
        public:
            /// <summary>
            /// double��Complex
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            static inline operator Complex(double value)
            {
                return Complex(value, 0);
            }
            /// <summary>
            /// int��Complex
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            static inline operator Complex(int value)
            {
                return Complex(value, 0);
            }
            /// <summary>
            /// System::Numerics::Complex��Complex
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            //static inline operator Complex(System::Numerics::Complex value)
            //{
            //    return Complex(value.Real, value.Imaginary);
            //}
            /// <summary>
            /// Complex��System::Numerics::Complex
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            static System::Numerics::Complex ToDotNetComplex(Complex value)
            {
                return System::Numerics::Complex(value.r, value.i);
            }
            /// <summary>
            /// ���f���̑傫��(��Βl)���擾����
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            static double Abs(Complex value)
            {
                return value.Magnitude;
            }
            /// <summary>
            /// ���f���̕��f�������擾����
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            static Complex Conjugate(Complex value)
            {
                return Complex(value.r, -value.i);
            }
            /// <summary>
            /// ���f���̕��������擾����
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            static Complex Sqrt(Complex value)
            {
                System::Numerics::Complex ret = System::Numerics::Complex::Sqrt(ToDotNetComplex(value));
                return Complex(ret.Real, ret.Imaginary);
            }
            /// <summary>
            /// exp(���f��)
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            static Complex Exp(Complex value)
            {
                System::Numerics::Complex ret = System::Numerics::Complex::Exp(ToDotNetComplex(value));
                return Complex(ret.Real, ret.Imaginary);
            }

            /// <summary>
            /// ���Z
            /// </summary>
            /// <param name="lhs"></param>
            /// <param name="rhs"></param>
            /// <returns></returns>
            static inline Complex Add(Complex lhs, Complex rhs)
            {
                return Complex(lhs.r + rhs.r, lhs.i + rhs.i);
            }
            static inline Complex Add(double d, Complex rhs)
            {
                return Complex(d + rhs.r, rhs.i);
            }
            static inline Complex Add(Complex lhs, double d)
            {
                return Complex(lhs.r + d, lhs.i);
            }
            //
            static inline Complex operator+(Complex lhs, Complex rhs)
            {
                return Complex(lhs.r + rhs.r, lhs.i + rhs.i);
            }
            static inline Complex operator+(double d, Complex rhs)
            {
                return Complex(d + rhs.r, rhs.i);
            }
            static inline Complex operator+(Complex lhs, double d)
            {
                return Complex(lhs.r + d, lhs.i);
            }
            /// <summary>
            /// ���Z
            /// </summary>
            /// <param name="lhs"></param>
            /// <param name="rhs"></param>
            /// <returns></returns>
            static inline Complex Subtract(Complex lhs, Complex rhs)
            {
                return Complex(lhs.r - rhs.r, lhs.i - rhs.i);
            }
            static inline Complex Subtract(double d, Complex rhs)
            {
                return Complex(d - rhs.r, -rhs.i);
            }
            static inline Complex Subtract(Complex lhs, double d)
            {
                return Complex(lhs.r - d, lhs.i);
            }
            //
            static inline Complex operator-(Complex lhs, Complex rhs)
            {
                return Complex(lhs.r - rhs.r, lhs.i - rhs.i);
            }
            static inline Complex operator-(double d, Complex rhs)
            {
                return Complex(d - rhs.r, -rhs.i);
            }
            static inline Complex operator-(Complex lhs, double d)
            {
                return Complex(lhs.r - d, lhs.i);
            }
            /// <summary>
            /// ��Z
            /// </summary>
            /// <param name="lhs"></param>
            /// <param name="rhs"></param>
            /// <returns></returns>
            static inline Complex Multiply(Complex lhs, Complex rhs)
            {
                return Complex(lhs.r * rhs.r - lhs.i * rhs.i, lhs.r * rhs.i + lhs.i * rhs.r);
            }
            static inline Complex Multiply(double d, Complex rhs)
            {
                return Complex(d * rhs.r, d * rhs.i);
            }
            static inline Complex Multiply(Complex lhs, double d)
            {
                return Complex(lhs.r * d, lhs.i * d);
            }
            //
            static inline Complex operator*(Complex lhs, Complex rhs)
            {
                return Complex(lhs.r * rhs.r - lhs.i * rhs.i, lhs.r * rhs.i + lhs.i * rhs.r);
            }
            static inline Complex operator*(double d, Complex rhs)
            {
                return Complex(d * rhs.r, d * rhs.i);
            }
            static inline Complex operator*(Complex lhs, double d)
            {
                return Complex(lhs.r * d, lhs.i * d);
            }
            /// <summary>
            /// ���Z
            ///    lhs * conj(rhs) / |rhs|^2
            /// </summary>
            /// <param name="lhs"></param>
            /// <param name="rhs"></param>
            /// <returns></returns>
            static inline Complex Divide(Complex lhs, Complex rhs)
            {
                double rhsSquare = rhs.r * rhs.r + rhs.i * rhs.i;
                return Complex((lhs.r * rhs.r + lhs.i * rhs.i) / rhsSquare, (-lhs.r * rhs.i + lhs.i * rhs.r) / rhsSquare);
            }
            static inline Complex Divide(double d, Complex rhs)
            {
                double rhsSquare = rhs.r * rhs.r + rhs.i * rhs.i;
                return Complex(d * rhs.r / rhsSquare, -d * rhs.i / rhsSquare);
            }
            static inline Complex Divide(Complex lhs, double d)
            {
                return Complex(lhs.r / d, lhs.i / d);
            }
            //
            static inline Complex operator/(Complex lhs, Complex rhs)
            {
                double rhsSquare = rhs.r * rhs.r + rhs.i * rhs.i;
                return Complex((lhs.r * rhs.r + lhs.i * rhs.i) / rhsSquare, (-lhs.r * rhs.i + lhs.i * rhs.r) / rhsSquare);
            }
            static inline Complex operator/(double d, Complex rhs)
            {
                double rhsSquare = rhs.r * rhs.r + rhs.i * rhs.i;
                return Complex(d * rhs.r / rhsSquare, -d * rhs.i / rhsSquare);
            }
            static inline Complex operator/(Complex lhs, double d)
            {
                return Complex(lhs.r / d, lhs.i / d);
            }
        };

        /// <summary>
        /// CLAPACK �� CLR ��ŗ��p���邽�߂̃��b�p�[�N���X
        ///   ryujimiya�ǉ�����ʂɂ��܂���
        /// </summary>
        public ref class FunctionExt : public Function
        {

        public:
            /// <summary>
            /// <para>A * X = B �������i X �����j�D</para>
            /// <para>A �� n�~n �̍s��CX �� B �� n�~nrhs �̍s��ł���D</para>
            /// </summary>
            /// <param name="X"><c>A * X = B</c> �̉��ł��� X ���i�[�����i���ۂɂ� B �Ɠ����I�u�W�F�N�g���w���j</param>
            /// <param name="x_row">�s�� X �̍s�����i�[�����i<c>== <paramref name="b_row"/></c>�j</param>
            /// <param name="x_col">�s�� X �̗񐔂��i�[�����i<c>== <paramref name="b_col"/></c>�j</param>
            /// <param name="A">�W���s��iLU�����̌��ʂł��� P*L*U �ɏ�����������DP*L*U�ɂ��Ă�<see cref="dgetrf"/>���Q�Ɓj</param>
            /// <param name="a_row">�s��A�̍s��</param>
            /// <param name="a_col">�s��A�̗�</param>
            /// <param name="B">�s�� B�i������CLAPACK�֐��ɂ�� X �̒l���i�[�����j</param>
            /// <param name="b_row">�s��B�̍s��</param>
            /// <param name="b_col">�s��B�̗�</param>
            /// <returns>��� 0 ���Ԃ��Ă���D</returns>
            /// <exception cref="IllegalClapackArgumentException">
            /// ������ zgesv_�֐��ɓn���ꂽ�����ɖ�肪����� throw �����D
            /// </exception>
            /// <exception cref="IllegalClapackResultException">
            /// �s�� A �� LU�����ɂ����āCU[i, i] �� 0 �ƂȂ��Ă��܂����ꍇ�� throw �����D
            /// ���̏ꍇ�C�������߂邱�Ƃ��ł��Ȃ��D
            /// </exception>
            /// <remarks>
            /// <para>�Ή�����CLAPACK�֐��iCLAPACK/SRC/zgesv.c�j</para>
            /// <code>
            /// int zgesv_(integer *n, integer *nrhs,
            ///            doublecomplex *a, integer *lda, integer *ipiv,
            ///            doublecomplex *b, integer *ldb, integer *info)
            /// </code>
            /// <para>zgesv_ �֐��̓����ł� LU�������g�p����Ă���D</para>
            /// </remarks>
            static int zgesv(array<Complex>^% X, int% x_row, int% x_col,
                             array<Complex>^  A, int  a_row, int  a_col,
                             array<Complex>^  B, int  b_row, int  b_col)
            {
                integer n    = a_row;    // input: �A���ꎟ�������̎����D�����s��[A]�̎���(n��0)�D 
                integer nrhs = b_col;    // input: �s��B��Column��
                
                pin_ptr<void> a_ptr = &A[0];
                doublecomplex* a = (doublecomplex *)(void *)a_ptr;
                
                pin_ptr<void> b_ptr = &B[0];
                doublecomplex* b = (doublecomplex *)(void *)b_ptr;

                // �z��`���� a[lda�~n]
                //   input : n�sn��̌W���s��[A]
                //   output: LU������̍s��[L]�ƍs��[U]�D�������C�s��[L]�̒P�ʑΊp�v�f�͊i�[����Ȃ��D
                //           A=[P]*[L]*[U]�ł���C[P]�͍s�Ɨ�����ւ��鑀��ɑΉ�����u���s��ƌĂ΂�C0��1���i�[�����D
                
                integer lda = n;
                // input: �s��A�̑�ꎟ��(�̃������i�[��)�Dlda��max(1,n)�ł���C�ʏ�� lda==n �ŗǂ��D

                integer* ipiv = new integer[n];
                // output: �傫��n�̔z��D�u���s��[P]���`���鎲�I��p�Y�����D

                // input/output: �z��`����b[ldb�~nrhs]�D�ʏ��nrhs��1�Ȃ̂ŁC�z��`����b[ldb]�ƂȂ�D
                // input : b[ldb�~nrhs]�̔z��`���������E�Ӎs��{B}�D
                // output: info==0 �̏ꍇ�ɁCb[ldb�~nrhs]�`���̉��s��{X}���i�[�����D

                integer ldb = b_row;
                // input: �z��b�̑�ꎟ��(�̃������i�[��)�Dldb��max(1,n)�ł���C�ʏ�� ldb==n �ŗǂ��D

                integer info = 1;
                // output:
                // info==0: ����I��
                // info < 0: info==-i �Ȃ�΁Ci�Ԗڂ̈����̒l���Ԉ���Ă��邱�Ƃ������D
                // 0 < info <N-1: �ŗL�x�N�g���͌v�Z����Ă��Ȃ����Ƃ������D
                // info > N: LAPACK���Ŗ�肪���������Ƃ������D

                int ret;
                try
                {
                    ret = zgesv_(&n, &nrhs, a, &lda, ipiv, b, &ldb, &info);
                    
                    if(info == 0)
                    {
                        X = B;
                        x_row = b_row;
                        x_col = b_col;
                    }
                    else
                    {
                        X = nullptr;
                        x_row = 0;
                        x_col = 0;
                        
                        if(info < 0)
                        {
                            throw gcnew IllegalClapackArgumentException("Error occurred: " + -info + "-th argument had an illegal value in the clapack.Function.zgesv", -info);
                        }
                        else
                        {
                            throw gcnew IllegalClapackResultException("Error occurred: zgesv_", info);
                        }
                    }
                }
                finally
                {
                    delete[] ipiv; ipiv = nullptr;
                }

                return ret;
            }

            /// <summary>
            /// <para>A * X = B �������iX �����j�D</para>
            /// <para>A �� n�~n �̃o���h�s��CX �� B �� n�~nrhs �̍s��ł���D</para>
            /// </summary>
            /// <param name="X"><c>A * X = B</c> �̉��ł��� X ���i�[�����i���ۂɂ� B �Ɠ����I�u�W�F�N�g���w���j</param>
            /// <param name="x_row">�s�� X �̍s�����i�[�����i<c>== <paramref name="b_row"/></c>�j</param>
            /// <param name="x_col">�s�� X �̗񐔂��i�[�����i<c>== <paramref name="b_col"/></c>�j</param>
            /// <param name="A">�o���h�X�g���[�W�`���Ŋi�[���ꂽ�W���s��iLU�����̌��ʂł��� P*L*U �ɏ�����������D�j</param>
            /// <param name="a_row">�s��A�̍s��</param>
            /// <param name="a_col">�s��A�̗�</param>
            /// <param name="kl">�o���h�s��A����subdiagonal�̐�</param>
            /// <param name="ku">�o���h�s��A����superdiagonal�̐�</param>
            /// <param name="B">�s�� B�i������CLAPACK�֐��ɂ�� X �̒l���i�[�����j</param>
            /// <param name="b_row">�s��B�̍s��</param>
            /// <param name="b_col">�s��B�̗�</param>
            /// <returns>��� 0 ���Ԃ��Ă���D</returns>
            /// <exception cref="IllegalClapackArgumentException">
            /// ������ zgbsv_�֐��ɓn���ꂽ�����ɖ�肪����� throw �����D
            /// </exception>
            /// <exception cref="IllegalClapackResultException">
            /// �s�� A �� LU�����ɂ����āCU[i, i] �� 0 �ƂȂ��Ă��܂����ꍇ�� throw �����D
            /// ���̏ꍇ�C�������߂邱�Ƃ��ł��Ȃ��D
            /// </exception>
            /// <remarks>
            /// <para>�Ή�����CLAPACK�֐��iCLAPACK/SRC/zgbsv.c�j</para>
            /// <code>
            /// int zgbsv_(integer *n, integer *kl, integer *ku, integer *nrhs,
            ///            doublecomplex *ab, integer *ldab, integer *ipiv,
            ///            doublecomplex *b, integer *ldb, integer *info)
            /// </code>
            /// <para>zgbsv_ �֐��̓����ł� LU�������g�p����Ă���D</para>
            /// </remarks>
            static int zgbsv(array<Complex>^% X, int% x_row, int% x_col,
                             array<Complex>^  A, int  a_row, int  a_col,
                             int kl, int ku,
                             array<Complex>^  B, int  b_row, int  b_col)
            {
                pin_ptr<void> a_ptr = &A[0];
                doublecomplex* a = (doublecomplex *)(void *)a_ptr;
                // COMPLEX*16 array, dimension (LDAB,N)
                // On entry, the matrix A in band storage, in rows KL+1 to 2*KL+KU+1; rows 1 to KL of the array need not be set.
                // The j-th column of A is stored in the j-th column of the array AB as follows:
                // AB(KL+KU+1+i-j,j) = A(i,j) for max(1,j-KU)<=i<=min(N,j+KL)
                
                pin_ptr<void> b_ptr = &B[0];
                doublecomplex* b = (doublecomplex *)(void *)b_ptr;
                // COMPLEX*16 array, dimension (LDB,NRHS)
                // On entry, the N-by-NRHS right hand side matrix B.
                
                integer n = a_row;
                // The number of linear equations, i.e., the order of the matrix A.  N >= 0.

                integer kl_ = kl;
                // The number of subdiagonals within the band of A.  KL >= 0.

                integer ku_ = ku;
                // The number of superdiagonals within the band of A.  KU >= 0.

                integer nrhs = b_col;
                // The number of right hand sides, i.e., the number of columns

                integer lda = 2 * kl + ku + 1;
                // The leading dimension of the array AB.  LDAB >= 2*KL+KU+1.

                integer* ipiv = new integer[n];

                integer ldb = b_row;

                integer info = 1;
                // output:
                // info==0: ����I��
                // info < 0: info==-i �Ȃ�΁Ci�Ԗڂ̈����̒l���Ԉ���Ă��邱�Ƃ������D
                // 0 < info <N-1: �ŗL�x�N�g���͌v�Z����Ă��Ȃ����Ƃ������D
                // info > N: LAPACK���Ŗ�肪���������Ƃ������D

                int ret;
                try
                {
                    ret = zgbsv_(&n, &kl_, &ku_, &nrhs, a, &lda, ipiv, b, &ldb, &info);
                    
                    if(info == 0)
                    {
                        X = B;
                        x_row = b_row;
                        x_col = b_col;
                    }
                    else
                    {
                        X = nullptr;
                        x_row = 0;
                        x_col = 0;
                        
                        if(info < 0)
                        {
                            throw gcnew IllegalClapackArgumentException("Error occurred: " + -info + "-th argument had an illegal value in the clapack.Function.zgbsv", -info);
                        }
                        else
                        {
                            throw gcnew IllegalClapackResultException("Error occurred: zgbsv_", info);
                        }
                    }
                }
                finally
                {
                    delete[] ipiv; ipiv = nullptr;
                }

                return ret;
            }

            /// <summary>
            /// <para>�ŗL�l����</para>
            /// <para>�v�Z���ꂽ�ŗL�x�N�g���́C�傫���i���[�N���b�h�m�����j�� 1 �ɋK�i������Ă���D</para>
            /// </summary>
            /// <param name="X">�ŗL�l���������s��i�v�Z�̉ߒ��ŏ㏑�������j</param>
            /// <param name="x_row">�s�� <paramref name="X"/> �̍s��</param>
            /// <param name="x_col">�s�� <paramref name="X"/> �̗�</param>
            /// <param name="evals">�ŗL�l</param>
            /// <param name="evecs">�ŗL�x�N�g��</param>
            /// <returns>��� 0 ���Ԃ��Ă���D</returns>
            /// <remarks>
            /// <para>�Ή�����CLAPACK�֐��iCLAPACK/BLAS/SRC/zgeev.c�j</para>
            /// <code>
            /// int zgeev_(char *jobvl, char *jobvr, integer *n, 
            ///            doublecomplex *a, integer *lda, doublecomplex *w, doublecomplex *vl, 
            ///            integer *ldvl, doublecomplex *vr, integer *ldvr, doublecomplex *work, 
            ///            integer *lwork, doublereal *rwork, integer *info)
            /// </code>
            /// </remarks>
            static int zgeev(array<Complex>^ X, int x_row, int x_col,
                             array<Complex>^% evals,
                             array< array<Complex>^ >^% evecs)
            {
                char jobvl = 'N';
                // ���ŗL�x�N�g����
                //   if jobvl == 'V' then �v�Z����
                //   if jobvl == 'N' then �v�Z���Ȃ�

                char jobvr = 'V';
                // �E�ŗL�x�N�g����
                //   if jobvr == 'V' then �v�Z����
                //   if jobvr == 'N' then �v�Z���Ȃ�

                integer n = x_col;
                // �s�� X �̑傫���iN�~N�Ȃ̂ŁC�Е������ł悢�j
                
                integer lda = n;
                // the leading dimension of the array A. lda >= max(1, N).

                pin_ptr<void> a_ptr = &X[0];
                doublecomplex* a = (doublecomplex *)(void *)a_ptr;

                // [lda, n] N�~N �̍s�� X
                // �z�� a �i�s�� X�j�́C�v�Z�̉ߒ��ŏ㏑�������D
                
                doublecomplex* w = new doublecomplex[n];
                

                /*
                 * �����ŗL�x�N�g���͌v�Z���Ȃ�
                 */
                

                integer ldvl = 1;
                // �K�� 1 <= ldvl �𖞂����K�v������D
                // if jobvl == 'V' then N <= ldvl

                doublecomplex* vl = nullptr;
                // vl is not referenced, because jobvl == 'N'.
                
                /*
                 * ���E�ŗL�x�N�g���͌v�Z����
                 */

                integer ldvr = n;
                // �K�� 1 <= ldvr �𖞂����K�v������D
                // if jobvr == 'V' then N <= ldvr

                doublecomplex* vr = new doublecomplex[ldvr * n];
                // if jobvr == 'V' then �E�ŗL�x�N�g���� vr �̊e��ɁC�ŗL�l�Ɠ��������Ŋi�[�����D
                // if jobvr == 'N' then vr is not referenced.
                
                //
                // ���̑�

                //integer lwork = 4*n;
                integer lwork = 2*n;
                // max(1,2*N) <= lwork
                // �ǂ��p�t�H�[�}���X�𓾂邽�߂ɁC���̏ꍇ lwork �͑傫�����ׂ����D
                doublecomplex* work = new doublecomplex[lwork];
                // if info == 0 then work[0] returns the optimal lwork.

                doublereal* rwork = new doublereal[2 * n];
                
                integer info = 0;
                // if info == 0 then ����I��
                // if info <  0 then -info �Ԗڂ̈����̒l���Ԉ���Ă���D
                // if info >  0 then QR�A���S���Y���́C�S�Ă̌ŗL�l���v�Z�ł��Ȃ������D
                //                   �ŗL�x�N�g���͌v�Z����Ă��Ȃ��D
                //                   wr[info+1:N] �� wl[info+1:N] �ɂ́C���������ŗL�l���܂܂�Ă���D
                
                
                int ret;
                try
                {
                    // CLAPACK���[�`��
                    ret = zgeev_(&jobvl, &jobvr, &n, a, &lda, w, vl, &ldvl, vr, &ldvr, work, &lwork, rwork, &info);

                    if(info == 0)
                    {
                        //
                        // �ŗL�l���i�[
                        evals = gcnew array<Complex>(n);
                        for (int i = 0; i < n; i++)
                        {
                            evals[i] = Complex(w[i].r, w[i].i);
                        }
                        
                        //
                        // �ŗL�x�N�g�����i�[
                        evecs = gcnew array< array<Complex>^ >(n);
                        for (int i = 0; i < n; i++)
                        {
                            // �ʏ�̊i�[����
                            evecs[i] = gcnew array<Complex>(ldvr);
                            for(int j=0; j<ldvr; ++j)
                            {
                                doublecomplex v = vr[i*ldvr + j];
                                evecs[i][j] = Complex(v.r, v.i);
                            }
                        }// end for i
                    }// end if info == 0
                    else
                    {
                        if(info < 0)
                        {
                            throw gcnew IllegalClapackArgumentException(
                                "Error occurred: " + -info
                                    + "-th argument had an illegal value in the clapack.Function.zgeev", -info);
                        }
                        else
                        {
                            throw gcnew IllegalClapackResultException("Error occurred: zgeev_", info);
                        }
                    }
                }
                finally
                {
                    // unmanaged code �̌�n��
                    delete[] w; w = nullptr;
                    delete[] vr; vr = nullptr;
                    delete[] work; work = nullptr;
                    delete[] rwork; rwork = nullptr;
                }

                return ret;
            }

            /// <summary>
            /// <para>��ʉ��ŗL�l��� Ax=��Bx������</para>
            /// <para>�v�Z���ꂽ�ŗL�x�N�g���́C�傫���i���[�N���b�h�m�����j�� 1 �ɋK�i������Ă���D</para>
            /// </summary>
            /// <param name="A">�ŗL�l���������s��A�i�v�Z�̉ߒ��ŏ㏑�������j</param>
            /// <param name="a_row">�s�� <paramref name="A"/> �̍s��</param>
            /// <param name="a_col">�s�� <paramref name="A"/> �̗�</param>
            /// <param name="B">�ŗL�l���������s��B�i�v�Z�̉ߒ��ŏ㏑�������j</param>
            /// <param name="b_row">�s�� <paramref name="B"/> �̍s��</param>
            /// <param name="b_col">�s�� <paramref name="B"/> �̗�</param>
            /// <param name="r_evals">�ŗL�l�̎�����</param>
            /// <param name="i_evals">�ŗL�l�̋�����</param>
            /// <param name="r_evecs">�ŗL�x�N�g���̎�����</param>
            /// <param name="i_evecs">�ŗL�x�N�g���̋�����</param>
            /// <returns>��� 0 ���Ԃ��Ă���D</returns>
            /// <remarks>
            /// <para>�Ή�����CLAPACK�֐��iCLAPACK/BLAS/SRC/dggev.c�j</para>
            /// <code>
            /// int dggev_(char *jobvl, char *jobvr, integer *n,
            ///            doublereal *a, integer *lda,
            ///            doublereal *b, integer *ldb,
            ///            doublereal *alphar, doublereal *alphai,
            ///            doublereal *beta,
            ///            doublereal *vl, integer *ldvl, doublereal *vr, integer *ldvr,
            ///            doublereal *work, integer *lwork, integer *info);
            /// </code>
            /// </remarks>
            static int dggev(array<doublereal>^ A, int a_row, int a_col,
                             array<doublereal>^ B, int b_row, int b_col,
                             array<doublereal>^% r_evals,
                             array<doublereal>^% i_evals,
                             array< array<doublereal>^ >^% r_evecs,
                             array< array<doublereal>^ >^% i_evecs )
            {
                char jobvl = 'N';
                // ���ŗL�x�N�g����
                //   if jobvl == 'V' then �v�Z����
                //   if jobvl == 'N' then �v�Z���Ȃ�

                char jobvr = 'V';
                // �E�ŗL�x�N�g����
                //   if jobvr == 'V' then �v�Z����
                //   if jobvr == 'N' then �v�Z���Ȃ�

                integer n = a_col;
                // �s�� A, B �̑傫���iN�~N�Ȃ̂ŁC�Е������ł悢�j

                integer lda = n;
                // the leading dimension of the array A. lda >= max(1, N).

                pin_ptr<doublereal> a = &A[0];
                // [lda, n] N�~N �̍s�� A
                // �z�� a �i�s�� A�j�́C�v�Z�̉ߒ��ŏ㏑�������D
                
                integer ldb = n;
                // the leading dimension of B. ldb >= max(1,N).

                pin_ptr<doublereal> b = &B[0];
                // [ldb, n] N�~N �̍s�� B
                // �z�� b �i�s�� B�j�́C�v�Z�̉ߒ��ŏ㏑�������D

                doublereal* alphar = new doublereal[n];
                // �v�Z���ꂽ�ŗL�l�̎����̕��q����������D

                doublereal* alphai = new doublereal[n];
                // �v�Z���ꂽ�ŗL�l�̋����̕��q����������D
                // ���f����΂̏ꍇ�́Calphai[j]=(���l)�Calphai[j+1]=(���l) �̏��ɓ���D

                doublereal* beta = new doublereal[n];
                // �v�Z���ꂽ�ŗL�l�̕��ꕔ��������
                //  (alphar[j] + alphai[j] * i) / beta[j] (i:�����P��)����ʉ��ŗL�l�ƂȂ�

                /*
                 * �����ŗL�x�N�g���͌v�Z���Ȃ�
                 */


                integer ldvl = 1;
                // �K�� 1 <= ldvl �𖞂����K�v������D
                // if jobvl == 'V' then N <= ldvl

                doublereal* vl = nullptr;
                // vl is not referenced, because jobvl == 'N'.

                /*
                 * ���E�ŗL�x�N�g���͌v�Z����
                 */

                integer ldvr = n;
                // �K�� 1 <= ldvr �𖞂����K�v������D
                // if jobvr == 'V' then N <= ldvr

                doublereal* vr = new doublereal[ldvr * n];
                // if jobvr == 'V' then �E�ŗL�x�N�g���� vr �̊e��ɁC�ŗL�l�Ɠ��������Ŋi�[�����D
                // if jobvr == 'N' then vr is not referenced.

                //
                // ���̑�

                integer lwork = 8*n;
                // max(1, 8*N) <= lwork
                // if jobvl == 'V' or jobvr == 'V' then 8*N <= lwork
                // �ǂ��p�t�H�[�}���X�𓾂邽�߂ɁC���̏ꍇ lwork �͑傫�����ׂ����D
                doublereal* work = new doublereal[lwork];
                // if info == 0 then work[0] returns the optimal lwork.

                integer info = 0;
                // if info == 0 then ����I��
                // if info <  0 then -info �Ԗڂ̈����̒l���Ԉ���Ă���D
                // if info >  0 then QR�A���S���Y���́C�S�Ă̌ŗL�l���v�Z�ł��Ȃ������D
                //                   �ŗL�x�N�g���͌v�Z����Ă��Ȃ��D
                //                   wr[info+1:N] �� wl[info+1:N] �ɂ́C���������ŗL�l���܂܂�Ă���D


                int ret;
                try
                {
                    // CLAPACK���[�`��
                    ret = dggev_(&jobvl, &jobvr, &n, a, &lda, b, &ldb, alphar, alphai, beta, vl, &ldvl, vr, &ldvr, work, &lwork, &info);

                    if(info == 0)
                    {
                        //
                        // �ŗL�l���i�[
                        r_evals = gcnew array<doublereal>(n);
                        i_evals = gcnew array<doublereal>(n);

                        for(int i=0; i<n; ++i)
                        {
                            if (Math::Abs(beta[i]) < CalculationLowerLimit)
                            {
                                r_evals[i] = ((Math::Abs(alphar[i]) < CalculationLowerLimit) ?
                                    (Double::NaN)
                                    : ((Math::Abs(alphar[i]) > 0) ? (Double::PositiveInfinity) : (Double::NegativeInfinity)));
                                i_evals[i] = ((Math::Abs(alphai[i]) < CalculationLowerLimit) ?
                                    (Double::NaN)
                                    : ((Math::Abs(alphai[i]) > 0) ? (Double::PositiveInfinity) : (Double::NegativeInfinity)));
                            }
                            else
                            {
                                r_evals[i] = ((Math::Abs(alphar[i]) < CalculationLowerLimit) ? (0.0) : (alphar[i] / beta[i]));
                                i_evals[i] = ((Math::Abs(alphai[i]) < CalculationLowerLimit) ? (0.0) : (alphai[i] / beta[i]));
                            }
                        }

                        //
                        // �ŗL�x�N�g�����i�[
                        r_evecs = gcnew array< array<doublereal>^ >(n);
                        i_evecs = gcnew array< array<doublereal>^ >(n);

                        for(int i=0; i<n; ++i)
                        {
                            if(Math::Abs(alphai[i]) < CalculationLowerLimit)
                            {
                                // �ʏ�̊i�[����
                                r_evecs[i] = gcnew array<doublereal>(ldvr);
                                i_evecs[i] = gcnew array<doublereal>(ldvr);

                                for(int j=0; j<ldvr; ++j)
                                {
                                    r_evecs[i][j] = vr[i*ldvr + j];
                                    i_evecs[i][j] = 0.0;
                                }
                            }
                            else
                            {
                                // �����ɂȂ����Ƃ�
                                array<doublereal>^ realvec1 = gcnew array<doublereal>(ldvr);
                                array<doublereal>^ realvec2 = gcnew array<doublereal>(ldvr);
                                array<doublereal>^ imgyvec1 = gcnew array<doublereal>(ldvr);
                                array<doublereal>^ imgyvec2 = gcnew array<doublereal>(ldvr);

                                for(int j=0; j<ldvr; ++j)
                                {
                                    realvec1[j] =  vr[ i   *ldvr + j];
                                    realvec2[j] =  vr[ i   *ldvr + j];
                                    imgyvec1[j] =  vr[(i+1)*ldvr + j];
                                    imgyvec2[j] = -vr[(i+1)*ldvr + j];
                                }
                                r_evecs[i  ] = realvec1;
                                r_evecs[i+1] = realvec2;
                                i_evecs[i  ] = imgyvec1;
                                i_evecs[i+1] = imgyvec2;

                                ++i;    // 2�񂸂Q�Ƃ���̂ŁC�����ŃJ�E���g�A�b�v
                            }

                        }// end for i
                    }// end if info == 0
                    else
                    {
                        if(info < 0)
                        {
                            throw gcnew IllegalClapackArgumentException(
                                "Error occurred: " + -info
                                    + "-th argument had an illegal value in the clapack.Function.dggev", -info);
                        }
                        else
                        {
                            throw gcnew IllegalClapackResultException("Error occurred: dggev_", info);
                        }
                    }
                }
                finally
                {
                    // unmanaged code �̌�n��
                    delete[] alphar; alphar = nullptr;
                    delete[] alphai; alphai = nullptr;
                    delete[] beta; beta = nullptr;
                    delete[] vr; vr = nullptr;
                    delete[] work; work = nullptr;
                }

                return ret;
            }

            /// <summary>
            /// <para>��ʉ��ŗL�l��� Ax=��Bx������</para>
            /// <para>A�͑Ώ̑эs��. B�͑Ώ̑эs�񂩂���l�s��ł���. �v�Z���ꂽ�ŗL�x�N�g���́C�傫���i���[�N���b�h�m�����j�� 1 �ɋK�i������Ă���D</para>
            /// </summary>
            /// <param name="A">�ŗL�l���������эs��A�i�v�Z�̉ߒ��ŏ㏑�������j</param>
            /// <param name="a_row">�s�� <paramref name="A"/> �̍s��</param>
            /// <param name="a_col">�s�� <paramref name="A"/> �̗�</param>
            /// <param name="ka">�s�� <paramref name="A"/> ��superdiagonals�̃T�C�Y</param>
            /// <param name="B">�ŗL�l���������эs��B�i�v�Z�̉ߒ��ŏ㏑�������j</param>
            /// <param name="b_row">�s�� <paramref name="B"/> �̍s��</param>
            /// <param name="b_col">�s�� <paramref name="B"/> �̗�</param>
            /// <param name="kb">�s�� <paramref name="B"/> ��superdiagonals�̃T�C�Y</param>
            /// <param name="evals">�ŗL�l</param>
            /// <param name="evecs">�ŗL�x�N�g��</param>
            /// <returns>��� 0 ���Ԃ��Ă���D</returns>
            /// <remarks>
            /// <para>�Ή�����CLAPACK�֐��iCLAPACK/BLAS/SRC/dsbgv.c�j</para>
            /// <code>
            /// int dsbgv_(char *jobz, char *uplo, integer *n,
            ///            integer *ka, integer *kb, 
            ///            doublereal *ab, integer *ldab,
            ///            doublereal *bb, integer *ldbb,
            ///            doublereal *w,
            ///            doublereal *z, integer *ldz,
            ///            doublereal *work, integer *info);
            /// </code>
            /// </remarks>
            static int dsbgv(array<doublereal>^ A, int a_row, int a_col, int ka, 
                             array<doublereal>^ B, int b_row, int b_col, int kb,
                             array<doublereal>^% evals,
                             array< array<doublereal>^ >^% evecs )
            {
                char jobz = 'V';
                // �ŗL�x�N�g����
                //   if jobvl == 'V' then �v�Z����
                //   if jobvl == 'N' then �v�Z���Ȃ�

                char uplo = 'U';
                // if uplo == 'U' then upper triangles of A and B are stored.
                // if uplo == 'L' then lower triangles of A and B are stored.

                integer n = a_col;
                // �s�� A, B �̑傫���iN�~N�Ȃ̂ŁC�Е������ł悢�j

                integer ka_ = ka;
                // the number of superdiagonals of the matrix A if uplo == 'U'
                // or the number of subdiagonals if uplo == 'L'. ka >= 0.
                
                integer kb_ = kb;
                // the number of superdiagonals of the matrix B if uplo == 'U'
                // or the number of subdiagonals if uplo == 'L'. kb >= 0.

                integer ldab = ka_ + 1;
                // the leading dimension of the array AB. ldab >= ka + 1.

                pin_ptr<doublereal> ab = &A[0];
                // [ldab, n] N�~N �̍s�� A�̏�O�p�s��i�܂��͉��O�p�s��)��эs��`���Ŋi�[
                // if UPLO = 'U', AB(ka+1+i-j,j) = A(i,j) for max(1,j-ka)<=i<=j;
                // if UPLO = 'L', AB(1+i-j,j)    = A(i,j) for j<=i<=min(n,j+ka)
                // �z�� ab �i�s�� A�j�́C�v�Z�̉ߒ��ŏ㏑�������D
                
                integer ldbb = kb_ + 1;
                // the leading dimension of the array BB. ldbb >= kb + 1.

                pin_ptr<doublereal> bb = &B[0];
                // [ldbb, n] N�~N �̍s�� B�̏�O�p�s��i�܂��͉��O�p�s��)��эs��`���Ŋi�[
                // if UPLO = 'U', BB(kb+1+i-j,j) = B(i,j) for max(1,j-kb)<=i<=j;
                // if UPLO = 'L', BB(1+i-j,j)    = B(i,j) for j<=i<=min(n,j+kb).
                // �z�� bb �i�s�� B�j�́C�v�Z�̉ߒ��ŏ㏑�������D

                doublereal* w = new doublereal[n];
                // �v�Z���ꂽ�ŗL�l������D

                integer ldz = n;
                // �K�� 1 <= ldz �𖞂����K�v������D
                // if jobz == 'V' then N <= ldz

                doublereal* z = new doublereal[ldz * n];
                // if jobz == 'V' then �ŗL�x�N�g���� z �̊e��ɁC�ŗL�l�Ɠ��������Ŋi�[�����D
                // if jobz == 'N' then vr is not referenced.

                //
                // ���̑�

                doublereal* work = new doublereal[3*n];
                // dimension (3*N)

                integer info = 0;
                // if info == 0 then ����I��
                // if info <  0 then -info �Ԗڂ̈����̒l���Ԉ���Ă���D
                // if info >  0 then QR�A���S���Y���́C�S�Ă̌ŗL�l���v�Z�ł��Ȃ������D
                //                   �ŗL�x�N�g���͌v�Z����Ă��Ȃ��D
                //                   wr[info+1:N] �� wl[info+1:N] �ɂ́C���������ŗL�l���܂܂�Ă���D

                int ret;
                try
                {
                    // CLAPACK���[�`��
                    ret = dsbgv_(&jobz, &uplo, &n, &ka_, &kb_, ab, &ldab, bb, &ldbb, w, z, &ldz, work, &info);

                    if(info == 0)
                    {
                        //
                        // �ŗL�l���i�[
                        evals = gcnew array<doublereal>(n);

                        for(int i=0; i<n; ++i)
                        {
                            evals[i] = ((Math::Abs(w[i]) < CalculationLowerLimit) ? (0.0) : (w[i]));
                        }

                        //
                        // �ŗL�x�N�g�����i�[
                        evecs = gcnew array< array<doublereal>^ >(n);

                        for(int i=0; i<n; ++i)
                        {
                            // �ʏ�̊i�[����
                            evecs[i] = gcnew array<doublereal>(ldz);

                            for(int j=0; j<ldz; ++j)
                            {
                                evecs[i][j] = z[i*ldz + j];
                            }
                        }// end for i
                    }// end if info == 0
                    else
                    {
                        if(info < 0)
                        {
                            throw gcnew IllegalClapackArgumentException(
                                "Error occurred: " + -info
                                    + "-th argument had an illegal value in the clapack.Function.dsbgv", -info);
                        }
                        else if (info > n && info <= 2*n)
                        {
                            throw gcnew IllegalClapackResultException(
                                "Error occurred: " +  "B(" + (info - n - 1) + ", " + (info - n - 1) + ") was was negative. B is not positive definite.", (info - n - 1));
                        }
                        else
                        {
                            throw gcnew IllegalClapackResultException("Error occurred: dsbgv_", info);
                        }
                    }
                }
                finally
                {
                    // unmanaged code �̌�n��
                    delete[] w; w = nullptr;
                    delete[] z; z = nullptr;
                    delete[] work; work = nullptr;
                }

                return ret;
            }

            /// <summary>
            /// <para>��ʉ��ŗL�l��� Ax=��Bx������</para>
            /// <para>�v�Z���ꂽ�ŗL�x�N�g���́C�傫���i���[�N���b�h�m�����j�� 1 �ɋK�i������Ă���D</para>
            /// </summary>
            /// <param name="A">�ŗL�l���������s��A�i�v�Z�̉ߒ��ŏ㏑�������j</param>
            /// <param name="a_row">�s�� <paramref name="A"/> �̍s��</param>
            /// <param name="a_col">�s�� <paramref name="A"/> �̗�</param>
            /// <param name="B">�ŗL�l���������s��B�i�v�Z�̉ߒ��ŏ㏑�������j</param>
            /// <param name="b_row">�s�� <paramref name="B"/> �̍s��</param>
            /// <param name="b_col">�s�� <paramref name="B"/> �̗�</param>
            /// <param name="evals">�ŗL�l</param>
            /// <param name="evecs">�ŗL�x�N�g��</param>
            /// <returns>��� 0 ���Ԃ��Ă���D</returns>
            /// <remarks>
            /// <para>�Ή�����CLAPACK�֐��iCLAPACK/BLAS/SRC/zggev.c�j</para>
            /// <code>
            /// int zggev_(char *jobvl, char *jobvr, integer *n, 
            ///            doublecomplex *a, integer *lda, doublecomplex *b, integter *ldb,
            ///            doublecomplex *alpha, doublecomplex *beta,
            ///            doublecomplex *vl, integer *ldvl, doublecomplex *vr, integer *ldvr,
            ///            doublecomplex *work, integer *lwork, doublereal *rwork, integer *info)
            /// </code>
            /// </remarks>
            static int zggev(array<Complex>^ A, int a_row, int a_col,
                             array<Complex>^ B, int b_row, int b_col,
                             array<Complex>^% evals,
                             array< array<Complex>^ >^% evecs)
            {
                char jobvl = 'N';
                // ���ŗL�x�N�g����
                //   if jobvl == 'V' then �v�Z����
                //   if jobvl == 'N' then �v�Z���Ȃ�

                char jobvr = 'V';
                // �E�ŗL�x�N�g����
                //   if jobvr == 'V' then �v�Z����
                //   if jobvr == 'N' then �v�Z���Ȃ�

                integer n = a_col;
                // �s�� A, B �̑傫���iN�~N�Ȃ̂ŁC�Е������ł悢�j
                
                integer lda = n;
                // the leading dimension of the array A. lda >= max(1, N).

                pin_ptr<void> a_ptr = &A[0];
                doublecomplex* a = (doublecomplex *)(void *)a_ptr;
                // [lda, n] N�~N �̍s�� A
                // �z�� a �i�s�� A�j�́C�v�Z�̉ߒ��ŏ㏑�������D

                integer ldb = n;
                // The leading dimension of B.  ldb >= max(1, N).

                pin_ptr<void> b_ptr = &B[0];
                doublecomplex* b = (doublecomplex *)(void *)b_ptr;
                // [ldb, n] N�~N �̍s�� B
                // �z�� b �i�s�� B�j�́C�v�Z�̉ߒ��ŏ㏑�������D
                
                doublecomplex* alpha = new doublecomplex[n];
                // �v�Z���ꂽ�ŗL�l�̕��q����������D

                doublecomplex* beta = new doublecomplex[n];
                // �v�Z���ꂽ�ŗL�l�̕��ꕔ��������D
                

                /*
                 * �����ŗL�x�N�g���͌v�Z���Ȃ�
                 */
                

                integer ldvl = 1;
                // �K�� 1 <= ldvl �𖞂����K�v������D
                // if jobvl == 'V' then N <= ldvl

                doublecomplex* vl = nullptr;
                // vl is not referenced, because jobvl == 'N'.
                
                /*
                 * ���E�ŗL�x�N�g���͌v�Z����
                 */

                integer ldvr = n;
                // �K�� 1 <= ldvr �𖞂����K�v������D
                // if jobvr == 'V' then N <= ldvr

                doublecomplex* vr = new doublecomplex[ldvr * n];
                // if jobvr == 'V' then �E�ŗL�x�N�g���� vr �̊e��ɁC�ŗL�l�Ɠ��������Ŋi�[�����D
                // if jobvr == 'N' then vr is not referenced.
                
                //
                // ���̑�

                integer lwork = 2*n;
                // max(1,2*N) <= lwork
                // �ǂ��p�t�H�[�}���X�𓾂邽�߂ɁC���̏ꍇ lwork �͑傫�����ׂ����D
                doublecomplex* work = new doublecomplex[lwork];
                // if info == 0 then work[0] returns the optimal lwork.

                doublereal* rwork = new doublereal[8 * n];
                // workspace dimension (8*N)
                
                integer info = 0;
                // if info == 0 then ����I��
                // if info <  0 then -info �Ԗڂ̈����̒l���Ԉ���Ă���D
                // if info >  0 then QR�A���S���Y���́C�S�Ă̌ŗL�l���v�Z�ł��Ȃ������D
                //                   �ŗL�x�N�g���͌v�Z����Ă��Ȃ��D
                //                   wr[info+1:N] �� wl[info+1:N] �ɂ́C���������ŗL�l���܂܂�Ă���D
                
                
                int ret;
                try
                {
                    // CLAPACK���[�`��
                    ret = zggev_(&jobvl, &jobvr, &n, a, &lda, b, &ldb, alpha, beta, vl, &ldvl, vr, &ldvr, work, &lwork, rwork, &info);

                    if(info == 0)
                    {
                        //
                        // �ŗL�l���i�[
                        evals = gcnew array<Complex>(n);
                        for (int i = 0; i < n; i++)
                        {
                            if (Math::Abs(beta[i].r) + Math::Abs(beta[i].i) < CalculationLowerLimit)
                            {
                                double realValue = ((Math::Abs(alpha[i].r) < CalculationLowerLimit) ?
                                    (Double::NaN)
                                    : ((Math::Abs(alpha[i].r) > 0) ? (Double::PositiveInfinity) : (Double::NegativeInfinity)));
                                double imagValue = ((Math::Abs(alpha[i].i) < CalculationLowerLimit) ?
                                    (Double::NaN)
                                    : ((Math::Abs(alpha[i].i) > 0) ? (Double::PositiveInfinity) : (Double::NegativeInfinity)));
                                evals[i] = Complex(realValue, imagValue);
                            }
                            else
                            {
                                //evals[i] = Complex(alpha[i].r, alpha[i].i) / Complex(beta[i].r, beta[i].i);
                                double betaSquareNorm = beta[i].r * beta[i].r + beta[i].i * beta[i].i;
                                evals[i] = Complex(
                                    (alpha[i].r * beta[i].r + alpha[i].i * beta[i].i) / betaSquareNorm,
                                    (-alpha[i].r * beta[i].i + alpha[i].i * beta[i].r) / betaSquareNorm
                                    );
                            }
                        }
                        
                        //
                        // �ŗL�x�N�g�����i�[
                        evecs = gcnew array< array<Complex>^ >(n);
                        for (int i = 0; i < n; i++)
                        {
                            // �ʏ�̊i�[����
                            evecs[i] = gcnew array<Complex>(ldvr);
                            for(int j=0; j<ldvr; ++j)
                            {
                                doublecomplex v = vr[i*ldvr + j];
                                evecs[i][j] = Complex(v.r, v.i);
                            }
                        }// end for i
                    }// end if info == 0
                    else
                    {
                        if(info < 0)
                        {
                            throw gcnew IllegalClapackArgumentException(
                                "Error occurred: " + -info
                                    + "-th argument had an illegal value in the clapack.Function.zggev", -info);
                        }
                        else
                        {
                            throw gcnew IllegalClapackResultException("Error occurred: zggev_", info);
                        }
                    }
                }
                finally
                {
                    // unmanaged code �̌�n��
                    delete[] alpha; alpha = nullptr;
                    delete[] beta; beta = nullptr;
                    delete[] vr; vr = nullptr;
                    delete[] work; work = nullptr;
                    delete[] rwork; rwork = nullptr;
                }

                return ret;
            }

            /// <summary>
            /// <para>��ʉ��ŗL�l��� Ax=��Bx������</para>
            /// <para>A�̓G���~�[�g�эs��. B�̓G���~�[�g�эs�񂩂���l�s��ł���. �v�Z���ꂽ�ŗL�x�N�g���́C�傫���i���[�N���b�h�m�����j�� 1 �ɋK�i������Ă���D</para>
            /// </summary>
            /// <param name="A">�ŗL�l���������эs��A�i�v�Z�̉ߒ��ŏ㏑�������j</param>
            /// <param name="a_row">�s�� <paramref name="A"/> �̍s��</param>
            /// <param name="a_col">�s�� <paramref name="A"/> �̗�</param>
            /// <param name="ka">�s�� <paramref name="A"/> ��superdiagonals�̃T�C�Y</param>
            /// <param name="B">�ŗL�l���������эs��B�i�v�Z�̉ߒ��ŏ㏑�������j</param>
            /// <param name="b_row">�s�� <paramref name="B"/> �̍s��</param>
            /// <param name="b_col">�s�� <paramref name="B"/> �̗�</param>
            /// <param name="kb">�s�� <paramref name="B"/> ��superdiagonals�̃T�C�Y</param>
            /// <param name="evals">�ŗL�l</param>
            /// <param name="evecs">�ŗL�x�N�g��</param>
            /// <returns>��� 0 ���Ԃ��Ă���D</returns>
            /// <remarks>
            /// <para>�Ή�����CLAPACK�֐��iCLAPACK/BLAS/SRC/dsbgv.c�j</para>
            /// <code>
            /// int zhbgv_(char *jobz, char *uplo, integer *n,
            ///            integer *ka, integer *kb, 
            ///            doublecomplex *ab, integer *ldab,
            ///            doublecomplex *bb, integer *ldbb,
            ///            doublecomplex *w,
            ///            doublecomplex *z, integer *ldz,
            ///            doublecomplex *work, doublereal *rwork, integer *info);
            /// </code>
            /// </remarks>
            static int zhbgv(array<Complex>^ A, int a_row, int a_col, int ka, 
                             array<Complex>^ B, int b_row, int b_col, int kb,
                             array<doublereal>^% evals,
                             array< array<Complex>^ >^% evecs )
            {
                char jobz = 'V';
                // �ŗL�x�N�g����
                //   if jobvl == 'V' then �v�Z����
                //   if jobvl == 'N' then �v�Z���Ȃ�

                char uplo = 'U';
                // if uplo == 'U' then upper triangles of A and B are stored.
                // if uplo == 'L' then lower triangles of A and B are stored.

                integer n = a_col;
                // �s�� A, B �̑傫���iN�~N�Ȃ̂ŁC�Е������ł悢�j

                integer ka_ = ka;
                // the number of superdiagonals of the matrix A if uplo == 'U'
                // or the number of subdiagonals if uplo == 'L'. ka >= 0.
                
                integer kb_ = kb;
                // the number of superdiagonals of the matrix B if uplo == 'U'
                // or the number of subdiagonals if uplo == 'L'. kb >= 0.

                integer ldab = ka_ + 1;
                // the leading dimension of the array AB. ldab >= ka + 1.

                pin_ptr<void> ab_ptr = &A[0];
                doublecomplex *ab = (doublecomplex *)(void *)ab_ptr;
                // [ldab, n] N�~N �̍s�� A�̏�O�p�s��i�܂��͉��O�p�s��)��эs��`���Ŋi�[
                // if UPLO = 'U', AB(ka+1+i-j,j) = A(i,j) for max(1,j-ka)<=i<=j;
                // if UPLO = 'L', AB(1+i-j,j)    = A(i,j) for j<=i<=min(n,j+ka)
                // �z�� ab �i�s�� A�j�́C�v�Z�̉ߒ��ŏ㏑�������D
                
                integer ldbb = kb_ + 1;
                // the leading dimension of the array BB. ldbb >= kb + 1.

                pin_ptr<void> bb_ptr = &B[0];
                doublecomplex *bb = (doublecomplex *)(void *)bb_ptr;
                // [ldbb, n] N�~N �̍s�� B�̏�O�p�s��i�܂��͉��O�p�s��)��эs��`���Ŋi�[
                // if UPLO = 'U', BB(kb+1+i-j,j) = B(i,j) for max(1,j-kb)<=i<=j;
                // if UPLO = 'L', BB(1+i-j,j)    = B(i,j) for j<=i<=min(n,j+kb).
                // �z�� bb �i�s�� B�j�́C�v�Z�̉ߒ��ŏ㏑�������D

                doublereal* w = new doublereal[n];
                // �v�Z���ꂽ�ŗL�l������D

                integer ldz = n;
                // �K�� 1 <= ldz �𖞂����K�v������D
                // if jobz == 'V' then N <= ldz

                doublecomplex* z = new doublecomplex[ldz * n];
                // if jobz == 'V' then �ŗL�x�N�g���� z �̊e��ɁC�ŗL�l�Ɠ��������Ŋi�[�����D
                // if jobz == 'N' then z is not referenced.

                //
                // ���̑�

                doublecomplex* work = new doublecomplex[n];
                // dimension(N)
                doublereal *rwork = new doublereal[3 * n];
                // dimension (3*N)

                integer info = 0;
                // if info == 0 then ����I��
                // if info <  0 then -info �Ԗڂ̈����̒l���Ԉ���Ă���D
                // if info >  0 then QR�A���S���Y���́C�S�Ă̌ŗL�l���v�Z�ł��Ȃ������D
                //                   �ŗL�x�N�g���͌v�Z����Ă��Ȃ��D
                //                   wr[info+1:N] �� wl[info+1:N] �ɂ́C���������ŗL�l���܂܂�Ă���D

                int ret;
                try
                {
                    // CLAPACK���[�`��
                    ret = zhbgv_(&jobz, &uplo, &n, &ka_, &kb_, ab, &ldab, bb, &ldbb, w, z, &ldz, work, rwork, &info);

                    if(info == 0)
                    {
                        //
                        // �ŗL�l���i�[
                        evals = gcnew array<doublereal>(n);

                        for(int i=0; i<n; ++i)
                        {
                            evals[i] = ((Math::Abs(w[i]) < CalculationLowerLimit) ? (0.0) : (w[i]));
                        }

                        //
                        // �ŗL�x�N�g�����i�[
                        evecs = gcnew array< array<Complex>^ >(n);

                        for(int i=0; i<n; ++i)
                        {
                            // �ʏ�̊i�[����
                            evecs[i] = gcnew array<Complex>(ldz);

                            for(int j=0; j<ldz; ++j)
                            {
                                doublecomplex v = z[i*ldz + j];
                                evecs[i][j] = Complex(v.r, v.i);
                            }
                        }// end for i
                    }// end if info == 0
                    else
                    {
                        if(info < 0)
                        {
                            throw gcnew IllegalClapackArgumentException(
                                "Error occurred: " + -info
                                    + "-th argument had an illegal value in the clapack.Function.zhbgv", -info);
                        }
                        else if (info > n && info <= 2*n)
                        {
                            throw gcnew IllegalClapackResultException(
                                "Error occurred: " +  "B(" + (info - n - 1) + ", " + (info - n - 1) + ") was was negative. B is not positive definite.", (info - n - 1));
                        }
                        else
                        {
                            throw gcnew IllegalClapackResultException("Error occurred: zhbgv_", info);
                        }
                    }
                }
                finally
                {
                    // unmanaged code �̌�n��
                    delete[] w; w = nullptr;
                    delete[] z; z = nullptr;
                    delete[] work; work = nullptr;
                    delete[] rwork; rwork = nullptr;
                }

                return ret;
            }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // System::Numerics::Complex���g�p����I/F
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public:
            /// <summary>
            /// <para>A * X = B �������i X �����j�D</para>
            /// <para>A �� n�~n �̍s��CX �� B �� n�~nrhs �̍s��ł���D</para>
            /// </summary>
            /// <param name="X"><c>A * X = B</c> �̉��ł��� X ���i�[�����i���ۂɂ� B �Ɠ����I�u�W�F�N�g���w���j</param>
            /// <param name="x_row">�s�� X �̍s�����i�[�����i<c>== <paramref name="b_row"/></c>�j</param>
            /// <param name="x_col">�s�� X �̗񐔂��i�[�����i<c>== <paramref name="b_col"/></c>�j</param>
            /// <param name="A">�W���s��iLU�����̌��ʂł��� P*L*U �ɏ�����������DP*L*U�ɂ��Ă�<see cref="dgetrf"/>���Q�Ɓj</param>
            /// <param name="a_row">�s��A�̍s��</param>
            /// <param name="a_col">�s��A�̗�</param>
            /// <param name="B">�s�� B�i������CLAPACK�֐��ɂ�� X �̒l���i�[�����j</param>
            /// <param name="b_row">�s��B�̍s��</param>
            /// <param name="b_col">�s��B�̗�</param>
            /// <returns>��� 0 ���Ԃ��Ă���D</returns>
            /// <exception cref="IllegalClapackArgumentException">
            /// ������ zgesv_�֐��ɓn���ꂽ�����ɖ�肪����� throw �����D
            /// </exception>
            /// <exception cref="IllegalClapackResultException">
            /// �s�� A �� LU�����ɂ����āCU[i, i] �� 0 �ƂȂ��Ă��܂����ꍇ�� throw �����D
            /// ���̏ꍇ�C�������߂邱�Ƃ��ł��Ȃ��D
            /// </exception>
            /// <remarks>
            /// <para>�Ή�����CLAPACK�֐��iCLAPACK/SRC/zgesv.c�j</para>
            /// <code>
            /// int zgesv_(integer *n, integer *nrhs,
            ///            doublecomplex *a, integer *lda, integer *ipiv,
            ///            doublecomplex *b, integer *ldb, integer *info)
            /// </code>
            /// <para>zgesv_ �֐��̓����ł� LU�������g�p����Ă���D</para>
            /// </remarks>
            static int zgesv(array<System::Numerics::Complex>^% X, int% x_row, int% x_col,
                             array<System::Numerics::Complex>^  A, int  a_row, int  a_col,
                             array<System::Numerics::Complex>^  B, int  b_row, int  b_col)
            {
                integer n    = a_row;    // input: �A���ꎟ�������̎����D�����s��[A]�̎���(n��0)�D 
                integer nrhs = b_col;    // input: �s��B��Column��
                
                // A��native�̕��f���\���̂ɕϊ�
                doublecomplex* a = nullptr;
                try
                {
                    a = new doublecomplex[a_row * a_col];
                    for (int i = 0; i < a_row * a_col; i++)
                    {
                        a[i].r = A[i].Real;
                        a[i].i = A[i].Imaginary;
                    }
                }
                catch (System::Exception^ /*exception*/)
                {
                    if (a != nullptr)
                    {
                        delete[] a;
                        a = nullptr;
                    }
                    throw;
                }
                // B��native�̕��f���\���̂ɕϊ�
                doublecomplex* b = nullptr;
                try
                {
                    b = new doublecomplex[b_row * b_col];
                    for (int i = 0; i < b_row * b_col; i++)
                    {
                        b[i].r = B[i].Real;
                        b[i].i = B[i].Imaginary;
                    }
                }
                catch (System::Exception^ /*exception*/)
                {
                    if (a != nullptr)
                    {
                        delete[] a;
                        a = nullptr;
                    }
                    if (b != nullptr)
                    {
                        delete[] b;
                        b = nullptr;
                    }
                    throw;
                }

                // �z��`���� a[lda�~n]
                //   input : n�sn��̌W���s��[A]
                //   output: LU������̍s��[L]�ƍs��[U]�D�������C�s��[L]�̒P�ʑΊp�v�f�͊i�[����Ȃ��D
                //           A=[P]*[L]*[U]�ł���C[P]�͍s�Ɨ�����ւ��鑀��ɑΉ�����u���s��ƌĂ΂�C0��1���i�[�����D
                
                integer lda = n;
                // input: �s��A�̑�ꎟ��(�̃������i�[��)�Dlda��max(1,n)�ł���C�ʏ�� lda==n �ŗǂ��D

                integer* ipiv = new integer[n];
                // output: �傫��n�̔z��D�u���s��[P]���`���鎲�I��p�Y�����D

                // input/output: �z��`����b[ldb�~nrhs]�D�ʏ��nrhs��1�Ȃ̂ŁC�z��`����b[ldb]�ƂȂ�D
                // input : b[ldb�~nrhs]�̔z��`���������E�Ӎs��{B}�D
                // output: info==0 �̏ꍇ�ɁCb[ldb�~nrhs]�`���̉��s��{X}���i�[�����D

                integer ldb = b_row;
                // input: �z��b�̑�ꎟ��(�̃������i�[��)�Dldb��max(1,n)�ł���C�ʏ�� ldb==n �ŗǂ��D

                integer info = 1;
                // output:
                // info==0: ����I��
                // info < 0: info==-i �Ȃ�΁Ci�Ԗڂ̈����̒l���Ԉ���Ă��邱�Ƃ������D
                // 0 < info <N-1: �ŗL�x�N�g���͌v�Z����Ă��Ȃ����Ƃ������D
                // info > N: LAPACK���Ŗ�肪���������Ƃ������D

                int ret;
                try
                {
                    ret = zgesv_(&n, &nrhs, a, &lda, ipiv, b, &ldb, &info);
                    
                    for (int i = 0; i < a_row * a_col; i++)
                    {
                        A[i] = System::Numerics::Complex(a[i].r, a[i].i);
                    }
                    for (int i = 0; i < b_row * b_col; i++)
                    {
                        B[i] = System::Numerics::Complex(b[i].r, b[i].i);
                    }

                    if(info == 0)
                    {
                        X = B;
                        x_row = b_row;
                        x_col = b_col;
                    }
                    else
                    {
                        X = nullptr;
                        x_row = 0;
                        x_col = 0;
                        
                        if(info < 0)
                        {
                            throw gcnew IllegalClapackArgumentException("Error occurred: " + -info + "-th argument had an illegal value in the clapack.Function.zgesv", -info);
                        }
                        else
                        {
                            throw gcnew IllegalClapackResultException("Error occurred: zgesv_", info);
                        }
                    }
                }
                finally
                {
                    delete[] ipiv; ipiv = nullptr;
                    delete[] a;
                    delete[] b;
                }

                return ret;
            }

            /// <summary>
            /// <para>�ŗL�l����</para>
            /// <para>�v�Z���ꂽ�ŗL�x�N�g���́C�傫���i���[�N���b�h�m�����j�� 1 �ɋK�i������Ă���D</para>
            /// </summary>
            /// <param name="X">�ŗL�l���������s��i�v�Z�̉ߒ��ŏ㏑�������j</param>
            /// <param name="x_row">�s�� <paramref name="X"/> �̍s��</param>
            /// <param name="x_col">�s�� <paramref name="X"/> �̗�</param>
            /// <param name="evals">�ŗL�l</param>
            /// <param name="evecs">�ŗL�x�N�g��</param>
            /// <returns>��� 0 ���Ԃ��Ă���D</returns>
            /// <remarks>
            /// <para>�Ή�����CLAPACK�֐��iCLAPACK/BLAS/SRC/zgeev.c�j</para>
            /// <code>
            /// int zgeev_(char *jobvl, char *jobvr, integer *n, 
            ///            doublecomplex *a, integer *lda, doublecomplex *w, doublecomplex *vl, 
            ///            integer *ldvl, doublecomplex *vr, integer *ldvr, doublecomplex *work, 
            ///            integer *lwork, doublereal *rwork, integer *info)
            /// </code>
            /// </remarks>
            static int zgeev(array<System::Numerics::Complex>^ X, int x_row, int x_col,
                             array<System::Numerics::Complex>^% evals,
                             array< array<System::Numerics::Complex>^ >^% evecs)
            {
                char jobvl = 'N';
                // ���ŗL�x�N�g����
                //   if jobvl == 'V' then �v�Z����
                //   if jobvl == 'N' then �v�Z���Ȃ�

                char jobvr = 'V';
                // �E�ŗL�x�N�g����
                //   if jobvr == 'V' then �v�Z����
                //   if jobvr == 'N' then �v�Z���Ȃ�

                integer n = x_col;
                // �s�� X �̑傫���iN�~N�Ȃ̂ŁC�Е������ł悢�j
                
                integer lda = n;
                // the leading dimension of the array A. lda >= max(1, N).

                /////pin_ptr<doublereal> a = &X[0];
                doublecomplex* a = nullptr;
                try
                {
                    a = new doublecomplex[x_row * x_col];
                    for (int i = 0; i < x_row * x_col; i++)
                    {
                        a[i].r = X[i].Real;
                        a[i].i = X[i].Imaginary;
                    }
                }
                catch (System::Exception^ /*exception*/)
                {
                    if (a != nullptr)
                    {
                        delete[] a;
                        a = nullptr;
                    }
                    throw;
                }

                // [lda, n] N�~N �̍s�� X
                // �z�� a �i�s�� X�j�́C�v�Z�̉ߒ��ŏ㏑�������D
                
                doublecomplex* w = new doublecomplex[n];
                

                /*
                 * �����ŗL�x�N�g���͌v�Z���Ȃ�
                 */
                

                integer ldvl = 1;
                // �K�� 1 <= ldvl �𖞂����K�v������D
                // if jobvl == 'V' then N <= ldvl

                doublecomplex* vl = nullptr;
                // vl is not referenced, because jobvl == 'N'.
                
                /*
                 * ���E�ŗL�x�N�g���͌v�Z����
                 */

                integer ldvr = n;
                // �K�� 1 <= ldvr �𖞂����K�v������D
                // if jobvr == 'V' then N <= ldvr

                doublecomplex* vr = new doublecomplex[ldvr * n];
                // if jobvr == 'V' then �E�ŗL�x�N�g���� vr �̊e��ɁC�ŗL�l�Ɠ��������Ŋi�[�����D
                // if jobvr == 'N' then vr is not referenced.
                
                //
                // ���̑�

                integer lwork = 4*n;
                // max(1,2*N) <= lwork
                // �ǂ��p�t�H�[�}���X�𓾂邽�߂ɁC���̏ꍇ lwork �͑傫�����ׂ����D
                doublecomplex* work = new doublecomplex[lwork];
                // if info == 0 then work[0] returns the optimal lwork.

                doublereal* rwork = new doublereal[2 * n];
                
                integer info = 0;
                // if info == 0 then ����I��
                // if info <  0 then -info �Ԗڂ̈����̒l���Ԉ���Ă���D
                // if info >  0 then QR�A���S���Y���́C�S�Ă̌ŗL�l���v�Z�ł��Ȃ������D
                //                   �ŗL�x�N�g���͌v�Z����Ă��Ȃ��D
                //                   wr[info+1:N] �� wl[info+1:N] �ɂ́C���������ŗL�l���܂܂�Ă���D
                
                
                int ret;
                try
                {
                    // CLAPACK���[�`��
                    ret = zgeev_(&jobvl, &jobvr, &n, a, &lda, w, vl, &ldvl, vr, &ldvr, work, &lwork, rwork, &info);

                    if(info == 0)
                    {
                        //
                        // �ŗL�l���i�[
                        evals = gcnew array<System::Numerics::Complex>(n);
                        for(int i=0; i<n; ++i)
                        {
                            evals[i] = System::Numerics::Complex(w[i].r, w[i].i);
                        }
                        
                        //
                        // �ŗL�x�N�g�����i�[
                        evecs = gcnew array< array<System::Numerics::Complex>^ >(n);
                        for(int i=0; i<n; ++i)
                        {
                            // �ʏ�̊i�[����
                            evecs[i] = gcnew array<System::Numerics::Complex>(ldvr);
                            for(int j=0; j<ldvr; ++j)
                            {
                                doublecomplex v = vr[i*ldvr + j];
                                evecs[i][j] = System::Numerics::Complex(v.r, v.i);
                            }
                        }// end for i
                    }// end if info == 0
                    else
                    {
                        if(info < 0)
                        {
                            throw gcnew IllegalClapackArgumentException(
                                "Error occurred: " + -info
                                    + "-th argument had an illegal value in the clapack.Function.zgeev", -info);
                        }
                        else
                        {
                            throw gcnew IllegalClapackResultException("Error occurred: zgeev_", info);
                        }
                    }
                }
                finally
                {
                    // unmanaged code �̌�n��
                    delete[] w; w = nullptr;
                    delete[] vr; vr = nullptr;
                    delete[] work; work = nullptr;
                    delete[] rwork; rwork = nullptr;
                    delete[] a; // BUGFIX delete�Y��
                }

                return ret;
            }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ValueType���g�p����I/F
        //   (�{����Complex�̂��肾�����̂ł����A���߂�Ȃ����BValueType(System::Numerics::Complex^)�ł̎󂯓n���ɂȂ��Ă��܂�)
        //
        //   ������native�̍\����doublereal�̃������m�ۂ��s�����߁A������������Ȃ��Ȃ邱�Ƃ�����
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public:
            /// <summary>
            /// <para>Complex�z������k����</para>
            /// <para>���k�O�̃T�C�Y�ƈ��k��̃T�C�Y�͓���.nullptr�ɒu������镪�������팸�ɂȂ�</para>
            /// </summary>
            /// <param name="A">[IN]�Ώ�Complex�z��A[OUT]���k���ꂽComplex�z��</param>
            static void CompressMatFor_zgesv(array<System::Numerics::Complex^>^% A)
            {
                array<System::Numerics::Complex^>^ compressed = gcnew array<System::Numerics::Complex^>(A->Length);
                int indexCounter = 0;
                int zeroCounter = 0;
                for (int i = 0; i < A->Length; i++)
                {
                    if (A[i] == nullptr || System::Numerics::Complex::Abs(*A[i]) < CalculationLowerLimit)
                    {
                        zeroCounter++;
                        if (i == A->Length - 1 && zeroCounter > 0)
                        {
                            compressed[indexCounter++] = gcnew System::Numerics::Complex();
                            compressed[indexCounter++] = gcnew System::Numerics::Complex((double)zeroCounter, 0);
                            zeroCounter = 0;
                        }
                    }
                    else
                    {
                        if (zeroCounter > 0)
                        {
                            compressed[indexCounter++] = gcnew System::Numerics::Complex();
                            compressed[indexCounter++] = gcnew System::Numerics::Complex((double)zeroCounter, 0);
                            zeroCounter = 0;
                        }
                        compressed[indexCounter++] = A[i];
                    }
                }
                if (A->Length != indexCounter)
                {
                    A = compressed;
                }
                compressed = nullptr;
            }

        private:
            /// <summary>
            /// <para>���k���ꂽComplex�z������ɖ߂�</para>
            /// <para>A(�𓀑O)�̃T�C�Y��a(�𓀌�)�̃T�C�Y�͓���.</para>
            /// </summary>
            /// <param name="A">���k���ꂽComplex�z��</param>
            /// <param name="a">�o��doublecomplex�z��(�������m�ۂ͍s���Ă�����̂Ƃ���)</param>
            static void deCompressMat(array<System::Numerics::Complex^>^ A, doublecomplex *a)
            {
                int aryIndex = 0;
                int i = 0;
                while (i < A->Length && A[i] != nullptr)
                {
                    if (System::Numerics::Complex::Abs(*A[i]) < CalculationLowerLimit)
                    {
                        // 1�ǂݔ�΂�
                        i++;
                        int zeroCnt = (int)A[i++]->Real;
                        for (int k = 0; k < zeroCnt; k++)
                        {
                            a[aryIndex].r = 0;
                            a[aryIndex].i = 0;
                            aryIndex++;
                        }
                    }
                    else
                    {
                        a[aryIndex].r = A[i]->Real;
                        a[aryIndex].i = A[i]->Imaginary;
                        i++;
                        aryIndex++;
                    }
                }
            }

        public:
            /// <summary>
            /// <para>A * X = B �������i X �����j�D</para>
            /// <para>A �� n�~n �̍s��CX �� B �� n�~nrhs �̍s��ł���D</para>
            /// </summary>
            /// <param name="X"><c>A * X = B</c> �̉��ł��� X ���i�[�����i���ۂɂ� B �Ɠ����I�u�W�F�N�g���w���j</param>
            /// <param name="x_row">�s�� X �̍s�����i�[�����i<c>== <paramref name="b_row"/></c>�j</param>
            /// <param name="x_col">�s�� X �̗񐔂��i�[�����i<c>== <paramref name="b_col"/></c>�j</param>
            /// <param name="A">�W���s��iLU�����̌��ʂł��� P*L*U �ɏ�����������DP*L*U�ɂ��Ă�<see cref="dgetrf"/>���Q�Ɓj</param>
            /// <param name="a_row">�s��A�̍s��</param>
            /// <param name="a_col">�s��A�̗�</param>
            /// <param name="B">�s�� B�i������CLAPACK�֐��ɂ�� X �̒l���i�[�����j</param>
            /// <param name="b_row">�s��B�̍s��</param>
            /// <param name="b_col">�s��B�̗�</param>
            /// <returns>��� 0 ���Ԃ��Ă���D</returns>
            /// <exception cref="IllegalClapackArgumentException">
            /// ������ zgesv_�֐��ɓn���ꂽ�����ɖ�肪����� throw �����D
            /// </exception>
            /// <exception cref="IllegalClapackResultException">
            /// �s�� A �� LU�����ɂ����āCU[i, i] �� 0 �ƂȂ��Ă��܂����ꍇ�� throw �����D
            /// ���̏ꍇ�C�������߂邱�Ƃ��ł��Ȃ��D
            /// </exception>
            /// <remarks>
            /// <para>�Ή�����CLAPACK�֐��iCLAPACK/SRC/zgesv.c�j</para>
            /// <code>
            /// int zgesv_(integer *n, integer *nrhs,
            ///            doublecomplex *a, integer *lda, integer *ipiv,
            ///            doublecomplex *b, integer *ldb, integer *info)
            /// </code>
            /// <para>zgesv_ �֐��̓����ł� LU�������g�p����Ă���D</para>
            /// </remarks>
            static int zgesv(array<System::Numerics::Complex^>^% X, int% x_row, int% x_col,
                             array<System::Numerics::Complex^>^  A, int  a_row, int  a_col,
                             array<System::Numerics::Complex^>^  B, int  b_row, int  b_col)
            {
                return zgesv(X, x_row, x_col, A, a_row, a_col, B, b_row, b_col, false);
            }
            static int zgesv(array<System::Numerics::Complex^>^% X, int% x_row, int% x_col,
                             array<System::Numerics::Complex^>^  A, int  a_row, int  a_col,
                             array<System::Numerics::Complex^>^  B, int  b_row, int  b_col,
                             bool compressFlg)
            {
                integer n    = a_row;    // input: �A���ꎟ�������̎����D�����s��[A]�̎���(n��0)�D 
                integer nrhs = b_col;    // input: �s��B��Column��
                
                // A��native�̕��f���\���̂ɕϊ�
                doublecomplex* a = nullptr;
                try
                {
                    a = new doublecomplex[a_row * a_col];
                    if (compressFlg)
                    {
                        deCompressMat(A, a);
                    }
                    else
                    {
                        for (int i = 0; i < a_row * a_col; i++)
                        {
                            a[i].r = A[i]->Real;
                            a[i].i = A[i]->Imaginary;
                        }
                    }
                }
                catch (System::Exception^ /*exception*/)
                {
                    if (a != nullptr)
                    {
                        delete[] a;
                        a = nullptr;
                    }
                    throw;
                }
                // B��native�̕��f���\���̂ɕϊ�
                doublecomplex* b = nullptr;
                try
                {
                    b = new doublecomplex[b_row * b_col];
                    for (int i = 0; i < b_row * b_col; i++)
                    {
                        b[i].r = B[i]->Real;
                        b[i].i = B[i]->Imaginary;
                    }
                }
                catch (System::Exception^ /*exception*/)
                {
                    if (a != nullptr)
                    {
                        delete[] a;
                        a = nullptr;
                    }
                    if (b != nullptr)
                    {
                        delete[] b;
                        b = nullptr;
                    }
                    throw;
                }

                // �z��`���� a[lda�~n]
                //   input : n�sn��̌W���s��[A]
                //   output: LU������̍s��[L]�ƍs��[U]�D�������C�s��[L]�̒P�ʑΊp�v�f�͊i�[����Ȃ��D
                //           A=[P]*[L]*[U]�ł���C[P]�͍s�Ɨ�����ւ��鑀��ɑΉ�����u���s��ƌĂ΂�C0��1���i�[�����D
                
                integer lda = n;
                // input: �s��A�̑�ꎟ��(�̃������i�[��)�Dlda��max(1,n)�ł���C�ʏ�� lda==n �ŗǂ��D

                integer* ipiv = new integer[n];
                // output: �傫��n�̔z��D�u���s��[P]���`���鎲�I��p�Y�����D

                // input/output: �z��`����b[ldb�~nrhs]�D�ʏ��nrhs��1�Ȃ̂ŁC�z��`����b[ldb]�ƂȂ�D
                // input : b[ldb�~nrhs]�̔z��`���������E�Ӎs��{B}�D
                // output: info==0 �̏ꍇ�ɁCb[ldb�~nrhs]�`���̉��s��{X}���i�[�����D

                integer ldb = b_row;
                // input: �z��b�̑�ꎟ��(�̃������i�[��)�Dldb��max(1,n)�ł���C�ʏ�� ldb==n �ŗǂ��D

                integer info = 1;
                // output:
                // info==0: ����I��
                // info < 0: info==-i �Ȃ�΁Ci�Ԗڂ̈����̒l���Ԉ���Ă��邱�Ƃ������D
                // 0 < info <N-1: �ŗL�x�N�g���͌v�Z����Ă��Ȃ����Ƃ������D
                // info > N: LAPACK���Ŗ�肪���������Ƃ������D

                int ret;
                try
                {
                    ret = zgesv_(&n, &nrhs, a, &lda, ipiv, b, &ldb, &info);
                    
                    for (int i = 0; i < a_row * a_col; i++)
                    {
                        System::Numerics::Complex^ c = gcnew System::Numerics::Complex(a[i].r, a[i].i);
                        A[i] = c;
                    }
                    for (int i = 0; i < b_row * b_col; i++)
                    {
                        System::Numerics::Complex^ c = gcnew System::Numerics::Complex(b[i].r, b[i].i);
                        B[i] = c;
                    }

                    if(info == 0)
                    {
                        X = B;
                        x_row = b_row;
                        x_col = b_col;
                    }
                    else
                    {
                        X = nullptr;
                        x_row = 0;
                        x_col = 0;
                        
                        if(info < 0)
                        {
                            throw gcnew IllegalClapackArgumentException("Error occurred: " + -info + "-th argument had an illegal value in the clapack.Function.zgesv", -info);
                        }
                        else
                        {
                            throw gcnew IllegalClapackResultException("Error occurred: zgesv_", info);
                        }
                    }
                }
                finally
                {
                    delete[] ipiv; ipiv = nullptr;
                    delete[] a;
                    delete[] b;
                }

                return ret;
            }

            /// <summary>
            /// <para>�ŗL�l����</para>
            /// <para>�v�Z���ꂽ�ŗL�x�N�g���́C�傫���i���[�N���b�h�m�����j�� 1 �ɋK�i������Ă���D</para>
            /// </summary>
            /// <param name="X">�ŗL�l���������s��i�v�Z�̉ߒ��ŏ㏑�������j</param>
            /// <param name="x_row">�s�� <paramref name="X"/> �̍s��</param>
            /// <param name="x_col">�s�� <paramref name="X"/> �̗�</param>
            /// <param name="evals">�ŗL�l</param>
            /// <param name="evecs">�ŗL�x�N�g��</param>
            /// <returns>��� 0 ���Ԃ��Ă���D</returns>
            /// <remarks>
            /// <para>�Ή�����CLAPACK�֐��iCLAPACK/BLAS/SRC/zgeev.c�j</para>
            /// <code>
            /// int zgeev_(char *jobvl, char *jobvr, integer *n, 
            ///            doublecomplex *a, integer *lda, doublecomplex *w, doublecomplex *vl, 
            ///            integer *ldvl, doublecomplex *vr, integer *ldvr, doublecomplex *work, 
            ///            integer *lwork, doublereal *rwork, integer *info)
            /// </code>
            /// </remarks>
            static int zgeev(array<System::Numerics::Complex^>^ X, int x_row, int x_col,
                             array<System::Numerics::Complex^>^% evals,
                             array< array<System::Numerics::Complex^>^ >^% evecs)
            {
                char jobvl = 'N';
                // ���ŗL�x�N�g����
                //   if jobvl == 'V' then �v�Z����
                //   if jobvl == 'N' then �v�Z���Ȃ�

                char jobvr = 'V';
                // �E�ŗL�x�N�g����
                //   if jobvr == 'V' then �v�Z����
                //   if jobvr == 'N' then �v�Z���Ȃ�

                integer n = x_col;
                // �s�� X �̑傫���iN�~N�Ȃ̂ŁC�Е������ł悢�j
                
                integer lda = n;
                // the leading dimension of the array A. lda >= max(1, N).

                /////pin_ptr<doublereal> a = &X[0];
                doublecomplex* a = nullptr;
                try
                {
                    a = new doublecomplex[x_row * x_col];
                    for (int i = 0; i < x_row * x_col; i++)
                    {
                        a[i].r = X[i]->Real;
                        a[i].i = X[i]->Imaginary;
                    }
                }
                catch (System::Exception^ /*exception*/)
                {
                    if (a != nullptr)
                    {
                        delete[] a;
                        a = nullptr;
                    }
                    throw;
                }

                // [lda, n] N�~N �̍s�� X
                // �z�� a �i�s�� X�j�́C�v�Z�̉ߒ��ŏ㏑�������D
                
                doublecomplex* w = new doublecomplex[n];
                

                /*
                 * �����ŗL�x�N�g���͌v�Z���Ȃ�
                 */
                

                integer ldvl = 1;
                // �K�� 1 <= ldvl �𖞂����K�v������D
                // if jobvl == 'V' then N <= ldvl

                doublecomplex* vl = nullptr;
                // vl is not referenced, because jobvl == 'N'.
                
                /*
                 * ���E�ŗL�x�N�g���͌v�Z����
                 */

                integer ldvr = n;
                // �K�� 1 <= ldvr �𖞂����K�v������D
                // if jobvr == 'V' then N <= ldvr

                doublecomplex* vr = new doublecomplex[ldvr * n];
                // if jobvr == 'V' then �E�ŗL�x�N�g���� vr �̊e��ɁC�ŗL�l�Ɠ��������Ŋi�[�����D
                // if jobvr == 'N' then vr is not referenced.
                
                //
                // ���̑�

                integer lwork = 4*n;
                // max(1,2*N) <= lwork
                // �ǂ��p�t�H�[�}���X�𓾂邽�߂ɁC���̏ꍇ lwork �͑傫�����ׂ����D
                doublecomplex* work = new doublecomplex[lwork];
                // if info == 0 then work[0] returns the optimal lwork.

                doublereal* rwork = new doublereal[2 * n];
                
                integer info = 0;
                // if info == 0 then ����I��
                // if info <  0 then -info �Ԗڂ̈����̒l���Ԉ���Ă���D
                // if info >  0 then QR�A���S���Y���́C�S�Ă̌ŗL�l���v�Z�ł��Ȃ������D
                //                   �ŗL�x�N�g���͌v�Z����Ă��Ȃ��D
                //                   wr[info+1:N] �� wl[info+1:N] �ɂ́C���������ŗL�l���܂܂�Ă���D
                
                
                int ret;
                try
                {
                    // CLAPACK���[�`��
                    ret = zgeev_(&jobvl, &jobvr, &n, a, &lda, w, vl, &ldvl, vr, &ldvr, work, &lwork, rwork, &info);

                    if(info == 0)
                    {
                        //
                        // �ŗL�l���i�[
                        evals = gcnew array<System::Numerics::Complex^>(n);
                        for(int i=0; i<n; ++i)
                        {
                            evals[i] = gcnew System::Numerics::Complex(w[i].r, w[i].i);
                        }
                        
                        //
                        // �ŗL�x�N�g�����i�[
                        evecs = gcnew array< array<System::Numerics::Complex^>^ >(n);
                        for(int i=0; i<n; ++i)
                        {
                            // �ʏ�̊i�[����
                            evecs[i] = gcnew array<System::Numerics::Complex^>(ldvr);
                            for(int j=0; j<ldvr; ++j)
                            {
                                doublecomplex v = vr[i*ldvr + j];
                                evecs[i][j] = gcnew System::Numerics::Complex(v.r, v.i);
                            }
                        }// end for i
                    }// end if info == 0
                    else
                    {
                        if(info < 0)
                        {
                            throw gcnew IllegalClapackArgumentException(
                                "Error occurred: " + -info
                                    + "-th argument had an illegal value in the clapack.Function.zgeev", -info);
                        }
                        else
                        {
                            throw gcnew IllegalClapackResultException("Error occurred: zgeev_", info);
                        }
                    }
                }
                finally
                {
                    // unmanaged code �̌�n��
                    delete[] w; w = nullptr;
                    delete[] vr; vr = nullptr;
                    delete[] work; work = nullptr;
                    delete[] rwork; rwork = nullptr;
                    delete[] a; // BUGFIX delete�Y��
                }

                return ret;
            }
        };

    }// end namespace clapack
}// end namespace KrdLab
