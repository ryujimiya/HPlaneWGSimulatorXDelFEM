#H面導波管シミュレーター X DELFEM  

「HPlaneWGSimulator X DelFEM」は、H面導波管回路の散乱パラメータを計算するプログラム「[H面導波管シミュレーター(HPlaneWGSimulator)](https://github.com/ryujimiya/HPlaneWGSimulator)」のDelFEM版です。
「HPlaneWGSimulator X DelFEM」では折れ線近似で回路形状をモデリングできます。使用できる要素は２次か１次の三角形要素です。
FEMソルバー部分と電界分布図やSパラメータのグラフ等の結果表示部分は「H面導波管シミュレーター(HPlaneWGSimulator)」と同じです。

最後に本アプリケーションでは下記ライブラリを使用しています。ここに記し深謝致します。
- DelFEM
CADとメッシュ生成に梅谷信行氏のDelFEMを利用しています。また有限要素法の計算に関して、一部DelFEMを引用しています。
　　DelFEM　[有限要素法(FEM)のページ](http://ums.futene.net/)  

- Lisys
行列の固有値計算及び線形方程式計算にKrdLab氏のLisysを用いています。
　　Lisys　 [KrdLabの不定期日記 2009-05-07](http://d.hatena.ne.jp/KrdLab/20090507)  
