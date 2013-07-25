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
    class CadDatFile_Ver_1_2
    {
        /////////////////////////////////////////////////////////////////////////////
        // 定数
        /////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Ver1.2.0.0のファイルのバックアップファイルの接尾辞
        /// </summary>
         public static readonly string Ver1_2_Backup_Suffix = "_VER_1_2_0_0_BACKUP";

         /////////////////////////////////////////////////////////////////////////////
         // 型
         /////////////////////////////////////////////////////////////////////////////
         /// <summary>
        /// Ver1.2のエッジクラスの情報
        /// </summary>
        public class Edge_Ver_1_2
        {
            /// <summary>
            /// 辺の集合の番号 (ポート番号に相当する)
            /// </summary>
            public int No = 0;
            /// <summary>
            /// 辺の長さ
            ///    X方向境界の場合 Delta.Heigthは0 (new Size(1,0)を指定)
            ///    Y方向境界の場合 Delta,Widthは0 (new Size(0, 1)を指定)
            /// </summary>
            public Size Delta = new Size(0, 0);
            /// <summary>
            /// 辺の開始位置リスト
            ///   Points[0] 始点
            ///   Points[1] 終点
            /// </summary>
            public Point[] Points = new Point[2];

            /// <summary>
            /// 空?
            /// </summary>
            /// <returns></returns>
            public bool IsEmpty()
            {
                return Delta.Equals(new Size(0, 0));
            }

            /// <summary>
            /// 単純にPoints[0]とPoints[1]を入れ替える
            /// Points[0]に始点、Points[1]に終点座標を入れた場合、こちらを使用。
            /// (このインスタンス自身は変更しない)
            /// </summary>
            /// <returns></returns>
            public Edge_Ver_1_2 GetSimpleSwap()
            {
                Edge_Ver_1_2 e = new Edge_Ver_1_2();
                e.No = this.No;
                e.Delta = new Size(-this.Delta.Width, -this.Delta.Height);
                Point p1 = this.Points[0]; // 辺の始点を格納しているものとする
                Point p2 = this.Points[1]; // 辺の終点を格納しているものとする
                e.Points[0] = p2;
                e.Points[1] = p1;
                return e;
            }

            /// <summary>
            /// 辺が同じものか?
            ///  Noは比較しない. 辺の長さと、開始位置リストをチェックする
            /// </summary>
            /// <param name="tagt"></param>
            /// <returns></returns>
            public bool EqualEdge(Edge_Ver_1_2 tagt)
            {
                bool equals = false;
                if (!this.Delta.Equals(tagt.Delta))
                {
                    return equals;
                }                
                for (int ino = 0; ino < this.Points.Length; ino++)
                {
                    if (Points[ino].X != tagt.Points[ino].X)
                    {
                        return equals;
                    }
                    if (Points[ino].Y != tagt.Points[ino].Y)
                    {
                        return equals;
                    }
                }
                equals = true;
                return equals;
            }

            /// <summary>
            /// 包含関係にあるか？
            /// </summary>
            /// <param name="tagt"></param>
            /// <returns></returns>
            public bool Contains(Edge_Ver_1_2 tagt, out Point minPt, out Point maxPt)
            {
                bool contains = false;
                minPt = new Point();
                maxPt = new Point();
                if (this.Delta.Equals(tagt.Delta))
                {
                    if (this.Delta.Equals(new Size(1, 0)) && this.Points[0].Y == tagt.Points[0].Y)
                    {
                        int minX;
                        int maxX;
                        if (tagt.Points[1].X <= this.Points[0].X)
                        {
                            //  this                 +-------------+
                            //  tagt +-------------+
                            //
                            //  this               +-------------+
                            //  tagt +-------------+
                        }
                        else if (tagt.Points[0].X >= this.Points[1].X)
                        {
                            //  this +-------------+
                            //  tagt               +-------------+
                            //
                            //  this +-------------+
                            //  tagt                 +-------------+
                        }
                        else
                        {
                            contains = true;
                            if (this.Points[0].X >= tagt.Points[0].X)
                            {
                                minX = this.Points[0].X;
                            }
                            else
                            {
                                minX = tagt.Points[0].X;
                            }
                            if (this.Points[1].X <= tagt.Points[1].X)
                            {
                                maxX = this.Points[1].X;
                            }
                            else
                            {
                                maxX = tagt.Points[1].X;
                            }
                            minPt = new Point(minX, this.Points[0].Y);
                            maxPt = new Point(maxX, this.Points[0].Y);
                        }
                    }
                    else if (this.Delta.Equals(new Size(0, 1)) && this.Points[0].X == tagt.Points[0].X)
                    {
                        int minY;
                        int maxY;
                        if (tagt.Points[1].Y <= this.Points[0].Y)
                        {
                            //  this                 +-------------+
                            //  tagt +-------------+
                            //
                            //  this               +-------------+
                            //  tagt +-------------+
                        }
                        else if (tagt.Points[0].Y >= this.Points[1].Y)
                        {
                            //  this +-------------+
                            //  tagt               +-------------+
                            //
                            //  this +-------------+
                            //  tagt                 +-------------+
                        }
                        else
                        {
                            contains = true;
                            if (this.Points[0].Y >= tagt.Points[0].Y)
                            {
                                minY = this.Points[0].Y;
                            }
                            else
                            {
                                minY = tagt.Points[0].Y;
                            }
                            if (this.Points[1].Y <= tagt.Points[1].Y)
                            {
                                maxY = this.Points[1].Y;
                            }
                            else
                            {
                                maxY = tagt.Points[1].Y;
                            }
                            minPt = new Point(this.Points[0].X, minY);
                            maxPt = new Point(this.Points[0].X, maxY);
                        }
                    }
                }
                return contains;
            }

            /// <summary>
            /// Deltaに格納する値の計算
            /// </summary>
            /// <param name="p1"></param>
            /// <param name="p2"></param>
            /// <returns></returns>
            public static Size CalcDelta(Point p1, Point p2)
            {
                Size delta = new Size(0, 0);
                if (p1.X == p2.X && p1.Y == p2.Y)
                {
                    // 同一点
                    delta = new Size(0, 0);
                }
                else if (p1.Y == p2.Y)
                {
                    if (p2.X - p1.X >= 0)
                    {
                        delta = new Size(1, 0);
                    }
                    else
                    {
                        delta = new Size(-1, 0);  // ver1.2.0.0内では使用不可、今回拡張使用している
                    }
                }
                else if (p1.X == p2.X)
                {
                    if (p2.Y - p1.Y >= 0)
                    {
                        delta = new Size(0, 1);
                    }
                    else
                    {
                        delta = new Size(0, -1);  // ver1.2.0.0内では使用不可、今回拡張使用している
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("not implemented");
                }
                return delta;
            }
        }

        /// <summary>
        /// Ver1.2.0.0形式のCadファイルフォーマットで図面情報を保存する
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="maxDiv"></param>
        /// <param name="areaSelection"></param>
        /// <param name="areaToMediaIndex"></param>
        /// <param name="edgeList"></param>
        /// <param name="incidentPortNo"></param>
        /// <param name="medias"></param>
        public static void SaveToFile_Ver_1_2(
            string filename,
            Size maxDiv, bool[,] areaSelection, int[,] areaToMediaIndex,
            IList<CadDatFile_Ver_1_2.Edge_Ver_1_2> edgeList,
            int incidentPortNo,
            MediaInfo[] medias
            )
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(filename))
                {
                    int counter;
                    string line;

                    // 領域: 書き込む個数の計算
                    counter = 0;
                    for (int y = 0; y < maxDiv.Height; y++)
                    {
                        for (int x = 0; x < maxDiv.Width; x++)
                        {
                            if (areaSelection[y, x])
                            {
                                counter++;
                            }
                        }
                    }
                    // 領域: 書き込み
                    sw.WriteLine("AreaSelection,{0}", counter);
                    for (int y = 0; y < maxDiv.Height; y++)
                    {
                        for (int x = 0; x < maxDiv.Width; x++)
                        {
                            if (areaSelection[y, x])
                            {
                                // ver1.1.0.0から座標の後に媒質インデックスを追加
                                sw.WriteLine("{0},{1},{2}", x, y, areaToMediaIndex[y, x]);
                            }
                        }
                    }
                    // ポート境界: 書き込み個数の計算
                    sw.WriteLine("EdgeList,{0}", edgeList.Count);
                    // ポート境界: 書き込み
                    foreach (CadDatFile_Ver_1_2.Edge_Ver_1_2 edge in edgeList)
                    {
                        sw.WriteLine("{0},{1},{2},{3},{4}", edge.No, edge.Points[0].X, edge.Points[0].Y, edge.Points[1].X, edge.Points[1].Y);
                    }
                    // 入射ポート番号
                    sw.WriteLine("IncidentPortNo,{0}", incidentPortNo);
                    //////////////////////////////////////////
                    //// Ver1.1.0.0からの追加情報
                    //////////////////////////////////////////
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
        /// Ver1.2.0.0形式のCadファイルフォーマットで図面情報を読み込む
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="maxDiv"></param>
        /// <param name="areaSelection"></param>
        /// <param name="areaToMediaIndex"></param>
        /// <param name="edgeList"></param>
        /// <param name="yBoundarySelection">２次的な情報(edgeListから生成される)</param>
        /// <param name="xBoundarySelection">２次的な情報(edgeListから生成される)</param>
        /// <param name="incidentPortNo"></param>
        /// <param name="medias"></param>
        /// <returns></returns>
        public static bool LoadFromFile_Ver_1_2(
            string filename,
            out Size maxDiv, out bool[,] areaSelection, out int[,] areaToMediaIndex,
            out IList<CadDatFile_Ver_1_2.Edge_Ver_1_2> edgeList, out bool[,] yBoundarySelection, out bool[,] xBoundarySelection,
            out int incidentPortNo,
            out MediaInfo[] medias
            )
        {
            bool success = false;

            // ver1.2.0.0の設定値
            int maxMediaCount = 3;
            maxDiv = new Size(30, 30);

            // 初期化
            areaSelection = new bool[maxDiv.Width, maxDiv.Height];
            areaToMediaIndex = new int[maxDiv.Width, maxDiv.Height];
            for (int y = 0; y < maxDiv.Height; y++)
            {
                for (int x = 0; x < maxDiv.Width; x++)
                {
                    areaSelection[y, x] = false;
                    areaToMediaIndex[y, x] = 0;
                }
            }
            edgeList = new List<CadDatFile_Ver_1_2.Edge_Ver_1_2>();
            yBoundarySelection = new bool[maxDiv.Height, maxDiv.Width + 1];
            xBoundarySelection = new bool[maxDiv.Height + 1, maxDiv.Width];
            for (int x = 0; x < maxDiv.Width + 1; x++)
            {
                for (int y = 0; y < maxDiv.Height; y++)
                {
                    yBoundarySelection[y, x] = false;
                }
            }
            for (int y = 0; y < maxDiv.Height + 1; y++)
            {
                for (int x = 0; x < maxDiv.Width; x++)
                {
                    xBoundarySelection[y, x] = false;
                }
            }
            incidentPortNo = 1;
            medias = new MediaInfo[maxMediaCount];
            for (int i = 0; i < medias.Length; i++)
            {
                MediaInfo media = new MediaInfo();
                media.BackColor = CadLogic.MediaBackColors[i];
                medias[i] = media;
            }

            try
            {
                using (StreamReader sr = new StreamReader(filename))
                {
                    string line;
                    string[] tokens;
                    const char delimiter = ',';
                    int cnt = 0;

                    // 領域選択
                    line = sr.ReadLine();
                    tokens = line.Split(delimiter);
                    if (tokens[0] != "AreaSelection")
                    {
                        MessageBox.Show("領域選択情報がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return success;
                    }
                    cnt = int.Parse(tokens[1]);
                    for (int i = 0; i < cnt; i++)
                    {
                        line = sr.ReadLine();
                        tokens = line.Split(delimiter);
                        if (tokens.Length != 2 && tokens.Length != 3)  // ver1.1.0.0で媒質インデックス追加
                        {
                            MessageBox.Show("領域選択情報が不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return success;
                        }
                        int x = int.Parse(tokens[0]);
                        int y = int.Parse(tokens[1]);
                        int mediaIndex = 0;//CadLogic.DefMediaIndex;
                        if (tokens.Length == 3)
                        {
                            mediaIndex = int.Parse(tokens[2]);
                        }
                        if ((x >= 0 && x < maxDiv.Width) && (y >= 0 && y < maxDiv.Height))
                        {
                            areaSelection[y, x] = true;
                        }
                        else
                        {
                            MessageBox.Show("領域選択座標値が不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return success;
                        }
                        // ver1.1.0.0で追加
                        areaToMediaIndex[y, x] = mediaIndex;
                    }

                    // ポート境界
                    line = sr.ReadLine();
                    tokens = line.Split(delimiter);
                    if (tokens[0] != "EdgeList")
                    {
                        MessageBox.Show("境界選択情報がありません", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return success;
                    }
                    cnt = int.Parse(tokens[1]);
                    for (int i = 0; i < cnt; i++)
                    {
                        line = sr.ReadLine();
                        tokens = line.Split(delimiter);
                        if (tokens.Length != 5)
                        {
                            MessageBox.Show("境界選択情報が不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return success;
                        }
                        int edgeNo = int.Parse(tokens[0]);
                        Point[] p = new Point[2];
                        for (int k = 0; k < p.Length; k++)
                        {
                            p[k] = new Point();
                            p[k].X = int.Parse(tokens[1 + k * 2]);
                            p[k].Y = int.Parse(tokens[1 + k * 2 + 1]);

                        }
                        Size delta = new Size(0, 0);
                        if (p[0].X == p[1].X)
                        {
                            // Y方向境界
                            delta = new Size(0, 1);
                        }
                        else if (p[0].Y == p[1].Y)
                        {
                            // X方向境界
                            delta = new Size(1, 0);
                        }
                        else
                        {
                            MessageBox.Show("境界選択情報が不正です", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return success;
                        }
                        CadDatFile_Ver_1_2.Edge_Ver_1_2 edge = new CadDatFile_Ver_1_2.Edge_Ver_1_2();
                        edge.Delta = delta;
                        edge.No = edgeNo;
                        edge.Points[0] = p[0];
                        edge.Points[1] = p[1];
                        edgeList.Add(edge);
                    }
                    // ２次的な情報(edgeListから生成される)
                    foreach (CadDatFile_Ver_1_2.Edge_Ver_1_2 edge in edgeList)
                    {
                        if (edge.Delta.Width == 0)
                        {
                            // Y方向境界
                            int x = edge.Points[0].X;
                            int sty = edge.Points[0].Y;
                            int edy = edge.Points[1].Y;
                            for (int y = sty; y < edy; y++)
                            {
                                yBoundarySelection[y, x] = true;
                            }
                        }
                        else if (edge.Delta.Height == 0)
                        {
                            // X方向境界
                            int y = edge.Points[0].Y;
                            int stx = edge.Points[0].X;
                            int edx = edge.Points[1].X;
                            for (int x = stx; x < edx; x++)
                            {
                                xBoundarySelection[y, x] = true;
                            }
                        }
                        else
                        {
                            MessageBox.Show("Not implemented");
                        }
                    }

                    line = sr.ReadLine();
                    if (line.Length == 0)
                    {
                        MessageBox.Show("入射ポート番号がありません");
                        return success;
                    }
                    tokens = line.Split(delimiter);
                    if (tokens[0] != "IncidentPortNo")
                    {
                        MessageBox.Show("入射ポート番号がありません");
                        return success;
                    }
                    incidentPortNo = int.Parse(tokens[1]);

                    //////////////////////////////////////////
                    //// Ver1.1.0.0からの追加情報
                    //////////////////////////////////////////
                    line = sr.ReadLine();
                    if (line == null || line.Length == 0)
                    {
                        // 媒質情報なし
                        // ver1.0.0.0
                    }
                    else
                    {
                        // 媒質情報？
                        // ver1.1.0.0
                        tokens = line.Split(delimiter);
                        if (tokens[0] != "Medias")
                        {
                            MessageBox.Show("媒質情報がありません");
                            return success;
                        }
                        cnt = int.Parse(tokens[1]);
                        if (cnt > maxMediaCount)
                        {
                            MessageBox.Show("媒質情報の個数が不正です");
                            return success;
                        }
                        for (int i = 0; i < cnt; i++)
                        {
                            line = sr.ReadLine();
                            if (line.Length == 0)
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
                }
                success = true;
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
                MessageBox.Show(exception.Message);
            }

            return success;
        }

        /// <summary>
        /// Ver1.2.0.0形式の図面ファイルから読み込み(Ver1.2.0.0→Ver1.3.0.0の変換あり)
        /// </summary>
        /// <param name="in_filename"></param>
        /// <param name="isBackupFile">バックアップファイル？</param>
        /// <param name="out_cad2d">格納先Cadオブジェクトのリファレンス</param>
        /// <param name="out_LoopList">ループ情報のリスト</param>
        /// <param name="out_edgeCollectionList">ポートのエッジコレクションのリスト</param>
        /// <param name="out_incidentPortNo">入射ポート番号</param>
        /// <param name="out_medias">媒質情報リストのリファレンス(比誘電率、比透磁率だけセットされる)</param>
        /// <returns></returns>
        public static bool LoadFromFile_Ver_1_2(
            string in_filename,
            out bool isBackupFile,
            ref CCadObj2D out_cad2d,
            ref IList<CadLogic.Loop> out_LoopList,
            ref IList<EdgeCollection> out_edgeCollectionList,
            out int out_incidentPortNo,
            ref MediaInfo[] out_medias
            )
        {
            bool success = false;

            ////////////////////////////////////////////////////////
            // 出力データの初期化

            // バックアップファイル？
            isBackupFile = false;
            // 図面のクリア
            out_cad2d.Clear();
            // ループ情報リストの初期化
            out_LoopList.Clear();
            // 入射ポートの初期化
            out_incidentPortNo = 1;
            // ポートのエッジコレクションのリストを初期化
            out_edgeCollectionList.Clear();
            // 媒質の比誘電率、比透磁率の逆数の初期化
            foreach (MediaInfo media in out_medias)
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

            ////////////////////////////////////////////////////////
            // バックアップファイルの作成
            string filename = "";
            if (in_filename.IndexOf(Ver1_2_Backup_Suffix) >= 0)
            {
                isBackupFile = true;
                MessageBox.Show("このファイルはバックアップファイルです。このファイルを上書き保存しないようご注意ください。",
                    "旧データ形式からの変換", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 入力ファイル名をそのまま利用
                filename = in_filename;
            }
            else
            {
                // 指定されたCadファイルとその入出力データをリネームする
                string basename = Path.GetDirectoryName(in_filename) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(in_filename);
                string inputfilename = basename + Constants.FemInputExt;
                string outputfilename = basename + Constants.FemOutputExt;
                string indexfilename = outputfilename + Constants.FemOutputIndexExt;
                //ReadOnlyにするのを止める
                ////FEM入出力データは移動しない(Ver1.2のアプリで再計算すると落ちるので止めます:データ削除でtry catchしていないのが原因だと思われる)
                //string[] tagtfilenames = { in_filename };
                // 全ファイルを移動する
                string[] tagtfilenames = { in_filename, inputfilename, outputfilename, indexfilename };
                foreach (string tagtfilename in tagtfilenames)
                {
                    string fname = Path.GetFileNameWithoutExtension(tagtfilename);
                    string ext = Path.GetExtension(tagtfilename);
                    if (fname != Path.GetFileNameWithoutExtension(fname)) // .out.idxの場合、ファイル名に.outまで入るので小細工する
                    {
                        string ext2 = Path.GetExtension(fname);
                        string fname2 = Path.GetFileNameWithoutExtension(fname);
                        ext = ext2 + ext;
                        fname = fname2;
                    }
                    string tagtbasename = Path.GetDirectoryName(tagtfilename) + Path.DirectorySeparatorChar + fname;
                    string backupFilename = tagtbasename + Ver1_2_Backup_Suffix + ext;
                    if (File.Exists(tagtfilename))
                    {
                        if (!File.Exists(backupFilename))
                        {
                            // 対象ファイルが存在し、かつバックアップファイルが存在しないとき
                            //バックアップファイルの作成
                            //MyUtilLib.MyUtil.MoveFileWithReadOnly(tagtfilename, backupFilename);
                            try
                            {
                                // そのまま移動（更新時刻等の再設定が面倒なのでコピーでなく移動する)
                                File.Move(tagtfilename, backupFilename);

                                // コピーとして戻す
                                File.Copy(backupFilename, tagtfilename);
                            }
                            catch (Exception exception)
                            {
                                System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
                                MessageBox.Show(exception.Message);
                            }
                        }
                        else
                        {
                            // 対象ファイルは存在し、バックアップが存在するとき
                            // バックアップを作成できない
                            MessageBox.Show("すでに別のバックアップファイルがあるので、バックアップを作成できません。このファイルを上書き保存しないようご注意ください。",
                                "旧データ形式からの変換", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }

                // バックアップファイルに移動したので、バックアップファイルから読み込む
                filename = basename + Ver1_2_Backup_Suffix + Constants.CadExt;
            }

            // Ver1.2.0.0形式のファイルの読み込み
            Size maxDiv;
            bool[,] areaSelection;
            int[,] areaToMediaIndex;
            IList<CadDatFile_Ver_1_2.Edge_Ver_1_2> edgeList_Ver_1_2;
            bool[,] yBoundarySelection;
            bool[,] xBoundarySelection;
            int incidentPortNo_Ver_1_2;
            MediaInfo[] medias_Ver_1_2;

            bool loadSuccess = false;
            loadSuccess = CadDatFile_Ver_1_2.LoadFromFile_Ver_1_2(
                filename, out maxDiv, out areaSelection, out areaToMediaIndex,
                out edgeList_Ver_1_2, out yBoundarySelection, out xBoundarySelection,
                out incidentPortNo_Ver_1_2, out medias_Ver_1_2
                );
            if (!loadSuccess)
            {
                return success;
            }

            /////////////////////////////////////////////////////////////////////////////////
            // 本バージョンへのデータ変換
            /////////////////////////////////////////////////////////////////////////////////

            /////////////////////////////////////////////////////////////////////////////////
            // そのまま使用できる情報
            out_incidentPortNo = incidentPortNo_Ver_1_2;

            /////////////////////////////////////////////////////////////////////////////////
            // 変換が必要な情報
            // Medias --> インデックスを1加算
            // AreaToMediaIndex --> インデックスを1加算
            // AreaSelection, AreaToMediaIndex  --> loop
            // EdgeVer1_2 --> EdgeCollection

            //////////////////////////
            // 媒質情報は、Ver1.2→Ver1.3で媒質インデックスが1つずれる(導体が追加されたため)
            for (int oldMediaIndex = 0; oldMediaIndex < medias_Ver_1_2.Length; oldMediaIndex++)
            {
                if (oldMediaIndex + 1 < out_medias.Length)
                {
                    int mediaIndex = oldMediaIndex + 1;  //インデックスを1加算
                    out_medias[mediaIndex].SetP(medias_Ver_1_2[oldMediaIndex].P);
                    out_medias[mediaIndex].SetQ(medias_Ver_1_2[oldMediaIndex].Q);
                }
            }
            for (int y = 0; y < maxDiv.Height; y++)
            {
                for (int x = 0; x < maxDiv.Width; x++)
                {
                    areaToMediaIndex[y, x]++; //インデックスを1加算
                }
            }

            ///////////////////////////
            // 変換準備
            /*
            // 領域選択情報を媒質情報にマージする(非選択の場合もインデックスは0が設定されているがこれを-1に変更)
            for (int y = 0; y < maxDiv.Height; y++)
            {
                for (int x = 0; x < maxDiv.Width; x++)
                {
                    if (!areaSelection[y, x])
                    {
                        // エリアが選択されていない
                        areaToMediaIndex[y, x] = -1; // 未指定に設定する
                    }
                }
            }
             */
            // 非選択部分は導体媒質を設定する
            for (int y = 0; y < maxDiv.Height; y++)
            {
                for (int x = 0; x < maxDiv.Width; x++)
                {
                    if (!areaSelection[y, x])
                    {
                        // エリアが選択されていない
                        areaToMediaIndex[y, x] = CadLogic.MetalMediaIndex; // 導体を指定する
                    }
                }
            }

            // ループの取得
            IList<DelFEM4NetCom.Pair<int, IList<Point>>> loopList = getLoopList(maxDiv, areaToMediaIndex);

            // ループをエッジに変換
            IList<IList<CadDatFile_Ver_1_2.Edge_Ver_1_2>> loopEdgeListList = getLoopEdgeList(loopList);

            // ポート境界のエッジリスト分、エッジコレクションを先に作成する(複数のループにまたがる場合があるため）
            foreach (CadDatFile_Ver_1_2.Edge_Ver_1_2 portEdge in edgeList_Ver_1_2)
            {
                EdgeCollection edgeCollection = new EdgeCollection();
                edgeCollection.No = portEdge.No;
                out_edgeCollectionList.Add(edgeCollection);
            }

            var newLoopEdgeListList = new List<IList<CadDatFile_Ver_1_2.Edge_Ver_1_2>>();
            // ループ数分チェック
            foreach (IList<CadDatFile_Ver_1_2.Edge_Ver_1_2> loopEdgeList in loopEdgeListList)
            {
                // 1つのループ
                var newLoopEdgeList = new List<CadDatFile_Ver_1_2.Edge_Ver_1_2>();
                foreach (CadDatFile_Ver_1_2.Edge_Ver_1_2 loopEdge in loopEdgeList)
                {
                    // 1つのエッジ

                    // ポート境界を含んでいれば分解する
                    // 頂点リスト
                    IList<Point> vertexPts = new List<Point>();
                    // 頂点にポートが対応していれば、ポート番号を格納
                    // 既定値は -1
                    IList<int> toPortNo = new List<int>();

                    // エッジの最初の頂点を追加
                    vertexPts.Add(loopEdge.Points[0]);
                    toPortNo.Add(-1);

                    // ポート境界のエッジリストを走査
                    foreach (CadDatFile_Ver_1_2.Edge_Ver_1_2 portEdge in edgeList_Ver_1_2)
                    {
                        // contains?
                        Point minPt;
                        Point maxPt;
                        if (loopEdge.Contains(portEdge, out minPt, out maxPt))
                        {
                            // ポート境界が含まれていた
                            // ポート境界の頂点を追加
                            //vertexPts.Add(portEdge.Points[0]); // 始点
                            //vertexPts.Add(portEdge.Points[1]); // 終点
                            vertexPts.Add(minPt); // 始点
                            vertexPts.Add(maxPt); // 終点
                            // ポート番号を頂点にあわせて追加
                            toPortNo.Add(portEdge.No);
                            toPortNo.Add(portEdge.No);
                        }
                        else if (loopEdge.GetSimpleSwap().Contains(portEdge, out minPt, out maxPt))
                        {
                            // ポート境界の頂点を追加
                            //vertexPts.Add(portEdge.Points[1]);  // swap 終点
                            //vertexPts.Add(portEdge.Points[0]);  // swap 始点
                            vertexPts.Add(maxPt);  // swap 終点
                            vertexPts.Add(minPt);  // swap 始点
                            // ポート番号を頂点にあわせて追加
                            toPortNo.Add(portEdge.No);
                            toPortNo.Add(portEdge.No);
                        }
                    }
                    // 最後の頂点を追加
                    vertexPts.Add(loopEdge.Points[1]);
                    toPortNo.Add(-1);

                    // 頂点を元にしてエッジを再構築
                    for (int ino = 0; ino < vertexPts.Count - 1; ino++)
                    {
                        CadDatFile_Ver_1_2.Edge_Ver_1_2 work = new CadDatFile_Ver_1_2.Edge_Ver_1_2();
                        // ポート番号があれば格納
                        if (toPortNo[ino] != -1)
                        {
                            work.No = toPortNo[ino];
                        }
                        else
                        {
                            work.No = 0;
                        }
                        // 辺の始点、終点
                        work.Points[0] = vertexPts[ino];
                        work.Points[1] = vertexPts[ino + 1];
                        // Deltaの計算＆格納
                        Point p1 = work.Points[0];
                        Point p2 = work.Points[1];
                        work.Delta = CadDatFile_Ver_1_2.Edge_Ver_1_2.CalcDelta(p1, p2);

                        // 空チェック
                        if (work.IsEmpty())
                        {
                            // 空の場合追加しない
                        }
                        else
                        {
                            // 追加
                            newLoopEdgeList.Add(work);
                        }
                    }
                    // 1つのエッジ再構築終了

                } // エッジ

                // １つのループのエッジリストをリストに追加
                newLoopEdgeListList.Add(newLoopEdgeList);
            } // ループ

            ///////////////////////////////////////////////////////////////////
            // version1.3.0.0のデータ構造に反映する
            bool errorCCadObj = false;

            // ループ領域を作成
            //uint baseLoopId = 0;
            for (int loopIndex = 0; loopIndex < loopList.Count; loopIndex++)
            {
                System.Diagnostics.Debug.WriteLine("loopIndex: {0}", loopIndex);
                DelFEM4NetCom.Pair<int, IList<Point>> loop = loopList[loopIndex];
                // 媒質インデックス
                int mediaIndex = loop.First;
                // ループを構成する頂点
                IList<Point> pts = loop.Second;

                IList<CVector2D> pps = new List<CVector2D>();
                /*
                foreach (Point pt in pts)
                {
                    double xx = pt.X - maxDiv.Width * 0.5;
                    double yy = maxDiv.Height - pt.Y - maxDiv.Height * 0.5;
                    pps.Add(new CVector2D(xx, yy));
                }
                 */
                // このループのエッジリストを取得する
                IList<CadDatFile_Ver_1_2.Edge_Ver_1_2> loopEdgeList = newLoopEdgeListList[loopIndex];

                // エッジのリストインデックス→エッジコレクションのリストのインデックス変換マップ
                IList<int> loopEdgeIndexToEdgeCollectionIndex = new List<int>();

                // OpenGlの頂点リストを作成
                for (int loopEdgeIndex = 0; loopEdgeIndex < loopEdgeList.Count; loopEdgeIndex++)
                {
                    // エッジ
                    CadDatFile_Ver_1_2.Edge_Ver_1_2 work = loopEdgeList[loopEdgeIndex];

                    ////////////////////////////////
                    // OpenGlの頂点作成
                    double xx;
                    double yy;
                    Point pt;
                    pt = work.Points[0]; //始点を追加していく
                    xx = pt.X - maxDiv.Width * 0.5;
                    yy = maxDiv.Height - pt.Y - maxDiv.Height * 0.5;
                    pps.Add(new CVector2D(xx, yy));
                    System.Diagnostics.Debug.WriteLine("pps[{0}]: {1}, {2}", pps.Count - 1, pps[pps.Count - 1].x, pps[pps.Count - 1].y);

                    // check: 終点は次のエッジの始点のはず
                    if (loopEdgeIndex < loopEdgeList.Count - 1)
                    {
                        CadDatFile_Ver_1_2.Edge_Ver_1_2 next = loopEdgeList[loopEdgeIndex + 1];
                        System.Diagnostics.Debug.Assert(work.Points[1].Equals(next.Points[0]));
                    }

                    /* ループの最後の点は追加しない
                    if (loopEdgeIndex == loopEdgeList.Count - 1)
                    {
                        // 最後だけ終点を追加
                        pt = work.Points[1];
                        xx = pt.X - maxDiv.Width * 0.5;
                        yy = maxDiv.Height - pt.Y - maxDiv.Height * 0.5;
                        pps.Add(new CVector2D(xx, yy));
                    }
                    */

                    int edgeCollectionIndex = -1;
                    if (work.No != 0)
                    {
                        ////////////////////////////////////////////////////////
                        // ポート情報がある場合
                        // check
                        {
                            EdgeCollection edgeCollection = out_edgeCollectionList[work.No - 1];
                            System.Diagnostics.Debug.Assert(work.No == edgeCollection.No);
                        }
                        // エッジコレクションリストのインデックスをセット
                        edgeCollectionIndex = work.No - 1;
                    }
                    // エッジのリストインデックス→エッジコレクションのリストのインデックス変換マップ(ポートがない場合も-1で追加する)
                    loopEdgeIndexToEdgeCollectionIndex.Add(edgeCollectionIndex);
                } // loopEdgeIndex

                uint id_l = 0;
                // 多角形でループを作成するのを止める
                //uint id_l = out_cad2d.AddPolygon(pps, baseLoopId).id_l_add;
                // 自力でループ作成
                System.Diagnostics.Debug.WriteLine("makeLoop: loopIndex: {0}", loopIndex);
                id_l = CadLogic.makeLoop(out_cad2d, pps, out_LoopList, false);
                if (id_l == 0)
                {
                    // 領域分割でできた領域は、現状取り込みを実装していません。
                    MessageBox.Show("領域の追加に失敗しました。");
                    errorCCadObj = true;
                }
                else
                {
                    //////////////////////////////////////////////////////////////////////
                    // 辺と頂点を取得
                    IList<uint> vIdList = null;
                    IList<uint> eIdList = null;
                    CadLogic.GetEdgeVertexListOfLoop(out_cad2d, id_l, out vIdList, out eIdList);
                    // 元々の辺のインデックスに対して生成された辺のIDのリストを要素とするリスト
                    IList<IList<uint>> generatedEIdsList = new List<IList<uint>>();
                    // ループ作成時に辺が分割された場合も考慮
                    int eIdIndexOfs = 0;
                    System.Diagnostics.Debug.WriteLine("pps[0]: {0},{1}", pps[0].x, pps[0].y);
                    {
                        Edge_Ver_1_2 loopEdge0 = loopEdgeList[0];
                        CVector2D loopEdge0_pp_v1;
                        CVector2D loopEdge0_pp_v2;
                        {
                            double xx;
                            double yy;
                            Point pt;
                            pt = loopEdge0.Points[0];
                            xx = pt.X - maxDiv.Width * 0.5;
                            yy = maxDiv.Height - pt.Y - maxDiv.Height * 0.5;
                            loopEdge0_pp_v1 = new CVector2D(xx, yy);
                            pt = loopEdge0.Points[1];
                            xx = pt.X - maxDiv.Width * 0.5;
                            yy = maxDiv.Height - pt.Y - maxDiv.Height * 0.5;
                            loopEdge0_pp_v2 = new CVector2D(xx, yy);
                        }
                        System.Diagnostics.Debug.WriteLine("loopEdge0_pp_v1: {0},{1} loopEdge0_pp_v2: {2},{3}", loopEdge0_pp_v1.x, loopEdge0_pp_v1.y, loopEdge0_pp_v2.x, loopEdge0_pp_v2.y);
                        for (int eIdIndexSearch = 0; eIdIndexSearch < eIdList.Count; eIdIndexSearch++)
                        {
                            uint id_e = eIdList[eIdIndexSearch];
                            bool isIncluding = CadLogic.isEdgeIncludingEdge(out_cad2d, loopEdge0_pp_v1, loopEdge0_pp_v2, id_e);
                            if (isIncluding)
                            {
                                eIdIndexOfs = eIdIndexSearch;
                                break;
                            }
                        }
                    }
                    System.Diagnostics.Debug.WriteLine("eIdIndexOfs:{0}", eIdIndexOfs);

                    for (int loopEdgeIndex = 0; loopEdgeIndex < loopEdgeList.Count; loopEdgeIndex++)
                    {
                        IList<uint> generatedEIds = new List<uint>();
                        Edge_Ver_1_2 loopEdge = loopEdgeList[loopEdgeIndex];
                        CVector2D loopEdge_pp_v1;
                        CVector2D loopEdge_pp_v2;
                        {
                            double xx;
                            double yy;
                            Point pt;
                            pt = loopEdge.Points[0];
                            xx = pt.X - maxDiv.Width * 0.5;
                            yy = maxDiv.Height - pt.Y - maxDiv.Height * 0.5;
                            loopEdge_pp_v1 = new CVector2D(xx, yy);
                            pt = loopEdge.Points[1];
                            xx = pt.X - maxDiv.Width * 0.5;
                            yy = maxDiv.Height - pt.Y - maxDiv.Height * 0.5;
                            loopEdge_pp_v2 = new CVector2D(xx, yy);
                        }
                        System.Diagnostics.Debug.WriteLine("  loopEdgeIndex:{0}", loopEdgeIndex);
                        System.Diagnostics.Debug.WriteLine("    loopEdge_pp_v1: {0},{1} loopEdge0_pp_v2: {2},{3}", loopEdge_pp_v1.x, loopEdge_pp_v1.y, loopEdge_pp_v2.x, loopEdge_pp_v2.y);
                        for (int eIdIndex = 0; eIdIndex < eIdList.Count; eIdIndex++)
                        {
                            uint id_e = eIdList[(eIdIndex + eIdIndexOfs) % eIdList.Count]; // 1つずらして取得
                            //System.Diagnostics.Debug.WriteLine("            {0} id_e:{1}", (eIdIndex + eIdIndexOfs) % eIdList.Count, id_e);
                            bool isIncluding = CadLogic.isEdgeIncludingEdge(out_cad2d, loopEdge_pp_v1, loopEdge_pp_v2, id_e);
                            if (!isIncluding)
                            {
                                continue;
                            }
                            generatedEIds.Add(id_e);
                            // check
                            {
                                uint id_v1 = 0;
                                uint id_v2 = 0;
                                CadLogic.getVertexIdsOfEdgeId(out_cad2d, id_e, out id_v1, out id_v2);
                                CVector2D pp_v1 = out_cad2d.GetVertexCoord(id_v1);
                                CVector2D pp_v2 = out_cad2d.GetVertexCoord(id_v2);
                                System.Diagnostics.Debug.WriteLine("    eId: {0}, pp_v1: {1},{2} pp_v2: {3},{4}", id_e, pp_v1.x, pp_v1.y, pp_v2.x, pp_v2.y);
                            }
                        }
                        generatedEIdsList.Add(generatedEIds);
                    }

                    ///////////////////////////////////////////////////////////////////////
                    // エッジコレクションにポート境界に対応する辺のIDをセットする
                    //IList<EdgeCollection> workEdgeCollectionList = new List<EdgeCollection>();  // ここで辺のIDをセットしたコレクションを辺の色付けのために一時保管する
                    for (int loopEdgeIndex = 0; loopEdgeIndex < loopEdgeList.Count; loopEdgeIndex++)
                    {
                        int edgeCollectionIndex = loopEdgeIndexToEdgeCollectionIndex[loopEdgeIndex];
                        if (edgeCollectionIndex != -1)
                        {
                            //
                            EdgeCollection edgeCollection = out_edgeCollectionList[edgeCollectionIndex];
                            IList<uint> generatedEIds = generatedEIdsList[loopEdgeIndex];
                            foreach (uint id_e in generatedEIds)
                            {
                                // 辺のIDをエッジコレクションにセット
                                // 辺の連続性はここではチェックしない（ばらばらで追加されるので）
                                bool chkFlg = false;
                                if (!edgeCollection.ContainsEdgeId(id_e))
                                {
                                    bool ret = edgeCollection.AddEdgeId(id_e, out_cad2d, chkFlg);
                                }
                            }

                            // 一時保管
                            //workEdgeCollectionList.Add(edgeCollection);
                        }
                    }
                    ////////////////////////////////////////////////////////////////////////
                    // 最初のループならばそのIdを記録する
                    //if (baseLoopId == 0)
                    //{
                    //    baseLoopId = id_l;
                    //}
                    // ループの情報を追加する
                    out_LoopList.Add(new CadLogic.Loop(id_l, mediaIndex));

                    // まだ処理が完全でないので下記は処理しない
                    // ループの内側にあるループを子ループに設定する
                    //CadLogic.reconstructLoopsInsideLoopAsChild(out_cad2d, id_l, ref out_LoopList, ref out_edgeCollectionList, out_medias, ref out_incidentPortNo);

                    ////////////////////////////////////////////////////////////////////////
                    // Cadオブジェクトの色をセットする
                    // ループとその辺、頂点の色をセット
                    MediaInfo media = out_medias[mediaIndex];
                    Color backColor = media.BackColor;
                    CadLogic.SetupColorOfCadObjectsForOneLoop(out_cad2d, id_l, backColor);
                    // ポートの色をセットする
                    //CadLogic.SetupColorOfPortEdgeCollection(out_cad2d, workEdgeCollectionList, incidentPortNo_Ver_1_2);
                } // if id_l != 0
            } // loopIndex

            // 番号順に並び替え
            ((List<EdgeCollection>)out_edgeCollectionList).Sort();
            // エッジコレクションの辺IDリストをソートする
            foreach (EdgeCollection workEdgeCollection in out_edgeCollectionList)
            {
                bool ret = workEdgeCollection.SortEdgeIds(out_cad2d);
            }

            // ポートの色をセットする
            CadLogic.SetupColorOfPortEdgeCollection(out_cad2d, out_edgeCollectionList, out_incidentPortNo);

            /////////////////////////////////////////////
            // 外側の導体媒質ループ削除処理
            //
            // 外側の導体媒質ループを取得する
            IList<CadLogic.Loop> delLoopList = new List<CadLogic.Loop>();
            foreach (CadLogic.Loop loop in out_LoopList)
            {
                if (loop.MediaIndex == CadLogic.MetalMediaIndex) // 導体の場合のみ実行
                {
                    // 外側の判定 --->他のループと共有していない辺が存在する
                    IList<uint> vIdList = null;
                    IList<uint> eIdList = null;
                    CadLogic.GetEdgeVertexListOfLoop(out_cad2d, loop.LoopId, out vIdList, out eIdList);

                    bool notSharedEdgeExists = false;
                    foreach (uint eId in eIdList)
                    {
                        if (CadLogic.isEdgeSharedByLoops(out_cad2d, eId))
                        {
                            // 辺を２つのループで共有している
                        }
                        else
                        {
                            // 辺を共有していない
                            notSharedEdgeExists = true;
                            break;
                        }
                    }
                    if (notSharedEdgeExists)
                    {
                        delLoopList.Add(loop);
                    }
                }
            }
            // 外側の導体媒質ループを削除
            foreach (CadLogic.Loop deltarget in delLoopList)
            {
                CadLogic.delLoop(out_cad2d, deltarget.LoopId, ref out_LoopList, ref out_edgeCollectionList, out_medias, ref out_incidentPortNo);
            }

            if (errorCCadObj)
            {
                success = false;
            }
            else
            {
                success = true;
            }
            return success;
        }

        /// <summary>
        /// ループをエッジに分解
        /// Note: CadDatFile.Edge_Ver1_2の定義と異なる使用方法をとります。
        ///       Points[0]には始点、Points[1]には終点を格納します
        ///       Deltaにはnew Size(1, 0), new Size(0, 1)に加えて、new Size(-1, 0), new Size(0, -1)が格納されます。
        /// </summary>
        /// <param name="loopList"></param>
        /// <returns></returns>
        private static IList<IList<CadDatFile_Ver_1_2.Edge_Ver_1_2>> getLoopEdgeList(IList<DelFEM4NetCom.Pair<int, IList<Point>>> loopList)
        {
            IList<IList<CadDatFile_Ver_1_2.Edge_Ver_1_2>> loopEdgeListList = new List<IList<CadDatFile_Ver_1_2.Edge_Ver_1_2>>();
            // ループをエッジに分解
            // Note: CadDatFile.Edge_Ver1_2の定義と異なる使用方法をとります。
            //       Points[0]には始点、Points[1]には終点を格納します
            //       Deltaにはnew Size(1, 0), new Size(0, 1)に加えて、new Size(-1, 0), new Size(0, -1)が格納されます。
            foreach (DelFEM4NetCom.Pair<int, IList<Point>> loop in loopList)
            {
                // ループの媒質インデックス
                int mediaIndex = loop.First;
                // ループの頂点のリスト  終点＝始点は含まれない
                IList<Point> pts_org = loop.Second;
                IList<Point> pts = new List<Point>();
                // 最後の頂点を追加したリストを作成
                foreach (Point pp in pts_org)
                {
                    pts.Add(pp);
                }
                pts.Add(pts[0]); // 最後に終点=始点を追加

                IList<CadDatFile_Ver_1_2.Edge_Ver_1_2> loopEdgeList = new List<CadDatFile_Ver_1_2.Edge_Ver_1_2>();
                CadDatFile_Ver_1_2.Edge_Ver_1_2 workEdge = null;
                workEdge = new CadDatFile_Ver_1_2.Edge_Ver_1_2();
                workEdge.Delta = new Size(0, 0);
                workEdge.Points[0] = pts[0];
                workEdge.Points[1] = pts[0];
                for (int ino = 1; ino < pts.Count; ino++)
                {
                    Point p1 = workEdge.Points[1];
                    Point p2 = pts[ino];
                    Size delta = CadDatFile_Ver_1_2.Edge_Ver_1_2.CalcDelta(p1, p2);
                    if (workEdge.Delta.Equals(new Size(0, 0)))
                    {
                        workEdge.Delta = delta;
                        workEdge.Points[1] = p2;
                    }
                    else
                    {
                        if (delta.Equals(workEdge.Delta))
                        {
                            workEdge.Points[1] = p2;
                        }
                        else
                        {
                            loopEdgeList.Add(workEdge);

                            workEdge = new CadDatFile_Ver_1_2.Edge_Ver_1_2();
                            //workEdge.Delta = new Size(0, 0);
                            workEdge.Delta = delta;
                            workEdge.Points[0] = p1;
                            workEdge.Points[1] = p2;
                        }
                    }
                    if (ino == pts.Count - 1)
                    {
                        loopEdgeList.Add(workEdge);
                    }
                }
                loopEdgeListList.Add(loopEdgeList);
            }
            return loopEdgeListList;
        }

        /// <summary>
        /// ループを取得する
        /// </summary>
        /// <param name="maxDiv"></param>
        /// <param name="areaToMediaIndex"></param>
        /// <returns></returns>
        private static IList<DelFEM4NetCom.Pair<int, IList<Point>>> getLoopList(Size in_maxDiv, int[,] in_areaToMediaIndex)
        {
            // 空のポイント
            Point emptyPt = new Point(int.MaxValue, int.MaxValue); // Point.Emptyは(0,0)なので使用できない
            // ループのリスト
            IList<DelFEM4NetCom.Pair<int, IList<Point>>> loopList = new List<DelFEM4NetCom.Pair<int, IList<Point>>>();

            // 2倍の領域に拡大する
            // 幅/高さが1の領域があるとループ取得できないので
            int scale = 2;
            Size maxDiv = new Size(in_maxDiv.Width * scale, in_maxDiv.Height * scale);
            int[,] areaToMediaIndex = new int[maxDiv.Height, maxDiv.Width];
            for (int y = 0; y < maxDiv.Height; y++)
            {
                for (int x = 0; x < maxDiv.Width; x++)
                {
                    areaToMediaIndex[y, x] = in_areaToMediaIndex[y / scale, x / scale];
                }
            }

            int mainLoopCounter = 0;
            int searchX = 0;
            int searchY = 0;
            bool mainLoopFlg = true;
            while (mainLoopFlg)
            {
                System.Diagnostics.Debug.WriteLine("mainLoop:{0}", mainLoopCounter);

                Point stPt = emptyPt;
                for (/*searchY = 0*/; searchY < maxDiv.Height; searchY++)
                {
                    for (searchX = 0; searchX < maxDiv.Width; searchX++)
                    {
                        if (areaToMediaIndex[searchY, searchX] != -1)
                        {
                            stPt = new Point(searchX, searchY);
                            break;
                        }
                    }
                    if (stPt != emptyPt)
                    {
                        break;
                    }
                }
                ////////////////////////////
                // 無限ループ終了条件
                if (stPt == emptyPt)
                {
                    mainLoopFlg = false;
                    //MessageBox.Show(string.Format("completed! {0}", mainLoopCounter));
                    System.Diagnostics.Debug.WriteLine("completed! {0}", mainLoopCounter);
                    break;
                }

                ////////////////////////////
                // 境界のループを作成する
                // 境界の頂点のリスト
                IList<Point> pts = new List<Point>();
                // 進んだエリアのリスト
                IList<Point> areas = new List<Point>();
                //このループの媒質インデックスを確定
                int mediaIndex = areaToMediaIndex[stPt.Y, stPt.X];
                // ループ作成開始
                // 自分のいる位置を -1で埋めて
                // 0: X方向へ 1:Y方向へ 2: -X方向へ 3:-Y方向へ移動
                //       3
                //      ↑
                //  2 ←  → 0
                //      ↓
                //       1
                System.Diagnostics.Debug.WriteLine("stPt : {0},{1}", stPt.X, stPt.Y);
                int fillingX = stPt.X;
                int fillingY = stPt.Y;
                {
                    Point[] pp = new Point[3];
                    int cnt = 0;
                    pp[cnt++] = new Point(fillingX + 1, fillingY);
                    pp[cnt++] = new Point(fillingX, fillingY);
                    pp[cnt++] = new Point(fillingX, fillingY + 1);
                    string debugStr = "";
                    for (int ino = 0; ino < cnt; ino++)
                    {
                        pts.Add(pp[ino]);
                        //debugStr += string.Format("{0},{1}  ", pp[ino].X, pp[ino].Y);
                        debugStr += string.Format("{0},{1}  ", pts[pts.Count - 1].X, pts[pts.Count - 1].Y);
                    }
                    System.Diagnostics.Debug.WriteLine(debugStr);
                    areaToMediaIndex[fillingY, fillingX] = -1;
                    areas.Add(new Point(fillingX, fillingY));
                }
                // 最初の３点を別にとっておく。終了判定で使用する
                IList<Point> firstpts = new List<Point>();
                foreach (Point pp in pts)
                {
                    firstpts.Add(pp);
                }

                int prevdirection = 0; // X方向を走査して最初の点を見つけたので、0
                int prevprevdirection = -1;
                bool fillingLoopFlg = true;
                System.Diagnostics.Debug.WriteLine("filling loop start {0},{1}", fillingX, fillingY);
                int fillingLoopCounter = 0;
                while (fillingLoopFlg)
                {
                    //System.Diagnostics.Debug.WriteLine("fillingLoop:{0}", fillingLoopCounter);
                    int prevfillingX = fillingX;
                    int prevfillingY = fillingY;
                    int direction = -1;
                    if (prevdirection == 0)
                    {
                        // 2は前いた場所へ戻る
                        direction = (direction != -1) ? direction : chkDirection(1, mediaIndex, ref areaToMediaIndex, ref fillingX, ref fillingY);
                        direction = (direction != -1) ? direction : chkDirection(0, mediaIndex, ref areaToMediaIndex, ref fillingX, ref fillingY);
                        direction = (direction != -1) ? direction : chkDirection(3, mediaIndex, ref areaToMediaIndex, ref fillingX, ref fillingY);
                        //direction = (direction != -1) ? direction : chkDirection(2, mediaIndex, ref areaToMediaIndex, ref fillingX, ref fillingY);
                    }
                    else if (prevdirection == 1)
                    {
                        // 3は前いた場所へ戻る
                        direction = (direction != -1) ? direction : chkDirection(2, mediaIndex, ref areaToMediaIndex, ref fillingX, ref fillingY);
                        direction = (direction != -1) ? direction : chkDirection(1, mediaIndex, ref areaToMediaIndex, ref fillingX, ref fillingY);
                        direction = (direction != -1) ? direction : chkDirection(0, mediaIndex, ref areaToMediaIndex, ref fillingX, ref fillingY);
                        //direction = (direction != -1) ? direction : chkDirection(3, mediaIndex, ref areaToMediaIndex, ref fillingX, ref fillingY);
                    }
                    else if (prevdirection == 2)
                    {
                        // 0は前いた場所へ戻る
                        direction = (direction != -1) ? direction : chkDirection(3, mediaIndex, ref areaToMediaIndex, ref fillingX, ref fillingY);
                        direction = (direction != -1) ? direction : chkDirection(2, mediaIndex, ref areaToMediaIndex, ref fillingX, ref fillingY);
                        direction = (direction != -1) ? direction : chkDirection(1, mediaIndex, ref areaToMediaIndex, ref fillingX, ref fillingY);
                        //direction = (direction != -1) ? direction : chkDirection(0, mediaIndex, ref areaToMediaIndex, ref fillingX, ref fillingY);
                    }
                    else if (prevdirection == 3)
                    {
                        // 1は前いた場所へ戻る
                        direction = (direction != -1) ? direction : chkDirection(0, mediaIndex, ref areaToMediaIndex, ref fillingX, ref fillingY);
                        direction = (direction != -1) ? direction : chkDirection(3, mediaIndex, ref areaToMediaIndex, ref fillingX, ref fillingY);
                        direction = (direction != -1) ? direction : chkDirection(2, mediaIndex, ref areaToMediaIndex, ref fillingX, ref fillingY);
                        //direction = (direction != -1) ? direction : chkDirection(1, mediaIndex, ref areaToMediaIndex, ref fillingX, ref fillingY);
                    }
                    /////////////////////////// 
                    // 1つのループ作成終了判定(エラーで終了)
                    if (direction == -1)
                    {
                        // 終わり?
                        fillingLoopFlg = false;
                        System.Diagnostics.Debug.WriteLine("cannot find next direction! {0},{1}", fillingX, fillingY);
                        //MessageBox.Show(string.Format("cannot find next direction! {0},{1}", fillingX, fillingY));
                    }
                    else
                    {
                        ///////////////

                        Point[] pp = new Point[3];
                        int cnt = 0;
                        if (direction == 0)
                        {
                            if (prevdirection == 2)
                            {
                                // logic error!
                            }
                            else if (prevdirection == 1)
                            {
                                pp[cnt++] = new Point(fillingX - 1, fillingY);
                                pp[cnt++] = new Point(fillingX - 1, fillingY + 1); // 角
                                pp[cnt++] = new Point(fillingX, fillingY + 1);
                            }
                            else if (prevdirection == 0)
                            {
                                if (prevprevdirection == 2)
                                {
                                    // logic error
                                }
                                if (prevprevdirection == 1 || prevprevdirection == 0)
                                {
                                    pp[cnt++] = new Point(fillingX - 2, fillingY + 1);
                                }
                                else if (prevprevdirection == 3)
                                {
                                    //pp[cnt++] = new Point(fillingX - 1, fillingY + 1);
                                }
                                pp[cnt++] = new Point(fillingX - 1, fillingY + 1);
                                pp[cnt++] = new Point(fillingX, fillingY + 1);
                            }
                            else if (prevdirection == 3)
                            {
                                pp[cnt++] = new Point(fillingX, fillingY + 1); // 角
                                pp[cnt++] = new Point(fillingX + 1, fillingY + 1);
                            }
                        }
                        else if (direction == 1)
                        {
                            if (prevdirection == 3)
                            {
                                // logic error!
                            }
                            else if (prevdirection == 2)
                            {
                                pp[cnt++] = new Point(fillingX + 1, fillingY - 1);
                                pp[cnt++] = new Point(fillingX, fillingY - 1); // 角
                                pp[cnt++] = new Point(fillingX, fillingY);
                            }
                            else if (prevdirection == 1)
                            {
                                if (prevprevdirection == 3)
                                {
                                    // logic error
                                }
                                if (prevprevdirection == 2 || prevprevdirection == 1)
                                {
                                    pp[cnt++] = new Point(fillingX, fillingY - 2); // !!
                                }
                                else if (prevprevdirection == 0)
                                {
                                    //pp[cnt++] = new Point(fillingX, fillingY - 1);
                                }
                                pp[cnt++] = new Point(fillingX, fillingY - 1);
                                pp[cnt++] = new Point(fillingX, fillingY);
                            }
                            else if (prevdirection == 0)
                            {
                                pp[cnt++] = new Point(fillingX, fillingY); // 角
                            }
                        }
                        else if (direction == 2)
                        {
                            if (prevdirection == 0)
                            {
                                // logic error!
                            }
                            else if (prevdirection == 3)
                            {
                                pp[cnt++] = new Point(fillingX + 2, fillingY + 1);
                                pp[cnt++] = new Point(fillingX + 2, fillingY); // 角
                                pp[cnt++] = new Point(fillingX + 1, fillingY);
                            }
                            else if (prevdirection == 2)
                            {
                                if (prevprevdirection == 0)
                                {
                                    // logic error
                                }
                                if (prevprevdirection == 3 || prevprevdirection == 2)
                                {
                                    pp[cnt++] = new Point(fillingX + 3, fillingY); // !!
                                }
                                else if (prevprevdirection == 1)
                                {
                                    //pp[cnt++] = new Point(fillingX + 2, fillingY);
                                }
                                pp[cnt++] = new Point(fillingX + 2, fillingY);
                                pp[cnt++] = new Point(fillingX + 1, fillingY);
                            }
                            else if (prevdirection == 1)
                            {
                                pp[cnt++] = new Point(fillingX + 1, fillingY); // 角
                            }
                        }
                        else if (direction == 3)
                        {
                            if (prevdirection == 1)
                            {
                                // logic error!
                            }
                            else if (prevdirection == 0)
                            {
                                pp[cnt++] = new Point(fillingX, fillingY + 2);
                                pp[cnt++] = new Point(fillingX + 1, fillingY + 2); // 角
                                pp[cnt++] = new Point(fillingX + 1, fillingY + 1);
                            }
                            else if (prevdirection == 3)
                            {
                                if (prevprevdirection == 1)
                                {
                                    // logic error
                                }
                                if (prevprevdirection == 0 || prevprevdirection == 3)
                                {
                                    pp[cnt++] = new Point(fillingX + 1, fillingY + 3);  // !!
                                }
                                else if (prevprevdirection == 2)
                                {
                                    //pp[cnt++] = new Point(fillingX + 1, fillingY + 2);
                                }
                                pp[cnt++] = new Point(fillingX + 1, fillingY + 2);
                                pp[cnt++] = new Point(fillingX + 1, fillingY + 1);
                            }
                            else if (prevdirection == 2)
                            {
                                pp[cnt++] = new Point(fillingX + 1, fillingY + 1); // 角
                            }
                        }
                        string debugStr = "";

                        /////////////////////////// 
                        // 1つのループ作成終了判定
                        bool isGoal = false; // あがり判定
                        int inoGoal = -1;
                        for (int ino = 0; ino < cnt; ino++)
                        {
                            Point first = firstpts[0];
                            if (ino != 0 && first.X == pp[ino].X && first.Y == pp[ino].Y)
                            {
                                isGoal = true;
                                inoGoal = ino;
                            }
                        }

                        for (int ino = 0; ino < cnt; ino++)
                        {
                            if (inoGoal != -1 && ino == inoGoal)
                            {
                                break;
                            }
                            Point prevprev = emptyPt;
                            Point prev = emptyPt;
                            if (pts.Count >= 2)
                            {
                                prevprev = pts[pts.Count - 2];
                            }
                            if (pts.Count >= 1)
                            {
                                prev = pts[pts.Count - 1];
                            }
                            if (!(prevprev.X == pp[ino].X && prevprev.Y == pp[ino].Y) &&
                                !(prev.X == pp[ino].X && prev.Y == pp[ino].Y))
                            {
                                pts.Add(pp[ino]);
                                //debugStr += string.Format("{0},{1}  ", pp[ino].X, pp[ino].Y);
                                debugStr += string.Format("{0},{1}  ", pts[pts.Count - 1].X, pts[pts.Count - 1].Y);
                            }
                        }
                        areaToMediaIndex[fillingY, fillingX] = -1;
                        areas.Add(new Point(fillingX, fillingY));
                        //System.Diagnostics.Debug.Write("    " + new char[] { '→', '↓', '←', '↑' }[direction] + " ");
                        //System.Diagnostics.Debug.WriteLine(debugStr);

                        /////////////////////////// 
                        // 1つのループ作成終了判定
                        if (isGoal)
                        {
                            fillingLoopFlg = false;
                            System.Diagnostics.Debug.WriteLine("filling loop(No.{0}) end with {1},{2}", mainLoopCounter, fillingX, fillingY);
                        }
                    }
                    // 現在の方向を前の方向にセットして、次の点に進む
                    prevprevdirection = prevdirection;
                    prevdirection = direction;
                    fillingLoopCounter++;
                } // fillingLoopFlg

                ////////////////////////////////////////////////////////
                // ループ内の媒質消去が正常ならループをリストに追加する
                // 点をスケール分の1に戻す
                IList<Point> out_pts = new List<Point>();
                foreach (Point pp in pts)
                {
                    Point prev = emptyPt;
                    if (out_pts.Count > 0)
                    {
                        prev = out_pts[out_pts.Count - 1];
                    }
                    int xx = pp.X / scale;
                    int yy = pp.Y / scale;
                    if (prev == emptyPt || (!(prev.X == xx && prev.Y == yy)))
                    {
                        out_pts.Add(new Point(xx, yy));
                    }
                }
                DelFEM4NetCom.Pair<int, IList<Point>> loop = new DelFEM4NetCom.Pair<int, IList<Point>>(mediaIndex, out_pts);
                loopList.Add(loop);
                System.Diagnostics.Debug.WriteLine("loop added");

                // ループ内の媒質を消去
                System.Diagnostics.Debug.WriteLine("erasing ");
                int erasingLoopCount = 0;
                //Stack<Point> eraseAreaStack = new Stack<Point>();
                Stack<string> eraseAreaStack = new Stack<string>(); // ポイントを文字列 X座標_Y座標形式で格納(Containsを使いたいので文字列にした)
                foreach (Point areaLeftTop in areas)
                {
                    string ptstr = string.Format("{0}_{1}", areaLeftTop.X, areaLeftTop.Y);
                    if (!eraseAreaStack.Contains(ptstr))
                    {
                        eraseAreaStack.Push(ptstr);
                    }
                    else
                    {
                        //System.Diagnostics.Debug.WriteLine("contains!!! eraseAreaStack:" + ptstr);
                    }
                    //eraseAreaStack.Push(areaLeftTop);
                    //System.Diagnostics.Debug.WriteLine("eraseAreaStack push:{0},{1}", areaLeftTop.X, areaLeftTop.Y);
                }
                while (eraseAreaStack.Count > 0)
                {
                    //System.Diagnostics.Debug.WriteLine("erasingLoop: {0}", erasingLoopCount);
                    Point eraseArea;
                    string eraseAreaStr = eraseAreaStack.Pop();
                    string[] tokens = eraseAreaStr.Split('_');
                    eraseArea = new Point(int.Parse(tokens[0]), int.Parse(tokens[1]));
                    int eraseX = eraseArea.X;
                    int eraseY = eraseArea.Y;
                    areaToMediaIndex[eraseY, eraseX] = -1;
                    for (int iy = 0; iy < 3; iy++)
                    {
                        int y = eraseArea.Y + iy - 1;
                        if (y < 0 || y >= maxDiv.Height) continue;
                        for (int ix = 0; ix < 3; ix++)
                        {
                            int x = eraseArea.X + ix - 1;
                            if (x < 0 || x >= maxDiv.Width) continue;
                            if (iy == 1 && ix == 1)
                            {
                                // 自分自身
                                continue;
                            }
                            if (areaToMediaIndex[y, x] != -1 && areaToMediaIndex[y, x] == mediaIndex)
                            {
                                string ptstr = string.Format("{0}_{1}", x, y);
                                if (!eraseAreaStack.Contains(ptstr))
                                {
                                    eraseAreaStack.Push(ptstr);
                                }
                                else
                                {
                                    //System.Diagnostics.Debug.WriteLine("contains!!! eraseAreaStack:" + ptstr);
                                }
                                //eraseAreaStack.Push(new Point(x, y));
                                //System.Diagnostics.Debug.WriteLine("eraseAreaStack push:{0},{1}", x, y);
                            }
                        }
                    }
                    erasingLoopCount++;
                }
                System.Diagnostics.Debug.WriteLine("erase done");
                /*
                int minX = int.MaxValue;
                int minY = int.MaxValue;
                int maxX = int.MinValue;
                int maxY = int.MinValue;
                foreach (Point p in pts)
                {
                    if (minX > p.X)
                    {
                        minX = p.X;
                    }
                    if (minY > p.Y)
                    {
                        minY = p.Y;
                    }
                    if (maxX < p.X)
                    {
                        maxX = p.X;
                    }
                    if (maxY < p.Y)
                    {
                        maxY = p.Y;
                    }
                }
                for (int y = minY; y < maxY + 1; y++)
                {
                    if (y < 0 || y >= maxDiv.Height) continue;
                    bool erasing;
                    erasing = false;
                    for (int x = minX; x < maxX + 1; x++)
                    {
                        if (x < 0 || x >= maxDiv.Width) continue;
                        int workmediaIndex = areaToMediaIndex[y, x];
                        if (!erasing && workmediaIndex == -1)
                        {
                            continue;
                        }
                        else if (!erasing && workmediaIndex != -1 && workmediaIndex == mediaIndex)
                        {
                            erasing = true;
                            areaToMediaIndex[y, x] = -1;
                        }
                        else if (erasing && workmediaIndex != -1 && workmediaIndex == mediaIndex)
                        {
                            areaToMediaIndex[y, x] = -1;
                        }
                        else if (erasing && (workmediaIndex == -1 || workmediaIndex != mediaIndex)
                        {
                            // 次の媒質、または電気壁に到達
                            break;
                        }
                    }
                    erasing = false;
                    for (int x = maxX; x >= minX; x--)
                    {
                        if (x < 0 || x >= maxDiv.Width) continue;
                        int workmediaIndex = areaToMediaIndex[y, x];
                        if (!erasing && workmediaIndex == -1)
                        {
                            continue;
                        }
                        else if (!erasing && workmediaIndex != -1 && workmediaIndex == mediaIndex)
                        {
                            erasing = true;
                            areaToMediaIndex[y, x] = -1;
                        }
                        else if (erasing && workmediaIndex != -1 && workmediaIndex == mediaIndex)
                        {
                            areaToMediaIndex[y, x] = -1;
                        }
                        else if (erasing && (workmediaIndex == -1 || workmediaIndex != mediaIndex)
                        {
                            // 次の媒質、または電気壁に到達
                            break;
                        }
                    }
                }
                 */
                mainLoopCounter++;
            }
            return loopList;
        }
        /// <summary>
        /// ループ取得処理で、次に進む方向をチェックする
        /// </summary>
        /// <param name="chkdirection"></param>
        /// <param name="mediaIndex"></param>
        /// <param name="areaToMediaIndex"></param>
        /// <param name="fillingX"></param>
        /// <param name="fillingY"></param>
        /// <returns></returns>
        private static int chkDirection(int chkdirection, int mediaIndex, ref int[,] areaToMediaIndex, ref int fillingX, ref int fillingY)
        {
            int direction = -1;
            int chkX;
            int chkY;
            int maxX = areaToMediaIndex.GetLength(1);
            int maxY = areaToMediaIndex.GetLength(0);
            if (chkdirection == 2)
            {
                //2
                chkX = fillingX - 1;
                chkY = fillingY;
                if (!(0 <= chkX && chkX < maxX && 0 <= chkY && chkY < maxY))
                {
                    return direction;
                }
                if (areaToMediaIndex[chkY, chkX] == mediaIndex)
                {
                    direction = 2;
                    fillingX = chkX;
                    fillingY = chkY;
                }
            }
            if (chkdirection == 1)
            {
                //1
                chkX = fillingX;
                chkY = fillingY + 1;
                if (!(0 <= chkX && chkX < maxX && 0 <= chkY && chkY < maxY))
                {
                    return direction;
                }
                if (areaToMediaIndex[chkY, chkX] == mediaIndex)
                {
                    direction = 1;
                    fillingX = chkX;
                    fillingY = chkY;
                }
            }
            if (chkdirection == 0)
            {
                // 0
                chkX = fillingX + 1;
                chkY = fillingY;
                if (!(0 <= chkX && chkX < maxX && 0 <= chkY && chkY < maxY))
                {
                    return direction;
                }
                if (areaToMediaIndex[chkY, chkX] == mediaIndex)
                {
                    direction = 0;
                    fillingX = chkX;
                    fillingY = chkY;
                }
            }
            if (chkdirection == 3)
            {
                // 3
                chkX = fillingX;
                chkY = fillingY - 1;
                if (!(0 <= chkX && chkX < maxX && 0 <= chkY && chkY < maxY))
                {
                    return direction;
                }
                if (areaToMediaIndex[chkY, chkX] == mediaIndex)
                {
                    direction = 3;
                    fillingX = chkX;
                    fillingY = chkY;
                }
            }
            return direction;
        }
    }
}
