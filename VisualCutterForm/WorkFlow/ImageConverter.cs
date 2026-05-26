using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using OpenCvSharp;

namespace VisualMaster.WorkFlow
{
    public static class ImageConverter
    {
        public static Mat BitmapToMat(Bitmap bitmap)
        {
            if (bitmap == null) return null;

            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);

            try
            {
                int channels;
                MatType type;

                switch (bitmap.PixelFormat)
                {
                    case PixelFormat.Format24bppRgb:
                        channels = 3;
                        type = MatType.CV_8UC3;
                        break;
                    case PixelFormat.Format32bppArgb:
                    case PixelFormat.Format32bppRgb:
                        channels = 4;
                        type = MatType.CV_8UC4;
                        break;
                    case PixelFormat.Format8bppIndexed:
                        channels = 1;
                        type = MatType.CV_8UC1;
                        break;
                    default:
                        bitmap.UnlockBits(bmpData);
                        using (var bmp24 = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb))
                        {
                            using (var g = Graphics.FromImage(bmp24))
                            {
                                g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
                            }
                            return BitmapToMat(bmp24);
                        }
                }

                var mat = new Mat(bitmap.Height, bitmap.Width, type);

                if (bmpData.Stride == (int)mat.Step())
                {
                    int size = bmpData.Stride * bitmap.Height;
                    byte[] buffer = new byte[size];
                    Marshal.Copy(bmpData.Scan0, buffer, 0, size);
                    Marshal.Copy(buffer, 0, mat.Data, size);
                }
                else
                {
                    int rowBytes = bitmap.Width * channels;
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        IntPtr srcRow = IntPtr.Add(bmpData.Scan0, y * bmpData.Stride);
                        byte[] rowBuf = new byte[rowBytes];
                        Marshal.Copy(srcRow, rowBuf, 0, rowBytes);
                        Marshal.Copy(rowBuf, 0, IntPtr.Add(mat.Data, (int)(y * mat.Step())), rowBytes);
                    }
                }

                if (channels == 3)
                    Cv2.CvtColor(mat, mat, ColorConversionCodes.BGR2RGB);
                else if (channels == 4)
                    Cv2.CvtColor(mat, mat, ColorConversionCodes.BGRA2BGR);

                return mat;
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }
        }

        public static Bitmap MatToBitmap(Mat mat)
        {
            if (mat == null || mat.IsDisposed || mat.Empty()) return null;

            Mat temp = null;
            try
            {
                PixelFormat pixelFormat;
                Mat display;
                int channels;

                switch (mat.Channels())
                {
                    case 1:
                        display = mat;
                        channels = 1;
                        pixelFormat = PixelFormat.Format8bppIndexed;
                        break;
                    case 3:
                        temp = new Mat();
                        Cv2.CvtColor(mat, temp, ColorConversionCodes.BGR2RGB);
                        display = temp;
                        channels = 3;
                        pixelFormat = PixelFormat.Format24bppRgb;
                        break;
                    case 4:
                        temp = new Mat();
                        Cv2.CvtColor(mat, temp, ColorConversionCodes.BGRA2BGR);
                        display = temp;
                        channels = 3;
                        pixelFormat = PixelFormat.Format24bppRgb;
                        break;
                    default:
                        return null;
                }

                var bitmap = new Bitmap(display.Width, display.Height, pixelFormat);

                if (pixelFormat == PixelFormat.Format8bppIndexed)
                {
                    var palette = bitmap.Palette;
                    for (int i = 0; i < 256; i++)
                        palette.Entries[i] = Color.FromArgb(i, i, i);
                    bitmap.Palette = palette;
                }

                var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                var bmpData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, pixelFormat);

                try
                {
                    if (bmpData.Stride == (int)display.Step())
                    {
                        int size = (int)display.Step() * display.Height;
                        byte[] buffer = new byte[size];
                        Marshal.Copy(display.Data, buffer, 0, size);
                        Marshal.Copy(buffer, 0, bmpData.Scan0, size);
                    }
                    else
                    {
                        int rowBytes = display.Width * channels;
                        for (int y = 0; y < display.Height; y++)
                        {
                            byte[] rowBuf = new byte[rowBytes];
                            Marshal.Copy(IntPtr.Add(display.Data, (int)(y * display.Step())), rowBuf, 0, rowBytes);
                            Marshal.Copy(rowBuf, 0, IntPtr.Add(bmpData.Scan0, y * bmpData.Stride), rowBytes);
                        }
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bmpData);
                }

                return bitmap;
            }
            finally
            {
                temp?.Dispose();
            }
        }
    }
}
