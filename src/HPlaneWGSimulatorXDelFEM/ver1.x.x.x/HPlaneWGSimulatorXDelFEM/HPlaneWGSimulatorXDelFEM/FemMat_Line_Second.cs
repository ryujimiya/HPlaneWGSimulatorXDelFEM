﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
//using System.Numerics; // Complex
//using System.Text.RegularExpressions;
using MyUtilLib.Matrix;

namespace HPlaneWGSimulatorXDelFEM
{
    /// <summary>
    /// ２次線要素
    /// </summary>
    class FemMat_Line_Second
    {
        /// <summary>
        /// ２次線要素を作成する（固有値問題用）
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="EdgeToElementNoH"></param>
        /// <param name="Elements"></param>
        /// <param name="elements"></param>
        public static void MkElements(
            IList<int> nodes,
            Dictionary<string, IList<int>> EdgeToElementNoH,
            IList<FemElement> Elements,
            ref IList<FemLineElement> elements)
        {
            // 要素内節点数
            const int nno = Constants.LineNodeCnt_SecondOrder; //3; // 2次線要素

            // 要素リスト作成
            int elemCnt = (nodes.Count - 1) / 2; // 2次線要素
            for (int elemIndex = 0; elemIndex < elemCnt; elemIndex++)
            {
                // 線要素の要素内節点
                // 節点番号はポート上の1D節点番号(1起点の番号)
                //  1   3   2
                //  +---+---+   並びに注意:頂点→内部の点
                FemLineElement element = new FemLineElement();
                element.No = elemIndex + 1;
                element.NodeNumbers = new int[nno];
                element.NodeNumbers[0] = 2 * elemIndex + 1;
                element.NodeNumbers[1] = 2 * elemIndex + 2 + 1;
                element.NodeNumbers[2] = 2 * elemIndex + 1 + 1;
                element.MediaIndex = 0;
                elements.Add(element);
            }

            // 要素を１辺とする2D領域の要素番号を取得
            for (int elemIndex = 0; elemIndex < elements.Count; elemIndex++)
            {
                FemElement element = elements[elemIndex];
                int[] nodeNumbers = element.NodeNumbers;

                // 1辺だけ調べればよい(1-3をチェック)
                int stNodeNumber = nodes[nodeNumbers[0] - 1];
                int edNodeNumber = nodes[nodeNumbers[2] - 1];
                string edgeKey = "";
                if (stNodeNumber < edNodeNumber)
                {
                    edgeKey = string.Format("{0}_{1}", stNodeNumber, edNodeNumber);
                }
                else
                {
                    edgeKey = string.Format("{0}_{1}", edNodeNumber, stNodeNumber);
                }
                if (!EdgeToElementNoH.ContainsKey(edgeKey))
                {
                    System.Diagnostics.Debug.WriteLine("logical error: Not find edge {0}", edgeKey);
                }
                else
                {
                    // 隣接する2Dの要素を1つ取得する
                    int elemNo2d = EdgeToElementNoH[edgeKey][0];
                    FemElement element2d = Elements[elemNo2d - 1];

                    // 媒質インデックスをセット
                    element.MediaIndex = element2d.MediaIndex;
                }
            }
        }

