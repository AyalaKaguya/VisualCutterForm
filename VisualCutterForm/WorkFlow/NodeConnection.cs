using System;

namespace VisualMaster.WorkFlow
{
    public class NodeConnection
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid FromNodeId { get; set; }
        public string FromPinName { get; set; }
        public Guid ToNodeId { get; set; }
        public string ToPinName { get; set; }

        public NodeConnection() { }

        public NodeConnection(Guid fromNodeId, string fromPinName, Guid toNodeId, string toPinName)
        {
            FromNodeId = fromNodeId;
            FromPinName = fromPinName;
            ToNodeId = toNodeId;
            ToPinName = toPinName;
        }
    }
}
