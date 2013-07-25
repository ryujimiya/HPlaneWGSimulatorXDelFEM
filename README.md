#H面導波管シミュレーター X DelFEM  
  
**Latest Release**  
version1.6.0.5  
　[installer](https://github.com/ryujimiya/HPlaneWGSimulatorXDelFEM/tree/master/publish)  

![HPlaneWGSimulatorXDelFEM](http://cdn-ak.f.st-hatena.com/images/fotolife/r/ryujimiya/20120930/20120930073724.jpg)  

**News**  
  2012-12-19 HPlaneWGSimulatorXDelFEM version1.6.0.3 Release   
　　[DelFEM4Net 1.0.0.7](http://code.google.com/p/delfem4net/)に更新しました。   
　　KrdLab.clapack.FunctionExt.zhbgv(複素エルミートバンド行列の一般化固有値問題解法)を追加しました。    
    
  2012-12-03 HPlaneWGSimulatorXDelFEM version1.6.0.2(modified version1.6.0.1) Release   
　●パーツ移動追加   
　　頂点や辺、領域をドラッグして図面のパーツを移動できます。   
　　※マス目単位の移動に修正しました(version1.6.0.2)   
　●その他   
　　BUGFIX：図形作成のとき自動計算が実行されなかったのを修正   
   
  2012-11-25 HPlaneWGSimulatorXDelFEM version1.6.0.0 Release   
　通常版の機能をマージ   
　●E面導波管、平行平板TMの計算ができます。   
　平行平板 TE/TM   
　　両端を電気壁で遮蔽した平行平板導波路の解析を追加しました。  
　　TEはH面導波管と同じ定式化となります。   
　　TMは今回追加した定式化です。   
  E面導波管TE   
　　LSEモード(E面に垂直な電界が０)としてTEモードの定式化を行いました。   
　　まだ定式化にバグがあるかもしれませんが試作機能として追加しています。   
　   
　E面導波管伝達問題の計算例   
　　[E面コーナーベンド](http://ryujimiya.hatenablog.com/entry/2012/11/22/002733)  
　　[E面T分岐](http://ryujimiya.hatenablog.com/entry/2012/11/22/010458)   
　　[E面スタブ](http://ryujimiya.hatenablog.com/entry/2012/11/22/015323)   
　平行平板導波路(TMモード)の計算例   
　　[誘電体スラブ導波路終端](http://ryujimiya.hatenablog.com/entry/2012/11/25/204048)   
　   
　●散乱係数周波数特性の対数グラフ   
　●自動計算モード   
　●等高線図   
　等高線図の種類を追加しました。表示できる分布図は次の４つになりました。   
　　|Ez|分布図 (従来のもの)   
　　Ez実数部の分布図 (新規)   
　　Hベクトルのベクトル表示   
　　複素ポインティングベクトルのベクトル表示   
　　(おまけ）４画面表示   
　　※ベクトル表示は、ダブルクリックしてパネルを最大化しないとまともに見れないかもしれません。   
   
  2012-10-30 HPlaneWGSimulatorXDelFEM version1.0.0.3 Release  
　　PCOCGの前処理をILU(0)→ILU(1)に変更しました。  
　　clapack解法にzgbsv(バンド行列)を追加しました。  
  
  2012-10-16 HPlaneWGSimulatorXDelFEM version1.0.0.2 Release  
　　PCOCGの求解処理をDelFEMの機能をより多く使用し、計算速度を改善しました。  
　　Version1.2.0.1が最速かなと思っていましたがversion1.2.0.2は同等かそれ以上になっていると思います。  
　　また、従来のzgesvによる解法の処理も素の”HPlaneWGSimulator”で対応した処理を組み込んで特にメモリの確保処理を改善しています。  
  
  2012-10-11 HPlaneWGSimulatorXDelFEM version1.0.0.1 Release  
　　線形方程式の解法にDelFEMのPCOCGを組み込みました。この解法は回路によっては誤差があったり収束しなかったりしますが計算時間を大幅に短縮できます。  
　　従来のclapckの直接解法とあわせてご利用ください。  
  
**Summary**  
  
「HPlaneWGSimulator X DelFEM」は、H面導波管回路の散乱パラメータを計算するプログラム「[H面導波管シミュレーター(HPlaneWGSimulator)](https://github.com/ryujimiya/HPlaneWGSimulator)」のDelFEM版です。  
「HPlaneWGSimulator X DelFEM」では折れ線近似で回路形状をモデリングできます。使用できる要素は２次か１次の三角形要素です。  
FEMソルバー部分と電界分布図やSパラメータのグラフ等の結果表示部分は「H面導波管シミュレーター(HPlaneWGSimulator)」と同じです。  
  
最後に本アプリケーションでは下記ライブラリを使用しています。ここに記し深謝致します。  
- DelFEM  
CAD、メッシュ生成と線形方程式解法(PCOCG)に梅谷信行氏のDelFEMを利用しています。また有限要素法の計算に関して、一部DelFEMを引用しています。  
　　DelFEM　[有限要素法(FEM)のページ](http://ums.futene.net/)  
　　DelFEM4Net　[DelFEM4Net　DelFEM wrapper for C# (.net framework) applications](http://code.google.com/p/delfem4net/)   
- Lisys  
行列の固有値計算及び線形方程式計算にKrdLab氏のLisysを用いています。  
　　Lisys　 [KrdLabの不定期日記 2009-05-07](http://d.hatena.ne.jp/KrdLab/20090507)  
  
**About Licence**  
  
HPlaneWGSimulatorXDelFEMのアセンブリ、ソースコード（下記注釈を除く）の著作権は、りゅうじみやにありますが、それらの利用になんら制限はありません。ただし、動作の保証はできませんので予め御了承願います。  
※DelFEMソースコード及びアセンブリの著作権は、梅谷信行氏にあります。  
※同梱されているLisysの著作権は、KrdLab氏にあります。  
　　DelFEM: delfem handy environment for finite elemet analyisis  
　　　　http://code.google.com/p/delfem/  
　　Lisys: KrdLabの不定期日記  
　　　　http://d.hatena.ne.jp/KrdLab/20090507  

**Contact to Human**  
  
何かございましたら下記までご連絡ください。  
りゅうじみや ryujimiya(あっと)mail.goo.ne.jp  

