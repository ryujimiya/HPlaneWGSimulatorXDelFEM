using System;
using System.Collections.Generic;
using System.Text;

namespace KrdLab.Lisys.Testing
{
    /// <summary>
    /// �����U���̌���iF����j
    /// </summary>
    public class F
    {
        /// <summary>
        /// �����U���̌�����s���D
        /// </summary>
        /// <param name="set1">����ΏۂƂȂ鐔�l�Q</param>
        /// <param name="set2">����ΏۂƂȂ鐔�l�Q</param>
        /// <param name="level">�L�Ӑ���</param>
        /// <param name="p">p�l���i�[�����iout�j</param>
        /// <param name="f">���蓝�v�ʁi��Βl�j���i�[�����iout�j</param>
        /// <returns>true�̏ꍇ�́u�L�Ӎ�����v�Cfalse�̏ꍇ�́u�L�Ӎ������v���Ӗ�����</returns>
        public static bool Test(IVector set1, IVector set2, double level, out double p, out double f)
        {
            int size1 = set1.Size;
            int size2 = set2.Size;
            if (size1 < 2 || size2 < 2)
            {
                throw new Exception.IllegalArgumentException();
            }

            // ���v��
            double u1 = set1.Variance;
            double u2 = set2.Variance;

            int dof1, dof2;

            if (u1 > u2)
            {
                f = u1 / u2;
                dof1 = size1 - 1;
                dof2 = size2 - 1;
            }
            else
            {
                f = u2 / u1;
                dof1 = size2 - 1;
                dof2 = size1 - 1;
            }

            p = GSL.Functions.cdf_fdist_Q(f, dof1, dof2);

            return p <= level;
        }
    }
}