        /// <summary>
        /// 1Dヘルムホルツ方程式固有値問題の要素行列を加算する
        /// </summary>
        /// <param name="waveLength">波長(E面の場合のみ使用する)</param>
        /// <param name="element">線要素</param>
        /// <param name="coords">座標リスト</param>
        /// <param name="toSorted">節点番号→ソート済み節点インデックスマップ</param>
        /// <param name="Medias">媒質情報リスト</param>
        /// <param name="WGStructureDv">導波路構造区分</param>
        /// <param name="WaveModeDv">計算する波のモード区分</param>
        /// <param name="waveguideWidthForEPlane">導波路幅(E面解析用)</param>
        /// <param name="txx_1d">txx行列</param>
        /// <param name="ryy_1d">ryy行列</param>
        /// <param name="uzz_1d">uzz行列</param>
        public static  void AddElementMatOf1dEigenValueProblem(
            double waveLength,
            FemLineElement element,
            IList<double> coords,
            Dictionary<int, int> toSorted,
            MediaInfo[] Medias,
            FemSolver.WGStructureDV WGStructureDv,
            FemSolver.WaveModeDV WaveModeDv,
            double waveguideWidthForEPlane,
            ref MyDoubleMatrix txx_1d, ref MyDoubleMatrix ryy_1d, ref MyDoubleMatrix uzz_1d)
        {
            // 定数
            const double pi = Constants.pi;
            const double c0 = Constants.c0;
            // 波数
            double k0 = 2.0 * pi / waveLength;
            // 角周波数
            double omega = k0 * c0;

            // ２次線要素
            const int nno = Constants.LineNodeCnt_SecondOrder; // 3;

            int[] nodeNumbers = element.NodeNumbers;
            System.Diagnostics.Debug.Assert(nno == nodeNumbers.Length);

            // 座標の取得
            double[] elementCoords = new double[nno];
            for (int n = 0; n < nno; n++)
            {
                int nodeIndex = nodeNumbers[n] - 1;
                elementCoords[n] = coords[nodeIndex];
            }
            // 線要素の長さ
            double elen = Math.Abs(elementCoords[1] - elementCoords[0]);
            // 媒質インデックス
            int mediaIndex = element.MediaIndex;
            // 媒質
            MediaInfo media = Medias[mediaIndex];
            double[,] media_P = null;
            double[,] media_Q = null;
            // ヘルムホルツ方程式のパラメータP,Qを取得する
            FemSolver.GetHelmholtzMediaPQ(
                k0,
                media,
                WGStructureDv,
                WaveModeDv,
                waveguideWidthForEPlane,
                out media_P,
                out media_Q);

            double[,] integralN = new double[nno, nno]
                {
                    {  4.0 / 30.0 * elen, -1.0 / 30.0 * elen,  2.0 / 30.0 * elen },
                    { -1.0 / 30.0 * elen,  4.0 / 30.0 * elen,  2.0 / 30.0 * elen },
                    {  2.0 / 30.0 * elen,  2.0 / 30.0 * elen, 16.0 / 30.0 * elen },
                };
            double[,] integralDNDY = new double[nno, nno]
                {
                    {  7.0 / (3.0 * elen),  1.0 / (3.0 * elen), -8.0 / (3.0 * elen) },
                    {  1.0 / (3.0 * elen),  7.0 / (3.0 * elen), -8.0 / (3.0 * elen) },
                    { -8.0 / (3.0 * elen), -8.0 / (3.0 * elen), 16.0 / (3.0 * elen) },
                };

            for (int ino = 0; ino < nno; ino++)
            {
                int inoBoundary = nodeNumbers[ino];
                int inoSorted;
                if (!toSorted.ContainsKey(inoBoundary)) continue;
                inoSorted = toSorted[inoBoundary];
                for (int jno = 0; jno < nno; jno++)
                {
                    int jnoBoundary = nodeNumbers[jno];
                    int jnoSorted;
                    if (!toSorted.ContainsKey(jnoBoundary)) continue;
                    jnoSorted = toSorted[jnoBoundary];
                    // 対称バンド行列対応
                    if (ryy_1d is MyDoubleSymmetricBandMatrix && jnoSorted < inoSorted)
                    {
                        continue;
                    }

                    double e_txx_1d_inojno = media_P[0, 0] * integralDNDY[ino, jno];
                    double e_ryy_1d_inojno = media_P[1, 1] * integralN[ino, jno];
                    double e_uzz_1d_inojno = media_Q[2, 2] * integralN[ino, jno];
                    //txx_1d[inoSorted, jnoSorted] += e_txx_1d_inojno;
                    //ryy_1d[inoSorted, jnoSorted] += e_ryy_1d_inojno;
                    //uzz_1d[inoSorted, jnoSorted] += e_uzz_1d_inojno;
                    txx_1d._body[txx_1d.GetBufferIndex(inoSorted, jnoSorted)] += e_txx_1d_inojno;
                    ryy_1d._body[ryy_1d.GetBufferIndex(inoSorted, jnoSorted)] += e_ryy_1d_inojno;
                    uzz_1d._body[uzz_1d.GetBufferIndex(inoSorted, jnoSorted)] += e_uzz_1d_inojno;
                }
            }
        }
    }
}
