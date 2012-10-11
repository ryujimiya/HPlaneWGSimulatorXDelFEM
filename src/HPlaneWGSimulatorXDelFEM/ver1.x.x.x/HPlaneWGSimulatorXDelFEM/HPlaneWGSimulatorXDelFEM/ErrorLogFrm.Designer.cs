namespace HPlaneWGSimulatorXDelFEM
{
    partial class ErrorLogFrm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.ErrorLogDGV = new System.Windows.Forms.DataGridView();
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.ErrorLogDGV)).BeginInit();
            this.SuspendLayout();
            // 
            // ErrorLogDGV
            // 
            this.ErrorLogDGV.AllowUserToAddRows = false;
            this.ErrorLogDGV.AllowUserToDeleteRows = false;
            this.ErrorLogDGV.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.ErrorLogDGV.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column1});
            this.ErrorLogDGV.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ErrorLogDGV.Location = new System.Drawing.Point(0, 0);
            this.ErrorLogDGV.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.ErrorLogDGV.Name = "ErrorLogDGV";
            this.ErrorLogDGV.ReadOnly = true;
            this.ErrorLogDGV.RowHeadersVisible = false;
            this.ErrorLogDGV.RowTemplate.Height = 21;
            this.ErrorLogDGV.Size = new System.Drawing.Size(331, 284);
            this.ErrorLogDGV.TabIndex = 0;
            // 
            // Column1
            // 
            this.Column1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Column1.HeaderText = "エラーログ";
            this.Column1.Name = "Column1";
            this.Column1.ReadOnly = true;
            this.Column1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // ErrorLogFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(331, 284);
            this.Controls.Add(this.ErrorLogDGV);
            this.Font = new System.Drawing.Font("MS UI Gothic", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "ErrorLogFrm";
            this.Text = "エラーログ";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ErrorLogFrm_FormClosed);
            ((System.ComponentModel.ISupportInitialize)(this.ErrorLogDGV)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView ErrorLogDGV;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
    }
}