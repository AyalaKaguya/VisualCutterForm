using VisualMaster.Api;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace VisualMaster.Forms.Camera
{
    public partial class CameraDiscoveryControl : UserControl
    {
        private ICameraManager _manager;

        public event EventHandler<CameraInfo> CameraSelected;
        public CameraInfo SelectedCamera { get; private set; }

        public CameraDiscoveryControl()
        {
            Dock = DockStyle.Fill;
            InitializeComponent();
            this.Resize += OnResize;
            _btnRefresh.Click += (s, e) => RefreshList();
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
        }

        public void SetCameraManager(ICameraManager manager)
        {
            _manager = manager;
        }

        private void OnResize(object sender, EventArgs e)
        {
            _listView.Size = new System.Drawing.Size(ClientSize.Width, ClientSize.Height - 34);
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
