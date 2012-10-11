﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Numerics; // Complex
//using System.Text.RegularExpressions;
using MyUtilLib.Matrix;

namespace HPlaneWGSimulatorXDelFEM
{
    /// <summary>
    /// １次三角形要素：ヘルムホルツ方程式の要素行列
    /// </summary>
    class FemMat_Tri_First
    {
        /// <summary>
        /// ヘルムホルツ方程式に対する有限要素マトリクス作成
        /// </summary>
        /// <param name="waveLength">波長</param>
        /// <param name="toSorted">ソートされた節点インデックス（ 2D節点番号→ソート済みリストインデックスのマップ）</param>
        /// <param name="element">有限要素</param>
        /// <param name="Nodes">節点リスト</param>
        /// <param name="Medias">媒質リスト</param>
        /// <param name="ForceNodeNumberH">強制境界節点ハッシュ</param>
        /// <param name="mat">マージされる全体行列</param>
        public static  void AddElementMat(double waveLength,
            Dictionary<int, int> toSorted,
            FemElement element,
            IList<FemNode> Nodes,
            MediaInfo[] Medias,
            Dictionary<int, bool> ForceNodeNumberH,
            ref MyComplexMatrix mat)
        {
            // 定数
            const double pi = Constants.pi;
            const double c0 = Constants.c0;
            // 波数
            double k0 = 2.0 * pi / waveLength;
            // 角周波数
            double omega = k0 * c0;

            // 要素頂点数
            //const int vertexCnt = Constants.TriVertexCnt; //3;
            // 要素内節点数
            const int nno = Constants.TriNodeCnt_FirstOrder; //3;  // 1次三角形要素
            // 座標次元数
            const int ndim = Constants.CoordDim2D; //2;

            int[] nodeNumbers = element.NodeNumbers;
            int[] no_c = new int[nno];
            MediaInfo media = Medias[element.MediaIndex];  // ver1.1.0.0 媒質情報の取得
            double[,] media_P = media.P;
            media_P = MyMatrixUtil.matrix_Inverse(media_P);
            double[,] media_Q = media.Q;
            // 節点座標(IFの都合上配列の配列形式の2次元配列を作成)
            double[][] pp = new double[nno][];
            for (int ino = 0; ino < nno; ino++)
            {
                int nodeNumber = nodeNumbers[ino];
                int nodeIndex = nodeNumber - 1;
                FemNode node = Nodes[nodeIndex];

                no_c[ino] = nodeNumber;
                pp[ino] = new double[ndim];
                for (int n = 0; n < ndim; n++)
                {
                    pp[ino][n] = node.Coord[n];
                }
            }
            // 面積を求める
            double area = KerEMatTri.TriArea(pp[0], pp[1], pp[2]);
            //Console.WriteLine("Elem No {0} area:  {1}", element.No, area);
            System.Diagnostics.Debug.Assert(area >= 0.0);

            // 面積座標の微分を求める
            //   dldx[k, n] k面積座標Lkのn方向微分
            double[,] dldx = null;
            double[] const_term = null;
            KerEMatTri.TriDlDx(out dldx, out const_term, pp[0], pp[1], pp[2]);

            // ∫dN/dndN/dn dxdy
            //     integralDNDX[n, ino, jno]  n = 0 --> ∫dN/dxdN/dx dxdy
            //                                n = 1 --> ∫dN/dydN/dy dxdy
            double[, ,] integralDNDX = new double[ndim, nno, nno];
            for (int ino = 0; ino < nno; ino++)
            {
                for (int jno = 0; jno < nno; jno++)
                {
                    integralDNDX[0, ino, jno] = area * dldx[ino, 0] * dldx[jno, 0];
                    integralDNDX[1, ino, jno] = area * dldx[ino, 1] * dldx[jno, 1];
                }
            }
            // ∫N N dxdy
            double[,] integralN = new double[nno, nno]
                {
                    { area / 6.0 , area / 12.0, area / 12.0 },
                    { area / 12.0, area /  6.0, area / 12.0 },
                    { area / 12.0, area / 12.0, area /  6.0 },
                };

            // 要素剛性行列を作る
            Complex[,] emat = new Complex[nno, nno];
            for (int ino = 0; ino < nno; ino++)
            {
                for (int jno = 0; jno < nno; jno++)
                {
                    emat[ino, jno] = media_P[0, 0] * integralDNDX[1, ino, jno] + media_P[1, 1] * integralDNDX[0, ino, jno]
                                         - k0 * k0 * media_Q[2, 2] * integralN[ino, jno];
                }
            }

            // 要素剛性行列にマージする
            for (int ino = 0; ino < nno; ino++)
            {
                int iNodeNumber = no_c[ino];
                if (ForceNodeNumberH.ContainsKey(iNodeNumber)) continue;
                int inoGlobal = toSorted[iNodeNumber];
                for (int jno = 0; jno < nno; jno++)
                {
                    int jNodeNumber = no_c[jno];
                    if (ForceNodeNumberH.ContainsKey(jNodeNumber)) continue;
                    int jnoGlobal = toSorted[jNodeNumber];

                    //mat[inoGlobal, jnoGlobal] += emat[ino, jno];
                    if (mat._body[inoGlobal + jnoGlobal * mat.RowSize] == null)
                    {
                        mat._body[inoGlobal + jnoGlobal * mat.RowSize] = emat[ino, jno];
                    }
                    else
                    {
                        mat._body[inoGlobal + jnoGlobal * mat.RowSize] = (Complex)mat._body[inoGlobal + jnoGlobal * mat.RowSize] + emat[ino, jno];
                    }
                }
            }
        }
    }
}
