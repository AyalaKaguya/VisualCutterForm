using System;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;

namespace VisualMaster.WorkFlow.Nodes
{
    [NodeCategory("OpenCV", "高斯模糊")]
    public class CvGaussianBlurNode : FlowNode
    {
        [NodeInput("输入", Description = "Mat")]
        public Mat Input { get; set; }

        [NodeProperty("核大小", Category = "模糊", Min = 1, Max = 31)]
        public int KernelSize { get; set; } = 3;

        [NodeProperty("SigmaX", Category = "模糊")]
        public double SigmaX { get; set; }

        [NodeOutput("输出", Description = "Mat")]
        public Mat Output { get; set; }

        public override Task ExecuteAsync(FlowContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Input == null || Input.IsDisposed || Input.Empty()) { Output = null; return Task.CompletedTask; }

            var k = Math.Max(1, KernelSize | 1);
            using (var result = new Mat())
            {
                Cv2.GaussianBlur(Input, result, new Size(k, k), SigmaX);
                Output = result.Clone();
            }
            return Task.CompletedTask;
        }
    }

    [NodeCategory("OpenCV", "中值模糊")]
    public class CvMedianBlurNode : FlowNode
    {
        [NodeInput("输入", Description = "Mat")]
        public Mat Input { get; set; }

        [NodeProperty("核大小", Category = "模糊", Min = 1, Max = 31)]
        public int KernelSize { get; set; } = 3;

        [NodeOutput("输出", Description = "Mat")]
        public Mat Output { get; set; }

        public override Task ExecuteAsync(FlowContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Input == null || Input.IsDisposed || Input.Empty()) { Output = null; return Task.CompletedTask; }

            var k = Math.Max(1, KernelSize | 1);
            using (var result = new Mat())
            {
                Cv2.MedianBlur(Input, result, k);
                Output = result.Clone();
            }
            return Task.CompletedTask;
        }
    }

    [NodeCategory("OpenCV", "边缘检测")]
    public class CvCannyNode : FlowNode
    {
        [NodeInput("输入", Description = "Mat")]
        public Mat Input { get; set; }

        [NodeProperty("低阈值", Category = "边缘")]
        public double Threshold1 { get; set; } = 50;

        [NodeProperty("高阈值", Category = "边缘")]
        public double Threshold2 { get; set; } = 150;

        [NodeOutput("输出", Description = "Mat")]
        public Mat Output { get; set; }

        public override Task ExecuteAsync(FlowContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Input == null || Input.IsDisposed || Input.Empty()) { Output = null; return Task.CompletedTask; }

            using (var result = new Mat())
            {
                Cv2.Canny(Input, result, Threshold1, Threshold2);
                Output = result.Clone();
            }
            return Task.CompletedTask;
        }
    }

    [NodeCategory("OpenCV", "阈值二值化")]
    public class CvThresholdNode : FlowNode
    {
        [NodeInput("输入", Description = "Mat")]
        public Mat Input { get; set; }

        [NodeProperty("阈值", Category = "阈值")]
        public double Threshold { get; set; } = 128;

        [NodeProperty("最大值", Category = "阈值")]
        public double MaxValue { get; set; } = 255;

        [NodeProperty("类型", Category = "阈值")]
        public CvThresholdType ThresholdType { get; set; } = CvThresholdType.Binary;

        [NodeOutput("输出", Description = "Mat")]
        public Mat Output { get; set; }

        public override Task ExecuteAsync(FlowContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Input == null || Input.IsDisposed || Input.Empty()) { Output = null; return Task.CompletedTask; }

            using (var result = new Mat())
            {
                Cv2.Threshold(Input, result, Threshold, MaxValue, (ThresholdTypes)(int)ThresholdType);
                Output = result.Clone();
            }
            return Task.CompletedTask;
        }
    }

    public enum CvThresholdType
    {
        Binary = 0,
        BinaryInv = 1,
        Trunc = 2,
        ToZero = 3,
        ToZeroInv = 4,
        Otsu = 8,
    }

    [NodeCategory("OpenCV", "颜色转换")]
    public class CvCvtColorNode : FlowNode
    {
        [NodeInput("输入", Description = "Mat")]
        public Mat Input { get; set; }

        [NodeProperty("转换模式", Category = "颜色")]
        public CvColorConversion Conversion { get; set; } = CvColorConversion.BGR2GRAY;

        [NodeOutput("输出", Description = "Mat")]
        public Mat Output { get; set; }

        public override Task ExecuteAsync(FlowContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Input == null || Input.IsDisposed || Input.Empty()) { Output = null; return Task.CompletedTask; }

            using (var result = new Mat())
            {
                Cv2.CvtColor(Input, result, (ColorConversionCodes)(int)Conversion);
                Output = result.Clone();
            }
            return Task.CompletedTask;
        }
    }

    public enum CvColorConversion
    {
        BGR2GRAY = 6,
        GRAY2BGR = 8,
        BGR2HSV = 40,
        HSV2BGR = 54,
        BGR2RGB = 4,
        RGB2BGR = 4,
    }

    [NodeCategory("OpenCV", "图像缩放")]
    public class CvResizeNode : FlowNode
    {
        [NodeInput("输入", Description = "Mat")]
        public Mat Input { get; set; }

        [NodeProperty("宽度", Category = "尺寸")]
        public int Width { get; set; } = 640;

        [NodeProperty("高度", Category = "尺寸")]
        public int Height { get; set; } = 480;

        [NodeProperty("插值方式", Category = "尺寸")]
        public CvInterpolation Interpolation { get; set; } = CvInterpolation.Linear;

        [NodeOutput("输出", Description = "Mat")]
        public Mat Output { get; set; }

        public override Task ExecuteAsync(FlowContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Input == null || Input.IsDisposed || Input.Empty()) { Output = null; return Task.CompletedTask; }

            using (var result = new Mat())
            {
                Cv2.Resize(Input, result, new Size(Width, Height), 0, 0, (InterpolationFlags)(int)Interpolation);
                Output = result.Clone();
            }
            return Task.CompletedTask;
        }
    }

    public enum CvInterpolation
    {
        Nearest = 0,
        Linear = 1,
        Cubic = 2,
        Area = 3,
        Lanczos4 = 4,
    }

    [NodeCategory("OpenCV", "膨胀")]
    public class CvDilateNode : FlowNode
    {
        [NodeInput("输入", Description = "Mat")]
        public Mat Input { get; set; }

        [NodeProperty("核大小", Category = "形态", Min = 1, Max = 31)]
        public int KernelSize { get; set; } = 3;

        [NodeProperty("迭代次数", Category = "形态", Min = 1, Max = 10)]
        public int Iterations { get; set; } = 1;

        [NodeOutput("输出", Description = "Mat")]
        public Mat Output { get; set; }

        public override Task ExecuteAsync(FlowContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Input == null || Input.IsDisposed || Input.Empty()) { Output = null; return Task.CompletedTask; }

            var k = Math.Max(1, KernelSize);
            using (var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(k, k)))
            using (var result = new Mat())
            {
                Cv2.Dilate(Input, result, kernel, iterations: Iterations);
                Output = result.Clone();
            }
            return Task.CompletedTask;
        }
    }

    [NodeCategory("OpenCV", "腐蚀")]
    public class CvErodeNode : FlowNode
    {
        [NodeInput("输入", Description = "Mat")]
        public Mat Input { get; set; }

        [NodeProperty("核大小", Category = "形态", Min = 1, Max = 31)]
        public int KernelSize { get; set; } = 3;

        [NodeProperty("迭代次数", Category = "形态", Min = 1, Max = 10)]
        public int Iterations { get; set; } = 1;

        [NodeOutput("输出", Description = "Mat")]
        public Mat Output { get; set; }

        public override Task ExecuteAsync(FlowContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Input == null || Input.IsDisposed || Input.Empty()) { Output = null; return Task.CompletedTask; }

            var k = Math.Max(1, KernelSize);
            using (var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(k, k)))
            using (var result = new Mat())
            {
                Cv2.Erode(Input, result, kernel, iterations: Iterations);
                Output = result.Clone();
            }
            return Task.CompletedTask;
        }
    }
}
