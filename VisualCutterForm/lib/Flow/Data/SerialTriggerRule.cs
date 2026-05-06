using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace VisualCutterForm.Lib.Flow.Data
{
    public enum SerialMatchMode
    {
        Exact,
        Contains,
        Regex,
        BinaryMatch,
    }

    public class SerialTriggerRule
    {
        public string RuleId { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
        public string Description { get; set; }
        public SerialMatchMode Mode { get; set; } = SerialMatchMode.Contains;
        public string Pattern { get; set; }
        public byte[] BinaryPattern { get; set; }

        public bool AutoResponseEnabled { get; set; }
        public string AutoResponseText { get; set; }
        public byte[] AutoResponseBytes { get; set; }

        public Guid? TargetSubGraphId { get; set; }

        public bool Matches(string text)
        {
            if (text == null || Pattern == null) return false;

            switch (Mode)
            {
                case SerialMatchMode.Exact:
                    return text.Trim() == Pattern.Trim();
                case SerialMatchMode.Contains:
                    return text.Contains(Pattern);
                case SerialMatchMode.Regex:
                    try { return Regex.IsMatch(text, Pattern); }
                    catch { return false; }
                default:
                    return false;
            }
        }

        public bool MatchesBinary(byte[] data)
        {
            if (data == null || BinaryPattern == null) return false;
            if (BinaryPattern.Length == 0) return false;

            if (Mode != SerialMatchMode.BinaryMatch) return false;

            for (int i = 0; i <= data.Length - BinaryPattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < BinaryPattern.Length; j++)
                {
                    if (data[i + j] != BinaryPattern[j])
                    { match = false; break; }
                }
                if (match) return true;
            }
            return false;
        }

        public SerialTriggerRule Clone()
        {
            return new SerialTriggerRule
            {
                RuleId = RuleId,
                Description = Description,
                Mode = Mode,
                Pattern = Pattern,
                BinaryPattern = BinaryPattern != null ? (byte[])BinaryPattern.Clone() : null,
                AutoResponseEnabled = AutoResponseEnabled,
                AutoResponseText = AutoResponseText,
                AutoResponseBytes = AutoResponseBytes != null ? (byte[])AutoResponseBytes.Clone() : null,
                TargetSubGraphId = TargetSubGraphId,
            };
        }
    }
}
