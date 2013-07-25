using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Runtime.InteropServices;
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
    class GraphPaperDrawer : CEmptyDrawer
    {
        ////////////////////////////////////////////////////////////////////
        // 定数
        ////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 方眼紙のグリッドの色
        /// </summary>
        private static readonly Color GridColor = Color.DarkGray;

        ////////////////////////////////////////////////////////////////////
        // 変数
        ////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 方眼紙の1辺の分割数
        /// </summary>
        private int MaxDiv = 30;
        /// <summary>
        /// １辺の長さ
        /// </summary>
        private double Width = 30; // マージンなし
        /// <summary>
        /// １マスの長さ
        /// </summary>
        private double DeltaX = 1.0;
        /// <summary>
        /// オフセット
        /// </summary>
        private double OfsX = 1.0;
        /// <summary>
        /// Z座標
        /// </summary>
        private double Z = 15;  // 最前面(方眼紙のマス目が物体で隠れないように)

        public GraphPaperDrawer(double w, int ndiv) : base()
        {
            Width = w;
            MaxDiv = ndiv;
            DeltaX = w / (double)ndiv; // マージンなし
            OfsX = - Width * 0.5;  // マージンなし
            Z = w * 0.5; // 最前面(方眼紙のマス目が物体で隠れないように)
            System.Diagnostics.Debug.WriteLine("GraphPapaerDrawer Width:{0} MaxDiv:{1} OfsX:{2} DeltaX:{3} Z:{4}", Width, MaxDiv, DeltaX, OfsX, Z);
        }

        public GraphPaperDrawer()
            : base()
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public override void Draw()
        {
            //base.Draw();

            //Gl.glDisable(Gl.GL_DEPTH_TEST); // 隠れている?オーダーを無効にして描画
            Gl.glColor3d(GridColor.R / (double)255, GridColor.G / (double)255.0, GridColor.B / (double)255);
            double hw = Width * 0.5;
            for (int y = 0; y < MaxDiv + 1; y++)
            {
                if (y % 5 == 0 || y == MaxDiv)
                {
                    Gl.glDisable(Gl.GL_LINE_STIPPLE);
                    Gl.glLineWidth(1.0f);
                }
                else
                {
                    Gl.glEnable(Gl.GL_LINE_STIPPLE);
                    Gl.glLineStipple(1, 0xAAAA);
                    Gl.glLineWidth(1.0f);
                }
                double yy = OfsX + DeltaX * y;
                double[] xxs = new double[]{ OfsX, OfsX + DeltaX * MaxDiv };
                Gl.glBegin(Gl.GL_LINES);
                foreach (double xx in xxs)
                {
                    Gl.glVertex3d(xx, yy, Z);
                }
                Gl.glEnd();
            }
            for (int x = 0; x < MaxDiv + 1; x++)
            {
                if (x % 5 == 0 || x == MaxDiv)
                {
                    Gl.glDisable(Gl.GL_LINE_STIPPLE);
                    Gl.glLineWidth(1.0f);
                }
                else
                {
                    Gl.glEnable(Gl.GL_LINE_STIPPLE);
                    Gl.glLineStipple(1, 0xAAAA);
                    Gl.glLineWidth(1.0f);
                }
                double xx = OfsX + DeltaX * x;
                double[] yys = new double[] { OfsX, OfsX + DeltaX * MaxDiv };
                Gl.glBegin(Gl.GL_LINES);
                foreach (double yy in yys)
                {
                    Gl.glVertex3d(xx, yy, Z);
                }
                Gl.glEnd();
            }
            //Gl.glEnable(Gl.GL_DEPTH_TEST);// Zオーダーを元に戻す(有効化)
        }

        public override void DrawSelection(uint idraw)
        {
            base.DrawSelection(idraw);
        }

        public override DelFEM4NetCom.CBoundingBox3D GetBoundingBox(double[] rot)
        {
            //DelFEM4NetCom.CBoundingBox3D bb = base.GetBoundingBox(rot);

            double hw = Width * 0.5;
            CBoundingBox3D bb = new CBoundingBox3D(-hw, hw, -hw, hw, Z, Z);
            return bb;
        }

        public override void AddSelected(int[] selec_flag)
        {
            base.AddSelected(selec_flag);
        }

        public override void ClearSelected()
        {
            base.ClearSelected();
        }
        
    }
}
