using System;
using System.Drawing;
using System.Windows.Forms;

namespace VisualCutterForm.Lib
{
    public static class LogRingBuffer
    {
        private const int MaxLines = 10000;

        public static void Append(RichTextBox rtb, string text, Color color)
        {
            if (rtb == null || rtb.IsDisposed) return;

            if (rtb.InvokeRequired)
            {
                rtb.BeginInvoke((Action)(() => Append(rtb, text, color)));
                return;
            }

            var ts = DateTime.Now.ToString("HH:mm:ss.fff");
            rtb.SelectionStart = rtb.TextLength;
            rtb.SelectionLength = 0;
            rtb.SelectionColor = Color.FromArgb(120, 120, 120);
            rtb.AppendText($"[{ts}] ");
            rtb.SelectionColor = color;
            rtb.AppendText(text + "\n");

            TrimHead(rtb);
            rtb.ScrollToCaret();
        }

        private static void TrimHead(RichTextBox rtb)
        {
            while (rtb.Lines.Length > MaxLines + 1)
            {
                var idx = rtb.GetFirstCharIndexFromLine(1);
                if (idx <= 0) break;
                rtb.Select(0, idx);
                rtb.SelectedText = "";
            }
        }
    }
}
