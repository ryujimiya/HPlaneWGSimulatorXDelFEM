using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
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
using Tao.OpenGl;
using Tao.Platform.Windows;

namespace HPlaneWGSimulatorXDelFEM
{
    /// <summary>
    /// CadLogicのデータを管理
    /// </summary>
    class CadLogicBase : IDisposable
    {
        ////////////////////////////////////////////////////////////////////////
        // 型
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Cadモード
        ///   None 操作なし
        ///   Area 図形作成
        ///   MediaFill 媒質埋め込み
        ///   Port ポート境界選択
        ///   Erase 消しゴム
        ///   ポート番号振り
        /// </summary>
        public enum CadModeType { None, Area, MediaFill, Port, Erase, IncidentPort, PortNumbering };

        /// <summary>
        /// ループ情報
        /// </summary>
        public class Loop
        {
            /////////////////////////////////////////////////
            // 変数
            /////////////////////////////////////////////////
            /// <summary>
            /// ループID
            /// </summary>
            public uint LoopId
            {
                get;
                private set;
            }
            /// <summary>
            /// 媒質インデックス
            /// </summary>
            public int MediaIndex
            {
                get;
                private set;
            }

            /// <summary>
            /// コンストラクタ
            /// </summary>
            public Loop()
            {
                LoopId = 0;
                MediaIndex = 0;
            }

            /// <summary>
            /// コンストラクタ２
            /// </summary>
            /// <param name="loopId"></param>
            /// <param name="mediaIndex"></param>
            public Loop(uint loopId, int mediaIndex)
            {
                this.Set(loopId, mediaIndex);
            }

            /// <summary>
            /// コピーコンストラクタ
            /// </summary>
            /// <param name="src"></param>
            public Loop(Loop src)
            {
                this.Copy(src);
            }

            /// <summary>
            /// 有効?
            /// </summary>
            /// <returns></returns>
            public bool IsValid()
            {
                return LoopId != 0;
            }

            /// <summary>
            /// 値のセット
            /// </summary>
            /// <param name="loopId"></param>
            /// <param name="mediaIndex"></param>
            public void Set(uint loopId, int mediaIndex)
            {
                System.Diagnostics.Debug.Assert(loopId != 0);
                LoopId = loopId;
                MediaIndex = mediaIndex;
            }

