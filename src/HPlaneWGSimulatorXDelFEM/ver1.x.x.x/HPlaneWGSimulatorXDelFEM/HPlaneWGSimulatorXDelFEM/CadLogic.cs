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
    /// Cadロジック
    /// </summary>
    class CadLogic : CadLogicBase
    {
        ////////////////////////////////////////////////////////////////////////
        // 型
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 変更通知デリゲート
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="prevCadMode"></param>
        public delegate void ChangeDeleagte(object sender, CadModeType prevCadMode);

        /// <summary>
        /// 領域選択フラグアレイの思い出具象クラス
        ///   この派生クラスCadLogicでCadBaseのフィールドのポインタを格納すると、MementoCommand実行後、別のものを指してしまうので注意!!!!!
        /// </summary>
        class CadLogicBaseMemento : MyUtilLib.Memento<CadLogicBase, CadLogicBase>, IDisposable
        {
            protected CadLogicBase _MementoData;
            /// <summary>
            /// 
            /// </summary>
            public override CadLogicBase MementoData
            {
                get
                {
                    //Console.WriteLine("CadLogicBaseMemento get MementoData");
                    CadLogicBase data = new CadLogicBase();
                    data.CopyData(_MementoData);
                    //Console.WriteLine("  CadLogicBaseMemento get MementoData done");
                    return data;
                }
                protected set
                {
                    //Console.WriteLine("CadLogicBaseMemento set MementoData");
                    _MementoData.CopyData(value);
                    //Console.WriteLine("  CadLogicBaseMemento set MementoData done");
                }
            }

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="mementoData"></param>
            /// <param name="target"></param>
            public CadLogicBaseMemento(CadLogicBase mementoData, CadLogicBase target)
            {
                //Console.WriteLine("CadLogicBaseMemento Constructor");
                _MementoData = new CadLogicBase();
                //base.MementoData = mementoData;
                _MementoData.CopyData(mementoData);  // コピーされる
                base.Target = target;
                //Console.WriteLine("  CadLogicBaseMemento Constructor done");
            }

            /// <summary>
            /// ターゲットに思い出を反映させる
            /// </summary>
            /// <param name="mementoData"></param>
            public override void SetMemento(CadLogicBase mementoData)
            {
                //Console.WriteLine("CadLogicBaseMemento SetMemento");
                //base.MementoData = mementoData;
                _MementoData.CopyData(mementoData);
                base.Target.CopyData(mementoData);
                //Console.WriteLine("  CadLogicBaseMemento SetMemento done");
            }

            /// <summary>
            /// デストラクタ
            /// </summary>
            ~CadLogicBaseMemento()
            {
                //Console.WriteLine("CadLogicBaseMemento Finalizer");
                Dispose(false);
                //Console.WriteLine("  CadLogicBaseMemento Finalizer done");
            }

            /// <summary>
            /// 破棄
            /// </summary>
            /// <param name="disposing"></param>
            protected void Dispose(bool disposing)
            {
                //Console.WriteLine("CadLogicBaseMemento Dispose {0}", disposing);
                if (_MementoData != null)
                {
                    CadLogicBase data = _MementoData;
                    data.Dispose();
                    _MementoData = null;
                }
                //Console.WriteLine("  CadLogicBaseMemento Dispose {0} done", disposing);
            }

            /// <summary>
            /// 破棄
            /// </summary>
            public void Dispose()
            {
                //Console.WriteLine("CadLogicBaseMemento Dispose");
                Dispose(true);
                GC.SuppressFinalize(this);
                //Console.WriteLine("  CadLogicBaseMemento Dispose done");
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // 定数
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 元に戻す操作を記憶するスタック数
        /// </summary>
        private const int MaxUndoStackCnt = 200;
        /// <summary>
        /// 編集中対象の描画色(ラバーバンド描画に使用)
        ///     Note: ヒットテストのときの色もColor.Yellow(DelFEM側で設定)だが Ver1.2と同じにする
        /// </summary>
        private static readonly Color EditingColor = Color.Yellow;
        /// <summary>
        /// 図形作成中に生成した辺の描画色
        /// </summary>
        private static Color TmpEdgeColor = Color.Black;
        /// <summary>
        /// ポート境界の色
        /// </summary>
        private static readonly Color PortColor = Color.Black;
        /// <summary>
        /// 入射ポートの境界の色
        /// </summary>
        private static readonly Color IncidentPortColor = Color.Cyan;
        /// <summary>
        ///  線の幅
        /// </summary>
        private const int LineWidth = 5;

        ////////////////////////////////////////////////////////////////////////
        // フィールド
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 変更通知イベント
        /// </summary>
        public event ChangeDeleagte Change = null;

        /// <summary>
        /// バックアップファイルから読み込んだ？
        /// </summary>
        public bool IsBackupFile
        {
            get;
            private set;
        }
        /// <summary>
        /// 作成したメッシュ
        /// </summary>
        CMesher2D Mesher2D = null;
        /// <summary>
        /// メッシュを表示する?
        /// </summary>
        public bool MeshDrawFlg
        {
            get;
            set;
        }

        /// <summary>
        /// Cadパネル
        /// </summary>
        private SimpleOpenGlControl CadPanel = null;
        /// <summary>
        /// カメラ
        /// </summary>
        private CCamera Camera = new CCamera();
        /// <summary>
        /// 描画オブジェクトアレイインスタンス
        /// </summary>
        private CDrawerArray DrawerAry = new CDrawerArray();
        /// <summary>
        /// マウス移動位置
        /// </summary>
        private Point MouseMovePt = new Point();
        /// <summary>
        /// キー修飾子 
        /// </summary>
        private Keys KeyModifiers = Keys.None;

        /// <summary>
        /// 方眼紙のサイズ
        /// </summary>
        private static readonly int GraphPaperWidth = Constants.MaxDiv.Width; // マージンなし
        /// <summary>
        /// 方眼紙のX方向分割数
        /// </summary>
        private static readonly int GraphPaperDivX = Constants.MaxDiv.Width;
        /// <summary>
        /// 方眼紙描画オブジェクト
        /// </summary>
        private GraphPaperDrawer GraphPaper = null;
        /// <summary>
        /// 描画オブジェクトアレイの再構築が必要?
        ///   (ヒットテスト以外はEditCad2Dが変更されたらフラグを立てる)
        /// </summary>
        private bool RefreshDrawerAryFlg = false;
        /// <summary>
        /// マウス選択開始ポイント
        /// </summary>
        private Point StartPt;
        /// <summary>
        /// マウス選択終了ポイント
        /// </summary>
        private Point EndPt;
        /// <summary>
        /// ドラッグ中?
        /// </summary>
        private bool DragFlg = false;
        /// <summary>
        /// 移動対称CAD要素タイプ
        /// </summary>
        private CAD_ELEM_TYPE MovElemType = CAD_ELEM_TYPE.NOT_SET;
        /// <summary>
        /// 移動対称要素ID
        /// </summary>
        private uint MovObjId = 0;
        /// <summary>
        /// Cadモード
        /// </summary>
        public CadModeType CadMode
        {
            get { return _CadMode; }
            set 
            {
                CadModeType prevMode = _CadMode;
                if (prevMode != value)
                {
                    if (value == CadModeType.PortNumbering)
                    {
                        // ポート番号振りモードをセットされた場合、番号シーケンスを初期化する
                        PortNumberingSeq = 1;
                    }
                    _CadMode = value;
                }
            }
        }
        /// <summary>
        /// 編集図面Cad
        /// </summary>
        protected CCadObj2D_Move EditCad2D = null;
        /// <summary>
        /// 図形を編集中？
        /// </summary>
        public bool IsEditing
        {
            get
            {
                return (EditPts.Count > 0);
            }
        }
        /// <summary>
        /// ポート番号付与用シーケンス
        /// </summary>
        private int PortNumberingSeq = 1;
        /// <summary>
        /// 作成中ののポート境界番号
        /// Control + LButtonUpで連続した境界を選択
        /// </summary>
        private int EditPortNo = 0;  // 番号にしたのは、この派生クラスCadLogicでCadBaseのフィールドのポインタを格納すると、MementoCommand実行後、別のものを指してしまうため
        /// <summary>
        /// 選択中の媒質インデックス
        /// </summary>
        public int SelectedMediaIndex
        {
            get;
            set;
        }
        /// <summary>
        /// コマンド管理
        /// </summary>
        private MyUtilLib.CommandManager CmdManager = null;
        /// <summary>
        /// CadロジックデータのMemento
        /// </summary>
        private CadLogicBaseMemento CadLogicBaseMmnt = null;
        /// <summary>
        /// Cad図面が変更された?
        /// </summary>
        private bool isDirty = false;
        /// <summary>
        /// Cad図面が変更された?
        /// </summary>
        public bool IsDirty
        {
            get { return isDirty; }
            private set { isDirty = value; }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public CadLogic(SimpleOpenGlControl simpleOpenGlControl)
            : base()
        {
            CadPanel = simpleOpenGlControl;

            // SimpleOpenGlControlの初期化
            CadPanel.InitializeContexts();

            // 色の設定
            CadLogic.TmpEdgeColor = CadPanel.ForeColor;

            // Cadオブジェクトを生成
            EditCad2D = new CCadObj2D_Move();

            // 方眼紙描画オブジェクトを生成
            GraphPaper = new GraphPaperDrawer(GraphPaperWidth, GraphPaperDivX);
            RefreshDrawerAryFlg = true;

            // 領域を決定する
            SetupRegionSize();
            // コマンド管理インスタンスを生成(Undo/Redo用)
            CmdManager = new MyUtilLib.CommandManager(MaxUndoStackCnt);

            // 初期化処理
            init();
        }

        /// <summary>
        /// 描画オブジェクトのリストを更新する
        /// </summary>
        private void refreshDrawerAry()
        {
            if (!RefreshDrawerAryFlg)
            {
                return;
            }
            // ポートの色をセットする
            CadLogic.SetupColorOfPortEdgeCollection(EditCad2D, EdgeCollectionList, IncidentPortNo);

            DrawerAry.Clear();
            // 空の方眼紙を追加する
            DrawerAry.PushBack(GraphPaper);
            // Cad図面
            CDrawer_Cad2D drawer = new CDrawer_Cad2D(EditCad2D);
            uint lineWidth = (uint)(CadLogic.LineWidth * CadPanel.Width / (double)400);
            drawer.SetLineWidth(lineWidth);
            DrawerAry.PushBack(drawer);
            RefreshDrawerAryFlg = false;
        }

        /// <summary>
        /// 領域を決定する
        /// </summary>
        public void SetupRegionSize(double offsetX = 0, double offsetY = 0, double scale = 1.4)
        {
            // 描画オブジェクトを更新する
            refreshDrawerAry();
            // 描画オブジェクトのバウンディングボックスを使ってカメラの変換行列を初期化する
            DrawerAry.InitTrans(Camera);
            // カメラのスケール調整
            // DrawerArrayのInitTransを実行すると、物体のバウンディングボックス + マージン分(×1.5)がとられる。
            // マージンを表示上をなくすためスケールを拡大して調整する
            Camera.SetScale(scale);            
            // カメラをパンニングさせ位置を調整
            Camera.MousePan(0.0, 0.0, offsetX, offsetY);

            int w = CadPanel.Width;
            int h = CadPanel.Height;
            resizeScene(w, h);
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected new void init()
        {
            base.init();

            IsBackupFile = false;
            EditCad2D.Clear();
            EditPortNo = 0;
            CadMode = CadModeType.None;
            SelectedMediaIndex = DefMediaIndex;
            if (Mesher2D != null)
            {
                Mesher2D.Clear();
                Mesher2D.Dispose();
                Mesher2D = null;
            }
            RefreshDrawerAryFlg = true;
            refreshDrawerAry();

            // Mementoの破棄
            if (CadLogicBaseMmnt != null)
            {
                CadLogicBaseMmnt.Dispose();
                CadLogicBaseMmnt = null;
            }
            // Memento初期化
            // 現在の状態をMementoに記憶させる
            setMemento();
            // コマンド管理初期化
            CmdManager.Refresh();
            isDirty = false;
        }

        /// <summary>
        /// データの初期化
        /// </summary>
        public void InitData()
        {
            init();
        }

        /// <summary>
        /// デストラクタ
        /// </summary>
        ~CadLogic()
        {
            Console.WriteLine("-------CadLogic Finalizer-------");
            Dispose(false);
        }

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            Console.WriteLine("-------CadLogic Dispose-------");
            /*修正済み
            // drawerAryのクリアでnative側でインスタンスを削除しているため、マネージ側で削除してはいけないが削除している,DelFEM4Netを修正する必要あり
            foreach (CDrawer drawer in DrawerAry.m_drawer_ary)
            {
                // ファイナライザが走らないように抑制してみる
                GC.SuppressFinalize(drawer);
            }
             */
            if (EditCad2D != null)
            {
                EditCad2D.Clear();
                //if (this is CadLogic)  // DEBUG!!!!!!!
                {
                    Console.WriteLine("      EditCad2D Dispose");
                    EditCad2D.Dispose();
                    Console.WriteLine("      EditCad2D Dispose done");
                }
                EditCad2D = null;
            }
            DrawerAry.Clear();
            if (Mesher2D != null)
            {
                Mesher2D.Clear();
                Mesher2D.Dispose();
                Mesher2D = null;
            }
            //
            CmdManager.Refresh();
            if (CadLogicBaseMmnt != null)
            {
                CadLogicBaseMmnt.Dispose();
                CadLogicBaseMmnt = null;
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// リソース破棄
        /// </summary>
        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// SimpleOpenGlControlのシーンの描画
        /// </summary>
        private void renderScene()
        {
            Color backColor = CadPanel.BackColor;
            //Gl.glClearColor(1.0f, 1.0f, 1.0f, 1.0f);
            Gl.glClearColor(backColor.R / 255.0f, backColor.G / 255.0f, backColor.B / 255.0f, 1.0f);
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
            Gl.glEnable(Gl.GL_DEPTH_TEST);
            Gl.glEnable(Gl.GL_POLYGON_OFFSET_FILL);
            Gl.glPolygonOffset(1.1f, 4.0f);
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glLoadIdentity();
            DrawerGlUtility.SetModelViewTransform(Camera);

            //Console.WriteLine("renderScene:DrawerAry count = {0}", DrawerAry.m_drawer_ary.Count);
            try
            {
                if (MeshDrawFlg && Mesher2D != null)
                {
                    // メッシュ表示
                    using (CDrawerArray meshDrawerAry = new CDrawerArray())
                    {
                        meshDrawerAry.PushBack(GraphPaper);
                        meshDrawerAry.PushBack(new CDrawerMsh2D(Mesher2D));
                        meshDrawerAry.Draw();
                        meshDrawerAry.Clear();
                    }
                }
                else
                {
                    DrawerAry.Draw();
                    drawPortNumberText();
                    drawEditPtsTemporayLine();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message + " " + exception.StackTrace);
                MessageBox.Show(exception.Message);
            }

            Gl.glFlush();
        }

        /// <summary>
        /// CadPanelのリサイズイベントハンドラ
        /// </summary>
        /// <param name="e"></param>
        public void CadPanelResize(EventArgs e)
        {
            Console.WriteLine("CadPanelResize");
            int scrollPosX = CadPanel.AutoScrollPosition.X;
            int scrollPosY = CadPanel.AutoScrollPosition.Y;
            int w = CadPanel.Width;
            int h = CadPanel.Height;
            resizeScene(w, h);
        }

        /// <summary>
        /// SimpleOpenGlControlのリサイズ処理
        /// </summary>
        /// <param name="w"></param>
        /// <param name="h"></param>
        private void resizeScene(int w, int h)
        {
            Camera.SetWindowAspect((double)w / h);
            Gl.glViewport(0, 0, w, h);
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glLoadIdentity();
            DrawerGlUtility.SetProjectionTransform(Camera);

            // 線のサイズを画面に合わせて変更させる為、サイズ変更時にDrawerArrayを更新する
            RefreshDrawerAryFlg = true;
            refreshDrawerAry();
        }

        /// <summary>
        /// マウスで指定したウインドウ座標をOpenGL座標に変換
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public static void ScreenPointToCoord(Point pt, CCamera camera, out double ox, out double oy)
        {
            int[] viewport = new int[4];
            Gl.glGetIntegerv(Gl.GL_VIEWPORT, viewport);
            int winW = viewport[2];
            int winH = viewport[3];

            //モデルビュー行列、射影行列を格納する配列
            double[] modelviewMatrix = new double[16];
            double[] projectionMatrix = new double[16];

            int glY;
            double depth = 0.887; //デプス値(何でもよい)
            Gl.glGetDoublev(Gl.GL_PROJECTION_MATRIX, projectionMatrix);
            Gl.glGetDoublev(Gl.GL_MODELVIEW_MATRIX, modelviewMatrix);
            glY = winH - pt.Y;

            double oz;
            Glu.gluUnProject((double)pt.X, (double)glY, depth,
                modelviewMatrix, projectionMatrix, viewport,
                out ox, out oy, out oz);
            //Console.WriteLine("{0},{1},{2}", ox, oy, oz);
        }

        /// <summary>
        /// マウスで指定したウインドウ座標を正規化デバイス座標に変換
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public static void ScreenPointToNormalizedCoord(Point pt, CCamera camera, out double ox, out double oy)
        {
            ScreenPointToCoord(pt, camera, out ox, out oy);
            double inv_scale = 1.0 / camera.GetScale();
            ox *= inv_scale;
            oy *= inv_scale;
            //Console.WriteLine("NormalizedCoord {0},{1}", ox, oy);
        }


        /// <summary>
        /// OpenGL座標をウインドウ座標に変換
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public static Point CoordToScreenPoint(CCamera camera, double x, double y)
        {
            Point pt = new Point();
            int[] viewport = new int[4];
            Gl.glGetIntegerv(Gl.GL_VIEWPORT, viewport);
            int winW = viewport[2];
            int winH = viewport[3];

            //モデルビュー行列、射影行列を格納する配列
            double[] modelviewMatrix = new double[16];
            double[] projectionMatrix = new double[16];

            double depth = 0.887; //デプス値(何でもよい)
            Gl.glGetDoublev(Gl.GL_PROJECTION_MATRIX, projectionMatrix);
            Gl.glGetDoublev(Gl.GL_MODELVIEW_MATRIX, modelviewMatrix);

            double ox;
            double oy;
            double oz;
            Glu.gluProject((double)x, (double)y, depth,
                projectionMatrix, modelviewMatrix, viewport,
                out ox, out oy, out oz);
            //Console.WriteLine("{0},{1},{2}", ox, oy, oz);
            pt.X = (int)ox;
            pt.Y = (int)(winH - oy);
            return pt;
        }

        /// <summary>
        /// OpenGl文字列描画
        ///   現状Ascii文字のみ
        /// </summary>
        /// <param name="text"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="font"></param>
        /// <param name="color"></param>
        public static void glDrawString(string text, double x, double y, Font font, Color color)
        {
            IntPtr hdc = Wgl.wglGetCurrentDC();
            IntPtr hfont = font.ToHfont();
            Gl.glColor3d(color.R / (double)255, color.G / (double)255, color.B / (double)255);
            IntPtr saveObj = MyUtilLib.WinAPI.SelectObject(hdc, hfont);
            Encoding enc = Encoding.Default;
            //Encoding enc = Encoding.GetEncoding(932);
            byte[] bytes = enc.GetBytes(text);
            int length = bytes.Length;
            int list = Gl.glGenLists(length);
            for (int i = 0; i < length; i++)
            {
                Wgl.wglUseFontBitmaps(hdc, bytes[i], 1, list + i);
            }
            double z = 0.0;
            Gl.glRasterPos3d(x, y, z);
            //ディスプレイリストで描画
            for (int i = 0; i < length; i++)
            {
                Gl.glCallList(list + i);
            }
            //ディスプレイリスト破棄
            Gl.glDeleteLists(list, length);
            list = 0;
            MyUtilLib.WinAPI.SelectObject(hdc, saveObj);
            MyUtilLib.WinAPI.DeleteObject(hfont);
        }
        /// <summary>
        /// ポート番号テキスト表示
        /// </summary>
        /// <param name="g"></param>
        private void drawPortNumberText()
        {
            //Color backColor = Color.Blue;

            // 描画範囲は方眼紙のバウンディングボックス内
            double[] rot = null;
            Camera.RotMatrix33(out rot);
            CBoundingBox3D bb = GraphPaper.GetBoundingBox(rot);

            // ウィンドウの幅、高さの取得
            int[] viewport = new int[4];
            Gl.glGetIntegerv(Gl.GL_VIEWPORT, viewport);
            int winW = viewport[2];
            int winH = viewport[3];
            //int fontSize = 16;
            int fontSize = (int)(16 * winW / (double)400);
            double textSizeX = fontSize * GraphPaperWidth / (double)winW;
            double offsetX = textSizeX;
            //byte gdiCharset = 128; // 0: ANSI 1: DEFAULT 128:SHIFTJIS 
            //using (Font font = new Font("MS UI Gothic", fontSize, FontStyle.Bold, GraphicsUnit.Point, gdiCharset))
            using (Font font = new Font("MS UI Gothic", fontSize, FontStyle.Bold))
            {
                CadLogic.glDrawString(" ", 0, 0, font, Color.White);// 最初の文字が描画されないので暫定対応(空白をダミーで描画)
                foreach (EdgeCollection edge in EdgeCollectionList)
                {
                    if (edge.IsEmpty())
                    {
                        continue;
                    }
                    uint eId = edge.EdgeIds[0];
                    CVector2D pp1 = EditCad2D.GetEdge(eId).GetVtxCoord(true);
                    CVector2D pp2 = EditCad2D.GetEdge(eId).GetVtxCoord(false);
                    double xx;
                    double yy;
                    if (Math.Abs(pp1.x - pp2.x) >= Math.Abs(pp1.y - pp2.y))
                    {
                        xx = ((pp2.x >= pp1.x) ? pp1.x : pp2.x) + offsetX;
                        yy = ((pp2.x >= pp1.x) ? pp1.y : pp2.y) + offsetX / 2;
                    }
                    else
                    {
                        xx = ((pp2.y >= pp1.y) ? pp2.x : pp1.x) + offsetX / 2;
                        yy = ((pp2.y >= pp1.y) ? pp2.y : pp1.y) - offsetX * 2;
                    }
                    CVector2D drawpp = new CVector2D(xx, yy);
                    //CVector2D drawpp = new CVector2D(pp1.x - textSizeX, pp1.y - textSizeX);
                    //CVector2D drawpp = new CVector2D((pp1.x * 9 + pp2.x) / 10, (pp1.y * 9 + pp2.y) / 10);
                    if (drawpp.x < bb.x_min)
                    {
                        drawpp.x = bb.x_min;
                    }
                    if (drawpp.x >= bb.x_max - textSizeX)
                    {
                        drawpp.x = bb.x_max - textSizeX;
                    }
                    if (drawpp.y < bb.y_min)
                    {
                        drawpp.y = bb.y_min;
                    }
                    if (drawpp.y >= bb.y_max - textSizeX)
                    {
                        drawpp.y = bb.y_max - textSizeX;
                    }
                    /*
                    Point drawPt = CoordToScreenPoint(Camera, drawpp.x, drawpp.y);
                    byte[] backColorBytes = new byte[4];
                    Gl.glReadPixels((int)drawpp.x, (int)drawpp.y, 1, 1, (int)Gl.GL_RGBA, (int)Gl.GL_UNSIGNED_BYTE, backColorBytes);
                    Color backColor = Color.FromArgb(backColorBytes[0], backColorBytes[1], backColorBytes[2]);
                    */
                    //Color backColor = Color.White;
                    Color textColor;
                    string text = string.Format("{0}", edge.No);
                    textColor = Color.LightBlue;
                    CadLogic.glDrawString(text, drawpp.x, drawpp.y, font, textColor);
                    //textColor = Color.FromArgb(0xff & (backColor.R + 0x40), 0xff & (backColor.G + 0x40), 0xff & (backColor.B + 0x40));
                    textColor = Color.Black;
                    CadLogic.glDrawString(text, drawpp.x + 0.1, drawpp.y - 0.1, font, textColor);
                }
            }
        }

        /// <summary>
        /// 図形作成途中の一時的な線分を描画
        /// </summary>
        private void drawEditPtsTemporayLine()
        {
            Point pt = MouseMovePt;
            double x;
            double y;
            ScreenPointToCoord(pt, Camera, out x, out y);
            if (EditPts.Count > 0)
            {
                Color lineColor = EditingColor;
                int lineWidth = 4;
                double z = 15;
                Gl.glColor3d(lineColor.R / (double)255, lineColor.G / (double)255.0, lineColor.B / (double)255);
                Gl.glLineWidth(lineWidth);
                Gl.glBegin(Gl.GL_LINES);
                Gl.glVertex3d(EditPts[EditPts.Count - 1].x, EditPts[EditPts.Count - 1].y, z);
                Gl.glVertex3d(x, y, z);
                Gl.glEnd();
            }
        }

        /// <summary>
        /// Cadパネル描画イベント処理
        /// </summary>
        /// <param name="g"></param>
        public void CadPanelPaint(Graphics g)
        {
            //Console.WriteLine("CadPanelPaint");
            renderScene();
        }

        /// <summary>
        /// マウスクリックイベント処理
        /// </summary>
        /// <param name="e"></param>
        public void CadPanelMouseClick(MouseEventArgs e)
        {

        }

        /// <summary>
        /// マウスダウンイベント処理
        /// </summary>
        /// <param name="e"></param>
        public void CadPanelMouseDown(MouseEventArgs e)
        {
            bool executed = false;

            if (e.Button == MouseButtons.Left)
            {
                executed = mouseLeftButtonDown(e);
            }
            else if (e.Button == MouseButtons.Right)
            {
                executed = mouseRightButtonDown(e);
            }

            if (executed)
            {
                // 描画オブジェクトアレイを更新する
                refreshDrawerAry();
                // Cadパネルの再描画
                CadPanel.Invalidate();
            }
        }

        /// <summary>
        /// マウス左ボタンが押された時の処理
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool mouseLeftButtonDown(MouseEventArgs e)
        {
            bool executed = false;
            Point pt = e.Location;

            DragFlg = true;
            StartPt = e.Location;
            EndPt = StartPt;

            if (CadMode == CadModeType.MoveObj)
            {
                CAD_ELEM_TYPE partElemType;
                uint partId;
                bool hit = hitTest(pt, out partElemType, out partId);
                if (hit)
                {
                    MovElemType = partElemType;
                    MovObjId = partId;
                }
                executed = true; // 常に実行される
            }


            return executed;
        }

        /// <summary>
        /// マウス左ボタンが押された時の処理
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool mouseRightButtonDown(MouseEventArgs e)
        {
            bool executed = false;
            Point pt = e.Location;


            return executed;
        }

        /// <summary>
        /// マウス移動イベント処理
        /// </summary>
        /// <param name="e"></param>
        public void CadPanelMouseMove(MouseEventArgs e)
        {
            bool executed = false;
            Point pt = e.Location;
            Point prevpt = MouseMovePt;
            MouseMovePt = pt;
            EndPt = e.Location;

            /*
            if (KeyModifiers.HasFlag(Keys.Control))
            {
                MouseRotation(prevpt, pt);
            }
            else if (KeyModifiers.HasFlag(Keys.Shift))
            {
                MousePan(prevpt, pt);
            }
            */
            if (CadMode == CadModeType.MoveObj)
            {
                if (DragFlg)
                {
                    // Cadオブジェクトの移動
                    executed = doMoveObject(true, ref StartPt, EndPt);
                }
                else
                {
                    CAD_ELEM_TYPE partElemType;
                    uint partId;
                    bool hit = hitTest(pt, out partElemType, out partId);
                    executed = true; // 常に実行される
                }

            }
            else if (CadMode != CadModeType.None)
            {
                CAD_ELEM_TYPE partElemType;
                uint partId;
                bool hit = hitTest(pt, out partElemType, out partId);
                executed = true; // 常に実行される
            }

            if (executed)
            {
                // 描画オブジェクトアレイを更新する
                refreshDrawerAry();
                // Cadパネルの再描画
                CadPanel.Invalidate();
            }
        }

        /// <summary>
        /// マウスによる物体の移動/拡大に使用する移動量計測用の点の位置
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="movX"></param>
        /// <param name="movY"></param>
        private static void getMovPt(Point pt, out double movX, out double movY)
        {
            // 正規化座標 ; 回転が入るとマウスの移動方向と正規化座標の方向が逆転する等、移動量としては使えない
            //CadLogic.ScreenPointToNormalizedCoord(pt, camera, out movX, out movY);

            // ウィンドウの幅、高さの取得
            int[] viewport = new int[4];
            Gl.glGetIntegerv(Gl.GL_VIEWPORT, viewport);
            int winW = viewport[2];
            int winH = viewport[3];
            // 移動量計測用のポイントに変換する
            movX = (2.0 * pt.X - winW) / (double)winW;
            movY = (winH - 2.0 * pt.Y) / (double)winH;
        }

        /// <summary>
        /// カメラを水平方向に回転させる(パン:Panoramic Viewing = パノラマのように見る)
        /// </summary>
        /// <param name="prevpt"></param>
        /// <param name="pt"></param>
        public void MousePan(Point prevpt, Point pt)
        {
            double movBeginX;
            double movBeginY;
            double movEndX;
            double movEndY;
            getMovPt(prevpt, out movBeginX, out movBeginY);
            getMovPt(pt, out movEndX, out movEndY);
            Camera.MousePan(movBeginX, movBeginY, movEndX, movEndY);
            //Gl.glFlush();
            CadPanel.Refresh();
        }

        /// <summary>
        /// 回転
        /// </summary>
        /// <param name="prevpt"></param>
        /// <param name="pt"></param>
        public void MouseRotation(Point prevpt, Point pt)
        {
            double movBeginX;
            double movBeginY;
            double movEndX;
            double movEndY;
            getMovPt(prevpt, out movBeginX, out movBeginY);
            getMovPt(pt, out movEndX, out movEndY);
            Camera.MouseRotation(movBeginX, movBeginY, movEndX, movEndY);
            //Gl.glFlush();
            CadPanel.Refresh();
        }

        /// <summary>
        /// マウスアップイベント処理
        /// </summary>
        /// <param name="e"></param>
        public void CadPanelMouseUp(MouseEventArgs e)
        {
            bool executed = false;

            if (e.Button == MouseButtons.Left)
            {
                executed = mouseLeftButtonUp(e);
            }
            else if (e.Button == MouseButtons.Right)
            {
                executed = mouseRightButtonUp(e);
            }

            if (executed)
            {
                // 描画オブジェクトアレイを更新する
                refreshDrawerAry();
                // Cadパネルの再描画
                CadPanel.Invalidate();
            }
        }

        /// <summary>
        /// マウス左ボタンが離された時の処理
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool mouseLeftButtonUp(MouseEventArgs e)
        {
            bool executed = false;
            Point pt = e.Location;
            EndPt = e.Location;
            DragFlg = false;
            /*
            Point minPt = new Point();
            Point maxPt = new Point();
            if (StartPt.X <= EndPt.X)
            {
                minPt.X = StartPt.X;
                maxPt.X = EndPt.X;
            }
            else
            {
                minPt.X = EndPt.X;
                maxPt.X = StartPt.X;
            }
            if (StartPt.Y <= EndPt.Y)
            {
                minPt.Y = StartPt.Y;
                maxPt.Y = EndPt.Y;
            }
            else
            {
                minPt.Y = EndPt.Y;
                maxPt.Y = StartPt.Y;
            }
             */

            // Cadオブジェクトの移動
            executed = doMoveObject(false, ref StartPt, EndPt);
            if (!executed)
            {
                // 領域作成
                executed = doMakeDisconArea(pt);
            }
            if (!executed)
            {
                // 領域削除
                executed = doEraseDisconArea(pt);
            }
            if (!executed)
            {
                // 媒質埋め込み
                executed = doFillMedia(pt);
            }
            if (!executed)
            {
                // ポート追加
                executed = doSelectPort(pt); 
            }
            if (!executed)
            {
                // ポート削除
                executed = doErasePort(pt);
            }
            if (!executed)
            {
                // 辺削除
                executed = doEraseCadEdge(pt);
            }
            if (!executed)
            {
                // ポートの番号振り
                executed = doNumberingPort(pt);
            }
            if (!executed)
            {
                // 入射ポートの選択
                executed = doSelectIncidentPort(pt);
            }

            return executed;
        }

        /// <summary>
        /// マウス右ボタンが離された時の処理
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool mouseRightButtonUp(MouseEventArgs e)
        {
            bool executed = false;

            return executed;
        }

        /// <summary>
        /// キーダウンイベント処理
        /// </summary>
        /// <param name="e"></param>
        public void CadPanelKeyDown(KeyEventArgs e)
        {
            KeyModifiers = e.Modifiers;

            /*
            if (e.KeyCode == Keys.PageUp)
            {
                if (e.Modifiers.HasFlag(Keys.Shift))
                {
                    if (Camera.IsPers())
                    {
                        double tmp_fov_y = Camera.GetFovY() + 10.0;
                        Camera.SetFovY(tmp_fov_y);
                    }
                }
                else
                {
                    double tmp_scale = Camera.GetScale() * 0.9;
                    Camera.SetScale(tmp_scale);
                }
            }
            else if (e.KeyCode == Keys.PageDown)
            {
                if (e.Modifiers.HasFlag(Keys.Shift))
                {
                    if (Camera.IsPers())
                    {
                        double tmp_fov_y = Camera.GetFovY() - 10.0;
                        Camera.SetFovY(tmp_fov_y);
                    }
                }
                else
                {
                    double tmp_scale = Camera.GetScale() * 1.111;
                    Camera.SetScale(tmp_scale);
                }
            }
            else if (e.KeyCode == Keys.Home)
            {
                DrawerAry.InitTrans(Camera);
                Camera.Fit();
            }
            else if (e.KeyCode == Keys.End)
            {
                if(Camera.IsPers())
                {
                    Camera.SetIsPers(false);
                }
                else
                {
                    Camera.SetIsPers(true);
                }
            }
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glLoadIdentity();
            DrawerGlUtility.SetProjectionTransform(Camera);
            CadPanel.Invalidate();
             */
        }

        /// <summary>
        /// キーを押したままのとき発生するイベント処理
        /// </summary>
        /// <param name="e"></param>
        public void CadPanelKeyPress(KeyPressEventArgs e)
        {
        }

        /// <summary>
        /// キーアップイベント処理
        /// </summary>
        /// <param name="e"></param>
        public void CadPanelKeyUp(KeyEventArgs e)
        {
            KeyModifiers = e.Modifiers;

            if (!KeyModifiers.HasFlag(Keys.Control))  // Controlキーが離された
            {
                if (EditPortNo != 0)
                {
                    EditPortNo = 0;
                    Console.WriteLine("EditPortNo cleared!");
                }
            }
        }

        /// <summary>
        /// Cadオブジェクトの移動
        /// </summary>
        /// <param name="elemType"></param>
        /// <param name="objId"></param>
        /// <param name="screenPt"></param>
        private static bool moveObject(CCadObj2D_Move EditCad2D, CCamera Camera, CAD_ELEM_TYPE elemType, uint objId, Point startPt, Point endPt)
        {
            bool executed = false;
            if (objId == 0)
            {
                return executed;
            }
            if (elemType == CAD_ELEM_TYPE.VERTEX)
            {
                double ox = 0.0;
                double oy = 0.0;
                CadLogic.ScreenPointToCoord(endPt, Camera, out ox, out oy);
                uint id_v = objId;
                bool ret = EditCad2D.MoveVertex(id_v, new CVector2D(ox, oy));
                if (ret)
                {
                    executed = true;
                }
                else
                {
                    Console.WriteLine("failed: MoveVertex {0}, {1}, {2}", id_v, ox, oy);
                }
            }
            else if (elemType == CAD_ELEM_TYPE.EDGE)
            {
                double movBeginX = 0.0;
                double movBeginY = 0.0;
                double movEndX = 0.0;
                double movEndY = 0.0;
                CadLogic.ScreenPointToCoord(startPt, Camera, out movBeginX, out movBeginY);
                CadLogic.ScreenPointToCoord(endPt, Camera, out movEndX, out movEndY);
                uint id_e = objId;
                bool ret = EditCad2D.MoveEdge(id_e, new CVector2D(movEndX - movBeginX, movEndY - movBeginY));
                if (ret)
                {
                    executed = true;
                }
                else
                {
                    Console.WriteLine("failed: MoveEdge {0}, {1}, {2}", id_e, movEndX - movBeginX, movEndY - movBeginY);
                }
            }
            else if (elemType == CAD_ELEM_TYPE.LOOP)
            {
                double movBeginX = 0.0;
                double movBeginY = 0.0;
                double movEndX = 0.0;
                double movEndY = 0.0;
                CadLogic.ScreenPointToCoord(startPt, Camera, out movBeginX, out movBeginY);
                CadLogic.ScreenPointToCoord(endPt, Camera, out movEndX, out movEndY);
                uint id_l = objId;
                bool ret = EditCad2D.MoveLoop(id_l, new CVector2D(movEndX - movBeginX, movEndY - movBeginY));
                if (ret)
                {
                    executed = true;
                }
                else
                {
                    Console.WriteLine("failed: MoveLoop {0}, {1}, {2}", id_l, movEndX - movBeginX, movEndY - movBeginY);
                }
            }
            return executed;
        }

        /// <summary>
        /// Cadオブジェクト移動処理
        /// </summary>
        /// <param name="isDragging"></param>
        /// <returns></returns>
        private bool doMoveObject(bool isDragging, ref Point stPt, Point edPt)
        {
            bool executed = false;
            if (CadMode == CadModeType.MoveObj)
            {
                // Cadオブジェクトの移動
                executed = moveObject(EditCad2D, Camera, MovElemType, MovObjId, stPt, edPt);
                stPt = edPt;
                if (executed)
                {
                    if (isDragging)
                    {
                        // Undo対象にはしない,自動計算もしない
                        // 描画オブジェクトアレイの更新フラグを立てる
                        RefreshDrawerAryFlg = true;
                    }
                    else
                    {
                        // コマンドを実行する
                        invokeCadOperationCmd();
                        //  Note:ここでCadObjが新しくなるのでrefreshDrawerAry()をこの後実行すること
                        // 描画オブジェクトアレイの更新フラグを立てる
                        RefreshDrawerAryFlg = true;
                        if (Change != null)
                        {
                            Change(this, CadMode);
                        }
                    }
                }
                if (executed && !isDirty)
                {
                    isDirty = true;
                }
            }
            return executed;
        }

        /// <summary>
        /// 領域を作成する
        ///   領域の頂点追加/領域確定
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private bool doMakeDisconArea(Point pt)
        {
            bool executed = false;
            if (CadMode != CadModeType.Area)
            {
                return executed;
            }

            // マス目の長さ(デバイス座標系)
            double delta = GraphPaperWidth / (double)GraphPaperDivX;

            // ウィンドウの幅、高さ、openGLクリッピング座標の幅、高さ
            //int[] viewport = new int[4];
            //Gl.glGetIntegerv(Gl.GL_VIEWPORT, viewport);
            //int winW = viewport[2];
            //int winH = viewport[3];
            //double hw;
            //double hh;
            //double hd;
            //Camera.GetOrtho(out hw, out hh, out hd);
            //double th = 5 * (2.0 * hw) / winW; // 5ピクセル
            double th = delta * 0.5;

            // スクリーンの位置をデバイス座標に変換
            double x;
            double y;
            CadLogic.ScreenPointToCoord(pt, Camera, out x, out y);

            // 方眼紙のバウンディングボックスを取得する
            double[] rot = null;
            Camera.RotMatrix33(out rot);
            using (CBoundingBox3D bb = GraphPaper.GetBoundingBox(rot))
            using (CBoundingBox3D bb2 = new CBoundingBox3D(bb.x_min, bb.x_max, bb.y_min, bb.y_max, bb.z_min, bb.z_max))
            {
                // すこしだけ大きくする
                bb2.x_max += th;
                bb2.x_min -= th;
                bb2.y_max += th;
                bb2.y_min -= th;
                // 方眼紙内部かどうかチェックする
                if (!bb2.IsInside(new CVector3D(x, y, bb2.z_min)))
                {
                    // 方眼紙の内部でない
                    return executed;
                }
                // 領域をはみ出している可能性があるので、元のバウンディングボックスで範囲外のものを修正
                if (x < bb.x_min)
                {
                    x = bb.x_min;
                }
                if (x > bb.x_max)
                {
                    x = bb.x_max;
                }
                if (y < bb.y_min)
                {
                    y = bb.y_min;
                }
                if (y > bb.y_max)
                {
                    y = bb.y_max;
                }
            }
            // 方眼紙のマス目の頂点に合わせる
            x = Math.Round(x / delta) * delta;
            y = Math.Round(y / delta) * delta;
            CVector2D pp = new CVector2D(x, y);

            // 追加されたループIDを格納するリスト
            IList<uint> addLoopIds = new List<uint>();

            // これから追加する点の事前チェック
            bool handled = false; // 処理した？
            {
                // 編集中のCadの頂点や辺はヒットテストで捕捉する
                // ヒットテスト実行
                CAD_ELEM_TYPE partElemType = CAD_ELEM_TYPE.NOT_SET;
                uint partId = 0;
                //BUGFIX
                //ヒットテストは修正された位置で行う必要がある
                //hitTest(pt, out partElemType, out partId);
                Point ptModified = CoordToScreenPoint(Camera, pp.x, pp.y);
                hitTest(ptModified, out partElemType, out partId);
                if (partId != 0 && partElemType == CAD_ELEM_TYPE.EDGE)
                {
                    uint parentEdgeId = partId;
                    uint id_v_s;
                    uint id_v_e;
                    EditCad2D.GetIdVertex_Edge(out id_v_s, out id_v_e, parentEdgeId);
                    if (EditVertexIds.IndexOf(id_v_s) >= 0 ||
                        EditVertexIds.IndexOf(id_v_e) >= 0)
                    {
                        // 編集中の辺上に頂点を追加しようとした
                        MessageBox.Show("編集中の辺上に頂点を追加できません。", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        executed = false;
                        return executed;
                    }
                    else
                    {
                        /////////////////////////////////////////////////
                        // 孤立した辺1かどうかをチェックする

                        // ループに属している辺の辺上にあるかどうかチェックし、辺上ならば辺IDを返す
                        uint hit_eId = getEdgeIdIncludingPoint(EditCad2D, pp, LoopList);
                        if (hit_eId != 0)
                        {
                            // ループに属している辺の辺上
                            // 通常の処理を行う
                            System.Diagnostics.Debug.Assert(partId == hit_eId);
                        }
                        else
                        {
                            /////////////////////////////////////////////////
                            // 管理外の（ループを形成していない)頂点
                            // イレギュラーな処理

                            // 通常の頂点の追加処理はできないので処理済みにする
                            handled = true;

                            // 孤立した辺の処理を行う(頂点の追加)
                            hit_eId = partId;  // !!!!!!!
                            int index_pp = EditPts.Count;
                            executed = addVertexAndEdgeAtStandAloneEdge(EditCad2D, pp, hit_eId, index_pp, LoopList,
                                ref EditPts, ref EditVertexIds, ref EditEdgeIds, ref addLoopIds, true);
                        }

                    }
                }
                else if (partId != 0 && partElemType == CAD_ELEM_TYPE.VERTEX)
                {
                    if (EditPts.Count >= 3 && CVector2D.Distance(EditPts[0], pp) < th)
                    {
                        // ループ確定
                        pp = EditPts[0]; //!!!!!!1
                    }
                    else
                    {
                        // 編集中の頂点上？
                        bool isEditingVertex = false;
                        foreach (CVector2D work_pp in EditPts)
                        {
                            if (CVector2D.Distance(work_pp, pp) < th)
                            {
                                isEditingVertex = true;
                                break;
                            }
                        }
                        if (isEditingVertex)
                        {
                            // 編集中の頂点上に頂点を追加しようとした
                            MessageBox.Show("編集中の頂点上に頂点を追加できません。", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            executed = false;
                            return executed;
                        }
                        else
                        {
                            /////////////////////////////////////////////////
                            // 孤立した頂点かどうかをチェックする

                            // 前に追加したループと共有している頂点かチェックする
                            IList<uint> hit_loopIdList = null;
                            uint hit_vId = 0;
                            CadLogic.getVertexIdBelongToLoopByCoord(EditCad2D, pp, LoopList, out hit_vId, out hit_loopIdList);
                            if (hit_vId != 0 && hit_loopIdList != null && hit_loopIdList.Count > 0)
                            {
                                // 前に追加したループと共有している点
                                // 通常処理を行う
                                System.Diagnostics.Debug.Assert(partId == hit_vId);
                            }
                            else
                            {
                                /////////////////////////////////////////////////
                                // 管理外の（ループを形成していない)頂点
                                // イレギュラーな処理

                                // 通常の辺の追加処理はできないので処理済みにする
                                handled = true;
                                
                                // 孤立した頂点の処理を行う(辺の追加)
                                hit_vId = partId;  // !!!!!!!
                                int index_pp = EditPts.Count;
                                executed = addEdgeConnectedToStandAloneVertex(EditCad2D, pp, hit_vId, index_pp, LoopList,
                                    ref EditPts, ref EditVertexIds, ref EditEdgeIds, ref addLoopIds, true);
                            }
                        }
                    }
                }
            }

            /////////////////
            if (!handled)
            {
                // 以下の処理は孤立した頂点、辺を考慮していません。
                // それらはイレギュラーな処理としてすでに処理済みとします。（ここにはこないようにする）
                int index_pp = EditPts.Count; // これから追加する点のインデックス
                executed = doMakeDisconAreaCore(EditCad2D, pp, index_pp, LoopList,
                    ref EditPts, ref EditVertexIds, ref EditEdgeIds, ref addLoopIds, true);
            }
            if (addLoopIds.Count > 0)
            {
                //MessageBox.Show("領域の分割確定");
                foreach (uint id_l in addLoopIds)
                {
                    //// ループの色を指定（指定しなければ(0.9,0.8,0.8)になる
                    //MediaInfo media = Medias[SelectedMediaIndex];
                    //Color backColor = media.BackColor;
                    //SetupColorOfCadObjectsForOneLoop(EditCad2D, id_l, backColor);
                    // ループ情報の追加
                    LoopList.Add(new Loop(id_l, SelectedMediaIndex));

                    // ループの内側にあるループを子ループに設定する
                    reconstructLoopsInsideLoopAsChild(EditCad2D, id_l, ref LoopList, ref EdgeCollectionList, Medias, ref IncidentPortNo);

                    //ループの色をすべて再設定する
                    SetupColorOfCadObjectsForAllLoops(EditCad2D, LoopList, Medias);

                    executed = true;
                }
                //
                EditPts.Clear();
                EditVertexIds.Clear();
                EditEdgeIds.Clear();
            }

            if (executed)
            {
                // コマンドを実行する
                invokeCadOperationCmd();
                //  Note:ここでCadObjが新しくなるのでrefreshDrawerAry()をこの後実行すること
                // 描画オブジェクトアレイの更新フラグを立てる
                RefreshDrawerAryFlg = true;
                if (Change != null)
                {
                    Change(this, CadMode);
                }

            }
            if (executed && !isDirty)
            {
                isDirty = true;
            }

            return executed;
        }

        /// <summary>
        /// 孤立した頂点と接続して辺を作成する
        /// </summary>
        /// <param name="EditCad2D"></param>
        /// <param name="pp"></param>
        /// <param name="hit_vId"></param>
        /// <param name="index_pp"></param>
        /// <param name="LoopList"></param>
        /// <param name="EditPts"></param>
        /// <param name="EditVertexIds"></param>
        /// <param name="EditEdgeIds"></param>
        /// <param name="addLoopIds"></param>
        /// <param name="showErrorFlg"></param>
        /// <returns></returns>
        public static bool addEdgeConnectedToStandAloneVertex(CCadObj2D EditCad2D, CVector2D pp, uint hit_vId, int index_pp, IList<Loop> LoopList,
            ref IList<CVector2D> EditPts, ref IList<uint> EditVertexIds, ref IList<uint> EditEdgeIds, ref IList<uint> addLoopIds, bool showErrorFlg)
        {
            bool executed = false;
            uint parentLoopId = 0;
            int addedCnt = 0;
            // 頂点は作成せず、辺だけ作成する
            // 辺の作成に関して、既存の辺が存在するかチェック
            uint existEId = 0;
            {
                // １つ前の点に関して、前に追加したループと共有している頂点かチェックする
                IList<uint> prev_hit_loopIdList = null;
                uint prev_hit_vId = 0;
                if (index_pp >= 1)
                {
                    //CVector2D prevPt = pps[index_pp - 1];
                    CVector2D prevPt = EditPts[EditPts.Count - 1];
                    CadLogic.getVertexIdBelongToLoopByCoord(EditCad2D, prevPt, LoopList, out prev_hit_vId, out prev_hit_loopIdList);
                    if (prev_hit_vId != 0)
                    {
                        existEId = getEdgeIdOfVertexIds(EditCad2D, hit_vId, prev_hit_vId);
                    }
                }
            }

            // 頂点は作成しない。リストに追加するだけ。
            EditPts.Add(pp);
            addedCnt++;
            System.Diagnostics.Debug.Assert(EditVertexIds.IndexOf(hit_vId) == -1);
            EditVertexIds.Add(hit_vId);

            // 辺の追加
            if (EditVertexIds.Count >= 2 && existEId == 0)
            {
                // １つ前の点と現在の点が既に辺を作っていなければ辺を追加

                uint id_e = 0;
                uint loopIdAddByConnectVertex = 0;
                bool ret = CadLogic.addEdgeByLastEditPts(EditCad2D, parentLoopId, ref EditVertexIds, ref EditEdgeIds, out id_e, out loopIdAddByConnectVertex, showErrorFlg);
                if (ret)
                {
                    executed = true;
                    if (loopIdAddByConnectVertex != 0)
                    {
                        addLoopIds.Add(loopIdAddByConnectVertex);
                    }
                }
                else
                {
                    //System.Diagnostics.Debug.Assert(false);
                    Console.WriteLine("[ERROR]parentElemType:{0}, parentId:{1}", CAD_ELEM_TYPE.LOOP, parentLoopId);
                    Console.WriteLine("[ERROR] add edge pp: {0},{1} index_pp: {2}", pp.x, pp.y, index_pp);
                }
            }
            return executed;
        }

        /// <summary>
        /// 孤立した辺上に頂点を追加する
        /// </summary>
        /// <param name="EditCad2D"></param>
        /// <param name="pp"></param>
        /// <param name="hit_eId"></param>
        /// <param name="index_pp"></param>
        /// <param name="LoopList"></param>
        /// <param name="EditPts"></param>
        /// <param name="EditVertexIds"></param>
        /// <param name="EditEdgeIds"></param>
        /// <param name="addLoopIds"></param>
        /// <param name="showErrorFlg"></param>
        /// <returns></returns>
        public static bool addVertexAndEdgeAtStandAloneEdge(CCadObj2D EditCad2D, CVector2D pp, uint hit_eId, int index_pp, IList<Loop> LoopList,
            ref IList<CVector2D> EditPts, ref IList<uint> EditVertexIds, ref IList<uint> EditEdgeIds, ref IList<uint> addLoopIds, bool showErrorFlg)
        {
            bool executed = false;

            CAD_ELEM_TYPE parentElemType = CAD_ELEM_TYPE.EDGE;
            uint parentId = hit_eId;

            int addedCnt = 0;

            // 頂点と辺を追加
            uint vertexIdAdd = 0;
            uint edgeIdAdd = 0;
            uint edgeIdAddByAddVertex = 0;
            uint loopIdAddByConnectVertex = 0;
            bool ret = CadLogic.addVertexAndEdge(EditCad2D, parentElemType, parentId, pp,
                ref EditPts, ref EditVertexIds, ref EditEdgeIds, out vertexIdAdd, out edgeIdAdd, out edgeIdAddByAddVertex, out loopIdAddByConnectVertex, showErrorFlg);
            if (ret)
            {
                executed = true;
                addedCnt++;
                if (edgeIdAddByAddVertex != 0)
                {
                    //System.Diagnostics.Debug.Assert(false);
                    if (edgeIdAdd != 0)
                    {
                        // 辺が作成されている場合、頂点の作成によってできた（分割された）辺は作成された辺の１つ前に挿入-->現在の最後の位置に挿入
                        EditEdgeIds.Insert(EditEdgeIds.Count - 1, edgeIdAddByAddVertex);
                    }
                    else
                    {
                        EditEdgeIds.Add(edgeIdAddByAddVertex);
                    }
                }
                if (loopIdAddByConnectVertex != 0)
                {
                    addLoopIds.Add(loopIdAddByConnectVertex);
                }
            }
            else
            {
                //System.Diagnostics.Debug.Assert(false);
                Console.WriteLine("[ERROR]parentElemType:{0}, parentId:{1}", parentElemType, parentId);
                Console.WriteLine("[ERROR] add vertex and edge pp: {0},{1} index_pp: {2}", pp.x, pp.y, index_pp);
            }
            return executed;
        }


        /// <summary>
        /// 領域作成コア処理
        ///   Note: ループに属さない孤立した頂点や辺を考慮していない
        /// </summary>
        /// <param name="EditCad2D"></param>
        /// <param name="pp"></param>
        /// <param name="index_pp"></param>
        /// <param name="LoopList"></param>
        /// <param name="EditPts"></param>
        /// <param name="EditVertexIds"></param>
        /// <param name="EditEdgeIds"></param>
        /// <param name="addLoopIds"></param>
        /// <param name="showErrorFlg"></param>
        /// <returns></returns>
        public static bool doMakeDisconAreaCore(CCadObj2D EditCad2D, CVector2D pp, int index_pp, IList<Loop> LoopList,
            ref IList<CVector2D> EditPts, ref IList<uint> EditVertexIds, ref IList<uint> EditEdgeIds, ref IList<uint> addLoopIds, bool showErrorFlg)
        {
            bool executed = false;

            int addedCnt = 0;
            bool addVertexAndEdgeFlg = true;

            if (EditPts.Count >= 3 && CVector2D.Distance(pp, EditPts[0]) < Constants.PrecisionLowerLimit) // loopFixFlg
            {
                // ループ確定
                CVector2D prev_pp = EditPts[EditPts.Count - 1];
                // 最後の辺の追加
                // 前に追加したループと共有している頂点かチェックする
                IList<uint> hit_loopIdList = null;
                uint hit_vId = 0;
                CadLogic.getVertexIdBelongToLoopByCoord(EditCad2D, prev_pp, LoopList, out hit_vId, out hit_loopIdList);

                uint parentLoopId = 0;

                // 先頭の頂点と接続する
                EditPts.Add(EditPts[0]);
                addedCnt++;
                EditVertexIds.Add(EditVertexIds[0]);
                parentLoopId = CadLogic.getLoopIdOfVertex(EditCad2D, EditVertexIds[0]);

                uint existEId = 0;
                {
                    // 先頭の点に関して、前に追加したループと共有している頂点かチェックする
                    IList<uint> next_hit_loopIdList = null;
                    uint next_hit_vId = 0;
                    if (index_pp >= 1)
                    {
                        CadLogic.getVertexIdBelongToLoopByCoord(EditCad2D, EditPts[0], LoopList, out next_hit_vId, out next_hit_loopIdList);
                        if (next_hit_vId != 0)
                        {
                            existEId = getEdgeIdOfVertexIds(EditCad2D, hit_vId, next_hit_vId);
                        }
                    }
                }

                if (hit_vId == 0 || existEId == 0)
                {
                    // １つ前の点と現在の点が既に辺を作っていなければ辺を追加
                    uint id_e = 0;
                    uint loopIdAddByConnectVertex = 0;
                    bool ret = CadLogic.addEdgeByLastEditPts(EditCad2D, parentLoopId, ref EditVertexIds, ref EditEdgeIds, out id_e, out loopIdAddByConnectVertex, showErrorFlg);
                    if (ret)
                    {
                        if (loopIdAddByConnectVertex != 0)
                        {
                            addLoopIds.Add(loopIdAddByConnectVertex);
                        }
                    }
                    else
                    {
                        //System.Diagnostics.Debug.Assert(false);
                        Console.WriteLine("[ERROR]parentElemType:{0}, parentId:{1}", CAD_ELEM_TYPE.LOOP, parentLoopId);
                        Console.WriteLine("[ERROR] add edge (last) pp: {0},{1} index_pp: {2}", pp, pp, index_pp);
                    }
                }
            }
            else
            {
                // 新しく作成しようとしている辺が既存の辺を含んでいる場合をチェック
                if (EditPts.Count >= 1)
                {
                    CVector2D prev_pp = EditPts[EditPts.Count - 1];

                    // 前の点の頂点ID
                    uint prev_VId = EditVertexIds[EditVertexIds.Count - 1]; //
                    System.Diagnostics.Debug.Assert(EditVertexIds.Count == EditPts.Count);

                    // これから作成する辺に含まれる辺の開始頂点、終了頂点を取得する
                    uint minVId = 0;
                    uint maxVId = 0;
                    getIncludedEdgesStEndVId(EditCad2D, prev_pp, pp, LoopList, out minVId, out maxVId);
                    if (minVId != 0 && maxVId != 0)
                    {
                        addVertexAndEdgeFlg = false;

                        CVector2D minVertexPt = EditCad2D.GetVertexCoord(minVId);
                        CVector2D maxVertexPt = EditCad2D.GetVertexCoord(maxVId);
                        Console.WriteLine("split Edge index_pp: {0} pts: ({1},{2}) - ({3},{4}) - ({5},{6}) - ({7},{8})",
                            index_pp, prev_pp.x, prev_pp.y, minVertexPt.x, minVertexPt.y, maxVertexPt.x, maxVertexPt.y, pp.x, pp.y);

                        // pps[index_pp - 1] と minVIdの間の辺の作成
                        if (CVector2D.Distance(prev_pp, minVertexPt) < Constants.PrecisionLowerLimit)
                        {
                            System.Diagnostics.Debug.Assert(minVId == prev_VId);
                        }
                        else
                        {
                            // 前の点と開始頂点の間の辺を作成
                            EditPts.Add(minVertexPt);
                            addedCnt++;
                            System.Diagnostics.Debug.Assert(EditVertexIds.IndexOf(minVId) == -1);
                            EditVertexIds.Add(minVId);

                            // 前の点を含むループIDを取得する
                            uint parentLoopId = getLoopIdIncludingPoint(EditCad2D, prev_pp, LoopList);

                            // １つ前の点と現在の点が既に辺を作っていなければ辺を追加
                            uint id_e = 0;
                            uint loopIdAddByConnectVertex = 0;
                            bool ret = CadLogic.addEdgeByLastEditPts(EditCad2D, parentLoopId, ref EditVertexIds, ref EditEdgeIds, out id_e, out loopIdAddByConnectVertex, showErrorFlg);
                            if (ret)
                            {
                                executed = true;
                                if (loopIdAddByConnectVertex != 0)
                                {
                                    addLoopIds.Add(loopIdAddByConnectVertex);
                                }
                            }
                            else
                            {
                                //System.Diagnostics.Debug.Assert(false);
                                Console.WriteLine("[ERROR]parentElemType:{0}, parentId:{1}", CAD_ELEM_TYPE.LOOP, parentLoopId);
                                Console.WriteLine("[ERROR] add edge (Split Behind) pp: {0},{1} index_pp: {2}", pp.x, pp.y, index_pp);
                            }
                        }
                        // minVId - maxVId間は、既存の辺、頂点なので作成しない
                        // 頂点リストにだけ追加
                        EditPts.Add(maxVertexPt);
                        addedCnt++;
                        System.Diagnostics.Debug.Assert(EditVertexIds.IndexOf(maxVId) == -1);
                        EditVertexIds.Add(maxVId);

                        // maxVIdとpp[index_pp]の間
                        if (CVector2D.Distance(pp, maxVertexPt) < Constants.PrecisionLowerLimit)
                        {
                            // maxVIdがpp[index_pp]の場合、もうすることはない
                            addVertexAndEdgeFlg = false;
                        }
                        else
                        {
                            // 以下 pp[index_pp]を頂点追加、maxVIdとpp[index_pp]の間の辺を追加の処理へ進む
                            addVertexAndEdgeFlg = true;
                        }
                    }
                }

                if (!addVertexAndEdgeFlg)
                {
                    if (addedCnt == 0)
                    {
                        // 頂点の作成が１つも行われなかった場合
                        EditPts.Add(pp);
                    }
                    if (EditPts.Count > 0)
                    {
                        System.Diagnostics.Debug.Assert(CVector2D.Distance(pp, EditPts[EditPts.Count - 1]) < Constants.PrecisionLowerLimit);
                    }
                }
                else
                {
                    // 途中経過のチェック
                    if (EditPts.Count > 0 && addedCnt > 0)
                    {
                        //嘘   prev_pp      min    max   pp
                        //     +------------+-------+----+   のとき EditPts[EditPts.Count - 1]はmaxが入っているはず
                        //System.Diagnostics.Debug.Assert(CVector2D.Distance(pp, EditPts[EditPts.Count - 1]) < Constants.PrecisionLowerLimit);
                    }
                    // 前に追加したループと共有している頂点かチェックする
                    IList<uint> hit_loopIdList = null;
                    uint hit_vId = 0;
                    CadLogic.getVertexIdBelongToLoopByCoord(EditCad2D, pp, LoopList, out hit_vId, out hit_loopIdList);
                    if (hit_vId > 0)
                    {
                        // 共有する頂点の場合
                        uint parentLoopId = 0;
                        if (hit_loopIdList.Count > 0)
                        {
                            parentLoopId = hit_loopIdList[0];
                        }

                        // 頂点は作成せず、辺だけ作成する
                        // 辺の作成に関して、既存の辺が存在するかチェック
                        uint existEId = 0;
                        {
                            // １つ前の点に関して、前に追加したループと共有している頂点かチェックする
                            IList<uint> prev_hit_loopIdList = null;
                            uint prev_hit_vId = 0;
                            if (index_pp >= 1)
                            {
                                //CVector2D prevPt = pps[index_pp - 1];
                                CVector2D prevPt = EditPts[EditPts.Count - 1];
                                CadLogic.getVertexIdBelongToLoopByCoord(EditCad2D, prevPt, LoopList, out prev_hit_vId, out prev_hit_loopIdList);
                                if (prev_hit_vId != 0)
                                {
                                    existEId = getEdgeIdOfVertexIds(EditCad2D, hit_vId, prev_hit_vId);
                                }
                            }
                        }

                        // 頂点は作成しない。リストに追加するだけ。
                        EditPts.Add(pp);
                        addedCnt++;
                        System.Diagnostics.Debug.Assert(EditVertexIds.IndexOf(hit_vId) == -1);
                        EditVertexIds.Add(hit_vId);

                        // 辺の追加
                        if (EditVertexIds.Count >= 2 && existEId == 0)
                        {
                            // １つ前の点と現在の点が既に辺を作っていなければ辺を追加

                            uint id_e = 0;
                            uint loopIdAddByConnectVertex = 0;
                            bool ret = CadLogic.addEdgeByLastEditPts(EditCad2D, parentLoopId, ref EditVertexIds, ref EditEdgeIds, out id_e, out loopIdAddByConnectVertex, showErrorFlg);
                            if (ret)
                            {
                                executed = true;
                                if (loopIdAddByConnectVertex != 0)
                                {
                                    addLoopIds.Add(loopIdAddByConnectVertex);
                                }
                            }
                            else
                            {
                                //System.Diagnostics.Debug.Assert(false);
                                Console.WriteLine("[ERROR]parentElemType:{0}, parentId:{1}", CAD_ELEM_TYPE.LOOP, parentLoopId);
                                Console.WriteLine("[ERROR] add edge pp: {0},{1} index_pp: {2}", pp.x, pp.y, index_pp);
                            }
                        }
                    }
                    else
                    {
                        // ループ内部、または新規の独立な点の場合

                        uint parentId = 0;
                        CAD_ELEM_TYPE parentElemType = CAD_ELEM_TYPE.LOOP;

                        bool addEdgeFlg = true;
                        // 辺上にあるかどうかチェックし、辺上ならば辺IDを返す
                        uint parentEdgeId = getEdgeIdIncludingPoint(EditCad2D, pp, LoopList);

                        if (parentEdgeId != 0)
                        {
                            parentId = parentEdgeId;
                            parentElemType = CAD_ELEM_TYPE.EDGE;

                            if (EditVertexIds.Count > 0)
                            {
                                // 前に追加した頂点が同じ辺上にあるかをチェック
                                uint prev_id_v = EditVertexIds[EditVertexIds.Count - 1];
                                CVector2D prev_pt_v = EditCad2D.GetVertexCoord(prev_id_v);
                                bool isOnEdge_prev_pt_v = isPointOnEdge(EditCad2D, parentEdgeId, prev_pt_v);
                                if (isOnEdge_prev_pt_v)
                                {
                                    addEdgeFlg = false;
                                }
                            }
                        }
                        else
                        {
                            // 包含関係を調べる必要あり
                            // 点を含むループIDを取得する
                            uint parentLoopId = getLoopIdIncludingPoint(EditCad2D, pp, LoopList);
                            parentId = parentLoopId;
                            parentElemType = CAD_ELEM_TYPE.LOOP;
                        }

                        if (addEdgeFlg)
                        {
                            // 頂点と辺を追加
                            uint vertexIdAdd = 0;
                            uint edgeIdAdd = 0;
                            uint edgeIdAddByAddVertex = 0;
                            uint loopIdAddByConnectVertex = 0;
                            bool ret = CadLogic.addVertexAndEdge(EditCad2D, parentElemType, parentId, pp,
                                ref EditPts, ref EditVertexIds, ref EditEdgeIds, out vertexIdAdd, out edgeIdAdd, out edgeIdAddByAddVertex, out loopIdAddByConnectVertex, showErrorFlg);
                            if (ret)
                            {
                                executed = true;
                                addedCnt++;
                                if (edgeIdAddByAddVertex != 0)
                                {
                                    //System.Diagnostics.Debug.Assert(false);
                                    if (edgeIdAdd != 0)
                                    {
                                        // 辺が作成されている場合、頂点の作成によってできた（分割された）辺は作成された辺の１つ前に挿入-->現在の最後の位置に挿入
                                        EditEdgeIds.Insert(EditEdgeIds.Count - 1, edgeIdAddByAddVertex);
                                    }
                                    else
                                    {
                                        EditEdgeIds.Add(edgeIdAddByAddVertex);
                                    }
                                }
                                if (loopIdAddByConnectVertex != 0)
                                {
                                    addLoopIds.Add(loopIdAddByConnectVertex);
                                }
                            }
                            else
                            {
                                //System.Diagnostics.Debug.Assert(false);
                                Console.WriteLine("[ERROR]parentElemType:{0}, parentId:{1}", parentElemType, parentId);
                                Console.WriteLine("[ERROR] add vertex and edge pp: {0},{1} index_pp: {2}", pp.x, pp.y, index_pp);
                            }
                        }
                        else
                        {
                            // 頂点のみ追加
                            uint vertexIdAdd = 0;
                            uint edgeIdAddByAddVertex = 0;
                            bool ret = CadLogic.addVertex(EditCad2D, parentElemType, parentId, pp,
                                ref EditPts, ref EditVertexIds, ref EditEdgeIds, out vertexIdAdd, out edgeIdAddByAddVertex, showErrorFlg);
                            if (ret)
                            {
                                executed = true;
                                addedCnt++;
                                if (edgeIdAddByAddVertex != 0)
                                {
                                    EditEdgeIds.Add(edgeIdAddByAddVertex);
                                }
                            }
                            else
                            {
                                //System.Diagnostics.Debug.Assert(false);
                                Console.WriteLine("[ERROR]parentElemType:{0}, parentId:{1}", parentElemType, parentId);
                                Console.WriteLine("[ERROR] add vertex pp: {0},{1}", pp.x, pp.y);
                            }
                        } // addEdgeFlg
                    } // hit_vId

                    if (addedCnt == 0)
                    {
                        // 頂点の作成が１つも行われなかった場合
                        //addPts.Add(pps[index_pp]);
                        System.Diagnostics.Debug.Assert(false);
                    }
                    if (EditPts.Count > 0)
                    {
                        System.Diagnostics.Debug.Assert(CVector2D.Distance(pp, EditPts[EditPts.Count - 1]) < Constants.PrecisionLowerLimit);
                    }
                } // addVertexAndEdgeFlg
            } // loopFixFlg

            return executed;
        }

        /// <summary>
        /// 頂点と辺を追加する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="parentElemType"></param>
        /// <param name="parentId"></param>
        /// <param name="pp"></param>
        /// <param name="editPts"></param>
        /// <param name="editVertexIds"></param>
        /// <param name="editEdgeIds"></param>
        /// <param name="vertexIdAdd"></param>
        /// <param name="edgeIdAdd"></param>
        /// <param name="edgeIdAddByAddVertex"></param>
        /// <param name="loopIdAddByConnectVertex"></param>
        /// <returns></returns>
        public static bool addVertexAndEdge(CCadObj2D cad2d, CAD_ELEM_TYPE parentElemType, uint parentId, CVector2D pp,
            ref IList<CVector2D> editPts, ref IList<uint> editVertexIds, ref IList<uint> editEdgeIds,
            out uint vertexIdAdd, out uint edgeIdAdd, out uint edgeIdAddByAddVertex, out uint loopIdAddByConnectVertex,
            bool showErrorFlg)
        {
            bool success = false;
            uint id_v = 0; // 作成された頂点のID
            uint id_e_add_by_addVertex = 0; // 頂点作成で辺が分割された場合に格納
            uint id_e = 0; // 作成された辺のID
            uint id_l_add_by_connectVertex = 0; // 辺の作成でループが作成された場合ループIDを格納 

            // 頂点を作成
            success = addVertex(cad2d, parentElemType, parentId, pp,
                ref editPts, ref editVertexIds, ref editEdgeIds, out id_v, out id_e_add_by_addVertex, showErrorFlg);
            if (!success || id_v == 0)
            {
                success = false;
            }
            else
            {
                success = true;
                uint id_l_of_addVertex = 0; // 追加された頂点の属するループID
                if (parentElemType == CAD_ELEM_TYPE.LOOP)
                {
                    id_l_of_addVertex = parentId; // 図面が空の状態だと0が入る、それ以外は頂点を追加したループのID
                }
                else if (parentElemType == CAD_ELEM_TYPE.EDGE && id_e_add_by_addVertex != 0)
                {
                    id_l_of_addVertex = 0;
                    id_l_of_addVertex = getLoopIdOfEdge(cad2d, id_e_add_by_addVertex);

                    // Note: 今回作成した頂点が辺上にある場合、前の頂点も辺上ならば辺の作成はできない
                }
                else
                {
                    // ロジックエラー
                    System.Diagnostics.Debug.Assert(false);
                }

                if (editVertexIds.Count >= 2)
                {
                    // 辺を作成
                    success = CadLogic.addEdgeByLastEditPts(cad2d, id_l_of_addVertex,
                        ref editVertexIds,
                        ref editEdgeIds,
                        out id_e, out id_l_add_by_connectVertex,
                        showErrorFlg);
                    if (!success)
                    {
                        // 失敗
                        // 頂点を削除する
                        cad2d.RemoveElement(CAD_ELEM_TYPE.VERTEX, id_v);
                        editVertexIds.RemoveAt(editVertexIds.Count - 1);
                        id_e_add_by_addVertex = 0;
                        editPts.RemoveAt(editPts.Count - 1);
                        ////EditParentLoopId = 0;
                    }
                }
            }
            vertexIdAdd = id_v;
            edgeIdAdd = id_e;
            edgeIdAddByAddVertex = id_e_add_by_addVertex;
            loopIdAddByConnectVertex = id_l_add_by_connectVertex;
            return success;
        }

        /// <summary>
        /// 頂点を作成する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="parentElemType"></param>
        /// <param name="parentId"></param>
        /// <param name="pp"></param>
        /// <param name="editPts"></param>
        /// <param name="editVertexIds"></param>
        /// <param name="editEdgeIds"></param>
        /// <param name="vertexIdAdd"></param>
        /// <param name="edgeIdAddByAddVertex"></param>
        /// <param name="showErrorFlg"></param>
        /// <returns></returns>
        public static bool addVertex(CCadObj2D cad2d, CAD_ELEM_TYPE parentElemType, uint parentId, CVector2D pp,
            ref IList<CVector2D> editPts, ref IList<uint> editVertexIds, ref IList<uint> editEdgeIds,
            out uint vertexIdAdd, out uint edgeIdAddByAddVertex,
            bool showErrorFlg)
        {
            bool success = false;
            uint id_v = 0; // 作成された頂点のID
            uint id_e_add_by_addVertex = 0; // 頂点作成で辺が分割された場合に格納

            // 頂点を作成
            //id_v = cad2d.AddVertex(parentElemType, parentId, pp).id_v_add;
            CCadObj2D.CResAddVertex resAddVertex = cad2d.AddVertex(parentElemType, parentId, pp);
            id_v = resAddVertex.id_v_add;
            id_e_add_by_addVertex = resAddVertex.id_e_add;
            if (id_v == 0)
            {
                // 頂点の作成に失敗
                if (showErrorFlg)
                {
                    MessageBox.Show("頂点の作成に失敗しました", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                resAddVertex.id_v_add = 0;
                success = false;
            }
            else
            {
                success = true;
                /*
                // 頂点の追加で辺が分割された場合は、辺のIDを記録する
                if (id_e_add_by_addVertex != 0)
                {
                    editEdgeIds.Add(id_e_add_by_addVertex);
                }
                 */
                // 頂点の作成に成功
                // 点を追加
                editPts.Add(pp);

                // 頂点をリストに追加
                editVertexIds.Add(id_v);

                // 一時作成の辺の色をセットする
                if (id_e_add_by_addVertex != 0)
                {
                    cad2d.SetColor_Edge(id_e_add_by_addVertex, CadLogic.ColorToColorDouble(CadLogic.TmpEdgeColor));
                }
            }
            vertexIdAdd = id_v;
            edgeIdAddByAddVertex = id_e_add_by_addVertex;
            return success;
        }

        /// <summary>
        /// 辺を作成する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="id_l_of_addVertex"></param>
        /// <param name="editVertexIds"></param>
        /// <param name="editEdgeIds"></param>
        /// <param name="id_e"></param>
        /// <param name="id_l_add_by_connectVertex"></param>
        /// <returns></returns>
        public static bool addEdgeByLastEditPts(CCadObj2D cad2d, uint id_l_of_addVertex,
            ref IList<uint> editVertexIds, ref IList<uint> editEdgeIds,
            out uint id_e, out uint id_l_add_by_connectVertex, bool showErrorFlg)
        {
            bool success = false;

            // 出力の初期化
            id_e = 0;
            id_l_add_by_connectVertex = 0;

            // 作成しようとしている辺がすでに作成されているかチェックする
            uint existedEId = CadLogic.getEdgeIdOfVertexIds(cad2d, editVertexIds[editVertexIds.Count - 2], editVertexIds[editVertexIds.Count - 1]);
            if (existedEId != 0)
            {
                // すでに追加しようとしている辺が存在する場合は追加しない
                if (showErrorFlg)
                {
                    MessageBox.Show("すでに辺は作成されています", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return success;
            }

            // 辺を作成
            //id_e = EditCad2D.ConnectVertex_Line(EditVertexIds[EditVertexIds.Count - 2], EditVertexIds[EditVertexIds.Count - 1]).id_e_add;
            CBRepSurface.CResConnectVertex resConnectVertex = cad2d.ConnectVertex_Line(editVertexIds[editVertexIds.Count - 2], editVertexIds[editVertexIds.Count - 1]);
            id_e = resConnectVertex.id_e_add;  // 追加された辺のIDを格納
            if (resConnectVertex.id_l_add != 0)
            {
                Console.WriteLine("id_l_of_addVertex:{0}", id_l_of_addVertex);
                Console.WriteLine("id_l_add:{0}", resConnectVertex.id_l_add);
                Console.WriteLine("id_l_add:{0}", resConnectVertex.id_l);

                id_l_add_by_connectVertex = (resConnectVertex.id_l_add != id_l_of_addVertex) ? resConnectVertex.id_l_add : resConnectVertex.id_l;  // 辺の作成でループが作成された場合ループIDを格納
                Console.WriteLine("id_l_add_by_connectVertex:{0}", id_l_add_by_connectVertex);
            }
            else
            {
                id_l_add_by_connectVertex = 0;
            }
            if (id_e == 0)
            {
                // 辺の作成に失敗
                if (showErrorFlg)
                {
                    MessageBox.Show("辺の作成に失敗しました", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                id_l_add_by_connectVertex = 0;

                success = false;
            }
            else
            {
                // 辺の作成に成功
                editEdgeIds.Add(id_e);

                // 一時作成の辺の色をセットする
                cad2d.SetColor_Edge(id_e, CadLogic.ColorToColorDouble(CadLogic.TmpEdgeColor));

                success = true;
            }
            return success;
        }

        /// <summary>
        /// ヒットテスト
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private bool hitTest(Point pt, out CAD_ELEM_TYPE partElemType, out uint partId)
        {
            bool hit = false;
            partElemType = CAD_ELEM_TYPE.NOT_SET;
            partId = 0;

            int sizeBuffer = 2048;
            DelFEM4NetCom.View.DrawerGlUtility.PickSelectBuffer pickSelectBuffer = null;
            DelFEM4NetCom.View.DrawerGlUtility.PickPre((uint)sizeBuffer, out pickSelectBuffer, (uint)pt.X, (uint)pt.Y, 5, 5, Camera);
            DrawerAry.DrawSelection();
            List<DelFEM4NetCom.View.SSelectedObject> aSelecObj = (List<DelFEM4NetCom.View.SSelectedObject>)DelFEM4NetCom.View.DrawerGlUtility.PickPost(pickSelectBuffer, (uint)pt.X, (uint)pt.Y, Camera);

            //uint[] select_buffer = pickSelectBuffer.ToArray();
            //
            //foreach (uint buf in select_buffer)
            //{
            //    if (buf != 0)
            //    {
            //        Console.Write("[" + buf + "]");
            //    }
            //}
            //Console.WriteLine();

            DrawerAry.ClearSelected();
            if (aSelecObj.Count > 0)
            {
                if (DrawerAry.m_drawer_ary.Count >= 2)
                {
                    CDrawer_Cad2D drawerCad2d = DrawerAry.m_drawer_ary[1] as CDrawer_Cad2D;
                    if (drawerCad2d != null)
                    {
                        drawerCad2d.GetCadPartID(aSelecObj[0].name, ref partElemType, ref partId);
                        //Console.WriteLine("partElemType:{0} partId:{1}", partElemType, partId);
                    }
                }
                //int index = 0;
                //foreach (int ival in aSelecObj[0].name)
                //{
                //    //Console.WriteLine("aSelecObj[0].name[{0}] = {1}", index, ival);
                //    index++;
                //}
                if ((CadMode == CadModeType.Area 
                    && ((!IsEditing && partElemType == CAD_ELEM_TYPE.LOOP)
                        || partElemType == CAD_ELEM_TYPE.EDGE || partElemType == CAD_ELEM_TYPE.VERTEX))
                    || (CadMode == CadModeType.MediaFill && partElemType == CAD_ELEM_TYPE.LOOP)
                    || ((CadMode == CadModeType.Port || CadMode == CadModeType.IncidentPort || CadMode == CadModeType.PortNumbering) && partElemType == CAD_ELEM_TYPE.EDGE)
                    || (CadMode == CadModeType.Erase && (partElemType == CAD_ELEM_TYPE.LOOP || partElemType == CAD_ELEM_TYPE.EDGE || partElemType == CAD_ELEM_TYPE.VERTEX))
                    || (CadMode == CadModeType.MoveObj && (partElemType == CAD_ELEM_TYPE.LOOP || partElemType == CAD_ELEM_TYPE.EDGE || partElemType == CAD_ELEM_TYPE.VERTEX))
                    )
                {
                    // 選択表示設定に追加する
                    DrawerAry.AddSelected(aSelecObj[0].name);
                }
                else
                {
                    // 選択表示にはしない
                }
                // ヒットフラグは選択表示する/しないに関係なくヒットしたらフラグを立てる
                hit = true;
            }
            /* メッシュ表示の場合
            if (aSelecObj.Count > 0)
            {
                int index = 0;
                foreach (int ival in aSelecObj[0].name)
                {
                    Console.WriteLine("aSelecObj[0].name[{0}] = {1}", index, ival);
                    index++;
                }
                DrawerAry.AddSelected(aSelecObj[0].name);
                hit = true;
            }
            else
            {
                da.ClearSelected();
            }
             */
            return hit;
        }

        /// <summary>
        /// ループの色と辺の色を全ループについて設定する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="loopList"></param>
        /// <param name="medias"></param>
        public static void SetupColorOfCadObjectsForAllLoops(CCadObj2D cad2d, IList<Loop> loopList, MediaInfo[] medias)
        {
            //ループの色をすべて再設定する
            foreach (Loop workLoop in loopList)
            {
                uint loopId = workLoop.LoopId;
                int mediaIndex = workLoop.MediaIndex;
                MediaInfo media = medias[mediaIndex];
                Color backCOlor = media.BackColor;
                SetupColorOfCadObjectsForOneLoop(cad2d, workLoop.LoopId, backCOlor);
            }
        }

        /// <summary>
        /// 領域を削除する
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private bool doEraseDisconArea(Point pt)
        {
            bool executed = false;
            if (CadMode != CadModeType.Erase)
            {
                return executed;
            }
            // ヒットテストで、ヒットしたパートの要素タイプとIdを取得する
            CAD_ELEM_TYPE partElemType;
            uint partId;
            bool hit = hitTest(pt, out partElemType, out partId);
            if (hit)
            {
                if (partElemType == CAD_ELEM_TYPE.LOOP && partId != 0 && EditCad2D.IsElemID(CAD_ELEM_TYPE.LOOP, partId))
                {
                    uint tagtLoopId = partId;
                    // ループの削除処理
                    executed = delLoop(EditCad2D, tagtLoopId, ref LoopList, ref EdgeCollectionList, Medias, ref IncidentPortNo);
                }
            }
            if (executed)
            {
                // コマンドを実行する
                invokeCadOperationCmd();
                //  Note:ここでCadObjが新しくなるのでrefreshDrawerAry()をこの後実行すること
                // 描画オブジェクトアレイの更新フラグを立てる
                RefreshDrawerAryFlg = true;
                if (Change != null)
                {
                    Change(this, CadMode);
                }
            }
            if (executed && !isDirty)
            {
                isDirty = true;
            }
            return executed;
        }

        /// <summary>
        /// ループ削除処理
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="tagtLoopId"></param>
        /// <param name="loopList"></param>
        /// <param name="edgeCollectionList"></param>
        /// <param name="medias"></param>
        /// <param name="incidentPortNo"></param>
        /// <returns></returns>
        public static bool delLoop(CCadObj2D cad2d, uint tagtLoopId, ref IList<Loop> loopList, ref IList<EdgeCollection> edgeCollectionList, MediaInfo[] medias, ref int incidentPortNo)
        {
            bool executed = false;
            if (tagtLoopId == 0)
            {
                System.Diagnostics.Debug.Assert(false);
                return executed;
            }

            // ループの頂点、辺のIDのリストを取得する
            IList<uint> vIdList = null;
            IList<uint> eIdList = null;
            CadLogic.GetEdgeVertexListOfLoop(cad2d, tagtLoopId, out vIdList, out eIdList);

            IList<uint> otherLoop_vIdList = new List<uint>();
            IList<uint> otherLoop_eIdList = new List<uint>();
            foreach (Loop workLoop in loopList)
            {
                uint workLoopId = workLoop.LoopId;
                if (workLoopId != tagtLoopId)
                {
                    IList<uint> work_vIdList = null;
                    IList<uint> work_eIdList = null;
                    CadLogic.GetEdgeVertexListOfLoop(cad2d, workLoopId, out work_vIdList, out work_eIdList);
                    foreach (uint work_id_v in work_vIdList)
                    {
                        otherLoop_vIdList.Add(work_id_v);
                    }
                    foreach (uint work_id_e in work_eIdList)
                    {
                        otherLoop_eIdList.Add(work_id_e);
                    }
                }
            }

            // ポート境界があれば削除する
            delPortBelongToLoop(cad2d, tagtLoopId, ref edgeCollectionList, ref incidentPortNo);

            // エラーチェック用
            Dictionary<uint, IList<uint>> saveLoopEdgesList = null;
            chkLoopEdges_PreProc(cad2d, loopList, out saveLoopEdgesList);

            // ループ削除
            //   辺と頂点も削除してくれているはず? -->してくれない-->というかループの場合なにもしないらしい
            //////EditCad2D.RemoveElement(CAD_ELEM_TYPE.LOOP, tagtLoopId);
            // ループを構成する辺と頂点を削除
            // 辺を削除
            foreach (uint id_e in eIdList)
            {
                if (otherLoop_eIdList.IndexOf(id_e) >= 0)
                {
                    // 他のループと共有している辺の場合(領域分割で作成された辺)
                    continue;
                }

                /* あとでループの色を再設定することにしたので削除する
                // 辺を削除すると、内側のループが、外側のループにマージされる仕様のようです。
                // ここでは、内側の領域を削除する用途で使用するので、まず内側の色に外側の色をセットします。
                // 外側のループは？
                // 隣のループは?の方が適切
                uint nextDoorLoopId = getNextDoorLoopId(cad2d, id_e, tagtLoopId);
                if (nextDoorLoopId != 0 && cad2d.IsElemID(CAD_ELEM_TYPE.LOOP, nextDoorLoopId))
                {
                    double[] colorDouble = null;
                    // 隣のループの色を取得
                    cad2d.GetColor_Loop(nextDoorLoopId, out colorDouble);
                    // 削除対象のループの色を隣の色にする
                    cad2d.SetColor_Loop(tagtLoopId, colorDouble);
                }
                 */

                // 辺を削除
                cad2d.RemoveElement(CAD_ELEM_TYPE.EDGE, id_e);

                // 領域すべてが他のループに囲まれていて削除できない場合がある
                // そのため、ここで辺の削除が行われていることを示すためにフラグを立てる
                executed = true;
            }
            if (executed)
            {
                // 頂点削除
                foreach (uint id_v in vIdList)
                {
                    if (otherLoop_vIdList.IndexOf(id_v) >= 0)
                    {
                        // 他のループと共有している頂点の場合(領域分割で作成された頂点)
                        continue;
                    }
                    cad2d.RemoveElement(CAD_ELEM_TYPE.VERTEX, id_v);
                }
            }

            if (executed)
            {
                // ループリストから削除
                Loop loop = getLoop(loopList, tagtLoopId);
                loopList.Remove(loop);

                // チェック用
                // ループIDが変更されているかチェックし、変更されていればループ情報を更新する
                chkLoopEdges_PostProc(cad2d, saveLoopEdgesList, tagtLoopId, 0, ref loopList);

                // 全ループの色を再設定する
                SetupColorOfCadObjectsForAllLoops(cad2d, loopList, medias);

            }

            return executed;
        }

        /// <summary>
        /// エラーチェック用前処理
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="loopList">ループ情報リスト</param>
        /// <param name="saveLoopEdgesList"></param>
        private static void chkLoopEdges_PreProc(CCadObj2D cad2d, IList<Loop> loopList, out Dictionary<uint, IList<uint>> saveLoopEdgesList)
        {
            saveLoopEdgesList = new Dictionary<uint, IList<uint>>();
            foreach (Loop workLoop in loopList)
            {
                IList<uint> work_vIdList = null;
                IList<uint> work_eIdList = null;
                GetEdgeVertexListOfLoop(cad2d, workLoop.LoopId, out work_vIdList, out work_eIdList);
                saveLoopEdgesList.Add(workLoop.LoopId, work_eIdList);
            }
        }
        /// <summary>
        /// ループIDが変更されているかチェックし、変更されていればループ情報を更新する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="saveLoopEdgesList"></param>
        /// <param name="delLoopId">削除されたループID</param>
        /// <param name="delEId">削除された辺ID</param>
        /// <param name="loopList">ループ情報リスト</param>
        private static void chkLoopEdges_PostProc(CCadObj2D cad2d, Dictionary<uint, IList<uint>> saveLoopEdgesList, uint delLoopId, uint delEId, ref IList<Loop> loopList)
        {
            if (delLoopId != 0)
            {
                // まず削除されたループは除外するためマップから削除
                saveLoopEdgesList.Remove(delLoopId);
            }

            // エラーチェック：ループIDが変更される??
            foreach (var loopEdges_pair in saveLoopEdgesList)
            {
                uint prevLoopId = loopEdges_pair.Key;
                IList<uint> eIds = loopEdges_pair.Value;

                // ループIDが変わってないか検査する
                Dictionary<uint, int> loopIdsHash = new Dictionary<uint,int>();
                foreach (uint eId in eIds)
                {
                    if (eId == delEId)
                    {
                        continue;
                    }
                    uint curLoopId = getLoopIdOfEdge(cad2d, eId);
                    if (curLoopId == 0)
                    {
                        continue;
                    }
                    /*
                    if (curLoopId != prevLoopId)
                    {
                    }
                     */
                    int hitCnt = 0;
                    if (loopIdsHash.ContainsKey(curLoopId))
                    {
                        hitCnt = loopIdsHash[curLoopId]; 
                        hitCnt++;
                        loopIdsHash[curLoopId] = hitCnt;
                    }
                    else
                    {
                        hitCnt = 1;
                        loopIdsHash.Add(curLoopId, hitCnt);
                    }
                }
                if (loopIdsHash.ContainsKey(prevLoopId))
                {
                    // 以前のループIDがある
                    // 変更されていないとする
                    Console.WriteLine("edge cnt = {0}, hit cnt = {1}", eIds.Count, loopIdsHash[prevLoopId]);
                }
                else
                {
                    // 変更された
                    // 一番ヒット数の多いループIDを新しいループIDとする
                    uint curLoopId = 0;
                    int maxHits = 0;
                    foreach (var loopIdsH_pair in loopIdsHash)
                    {
                        uint workLoopId = loopIdsH_pair.Key;
                        int hitCnt = loopIdsH_pair.Value;
                        if (maxHits < hitCnt)
                        {
                            curLoopId = workLoopId;
                            maxHits = hitCnt;
                        }
                    }
                    System.Diagnostics.Debug.Assert(curLoopId != 0);
                    // ループ情報を更新
                    Loop loop = getLoop(loopList, prevLoopId);
                    loop.Set(curLoopId, loop.MediaIndex);

                    Console.WriteLine("loopId changed {0} → {1}", prevLoopId, curLoopId);
                    //MessageBox.Show(string.Format("loopId changed {0} → {1}", prevLoopId, curLoopId));
                }
            }
        }

        /// <summary>
        /// ループの辺に属するポートを削除する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="tagtLoopId"></param>
        /// <param name="edgeCollectionList"></param>
        /// <param name="incidentPortNo"></param>
        /// <returns>実行された場合true、実行されなかった場合false</returns>
        public static bool delPortBelongToLoop(CCadObj2D cad2d, uint tagtLoopId, ref IList<EdgeCollection> edgeCollectionList, ref int incidentPortNo)
        {
            bool executed = false;
            if (tagtLoopId == 0)
            {
                System.Diagnostics.Debug.Assert(false);
                return executed;
            }

            // ループの頂点、辺のIDのリストを取得する
            IList<uint> vIdList = null;
            IList<uint> eIdList = null;
            CadLogic.GetEdgeVertexListOfLoop(cad2d, tagtLoopId, out vIdList, out eIdList);

            // ポート境界があれば削除する
            IList<EdgeCollection> delEdgeCollectionList = new List<EdgeCollection>();
            // すべてのポートについて検査
            foreach (EdgeCollection work in edgeCollectionList)
            {
                // 削除対象の辺集合か調べる
                foreach (uint id_e in eIdList)
                {
                    if (work.ContainsEdgeId(id_e))
                    {
                        delEdgeCollectionList.Add(work);
                        break;
                    }
                }
            }
            // ポート削除処理
            foreach (EdgeCollection deltarget in delEdgeCollectionList)
            {
                // 削除前検査：ポート削除対象が、ほかのループと共有しているか検査する
                bool isSharedPort = false;
                foreach (uint eId in deltarget.EdgeIds)
                {
                    if (isEdgeSharedByLoops(cad2d, eId))
                    {
                        isSharedPort = true;
                        break;
                    }
                }
                if (isSharedPort)
                {
                    // 削除しない
                    continue;
                }
                // ポート削除
                CadLogic.doErasePortCore(cad2d, deltarget, ref edgeCollectionList, ref incidentPortNo);
                executed = true;
            }
            return executed;
        }

        /// <summary>
        /// 領域を媒質で埋める
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private bool doFillMedia(Point pt)
        {
            bool executed = false;
            if (CadMode != CadModeType.MediaFill)
            {
                return executed;
            }
            // ヒットテストで、ヒットしたパートの要素タイプとIdを取得する
            CAD_ELEM_TYPE partElemType;
            uint partId;
            bool hit = hitTest(pt, out partElemType, out partId);
            if (hit)
            {
                if (partElemType == CAD_ELEM_TYPE.LOOP && partId != 0 && EditCad2D.IsElemID(CAD_ELEM_TYPE.LOOP, partId))
                {
                    uint tagtLoopId = partId;
                    // ループ情報を取得
                    Loop loop = getLoop(LoopList, tagtLoopId);
                    // ループ情報を更新(媒質インデックス)
                    loop.Set(loop.LoopId, SelectedMediaIndex);
                    // 全ループの色を再設定する
                    SetupColorOfCadObjectsForAllLoops(EditCad2D, LoopList, Medias);
                    executed = true;
                }
            }
            if (executed)
            {
                // コマンドを実行する
                invokeCadOperationCmd();
                //  Note:ここでCadObjが新しくなるのでrefreshDrawerAry()をこの後実行すること
                // 描画オブジェクトアレイの更新フラグを立てる
                RefreshDrawerAryFlg = true;
                if (Change != null)
                {
                    Change(this, CadMode);
                }
            }
            if (executed && !isDirty)
            {
                isDirty = true;
            }
            return executed;
        }

        /// <summary>
        /// 入出力ポートの選択処理
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private bool doSelectPort(Point pt)
        {
            bool executed = false;

            if (CadMode != CadModeType.Port)
            {
                return executed;
            }
            // ヒットテストで、ヒットしたパートの要素タイプとIdを取得する
            CAD_ELEM_TYPE partElemType;
            uint partId;
            bool hit = hitTest(pt, out partElemType, out partId);
            if (hit)
            {
                if (partElemType == CAD_ELEM_TYPE.EDGE && partId != 0 && EditCad2D.IsElemID(CAD_ELEM_TYPE.EDGE, partId))
                {
                    uint tagtEdgeId = partId;
                    // 辺がループに属しているかチェック
                    uint parentLoopId = getLoopIdOfEdge(EditCad2D, tagtEdgeId);
                    if (parentLoopId == 0)
                    {
                        // 辺がループに属していない
                        return executed;
                    }
                    // すでに対象の辺がポート境界に指定されていないかチェックする
                    EdgeCollection otherEdge = getEdgeCollectionByEdgeId(tagtEdgeId);
                    if (otherEdge == null)
                    {
                        // 新規の辺

                        EdgeCollection edge = null;
                        if (KeyModifiers.HasFlag(Keys.Control) && EditPortNo != 0)
                        {
                            // Controlが押されている場合は、直前に作成された境界に辺を追加する
                            ((List<EdgeCollection>)EdgeCollectionList).Sort(); // 順番に並んでいることを保証する
                            edge = EdgeCollectionList[EditPortNo - 1];
                            if (isNextDoorEdge(EditCad2D, tagtEdgeId, edge.EdgeIds[edge.EdgeIds.Count - 1]))
                            {
                                // 隣
                                bool ret = edge.AddEdgeId(tagtEdgeId, EditCad2D);
                                if (ret)
                                {
                                    executed = true;
                                }
                            }
                            else
                            {
                                // 隣でない
                                Console.WriteLine("not nextdoor edge");
                            }
                        }
                        else
                        {
                            // 新規のポート境界
                            edge = new EdgeCollection();
                            edge.No = EdgeCollectionList.Count + 1;
                            bool ret = edge.AddEdgeId(tagtEdgeId, EditCad2D);
                            if (ret)
                            {
                                // ポート境界の追加
                                EdgeCollectionList.Add(edge);
                                if (KeyModifiers.HasFlag(Keys.Control))
                                {
                                    EditPortNo = edge.No;
                                    Console.WriteLine("EditPortNo written!");
                                }
                                executed = true;
                            }
                        }
                        if (executed)
                        {
                            // 辺に色を付ける
                            Color portColor = (edge.No == IncidentPortNo) ? IncidentPortColor : PortColor;
                            double[] portColorDouble = CadLogic.ColorToColorDouble(portColor);
                            foreach (uint eId in edge.EdgeIds)
                            {
                                EditCad2D.SetColor_Edge(eId, portColorDouble);
                            }
                        }
                    }
                }
            }
            if (executed)
            {
                // コマンドを実行する
                invokeCadOperationCmd();
                //  Note:ここでCadObjが新しくなるのでrefreshDrawerAry()をこの後実行すること
                // 描画オブジェクトアレイの更新フラグを立てる
                RefreshDrawerAryFlg = true;
                if (Change != null)
                {
                    Change(this, CadMode);
                }
            }
            if (executed && !isDirty)
            {
                isDirty = true;
            }

            return executed;
        }

        /// <summary>
        /// 入出力ポートの削除処理
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private bool doErasePort(Point pt)
        {
            bool executed = false;

            if (CadMode != CadModeType.Erase)
            {
                return executed;
            }
            // ヒットテストで、ヒットしたパートの要素タイプとIdを取得する
            CAD_ELEM_TYPE partElemType;
            uint partId;
            bool hit = hitTest(pt, out partElemType, out partId);
            if (hit)
            {
                if (partElemType == CAD_ELEM_TYPE.EDGE && partId != 0 && EditCad2D.IsElemID(CAD_ELEM_TYPE.EDGE, partId))
                {
                    uint tagtEdgeId = partId;
                    EdgeCollection hitEdge = getEdgeCollectionByEdgeId(tagtEdgeId);
                    if (hitEdge != null)
                    {
                        doErasePortCore(EditCad2D, hitEdge, ref EdgeCollectionList, ref IncidentPortNo);

                        executed = true;
                    }
                }
            }
            if (executed)
            {
                // コマンドを実行する
                invokeCadOperationCmd();
                //  Note:ここでCadObjが新しくなるのでrefreshDrawerAry()をこの後実行すること
                // 描画オブジェクトアレイの更新フラグを立てる
                RefreshDrawerAryFlg = true;
                if (Change != null)
                {
                    Change(this, CadMode);
                }
            }
            if (executed && !isDirty)
            {
                isDirty = true;
            }

            return executed;
        }

        /// <summary>
        /// 辺のIDからエッジコレクションを取得する
        /// </summary>
        /// <param name="tagtEdgeId"></param>
        /// <returns></returns>
        private EdgeCollection getEdgeCollectionByEdgeId(uint tagtEdgeId)
        {
            EdgeCollection hitEdge = null;
            foreach (EdgeCollection work in EdgeCollectionList)
            {
                if (work.ContainsEdgeId(tagtEdgeId))
                {
                    hitEdge = work;
                    break;
                }
            }
            return hitEdge;
        }

        /// <summary>
        /// ポート境界の消去コア処理
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="hitEdge"></param>
        /// <param name="edgeCollectionList"></param>
        /// <param name="incidentPortNo"></param>
        public static void doErasePortCore(CCadObj2D cad2d, EdgeCollection hitEdge, ref IList<EdgeCollection> edgeCollectionList, ref int incidentPortNo)
        {
            // ヒットしたポート番号
            int hitPortNo = hitEdge.No;

            System.Diagnostics.Debug.Assert(!hitEdge.IsEmpty());
            // 辺ID(先頭の辺を採用)
            uint tagtEdgeId = hitEdge.EdgeIds[0];

            // ループの色
            double[] loopColorDouble = null;
            //辺が属するループを取得する
            uint id_l = getLoopIdOfEdge(cad2d, tagtEdgeId);
            // ループの背景色を取得する
            cad2d.GetColor_Loop(id_l, out loopColorDouble);
            // 辺の色をループの色に戻す
            Color loopColor = CadLogic.ColorDoubleToColor(loopColorDouble);
            Color loopLineColor = CadLogic.GetLoopLineColor(loopColor);
            double[] loopLineColorDouble = CadLogic.ColorToColorDouble(loopLineColor);
            foreach (uint eId in hitEdge.EdgeIds)
            {
                cad2d.SetColor_Edge(eId, loopLineColorDouble);
            }
            // ポートを削除
            edgeCollectionList.Remove(hitEdge);
            if (hitPortNo == incidentPortNo)
            {
                // 削除したポートが入射ポートだった場合
                // 入射ポートをリセットする
                incidentPortNo = 1;
                // 入射ポートがあれば辺の色を変更
                if (edgeCollectionList.Count >= incidentPortNo)
                {
                    double[] incidentPortColorDouble = CadLogic.ColorToColorDouble(IncidentPortColor);
                    EdgeCollection incidentEdge = edgeCollectionList[incidentPortNo - 1];
                    foreach (uint eId in incidentEdge.EdgeIds)
                    {
                        cad2d.SetColor_Edge(eId, incidentPortColorDouble);
                    }
                }
            }
            // ポート番号振り直し
            int saveIncidentPortNo = incidentPortNo;
            for (int portIndex = 0; portIndex < edgeCollectionList.Count; portIndex++)
            {
                EdgeCollection work = edgeCollectionList[portIndex];
                int no = work.No;
                work.No = portIndex + 1;
                if (no == saveIncidentPortNo)
                {
                    // 入射ポート番号の付け替え
                    incidentPortNo = work.No;
                }
            }
        }

        /// <summary>
        /// 辺の削除処理
        ///   危険　テスト実装
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private bool doEraseCadEdge(Point pt)
        {
            bool executed = false;

            if (CadMode != CadModeType.Erase)
            {
                return executed;
            }
            // ヒットテストで、ヒットしたパートの要素タイプとIdを取得する
            CAD_ELEM_TYPE partElemType;
            uint partId;
            bool hit = hitTest(pt, out partElemType, out partId);
            if (hit)
            {
                if (partElemType == CAD_ELEM_TYPE.EDGE && partId != 0 && EditCad2D.IsElemID(CAD_ELEM_TYPE.EDGE, partId))
                {
                    uint tagtEdgeId = partId;
                    // まずポート境界があれば削除
                    EdgeCollection hitEdge = getEdgeCollectionByEdgeId(tagtEdgeId);
                    if (hitEdge != null)
                    {
                        doErasePortCore(EditCad2D, hitEdge, ref EdgeCollectionList, ref IncidentPortNo);
                        executed = true;
                    }
                    // 辺を削除するとループが壊れるので壊れるループIDを記録する
                    uint brokenLoopId = 0;
                    {
                        // 辺のループIDを取得する
                        uint workLoopId = getLoopIdOfEdge(EditCad2D, tagtEdgeId);
                        // 辺を削除するとループが壊れるかチェックする
                        // 辺の両端に別の辺が接続していれば壊れる
                        // ＜壊れない例（除外したい）＞
                        //   +-------------+
                        //   |             |
                        //   |    +        |
                        //   |    |        |
                        //   +----+--------+
                        //
                        if (workLoopId != 0)
                        {
                            uint workVId1;
                            uint workVId2;
                            getVertexIdsOfEdgeId(EditCad2D, tagtEdgeId, out workVId1, out workVId2);
                            IList<uint> workEIdsOfVId1 = getEdgeIdsByVertexId(EditCad2D, workVId1);
                            IList<uint> workEIdsOfVId2 = getEdgeIdsByVertexId(EditCad2D, workVId2);
                            if (workEIdsOfVId1.Count >= 2 && workEIdsOfVId2.Count >= 2)
                            {
                                brokenLoopId = workLoopId;
                            }
                        }
                    }
                    // 辺の頂点を取得する
                    uint vId1 = 0;
                    uint vId2 = 0;
                    getVertexIdsOfEdgeId(EditCad2D, tagtEdgeId, out vId1, out vId2);

                    // エラーチェック用
                    // ループの辺IDを１つ取得しておく
                    uint exceptEId = tagtEdgeId; // 削除対象辺IDは除外して取得
                    // エラーチェック用
                    Dictionary<uint, IList<uint>> saveLoopEdgesList = null;
                    chkLoopEdges_PreProc(EditCad2D, LoopList, out saveLoopEdgesList);

                    if (brokenLoopId != 0)
                    {
                        // 壊れるループにポート境界があれば削除する
                        bool delport_exceuted = delPortBelongToLoop(EditCad2D, brokenLoopId, ref EdgeCollectionList, ref IncidentPortNo);
                        if (delport_exceuted)
                        {
                            if (!executed)
                            {
                                executed = true;
                            }
                        }
                    }

                    // 辺を削除
                    bool ret = EditCad2D.RemoveElement(CAD_ELEM_TYPE.EDGE, tagtEdgeId);
                    if (!ret)
                    {
                        // 失敗
                        MessageBox.Show("辺の削除に失敗しました");
                    }
                    else
                    {
                        if (!executed)
                        {
                            executed = true;
                        }

                        // 頂点を削除（辺に属していなけれは)
                        if (!isVertexOwnedByEdges(EditCad2D, vId1))
                        {
                            bool ret_rmVertex = EditCad2D.RemoveElement(CAD_ELEM_TYPE.VERTEX, vId1);
                            System.Diagnostics.Debug.Assert(ret_rmVertex);
                        }
                        if (!isVertexOwnedByEdges(EditCad2D, vId2))
                        {
                            bool ret_rmVertex = EditCad2D.RemoveElement(CAD_ELEM_TYPE.VERTEX, vId2);
                            System.Diagnostics.Debug.Assert(ret_rmVertex);
                        }

                        if (brokenLoopId != 0)
                        {
                            // 壊れたループのループ情報を削除する
                            //  Note:情報のみ削除する。ループの残骸の頂点や辺はCadオブジェクトから消去しない
                            Loop brokenLoop = getLoop(LoopList, brokenLoopId);
                            LoopList.Remove(brokenLoop);
                        }

                        // チェック用
                        // ループIDが変更されているかチェックし、変更されていればループ情報を更新する
                        chkLoopEdges_PostProc(EditCad2D, saveLoopEdgesList, brokenLoopId, tagtEdgeId, ref LoopList);
                    }
                    if (executed)
                    {
                        // 全ループの色を再設定する
                        SetupColorOfCadObjectsForAllLoops(EditCad2D, LoopList, Medias);
                    }
                }
            }
            if (executed)
            {
                // コマンドを実行する
                invokeCadOperationCmd();
                //  Note:ここでCadObjが新しくなるのでrefreshDrawerAry()をこの後実行すること
                // 描画オブジェクトアレイの更新フラグを立てる
                RefreshDrawerAryFlg = true;
                if (Change != null)
                {
                    Change(this, CadMode);
                }
            }
            if (executed && !isDirty)
            {
                isDirty = true;
            }

            return executed;
        }

        /// <summary>
        /// ループの線分の色を取得する
        /// </summary>
        /// <param name="loopColor"></param>
        /// <returns></returns>
        private static Color GetLoopLineColor(Color loopColor)
        {
            //Color loopLineColor = Color.FromArgb(0xff & (loopColor.R - 0x20), 0xff & (loopColor.G - 0x20), 0xff & (loopColor.B - 0x20));
            Color loopLineColor = Color.DarkGray;//Color.White;
            return loopLineColor;
        }

        /// <summary>
        /// Colorをdoule[]の規格化カラーに変換する
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static double[] ColorToColorDouble(Color color)
        {
            double[] colorDouble = new double[] { color.R / (double)255, color.G / (double)255, color.B / (double)255 };
            return colorDouble;
        }

        /// <summary>
        /// 規格化カラーをカラーに変換する
        /// </summary>
        /// <param name="colorDouble"></param>
        /// <returns></returns>
        public static Color ColorDoubleToColor(double[] colorDouble)
        {
            return Color.FromArgb((int)(colorDouble[0] * 255), (int)(colorDouble[1] * 255), (int)(colorDouble[2] * 255));
        }

        /// <summary>
        /// 入射ポートの選択
        /// </summary>
        /// <returns></returns>
        private bool doSelectIncidentPort(Point pt)
        {
            if (CadMode != CadModeType.IncidentPort)
            {
                return false;
            }
            bool executed = false;

            // ヒットテストで、ヒットしたパートの要素タイプとIdを取得する
            CAD_ELEM_TYPE partElemType;
            uint partId;
            bool hit = hitTest(pt, out partElemType, out partId);
            if (hit)
            {
                if (partElemType == CAD_ELEM_TYPE.EDGE && partId != 0 && EditCad2D.IsElemID(CAD_ELEM_TYPE.EDGE, partId))
                {
                    uint tagtEdgeId = partId;
                    EdgeCollection hitEdge = null;
                    int hitIndex = 0;
                    int portIndex = 0;
                    foreach (EdgeCollection work in EdgeCollectionList)
                    {
                        if (work.ContainsEdgeId(tagtEdgeId))
                        {
                            hitEdge = work;
                            hitIndex = portIndex;
                            break;
                        }
                        portIndex++;
                    }
                    if (hitEdge != null)
                    {
                        // ヒットしたポート番号
                        int hitPortNo = hitEdge.No;

                        IncidentPortNo = hitPortNo;

                        // ポートの色をセットする
                        //CadLogic.SetupColorOfPortEdgeCollection(EditCad2D, EdgeCollectionList, IncidentPortNo);

                        executed = true;
                    }
                }
            }
            if (executed)
            {
                // コマンドを実行する
                invokeCadOperationCmd();
                //  Note:ここでCadObjが新しくなるのでrefreshDrawerAry()をこの後実行すること
                // 描画オブジェクトアレイの更新フラグを立てる
                RefreshDrawerAryFlg = true;
                if (Change != null)
                {
                    Change(this, CadMode);
                }
            }
            if (executed && !isDirty)
            {
                isDirty = true;
            }
            return executed;
        }

        /// <summary>
        /// ポートの番号付与
        /// </summary>
        /// <returns></returns>
        private bool doNumberingPort(Point pt)
        {
            if (CadMode != CadModeType.PortNumbering)
            {
                return false;
            }
            bool executed = false;

            // ヒットテストで、ヒットしたパートの要素タイプとIdを取得する
            CAD_ELEM_TYPE partElemType;
            uint partId;
            bool hit = hitTest(pt, out partElemType, out partId);
            if (hit)
            {
                if (partElemType == CAD_ELEM_TYPE.EDGE && partId != 0 && EditCad2D.IsElemID(CAD_ELEM_TYPE.EDGE, partId))
                {
                    uint tagtEdgeId = partId;
                    EdgeCollection hitEdge = null;
                    int hitIndex = 0;
                    int portIndex = 0;
                    foreach (EdgeCollection work in EdgeCollectionList)
                    {
                        if (work.ContainsEdgeId(tagtEdgeId))
                        {
                            hitEdge = work;
                            hitIndex = portIndex;
                            break;
                        }
                        portIndex++;
                    }
                    if (hitEdge != null)
                    {
                        // ヒットしたポート番号
                        int hitPortNo = hitEdge.No;

                        int newNumber = PortNumberingSeq;
                        PortNumberingSeq++;
                        if (PortNumberingSeq > EdgeCollectionList.Count)
                        {
                            PortNumberingSeq = 1;
                        }

                        EdgeCollectionList[hitIndex].No = newNumber;
                        int newIncidentPortNo = -1;
                        for (int i = 0; i < EdgeCollectionList.Count; i++)
                        {
                            if (i == hitIndex) continue;
                            EdgeCollection edge = EdgeCollectionList[i];
                            int no = edge.No;
                            if (hitPortNo > newNumber)
                            {
                                if (edge.No >= newNumber && edge.No < hitPortNo)
                                {
                                    edge.No = edge.No + 1;
                                }
                            }
                            if (hitPortNo < newNumber)
                            {
                                if (edge.No > hitPortNo && edge.No <= newNumber)
                                {
                                    edge.No = edge.No - 1;
                                }
                            }
                            if (no == IncidentPortNo)
                            {
                                // 入射ポートの付け替え
                                //BUGFIXここでは、判定に使用しているIncidentPortNoを変更しない!!!!!
                                //IncidentPortNo = edge.No;
                                newIncidentPortNo = edge.No;
                            }
                            //Console.Write("{0},", edge.No);
                        }
                        if (newIncidentPortNo != -1)
                        {
                            IncidentPortNo = newIncidentPortNo;
                        }

                        //Console.WriteLine(" ");
                        // 番号順に並び替え
                        ((List<EdgeCollection>)EdgeCollectionList).Sort();
                        // ポートの色をセットする
                        //CadLogic.SetupColorOfPortEdgeCollection(EditCad2D, EdgeCollectionList, IncidentPortNo);

                        executed = true;
                    }
                }
            }
            if (executed)
            {
                // コマンドを実行する
                invokeCadOperationCmd();
                //  Note:ここでCadObjが新しくなるのでrefreshDrawerAry()をこの後実行すること
                // 描画オブジェクトアレイの更新フラグを立てる
                RefreshDrawerAryFlg = true;
                if (Change != null)
                {
                    Change(this, CadMode);
                }
            }
            if (executed && !isDirty)
            {
                isDirty = true;
            }
            return executed;
        }

        /// <summary>
        /// Cadオブジェクトのループとその辺、頂点の色をセットする
        /// </summary>
        /// <param name="cad2D"></param>
        /// <param name="id_l"></param>
        /// <param name="backColor"></param>
        public static void SetupColorOfCadObjectsForOneLoop(CCadObj2D cad2d, uint id_l, Color backColor)
        {
            // ループの頂点と辺のリストを取得する
            IList<uint> vIdList = null;
            IList<uint> eIdList = null;
            CadLogic.GetEdgeVertexListOfLoop(cad2d, id_l, out vIdList, out eIdList);

            // ループの色を指定（指定しなければ(0.9,0.8,0.8)になる
            double[] backColorDouble = CadLogic.ColorToColorDouble(backColor);
            cad2d.SetColor_Loop(id_l, backColorDouble);
            // 辺、頂点の色の変更
            Color loopLineColor = CadLogic.GetLoopLineColor(backColor);
            double[] lineColorDouble = CadLogic.ColorToColorDouble(loopLineColor);
            // 辺の色
            foreach (uint id_e in eIdList)
            {
                cad2d.SetColor_Edge(id_e, lineColorDouble);
            }
            // 頂点の色
            //foreach (uint id_v in vIdList)
            //{
            //    cad2d.SetColor_Vertex(id_v, lineColorDouble);
            //}
        }

        /// <summary>
        /// Cadオブジェクトのポート境界の色をセットする
        /// </summary>
        /// <param name="cad2D"></param>
        /// <param name="workEdgeCollectionList"></param>
        /// <param name="incidentPortNo"></param>
        public static void SetupColorOfPortEdgeCollection(CCadObj2D cad2D, IList<EdgeCollection> workEdgeCollectionList, int incidentPortNo)
        {
            // ポート境界の辺の色
            foreach (EdgeCollection edgeCollection in workEdgeCollectionList)
            {
                int portNo = edgeCollection.No;
                foreach (uint id_e in edgeCollection.EdgeIds)
                {
                    Color portColor = portNo == incidentPortNo ? CadLogic.IncidentPortColor : CadLogic.PortColor;
                    double[] portColorDouble = CadLogic.ColorToColorDouble(portColor);
                    cad2D.SetColor_Edge(id_e, portColorDouble);
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // CadObj2Dのユーティリティ
        /// <summary>
        /// ループの内側にあるループを子ループに設定する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="loopId"></param>
        /// <param name="loopList"></param>
        /// <param name="edgeCollectionList"></param>
        /// <param name="medias"></param>
        /// <param name="incidentPortNo"></param>
        public static void reconstructLoopsInsideLoopAsChild(CCadObj2D cad2d, uint loopId,
            ref IList<Loop> loopList, ref IList<EdgeCollection> edgeCollectionList, MediaInfo[] medias, ref int incidentPortNo)
        {
            if (loopId == 0)
            {
                System.Diagnostics.Debug.Assert(false);
                return;
            }

            //   内側にあるループIDのリストを取得する
            IList<uint> insideLoopIds = getLoopIdsInsideLoop(cad2d, loopId, loopList);
            //  子ループに設定
            foreach (uint childLoopId in insideLoopIds)
            {
                setLoop_ParentLoopId(cad2d, childLoopId, loopId, ref loopList, ref edgeCollectionList, medias, ref incidentPortNo);
            }
        }

        /// <summary>
        /// ループの内側にあるループのIDのリストを取得する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="loopId"></param>
        /// <param name="loopList"></param>
        /// <returns></returns>
        public static IList<uint> getLoopIdsInsideLoop(CCadObj2D cad2d, uint loopId, IList<Loop> loopList)
        {
            IList<uint> hitLoopIds = new List<uint>();
            if (loopId == 0)
            {
                System.Diagnostics.Debug.Assert(false);
                return hitLoopIds;
            }
            foreach (Loop workLoop in loopList)
            {
                if (loopId == workLoop.LoopId)
                {
                    continue;
                }
                // ワークループの１点をヒットテストする
                //   先ず頂点を取得
                uint vId = 0;
                {
                    IList<uint> vIdList = null;
                    IList<uint> eIdList = null;
                    GetEdgeVertexListOfLoop(cad2d, workLoop.LoopId, out vIdList, out eIdList);
                    if (vIdList.Count > 0)
                    {
                        vId = vIdList[0];
                    }
                }
                System.Diagnostics.Debug.Assert(vId != 0);
                // ワークループの頂点の座標を取得
                CVector2D v_pp = cad2d.GetVertexCoord(vId);
                // ワークループの頂点が、指定ループの内側の点か？
                bool inside = cad2d.CheckIsPointInsideLoop(loopId, v_pp);
                if (inside)
                {
                    hitLoopIds.Add(workLoop.LoopId);
                }
            }
            return hitLoopIds;
        }

        /// <summary>
        /// ループに親ループIDを設定する
        ///   子ループを削除して、親ループの子ループとして再作成する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="childLoopId">子ループID</param>
        /// <param name="parentLoopId">親ループID</param>
        /// <param name="loopList">ループ情報リスト</param>
        /// <param name="edgeCollectionList">ポート境界エッジコレクションのリスト</param>
        /// <param name="medias">媒質リスト</param>
        /// <param name="incidentPortNo">入射ポート番号</param>
        public static void setLoop_ParentLoopId(CCadObj2D cad2d, uint childLoopId, uint parentLoopId,
            ref IList<Loop> loopList, ref IList<EdgeCollection> edgeCollectionList, MediaInfo[] medias, ref int incidentPortNo)
        {
            if (childLoopId == 0 || parentLoopId == 0)
            {
                System.Diagnostics.Debug.Assert(false);
                return;
            }

            // 方針：子ループを削除して再設定する

            /* NG 再構築しないと駄目
            uint workParentId = getParentLoopId(cad2d, CAD_ELEM_TYPE.LOOP, childLoopId);
            if (workParentId != 0)
            {
                // どこかのループの子ループに設定済みの場合は何もしない
                return;
            }
             */

            // 子ループのループ情報を退避
            Loop childLoop = getLoop(loopList, childLoopId);
            Loop childLoopTmp = new Loop(childLoop.LoopId, childLoop.MediaIndex);
            // 子ループの頂点座標を取得する
            IList<CVector2D> v_pps = new List<CVector2D>();
            {
                IList<uint> vIdList = null;
                IList<uint> eIdList = null;
                GetEdgeVertexListOfLoop(cad2d, childLoopId, out vIdList, out eIdList);
                foreach (uint vId in vIdList)
                {
                    CVector2D v_pp = cad2d.GetVertexCoord(vId);
                    v_pps.Add(v_pp);
                }
            }
            // 子ループを削除
            bool del_ret = delLoop(cad2d, childLoopId, ref loopList, ref edgeCollectionList, medias, ref incidentPortNo);
            if (!del_ret)
            {
                MessageBox.Show("子ループの設定に失敗しました");
            }
            else
            {
                // ループを再追加
                //uint id_l_add = cad2d.AddPolygon(v_pps, parentLoopId).id_l_add;
                uint id_l_add = makeLoop(cad2d, v_pps, loopList, true);
                if (id_l_add == 0)
                {
                    MessageBox.Show("子ループの設定に失敗しました");
                }
                else
                {
                    // 子ループの作成に成功

                    // ループ情報を再登録
                    childLoopTmp.Set(id_l_add, childLoopTmp.MediaIndex);
                    loopList.Add(childLoopTmp);

                    // ループの色を設定
                    MediaInfo media = medias[childLoopTmp.MediaIndex];
                    Color backColor = media.BackColor;
                    SetupColorOfCadObjectsForOneLoop(cad2d, id_l_add, backColor);
                }
            }
        }

        /// <summary>
        /// ヒットテスト結果のパーツID、要素タイプを用いて親ループIDを特定する
        /// </summary>
        /// <param name="partId"></param>
        /// <param name="partElemType"></param>
        public static uint getParentLoopId(CCadObj2D cad2d, CAD_ELEM_TYPE partElemType, uint partId)
        {
            uint parentLoopId = 0;
            if (partId != 0 && partElemType == CAD_ELEM_TYPE.LOOP)
            {
                parentLoopId = partId;
            }
            else if (partId != 0 && partElemType == CAD_ELEM_TYPE.EDGE)
            {
                uint id_e = partId;
                parentLoopId = getLoopIdOfEdge(cad2d, id_e);
            }
            else if (partId != 0 && partElemType == CAD_ELEM_TYPE.VERTEX)
            {
                uint id_v = partId;
                parentLoopId = getLoopIdOfVertex(cad2d, id_v);
            }
            else
            {
                parentLoopId = 0;
            }
            Console.WriteLine("parentLoopId:{0}", parentLoopId);
            return parentLoopId;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 隣り合う辺か？
        /// </summary>
        /// <param name="eId1"></param>
        /// <param name="eId2"></param>
        /// <returns></returns>
        public static bool isNextDoorEdge(CCadObj2D cad2d, uint eId1, uint eId2)
        {
            bool isNextDoor = false;
            // ここで隣の辺のみに限定したい
            CEdge2D tagtEdge = cad2d.GetEdge(eId1);
            uint[] tagtVtx = new uint[] { tagtEdge.GetIdVtx(true), tagtEdge.GetIdVtx(false) };
            CEdge2D prevEdge = cad2d.GetEdge(eId2);
            uint[] prevVtx = new uint[] { prevEdge.GetIdVtx(true), prevEdge.GetIdVtx(false) };
            if (tagtVtx[0] == prevVtx[0] || tagtVtx[0] == prevVtx[1] ||
                tagtVtx[1] == prevVtx[0] || tagtVtx[1] == prevVtx[1])
            {
                isNextDoor = true;
            }
            return isNextDoor;
        }

        /// <summary>
        /// ループの頂点と辺のIDのリストを取得する
        /// </summary>
        /// <param name="EditCad2D"></param>
        /// <param name="id_l"></param>
        /// <param name="vIdList"></param>
        /// <param name="eIdList"></param>
        public static void GetEdgeVertexListOfLoop(CCadObj2D cad2d, uint id_l, out IList<uint> vIdList, out IList<uint> eIdList)
        {
            //Console.WriteLine("GetEdgeVertexListOfLoop:id_l = {0}", id_l);
            vIdList = new List<uint>();
            eIdList = new List<uint>();
            if (id_l == 0)
            {
                System.Diagnostics.Debug.Assert(false);
                return;
            }
            for (CBRepSurface.CItrLoop itrl = cad2d.GetItrLoop(id_l); !itrl.IsEndChild(); itrl.ShiftChildLoop())
            {
                if (!itrl.IsParent())
                {
                    // 親でないならスキップする
                    continue;
                }
                for (itrl.Begin(); !itrl.IsEnd(); itrl.Increment())
                {
                    uint id_e;
                    bool is_same_dir;
                    itrl.GetIdEdge(out id_e, out is_same_dir);
                    uint id_v = itrl.GetIdVertex();
                    eIdList.Add(id_e);
                    vIdList.Add(id_v);
                    //Console.WriteLine("    id_v = {0} id_e = {1}", id_v, id_e);
                }
            }
        }

        /// <summary>
        /// 頂点IDからループIDを取得する
        /// </summary>
        /// <param name="vId"></param>
        /// <returns></returns>
        public static uint getLoopIdOfVertex(CCadObj2D cad2d, uint vId)
        {
            uint loopId = 0;
        
            for (CBRepSurface.CItrVertex itrv = cad2d.GetItrVertex(vId); !itrv.IsEnd(); itrv.Increment())
            {
                uint id_l = itrv.GetIdLoop();
                if (id_l != 0)
                {
                    loopId = id_l;
                    break;
                }
            }
        
            return loopId;
        }

        /// <summary>
        /// 辺IDからループIDを取得する
        /// </summary>
        /// <param name="eId">辺ID</param>
        /// <returns>ループID</returns>
        public static uint getLoopIdOfEdge(CCadObj2D cad2d, uint eId)
        {
            uint loopId = 0;
            uint id_l_l;
            uint id_l_r;
            cad2d.GetIdLoop_Edge(out id_l_l, out id_l_r, eId);
            // ループから見て孤立した辺は除外する
            if (id_l_l != 0 && isStandAloneEdgeInsideLoop(cad2d, eId, id_l_l))
            {
                id_l_l = 0;
            }
            if (id_l_r != 0 && isStandAloneEdgeInsideLoop(cad2d, eId, id_l_r))
            {
                id_l_r = 0;
            }

            //System.Diagnostics.Debug.Assert(id_l_l != 0 || id_l_r != 0);
            if (id_l_l != 0)
            {
                loopId = id_l_l;
            }
            else if (id_l_r != 0)
            {
                loopId = id_l_r;
            }
            else
            {
                // 不完全な辺の作成も許可するようにしたので、Assertを外す
                Console.WriteLine("[Warn]getLoopIdOfEdge: id_l_l == 0 && id_l_r == 0");
                //System.Diagnostics.Debug.Assert(false);
                loopId = 0;
            }

            return loopId;
        }

        /// <summary>
        /// 辺がループ内部の孤立した辺か？
        ///   GetIdLoop_Edgeがループ内部の孤立した辺の場合も含んでいるのでそれを除外したい場合に使用する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="eId"></param>
        /// <param name="loopId"></param>
        /// <returns></returns>
        public static  bool isStandAloneEdgeInsideLoop(CCadObj2D cad2d, uint eId, uint loopId)
        {
            bool standAlone = false;
            if (eId == 0 || loopId == 0)
            {
                System.Diagnostics.Debug.Assert(false);
                return standAlone;
            }

            IList<uint> workVIdList = null;
            IList<uint> workEIdList = null;
            GetEdgeVertexListOfLoop(cad2d, loopId, out workVIdList, out workEIdList);
            if (workEIdList.IndexOf(eId) == -1)
            {
                standAlone = true;
            }
            return standAlone;
        }

        /// <summary>
        /// 辺が２つのループに共有されているか?
        /// </summary>
        /// <param name="eId"></param>
        /// <returns></returns>
        public static bool isEdgeSharedByLoops(CCadObj2D cad2d, uint eId)
        {
            uint id_l_l;
            uint id_l_r;
            cad2d.GetIdLoop_Edge(out id_l_l, out id_l_r, eId);
            /*これは孤立した辺でもよい --> 包含関係にある場合、親ループから見て子ループの辺は孤立した辺なので
            // ループから見て孤立した辺は除外する
            if (id_l_l != 0 && isStandAloneEdgeInsideLoop(cad2d, eId, id_l_l))
            {
                id_l_l = 0;
            }
            if (id_l_r != 0 && isStandAloneEdgeInsideLoop(cad2d, eId, id_l_r))
            {
                id_l_r = 0;
            }
             */
            return (id_l_l != 0 && id_l_r != 0);
        }

        /// <summary>
        /// 隣のループのIDを取得する
        /// </summary>
        /// <param name="tagtEdgeId"></param>
        /// <param name="tagtLoopId"></param>
        /// <returns></returns>
        public static uint getNextDoorLoopId(CCadObj2D cad2d, uint tagtEdgeId, uint tagtLoopId)
        {
            uint nextDoorLoopId = 0;
            uint id_l_l;
            uint id_l_r;
            cad2d.GetIdLoop_Edge(out id_l_l, out id_l_r, tagtEdgeId);
            /*これは孤立した辺でもよい --> 包含関係にある場合、親ループから見て子ループの辺は孤立した辺なので
            // ループから見て孤立した辺は除外する
            if (id_l_l != 0 && isStandAloneEdgeInsideLoop(cad2d, tagtEdgeId, id_l_l))
            {
                id_l_l = 0;
            }
            if (id_l_r != 0 && isStandAloneEdgeInsideLoop(cad2d, tagtEdgeId, id_l_r))
            {
                id_l_r = 0;
            }
             */

            if (id_l_l != 0 && id_l_l != tagtLoopId)
            {
                nextDoorLoopId = id_l_l;
            }
            else if (id_l_r != 0 && id_l_r != tagtLoopId)
            {
                nextDoorLoopId = id_l_r;
            }
            return nextDoorLoopId;
        }

        /// <summary>
        /// ループIDを指定してループ情報を取得する
        /// </summary>
        /// <param name="loopId"></param>
        /// <returns></returns>
        public static Loop getLoop(IList<Loop> loopList, uint loopId)
        {
            Loop hitLoop = null;
            foreach (Loop workLoop in loopList)
            {
                if (workLoop.LoopId == loopId)
                {
                    hitLoop = workLoop;
                    break;
                }
            }
            return hitLoop;
        }

        /// <summary>
        /// 指定された座標がループの頂点なら頂点ID、ループIDのペアをリストで返却する
        /// </summary>
        /// <param name="cad2d">Cadオブジェクト</param>
        /// <param name="chkPt">チェックする点</param>
        /// <param name="loopList">これまでに追加されたループリスト</param>
        public static void getVertexIdBelongToLoopByCoord(CCadObj2D cad2d, CVector2D chkPt, IList<CadLogic.Loop> loopList, out uint out_vertexId, out IList<uint> out_loopIdList)
        {
            out_loopIdList = new List<uint>();
            out_vertexId = 0;
            foreach (Loop loop in loopList)
            {
                uint workLoopId = loop.LoopId;

                // ループの頂点、辺のIDのリストを取得する
                IList<uint> vIdList = null;
                IList<uint> eIdList = null;
                CadLogic.GetEdgeVertexListOfLoop(cad2d, workLoopId, out vIdList, out eIdList);

                foreach (uint vId in vIdList)
                {
                    CVector2D vertexPt = cad2d.GetVertexCoord(vId);
                    if (CVector2D.Distance(vertexPt, chkPt) < Constants.PrecisionLowerLimit)
                    {
                        if (out_vertexId == 0)
                        {
                            out_vertexId = vId;
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(out_vertexId == vId);
                        }
                        out_loopIdList.Add(workLoopId);
                    }
                }
            }
        }

        /// <summary>
        /// 頂点IDから辺IDのリストを取得する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="id_v"></param>
        /// <returns></returns>
        public static IList<uint> getEdgeIdsByVertexId(CCadObj2D cad2d, uint id_v)
        {
            IList<uint> eIdList = new List<uint>();
            if (id_v != 0)
            {
                for (CBRepSurface.CItrVertex itrv = cad2d.GetItrVertex(id_v); !itrv.IsEnd(); itrv.Increment())
                {
                    bool ret;
                    uint id_e;
                    bool is_same_dir;
                    id_e = 0;
                    ret = itrv.GetIdEdge_Behind(out id_e, out is_same_dir);
                    if (ret && id_e != 0)
                    {
                        if (eIdList.IndexOf(id_e) == -1)
                        {
                            eIdList.Add(id_e);
                        }
                    }
                    id_e = 0;
                    ret = itrv.GetIdEdge_Ahead(out id_e, out is_same_dir);
                    if (ret && id_e != 0)
                    {
                        if (eIdList.IndexOf(id_e) == -1)
                        {
                            eIdList.Add(id_e);
                        }
                    }
                }
            }
            return eIdList;
        }

        /// <summary>
        /// ２つの頂点IDからなる辺IDを取得する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="id_v1"></param>
        /// <param name="id_v2"></param>
        /// <returns></returns>
        public static uint getEdgeIdOfVertexIds(CCadObj2D cad2d, uint id_v1, uint id_v2)
        {
            uint retEId = 0;

            System.Diagnostics.Debug.Assert(id_v1 != id_v2);
            if (id_v1 == id_v2)
            {
                return retEId;
            }

            IList<uint> eIdList1 = getEdgeIdsByVertexId(cad2d, id_v1);
            IList<uint> eIdList2 = getEdgeIdsByVertexId(cad2d, id_v2);

            foreach (uint workEId in eIdList1)
            {
                if (eIdList2.IndexOf(workEId) >= 0)
                {
                    retEId = workEId;
                    break;
                }
            }

            // check
            if (retEId != 0)
            {
                uint work_id_v1 = 0;
                uint work_id_v2 = 0;
                getVertexIdsOfEdgeId(cad2d, retEId, out work_id_v1, out work_id_v2);
                System.Diagnostics.Debug.Assert((work_id_v1 == id_v1 && work_id_v2 == id_v2) || (work_id_v1 == id_v2 && work_id_v2 == id_v1));
            }


            return retEId;
        }

        /// <summary>
        /// 辺IDから頂点ID２つを取得する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="id_e"></param>
        /// <param name="id_v1"></param>
        /// <param name="id_v2"></param>
        public static void getVertexIdsOfEdgeId(CCadObj2D cad2d, uint id_e, out uint id_v1, out uint id_v2)
        {
            cad2d.GetIdVertex_Edge(out id_v1, out id_v2, id_e);
            /*
            id_v1 = 0;
            id_v2 = 0;
            CEdge2D edge2d =cad2d.GetEdge(id_e);
            id_v1 = edge2d.GetIdVtx(true);
            id_v2 = edge2d.GetIdVtx(false);
             */
        }

        /// <summary>
        /// 頂点が辺に所有されているか？
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="vId"></param>
        /// <returns></returns>
        public static bool isVertexOwnedByEdges(CCadObj2D cad2d, uint vId)
        {
            bool owned = false;
            if (vId == 0)
            {
                System.Diagnostics.Debug.Assert(false);
                return owned;
            }

            CBRepSurface.CItrVertex itrv = cad2d.GetItrVertex(vId);
            int edgeCnt = 0;
            for (itrv.Begin(); !itrv.IsEnd(); itrv.Increment())
            {
                uint id_e;
                bool is_same_dir;
                itrv.GetIdEdge_Ahead(out id_e, out is_same_dir);
                if (id_e == 0)
                {
                    itrv.GetIdEdge_Behind(out id_e, out is_same_dir);
                }
                if (id_e != 0)
                {
                    edgeCnt++;
                }
            }
            System.Diagnostics.Debug.Assert(edgeCnt == itrv.CountEdge());
            if (edgeCnt > 0)
            {
                owned = true;
            }

            return owned;
        }

        /// <summary>
        /// 指定された点を包含するループのIDを取得する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="chkPt">チェックする点</param>
        /// <param name="loopList">これまでに追加されたループのリスト</param>
        /// <returns></returns>
        public static uint getLoopIdIncludingPoint(CCadObj2D cad2d, CVector2D chkPt, IList<Loop> loopList)
        {
            uint hit_id_l = 0;
            foreach (Loop loop in loopList)
            {
                bool ret = cad2d.CheckIsPointInsideLoop(loop.LoopId, chkPt);
                if (ret)
                {
                    hit_id_l = loop.LoopId;
                    break;
                }
            }
            return hit_id_l;
        }

        /// <summary>
        /// 指定ポイントが辺上にあれば辺IDを返却する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="chkPt">チェックするポイント</param>
        /// <param name="loopList">これまでに追加されたループのリスト</param>
        /// <returns>辺ID</returns>
        public static uint getEdgeIdIncludingPoint(CCadObj2D cad2d, CVector2D chkPt, IList<Loop> loopList)
        {
            uint hit_eId = 0;
            foreach (Loop loop in loopList)
            {
                IList<uint> vIdList = null;
                IList<uint> eIdList = null;
                GetEdgeVertexListOfLoop(cad2d, loop.LoopId, out vIdList, out eIdList);
                
                foreach (uint eId in eIdList)
                {
                    bool isOnEdge = isPointOnEdge(cad2d, eId, chkPt);
                    if (isOnEdge)
                    {
                        hit_eId = eId;
                        break;
                    }
                }
            }
            return hit_eId;
        }

        /// <summary>
        /// 指定ポイントが辺上にある？
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="eId">辺ID</param>
        /// <param name="chkPt">チェックするポイント</param>
        /// <returns></returns>
        public static bool isPointOnEdge(CCadObj2D cad2d, uint eId, CVector2D chkPt)
        {
            bool isOnEdge = false;
            uint id_v1 = 0;
            uint id_v2 = 0;
            getVertexIdsOfEdgeId(cad2d, eId, out id_v1, out id_v2);
            CVector2D pp_v1 = cad2d.GetVertexCoord(id_v1);
            CVector2D pp_v2 = cad2d.GetVertexCoord(id_v2);
            isOnEdge = isPointOnEdge(pp_v1, pp_v2, chkPt);
            return isOnEdge;
        }

        /// <summary>
        /// 指定ポイントが辺上にある？
        /// </summary>
        /// <param name="pp_v1">辺の始点の座標</param>
        /// <param name="pp_v2">辺の終点の座標</param>
        /// <param name="chkPt">チェックするポイント</param>
        /// <returns></returns>
        public static bool isPointOnEdge(CVector2D pp_v1, CVector2D pp_v2, CVector2D chkPt)
        {
            bool isOnEdge = false;
            /*
            double area = CVector2D.TriArea(pp_v1, pp_v2, chkPt);

            // 辺上の点だったら面積が０になるはず
            // -->間違い 直線上だったら面積が０になる(辺の延長線上の点でも０になる）
            if (Math.Abs(area) < Constants.PrecisionLowerLimit)
            {
                isOnEdge = true;
            }
             */
            // 辺上だったらその点がつくる２つの辺の長さの和が元の辺の長さのはず
            double le = CVector2D.Distance(pp_v1, pp_v2);
            double le1 = CVector2D.Distance(pp_v1, chkPt);
            double le2 = CVector2D.Distance(pp_v2, chkPt);
            if (Math.Abs(le - (le1 + le2)) < Constants.PrecisionLowerLimit)
            {
                isOnEdge = true;
            }
            return isOnEdge;
        }

        /// <summary>
        /// 辺が辺を含む？
        /// </summary>
        /// <param name="cad2d">Cadオブジェクト</param>
        /// <param name="pp_v1">辺の始点</param>
        /// <param name="pp_v2">辺の終点</param>
        /// <param name="chkEId">含むかチェックする辺のID</param>
        /// <returns></returns>
        public static bool isEdgeIncludingEdge(CCadObj2D cad2d, CVector2D pp_v1, CVector2D pp_v2, uint chkEId)
        {
            bool isIncluding = false;

            // チェックする辺の始点と終点を取得
            uint chk_id_v1 = 0;
            uint chk_id_v2 = 0;
            getVertexIdsOfEdgeId(cad2d, chkEId, out chk_id_v1, out chk_id_v2);
            CVector2D chk_pp_v1 = cad2d.GetVertexCoord(chk_id_v1);
            CVector2D chk_pp_v2 = cad2d.GetVertexCoord(chk_id_v2);

            // チェックする辺の始点と終点がどちらも元の辺の辺上なら、チェックする辺は元の辺に含まれる
            bool chk1 = isPointOnEdge(pp_v1, pp_v2, chk_pp_v1);
            bool chk2 = isPointOnEdge(pp_v1, pp_v2, chk_pp_v2);
            if (chk1 && chk2)
            {
                isIncluding = true;
            }
            return isIncluding;
        }

        /// <summary>
        /// これから作成する辺が他の辺を含んでいるかチェックし、含んでいればその辺IDを返却する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="newEdgeVId">これから作成する辺の始点</param>
        /// <param name="chkPt">これから作成する辺の終点ポイント</param>
        /// <param name="loopList">これまでに追加されたループのリスト</param>
        /// <returns>辺IDのリスト</returns>
        public static IList<uint> getEdgeListIdIncludedByNewEdge(CCadObj2D cad2d, CVector2D chk_pp_v1, CVector2D chk_pp_v2, IList<Loop> loopList)
        {
            IList<uint> hitEIdList = new List<uint>();

            foreach (Loop loop in loopList)
            {
                IList<uint> vIdList = null;
                IList<uint> eIdList = null;
                GetEdgeVertexListOfLoop(cad2d, loop.LoopId, out vIdList, out eIdList);

                foreach (uint eId in eIdList)
                {
                    bool isIncluding = isEdgeIncludingEdge(cad2d, chk_pp_v1, chk_pp_v2, eId);
                    if (isIncluding)
                    {
                        hitEIdList.Add(eId);
                    }
                }
            }
            return hitEIdList;
        }

        /// <summary>
        /// これから作成する辺が既存の辺を含んでいるかチェックし、含んでいれば（複数の辺の可能性あり）開始頂点ID、終了頂点IDを返却する
        /// </summary>
        /// <param name="cad2d"></param>
        /// <param name="pp_v1">これから作成する辺の始点</param>
        /// <param name="pp_v2">これから作成する辺の終点</param>
        /// <param name="loopList">これまでに追加したループのリスト</param>
        /// <param name="minDistance_vId">開始頂点ID</param>
        /// <param name="maxDistance_vId">終了頂点ID</param>
        public static void getIncludedEdgesStEndVId(CCadObj2D cad2d, CVector2D pp_v1, CVector2D pp_v2, IList<Loop> loopList, out uint minDistance_vId, out uint maxDistance_vId)
        {
            minDistance_vId = 0;
            maxDistance_vId = 0;

            IList<uint> includedEIds = getEdgeListIdIncludedByNewEdge(cad2d, pp_v1, pp_v2, loopList);
            if (includedEIds.Count != 0)
            {
                // 下記のみを考慮
                // index_pp -1                                     index_pp
                // +----------------+--------既存の辺------+----------+
                //              minDistance_vId        maxDistance_vId
                // 既存の辺がさらに歯抜けになる場合は、....エラーとして返せるか？
                double minDistance = double.MaxValue;
                double maxDistance = double.MinValue;
                foreach (uint includedEId in includedEIds)
                {
                    // 含まれる辺の頂点IDを取得
                    uint id_v1_of_includedEdge = 0;
                    uint id_v2_of_includedEdge = 0;
                    getVertexIdsOfEdgeId(cad2d, includedEId, out id_v1_of_includedEdge, out id_v2_of_includedEdge);
                    CVector2D pp_v1_of_includedEdge = cad2d.GetVertexCoord(id_v1_of_includedEdge);
                    CVector2D pp_v2_of_includedEdge = cad2d.GetVertexCoord(id_v2_of_includedEdge);
                    // これから作成する辺の始点から頂点までの距離を取得
                    double d1 = CVector2D.Distance(pp_v1, pp_v1_of_includedEdge);
                    double d2 = CVector2D.Distance(pp_v1, pp_v2_of_includedEdge);
                    if (d1 > maxDistance)
                    {
                        maxDistance = d1;
                        maxDistance_vId = id_v1_of_includedEdge;
                    }
                    if (d1 < minDistance)
                    {
                        minDistance = d1;
                        minDistance_vId = id_v1_of_includedEdge;
                    }
                    if (d2 > maxDistance)
                    {
                        maxDistance = d2;
                        maxDistance_vId = id_v2_of_includedEdge;
                    }
                    if (d2 < minDistance)
                    {
                        minDistance = d2;
                        minDistance_vId = id_v2_of_includedEdge;
                    }
                }
            }
        }

        /// <summary>
        /// ループを作成する
        /// </summary>
        /// <param name="cad2d">Cadオブジェクト</param>
        /// <param name="pps">追加するループの多角形の頂点リスト(ループを閉じる終点は含まない)</param>
        /// <param name="loopList">これまでに追加されたループのリスト</param>
        /// <returns></returns>
        public static uint makeLoop(CCadObj2D cad2d, IList<CVector2D> pps, IList<Loop> loopList, bool showErrorFlg)
        {
            // 多角形でループを作成するのを止める
            //uint id_l = out_cad2d.AddPolygon(pps, baseLoopId).id_l_add;

            uint id_l = 0;

            IList<CVector2D> addPts = new List<CVector2D>();
            IList<uint> addVertexIds = new List<uint>();
            IList<uint> addEdgeIds = new List<uint>();
            IList<uint> addLoopIds = new List<uint>();
            for (int index_pp = 0; index_pp < pps.Count; index_pp++)
            {
                int prevAddLoopCnt = addLoopIds.Count;
                /////////////////
                CVector2D pp = pps[index_pp];
                int work_index_pp = index_pp;
                bool executed;
                executed = doMakeDisconAreaCore(cad2d, pp, work_index_pp, loopList,
                    ref addPts, ref addVertexIds, ref addEdgeIds, ref addLoopIds, showErrorFlg);
                if (addLoopIds.Count != prevAddLoopCnt)
                {
                    //
                    //addPts.Clear();
                    //addVertexIds.Clear();
                    //addEdgeIds.Clear();
                }
                // 最後の辺の追加
                if (index_pp == pps.Count - 1 && addVertexIds.Count > 0)
                {
                    prevAddLoopCnt = addLoopIds.Count;
                    // 先頭の頂点と接続する
                    pp = pps[0];
                    work_index_pp = pps.Count;
                    executed = doMakeDisconAreaCore(cad2d, pp, work_index_pp, loopList,
                        ref addPts, ref addVertexIds, ref addEdgeIds, ref addLoopIds, showErrorFlg);
                    if (addLoopIds.Count != prevAddLoopCnt)
                    {
                        //
                        //addPts.Clear();
                        //addVertexIds.Clear();
                        //addEdgeIds.Clear();
                    }
                }
            }
            if (addLoopIds.Count > 0)
            {
                id_l = addLoopIds[0];
                System.Diagnostics.Debug.Assert(addLoopIds.Count == 1);
            }

            return id_l;
        }

        /// <summary>
        /// Cadデータの書き込み
        /// </summary>
        public void SerializeCadData(string filename)
        {
            isDirty = false;
            IsBackupFile = false;

            // ファイルへ書き込む
            CadDatFile.SaveToFile(
                filename,
                EditCad2D,
                LoopList,
                EdgeCollectionList,
                IncidentPortNo,
                Medias
            );

            /*
            // 保存したら元に戻すはクリアする
            // Mementoの破棄
            if (CadLogicBaseMmnt != null)
            {
                CadLogicBaseMmnt.Dispose();
                CadLogicBaseMmnt = null;
            }

            // Memento初期化
            // 現在の状態をMementoに記憶させる
            setMemento();
            // コマンド管理初期化
            CmdManager.Refresh();
             */
        }

        /// <summary>
        /// Cadデータの読み込み
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool DeserializeCadData(string filename)
        {
            bool success = false;

            init();
            isDirty = false;
            IsBackupFile = false;

            string out_appVersion;
            string out_useUtility;

            // Ver1.3.0.0
            CCadObj2D cad2dref = EditCad2D;
            success = CadDatFile.LoadFromFile(
                filename,
                out out_appVersion,
                out out_useUtility,
                ref cad2dref,
                ref LoopList,
                ref EdgeCollectionList,
                out IncidentPortNo,
                ref Medias);
            System.Diagnostics.Debug.Assert(cad2dref == EditCad2D);

            if (!success)
            {
                // 旧形式で読み込んでみる
                init();

                // Ver1.2.0.0形式のデータを読み込む
                DialogResult dialogResult = MessageBox.Show("Version1.2.0.0形式のファイルとして読み込みます。", "", MessageBoxButtons.OKCancel);
                if (dialogResult == DialogResult.OK)
                {
                    bool isBackupFile = false;
                    success = CadDatFile_Ver_1_2.LoadFromFile_Ver_1_2(
                        filename,
                        out isBackupFile,
                        ref cad2dref,
                        ref LoopList,
                        ref EdgeCollectionList,
                        out IncidentPortNo,
                        ref Medias
                        );
                    System.Diagnostics.Debug.Assert(cad2dref == EditCad2D);
                    IsBackupFile = isBackupFile;
                    if (!success)
                    {
                        // 読み込みに失敗
                        MessageBox.Show("エラーが発生しました。すべて変換できなかった可能性があります。", "Ver1.2.0.0形式のデータ取り込み", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        // 読み込み成功
                        if (!isBackupFile) // バックアップファイルでない場合保存する
                        {
                            // 新形式で保存
                            SerializeCadData(filename);
                        }
                    }
                }
            }

            // 読み込んだ後の情報でメメントを初期化する
            // Mementoの破棄
            if (CadLogicBaseMmnt != null)
            {
                CadLogicBaseMmnt.Dispose();
                CadLogicBaseMmnt = null;
            }
            // Memento初期化
            // 現在の状態をMementoに記憶させる
            setMemento();
            // コマンド管理初期化
            CmdManager.Refresh();

            // 再描画
            RefreshDrawerAryFlg = true;
            refreshDrawerAry();
            CadPanel.Invalidate();

            return success;
        }

        /// <summary>
        /// FEM入力データ作成
        /// </summary>
        /// <param name="filename">ファイル名(*.cad)</param>
        /// <param name="elemShapeDv">要素形状</param>
        /// <param name="order">補間次数</param>
        /// <param name="normalizedFreq1">計算開始規格化周波数</param>
        /// <param name="normalizedFreq2">計算終了規格化周波数</param>
        /// <param name="calcCnt">計算する周波数の数</param>
        /// <param name="wgStructureDv">導波路構造区分</param>
        /// <param name="waveModeDv">計算モード区分</param>
        /// <param name="lsEqnSolverDv">線形方程式解法区分</param>
        /// <param name="waveguideWidthForEPlane">導波路幅(E面解析用)</param>
        public void MkFemInputData(
            string filename,
            Constants.FemElementShapeDV elemShapeDv, int order,
            double normalizedFreq1, double normalizedFreq2, int calcCnt,
            FemSolver.WGStructureDV wgStructureDv,
            FemSolver.WaveModeDV waveModeDv,
            FemSolver.LinearSystemEqnSoverDV lsEqnSolverDv,
            double waveguideWidthForEPlane)
        {
            IList<double[]> doubleCoords = null;
            IList<int[]> elements = null;
            IList<IList<int>> portList = null;
            int[] forceBCNodeNumbers = null;
            bool ret = false;

            if (elemShapeDv == Constants.FemElementShapeDV.Triangle && order == Constants.SecondOrder)
            {
                // ２次三角形要素メッシュ作成
                ret = FemMeshLogic.MkTriMesh(
                    Constants.SecondOrder,
                    EditCad2D,
                    LoopList,
                    EdgeCollectionList,
                    ref Mesher2D,
                    out doubleCoords,
                    out elements,
                    out portList,
                    out forceBCNodeNumbers
                );
            }
            else if (elemShapeDv == Constants.FemElementShapeDV.QuadType2 && order == Constants.SecondOrder)
            {
                // ２次四角形要素メッシュ作成
                // 未対応です。
                ret = FemMeshLogic.MkQuadMesh(
                    Constants.SecondOrder,
                    EditCad2D,
                    LoopList,
                    EdgeCollectionList,
                    ref Mesher2D,
                    out doubleCoords,
                    out elements,
                    out portList,
                    out forceBCNodeNumbers
                );
            }
            else if (elemShapeDv == Constants.FemElementShapeDV.Triangle && order == Constants.FirstOrder)
            {
                // １次三角形要素メッシュ作成
                ret = FemMeshLogic.MkTriMesh(
                    Constants.FirstOrder,
                    EditCad2D,
                    LoopList,
                    EdgeCollectionList,
                    ref Mesher2D,
                    out doubleCoords,
                    out elements,
                    out portList,
                    out forceBCNodeNumbers
                );
            }
            else if (elemShapeDv == Constants.FemElementShapeDV.QuadType2 && order == Constants.FirstOrder)
            {
                // １次四角形要素メッシュ作成
                // 未対応です。
                ret = FemMeshLogic.MkQuadMesh(
                    Constants.FirstOrder,
                    EditCad2D,
                    LoopList,
                    EdgeCollectionList,
                    ref Mesher2D,
                    out doubleCoords,
                    out elements,
                    out portList,
                    out forceBCNodeNumbers
                );
            }
            else
            {
                ret = false;
            }
            if (!ret)
            {
                MessageBox.Show("メッシュ作成に失敗しました");
                return;
            }

            // 導波管幅の計算
            double waveguideWidth = FemSolver.DefWaveguideWidth;
            if (portList.Count > 0 && portList[0].Count >= 2)
            {
                IList<int> portNodes = portList[0];
                double[] pp1 = doubleCoords[portNodes[0] - 1];
                double[] pp2 = doubleCoords[portNodes[portNodes.Count - 1] - 1];
                waveguideWidth = FemMeshLogic.GetDistance(pp1, pp2);
            }
            Console.WriteLine("(MkFemInputData) waveguideWidth:{0}", waveguideWidth);
            // 計算開始、終了波長の計算
            double firstWaveLength = FemSolver.GetWaveLengthFromNormalizedFreq(normalizedFreq1, waveguideWidth);
            double lastWaveLength = FemSolver.GetWaveLengthFromNormalizedFreq(normalizedFreq2, waveguideWidth);

            // Fem入力データファイルへ保存
            int nodeCnt = doubleCoords.Count;
            int elemCnt = elements.Count;
            int portCnt = portList.Count;
            FemInputDatFile.SaveToFileFromCad(
                filename,
                nodeCnt, doubleCoords,
                elemCnt, elements,
                portCnt, portList,
                forceBCNodeNumbers,
                IncidentPortNo,
                Medias,
                firstWaveLength,
                lastWaveLength,
                calcCnt,
                wgStructureDv,
                waveModeDv,
                lsEqnSolverDv,
                waveguideWidthForEPlane);
        }

        /// <summary>
        /// 媒質リストの取得
        /// </summary>
        /// <returns></returns>
        public MediaInfo[] GetMediaList()
        {
            MediaInfo[] medias = new MediaInfo[Medias.Length];
            for (int i = 0; i < Medias.Length; i++)
            {
                medias[i] = Medias[i].Clone() as MediaInfo;
            }
            return medias;
        }

        /// <summary>
        /// 指定されたインデックスの媒質を取得
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public MediaInfo GetMediaInfo(int index)
        {
            if (index < 0 || index >= Medias.Length)
            {
                return null;
            }
            return (MediaInfo)Medias[index].Clone();
        }

        /// <summary>
        /// 指定されたインデックスの媒質情報を更新
        /// </summary>
        /// <param name="index"></param>
        /// <param name="media"></param>
        public void SetMediaInfo(int index, MediaInfo media)
        {
            if (index < 0 || index >= Medias.Length)
            {
                return;
            }
            Medias[index] = media.Clone() as MediaInfo;
        }

        /// <summary>
        /// Cad操作コマンドを実行する
        /// </summary>
        private void invokeCadOperationCmd()
        {
            Console.WriteLine("////invokeCadOperationCmd");

            // 現在のMementoを生成する
            //   MementoにCadオブジェクトデータを渡すためにシリアライズバッファに保存する
            saveEditCad2DToSerializedBuffer(EditCad2D);
            //   Mementoを生成
            CadLogicBase mementoData = this as CadLogicBase;
            CadLogicBaseMemento curMmnt = new CadLogicBaseMemento(mementoData, this);
            //  Memento側でファイルのコピーが終わったのでシリアライズバッファを削除する
            clearSerializedCadObjBuffer();

            Console.WriteLine("  curMmnt created");
            var cmd = new MyUtilLib.MementoCommand<CadLogicBase, CadLogicBase>(CadLogicBaseMmnt, curMmnt);
            // Note: 第１引数のMementoはコマンドインスタンス内に格納される。第２引数のMementoはMementoのデータが参照されるだけで格納されない
            //       よって、第１引数の破棄の責任はMementoCommandへ移行するが、第２引数は依然こちらの責任となる
            Console.WriteLine("  cmd created");
            // ここで、再度Cadデータが自分自身にセットされる（mementoでデータ更新するのが本来の使用方法なので)
            bool ret = CmdManager.Invoke(cmd);
            // Invokeによって作成されたシリアライズバッファから図面を読み込む
            CCadObj2D cad2dref = EditCad2D;
            loadEditCad2DFromSerializedBuffer(ref cad2dref);
            System.Diagnostics.Debug.Assert(cad2dref == EditCad2D);
            // Invokeで作成されたシリアライズバッファを削除する
            clearSerializedCadObjBuffer();
            Console.WriteLine("  invoked");
            if (!ret)
            {
                MessageBox.Show("状態の最大保存数を超えました。");
            }
            CadLogicBaseMmnt = curMmnt;
            // Note: ここでCadLogicBaseMntが変更されるが、変更される前のインスタンスの破棄責任はMementoCommandへ移行したので破棄処理は必要ない
            Console.WriteLine("////invokeCadOperationCmd end");
        }
        
        /// <summary>
        /// 元に戻す
        /// </summary>
        public void Undo()
        {
            CadModeType prevCadMode = CadMode;

            // CadLogicBaseのUndoを実行
            CmdManager.Undo();
            // Undoによって作成されたシリアライズバッファから図面を読み込む
            CCadObj2D cad2dref = EditCad2D;
            loadEditCad2DFromSerializedBuffer(ref cad2dref);
            System.Diagnostics.Debug.Assert(cad2dref == EditCad2D);
            // Undoで作成されたシリアライズバッファを削除する
            clearSerializedCadObjBuffer();

            // BUGFIX 
            // 現在の状態をMementoに記憶させる
            setMemento();

            RefreshDrawerAryFlg = true;
            refreshDrawerAry();
            isDirty = true;

            if (Change != null)
            {
                Change(this, prevCadMode);
            }
            CadPanel.Invalidate();
        }

        /// <summary>
        /// やり直す
        /// </summary>
        public void Redo()
        {
            CadModeType prevCadMode = CadMode;

            // CadLogicBaseのRedoを実行
            CmdManager.Redo();
            // Redoによって作成されたシリアライズバッファから図面を読み込む
            CCadObj2D cad2dref = EditCad2D;
            loadEditCad2DFromSerializedBuffer(ref cad2dref);
            System.Diagnostics.Debug.Assert(cad2dref == EditCad2D);
            // Redoで作成されたシリアライズバッファを削除する
            clearSerializedCadObjBuffer();

            // BUGFIX 
            // 現在の状態をMementoに記憶させる
            setMemento();

            RefreshDrawerAryFlg = true;
            refreshDrawerAry();

            isDirty = true;

            if (Change != null)
            {
                Change(this, prevCadMode);
            }
            CadPanel.Invalidate();
        }

        /// <summary>
        /// 現在の状態をMementoに記憶させる
        /// </summary>
        private void setMemento()
        {
            //   MementoにCadオブジェクトデータを渡すためにシリアライズバッファに保存する
            saveEditCad2DToSerializedBuffer(EditCad2D);
            //   Mementoを生成
            CadLogicBase mementoData = this as CadLogicBase;
            CadLogicBaseMmnt = new CadLogicBaseMemento(mementoData, this);
            //  Memento側でファイルのコピーが終わったのでシリアライズバッファを削除する
            clearSerializedCadObjBuffer();
        }

        /// <summary>
        /// 元に戻す操作が可能か
        /// </summary>
        /// <returns></returns>
        public bool CanUndo()
        {
            return CmdManager.CanUndo();
        }

        /// <summary>
        /// やり直し操作が可能か
        /// </summary>
        /// <returns></returns>
        public bool CanRedo()
        {
            return CmdManager.CanRedo();
        }
    }
}
