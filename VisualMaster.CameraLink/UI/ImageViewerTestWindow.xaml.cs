using Microsoft.Win32;
using System;
using System.Drawing;
using System.Windows;

namespace VisualMaster.CameraLink.UI
{
    public partial class ImageViewerTestWindow : Window
    {
        public ImageViewerTestWindow()
        {
            InitializeComponent();
            PathText.Text = "未打开图像";
        }

        private void OnOpenFileClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files|*.bmp;*.png;*.jpg;*.jpeg;*.tif;*.tiff|All Files|*.*",
                Multiselect = false,
            };
            if (dialog.ShowDialog(this) != true) return;

            try
            {
                using (var bitmap = new Bitmap(dialog.FileName))
                    Viewer.SetBitmap(bitmap);
                PathText.Text = dialog.FileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"打开图像失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