            /// <summary>
            /// コピー
            /// </summary>
            /// <param name="src"></param>
            public void Copy(Loop src)
            {
                this.LoopId = src.LoopId;
                this.MediaIndex = src.MediaIndex;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // 定数
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 利用ユーティリティ名
        /// </summary>
        public static readonly string UseUtility = "DelFEM";
        /// <summary>
        /// 分割数
        /// </summary>
        protected static readonly Size MaxDiv = Constants.MaxDiv;
        /// <summary>
        /// 媒質の個数
        /// </summary>
        protected const int MaxMediaCount = Constants.MaxMediaCount;
        public const int MetalMediaIndex = 0; // 導体領域
        public const int VacumnMediaIndex = 1; // 真空領域
        /// <summary>
        /// 媒質インデックスの既定値
        /// </summary>
        protected const int DefMediaIndex = VacumnMediaIndex;  // 真空
        /// <summary>
        /// 媒質の表示背景色
        /// </summary>
        public static Color[] MediaBackColors = new Color[MaxMediaCount]
            {
                Color.White, Color.Gray, Color.MistyRose, Color.LightGreen
            };

        ////////////////////////////////////////////////////////////////////////
        // フィールド
        ////////////////////////////////////////////////////////////////////////
        protected static Random RandForTmpFilename = new Random();
        /// <summary>
        /// シリアライズされたCadオブジェクト格納バッファ
        /// </summary>
        protected string SerializedCadObjBuff = "";
        /// <summary>
        /// Cadのループのリスト
        /// </summary>
        protected IList<Loop> LoopList = new List<Loop>();
        
        ////////////////////////////////////////////////////
        //編集中
        ////////////////////////////////////////////////////
        /// <summary>
        /// 編集中のVector2D 座標リスト
        /// </summary>
        protected IList<CVector2D> EditPts = new List<CVector2D>();
        /// <summary>
        /// 追加中の多角形の頂点IDのリスト
        /// </summary>
        protected IList<uint> EditVertexIds = new List<uint>();
        /// <summary>
        /// 追加中の多角形の辺IDのリスト
        /// </summary>
        protected IList<uint> EditEdgeIds = new List<uint>();

        /// <summary>
        /// 境界リストのリスト
        /// </summary>
        protected IList<EdgeCollection> EdgeCollectionList = new List<EdgeCollection>();
        /// <summary>
        /// 入射ポート番号
        /// </summary>
        protected int IncidentPortNo = 1;
        /// <summary>
        /// 媒質リスト
        /// </summary>
        protected MediaInfo[] Medias = new MediaInfo[Constants.MaxMediaCount];
        /// <summary>
        /// Cadモード
        /// </summary>
        protected CadModeType _CadMode = CadModeType.None;
        
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public CadLogicBase()
        {
            //Console.WriteLine("CadLogicBase Constructor");
            //Console.WriteLine("memory: {0}", GC.GetTotalMemory(false)); //DEBUG
            init();
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected void init()
        {
            _CadMode = CadModeType.None;
            SerializedCadObjBuff = "";
            LoopList.Clear();
            EdgeCollectionList.Clear();

            //edit
            EditPts.Clear();
            EditVertexIds.Clear();
            EditEdgeIds.Clear();

            IncidentPortNo = 1;
            for (int i = 0; i < Medias.Length; i++)
            {
                MediaInfo media = new MediaInfo();
                media.BackColor = MediaBackColors[i];
                Medias[i] = media;
            }
        }

        /// <summary>
        /// デストラクタ
        /// </summary>
        ~CadLogicBase()
        {
            //Console.WriteLine("  CadLogicBase Finalizer");
            Dispose(false);
            //Console.WriteLine("    CadLogicBase Finalizer done");
        }

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected virtual void Dispose(bool disposing)
        {
            //Console.WriteLine("    CadLogicBase Dispose {0}", disposing);
            // Cadオブジェクト一時ファイルを削除する
            clearSerializedCadObjBuffer();

            //Console.WriteLine("      CadLogicBase Dispose {0} done", disposing);
        }

        /// <summary>
        /// リソース破棄
        /// </summary>
        public void Dispose()
        {
            //Console.WriteLine("  CadLogicBase Dispose");
            Dispose(true);
            GC.SuppressFinalize(this);
            //Console.WriteLine("    CadLogicBase Dispose done");
        }

        /// <summary>
        /// Cadデータをコピーする
        /// </summary>
        /// <param name="src"></param>
        public void CopyData(CadLogicBase src)
        {
            //Console.WriteLine("-------CadLogicBase CopyData-------");
            if (src == this)
            {
                Console.WriteLine("Why? another me exists!");
                //System.Diagnostics.Debug.Assert(false);
                return;
            }

            // CadモードもUndo/Redo対象に入れる
            _CadMode = src._CadMode;
            // CadObj
            SerializedCadObjBuff = src.SerializedCadObjBuff;
            // LoopList
            LoopList.Clear();
            foreach (Loop srcLoop in src.LoopList)
            {
                Loop loop = new Loop(srcLoop);
                LoopList.Add(loop);
            }
            //check
            //foreach (EdgeCollection work in EdgeCollectionList)
            //{
            //    Console.WriteLine("  prev:No {0}  cnt {1}",  work.No, work.EdgeIds.Count);
            //}
            //foreach (EdgeCollection work in src.EdgeCollectionList)
            //{
            //    Console.WriteLine("  src:No {0}  cnt {1}", work.No, work.EdgeIds.Count);
            //}
            EdgeCollectionList.Clear();
            foreach (EdgeCollection srcEdge in src.EdgeCollectionList)
            {
                EdgeCollection edge = new EdgeCollection();
                edge.CP(srcEdge);
                EdgeCollectionList.Add(edge);
            }
            //check
            //foreach (EdgeCollection work in EdgeCollectionList)
            //{
            //    Console.WriteLine("  setted:No {0}  cnt {1}", work.No, work.EdgeIds.Count);
            //}

            // edit
            EditPts.Clear();
            foreach (CVector2D pp in src.EditPts)
            {
                EditPts.Add(pp);
            }
            // edit
            EditVertexIds.Clear();
            foreach (uint id_v in src.EditVertexIds)
            {
                EditVertexIds.Add(id_v);
            }
            // edit
            EditEdgeIds.Clear();
            foreach (uint id_e in src.EditEdgeIds)
            {
                EditEdgeIds.Add(id_e);
            }

            IncidentPortNo = src.IncidentPortNo;
            //SelectedMediaIndex = src.SelectedMediaIndex;
            for (int i = 0; i < src.Medias.Length; i++)
            {
                Medias[i].SetP(src.Medias[i].P);
                Medias[i].SetQ(src.Medias[i].Q);
            }
            //Console.WriteLine("-------CadLogicBase CopyData end----------");
        }

        /// <summary>
        /// 一時ファイル名を生成する
        /// </summary>
        /// <returns></returns>
        protected static string generateTmpFilename()
        {
            string tmpFilename = Application.UserAppDataPath + Path.DirectorySeparatorChar + "cadobj_tmp" + Path.DirectorySeparatorChar
                + string.Format("{0}", RandForTmpFilename.Next(1000000)) + ".tmp";
            return tmpFilename;
        }

        /// <summary>
        /// 一時ファイルの格納先ディレクトリを作成する
        /// </summary>
        /// <param name="dirname"></param>
        protected static void createDirectory(string dirname)
        {
            if (dirname != "" && !Directory.Exists(dirname))
            {
                try
                {
                    Directory.CreateDirectory(dirname);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message + " " + exception.StackTrace);
                    MessageBox.Show(exception.Message);
                }
            }
        }

        /// <summary>
        /// 一時ファイルの格納先ディレクトリを削除する
        /// </summary>
        public static void RemoveTmpFileDirectory()
        {
            // 一時ディレクトリを削除する
            string dummyTmpFilename = generateTmpFilename();
            string tmpFileDir = Path.GetDirectoryName(dummyTmpFilename);
            removeDirectory(tmpFileDir);
        }

        /// <summary>
        /// 一時ファイルの格納先ディレクトリを削除する
        /// </summary>
        /// <param name="dirname"></param>
        protected static void removeDirectory(string dirname)
        {
            if (dirname != "" && dirname.IndexOf(Application.UserAppDataPath) == 0 && Directory.Exists(dirname))
            {
                string[] files = Directory.GetFileSystemEntries(dirname);
                System.Diagnostics.Debug.Assert(files.Length == 0);
                if (files.Length == 0)
                {
                    try
                    {
                        Directory.Delete(dirname);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception.Message + " " + exception.StackTrace);
                        MessageBox.Show(exception.Message);
                    }
                }
            }
        }

