using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;

namespace VisualCutterForm.Lib.Flow.Nodes
{
    [NodeCategory("显示", "图像展示")]
    public class ImageDisplayNode : FlowNode
    {
        private readonly object _lock = new object();
        private Bitmap _previewBitmap;

        [NodeInput("图像", Description = "Mat")]
        public Mat Image { get; set; }

        public bool IsModified { get; private set; }

        public override Task ExecuteAsync(FlowContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Image != null && !Image.IsDisposed && !Image.Empty())
            {
                var bmp = ImageConverter.MatToBitmap(Image);
                lock (_lock)
                {
                    var old = _previewBitmap;
                    _previewBitmap = bmp;
                    old?.Dispose();
                    IsModified = true;
                }
            }
            else
            {
                IsModified = false;
            }

            return Task.CompletedTask;
        }

        public Bitmap GetPreviewBitmap()
        {
            lock (_lock)
            {
                if (_previewBitmap == null) return null;
                return new Bitmap(_previewBitmap);
            }
        }

        public void MarkViewed()
        {
            IsModified = false;
        }
    }
}
