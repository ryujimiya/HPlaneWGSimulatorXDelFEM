#pragma once

using namespace System;

namespace KrdLab {
	namespace clapack {
		namespace exception {

			/// <summary>
			/// clapack Function �̗�O��{�N���X
			/// </summary>
			public ref class ClwException : public Exception
			{
			public:
				ClwException()
				{}
				ClwException(String^ message)
					: Exception(message)
				{}
				ClwException(String^ message, Exception^ inner)
					: Exception(message, inner)
				{}
			};

			/// <summary>
			/// CLAPACK�̌v�Z���ʂ������ȏꍇ��throw�����D
			/// </summary>
			public ref class IllegalClapackResultException : public ClwException
			{
			private:
				int	_info;

			public:

				/// <summary>
				/// �G���[�̏�Ԃ�\�����l���擾����D
				/// </summary>
				property int Info
				{
					int get()
					{
						return this->_info;
					}
				}
				
				IllegalClapackResultException(int info)
				{
					this->_info = info;
				}

				IllegalClapackResultException(String^ message, int info)
					: ClwException(message)
				{
					this->_info = info;
				}

				IllegalClapackResultException(String^ message, int info, Exception^ inner)
					: ClwException(message, inner)
				{
					this->_info = info;
				}
			};

			/// <summary>
			/// CLAPACK�ɓn���ꂽ�����ɖ�肪����ꍇ�� throw �����D
			/// </summary>
			public ref class IllegalClapackArgumentException : public ClwException
			{
			private:
				int _index;

			public:
				/// <summary>
				/// ���̂�������̈ʒu���擾����D
				/// </summary>
				property int Index
				{
					int get()
					{
						return this->_index;
					}
				}

				IllegalClapackArgumentException(int index)
				{
					this->_index = index;
				}

				IllegalClapackArgumentException(String^ message, int index)
					: ClwException(message)
				{
					this->_index = index;
				}

				IllegalClapackArgumentException(String^ message, int index, Exception^ inner)
					: ClwException(message, inner)
				{
					this->_index = index;
				}
			};

			/// <summary>
			/// �����ΏۂƂȂ�s���x�N�g���̃T�C�Y����v���Ă��Ȃ��ꍇ��throw�����D
			/// </summary>
			public ref class MismatchSizeException : public ClwException
			{
			public:
				MismatchSizeException()
				{}

				MismatchSizeException(String^ message)
					: ClwException(message)
				{}

				MismatchSizeException(String^ message, Exception^ inner)
					: ClwException(message, inner)
				{}
			};

		}// end namespace exception
	}// end namespace clapack
}// end namespace KrdLab
