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
    /// �萔
    /// </summary>
    public class Constants
    {
        /// <summary>
        /// �v�Z���x����
        /// </summary>
        public static readonly double PrecisionLowerLimit = 1.0e-12;
    }

    /// <summary>
    /// �s�񑀍�֐��Q
    /// </summary> 
    public class MyMatrixUtil
    {
        public static void printMatrix(string tag, MyDoubleMatrix mat)
        {
            for (int i = 0; i < mat.RowSize; i++)
            {
                for (int j = 0; j < mat.ColumnSize; j++)
                {
                    double val = mat[i, j];
                    Console.WriteLine(tag + "(" + i + ", " + j + ")" + " = " + val);
                }
            }
        }
        public static void printMatrix(string tag, MyComplexMatrix mat)
        {
            for (int i = 0; i < mat.RowSize; i++)
            {
                for (int j = 0; j < mat.ColumnSize; j++)
                {
                    Complex val = mat[i, j];
                    Console.WriteLine(tag + "(" + i + ", " + j + ")" + " = "
                                       + "(" + val.Real + "," + val.Imaginary + ") " + Complex.Abs(val));
                }
            }
        }
        public static void printMatrixNoZero(string tag, MyComplexMatrix mat)
        {
            for (int i = 0; i < mat.RowSize; i++)
            {
                for (int j = 0; j < mat.ColumnSize; j++)
                {
                    Complex val = mat[i, j];
                    if (Complex.Abs(val) < Constants.PrecisionLowerLimit) continue;
                    Console.WriteLine(tag + "(" + i + ", " + j + ")" + " = "
                                       + "(" + val.Real + "," + val.Imaginary + ") ");
                }
            }
        }
        public static void printMatrixNoZero(string tag, MyDoubleMatrix mat)
        {
            for (int i = 0; i < mat.RowSize; i++)
            {
                for (int j = 0; j < mat.ColumnSize; j++)
                {
                    double val = mat[i, j];
                    if (Math.Abs(val) < Constants.PrecisionLowerLimit) continue;
                    Console.WriteLine(tag + "(" + i + ", " + j + ")" + " = " + val + " ");
                }
            }
        }
        public static void printMatrixNoZero(string tag, Complex[,] mat)
        {
            for (int i = 0; i < mat.GetLength(0); i++)
            {
                for (int j = 0; j < mat.GetLength(1); j++)
                {
                    Complex val = mat[i, j];
                    if (Complex.Abs(val) < Constants.PrecisionLowerLimit) continue;
                    Console.WriteLine(tag + "(" + i + ", " + j + ")" + " = "
                                       + "(" + val.Real + "," + val.Imaginary + ") ");
                }
            }
        }
        public static void printMatrixNoZero(string tag, double[,] mat)
        {
            for (int i = 0; i < mat.GetLength(0); i++)
            {
                for (int j = 0; j < mat.GetLength(1); j++)
                {
                    double val = mat[i, j];
                    if (Math.Abs(val) < Constants.PrecisionLowerLimit) continue;
                    Console.WriteLine(tag + "(" + i + ", " + j + ")" + " = " + val + " ");
                }
            }
        }
        public static void printVec(string tag, double[] vec)
        {
            for (int i = 0; i < vec.Length; i++)
            {
                Complex val = vec[i];
                Console.WriteLine(tag + "(" + i + ")" + " = " + val);
            }
        }
        public static void printVec(string tag, Complex[] vec)
        {
            for (int i = 0; i < vec.Length; i++)
            {
                Complex val = vec[i];
                Console.WriteLine(tag + "(" + i + ")" + " = "
                                   + "(" + val.Real + "," + val.Imaginary + ") " + Complex.Abs(val));
            }
        }
        /*
        public static void printVec(string tag, ValueType[] vec)
        {
            for (int i = 0; i < vec.Length; i++)
            {
                Complex val = (Complex)vec[i];
                Console.WriteLine(tag + "(" + i + ")" + " = "
                                   + "(" + val.Real + "," + val.Imaginary + ") " + Complex.Abs(val));
            }
        }
        */
        /*
        public static void compressVec(ref ValueType[] vec)
        {
            KrdLab.clapack.FunctionExt.CompressMatFor_zgesv(ref vec);
        }
         */

        public static double[] matrix_ToBuffer(MyDoubleMatrix mat, bool copyFlg = true)
        {
            /*
            double[] mat_ = new double[mat.RowSize * mat.ColumnSize];

            for (int j = 0; j < mat.ColumnSize; j++) //col
            {
                for (int i = 0; i < mat.RowSize; i++) // row
                {
                    mat_[j * mat.RowSize + i] = mat[i, j];
                }
            }
             */
            double[] mat_ = null;
            if (copyFlg)
            {
                mat_ = new double[mat.RowSize * mat.ColumnSize];
                mat._body.CopyTo(mat_, 0);
            }
            else
            {
                mat_ = mat._body;
            }
            return mat_;
        }

        public static MyDoubleMatrix matrix_FromBuffer(double[] mat_, int nRow, int nCol, bool copyFlg = true)
        {
            /*
            MyDoubleMatrix mat = new MyDoubleMatrix(nRow, nCol);
            for (int j = 0; j < mat.RowSize; j++) // col
            {
                for (int i = 0; i < mat.ColumnSize; i++)  // row
                {
                    mat[i, j] = mat_[j * mat.RowSize + i];
                }
            }
             */
            MyDoubleMatrix mat = null;
            if (copyFlg)
            {
                mat = new MyDoubleMatrix(mat_, nRow, nCol);
            }
            else
            {
                mat = new MyDoubleMatrix(nRow, nCol);
                mat._body = mat_;
            }
            return mat;
        }

        public static MyDoubleMatrix matrix_Inverse(MyDoubleMatrix matA)
        {
            System.Diagnostics.Debug.Assert(matA.RowSize == matA.ColumnSize);
            int n = matA.RowSize;
            double[] matA_ = matrix_ToBuffer(matA, true);
            double[] matB_ = new double[n * n];
            // �P�ʍs��
            for (int i = 0; i < matB_.Length; i++)
            {
                matB_[i] = 0.0;
            }
            for (int i = 0; i < n; i++)
            {
                matB_[i * n + i] = 1.0;
            }
            // [A][X] = [B]
            //  [B]�̓��e��������������̂ŁAmatX��V���ɐ��������AmatB���o�͂Ɏw�肵�Ă���
            int x_row = 0;
            int x_col = 0;
            KrdLab.clapack.Function.dgesv(ref matB_, ref x_row, ref x_col, matA_, n, n, matB_, n, n);

            MyDoubleMatrix matX = matrix_FromBuffer(matB_, x_row, x_col, false);
            return matX;
        }

        public static double[,] matrix_Inverse(double[,] matA)
        {
            MyDoubleMatrix matA_ = new MyDoubleMatrix(matA);
            matA_ = matrix_Inverse(matA_);
            return matA_.ToArray();
        }

        public static Complex[] matrix_ToBuffer(MyComplexMatrix mat, bool copyFlg = true)
        {
            /*
            Complex[] mat_ = new Complex[mat.RowSize * mat.ColumnSize];
            // row�����ɖ��߂Ă���
            for (int j = 0; j < mat.ColumnSize; j++) //col
            {
                for (int i = 0; i < mat.RowSize; i++) // row
                {
                    mat_[j * mat.RowSize + i] = mat[i, j];
                }
            }
            */

            Complex[] mat_ = null;
            if (copyFlg)
            {
                mat_ = new Complex[mat.RowSize * mat.ColumnSize];
                mat._body.CopyTo(mat_, 0);
            }
            else
            {
                mat_ = mat._body;
            }
            return mat_;
        }

        public static MyComplexMatrix matrix_FromBuffer(Complex[] mat_, int nRow, int nCol, bool copyFlg = true)
        {
            /*
            MyComplexMatrix mat = new MyComplexMatrix(nRow, nCol);
            // row�����ɖ��߂Ă���
            for (int j = 0; j < mat.ColumnSize; j++) // col
            {
                for (int i = 0; i < mat.RowSize; i++)  // row
                {
                    mat[i, j] = (Complex)mat_[j * mat.RowSize + i];
                }
            }
             */
            MyComplexMatrix mat = null;
            if (copyFlg)
            {
                mat = new MyComplexMatrix(mat_, nRow, nCol);
            }
            else
            {
                mat = new MyComplexMatrix(nRow, nCol);
                mat._body = mat_;
            }
            return mat;
        }

        public static MyComplexMatrix matrix_Inverse(MyComplexMatrix matA)
        {
            System.Diagnostics.Debug.Assert(matA.RowSize == matA.ColumnSize);
            int n = matA.RowSize;
            Complex[] matA_ = matrix_ToBuffer(matA, true);
            Complex[] matB_ = new Complex[n * n];
            // �P�ʍs��
            for (int i = 0; i < matB_.Length; i++)
            {
                matB_[i] = (Complex)0.0;
            }
            for (int i = 0; i < n; i++)
            {
                matB_[i * n + i] = (Complex)1.0;
            }
            // [A][X] = [B]
            //  [B]�̓��e��������������̂ŁAmatX��V���ɐ��������AmatB���o�͂Ɏw�肵�Ă���
            int x_row = 0;
            int x_col = 0;
            KrdLab.clapack.FunctionExt.zgesv(ref matB_, ref x_row, ref x_col, matA_, n, n, matB_, n, n);

            MyComplexMatrix matX = matrix_FromBuffer(matB_, x_row, x_col, false);
            return matX;
        }

        public static Complex[,] matrix_Inverse(Complex[,] matA)
        {
            MyComplexMatrix matA_ = new MyComplexMatrix(matA);
            matA_ = matrix_Inverse(matA_);
            return matA_.ToArray();
        }

        public static MyDoubleMatrix product(MyDoubleMatrix matA, MyDoubleMatrix matB)
        {
            System.Diagnostics.Debug.Assert(matA.ColumnSize == matB.RowSize);
            MyDoubleMatrix matX = new MyDoubleMatrix(matA.RowSize, matB.ColumnSize);
            for (int i = 0; i < matX.RowSize; i++)
            {
                for (int j = 0; j < matX.ColumnSize; j++)
                {
                    matX[i, j] = 0.0;
                    for (int k = 0; k < matA.ColumnSize; k++)
                    {
                        matX[i, j] += matA[i, k] * matB[k, j];
                    }
                }
            }
            return matX;
        }

        public static MyComplexMatrix product(MyComplexMatrix matA, MyDoubleMatrix matB)
        {
            System.Diagnostics.Debug.Assert(matA.ColumnSize == matB.RowSize);
            MyComplexMatrix matX = new MyComplexMatrix(matA.RowSize, matB.ColumnSize);
            for (int i = 0; i < matX.RowSize; i++)
            {
                for (int j = 0; j < matX.ColumnSize; j++)
                {
                    matX[i, j] = 0.0;
                    for (int k = 0; k < matA.ColumnSize; k++)
                    {
                        matX[i, j] += matA[i, k] * matB[k, j];
                    }
                }
            }
            return matX;
        }

        public static MyComplexMatrix product(MyDoubleMatrix matA, MyComplexMatrix matB)
        {
            System.Diagnostics.Debug.Assert(matA.ColumnSize == matB.RowSize);
            MyComplexMatrix matX = new MyComplexMatrix(matA.RowSize, matB.ColumnSize);
            for (int i = 0; i < matX.RowSize; i++)
            {
                for (int j = 0; j < matX.ColumnSize; j++)
                {
                    matX[i, j] = 0.0;
                    for (int k = 0; k < matA.ColumnSize; k++)
                    {
                        matX[i, j] += matA[i, k] * matB[k, j];
                    }
                }
            }
            return matX;
        }

        public static MyComplexMatrix product(MyComplexMatrix matA, MyComplexMatrix matB)
        {
            System.Diagnostics.Debug.Assert(matA.ColumnSize == matB.RowSize);
            MyComplexMatrix matX = new MyComplexMatrix(matA.RowSize, matB.ColumnSize);
            for (int i = 0; i < matX.RowSize; i++)
            {
                for (int j = 0; j < matX.ColumnSize; j++)
                {
                    matX[i, j] = 0.0;
                    for (int k = 0; k < matA.ColumnSize; k++)
                    {
                        matX[i, j] += matA[i, k] * matB[k, j];
                    }
                }
            }
            return matX;
        }

        // [X] = [A] + [B]
        public static MyDoubleMatrix plus(MyDoubleMatrix matA, MyDoubleMatrix matB)
        {
            System.Diagnostics.Debug.Assert(matA.RowSize == matB.RowSize);
            System.Diagnostics.Debug.Assert(matA.ColumnSize == matB.ColumnSize);
            MyDoubleMatrix matX = new MyDoubleMatrix(matA.RowSize, matA.ColumnSize);
            for (int i = 0; i < matA.RowSize; i++)
            {
                for (int j = 0; j < matA.ColumnSize; j++)
                {
                    matX[i, j] = matA[i, j] + matB[i, j];
                }
            }
            return matX;
        }

        // [X] = [A] - [B]
        public static MyDoubleMatrix minus(MyDoubleMatrix matA, MyDoubleMatrix matB)
        {
            System.Diagnostics.Debug.Assert(matA.RowSize == matB.RowSize);
            System.Diagnostics.Debug.Assert(matA.ColumnSize == matB.ColumnSize);
            MyDoubleMatrix matX = new MyDoubleMatrix(matA.RowSize, matA.ColumnSize);
            for (int i = 0; i < matA.RowSize; i++)
            {
                for (int j = 0; j < matA.ColumnSize; j++)
                {
                    matX[i, j] = matA[i, j] - matB[i, j];
                }
            }
            return matX;
        }

        // [X] = alpha * [A]
        public static MyDoubleMatrix product(double alpha, MyDoubleMatrix matA)
        {
            MyDoubleMatrix matX = new MyDoubleMatrix(matA.RowSize, matA.ColumnSize);
            for (int i = 0; i < matA.RowSize; i++)
            {
                for (int j = 0; j < matA.ColumnSize; j++)
                {
                    matX[i, j] = alpha * matA[i, j];
                }
            }
            return matX;
        }

        // {x} = alpha * {a}
        public static double[] product(double alpha, double[] vecA)
        {
            double[] vecX = new double[vecA.Length];
            for (int i = 0; i < vecX.Length; i++)
            {
                vecX[i] = alpha * vecA[i];
            }
            return vecX;
        }

        // {x} = ({v})*
        public static Complex[] vector_Conjugate(Complex[] vec)
        {
            Complex[] retVec = new Complex[vec.Length];
            for (int i = 0; i < retVec.Length; i++)
            {
                retVec[i] = Complex.Conjugate(vec[i]);
            }
            return retVec;
        }

        // {x} = [A]{v}
        public static Complex[] product(MyComplexMatrix matA, Complex[] vec)
        {
            System.Diagnostics.Debug.Assert(matA.ColumnSize == vec.Length);
            Complex[] retVec = new Complex[vec.Length];

            for (int i = 0; i < matA.RowSize; i++)
            {
                retVec[i] = new Complex(0.0, 0.0);
                for (int k = 0; k < matA.ColumnSize; k++)
                {
                    retVec[i] += matA[i, k] * vec[k];
                }
            }
            return retVec;
        }

        // {x} = [A]{v}
        public static Complex[] product(MyDoubleMatrix matA, Complex[] vec)
        {
            System.Diagnostics.Debug.Assert(matA.ColumnSize == vec.Length);
            Complex[] retVec = new Complex[vec.Length];

            for (int i = 0; i < matA.RowSize; i++)
            {
                retVec[i] = new Complex(0.0, 0.0);
                for (int k = 0; k < matA.ColumnSize; k++)
                {
                    retVec[i] += matA[i, k] * vec[k];
                }
            }
            return retVec;
        }

        // [X] = alpha * [A]
        public static MyComplexMatrix product(Complex alpha, MyComplexMatrix matA)
        {
            MyComplexMatrix matX = new MyComplexMatrix(matA.RowSize, matA.ColumnSize);
            for (int i = 0; i < matA.RowSize; i++)
            {
                for (int j = 0; j < matA.ColumnSize; j++)
                {
                    matX[i, j] = alpha * matA[i, j];
                }
            }
            return matX;
        }

        // {x} = alpha * {a}
        public static Complex[] product(Complex alpha, Complex[] vecA)
        {
            Complex[] vecX = new Complex[vecA.Length];
            for (int i = 0; i < vecX.Length; i++)
            {
                vecX[i] = alpha * vecA[i];
            }
            return vecX;
        }


        // [X] = [A] + [B]
        public static MyComplexMatrix plus(MyComplexMatrix matA, MyComplexMatrix matB)
        {
            System.Diagnostics.Debug.Assert(matA.RowSize == matB.RowSize);
            System.Diagnostics.Debug.Assert(matA.ColumnSize == matB.ColumnSize);
            MyComplexMatrix matX = new MyComplexMatrix(matA.RowSize, matA.ColumnSize);
            for (int i = 0; i < matA.RowSize; i++)
            {
                for (int j = 0; j < matA.ColumnSize; j++)
                {
                    matX[i, j] = matA[i, j] + matB[i, j];
                }
            }
            return matX;
        }

        // [X] = [A] - [B]
        public static MyComplexMatrix minus(MyComplexMatrix matA, MyComplexMatrix matB)
        {
            System.Diagnostics.Debug.Assert(matA.RowSize == matB.RowSize);
            System.Diagnostics.Debug.Assert(matA.ColumnSize == matB.ColumnSize);
            MyComplexMatrix matX = new MyComplexMatrix(matA.RowSize, matA.ColumnSize);
            for (int i = 0; i < matA.RowSize; i++)
            {
                for (int j = 0; j < matA.ColumnSize; j++)
                {
                    matX[i, j] = matA[i, j] - matB[i, j];
                }
            }
            return matX;
        }

        // �s��̍s�x�N�g���𔲂��o��
        public static Complex[] matrix_GetRowVec(MyComplexMatrix matA, int row)
        {
            Complex[] rowVec = new Complex[matA.ColumnSize];
            for (int j = 0; j < matA.ColumnSize; j++)
            {
                rowVec[j] = matA[row, j];
            }
            return rowVec;
        }

        public static void matrix_setRowVec(MyComplexMatrix matA, int row, Complex[] rowVec)
        {
            System.Diagnostics.Debug.Assert(matA.ColumnSize == rowVec.Length);
            for (int j = 0; j < matA.ColumnSize; j++)
            {
                matA[row, j] = rowVec[j];
            }
        }

        public static Complex[] matrix_GetRowVec(Complex[,] matA, int row)
        {
            Complex[] rowVec = new Complex[matA.GetLength(1)];
            for (int j = 0; j < matA.GetLength(1); j++)
            {
                rowVec[j] = matA[row, j];
            }
            return rowVec;
        }

        public static void matrix_setRowVec(Complex[,] matA, int row, Complex[] rowVec)
        {
            System.Diagnostics.Debug.Assert(matA.GetLength(1) == rowVec.Length);
            for (int j = 0; j < matA.GetLength(1); j++)
            {
                matA[row, j] = rowVec[j];
            }
        }


        /*
        // x = sqrt(c)
        public static Complex complex_Sqrt(Complex c)
        {
            System.Numerics.Complex work = new System.Numerics.Complex(c.Real, c.Imaginary);
            work = System.Numerics.Complex.Sqrt(work);
            return new Complex(work.Real, work.Imaginary);
        }
        */

        // [X] = [A]t
        public static MyDoubleMatrix matrix_Transpose(MyDoubleMatrix matA)
        {
            MyDoubleMatrix matX = new MyDoubleMatrix(matA.ColumnSize, matA.RowSize);
            for (int i = 0; i < matX.RowSize; i++)
            {
                for (int j = 0; j < matX.ColumnSize; j++)
                {
                    matX[i, j] = matA[j, i];
                }
            }
            return matX;
        }

        // [X] = [A]t
        public static MyComplexMatrix matrix_Transpose(MyComplexMatrix matA)
        {
            MyComplexMatrix matX = new MyComplexMatrix(matA.ColumnSize, matA.RowSize);
            for (int i = 0; i < matX.RowSize; i++)
            {
                for (int j = 0; j < matX.ColumnSize; j++)
                {
                    matX[i, j] = matA[j, i];
                }
            }
            return matX;
        }

        // [X] = ([A]*)t
        public static MyComplexMatrix matrix_ConjugateTranspose(MyComplexMatrix matA)
        {
            MyComplexMatrix matX = new MyComplexMatrix(matA.ColumnSize, matA.RowSize);
            for (int i = 0; i < matX.RowSize; i++)
            {
                for (int j = 0; j < matX.ColumnSize; j++)
                {
                    matX[i, j] = new Complex(matA[j, i].Real, -matA[j, i].Imaginary);
                }
            }
            return matX;
        }

        // x = {v1}t{v2}
        public static Complex vector_Dot(Complex[] v1, Complex[] v2)
        {
            System.Diagnostics.Debug.Assert(v1.Length == v2.Length);
            int n = v1.Length;
            Complex sum = new Complex(0.0, 0.0);
            for (int i = 0; i < n; i++)
            {
                sum += v1[i] * v2[i];
            }
            return sum;
        }

        // {x} = {a} + {b}
        public static Complex[] plus(Complex[] vecA, Complex[] vecB)
        {
            System.Diagnostics.Debug.Assert(vecA.Length == vecB.Length);
            Complex[] vecX = new Complex[vecA.Length];
            for (int i = 0; i < vecA.Length; i++)
            {
                vecX[i] = vecA[i] + vecB[i];
            }
            return vecX;
        }

        // {x} = {a} - {b}
        public static Complex[] minus(Complex[] vecA, Complex[] vecB)
        {
            System.Diagnostics.Debug.Assert(vecA.Length == vecB.Length);
            Complex[] vecX = new Complex[vecA.Length];
            for (int i = 0; i < vecA.Length; i++)
            {
                vecX[i] = vecA[i] - vecB[i];
            }
            return vecX;
        }
    }
    
}
