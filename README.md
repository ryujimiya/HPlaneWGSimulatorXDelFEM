#H面導波管シミュレーター X DelFEM  
  
**News**  
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

