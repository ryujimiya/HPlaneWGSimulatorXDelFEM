#include "stdafx.h"

/*
 * �C���N���[�h�t�@�C���Q
 * CLAPACK�̃C���N���[�h�͎��s���낵�Ă��邽�߁C������Ɖ�����...
 * 
 * @author KrdLab
 *
 */

// CLAPACK�́CC�̃��C�u����
extern "C" {
#include "f2c.h"
#include "fblaswr.h"
#include "clapack.h"

#include "f2cCanceller.h"
};

#include "Exceptions.h"
#include "CLW.h"
#include "Checker.h"
