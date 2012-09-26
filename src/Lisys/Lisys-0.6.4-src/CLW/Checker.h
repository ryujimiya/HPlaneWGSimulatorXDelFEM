#pragma once
/*
 * @author KrdLab
 */

namespace KrdLab {
	namespace clapack {

		/// <summary>
		/// �v�Z��K�v�ɂȂ�`�F�b�N���[�`�����`����D
		/// </summary>
		public ref class CalculationChecker
		{
		public:
			/// <summary>
			/// ���x�������l��艺�ł��邩�ǂ����𒲂ׂ�D
			/// </summary>
			/// <param name="value">���ׂ����l</param>
			/// <returns>�����l�������ꍇ��true�C���̑���false��Ԃ��D</returns>
			static bool IsLessThanLimit(double value)
			{
				if(value < Function::CalculationLowerLimit)
				{
					return true;
				}
				return false;
			}
		};
	}
}
