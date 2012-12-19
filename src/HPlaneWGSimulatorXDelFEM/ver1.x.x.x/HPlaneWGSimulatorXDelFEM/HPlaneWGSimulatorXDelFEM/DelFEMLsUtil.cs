using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DelFEM4NetCad;
using DelFEM4NetCom;
using DelFEM4NetCad.View;
using DelFEM4NetCom.View;
using DelFEM4NetFem;
using DelFEM4NetFem.Field;
using DelFEM4NetFem.Field.View;
using DelFEM4NetFem.Eqn;
using DelFEM4NetFem.Ls;
using DelFEM4NetMsh;
using DelFEM4NetMsh.View;
using DelFEM4NetMatVec;
using DelFEM4NetLsSol;

namespace HPlaneWGSimulatorXDelFEM
{
    /// <summary>
    /// DelFEMのリニアシステムユーティリティ
    /// </summary>
    class DelFEMLsUtil
    {
        /// <summary>
        /// フィルインレベル（既定値)
        /// </summary>
        //private const int DefFillInLevel = 0;
        private const int DefFillInLevel = 1;

        /// <summary>
        /// リニアシステム初期化（マージ開始までの処理)
        /// </summary>
        /// <param name="matPattern">行列の非０要素パターン</param>
        /// <param name="Ls"></param>
        /// <param name="Prec"></param>
        /// <param name="World"></param>
        /// <param name="FieldValId"></param>
        public static void SetupLinearSystem(
            bool[,] matPattern,
            out CFieldWorld World, out uint FieldValId,
            out CZLinearSystem Ls, out CZPreconditioner_ILU Prec, out int[] tmpBuffer)
        {
            World = new CFieldWorld();
            Ls = new CZLinearSystem();
            Prec = new CZPreconditioner_ILU();
            tmpBuffer = null;

            int matLen = matPattern.GetLength(0);

            //------------------------------------------------------------------
            // ワールド座標系を生成
            //------------------------------------------------------------------
            uint baseId = 0;
            setupWorld((uint)matLen, ref World, out baseId);

            // 界の値を扱うバッファ？を生成する。
            // フィールド値IDが返却される。
            //    要素の次元: 2次元-->ポイントにしたので0次元？ 界: 複素数スカラー 微分タイプ: 値 要素セグメント: 角節点
            FieldValId = World.MakeField_FieldElemDim(baseId, 0,
                FIELD_TYPE.ZSCALAR, FIELD_DERIVATION_TYPE.VALUE, ELSEG_TYPE.CORNER);

            //------------------------------------------------------------------
            // 界パターン追加
            //------------------------------------------------------------------
            Ls.AddPattern_Field(FieldValId, World);
            //  POINTのフィールドなので対角要素のパターンのみ追加される。非対角要素のパターンは追加されない

            // 非対角要素のパターンの追加
            {
                uint node_cnt = (uint)matLen;
                // 要素剛性行列(コーナ-コーナー)
                CZMatDia_BlkCrs_Ptr mat_cc = Ls.GetMatrixPtr(FieldValId, ELSEG_TYPE.CORNER, World);
                // 要素残差ベクトル(コーナー)
                CZVector_Blk_Ptr res_c = Ls.GetResidualPtr(FieldValId, ELSEG_TYPE.CORNER, World);

                // パターンを追加
                using (CIndexedArray crs = new CIndexedArray())
                {
                    crs.InitializeSize(mat_cc.NBlkMatCol());

                    //crs.Fill(mat_cc.NBlkMatCol(), mat_cc.NBlkMatRow());
                    using (UIntVectorIndexer index = crs.index)
                    using (UIntVectorIndexer ary = crs.array)
                    {
                        //BUGFIX
                        //for (int iblk = 0; iblk < (int)crs.Size(); iblk++)
                        // indexのサイズは crs.Size() + 1 (crs.Size() > 0のとき)
                        for (int iblk = 0; iblk < index.Count; iblk++)
                        {
                            index[iblk] = 0;
                        }
                        for (int iblk = 0; iblk < mat_cc.NBlkMatCol(); iblk++)
                        {
                            // 現在のマトリクスのインデックス設定をコピーする
                            uint npsup = 0;
                            ConstUIntArrayIndexer cur_rows = mat_cc.GetPtrIndPSuP((uint)iblk, out npsup);
                            foreach (uint row_index in cur_rows)
                            {
                                ary.Add(row_index);
                            }
                            index[iblk + 1] = (uint)ary.Count;
                            //Console.WriteLine("chk:{0} {1}", iblk, npsup);
                            // Note: インデックス設定はされていないので常にnpsup == 0 (cur_rows.Count == 0)

                            // すべての節点に関して列を追加
                            int ino_global = iblk;
                            int col = iblk;
                            if (col != -1 && ino_global != -1)
                            {
                                // 関連付けられていない節点をその行の列データの最後に追加
                                //int last_index = (int)index[col + 1] - 1;
                                //System.Diagnostics.Debug.Assert(last_index == ary.Count - 1);
                                int add_cnt = 0;
                                for (int jno_global = 0; jno_global < node_cnt; jno_global++)
                                {
                                    if (!matPattern[ino_global, jno_global])
                                    {
                                        continue;
                                    }
                                    uint row = (uint)jno_global;
                                    //if (ino_global != jno_global)  // 対角要素は除く
                                    if (col != row)  // 対角要素は除く
                                    {
                                        if (!cur_rows.Contains(row))
                                        {
                                            //ary.Insert(last_index + 1 + add_cnt, row);
                                            ary.Add(row);
                                            add_cnt++;
                                            //System.Diagnostics.Debug.WriteLine("added:" + col + " " + row);
                                        }
                                    }
                                }
                                if (add_cnt > 0)
                                {
                                    index[col + 1] = (uint)ary.Count;
                                }
                            }
                        }
                    }
                    crs.Sort();
                    System.Diagnostics.Debug.Assert(crs.CheckValid());
                    // パターンを削除する
                    mat_cc.DeletePattern();
                    // パターンを追加する
                    mat_cc.AddPattern(crs);
                }
            }

            //------------------------------------------------------------------
            // プリコンディショナ―
            //------------------------------------------------------------------
            // set Preconditioner
            //Prec.SetFillInLevel(1);
            // フィルインなしで不完全LU分解した方が収束が早く、また解もそれなりの結果だったので０に設定した
            //Prec.SetFillInLevel(0);
            // フィルインなしでは、収束しない場合がある。フィルインレベルを最大にする
            //Prec.SetFillInLevel((uint)mat.RowSize);
            //   完全LU分解（だと思われる) Prec.SetLinearSystemの処理が遅い
            //Prec.SetFillInLevel(3);
            //   誘電体スラブ導波路だと最低このレベル。これでも収束しない場合がある
            //以上から
            //メインの導波管の計算時間を短くしたいのでフィルインなしにする
            //Prec.SetFillInLevel(0);
            //    導波管だとこれで十分
            // フィルインレベルの設定(既定値)
            Prec.SetFillInLevel(DefFillInLevel);
            // ILU(0)のパターン初期化
            Prec.SetLinearSystem(Ls);

            //------------------------------------------------------------------
            // 剛性行列、残差ベクトルのマージ開始
            //------------------------------------------------------------------
            Ls.InitializeMarge();
            uint ntmp = Ls.GetTmpBufferSize();
            tmpBuffer = new int[ntmp];
            for (int i = 0; i < ntmp; i++)
            {
                tmpBuffer[i] = -1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="World"></param>
        /// <param name="FieldValId"></param>
        /// <param name="Ls"></param>
        /// <param name="Prec"></param>
        /// <param name="tmpBuffer"></param>
        /// <param name="resVec"></param>
        /// <param name="value_c_all"></param>
        /// <param name="isConverged"></param>
        /// <returns></returns>
        public static bool SolvePCOCG(
            ref CFieldWorld World, ref uint FieldValId,
            ref CZLinearSystem Ls, ref CZPreconditioner_ILU Prec, ref int[] tmpBuffer,
            ref KrdLab.clapack.Complex[] resVec, out KrdLab.clapack.Complex[] value_c_all, out bool isConverged)
        {
            bool success = false;

            //------------------------------------------------------------------
            // マージの終了
            //------------------------------------------------------------------
            double res = Ls.FinalizeMarge();
            tmpBuffer = null;

            // マトリクスサイズの取得
            // 要素剛性行列(コーナ-コーナー)
            CZMatDia_BlkCrs_Ptr mat_cc = Ls.GetMatrixPtr(FieldValId, ELSEG_TYPE.CORNER, World);
            // マトリクスのサイズ
            int matLen = (int)mat_cc.NBlkMatCol();

            //------------------------------------------------------------------
            // リニアシステムを解く
            //------------------------------------------------------------------
            Console.WriteLine("Solve_PCOCG : CZSolverLsIter.Solve_PCOCG");
            // プリコンディショナに値を設定してILU分解を行う
            Prec.SetValue(Ls);
            //double tol = 1.0e-1;
            //double tol = 1.0e-2;
            //double tol = 0.5e-3;
            //double tol = 1.0e-4;
            double tol = 1.0e-6;
            //uint maxIter = 500;
            //uint maxIter = 1000;
            //uint maxIter = 2000;
            //uint maxIter = 5000;
            //uint maxIter = 10000;
            uint maxIter = 20000;
            //uint maxIter = uint.MaxValue;
            uint iter = maxIter;
            bool ret = CZSolverLsIter.Solve_PCOCG(ref tol, ref iter, Ls, Prec);
            Console.WriteLine("iter : " + iter + " Res : " + tol + " ret : " + ret);
            if (iter == maxIter)
            {
                Console.WriteLine("Not converged");
                isConverged = false;
            }
            else
            {
                isConverged = true;
            }
            Console.WriteLine("Solve_PCOCG solved");

            //------------------------------------------------------------------
            // 計算結果の後処理
            //------------------------------------------------------------------
            // 計算結果をワールド座標系に反映する
            Ls.UpdateValueOfField(FieldValId, World, FIELD_DERIVATION_TYPE.VALUE);

            {
                uint node_cnt = (uint)matLen;
                //value_c_all = new System.Numerics.Complex[node_cnt]; // コーナーの値
                value_c_all = new KrdLab.clapack.Complex[node_cnt]; // コーナーの値

                CField valField = World.GetField(FieldValId);
                // 要素アレイIDのリストを取得
                IList<uint> aIdEA = valField.GetAryIdEA();
                // 要素アレイを走査
                foreach (uint eaId in aIdEA)
                {
                    CElemAry ea = World.GetEA(eaId);
                    CElemAry.CElemSeg es_c_va = valField.GetElemSeg(eaId, ELSEG_TYPE.CORNER, true, World);
                    CElemAry.CElemSeg es_c_co = valField.GetElemSeg(eaId, ELSEG_TYPE.CORNER, false, World);
                    // 要素の節点数
                    uint nno = 1; //点
                    // 要素節点の全体節点番号
                    uint[] no_c = new uint[nno];
                    // 要素節点の値
                    Complex[] value_c = new Complex[nno];
                    // 節点の値の節点セグメント
                    CNodeAry.CNodeSeg ns_c_val = valField.GetNodeSeg(ELSEG_TYPE.CORNER, true, World);
                    for (uint ielem = 0; ielem < ea.Size(); ielem++)
                    {
                        // 要素配列から要素セグメントの節点番号を取り出す
                        es_c_co.GetNodes(ielem, no_c);

                        // 節点の値を取って来る
                        es_c_va.GetNodes(ielem, no_c);
                        for (uint inoes = 0; inoes < nno; inoes++)
                        {
                            Complex[] tmpval = null;
                            ns_c_val.GetValue(no_c[inoes], out tmpval);
                            System.Diagnostics.Debug.Assert(tmpval.Length == 1);
                            value_c[inoes] = tmpval[0];
                            //System.Diagnostics.Debug.Write( "(" + value_c[inoes].Real + "," + value_c[inoes].Imag + ")" );
                        }

                        for (uint ino = 0; ino < nno; ino++)
                        {
                            //DelFEMの節点番号は０開始
                            //uint ino_global = no_c[ino] - 1;
                            uint ino_global = no_c[ino];
                            Complex cvalue = value_c[ino];
                            //value_c_all[ino_global] = new KrdLab.clapack.Complex(cvalue.Real, cvalue.Imag);
                            value_c_all[ino_global].Real = cvalue.Real;
                            value_c_all[ino_global].Imaginary = cvalue.Imag;
                        }
                    }
                }
            }

            Ls.Clear();
            Ls.Dispose();
            Ls = null;
            Prec.Clear();
            Prec.Dispose();
            Prec = null;
            World.Clear();
            World.Dispose();
            World = null;
            Console.WriteLine("Solve_PCOCG End");
            return success;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////
        // DekFEMの処理を隠ぺいした簡易版
        ////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// PCOCGで線形方程式を解く
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="resVec"></param>
        /// <param name="value_c_all"></param>
        /// <param name="isConverged"></param>
        /// <returns></returns>
        //public static bool SolvePCOCG(ref MyUtilLib.Matrix.MyComplexMatrix mat, ref System.Numerics.Complex[] resVec, out System.Numerics.Complex[] value_c_all, out bool isConverged)
        public static bool SolvePCOCG(ref MyUtilLib.Matrix.MyComplexMatrix mat, ref KrdLab.clapack.Complex[] resVec, out KrdLab.clapack.Complex[] value_c_all, out bool isConverged)
        {
            bool success = false;

            int matLen = mat.RowSize;

            Console.WriteLine("SolvePCOCG 1");
            //------------------------------------------------------------------
            // ワールド座標系を生成
            //------------------------------------------------------------------
            uint baseId = 0;
            CFieldWorld World = new CFieldWorld();
            setupWorld((uint)matLen, ref World, out baseId);

            // 界の値を扱うバッファ？を生成する。
            // フィールド値IDが返却される。
            //    要素の次元: 2次元-->ポイントにしたので0次元？ 界: 複素数スカラー 微分タイプ: 値 要素セグメント: 角節点
            uint FieldValId = World.MakeField_FieldElemDim(baseId, 0,
                FIELD_TYPE.ZSCALAR, FIELD_DERIVATION_TYPE.VALUE, ELSEG_TYPE.CORNER);

            Console.WriteLine("SolvePCOCG 2");
            //------------------------------------------------------------------
            // リニアシステム
            //------------------------------------------------------------------
            CZLinearSystem Ls = new CZLinearSystem();
            CZPreconditioner_ILU Prec = new CZPreconditioner_ILU();

            //------------------------------------------------------------------
            // 界パターン追加
            //------------------------------------------------------------------
            Ls.AddPattern_Field(FieldValId, World);
            //  POINTのフィールドなので対角要素のパターンのみ追加される。非対角要素のパターンは追加されない

            // 非対角要素のパターンの追加
            {
                uint node_cnt = (uint)matLen;
                // 要素剛性行列(コーナ-コーナー)
                CZMatDia_BlkCrs_Ptr mat_cc = Ls.GetMatrixPtr(FieldValId, ELSEG_TYPE.CORNER, World);
                // 要素残差ベクトル(コーナー)
                CZVector_Blk_Ptr res_c = Ls.GetResidualPtr(FieldValId, ELSEG_TYPE.CORNER, World);

                // パターンを追加
                using (CIndexedArray crs = new CIndexedArray())
                {
                    crs.InitializeSize(mat_cc.NBlkMatCol());

                    //crs.Fill(mat_cc.NBlkMatCol(), mat_cc.NBlkMatRow());
                    using (UIntVectorIndexer index = crs.index)
                    using (UIntVectorIndexer ary = crs.array)
                    {
                        for (int iblk = 0; iblk < (int)crs.Size(); iblk++)
                        {
                            index[iblk] = 0;
                        }
                        for (int iblk = 0; iblk < mat_cc.NBlkMatCol(); iblk++)
                        {
                            // 現在のマトリクスのインデックス設定をコピーする
                            uint npsup = 0;
                            ConstUIntArrayIndexer cur_rows = mat_cc.GetPtrIndPSuP((uint)iblk, out npsup);
                            foreach (uint row_index in cur_rows)
                            {
                                ary.Add(row_index);
                            }
                            index[iblk + 1] = (uint)ary.Count;
                            //Console.WriteLine("chk:{0} {1}", iblk, npsup);
                            // Note: インデックス設定はされていないので常にnpsup == 0 (cur_rows.Count == 0)

                            // すべての節点に関して列を追加
                            int ino_global = iblk;
                            int col = iblk;
                            if (col != -1 && ino_global != -1)
                            {
                                // 関連付けられていない節点をその行の列データの最後に追加
                                int last_index = (int)index[col + 1] - 1;
                                int add_cnt = 0;
                                for (int jno_global = 0; jno_global < node_cnt; jno_global++)
                                {
                                    // 行列の０要素を除く
                                    //if (System.Numerics.Complex.Abs(mat[(int)ino_global, (int)jno_global]) < Constants.PrecisionLowerLimit)
                                    //if (System.Numerics.Complex.Abs(mat[(int)ino_global, (int)jno_global]) < 1.0e-15)
                                    //if (mat._body[ino_global + jno_global * mat.RowSize].Magnitude < Constants.PrecisionLowerLimit)
                                    KrdLab.clapack.Complex cvalue = mat._body[ino_global + jno_global * mat.RowSize];
                                    if (cvalue.Real == 0 && cvalue.Imaginary == 0)
                                    {
                                        continue;
                                    }
                                    uint row = (uint)jno_global;
                                    if (ino_global != jno_global)  // 対角要素は除く
                                    {
                                        if (!cur_rows.Contains(row))
                                        {
                                            ary.Insert(last_index + 1 + add_cnt, row);
                                            add_cnt++;
                                            //System.Diagnostics.Debug.WriteLine("added:" + col + " " + row);
                                        }
                                    }
                                }
                                if (add_cnt > 0)
                                {
                                    index[col + 1] = (uint)ary.Count;
                                }
                            }
                        }
                    }
                    crs.Sort();
                    System.Diagnostics.Debug.Assert(crs.CheckValid());
                    // パターンを削除する
                    mat_cc.DeletePattern();
                    // パターンを追加する
                    mat_cc.AddPattern(crs);
                }
            }

            Console.WriteLine("SolvePCOCG 3");
            //------------------------------------------------------------------
            // プリコンディショナ―
            //------------------------------------------------------------------
            // set Preconditioner
            //Prec.SetFillInLevel(1);
            // フィルインなしで不完全LU分解した方が収束が早く、また解もそれなりの結果だったので０に設定した
            //Prec.SetFillInLevel(0);
            // フィルインなしでは、収束しない場合がある。フィルインレベルを最大にする
            //Prec.SetFillInLevel((uint)mat.RowSize);
            //   完全LU分解（だと思われる) Prec.SetLinearSystemの処理が遅い
            //Prec.SetFillInLevel(3);
            //   誘電体スラブ導波路だと最低このレベル。これでも収束しない場合がある
            //以上から
            //メインの導波管の計算時間を短くしたいのでフィルインなしにする
            //Prec.SetFillInLevel(0);
            //    導波管だとこれで十分
            // フィルインレベルの設定(既定値)
            Prec.SetFillInLevel(DefFillInLevel);
            // ILU(0)のパターン初期化
            Prec.SetLinearSystem(Ls);
            Console.WriteLine("SolvePCOCG 4");

            //------------------------------------------------------------------
            // 剛性行列、残差ベクトルのマージ
            //------------------------------------------------------------------
            Ls.InitializeMarge();
            {
                uint ntmp = Ls.GetTmpBufferSize();
                int[] tmpBuffer = new int[ntmp];
                for (int i = 0; i < ntmp; i++)
                {
                    tmpBuffer[i] = -1;
                }

                uint node_cnt = (uint)matLen;

                // 要素剛性行列(コーナ-コーナー)
                CZMatDia_BlkCrs_Ptr mat_cc = Ls.GetMatrixPtr(FieldValId, ELSEG_TYPE.CORNER, World);
                // 要素残差ベクトル(コーナー)
                CZVector_Blk_Ptr res_c = Ls.GetResidualPtr(FieldValId, ELSEG_TYPE.CORNER, World);

                bool[,] add_flg = new bool[node_cnt, node_cnt];
                for (int i = 0; i < node_cnt; i++)
                {
                    for (int j = 0; j < node_cnt; j++)
                    {
                        add_flg[i, j] = false;
                        // ０要素はマージ対象でないので最初から除外する
                        //if (System.Numerics.Complex.Abs(mat[i, j]) < Constants.PrecisionLowerLimit)
                        //if (mat._body[i + j * mat.RowSize].Magnitude < Constants.PrecisionLowerLimit)
                        KrdLab.clapack.Complex cvalue = mat._body[i + j * mat.RowSize];
                        if (cvalue.Real == 0 && cvalue.Imaginary == 0)
                        {
                            add_flg[i, j] = true;
                        }
                    }
                }
                for (int iblk = 0; iblk < mat_cc.NBlkMatCol(); iblk++)
                {
                    // 自分及び自分自身と関連のある"row"の要素をマージする( 1 x rowcntのベクトル : 行列の横ベクトルに対応)
                    uint npsup = 0;
                    ConstUIntArrayIndexer cur_rows = mat_cc.GetPtrIndPSuP((uint)iblk, out npsup);
                    uint colcnt = 1;
                    uint rowcnt = (uint)cur_rows.Count + 1; // cur_rowsには自分自身は含まれないので+1する
                    Complex[] emattmp = new Complex[colcnt * rowcnt];
                    uint[] no_c_tmp_col = new uint[colcnt];
                    uint[] no_c_tmp_row = new uint[rowcnt];
                    no_c_tmp_col[0] = (uint)iblk;
                    no_c_tmp_row[0] = (uint)iblk;
                    for (int irow = 0; irow < cur_rows.Count; irow++)
                    {
                        no_c_tmp_row[irow + 1] = cur_rows[irow];
                    }
                    {
                        uint ino = 0;
                        uint ino_global = no_c_tmp_col[ino];
                        for (int jno = 0; jno < rowcnt; jno++)
                        {
                            uint jno_global = no_c_tmp_row[jno];
                            //emat[ino, jno]
                            if (!add_flg[ino_global, jno_global])
                            {
                                //System.Numerics.Complex cvalue = (System.Numerics.Complex)mat[ino_global, jno_global];
                                //System.Numerics.Complex cvalue = (System.Numerics.Complex)mat._body[ino_global + jno_global * mat.RowSize];
                                KrdLab.clapack.Complex cvalue = mat._body[ino_global + jno_global * mat.RowSize];
                                emattmp[ino * rowcnt + jno] = new Complex(cvalue.Real, cvalue.Imaginary);
                                add_flg[ino_global, jno_global] = true;
                            }
                            else
                            {
                                // ここにはこない
                                System.Diagnostics.Debug.Assert(false);
                                emattmp[ino * rowcnt + jno] = new Complex(0, 0);
                            }
                        }
                    }
                    // マージ!
                    mat_cc.Mearge(1, no_c_tmp_col, rowcnt, no_c_tmp_row, 1, emattmp, ref tmpBuffer);
                }
                for (int i = 0; i < node_cnt; i++)
                {
                    for (int j = 0; j < node_cnt; j++)
                    {
                        //System.Diagnostics.Debug.WriteLine( i + " " + j + " " + add_flg[i, j] );
                        System.Diagnostics.Debug.Assert(add_flg[i, j]);
                    }
                }

                // 残差ベクトルにマージ
                for (uint ino_global = 0; ino_global < node_cnt; ino_global++)
                {
                    // 残差ベクトルにマージする
                    uint no_tmp = ino_global;
                    //System.Numerics.Complex cvalue = resVec[ino_global];
                    KrdLab.clapack.Complex cvalue = resVec[ino_global];
                    Complex val = new Complex(cvalue.Real, cvalue.Imaginary);
                    res_c.AddValue(no_tmp, 0, val);
                }
            }
            double res = Ls.FinalizeMarge();
            //------------------------------------------------------------------
            // リニアシステムを解く
            //------------------------------------------------------------------
            Console.WriteLine("Solve_PCOCG 5: CZSolverLsIter.Solve_PCOCG");
            // プリコンディショナに値を設定してILU分解を行う
            Prec.SetValue(Ls);
            //double tol = 1.0e-1;
            //double tol = 1.0e-2;
            //double tol = 0.5e-3;
            //double tol = 1.0e-4;
            double tol = 1.0e-6;
            //uint maxIter = 500;
            //uint maxIter = 1000;
            //uint maxIter = 2000;
            //uint maxIter = 5000;
            //uint maxIter = 10000;
            uint maxIter = 20000;
            //uint maxIter = uint.MaxValue;
            uint iter = maxIter;
            bool ret = CZSolverLsIter.Solve_PCOCG(ref tol, ref iter, Ls, Prec);
            Console.WriteLine("iter : " + iter + " Res : " + tol + " ret : " + ret);
            if (iter == maxIter)
            {
                Console.WriteLine("Not converged");
                isConverged = false;
            }
            else
            {
                isConverged = true;
            }
            Console.WriteLine("Solve_PCOCG 6");

            //------------------------------------------------------------------
            // 計算結果の後処理
            //------------------------------------------------------------------
            // 計算結果をワールド座標系に反映する
            Ls.UpdateValueOfField(FieldValId, World, FIELD_DERIVATION_TYPE.VALUE);

            {
                uint node_cnt = (uint)matLen;
                //value_c_all = new System.Numerics.Complex[node_cnt]; // コーナーの値
                value_c_all = new KrdLab.clapack.Complex[node_cnt]; // コーナーの値

                CField valField = World.GetField(FieldValId);
                // 要素アレイIDのリストを取得
                IList<uint> aIdEA = valField.GetAryIdEA();
                // 要素アレイを走査
                foreach (uint eaId in aIdEA)
                {
                    CElemAry ea = World.GetEA(eaId);
                    CElemAry.CElemSeg es_c_va = valField.GetElemSeg(eaId, ELSEG_TYPE.CORNER, true, World);
                    CElemAry.CElemSeg es_c_co = valField.GetElemSeg(eaId, ELSEG_TYPE.CORNER, false, World);
                    // 要素の節点数
                    uint nno = 1; //点
                    // 要素節点の全体節点番号
                    uint[] no_c = new uint[nno];
                    // 要素節点の値
                    Complex[] value_c = new Complex[nno];
                    // 節点の値の節点セグメント
                    CNodeAry.CNodeSeg ns_c_val = valField.GetNodeSeg(ELSEG_TYPE.CORNER, true, World);
                    for (uint ielem = 0; ielem < ea.Size(); ielem++)
                    {
                        // 要素配列から要素セグメントの節点番号を取り出す
                        es_c_co.GetNodes(ielem, no_c);

                        // 節点の値を取って来る
                        es_c_va.GetNodes(ielem, no_c);
                        for (uint inoes = 0; inoes < nno; inoes++)
                        {
                            Complex[] tmpval = null;
                            ns_c_val.GetValue(no_c[inoes], out tmpval);
                            System.Diagnostics.Debug.Assert(tmpval.Length == 1);
                            value_c[inoes] = tmpval[0];
                            //System.Diagnostics.Debug.Write( "(" + value_c[inoes].Real + "," + value_c[inoes].Imag + ")" );
                        }

                        for (uint ino = 0; ino < nno; ino++)
                        {
                            //DelFEMの節点番号は０開始
                            //uint ino_global = no_c[ino] - 1;
                            uint ino_global = no_c[ino];
                            Complex cvalue = value_c[ino];
                            //value_c_all[ino_global] = new KrdLab.clapack.Complex(cvalue.Real, cvalue.Imag);
                            value_c_all[ino_global].Real = cvalue.Real;
                            value_c_all[ino_global].Imaginary = cvalue.Imag;
                        }
                    }
                }
            }

            Ls.Clear();
            Ls.Dispose();
            Prec.Clear();
            Prec.Dispose();
            World.Clear();
            World.Dispose();
            Console.WriteLine("Solve_PCOCG End");
            return success;
        }

        /// <summary>
        /// ワールド座標系を構成する
        /// </summary>
        /// <param name="ncolrow">行列のrowSize(==columnSize)</param>
        /// <param name="world">ワールド座標系オブジェクト</param>
        /// <param name="id_field_base">フィールドIDのベース</param>
        /// <returns></returns>
        private static bool setupWorld(uint ncolrow, ref CFieldWorld world, out uint id_field_base)
        {
            bool success = false;

            id_field_base = 0;

            uint id_na, id_ns_co;
            {
                // 節点を生成
                uint ndim = 2;
                uint nnode = ncolrow;
                id_na = world.AddNodeAry(nnode);
                // 書き込み用に節点アレイのポインタを取得
                CNodeAryPtr na = world.GetNAPtr(id_na);
                id_ns_co = na.GetFreeSegID();
                CNodeAry.CNodeSeg ns_co = new CNodeAry.CNodeSeg(ndim, "COORD");
                IList<Pair<uint, CNodeAry.CNodeSeg>> ns_input_ary = new List<Pair<uint, CNodeAry.CNodeSeg>>();
                ns_input_ary.Add(new Pair<uint, CNodeAry.CNodeSeg>(id_ns_co, ns_co));
                // 座標は分からないので空リストを追加
                IList<double> coord = new List<double>();
                for (int i = 0; i < nnode; i++)
                {
                    for (int k = 0; k < ndim; k++)
                    {
                        coord.Add(0);
                    }
                }
                IList<int> add_id_ary = na.AddSegment(ns_input_ary, coord);
            }

            IList<CField.CElemInterpolation> aElemIntp = new List<CField.CElemInterpolation>();// 要素Index
            IList<Pair<uint, uint>> aEaEs = new List<Pair<uint, uint>>();
            {
                // 要素を生成
                uint id_ea;
                uint id_es;
                // 本当は三角形要素で要素を追加するところだが、わからないので
                // 要素は点で追加する
                int ilayer = 0;
                uint nnode = ncolrow;
                uint nnoel = 1; // 要素内節点数
                //CAD_ELEM_TYPE itype_cad_part = CAD_ELEM_TYPE.VERTEX;
                uint nelem = nnode / nnoel;
                id_ea = world.AddElemAry(nelem, ELEM_TYPE.POINT);
                // 書き込み用に要素アレイのポインタを取得
                CElemAryPtr ea = world.GetEAPtr(id_ea);
                // 節点番号のリスト
                IList<int> lnods = new List<int>((int)nnode);
                for (int ino = 0; ino < nnode; ino++)
                {
                    //lnods.Add(ino + 1);
                    //DelFEMの節点番号は０開始
                    lnods.Add(ino);
                }
                uint id_es_tmp = ea.GetFreeSegID();
                CElemAry.CElemSeg es = new CElemAry.CElemSeg(id_na, ELSEG_TYPE.CORNER);
                id_es = (uint)ea.AddSegment(id_es_tmp, es, lnods);

                aEaEs.Add(new Pair<uint, uint>(id_ea, id_es));
                CField.CElemInterpolation ei = new CField.CElemInterpolation(id_ea, 0, id_es, 0, 0, 0, 0);
                ei.ilayer = ilayer;
                aElemIntp.Add(ei);
            }
            {
                // 要素と節点の関連付け
                // 書き込み用に節点アレイのポインタを取得する
                CNodeAryPtr na = world.GetNAPtr(id_na);
                for (int ieaes = 0; ieaes < aEaEs.Count; ieaes++)
                {
                    na.AddEaEs(aEaEs[ieaes]);
                }
            }
            {
                // フィールドを生成
                uint id_field_parent = 0; // 親フィールド
                CField.CNodeSegInNodeAry nsna_c = new CField.CNodeSegInNodeAry(id_na, false, id_ns_co, 0, false, 0, 0, 0); // CORNER節点
                CField.CNodeSegInNodeAry nsna_b = new CField.CNodeSegInNodeAry(); // BUBBLE節点

                id_field_base = world.AddField(id_field_parent, aElemIntp, nsna_c, nsna_b);
            }

            return success;
        }
    }
}
