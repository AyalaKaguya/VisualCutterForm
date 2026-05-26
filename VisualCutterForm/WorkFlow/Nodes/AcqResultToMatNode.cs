using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;
using VisualMaster.WorkFlow.Data;

namespace VisualMaster.WorkFlow.Nodes
{
    [NodeCategory("转换", "取像结果转Mat")]
    public class AcqResultToMatNode : FlowNode
    {
        [NodeInput("取像结果", Description = "AcquisitionResult")]
        public AcquisitionResult Input { get; set; }

        [NodeOutput("图像", Description = "Mat")]
        public Mat Image { get; set; }

        [NodeOutput("宽度", Description = "int")]
        public int Width { get; set; }

        [NodeOutput("高度", Description = "int")]
        public int Height { get; set; }

        public override Task ExecuteAsync(FlowContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Input != null && Input.Image != null && !Input.Image.IsDisposed && !Input.Image.Empty())
            {
                Image = Input.Image.Clone();
                Width = Input.Width;
                Height = Input.Height;
            }
            else
            {
                Image = null;
                Width = 0;
                Height = 0;
            }

            return Task.CompletedTask;
        }
    }
}
