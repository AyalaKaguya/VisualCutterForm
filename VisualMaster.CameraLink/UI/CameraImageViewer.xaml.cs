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
        private bool _isFitMode = true;

        public CameraImageViewer()
        {
            InitializeComponent();
            ScaleTransform.ScaleX = 1;
            ScaleTransform.ScaleY = 1;
            Loaded += (s, e) => FitToView();
            SizeChanged += (s, e) =>
            {
                if (_isFitMode)
                    FitToView();
                else
                    UpdateNavigator();
            };
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

            double viewWidth = Math.Max(1, ImageLayer.ActualWidth);
            double viewHeight = Math.Max(1, ImageLayer.ActualHeight);
            double scaleX = Math.Max(0.01, (viewWidth - 24) / _bitmap.Width);
            double scaleY = Math.Max(0.01, (viewHeight - 24) / _bitmap.Height);
            double scale = Math.Min(scaleX, scaleY);
            SetZoom(scale);
            CenterImage();
            _isFitMode = true;
            UpdateNavigator();
        }

        public void ActualSize()
        {
            SetZoom(1);
            CenterImage();
            _isFitMode = false;
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

            System.Windows.Point imagePoint = e.GetPosition(ImageBounds);
            double oldZoom = ScaleTransform.ScaleX;
            double factor = e.Delta > 0 ? 1.15 : 1 / 1.15;
            SetZoom(oldZoom * factor);

            double newZoom = ScaleTransform.ScaleX;
            TranslateTransform.X += imagePoint.X * (oldZoom - newZoom);
            TranslateTransform.Y += imagePoint.Y * (oldZoom - newZoom);

            _isFitMode = false;
            UpdateNavigator();
            e.Handled = true;
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
            _isFitMode = false;
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
            double viewWidth = Math.Max(1, ImageLayer.ActualWidth);
            double viewHeight = Math.Max(1, ImageLayer.ActualHeight);
            Rect viewportRect = new Rect(0, 0, viewWidth, viewHeight);
            Rect imageRect = GetDisplayedImageRect();
            Rect worldRect = Rect.Union(viewportRect, imageRect);
            worldRect.Inflate(Math.Max(16, worldRect.Width * 0.05), Math.Max(16, worldRect.Height * 0.05));

            double scale = Math.Min(Navigator.Width / Math.Max(1, worldRect.Width),
                Navigator.Height / Math.Max(1, worldRect.Height));
            double offsetX = (Navigator.Width - worldRect.Width * scale) / 2;
            double offsetY = (Navigator.Height - worldRect.Height * scale) / 2;

            SetNavigatorRect(NavigatorImage, imageRect, worldRect, scale, offsetX, offsetY);
            SetNavigatorRect(NavigatorViewport, viewportRect, worldRect, scale, offsetX, offsetY);
        }

        private void CenterImage()
        {
            if (_bitmap == null) return;

            double viewWidth = Math.Max(1, ImageLayer.ActualWidth);
            double viewHeight = Math.Max(1, ImageLayer.ActualHeight);
            TranslateTransform.X = (viewWidth - _bitmap.Width * ScaleTransform.ScaleX) / 2;
            TranslateTransform.Y = (viewHeight - _bitmap.Height * ScaleTransform.ScaleY) / 2;
        }

        private Rect GetDisplayedImageRect()
        {
            if (_bitmap == null) return Rect.Empty;
            return new Rect(
                TranslateTransform.X,
                TranslateTransform.Y,
                _bitmap.Width * ScaleTransform.ScaleX,
                _bitmap.Height * ScaleTransform.ScaleY);
        }

        private static void SetNavigatorRect(System.Windows.Shapes.Rectangle shape, Rect rect, Rect worldRect,
            double scale, double offsetX, double offsetY)
        {
            shape.Width = Math.Max(2, rect.Width * scale);
            shape.Height = Math.Max(2, rect.Height * scale);
            Canvas.SetLeft(shape, offsetX + (rect.X - worldRect.X) * scale);
            Canvas.SetTop(shape, offsetY + (rect.Y - worldRect.Y) * scale);
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
