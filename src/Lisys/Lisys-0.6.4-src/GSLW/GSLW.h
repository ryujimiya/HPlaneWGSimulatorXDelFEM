// GSLW.h

#pragma once

using namespace System;

namespace KrdLab {
	namespace GSL {

		/// <summary>
		/// <para>GSL�̊֐���񋟂���N���X�i���ݎg�p�s�j</para>
		/// <para>DLL�ł�����΂�����ɏ�芷����D</para>
		/// </summary>
		public ref class Functions
		{
		public:
			// D = (-inf, x], return P(D)
			static double cdf_gaussian_P(double x, double sigma)
			{
				return gsl_cdf_gaussian_P(x, sigma);
			}

			// D = [x, +inf), return P(D)
			static double cdf_gaussian_Q(double x, double sigma)
			{
				return gsl_cdf_gaussian_Q(x, sigma);
			}
			
			// D = (-inf, x], return P(D)
			// ���U1�̃K�E�V�A��
			static double cdf_ugaussian_P(double x)
			{
				return gsl_cdf_ugaussian_P(x);
			}

			// D = [x, +inf), return P(D)
			// ���U1�̃K�E�V�A��
			static double cdf_ugaussian_Q(double x)
			{
				return gsl_cdf_ugaussian_Q(x);
			}

			// P = P(D), D = (-inf, x], return x
			static double cdf_gaussian_Pinv(double P, double sigma)
			{
				return gsl_cdf_gaussian_Pinv(P, sigma);
			}

			// Q = P(D), D = [x, +inf), return x
			static double cdf_gaussian_Qinv(double Q, double sigma)
			{
				return gsl_cdf_gaussian_Qinv(Q, sigma);
			}

			// P = P(D), D = (-inf, x], return x
			// ���U1�̃K�E�V�A��
			static double cdf_ugaussian_Pinv(double P)
			{
				return gsl_cdf_ugaussian_Pinv(P);
			}

			// Q = P(D), D = [x, +inf), return x
			// ���U1�̃K�E�V�A��
			static double cdf_ugaussian_Qinv(double Q)
			{
				return gsl_cdf_ugaussian_Qinv(Q);
			}


			static double cdf_chisq_P(double x, double dof)
			{
				return gsl_cdf_chisq_P(x, dof);
			}
			static double cdf_chisq_Q(double x, double dof)
			{
				return gsl_cdf_chisq_Q(x, dof);
			}

			static double cdf_fdist_P(double x, double dof1, double dof2)
			{
				return gsl_cdf_fdist_P(x, dof1, dof2);
			}
			static double cdf_fdist_Q(double x, double dof1, double dof2)
			{
				return gsl_cdf_fdist_Q(x, dof1, dof2);
			}

			static double cdf_tdist_P(double x, double dof)
			{
				return gsl_cdf_tdist_P(x, dof);
			}
			/// <summary>
			/// ���R�x<paramref name="dof" />��t���z�ɂ����āC���[<paramref name="x" />, +inf)�̐ϕ��l��Ԃ��D
			/// </summary>
			static double cdf_tdist_Q(double x, double dof)
			{
				return gsl_cdf_tdist_Q(x, dof);
			}
			static double cdf_tdist_Pinv(double p, double dof)
			{
				return gsl_cdf_tdist_Pinv(p, dof);
			}
			static double cdf_tdist_Qinv(double q, double dof)
			{
				return gsl_cdf_tdist_Qinv(q, dof);
			}


			// �񍀕��z
			static double cdf_binomial_P(unsigned int k, double p, unsigned int num)
			{
				return gsl_cdf_binomial_P(k, p, num);
			}
			static double cdf_binomial_Q(unsigned int k, double p, unsigned int num)
			{
				return gsl_cdf_binomial_Q(k, p, num);
			}

		};

	}
}
