using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
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
    /// Cadデータファイルの読み書き
    /// </summary>
    class CadDatFile
    {
        /////////////////////////////////////////////////////////////////////////////
        // 定数
        /////////////////////////////////////////////////////////////////////////////

         /////////////////////////////////////////////////////////////////////////////
         // 型
         /////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// 図面情報を保存する
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="editCad2D"></param>
        /// <param name="loopList"></param>
        /// <param name="edgeCollectionList"></param>
        /// <param name="incidentPortNo"></param>
        /// <param name="medias"></param>
        public static void SaveToFile(
            string filename,
            CCadObj2D editCad2D,
            IList<CadLogic.Loop> loopList,
            IList<EdgeCollection> edgeCollectionList,
            int incidentPortNo,
            MediaInfo[] medias
            )
        {
            // 番号順に並び替え
            ((List<EdgeCollection>)edgeCollectionList).Sort();

            try
            {
                // Cadオブジェクトデータファイル
                string basename = Path.GetDirectoryName(filename) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(filename);
                string cadObjFilename = basename + Constants.CadObjExt;

                // Cadオブジェクトデータを外部ファイルに書き込み
                using (CSerializer fout = new CSerializer(cadObjFilename, false))
                {
                    editCad2D.Serialize(fout);
                }

                // Cadデータの書き込み
                using (StreamWriter sw = new StreamWriter(filename))
                {
                    int counter;
                    string line;

                    // アプリケーションのバージョン番号
                    sw.WriteLine("AppVersion,{0}", MyUtilLib.MyUtil.getAppVersion());
                    // CadLogicの使用ユーティリティ名
                    sw.WriteLine("UseUtility,{0}", CadLogic.UseUtility);
                    //// ベースループID
                    //sw.WriteLine("BaseLoopId,{0}", baseLoopId);
                    // ループのリスト
                    counter = 0;
                    sw.WriteLine("LoopList,{0}", loopList.Count);
                    foreach (CadLogic.Loop loop in loopList)
                    {
                        sw.WriteLine("Loop,{0},{1},{2}", ++counter, loop.LoopId, loop.MediaIndex);
                    }
                    // ポートのエッジコレクションのリスト
                    counter = 0;
                    sw.WriteLine("EdgeCollectionList,{0}", edgeCollectionList.Count);
                    foreach (EdgeCollection edgeCollection in edgeCollectionList)
                    {
                        line = string.Format("EdgeCollection,{0},{1},{2},", ++counter, edgeCollection.No, edgeCollection.EdgeIds.Count);
                        foreach (uint eId in edgeCollection.EdgeIds)
                        {
                            line += string.Format("{0},", eId);
                        }
                        line = line.Remove(line.Length - 1); // 最後の,を削除
                        sw.WriteLine(line);
                    }
                    // 入射ポート番号
                    sw.WriteLine("IncidentPortNo,{0}", incidentPortNo);
                    // 媒質情報の個数
                    sw.WriteLine("Medias,{0}", medias.Length);
                    // 媒質情報の書き込み
                    for (int i = 0; i < medias.Length; i++)
                    {
                        MediaInfo media = medias[i];
                        line = string.Format("{0},", i);
                        double[,] p = media.P;
                        for (int m = 0; m < p.GetLength(0); m++)
                        {
                            for (int n = 0; n < p.GetLength(1); n++)
                            {
                                line += string.Format("{0},", p[m, n]);
                            }
                        }
                        double[,] q = media.Q;
                        for (int m = 0; m < q.GetLength(0); m++)
                        {
                            for (int n = 0; n < q.GetLength(1); n++)
                            {
                                line += string.Format("{0},", q[m, n]);
                            }
                        }
                        line = line.Remove(line.Length - 1); // 最後の,を削除
                        sw.WriteLine(line);
                    }
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
                MessageBox.Show(exception.Message);
            }
        }

        /// <summary>
        /// 図面情報を読み込む
        /// </summary>
        /// <returns></returns>
        public static bool LoadFromFile(
            string filename,
            out string appVersion,
            out string useUtility,
            ref CCadObj2D editCad2D,
            ref IList<CadLogic.Loop> loopList,
            ref IList<EdgeCollection> edgeCollectionList,
            out int incidentPortNo,
            ref MediaInfo[] medias
            )
        {
            bool success = false;

            ////////////////////////////////////////////////////////
            // 出力データの初期化

            // アプリケーションのバージョン番号
            appVersion = "";
            // ユーティリティ名
            useUtility = "";

            // 図面のクリア
            editCad2D.Clear();
            //// ベースループIDを初期化
            //baseLoopId = 0;
            // ループ情報リストの初期化
            loopList.Clear();
            // 入射ポートの初期化
            incidentPortNo = 1;
            // ポートのエッジコレクションのリストを初期化
            edgeCollectionList.Clear();
            // 媒質の比誘電率、比透磁率の逆数の初期化
            foreach (MediaInfo media in medias)
            {
                // 比透磁率の逆数
                media.SetP(new double[,]
                    {
                        {1.0, 0.0, 0.0},
                        {0.0, 1.0, 0.0},
                        {0.0, 0.0, 1.0}
                    });
                // 比誘電率
                media.SetQ(new double[,]
                    {
                        {1.0, 0.0, 0.0},
                        {0.0, 1.0, 0.0},
                        {0.0, 0.0, 1.0}
                    });
            }

            try
            {
                // Cadオブジェクトデータファイル
                string basename = Path.GetDirectoryName(filename) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(filename);
                string cadObjFilename = basename + Constants.CadObjExt;

                if (File.Exists(cadObjFilename))
                {
                    // Cadオブジェクトデータを外部ファイルから読み込む
                    using (CSerializer fin = new CSerializer(cadObjFilename, true))
                    {
                        editCad2D.Serialize(fin);
                    }
                }
                else
                {
                    MessageBox.Show("CadObjデータファイルがありません");
                    return success;
                }

                using (StreamReader sr = new StreamReader(filename))
                {
                    string line;
                    string[] tokens;
                    const char delimiter = ',';
                    int cnt = 0;

                    // アプリケーションのバージョン番号
                    line = sr.ReadLine();
                    if (line == null)
                    {
                        MessageBox.Show("アプリケーションのバージョン情報がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return success;
                    }
                    tokens = line.Split(delimiter);
                    if (tokens.Length != 2 || tokens[0] != "AppVersion")
                    {
                        MessageBox.Show("アプリケーションのバージョン情報がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return success;
                    }
                    appVersion = tokens[1];

                    // ユーティリティ名
                    line = sr.ReadLine();
                    if (line == null)
                    {
                        MessageBox.Show("ユーティリティ情報がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return success;
                    }
                    tokens = line.Split(delimiter);
                    if (tokens.Length != 2 || tokens[0] != "UseUtility")
                    {
                        MessageBox.Show("ユーティリティ情報がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return success;
                    }
                    useUtility = tokens[1];
                    if (useUtility != CadLogic.UseUtility)
                    {
                        MessageBox.Show("ユーティリティ情報が本アプリケーションのバージョンのものと一致しません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return success;
                    }

                    // ベースループID
                    //line = sr.ReadLine();
                    //if (line == null)
                    //{
                    //    MessageBox.Show("ベースループIDがありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //    return success;
                    //}
                    //tokens = line.Split(delimiter);
                    //if (tokens.Length != 2 || tokens[0] != "BaseLoopId")
                    //{
                    //    MessageBox.Show("ベースループIDがありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //    return success;
                    //}
                    //baseLoopId = uint.Parse(tokens[1]);

                    // ループのリスト
                    line = sr.ReadLine();
                    if (line == null)
                    {
                        MessageBox.Show("ループ一覧がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return success;
                    }
                    tokens = line.Split(delimiter);
                    if (tokens.Length != 2 || tokens[0] != "LoopList")
                    {
                        MessageBox.Show("ループ一覧がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return success;
                    }
                    cnt = int.Parse(tokens[1]);
                    for (int i = 0; i < cnt; i++)
                    {
                        line = sr.ReadLine();
                        if (line == null)
                        {
                            MessageBox.Show("ループ情報がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return success;
                        }
                        tokens = line.Split(delimiter);
                        if (tokens.Length != 4 || tokens[0] != "Loop")
                        {
                            MessageBox.Show("ループ情報が不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return success;
                        }
                        int countNo = int.Parse(tokens[1]);
                        uint loopId = uint.Parse(tokens[2]);
                        int mediaIndex = int.Parse(tokens[3]);
                        System.Diagnostics.Debug.Assert(countNo == i + 1);

                        CadLogic.Loop loop = new CadLogicBase.Loop(loopId, mediaIndex);
                        loopList.Add(loop);
                    }

                    // ポートのエッジコレクションのリスト
                    line = sr.ReadLine();
                    if (line == null)
                    {
                        MessageBox.Show("ポート一覧がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return success;
                    }
                    tokens = line.Split(delimiter);
                    if (tokens.Length != 2 || tokens[0] != "EdgeCollectionList")
                    {
                        MessageBox.Show("ポート一覧がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return success;
                    }
                    cnt = int.Parse(tokens[1]);
                    for (int i = 0; i < cnt; i++)
                    {
                        line = sr.ReadLine();
                        if (line == null)
                        {
                            MessageBox.Show("ポート情報がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return success;
                        }
                        tokens = line.Split(delimiter);
                        if (tokens.Length < (4 + 1) || tokens[0] != "EdgeCollection")
                        {
                            MessageBox.Show("ポート情報が不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return success;
                        }
                        int countNo = int.Parse(tokens[1]);
                        int portNo = int.Parse(tokens[2]);
                        int eIdCnt = int.Parse(tokens[3]);
                        System.Diagnostics.Debug.Assert(countNo == i + 1);
                        System.Diagnostics.Debug.Assert(eIdCnt != 0 && eIdCnt == (tokens.Length - 4));

                        EdgeCollection edgeCollection = new EdgeCollection();
                        edgeCollection.No = portNo;
                        for (int tokenIndex = 4; tokenIndex < tokens.Length; tokenIndex++)
                        {
                            uint eId = uint.Parse(tokens[tokenIndex]);
                            if (!edgeCollection.ContainsEdgeId(eId))
                            {
                                bool ret = edgeCollection.AddEdgeId(eId, editCad2D);
                            }
                        }
                        edgeCollectionList.Add(edgeCollection);
                    }
                    
                    // 入射ポート番号
                    line = sr.ReadLine();
                    if (line == null)
                    {
                        MessageBox.Show("入射ポート番号がありません");
                        return success;
                    }
                    tokens = line.Split(delimiter);
                    if (tokens.Length != 2 || tokens[0] != "IncidentPortNo")
                    {
                        MessageBox.Show("入射ポート番号がありません");
                        return success;
                    }
                    incidentPortNo = int.Parse(tokens[1]);

                    // 媒質情報
                    line = sr.ReadLine();
                    if (line == null)
                    {
                        MessageBox.Show("媒質情報がありません");
                        return success;
                    }
                    tokens = line.Split(delimiter);
                    if (tokens.Length != 2 || tokens[0] != "Medias")
                    {
                        MessageBox.Show("媒質情報がありません");
                        return success;
                    }
                    cnt = int.Parse(tokens[1]);
                    for (int i = 0; i < cnt; i++)
                    {
                        line = sr.ReadLine();
                        if (line == null)
                        {
                            MessageBox.Show("媒質情報が不正です");
                            return success;
                        }
                        tokens = line.Split(delimiter);
                        if (tokens.Length != 1 + 9 + 9)
                        {
                            MessageBox.Show("媒質情報が不正です");
                            return success;
                        }
                        int mediaIndex = int.Parse(tokens[0]);
                        System.Diagnostics.Debug.Assert(mediaIndex == i);
                        if (i >= medias.Length)
                        {
                            //読み飛ばす
                            continue;
                        }
                        double[,] p = new double[3, 3];
                        for (int m = 0; m < p.GetLength(0); m++)
                        {
                            for (int n = 0; n < p.GetLength(1); n++)
                            {
                                p[m, n] = double.Parse(tokens[1 + m * p.GetLength(1) + n]);
                            }
                        }
                            medias[i].SetP(p);

                        double[,] q = new double[3, 3];
                        for (int m = 0; m < q.GetLength(0); m++)
                        {
                            for (int n = 0; n < q.GetLength(1); n++)
                            {
                                q[m, n] = double.Parse(tokens[1 + 9 + m * q.GetLength(1) + n]);
                            }
                        }
                        medias[i].SetQ(q);
                    }
                }

                // 番号順に並び替え
                ((List<EdgeCollection>)edgeCollectionList).Sort();

                ////////////////////////////////////////////////////////////////////////
                // Cadオブジェクトの色をセットする
                // ループとその辺、頂点の色をセット
                foreach (CadLogic.Loop loop in loopList)
                {
                    uint id_l = loop.LoopId;
                    int mediaIndex = loop.MediaIndex;
                    MediaInfo media = medias[mediaIndex];
                    Color backColor = media.BackColor;
                    CadLogic.SetupColorOfCadObjectsForOneLoop(editCad2D, id_l, backColor);
                }
                // ポートの色をセットする
                CadLogic.SetupColorOfPortEdgeCollection(editCad2D, edgeCollectionList, incidentPortNo);

                success = true;
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
                MessageBox.Show(exception.Message);
            }

            return success;
        }
    }
}
