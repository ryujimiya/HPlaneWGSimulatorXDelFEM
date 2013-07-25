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
    /// 要素分割のロジックをここにまとめます
    /// </summary>
    class FemMeshLogic
    {
        /// <summary>
        /// メッシャーオブジェクトの情報表示(DEBUG用)
        /// </summary>
        /// <param name="mesher2d"></param>
        private static void printMesher2D(CMesher2D mesher2d)
        {
            IList<CVector2D> coords = mesher2d.GetVectorAry();
            IList<CTriAry2D> triArySet = mesher2d.GetTriArySet();
            IList<CBarAry> barArySet = mesher2d.GetBarArySet();
            IList<SVertex> vertexAry = mesher2d.GetVertexAry();

            int counter;

            counter = 0;
            foreach (CVector2D pp in coords)
            {
                System.Diagnostics.Debug.Write(string.Format("coords[{0}] ", counter));
                System.Diagnostics.Debug.Write(string.Format("{0}, {1}　", pp.x, pp.y));
                System.Diagnostics.Debug.WriteLine("");
                counter++;
            }
            counter = 0;
            foreach (SVertex vertex in vertexAry)
            {
                System.Diagnostics.Debug.Write(string.Format("vertexAry[{0}] ", counter));
                // ID
                System.Diagnostics.Debug.Write(string.Format("id: {0} ", vertex.id));
                // vertex id in CAD（0 if not related to CAD）
                System.Diagnostics.Debug.Write(string.Format("id_v_cad: {0} ", vertex.id_v_cad));
                System.Diagnostics.Debug.Write(string.Format("ilayer: {0}　", vertex.ilayer));
                // index of node
                System.Diagnostics.Debug.Write(string.Format("v: {0}", vertex.v));
                System.Diagnostics.Debug.WriteLine("");
                counter++;
            }
            counter = 0;
            foreach (CBarAry barAry in barArySet)
            {
                System.Diagnostics.Debug.Write(string.Format("barArySet[{0}] ", counter));
                System.Diagnostics.Debug.Write(string.Format("id: {0} ", barAry.id));
                System.Diagnostics.Debug.Write(string.Format("id_e_cad: {0} ", barAry.id_e_cad));
                System.Diagnostics.Debug.Write("id_lr:");
                foreach (uint id in barAry.id_lr)
                {
                    System.Diagnostics.Debug.Write(string.Format(" {0} ", id));
                }
                System.Diagnostics.Debug.Write("id_se:");
                foreach (uint id in barAry.id_se)
                {
                    System.Diagnostics.Debug.Write(string.Format(" {0} ", id));
                }
                System.Diagnostics.Debug.Write(string.Format("ilayer: {0} ", barAry.ilayer));
                System.Diagnostics.Debug.WriteLine("");
                System.Diagnostics.Debug.Write(string.Format("    m_aBar:"));
                foreach (SBar bar in barAry.m_aBar)
                {
                    // 頂点Index
                    System.Diagnostics.Debug.Write("    bar.v: ");
                    foreach (uint v_val in bar.v)
                    {
                        System.Diagnostics.Debug.Write(string.Format(" {0} ", v_val));
                    }
                    // 隣接要素Index
                    System.Diagnostics.Debug.Write("    bar.s2: ");
                    foreach (uint s2_val in bar.s2)
                    {
                        System.Diagnostics.Debug.Write(string.Format(" {0} ", s2_val));
                    }
                    // 隣接関係
                    System.Diagnostics.Debug.Write("    bar.r2: ");
                    foreach (uint r2_val in bar.r2)
                    {
                        System.Diagnostics.Debug.Write(string.Format(" {0} ", r2_val));
                    }
                    System.Diagnostics.Debug.WriteLine("");
                }
                System.Diagnostics.Debug.WriteLine("");
                counter++;
            }
            counter = 0;
            foreach (CTriAry2D triAry in triArySet)
            {
                System.Diagnostics.Debug.Write(string.Format("triArySet[{0}] ", counter));
                // ID
                System.Diagnostics.Debug.Write(string.Format("id: {0} ", triAry.id));
                // CADの面ID（CADに関連されてなければ０
                System.Diagnostics.Debug.Write(string.Format("id_l_cad: {0} ", triAry.id_l_cad));
                System.Diagnostics.Debug.Write(string.Format("ilayer: {0} ", triAry.ilayer));
                System.Diagnostics.Debug.WriteLine("");
                System.Diagnostics.Debug.Write("    m_aTri:");
                foreach (STri2D tri in triAry.m_aTri)
                {
                    // 頂点Index
                    System.Diagnostics.Debug.Write("    tri.v: ");
                    foreach (uint v_val in tri.v)
                    {
                        System.Diagnostics.Debug.Write(string.Format(" {0} ", v_val));
                    }
                    // 隣接する要素配列ID(-1:隣接要素なし、-2:自分の要素配列に隣接)
                    System.Diagnostics.Debug.Write("    tri.g2: ");
                    foreach (uint g2_val in tri.g2)
                    {
                        System.Diagnostics.Debug.Write(string.Format(" {0} ", g2_val));
                    }
                    // 隣接要素Index
                    System.Diagnostics.Debug.Write("    tri.s2: ");
                    foreach (uint s2_val in tri.s2)
                    {
                        System.Diagnostics.Debug.Write(string.Format(" {0} ", s2_val));
                    }
                    // 隣接関係
                    System.Diagnostics.Debug.Write("    tri.r2: ");
                    foreach (uint r2_val in tri.r2)
                    {
                        System.Diagnostics.Debug.Write(string.Format(" {0} ", r2_val));
                    }
                    System.Diagnostics.Debug.WriteLine("");
                }
                System.Diagnostics.Debug.WriteLine("");
            }
        }

        /// <summary>
        /// 三角形要素メッシュを作成する(DelFEM版)
        ///   ２次三角形/１次三角形共用ルーチン
        /// </summary>
        /// <param name="order">三角形要素の補間次数</param>
        /// <param name="EditCad2D">Cadオブジェクト</param>
        /// <param name="LoopList">ループリスト</param>
        /// <param name="EdgeCollectionList">ポート境界辺のコレクションのリスト</param>
        /// <param name="Mesher2D">メッシュ生成装置</param>
        /// <param name="doubleCoords">[OUT]座標リスト（このリストのインデックス+1が節点番号として扱われます)</param>
        /// <param name="elements">[OUT]要素データリスト 要素データはint[] = { 要素番号, 媒質インデックス, 節点番号1, 節点番号2, ...}</param>
        /// <param name="portList">[OUT]ポート節点リストのリスト</param>
        /// <param name="forceBCNodeNumbers">強制境界節点配列</param>
        /// <returns></returns>
        public static bool MkTriMesh(
            int order,
            CCadObj2D EditCad2D,
            IList<CadLogic.Loop> LoopList,
            IList<EdgeCollection> EdgeCollectionList,
            ref CMesher2D Mesher2D,
            out IList<double[]> doubleCoords,
            out IList<int[]> elements,
            out IList<IList<int>> portList,
            out int[] forceBCNodeNumbers)
        {
            elements = new List<int[]>(); // 要素リスト
            doubleCoords = new List<double[]>();
            forceBCNodeNumbers = null;
            portList = new List<IList<int>>();
            if (LoopList.Count == 0)
            {
                return false;
            }

            Dictionary<int, bool> forceBCNodeNumberDic = new Dictionary<int, bool>();
            int nodeCounter = 0; // 節点番号カウンター
            int elementCounter = 0; // 要素番号カウンター
            // ポート境界
            //int portCounter = 0;

            // 番号でソートする(ポート番号順になっていることを保証する)
            ((List<EdgeCollection>)EdgeCollectionList).Sort();

            ///////////////////////////////////////////////////////
            // FEMメッシュの作成
            if (Mesher2D != null)
            {
                Mesher2D.Clear();
                Mesher2D.Dispose();
                Mesher2D = null;
            }
            //１つの 要素の長さ
            double elementLength = Constants.MesherElementLength;
            // メッシャーを生成する
            Mesher2D = new CMesher2D(EditCad2D, elementLength);

            // メッシュの座標リスト 最初の数点はCadの頂点、その後メッシュ切りで生成された座標が格納される
            IList<CVector2D> coords = Mesher2D.GetVectorAry();
            // 頂点アレイのリスト: Cadの頂点に対応
            IList<SVertex> vertexAry = Mesher2D.GetVertexAry();
            // 線分アレイのリスト: Cadの辺に対応
            IList<CBarAry> barArySet = Mesher2D.GetBarArySet();
            // 三角形アレイのリスト: Cadの面（ループ)に対応
            IList<CTriAry2D> triArySet = Mesher2D.GetTriArySet();

            //printMesher2D(mesher2d);

            int coordIndexBase = coords.Count;
            nodeCounter = coords.Count;

            // 辺ID→エッジコレクションのリストインデックス変換マップ
            Dictionary<uint, int> eIdCadToEdgeCollectionIndex = new Dictionary<uint, int>();
            for (int i = 0; i < EdgeCollectionList.Count; i++)
            {
                EdgeCollection edgeCollection = EdgeCollectionList[i];
                foreach (uint eIdCad in edgeCollection.EdgeIds)
                {
                    if (!eIdCadToEdgeCollectionIndex.ContainsKey(eIdCad))
                    {
                        eIdCadToEdgeCollectionIndex.Add(eIdCad, i);
                    }
                }
            }

            // メッシュ辺の頂点ペア→ バーアレイのバー 変換マップ
            // key: "辺の頂点インデックス1_頂点インデックス2" value: int[2] value[0]: バーアレイのインデックス value[1]:バーアレイ内バーのリストインデックス
            Dictionary<string, int[]> edgeToBarIndexes = new Dictionary<string, int[]>();
            // メッシュの辺の頂点ペア → 辺の中点の頂点ID
            //    Noye: 1次要素の場合、中点は作成しないが、辺のハッシュとしてedgeToMiddlePtVertexIdを流用している
            Dictionary<string, uint> edgeToMiddlePtVertexId = new Dictionary<string, uint>();

            // 使用した頂点IDのマップ
            IList<uint> usedVertexIds = new List<uint>();

            // 節点番号付与
            //   2次三角形要素：メッシュの辺の中点を追加する
            // 要素データ作成
            elementCounter = 0;
            foreach (CTriAry2D triAry in triArySet)
            {
                // 三角形アレイのID
                uint id_triAry = triAry.id;
                // CadループID
                uint id_l_cad = triAry.id_l_cad;
                // 媒質インデックス
                int mediaIndex = 0;
                // 媒質インデックスの取得
                CadLogic.Loop loop = CadLogic.getLoop(LoopList, id_l_cad);
                mediaIndex = loop.MediaIndex;
                if (mediaIndex == CadLogic.MetalMediaIndex)
                {
                    // 導体の場合、要素を作成しない
                    continue;
                }

                // 節点番号は、頂点IDで割り振って、一連の処理を行う
                // ただし、領域に導体があった場合、歯抜けが存在することになるので全処理完了後に調整する
                foreach (STri2D tri in triAry.m_aTri)
                {
                    // 節点番号
                    int[] nodeNumbers = null;
                    if (order == Constants.FirstOrder)
                    {
                        // 1次三角形要素
                        nodeNumbers = new int[]
                            {
                                (int)tri.v[0] + 1,
                                (int)tri.v[1] + 1,
                                (int)tri.v[2] + 1,
                            };

                        // 中点は作成しないが、辺のハッシュは必要
                        for (int i = 0; i < 3; i++)
                        {
                            // (v1, v2)は節点3:(節点0, 節点1) 節点4:(節点1, 節点2) 節点5:(節点2, 節点0)に対応
                            // tri.vは頂点インデックスが格納されているので頂点IDにするために+1している
                            uint v1 = tri.v[i] + 1;
                            uint v2 = tri.v[(i + 1) % 3] + 1;
                            string vertexIdsKey = (v2 >= v1) ? string.Format("{0}_{1}", v1, v2) : string.Format("{0}_{1}", v2, v1);
                            if (edgeToMiddlePtVertexId.ContainsKey(vertexIdsKey))
                            {
                                // 登録済み
                            }
                            else
                            {
                                // 存在しない場合は、辺のキーを登録
                                uint dummy_vMiddle = 0;
                                edgeToMiddlePtVertexId.Add(vertexIdsKey, dummy_vMiddle);
                            }
                        }
                    }
                    else
                    {
                        // 2次三角形要素
                        nodeNumbers = new int[]
                            {
                                (int)tri.v[0] + 1,
                                (int)tri.v[1] + 1,
                                (int)tri.v[2] + 1,
                                0,
                                0,
                                0
                            };

                        // 中点作成
                        // 中点の節点番号を探す
                        for (int i = 0; i < 3; i++)
                        {
                            // (v1, v2)は節点3:(節点0, 節点1) 節点4:(節点1, 節点2) 節点5:(節点2, 節点0)に対応
                            // tri.vは頂点インデックスが格納されているので頂点IDにするために+1している
                            uint v1 = tri.v[i] + 1;
                            uint v2 = tri.v[(i + 1) % 3] + 1;
                            string vertexIdsKey = (v2 >= v1) ? string.Format("{0}_{1}", v1, v2) : string.Format("{0}_{1}", v2, v1);

                            // 中点の頂点ID
                            uint vMiddle = 0;
                            // 中点を作成済みかマップを検査
                            if (edgeToMiddlePtVertexId.ContainsKey(vertexIdsKey))
                            {
                                vMiddle = edgeToMiddlePtVertexId[vertexIdsKey];
                            }
                            else
                            {
                                // 存在しない場合は、中点を作成する
                                // 始点、終点の取得
                                CVector2D[] pts = new CVector2D[] { coords[(int)v1 - 1], coords[(int)v2 - 1] };  // 頂点ID - 1に注意
                                // 中点を追加
                                CVector2D mpt = new CVector2D((pts[0].x + pts[1].x) * 0.5, (pts[0].y + pts[1].y) * 0.5);
                                coords.Add(mpt);
                                nodeCounter++;
                                // 中点の頂点ID
                                vMiddle = (uint)coords.Count;

                                // 中点の頂点IDをマップに追加
                                edgeToMiddlePtVertexId.Add(vertexIdsKey, vMiddle);
                            }
                            System.Diagnostics.Debug.Assert(vMiddle != 0);

                            // 中点3, 4, 5の節点の節点番号を格納
                            nodeNumbers[i + 3] = (int)vMiddle;
                        }
                    }

                    // 要素番号
                    int elementNo = elementCounter + 1;
                    // 要素
                    int[] elementData = new int[2 + nodeNumbers.Length];
                    elementData[0] = elementNo;
                    elementData[1] = mediaIndex;
                    for (int ino = 0; ino < nodeNumbers.Length; ino++)
                    {
                        elementData[2 + ino] = nodeNumbers[ino];
                    }
                    elements.Add(elementData);

                    // 使用した頂点IDをリストに追加
                    foreach (int nodeNumber in nodeNumbers)
                    {
                        uint vId = (uint)nodeNumber;
                        if (usedVertexIds.IndexOf(vId) == -1) // 未追加なら追加する
                        {
                            usedVertexIds.Add(vId);
                        }
                    }

                    // 要素カウンタを進める
                    elementCounter++;
                }
            }
            System.Diagnostics.Debug.Assert(elementCounter == elements.Count);
            System.Diagnostics.Debug.Assert(nodeCounter == coords.Count);

            // Cad辺単位の処理
            //   電気壁節点を記録
            for (int barAryIndex = 0; barAryIndex < barArySet.Count; barAryIndex++)
            {
                CBarAry barAry = barArySet[barAryIndex];
                uint id_barAry = barAry.id;  // Note: 頂点のIDの最大値から開始して連番で振られているので、リストインデックスに変換し辛い
                uint id_e_cad = barAry.id_e_cad;

                // 電気壁節点、ポート境界節点を記録するための準備
                // この辺の属するCadループIDを取得する
                //uint id_l_cad = getLoopIdOfEdge(EditCad2D, id_e_cad);
                bool isSharedEdge = CadLogic.isEdgeSharedByLoops(EditCad2D, id_e_cad);
                // 強制境界か?
                //この判定は破綻した
                //// ベースループIDの場合は、境界を除きすべて電気壁
                //bool isForcedBoundary = (id_l_cad == BaseLoopId);
                // この判定も内部の導体対応で破綻した  内部の導体壁（図面的には空洞）が判定できない
                //辺が２つのループに共有されていないいう条件と導体媒質判定(内部ループ用)を併用して判定する
                bool isForcedBoundary = false;
                if (!isSharedEdge)
                {
                    // ２つのループが共有する辺でない場合ば電気壁(ループに属さない辺がないことが条件)
                    isForcedBoundary = true;
                }
                if (!isForcedBoundary)
                {
                    // 内部の導体判定
                    // 左右のループIDを取得
                    uint id_l_l_cad;
                    uint id_l_r_cad;
                    EditCad2D.GetIdLoop_Edge(out id_l_l_cad, out id_l_r_cad, id_e_cad);
                    int mediaIndex_l = CadLogic.VacumnMediaIndex;
                    int mediaIndex_r = CadLogic.VacumnMediaIndex;
                    // 媒質インデックスの取得
                    CadLogic.Loop workloop;
                    workloop = CadLogic.getLoop(LoopList, id_l_l_cad);
                    mediaIndex_l = workloop.MediaIndex;
                    workloop = CadLogic.getLoop(LoopList, id_l_r_cad);
                    mediaIndex_r = workloop.MediaIndex;
                    if (mediaIndex_l == CadLogic.MetalMediaIndex || mediaIndex_r == CadLogic.MetalMediaIndex)
                    {
                        isForcedBoundary = true;
                    }
                }

                // メッシュの辺を走査
                for (int barIndex = 0; barIndex < barAry.m_aBar.Count; barIndex++)
                {
                    SBar bar = barAry.m_aBar[barIndex];
                    System.Diagnostics.Debug.Assert(bar.v.Length == 2);
                    // 頂点IDを取得(bar.vは頂点のインデックスなので+1する)
                    uint v1 = bar.v[0] + 1;
                    uint v2 = bar.v[1] + 1;
                    string vertexIdsKey = (v2 >= v1) ? string.Format("{0}_{1}", v1, v2) : string.Format("{0}_{1}", v2, v1);
                    if (!edgeToMiddlePtVertexId.ContainsKey(vertexIdsKey))
                    {
                        // 中点が作成済みでない→導体内のbar
                        //  Noye: 1次要素の場合、中点は作成しないが、辺のハッシュとしてedgeToMiddlePtVertexIdを流用している
                        continue;
                    }

                    if (!edgeToBarIndexes.ContainsKey(vertexIdsKey))
                    {
                        edgeToBarIndexes.Add(vertexIdsKey, new int[] { barAryIndex, barIndex });
                    }

                    // 境界の辺の節点番号(線要素)
                    int[] nodeNumbers = null;
                    if (order == Constants.FirstOrder)
                    {
                        // 1次線要素
                        nodeNumbers = new int[] { (int)bar.v[0] + 1, (int)bar.v[1] + 1 };
                    }
                    else
                    {
                        // 中点
                        uint vMiddle = 0;
                        if (edgeToMiddlePtVertexId.ContainsKey(vertexIdsKey))
                        {
                            vMiddle = edgeToMiddlePtVertexId[vertexIdsKey];
                        }
                        else
                        {
                            // ロジックエラー
                            System.Diagnostics.Debug.Assert(false);
                        }
                        System.Diagnostics.Debug.Assert(vMiddle != 0);

                        // 2次線要素
                        nodeNumbers = new int[] { (int)bar.v[0] + 1, (int)vMiddle, (int)bar.v[1] + 1 };
                    }

                    if (isForcedBoundary)
                    {
                        // 電気壁節点の追加
                        foreach (int nodeNumber in nodeNumbers)
                        {
                            if (!forceBCNodeNumberDic.ContainsKey(nodeNumber))
                            {
                                forceBCNodeNumberDic.Add(nodeNumber, true);
                            }
                        }
                    }
                }
            }

            // ポート境界上の節点リストを取得する
            portList.Clear();
            foreach (EdgeCollection edgeColleciton in EdgeCollectionList)
            {
                IList<int> portNodes = new List<int>();
                foreach (uint eId in edgeColleciton.EdgeIds)
                {
                    CBarAry hitBarAry = null;
                    int hitBarAryIndex = -1;
                    for (int barAryIndex = 0; barAryIndex < barArySet.Count; barAryIndex++)
                    {
                        CBarAry barAry = barArySet[barAryIndex];
                        uint id_barAry = barAry.id;  // Note: 頂点のIDの最大値から開始して連番で振られているので、リストインデックスに変換し辛い
                        uint id_e_cad = barAry.id_e_cad;
                        if (id_e_cad == eId)
                        {
                            hitBarAry = barAry;
                            hitBarAryIndex = barAryIndex;
                            break;
                        }
                    }
                    if (hitBarAry == null)
                    {
                        // ロジックエラー
                        System.Diagnostics.Debug.Assert(false);
                    }
                    // メッシュの辺を走査
                    IList<int[]> barNodeNumbersList = new List<int[]>();
                    for (int barIndex = 0; barIndex < hitBarAry.m_aBar.Count; barIndex++)
                    {
                        SBar bar = hitBarAry.m_aBar[barIndex];
                        System.Diagnostics.Debug.Assert(bar.v.Length == 2);
                        // 頂点IDを取得(bar.vは頂点のインデックスなので+1する)
                        uint v1 = bar.v[0] + 1;
                        uint v2 = bar.v[1] + 1;
                        string vertexIdsKey = (v2 >= v1) ? string.Format("{0}_{1}", v1, v2) : string.Format("{0}_{1}", v2, v1);
                        if (!edgeToBarIndexes.ContainsKey(vertexIdsKey))
                        {
                            // すでに追加済みのはず
                            System.Diagnostics.Debug.Assert(false);
                            edgeToBarIndexes.Add(vertexIdsKey, new int[] { hitBarAryIndex, barIndex });
                        }

                        // 境界の辺の節点番号(線要素)
                        int[] nodeNumbers = null;
                        if (order == Constants.FirstOrder)
                        {
                            // 境界の辺の節点番号(1次線要素)
                            nodeNumbers = new int[] { (int)bar.v[0] + 1, (int)bar.v[1] + 1 };
                        }
                        else
                        {
                            // 中点
                            uint vMiddle = 0;
                            if (edgeToMiddlePtVertexId.ContainsKey(vertexIdsKey))
                            {
                                vMiddle = edgeToMiddlePtVertexId[vertexIdsKey];
                            }
                            else
                            {
                                // ロジックエラー
                                System.Diagnostics.Debug.Assert(false);
                            }
                            System.Diagnostics.Debug.Assert(vMiddle != 0);

                            // 境界の辺の節点番号(2次線要素)
                            nodeNumbers = new int[] { (int)bar.v[0] + 1, (int)vMiddle, (int)bar.v[1] + 1 };
                        }
                        barNodeNumbersList.Add(nodeNumbers);

                    } // bar

                    if (barNodeNumbersList.Count > 0)
                    {
                        while (barNodeNumbersList.Count > 0)
                        {
                            int[] hitBarNodeNumbers = null;
                            if (portNodes.Count == 0)
                            {
                                hitBarNodeNumbers = barNodeNumbersList[0];
                            }
                            else
                            {
                                int v_st = portNodes[0];
                                int v_ed = portNodes[portNodes.Count - 1];
                                foreach (int[] work in barNodeNumbersList)
                                {
                                    int work_v1 = work[0];
                                    int work_v2 = work[work.Length - 1];
                                    if (v_st == work_v1 || v_st == work_v2 || v_ed == work_v1 || v_ed == work_v2)
                                    {
                                        hitBarNodeNumbers = work;
                                        break;
                                    }
                                }
                            }
                            if (hitBarNodeNumbers == null)
                            {
                                System.Diagnostics.Debug.Assert(false);
                                break;
                            }
                            barNodeNumbersList.Remove(hitBarNodeNumbers);

                            int v1 = hitBarNodeNumbers[0];
                            int v2 = hitBarNodeNumbers[hitBarNodeNumbers.Length - 1];
                            if (portNodes.Count == 0 || v1 == portNodes[portNodes.Count - 1])
                            {
                                // 最初、or 線要素の始点がリストの終点の場合は、そのまま最後に追加
                                for (int ino = 0; ino < hitBarNodeNumbers.Length; ino++)
                                {
                                    int nodeNumber = hitBarNodeNumbers[ino];
                                    if (portNodes.IndexOf(nodeNumber) >= 0) continue;
                                    portNodes.Add(nodeNumber);
                                }
                            }
                            else if (v1 == portNodes[0])
                            {
                                // 線要素の始点がリストの始点の場合、先頭に逆順で追加
                                int inscnt = 0;
                                for (int ino = hitBarNodeNumbers.Length - 1; ino >= 0; ino--)
                                {
                                    int nodeNumber = hitBarNodeNumbers[ino];
                                    if (portNodes.IndexOf(nodeNumber) >= 0) continue;
                                    portNodes.Insert(0 + inscnt, nodeNumber);
                                    inscnt++;
                                }
                            }
                            else if (v2 == portNodes[0])
                            {
                                // 線要素の終点がリストの始点の場合、先頭にそのままの順番で追加
                                int inscnt = 0;
                                for (int ino = 0; ino < hitBarNodeNumbers.Length; ino++)
                                {
                                    int nodeNumber = hitBarNodeNumbers[ino];
                                    if (portNodes.IndexOf(nodeNumber) >= 0) continue;
                                    portNodes.Insert(0 + inscnt, nodeNumber);
                                    inscnt++;
                                }
                            }
                            else if (v2 == portNodes[portNodes.Count - 1])
                            {
                                // 線要素の終点がリストの終点の場合、最後に逆順で追加
                                for (int ino = hitBarNodeNumbers.Length - 1; ino >= 0; ino--)
                                {
                                    int nodeNumber = hitBarNodeNumbers[ino];
                                    if (portNodes.IndexOf(nodeNumber) >= 0) continue;
                                    portNodes.Add(nodeNumber);
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.Assert(false);
                            }
                            //check
                            //System.Diagnostics.Debug.WriteLine("===========");
                            //foreach (int nodeNumber in portNodes)
                            //{
                            //    System.Diagnostics.Debug.WriteLine(nodeNumber);
                            //}
                        }
                    }

                } // eId
                //　Mesherの辺はCadの辺の順になっていないので、Cadの辺の順に並び替える
                if (portNodes.Count > 0)
                {
                    // 取得したポート節点リストの最後の節点のMesher頂点ID
                    uint work_last_vId_msh = (uint)portNodes[portNodes.Count - 1];
                    // Cadのポート境界先頭の辺ID
                    uint first_eId_cad = edgeColleciton.EdgeIds[0];
                    // Cadの辺IDからCadの頂点IDを取得
                    uint first_id_v1_cad = 0;
                    uint first_id_v2_cad = 0;
                    CadLogic.getVertexIdsOfEdgeId(EditCad2D, first_eId_cad, out first_id_v1_cad, out first_id_v2_cad);
                    // Cadの頂点IDをMesherの頂点IDに変換
                    uint first_id_v1_msh = Mesher2D.GetElemID_FromCadID(first_id_v1_cad, CAD_ELEM_TYPE.VERTEX);
                    uint first_id_v2_msh = Mesher2D.GetElemID_FromCadID(first_id_v2_cad, CAD_ELEM_TYPE.VERTEX);
                    // 逆順チェック
                    if (work_last_vId_msh == first_id_v1_msh || work_last_vId_msh == first_id_v2_msh)
                    {
                        // 逆順になっているので、辺IDの順に並べ替える
                        int[] saveNodes = portNodes.ToArray();
                        portNodes.Clear();
                        for (int i = saveNodes.Length - 1; i >= 0; i--)
                        {
                            portNodes.Add(saveNodes[i]);
                        }
                    }
                }
                // ポート節点リストを追加
                portList.Add(portNodes);
            } // edgeCollection
            //portCounter = portList.Count;
            // check
            //foreach (IList<int> portNodes in portList)
            //{
            //    System.Diagnostics.Debug.WriteLine("------------");
            //    foreach (int nodeNumber in portNodes)
            //    {
            //        System.Diagnostics.Debug.WriteLine("{0}:    {1},    {2}", nodeNumber, coords[nodeNumber - 1].x, coords[nodeNumber - 1].y);
            //    }
            //}

            // 強制境界からポート境界の節点を取り除く
            // ただし、始点と終点は強制境界なので残す
            foreach (IList<int> nodes in portList)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (i != 0 && i != nodes.Count - 1)
                    {
                        int nodeNumber = nodes[i];
                        if (forceBCNodeNumberDic.ContainsKey(nodeNumber))
                        {
                            forceBCNodeNumberDic.Remove(nodeNumber);
                        }
                    }
                }
            }

            ///////////////////////////////////////////////////////////////
            // 節点番号の振り直し
            // usedVertexIdsリストのインデックス + 1を節点番号にする
            if (usedVertexIds.Count != coords.Count)
            {
                // 座標
                // 現在の座標リスト(頂点ID順)を退避
                CVector2D[] saveCoordAry = coords.ToArray();
                // 座標リストをクリア
                coords.Clear();
                // 空の座標リストを作成
                for (int nodeIndex = 0; nodeIndex < usedVertexIds.Count; nodeIndex++)
                {
                    coords.Add(null);
                }
                for (int vIndex = 0; vIndex < saveCoordAry.Length; vIndex++)
                {
                    uint vId = (uint)vIndex + 1;
                    int nodeIndex_Renumbering = usedVertexIds.IndexOf(vId);
                    if (nodeIndex_Renumbering != -1)
                    {
                        coords[nodeIndex_Renumbering] = saveCoordAry[vIndex];
                    }
                }
                nodeCounter = coords.Count;
                System.Diagnostics.Debug.Assert(coords.IndexOf(null) == -1);

                // 要素
                foreach (int[] elementData in elements)
                {
                    // 0: 要素番号
                    // 1: 媒質インデックス
                    // 2 - 7: ２次三角形要素の節点番号
                    // 2 - 4: １次三角形要素の節点番号
                    for (int i = 2; i < elementData.Length; i++)
                    {
                        uint vId = (uint)elementData[i];
                        int nodeIndex_Renumbering = usedVertexIds.IndexOf(vId);
                        System.Diagnostics.Debug.Assert(nodeIndex_Renumbering != -1);
                        elementData[i] = nodeIndex_Renumbering + 1;
                    }
                }

                // ポート
                for (int portIndex = 0; portIndex < portList.Count; portIndex++)
                {
                    IList<int> portNodes = portList[portIndex];
                    for (int i = 0; i < portNodes.Count; i++)
                    {
                        uint vId = (uint)portNodes[i];
                        int nodeIndex_Renumbering = usedVertexIds.IndexOf(vId);
                        System.Diagnostics.Debug.Assert(nodeIndex_Renumbering != -1);
                        portNodes[i] = nodeIndex_Renumbering + 1;
                    }
                }

                // 強制境界
                int[] saveForceBCNodeNumbers = forceBCNodeNumberDic.Keys.ToArray();
                forceBCNodeNumberDic.Clear();
                foreach (int vId_Int in saveForceBCNodeNumbers)
                {
                    uint vId = (uint)vId_Int;
                    int nodeIndex_Renumbering = usedVertexIds.IndexOf(vId);
                    System.Diagnostics.Debug.Assert(nodeIndex_Renumbering != -1);
                    int nodeNumber_Renumbering = nodeIndex_Renumbering + 1;

                    if (!forceBCNodeNumberDic.ContainsKey(nodeNumber_Renumbering))
                    {
                        forceBCNodeNumberDic.Add(nodeNumber_Renumbering, true);
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////
            // Fem入力データファイルのI/Fに合わせる
            // 座標値
            System.Diagnostics.Debug.Assert(doubleCoords.Count == 0);
            foreach (CVector2D pp in coords)
            {
                doubleCoords.Add(new double[] { pp.x, pp.y });
            }
            forceBCNodeNumbers = forceBCNodeNumberDic.Keys.ToArray();

            return true;
        }

        /// <summary>
        /// 四角形要素メッシュを作成する(DelFEM版)
        ///   ２次四角形形/１次四角形共用ルーチン
        /// </summary>
        /// <param name="order">四角形要素の補間次数</param>
        /// <param name="EditCad2D">Cadオブジェクト</param>
        /// <param name="LoopList">ループリスト</param>
        /// <param name="EdgeCollectionList">ポート境界辺のコレクションのリスト</param>
        /// <param name="Mesher2D">メッシュ生成装置</param>
        /// <param name="doubleCoords">[OUT]座標リスト（このリストのインデックス+1が節点番号として扱われます)</param>
        /// <param name="elements">[OUT]要素データリスト 要素データはint[] = { 要素番号, 媒質インデックス, 節点番号1, 節点番号2, ...}</param>
        /// <param name="portList">[OUT]ポート節点リストのリスト</param>
        /// <param name="forceBCNodeNumbers">強制境界節点配列</param>
        /// <returns></returns>
        public static bool MkQuadMesh(
            int order,
            CCadObj2D EditCad2D,
            IList<CadLogic.Loop> LoopList,
            IList<EdgeCollection> EdgeCollectionList,
            ref CMesher2D Mesher2D,
            out IList<double[]> doubleCoords,
            out IList<int[]> elements,
            out IList<IList<int>> portList,
            out int[] forceBCNodeNumbers)
        {
            MessageBox.Show("Not implemented");
            elements = new List<int[]>(); // 要素リスト
            doubleCoords = new List<double[]>();
            forceBCNodeNumbers = null;
            portList = new List<IList<int>>();
            return false;
        }

        /// <summary>
        /// 要素の節点数から要素形状区分と補間次数を取得する
        /// </summary>
        /// <param name="eNodeCnt">要素の節点数</param>
        /// <param name="elemShapeDv">要素形状区分</param>
        /// <param name="order">補間次数</param>
        /// <param name="vertexCnt">頂点数</param>
        public static void GetElementShapeDvAndOrderByElemNodeCnt(int eNodeCnt, out Constants.FemElementShapeDV elemShapeDv, out int order, out int vertexCnt)
        {
            elemShapeDv = Constants.FemElementShapeDV.Triangle;
            order = Constants.SecondOrder;
            vertexCnt = Constants.TriVertexCnt;
            if (eNodeCnt == Constants.TriNodeCnt_SecondOrder)
            {
                // ２次三角形
                elemShapeDv = Constants.FemElementShapeDV.Triangle;
                order = Constants.SecondOrder;
                vertexCnt = Constants.TriVertexCnt;
            }
            else if (eNodeCnt == Constants.QuadNodeCnt_SecondOrder_Type2)
            {
                // ２次四角形
                elemShapeDv = Constants.FemElementShapeDV.QuadType2;
                order = Constants.SecondOrder;
                vertexCnt = Constants.QuadVertexCnt;
            }
            else if (eNodeCnt == Constants.TriNodeCnt_FirstOrder)
            {
                // １次三角形
                elemShapeDv = Constants.FemElementShapeDV.Triangle;
                order = Constants.FirstOrder;
                vertexCnt = Constants.TriVertexCnt;
            }
            else if (eNodeCnt == Constants.QuadNodeCnt_FirstOrder)
            {
                // １次四角形
                elemShapeDv = Constants.FemElementShapeDV.QuadType2;
                order = Constants.FirstOrder;
                vertexCnt = Constants.QuadVertexCnt;
            }
            else
            {
                // 未対応
                System.Diagnostics.Debug.Assert(false);
            }
        }

        /// <summary>
        /// 辺と要素の対応マップ作成
        /// </summary>
        /// <param name="in_Elements"></param>
        /// <param name="out_EdgeToElementNoH"></param>
        public static void MkEdgeToElementNoH(IList<FemElement> in_Elements, ref Dictionary<string, IList<int>> out_EdgeToElementNoH)
        {
            if (in_Elements.Count == 0)
            {
                return;
            }
            int eNodeCnt = in_Elements[0].NodeNumbers.Length;
            Constants.FemElementShapeDV elemShapeDv;
            int order;
            int vertexCnt;
            GetElementShapeDvAndOrderByElemNodeCnt(eNodeCnt, out elemShapeDv, out order, out vertexCnt);

            if (elemShapeDv == Constants.FemElementShapeDV.Triangle && order == Constants.SecondOrder)
            {
                // ２次三角形要素
                TriSecondOrderElements_MkEdgeToElementNoH(in_Elements, ref out_EdgeToElementNoH);
            }
            else if (elemShapeDv == Constants.FemElementShapeDV.QuadType2 && order == Constants.SecondOrder)
            {
                // ２次四角形要素（セレンディピティ族)
                QuadSecondOrderElements_MkEdgeToElementNoH(in_Elements, ref out_EdgeToElementNoH);
            }
            else if (elemShapeDv == Constants.FemElementShapeDV.Triangle && order == Constants.FirstOrder)
            {
                // １次三角形要素
                TriFirstOrderElements_MkEdgeToElementNoH(in_Elements, ref out_EdgeToElementNoH);
            }
            else if (elemShapeDv == Constants.FemElementShapeDV.QuadType2 && order == Constants.FirstOrder)
            {
                // 1次四角形要素（セレンディピティ族)
                QuadFirstOrderElements_MkEdgeToElementNoH(in_Elements, ref out_EdgeToElementNoH);
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
        }

        /// <summary>
        /// ２次三角形要素：辺と要素の対応マップ作成
        /// </summary>
        /// <param name="in_Elements"></param>
        /// <param name="out_EdgeToElementNoH"></param>
        public static void TriSecondOrderElements_MkEdgeToElementNoH(IList<FemElement> in_Elements, ref Dictionary<string, IList<int>> out_EdgeToElementNoH)
        {
            // 辺と要素の対応マップ作成
            // 2次三角形要素の辺の始点、終点ローカル番号
            int[][] edgeStEdNoList = new int[Constants.TriEdgeCnt_SecondOrder][]
                        {
                            new int[]{1, 4},
                            new int[]{4, 2},
                            new int[]{2, 5},
                            new int[]{5, 3},
                            new int[]{3, 6},
                            new int[]{6, 1}
                        };
            out_EdgeToElementNoH.Clear();
            foreach (FemElement element in in_Elements)
            {
                foreach (int[] edgeStEdNo in edgeStEdNoList)
                {
                    int stNodeNumber = element.NodeNumbers[edgeStEdNo[0] - 1];
                    int edNodeNumber = element.NodeNumbers[edgeStEdNo[1] - 1];
                    string edgeKey = "";
                    if (stNodeNumber < edNodeNumber)
                    {
                        edgeKey = string.Format("{0}_{1}", stNodeNumber, edNodeNumber);
                    }
                    else
                    {
                        edgeKey = string.Format("{0}_{1}", edNodeNumber, stNodeNumber);
                    }
                    if (out_EdgeToElementNoH.ContainsKey(edgeKey))
                    {
                        out_EdgeToElementNoH[edgeKey].Add(element.No);
                    }
                    else
                    {
                        out_EdgeToElementNoH[edgeKey] = new List<int>();
                        out_EdgeToElementNoH[edgeKey].Add(element.No);
                    }
                }
            }
        }

        /// <summary>
        /// ２次四角形要素：辺と要素の対応マップ作成
        /// </summary>
        /// <param name="in_Elements"></param>
        /// <param name="out_EdgeToElementNoH"></param>
        public static void QuadSecondOrderElements_MkEdgeToElementNoH(IList<FemElement> in_Elements, ref Dictionary<string, IList<int>> out_EdgeToElementNoH)
        {
            // 辺と要素の対応マップ作成
            // ２次四角形要素（セレンディピティ族）
            //
            //    4+  7  +3      x
            //    |       |
            //    8       6
            //    |       |
            //    1+  5  +2
            //    
            //    y
            // 2次四角形要素の辺の始点、終点ローカル番号
            int[][] edgeStEdNoList = new int[Constants.QuadEdgeCnt_SecondOrder][]
                        {
                            new int[]{1, 5},
                            new int[]{5, 2},
                            new int[]{2, 6},
                            new int[]{6, 3},
                            new int[]{3, 7},
                            new int[]{7, 4},
                            new int[]{4, 8},
                            new int[]{8, 1},
                        };
            out_EdgeToElementNoH.Clear();
            foreach (FemElement element in in_Elements)
            {
                foreach (int[] edgeStEdNo in edgeStEdNoList)
                {
                    int stNodeNumber = element.NodeNumbers[edgeStEdNo[0] - 1];
                    int edNodeNumber = element.NodeNumbers[edgeStEdNo[1] - 1];
                    string edgeKey = "";
                    if (stNodeNumber < edNodeNumber)
                    {
                        edgeKey = string.Format("{0}_{1}", stNodeNumber, edNodeNumber);
                    }
                    else
                    {
                        edgeKey = string.Format("{0}_{1}", edNodeNumber, stNodeNumber);
                    }
                    if (out_EdgeToElementNoH.ContainsKey(edgeKey))
                    {
                        out_EdgeToElementNoH[edgeKey].Add(element.No);
                    }
                    else
                    {
                        out_EdgeToElementNoH[edgeKey] = new List<int>();
                        out_EdgeToElementNoH[edgeKey].Add(element.No);
                    }
                }
            }
        }

        /// <summary>
        /// １次三角形要素：辺と要素の対応マップ作成
        /// </summary>
        /// <param name="in_Elements"></param>
        /// <param name="out_EdgeToElementNoH"></param>
        public static void TriFirstOrderElements_MkEdgeToElementNoH(IList<FemElement> in_Elements, ref Dictionary<string, IList<int>> out_EdgeToElementNoH)
        {
            // 辺と要素の対応マップ作成
            // 1次三角形要素の辺の始点、終点ローカル番号
            int[][] edgeStEdNoList = new int[Constants.TriEdgeCnt_FirstOrder][]
                        {
                            new int[]{1, 2},
                            new int[]{2, 3},
                            new int[]{3, 1},
                        };
            out_EdgeToElementNoH.Clear();
            foreach (FemElement element in in_Elements)
            {
                foreach (int[] edgeStEdNo in edgeStEdNoList)
                {
                    int stNodeNumber = element.NodeNumbers[edgeStEdNo[0] - 1];
                    int edNodeNumber = element.NodeNumbers[edgeStEdNo[1] - 1];
                    string edgeKey = "";
                    if (stNodeNumber < edNodeNumber)
                    {
                        edgeKey = string.Format("{0}_{1}", stNodeNumber, edNodeNumber);
                    }
                    else
                    {
                        edgeKey = string.Format("{0}_{1}", edNodeNumber, stNodeNumber);
                    }
                    if (out_EdgeToElementNoH.ContainsKey(edgeKey))
                    {
                        out_EdgeToElementNoH[edgeKey].Add(element.No);
                    }
                    else
                    {
                        out_EdgeToElementNoH[edgeKey] = new List<int>();
                        out_EdgeToElementNoH[edgeKey].Add(element.No);
                    }
                }
            }
        }

        /// <summary>
        /// １次四角形要素：辺と要素の対応マップ作成
        /// </summary>
        /// <param name="in_Elements"></param>
        /// <param name="out_EdgeToElementNoH"></param>
        public static void QuadFirstOrderElements_MkEdgeToElementNoH(IList<FemElement> in_Elements, ref Dictionary<string, IList<int>> out_EdgeToElementNoH)
        {
            // 辺と要素の対応マップ作成
            // １次四角形要素
            //
            //    4+     +3      x
            //     |     |
            //     |     |
            //    1+     +2
            //    
            //    y
            // １次四角形要素の辺の始点、終点ローカル番号
            int[][] edgeStEdNoList = new int[Constants.QuadEdgeCnt_FirstOrder][]
                        {
                            new int[]{1, 2},
                            new int[]{2, 3},
                            new int[]{3, 4},
                            new int[]{4, 1},
                        };
            out_EdgeToElementNoH.Clear();
            foreach (FemElement element in in_Elements)
            {
                foreach (int[] edgeStEdNo in edgeStEdNoList)
                {
                    int stNodeNumber = element.NodeNumbers[edgeStEdNo[0] - 1];
                    int edNodeNumber = element.NodeNumbers[edgeStEdNo[1] - 1];
                    string edgeKey = "";
                    if (stNodeNumber < edNodeNumber)
                    {
                        edgeKey = string.Format("{0}_{1}", stNodeNumber, edNodeNumber);
                    }
                    else
                    {
                        edgeKey = string.Format("{0}_{1}", edNodeNumber, stNodeNumber);
                    }
                    if (out_EdgeToElementNoH.ContainsKey(edgeKey))
                    {
                        out_EdgeToElementNoH[edgeKey].Add(element.No);
                    }
                    else
                    {
                        out_EdgeToElementNoH[edgeKey] = new List<int>();
                        out_EdgeToElementNoH[edgeKey].Add(element.No);
                    }
                }
            }
        }

        /// <summary>
        /// 節点と要素番号のマップ作成
        /// </summary>
        /// <param name="in_Elements"></param>
        /// <param name="out_NodeToElementNoH"></param>
        public static void MkNodeToElementNoH(IList<FemElement> in_Elements, ref Dictionary<int, IList<int>> out_NodeToElementNoH)
        {
            //check
            for (int ieleno = 0; ieleno < in_Elements.Count; ieleno++)
            {
                System.Diagnostics.Debug.Assert(in_Elements[ieleno].No == ieleno + 1);
            }
            // 節点と要素番号のマップ作成
            foreach (FemElement element in in_Elements)
            {
                foreach (int nodeNumber in element.NodeNumbers)
                {
                    if (out_NodeToElementNoH.ContainsKey(nodeNumber))
                    {
                        out_NodeToElementNoH[nodeNumber].Add(element.No);
                    }
                    else
                    {
                        out_NodeToElementNoH[nodeNumber] = new List<int>();
                        out_NodeToElementNoH[nodeNumber].Add(element.No);
                    }
                }
            }
        }

        /// <summary>
        /// 点が要素内に含まれる？
        /// </summary>
        /// <param name="element">要素</param>
        /// <param name="test_pp">テストする点</param>
        /// <param name="Nodes">節点リスト（要素は座標を持たないので節点リストが必要）</param>
        /// <returns></returns>
        public static bool IsPointInElement(FemElement element, double[] test_pp, IList<FemNode> nodes)
        {
            bool hit = false;
            int eNodeCnt = element.NodeNumbers.Length;
            Constants.FemElementShapeDV elemShapeDv;
            int order;
            int vertexCnt;
            GetElementShapeDvAndOrderByElemNodeCnt(eNodeCnt, out elemShapeDv, out order, out vertexCnt);

            if (vertexCnt == Constants.TriVertexCnt)
            {
                // ２次/１次三角形要素
                hit = TriElement_IsPointInElement(element, test_pp, nodes);
            }
            else if (vertexCnt == Constants.QuadVertexCnt)
            {
                // ２次/１次四角形要素
                hit = QuadElement_IsPointInElement(element, test_pp, nodes);
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            return hit;
        }

        /// <summary>
        /// 三角形要素：点が要素内に含まれる？
        /// </summary>
        /// <param name="element">要素</param>
        /// <param name="test_pp">テストする点</param>
        /// <param name="Nodes">節点リスト（要素は座標を持たないので節点リストが必要）</param>
        /// <returns></returns>
        public static bool TriElement_IsPointInElement(FemElement element, double[] test_pp, IList<FemNode> nodes)
        {
            bool hit = false;

            // 三角形の頂点数
            const int vertexCnt = Constants.TriVertexCnt;
            double[][] pps = new double[vertexCnt][];
            // 2次三角形要素の最初の３点＝頂点の座標を取得
            for (int ino = 0; ino < vertexCnt; ino++)
            {
                int nodeNumber = element.NodeNumbers[ino];
                FemNode node = nodes[nodeNumber - 1];
                System.Diagnostics.Debug.Assert(node.No == nodeNumber);
                pps[ino] = node.Coord;
            }
            // バウンディングボックス取得
            double minX = double.MaxValue;
            double maxX = double.MinValue;
            double minY = double.MaxValue;
            double maxY = double.MinValue;
            foreach (double[] pp in pps)
            {
                double xx = pp[0];
                double yy = pp[1];
                if (minX > xx)
                {
                    minX = xx;
                }
                if (maxX < xx)
                {
                    maxX = xx;
                }
                if (minY > yy)
                {
                    minY = yy;
                }
                if (maxY < yy)
                {
                    maxY = yy;
                }
            }
            // バウンディングボックスでチェック
            if (test_pp[0] < minX || test_pp[0] > maxX)
            {
                return hit;
            }
            if (test_pp[1] < minY || test_pp[1] > maxY)
            {
                return hit;
            }

            // 頂点？
            foreach (double[] pp in pps)
            {
                if (Math.Abs(pp[0] - test_pp[0]) < Constants.PrecisionLowerLimit && Math.Abs(pp[1] - test_pp[1]) < Constants.PrecisionLowerLimit)
                {
                    hit = true;
                    break;
                }
            }
            if (!hit)
            {
                // 面積から内部判定する
                double area = KerEMatTri.TriArea(pps[0], pps[1], pps[2]);
                double sumOfSubArea = 0.0;
                for (int ino = 0; ino < vertexCnt; ino++)
                {
                    double[][] subArea_pp = new double[vertexCnt][];
                    subArea_pp[0] = pps[ino];
                    subArea_pp[1] = pps[(ino + 1) % vertexCnt];
                    subArea_pp[2] = test_pp;
                    //foreach (double[] work_pp in subArea_pp)
                    //{
                    //    System.Diagnostics.Debug.Write("{0},{1}  ", work_pp[0], work_pp[1]);
                    //}
                    double subArea = KerEMatTri.TriArea(subArea_pp[0], subArea_pp[1], subArea_pp[2]);
                    //System.Diagnostics.Debug.Write("  subArea = {0}", subArea);
                    //System.Diagnostics.Debug.WriteLine();
                    //BUGFIX
                    //if (subArea <= 0.0)
                    // 丁度辺上の場合は、サブエリアの１つが０になるのでこれは許可しないといけない
                    if (subArea < -1.0 * Constants.PrecisionLowerLimit)  // 0未満
                    {
                        sumOfSubArea = 0.0;
                        break;
                        // 外側？
                    }
                    sumOfSubArea += Math.Abs(subArea);
                }
                if (Math.Abs(area - sumOfSubArea) < Constants.PrecisionLowerLimit)
                {
                    hit = true;
                }
            }
            return hit;
        }

        /// <summary>
        /// 四角形要素：点が要素内に含まれる？
        /// </summary>
        /// <param name="element">要素</param>
        /// <param name="test_pp">テストする点</param>
        /// <param name="Nodes">節点リスト（要素は座標を持たないので節点リストが必要）</param>
        /// <returns></returns>
        public static bool QuadElement_IsPointInElement(FemElement element, double[] test_pp, IList<FemNode> nodes)
        {
            bool hit = false;

            // 四角形の頂点数
            const int vertexCnt = Constants.QuadVertexCnt;
            double[][] pps = new double[vertexCnt][];
            int[] nodeNumbers = new int[vertexCnt];
            // 2次四角形要素の最初の４点＝頂点の座標を取得
            for (int ino = 0; ino < vertexCnt; ino++)
            {
                int nodeNumber = element.NodeNumbers[ino];
                FemNode node = nodes[nodeNumber - 1];
                System.Diagnostics.Debug.Assert(node.No == nodeNumber);
                pps[ino] = node.Coord;
                nodeNumbers[ino] = nodeNumber;
            }
            // バウンディングボックス取得
            double minX = double.MaxValue;
            double maxX = double.MinValue;
            double minY = double.MaxValue;
            double maxY = double.MinValue;
            foreach (double[] pp in pps)
            {
                double xx = pp[0];
                double yy = pp[1];
                if (minX > xx)
                {
                    minX = xx;
                }
                if (maxX < xx)
                {
                    maxX = xx;
                }
                if (minY > yy)
                {
                    minY = yy;
                }
                if (maxY < yy)
                {
                    maxY = yy;
                }
            }
            // バウンディングボックスでチェック
            if (test_pp[0] < minX || test_pp[0] > maxX)
            {
                return hit;
            }
            if (test_pp[1] < minY || test_pp[1] > maxY)
            {
                return hit;
            }

            // ２つの三角形に分ける
            //        s
            //        |
            //    3+  +  +2
            //    |   |   |
            // ---|---+---|-->r
            //    |   |   |
            //    0+  +  +1
            //        |
            FemElement[] tris = new FemElement[2];
            tris[0] = new FemElement();
            tris[0].NodeNumbers = new int[] { nodeNumbers[0], nodeNumbers[1], nodeNumbers[3], 0, 0, 0 };
            tris[1] = new FemElement();
            tris[1].NodeNumbers = new int[] { nodeNumbers[2], nodeNumbers[3], nodeNumbers[1], 0, 0, 0 };
            foreach (FemElement tri in tris)
            {
                bool hitInsideTri = TriElement_IsPointInElement(tri, test_pp, nodes);
                if (hitInsideTri)
                {
                    hit = true;
                    break;
                }
            }
            return hit;
        }

        /// <summary>
        /// 2点間距離の計算
        /// </summary>
        /// <param name="p"></param>
        /// <param name="p0"></param>
        /// <returns></returns>
        public static double GetDistance(double[] p, double[] p0)
        {
            return Math.Sqrt((p[0] - p0[0]) * (p[0] - p0[0]) + (p[1] - p0[1]) * (p[1] - p0[1]));
        }

        /// <summary>
        /// 要素の節点数から該当するFemElementインスタンスを作成する
        /// </summary>
        /// <param name="eNodeCnt"></param>
        /// <returns></returns>
        public static FemElement CreateFemElementByElementNodeCnt(int eNodeCnt)
        {
            FemElement femElement = null;
            Constants.FemElementShapeDV elemShapeDv;
            int order;
            int vertexCnt;
            GetElementShapeDvAndOrderByElemNodeCnt(eNodeCnt, out elemShapeDv, out order, out vertexCnt);

            if (vertexCnt == Constants.TriVertexCnt)
            {
                femElement = new FemTriElement();
            }
            else if (vertexCnt == Constants.QuadVertexCnt)
            {
                femElement = new FemQuadElement();
            }
            else
            {
                femElement = new FemElement();
            }
            return femElement;
        }


    }
}
