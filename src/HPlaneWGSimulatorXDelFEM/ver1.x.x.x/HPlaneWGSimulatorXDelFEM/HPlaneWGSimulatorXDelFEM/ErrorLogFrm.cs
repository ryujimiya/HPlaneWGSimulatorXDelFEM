using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace HPlaneWGSimulatorXDelFEM
{
    public partial class ErrorLogFrm : Form
    {
        ////////////////////////////////////////////////////////////////////////
        // 型
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// フォームコントロール同期用デリゲート
        /// </summary>
        delegate void InvokeDelegate();
        delegate void ParameterizedInvokeDelegate(params Object[] parameter);
        ////////////////////////////////////////////////////////////////////////
        // 定数
        ////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////
        // フィールド
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///  シングルトンのインスタンス
        /// </summary>
        private static ErrorLogFrm Instance = null;
        
        /// <summary>
        ///  インスタンスの取得
        /// </summary>
        /// <returns></returns>
        private static ErrorLogFrm getInstance()
        {
            if (Instance == null)
            {
                Form mainFrm = Application.OpenForms[0];
                mainFrm.Invoke(new InvokeDelegate(delegate()
                    {
                        Instance = new ErrorLogFrm();
                        //Instance.Show(mainFrm);
                        Instance.Show();
                    }));
            }
            return Instance;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        private ErrorLogFrm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// エラーログを追加する
        /// </summary>
        /// <param name="message"></param>
        public static void AddErrorLogMessage(string filename, string message)
        {
            getInstance().addErrorLogMessage(filename, message);
        }

        /// <summary>
        /// エラーログを追加する
        /// </summary>
        /// <param name="message"></param>
        private void addErrorLogMessage(string filename, string message)
        {
            Form mainFrm = Application.OpenForms[0];
            mainFrm.Invoke(new InvokeDelegate(delegate()
                {
                    // ファイル名
                    string fn = Path.GetFileNameWithoutExtension(filename);
                    // 列の追加
                    DataGridViewRow row = new DataGridViewRow();
                    row.CreateCells(ErrorLogDGV);
                    row.Cells[0].Value =  message + " (" + fn + ")";
                    ErrorLogDGV.Rows.Add(row);
                    //自動スクロール
                    ErrorLogDGV.FirstDisplayedScrollingRowIndex = ErrorLogDGV.Rows.Count - 1;
                }));
        }

        private void ErrorLogFrm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Instance = null;
        }
    }
}
