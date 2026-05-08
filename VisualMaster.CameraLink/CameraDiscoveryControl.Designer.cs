using System.Drawing;
using System.Windows.Forms;

namespace VisualMaster.CameraLink
{
    partial class CameraDiscoveryControl
    {
        private System.ComponentModel.IContainer components = null;
        private ListView _listView;
        private Button _btnRefresh;
        private Label _lblStatus;

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
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "CameraDiscoveryControl";
            _btnRefresh = new Button
            {
                Text = "刷新",
                Location = new Point(4, 4),
                Size = new Size(60, 26),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 8.5F),
            };
            _btnRefresh.FlatAppearance.BorderSize = 1;

            _lblStatus = new Label
            {
                Location = new Point(70, 8),
                Size = new Size(300, 20),
                Text = "就绪",
                Font = new Font("Microsoft YaHei", 8.5F),
            };

            _listView = new ListView
            {
                Location = new Point(0, 34),
                Size = new Size(ClientSize.Width, ClientSize.Height - 34),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Font = new Font("Microsoft YaHei", 9F),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };

            _listView.Columns.Add("型号", 180);
            _listView.Columns.Add("序列号", 160);
            _listView.Columns.Add("传输", 90);
            _listView.Columns.Add("用户名称", 120);

            Controls.Add(_btnRefresh);
            Controls.Add(_lblStatus);
            Controls.Add(_listView);

            Resize += (s, e) =>
            {
                _listView.Size = new Size(ClientSize.Width, ClientSize.Height - 34);
            };
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