        /// <summary>
        /// 図面をシリアライズバッファに保存する
        /// </summary>
        protected void saveEditCad2DToSerializedBuffer(CCadObj2D cad2d)
        {
            // シリアライズされたCadオブジェクトのバッファを削除
            clearSerializedCadObjBuffer();

            // Cadオブジェクト一時ファイルを作成
            string tmpFilename = generateTmpFilename();
            createDirectory(Path.GetDirectoryName(tmpFilename));
            try
            {
                // 編集Cadオブジェクトをファイルに出力する
                using (CSerializer fout = new CSerializer(tmpFilename, false)) // is_loading:false
                {
                    cad2d.Serialize(fout);
                }

                // シリアライズしたCadオブジェクトをメモリにロードする
                using (StreamReader sr = new StreamReader(tmpFilename, Encoding.GetEncoding(932)))
                {
                    SerializedCadObjBuff = sr.ReadToEnd();
                }

                // 一時ファイルを削除する
                File.Delete(tmpFilename);

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message + " " + exception.StackTrace);
                MessageBox.Show(exception.Message);
            }
            Console.WriteLine("SerializedCadObjBuff size = {0}", SerializedCadObjBuff.Length);
        }

        /// <summary>
        /// 編集図面をシリアライズされたバッファから読み込む
        /// </summary>
        /// <param name="cad2d"></param>
        protected void loadEditCad2DFromSerializedBuffer(ref CCadObj2D cad2d)
        {
            // 図面をクリア
            cad2d.Clear();
            // シリアライズバッファから図面を読み込む

            if (SerializedCadObjBuff != "")
            {
            // Cadオブジェクト一時ファイルを作成
            string tmpFilename = generateTmpFilename();
            createDirectory(Path.GetDirectoryName(tmpFilename));
                try
                {
                    // シリアライズバッファを一時ファイルに出力する(Shift JIS)
                    using (StreamWriter sw = new StreamWriter(tmpFilename, false, Encoding.GetEncoding(932)))
                    {
                        sw.Write(SerializedCadObjBuff);
                    }
                    // 一時ファイルからCadオブジェクトにデータをロードする
                    using (CSerializer fin = new CSerializer(tmpFilename, true)) // is_loading:true
                    {
                        cad2d.Serialize(fin);
                    }
                    // 一時ファイルを削除する
                    File.Delete(tmpFilename);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message + " " + exception.StackTrace);
                    MessageBox.Show(exception.Message);
                }
            }
        }

        /// <summary>
        /// Cadオブジェクトシリアライズバッファのクリア
        /// </summary>
        protected void clearSerializedCadObjBuffer()
        {
            SerializedCadObjBuff = "";
        }

    }
}
