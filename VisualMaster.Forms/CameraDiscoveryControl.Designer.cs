using System.Drawing;
using System.Windows.Forms;

namespace VisualMaster.Forms
{
    partial class CameraDiscoveryControl
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListView _listView;
        private System.Windows.Forms.Button _btnRefresh;
        private System.Windows.Forms.Label _lblStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._btnRefresh = new System.Windows.Forms.Button();
            this._lblStatus = new System.Windows.Forms.Label();
            this._listView = new System.Windows.Forms.ListView();
            this.SuspendLayout();
            // 
            // _btnRefresh
            // 
            this._btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnRefresh.Font = new System.Drawing.Font("微软雅黑", 8.5F);
            this._btnRefresh.Location = new System.Drawing.Point(6, 6);
            this._btnRefresh.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this._btnRefresh.Name = "_btnRefresh";
            this._btnRefresh.Size = new System.Drawing.Size(90, 39);
            this._btnRefresh.TabIndex = 0;
            this._btnRefresh.Text = "刷新";
            // 
            // _lblStatus
            // 
            this._lblStatus.Font = new System.Drawing.Font("微软雅黑", 8.5F);
            this._lblStatus.Location = new System.Drawing.Point(105, 12);
            this._lblStatus.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this._lblStatus.Name = "_lblStatus";
            this._lblStatus.Size = new System.Drawing.Size(450, 30);
            this._lblStatus.TabIndex = 1;
            this._lblStatus.Text = "就绪";
            // 
            // _listView
            // 
            this._listView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._listView.Font = new System.Drawing.Font("微软雅黑", 9F);
            this._listView.FullRowSelect = true;
            this._listView.GridLines = true;
            this._listView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this._listView.HideSelection = false;
            this._listView.Location = new System.Drawing.Point(0, 51);
            this._listView.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this._listView.MultiSelect = false;
            this._listView.Name = "_listView";
            this._listView.Size = new System.Drawing.Size(553, 661);
            this._listView.TabIndex = 2;
            this._listView.UseCompatibleStateImageBehavior = false;
            this._listView.View = System.Windows.Forms.View.Details;
            // 
            // CameraDiscoveryControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this._listView);
            this.Controls.Add(this._lblStatus);
            this.Controls.Add(this._btnRefresh);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "CameraDiscoveryControl";
            this.Size = new System.Drawing.Size(555, 714);
            this.ResumeLayout(false);

        }
    }
}
