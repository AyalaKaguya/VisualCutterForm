using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;
using VisualCutterForm.Lib.Flow.Data;

namespace VisualCutterForm.Lib.Flow.Nodes
{
    [NodeCategory("取像", "文件取像")]
    public class FileAcquisitionNode : FlowNode
    {
        [NodeProperty("文件路径", Category = "取像")]
        public string FilePath { get; set; }

        [NodeProperty("文件模式", Category = "取像")]
        public string FilePattern { get; set; } = "*.bmp;*.jpg;*.png;*.tif";

        [NodeProperty("循环播放", Category = "取像")]
        public bool Loop { get; set; }

        [NodeProperty("排序方式", Category = "取像")]
        public FileSortMode SortMode { get; set; } = FileSortMode.Name;

        [NodeProperty("颜色排列", Category = "取像")]
        public ColorOrder ColorOrder { get; set; } = ColorOrder.BGR;

        [NodeOutput("取像结果", Description = "AcquisitionResult")]
        public AcquisitionResult Result { get; set; }

        private List<string> _fileList;
        private int _currentIndex;

        public override async Task ExecuteAsync(FlowContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            EnsureFileList();

            if (_fileList == null || _fileList.Count == 0)
                throw new InvalidOperationException("No image files found.");

            if (_currentIndex >= _fileList.Count)
            {
                if (Loop)
                    _currentIndex = 0;
                else
                    throw new InvalidOperationException("All files have been read (loop disabled).");
            }

            var path = _fileList[_currentIndex];
            _currentIndex++;

            Mat mat;
            try
            {
                mat = Cv2.ImRead(path, ImreadModes.Color);
                if (mat == null || mat.Empty())
                    throw new Exception("Failed to read image.");

                if (ColorOrder == ColorOrder.RGB)
                    Cv2.CvtColor(mat, mat, ColorConversionCodes.BGR2RGB);
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to read '{path}': {ex.Message}");
            }

            Result = new AcquisitionResult(mat)
            {
                SourceFilePath = path,
                Timestamp = DateTime.Now,
            };
            mat.Dispose();
        }

        private void EnsureFileList()
        {
            if (_fileList != null) return;

            var path = FilePath;
            if (string.IsNullOrEmpty(path))
                path = AppDomain.CurrentDomain.BaseDirectory;

            var dir = path;
            if (!Directory.Exists(dir))
            {
                dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                    return;
            }

            var extensions = (FilePattern ?? "*.bmp;*.jpg;*.png")
                .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().TrimStart('*').ToLowerInvariant())
                .ToHashSet();

            var allFiles = Directory.GetFiles(dir)
                .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            switch (SortMode)
            {
                case FileSortMode.Name:
                    allFiles.Sort(string.CompareOrdinal);
                    break;
                case FileSortMode.Date:
                    allFiles.Sort((a, b) => File.GetLastWriteTime(a).CompareTo(File.GetLastWriteTime(b)));
                    break;
                case FileSortMode.Numeric:
                    allFiles.Sort(new NaturalStringComparer());
                    break;
            }

            _fileList = allFiles;
            _currentIndex = 0;
        }

        public void Reset()
        {
            _fileList = null;
            _currentIndex = 0;
        }
    }

    public enum FileSortMode
    {
        Name,
        Date,
        Numeric,
    }

    public enum ColorOrder
    {
        BGR,
        RGB,
    }

    internal class NaturalStringComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            return SafeNativeMethods.StrCmpLogicalW(x, y);
        }
    }

    internal static class SafeNativeMethods
    {
        [System.Runtime.InteropServices.DllImport("shlwapi.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        public static extern int StrCmpLogicalW(string psz1, string psz2);
    }
}
