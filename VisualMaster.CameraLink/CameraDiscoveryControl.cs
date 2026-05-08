using VisualMaster.Api;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace VisualMaster.CameraLink
{
    public class CameraDiscoveryControl : UserControl
    {
        private ListView _listView;
        private Button _btnRefresh;
        private Label _lblStatus;
        private CameraManager _manager;

        public event EventHandler<CameraInfo> CameraSelected;
        public CameraInfo SelectedCamera { get; private set; }

        public CameraDiscoveryControl()
        {
            Dock = DockStyle.Fill;
            BuildUI();
        }

        public void SetCameraManager(CameraManager manager)
        {
            _manager = manager;
        }

        public void RefreshList()
        {
            try
            {
                if (_manager != null)
                {
                    var cameras = _manager.EnumerateCameras();
                    PopulateListView(cameras);
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"枚举失败: {ex.Message}";
            }
        }

        private void BuildUI()
        {
            _btnRefresh = new Button
            {
                Text = "刷新",
                Location = new Point(4, 4),
                Size = new Size(60, 26),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 8.5F),
            };
            _btnRefresh.FlatAppearance.BorderSize = 1;
            _btnRefresh.Click += (s, e) => RefreshList();

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

            _listView.SelectedIndexChanged += (s, e) =>
            {
                if (_listView.SelectedItems.Count > 0)
                {
                    var tag = _listView.SelectedItems[0].Tag as CameraInfo;
                    SelectedCamera = tag;
                    CameraSelected?.Invoke(this, tag);
                }
                else
                {
                    SelectedCamera = null;
                }
            };

            Controls.Add(_btnRefresh);
            Controls.Add(_lblStatus);
            Controls.Add(_listView);

            Resize += (s, e) =>
            {
                _listView.Size = new Size(ClientSize.Width, ClientSize.Height - 34);
            };
        }

        private void PopulateListView(List<CameraInfo> cameras)
        {
            _listView.Items.Clear();

            if (cameras.Count == 0)
            {
                _lblStatus.Text = "未发现相机";
                return;
            }

            _lblStatus.Text = $"发现 {cameras.Count} 台相机";

            foreach (var cam in cameras)
            {
                var item = new ListViewItem(cam.ModelName ?? "-");
                item.SubItems.Add(cam.SerialNumber ?? "-");
                item.SubItems.Add(cam.TransportTypeName ?? "-");
                item.SubItems.Add(cam.UserDefinedName ?? "");
                item.Tag = cam;
                _listView.Items.Add(item);
            }
        }
    }
}
