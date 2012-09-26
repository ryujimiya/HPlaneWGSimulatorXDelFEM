using System;
using System.Collections.Generic;
using System.Text;

namespace KrdLab.Lisys.Testing
{
    /// <summary>
    /// ����̎��
    /// </summary>
    public enum Method
    {
        /// <summary>
        /// �����U�������肷��
        /// </summary>
        AssumedEqualityOfVariances,

        /// <summary>
        /// �����U�������肵�Ȃ�
        /// </summary>
        NotAssumedEqualityOfVariances,

        /// <summary>
        /// �l�ɑΉ��֌W������i�g�p�s�C<see cref="NotSupportedException"/>��throw�����j
        /// </summary>
        PairedValues
    }

    /// <summary>
    /// ���ϒl�̍��̌���
    /// </summary>
    public class T
    {
        /// <summary>
        /// �ꕽ�ςɑ΂��镽�ϒl�̌�����s���D
        /// </summary>
        /// <param name="set">����ΏۂƂȂ鐔�l�Q</param>
        /// <param name="population">�ꕽ��</param>
        /// <param name="level">�L�Ӑ���</param>
        /// <param name="p">p�l���i�[�����iout�j</param>
        /// <param name="t">t�l���i�[�����iout�j</param>
        /// <returns>true�̏ꍇ�́u�L�Ӎ�����v�Cfalse�̏ꍇ�́u�L�Ӎ������v���Ӗ�����</returns>
        public static bool Test(IVector set, double population, double level, out double p, out double t)
        {
            VectorChecker.ZeroSize(set);

            t = (set.Average - population) / Math.Sqrt(set.Variance);
            p = GSL.Functions.cdf_tdist_Q(Math.Abs(t), set.Size - 1);
            return p <= level;
        }

        /// <summary>
        /// 2�Q�ɑ΂��镽�ϒl�̍��̌�����s���D
        /// </summary>
        /// <param name="set1">����ΏۂƂȂ鐔�l�Q</param>
        /// <param name="set2">����ΏۂƂȂ鐔�l�Q</param>
        /// <param name="type">t����̎��</param>
        /// <param name="level">�L�Ӑ���</param>
        /// <param name="p">p�l���i�[�����iout�j</param>
        /// <param name="t">���蓝�v�ʁi��Βl�j���i�[�����iout�j</param>
        /// <returns>true�̏ꍇ�́u�L�Ӎ�����v�Cfalse�̏ꍇ�́u�L�Ӎ������v���Ӗ�����</returns>
        public static bool Test(IVector set1, IVector set2, Method type, double level, out double p, out double t)
        {
            p = 1;  // p��t�́C�K���v�Z�l�����蓖�Ă���
            t = 0;
            switch (type)
            {
                case Method.AssumedEqualityOfVariances:
                    Default(set1, set2, out p, out t);
                    break;
                case Method.NotAssumedEqualityOfVariances:
                    Welch(set1, set2, out p, out t);
                    break;
                case Method.PairedValues:
                    Paired(set1, set2, out p, out t);
                    break;
            }
            return p <= level;
        }

        #region �������\�b�h

        private static void Default(IVector set1, IVector set2, out double p, out double t)
        {
            int size1 = set1.Size;
            int size2 = set2.Size;

            int phi_e = size1 + size2 - 2;  // ���R�x
            if (phi_e < 1)
            {
                throw new Exception.IllegalArgumentException();
            }

            // 2�̌Q�𕹂������U�̐���l
            double ue = (set1.Scatter + set2.Scatter) / phi_e;

            // ���v��
            t = Math.Abs(set1.Average - set2.Average) / Math.Sqrt(ue * (1.0 / size1 + 1.0 / size2));

            // p�l
            p = GSL.Functions.cdf_tdist_Q(t, phi_e);
        }

        private static void Welch(IVector set1, IVector set2, out double p, out double t)
        {
            int size1 = set1.Size;
            int size2 = set2.Size;

            if (size1 < 2 || size2 < 2)
            {
                throw new Exception.IllegalArgumentException();
            }

            // �s�Ε��U�l
            double u1 = set1.Variance;
            double u2 = set2.Variance;

            double u = u1 / size1 + u2 / size2;

            // ���v��
            t = Math.Abs(set1.Average - set2.Average) / Math.Sqrt(u);

            // ���R�x
            double phi_e = (u * u)
                            / (u1 * u1 / (size1 * size1 * (size1 - 1)) + u2 * u2 / (size2 * size2 * (size2 - 1)));

            // p�l
            p = GSL.Functions.cdf_tdist_Q(t, phi_e);
        }

        // ���������̌������K�v
        private static void Paired(IVector set1, IVector set2, out double p, out double t)
        {
            throw new NotSupportedException();

            //VectorChecker.SizeEquals(set1, set2);
            //VectorChecker.IsNotZeroSize(set1);

            //int n = set1.Size;
            //IVector d = new Vector(n);
            //for (int i = 0; i < n; ++i)
            //{
            //    d[i] = set1[i] - set2[i];
            //}

            //// ���v��
            //t = Math.Abs(d.Average) / Math.Sqrt(d.Scatter / n * (n - 1));

            //// p�l
            //p = GSL.Functions.cdf_tdist_Q(t, n - 1);
        }

        #endregion
    }
}
