using System.Collections.Generic;
using System.Linq;
using VisualMaster.Communication.Api;

namespace VisualMaster.Communication.Core
{
    public sealed class CommunicationOutputBuilder
    {
        public byte[] Build(CommunicationOutputEventConfig config, IDictionary<string, string> variables)
        {
            var result = new List<byte>();
            if (config?.Segments == null) return result.ToArray();

            foreach (var segment in config.Segments)
            {
                string value = segment.Value ?? "";
                if (segment.Kind == CommunicationOutputSegmentKind.Variable && variables != null)
                    variables.TryGetValue(value, out value);

                result.AddRange(CommunicationDataConverter.Encode(value, segment.DataType, segment.ByteOrder));
            }

            return result.ToArray();
        }
    }
}
