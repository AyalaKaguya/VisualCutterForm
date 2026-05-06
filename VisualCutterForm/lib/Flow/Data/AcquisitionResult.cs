using System;
using System.Drawing;
using OpenCvSharp;

namespace VisualCutterForm.Lib.Flow.Data
{
    public class AcquisitionResult : IDisposable
    {
        public Mat Image { get; set; }
        public string CameraSerial { get; set; }
        public float ExposureTimeUs { get; set; }
        public float Gain { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public DateTime Timestamp { get; set; }
        public long FrameNumber { get; set; }
        public string TriggerModeUsed { get; set; }
        public string SourceFilePath { get; set; }

        public bool IsFromCamera => !string.IsNullOrEmpty(CameraSerial);
        public bool IsFromFile => !string.IsNullOrEmpty(SourceFilePath);

        public AcquisitionResult() { }

        public AcquisitionResult(Mat image)
        {
            Image = image?.Clone();
            Timestamp = DateTime.Now;
        }

        public Bitmap ToBitmap()
        {
            if (Image == null || Image.IsDisposed) return null;
            try
            {
                return ImageConverter.MatToBitmap(Image);
            }
            catch
            {
                return null;
            }
        }

        public AcquisitionResult Clone()
        {
            return new AcquisitionResult
            {
                Image = Image?.Clone(),
                CameraSerial = CameraSerial,
                ExposureTimeUs = ExposureTimeUs,
                Gain = Gain,
                Width = Width,
                Height = Height,
                Timestamp = Timestamp,
                FrameNumber = FrameNumber,
                TriggerModeUsed = TriggerModeUsed,
                SourceFilePath = SourceFilePath,
            };
        }

        public void Dispose()
        {
            Image?.Dispose();
            Image = null;
        }
    }
}
