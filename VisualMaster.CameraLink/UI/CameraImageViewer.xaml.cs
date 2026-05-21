using Microsoft.Win32;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace VisualMaster.CameraLink.UI
{
    public partial class CameraImageViewer : UserControl
    {
        private Bitmap _bitmap;
        private bool _isPanning;
        private System.Windows.Point _lastPanPoint;

        public CameraImageViewer()
        {
            InitializeComponent();
            ScaleTransform.ScaleX = 1;
            ScaleTransform.ScaleY = 1;
            Loaded += (s, e) => FitToView();
            SizeChanged += (s, e) => UpdateNavigator();
        }

        public void SetBitmap(Bitmap bitmap)
        {
            _bitmap?.Dispose();
            _bitmap = bitmap == null ? null : new Bitmap(bitmap);

            if (_bitmap == null)
            {
                ImageHost.Source = null;
                InfoText.Text = "无图像";
                UpdateNavigator();
                return;
            }

            ImageHost.Source = ToBitmapSource(_bitmap);
            ImageBounds.Width = _bitmap.Width;
            ImageBounds.Height = _bitmap.Height;
            InfoText.Text = $"{_bitmap.Width} x {_bitmap.Height}  {_bitmap.PixelFormat}";
            FitToView();
        }

        public void SetMat(object mat)
        {
            if (mat == null)
            {
                SetBitmap(null);
                return;
            }

            MethodInfo method = mat.GetType().GetMethod("ToBitmap", Type.EmptyTypes);
            if (method == null)
                throw new ArgumentException("Mat object must expose ToBitmap().", nameof(mat));

            using (var bitmap = method.Invoke(mat, null) as Bitmap)
                SetBitmap(bitmap);
        }

        public void FitToView()
        {
            if (_bitmap == null || ActualWidth <= 0 || ActualHeight <= 0) return;

            double scaleX = Math.Max(0.01, (ActualWidth - 24) / _bitmap.Width);
            double scaleY = Math.Max(0.01, (ActualHeight - 24) / _bitmap.Height);
            double scale = Math.Min(scaleX, scaleY);
            SetZoom(scale);
            TranslateTransform.X = 0;
            TranslateTransform.Y = 0;
            UpdateNavigator();
        }

        public void ActualSize()
        {
            SetZoom(1);
            TranslateTransform.X = 0;
            TranslateTransform.Y = 0;
            UpdateNavigator();
        }

        private void SetZoom(double zoom)
        {
            zoom = Math.Max(0.02, Math.Min(32, zoom));
            ScaleTransform.ScaleX = zoom;
            ScaleTransform.ScaleY = zoom;
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_bitmap == null) return;
            double factor = e.Delta > 0 ? 1.15 : 1 / 1.15;
            SetZoom(ScaleTransform.ScaleX * factor);
            UpdateNavigator();
        }

        private void OnImageMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isPanning = true;
            _lastPanPoint = e.GetPosition(Root);
            ImageHost.CaptureMouse();
        }

        private void OnImageMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isPanning = false;
            ImageHost.ReleaseMouseCapture();
        }

        private void OnImageMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPanning) return;

            System.Windows.Point point = e.GetPosition(Root);
            TranslateTransform.X += point.X - _lastPanPoint.X;
            TranslateTransform.Y += point.Y - _lastPanPoint.Y;
            _lastPanPoint = point;
            UpdateNavigator();
        }

        private void OnFitClick(object sender, RoutedEventArgs e) => FitToView();

        private void OnActualSizeClick(object sender, RoutedEventArgs e) => ActualSize();

        private void OnSaveClick(object sender, RoutedEventArgs e) => SaveImage();

        private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var menu = new ContextMenu();
            menu.Items.Add(CreateMenuItem("适合窗口", (s, args) => FitToView()));
            menu.Items.Add(CreateMenuItem("1:1 像素", (s, args) => ActualSize()));
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateMenuItem("保存图像...", (s, args) => SaveImage()));
            menu.IsOpen = true;
        }

        private static MenuItem CreateMenuItem(string text, RoutedEventHandler handler)
        {
            var item = new MenuItem { Header = text };
            item.Click += handler;
            return item;
        }

        private void SaveImage()
        {
            if (_bitmap == null) return;

            var dialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|Bitmap Image|*.bmp|JPEG Image|*.jpg",
                FileName = $"camera_{DateTime.Now:yyyyMMdd_HHmmss}.png",
            };
            if (dialog.ShowDialog() != true) return;

            var ext = Path.GetExtension(dialog.FileName)?.ToLowerInvariant();
            var format = ext == ".bmp" ? ImageFormat.Bmp : ext == ".jpg" || ext == ".jpeg" ? ImageFormat.Jpeg : ImageFormat.Png;
            _bitmap.Save(dialog.FileName, format);
        }

        private void UpdateNavigator()
        {
            if (_bitmap == null)
            {
                Navigator.Visibility = Visibility.Collapsed;
                return;
            }

            Navigator.Visibility = Visibility.Visible;
            double imageRatio = (double)_bitmap.Width / Math.Max(1, _bitmap.Height);
            double navRatio = Navigator.Width / Navigator.Height;
            double imageW = imageRatio >= navRatio ? Navigator.Width : Navigator.Height * imageRatio;
            double imageH = imageRatio >= navRatio ? Navigator.Width / imageRatio : Navigator.Height;
            double imageX = (Navigator.Width - imageW) / 2;
            double imageY = (Navigator.Height - imageH) / 2;

            NavigatorImage.Width = imageW;
            NavigatorImage.Height = imageH;
            Canvas.SetLeft(NavigatorImage, imageX);
            Canvas.SetTop(NavigatorImage, imageY);

            double visibleW = Math.Min(1, ActualWidth / Math.Max(1, _bitmap.Width * ScaleTransform.ScaleX));
            double visibleH = Math.Min(1, ActualHeight / Math.Max(1, _bitmap.Height * ScaleTransform.ScaleY));
            NavigatorViewport.Width = Math.Max(8, imageW * visibleW);
            NavigatorViewport.Height = Math.Max(8, imageH * visibleH);
            Canvas.SetLeft(NavigatorViewport, imageX);
            Canvas.SetTop(NavigatorViewport, imageY);
        }

        private static BitmapSource ToBitmapSource(Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
    }
}
