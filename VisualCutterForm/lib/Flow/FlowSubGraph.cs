using System;
using System.Collections.Generic;

namespace VisualCutterForm.Lib.Flow
{
    public enum SubGraphTrigger
    {
        HardCameraTrigger,
        SoftManualTrigger,
        CommunicationTrigger,
        AlwaysRunning,
    }

    public class FlowSubGraph
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "子图";
        public SubGraphTrigger Trigger { get; set; } = SubGraphTrigger.SoftManualTrigger;
        public List<FlowNode> Nodes { get; set; } = new List<FlowNode>();
        public List<NodeConnection> Connections { get; set; } = new List<NodeConnection>();
        public bool IsRunning { get; set; }

        public FlowSubGraph Clone()
        {
            return new FlowSubGraph
            {
                Id = Id,
                Name = Name,
                Trigger = Trigger,
                Nodes = new List<FlowNode>(Nodes),
                Connections = new List<NodeConnection>(Connections),
            };
        }

        public FlowNode FindNode(Guid nodeId)
        {
            return Nodes.Find(n => n.Id == nodeId);
        }

        public List<FlowNode> GetTopologicalOrder()
        {
            var sorted = new List<FlowNode>();
            var visited = new HashSet<Guid>();
            var visiting = new HashSet<Guid>();

            foreach (var node in Nodes)
            {
                if (!visited.Contains(node.Id))
                    TopoVisit(node, sorted, visited, visiting);
            }

            return sorted;
        }

        private void TopoVisit(FlowNode node, List<FlowNode> sorted,
            HashSet<Guid> visited, HashSet<Guid> visiting)
        {
            if (visiting.Contains(node.Id))
                throw new InvalidOperationException($"Cycle detected at node '{node.Name}'.");

            if (visited.Contains(node.Id)) return;

            visiting.Add(node.Id);

            foreach (var conn in Connections)
            {
                if (conn.ToNodeId == node.Id)
                {
                    var src = Nodes.Find(n => n.Id == conn.FromNodeId);
                    if (src != null)
                        TopoVisit(src, sorted, visited, visiting);
                }
            }

            visiting.Remove(node.Id);
            visited.Add(node.Id);
            sorted.Add(node);
        }

        public void WireConnections()
        {
            foreach (var conn in Connections)
            {
                var fromNode = FindNode(conn.FromNodeId);
                var toNode = FindNode(conn.ToNodeId);
                if (fromNode == null || toNode == null) continue;

                var outPin = fromNode.FindOutput(conn.FromPinName);
                var inPin = toNode.FindInput(conn.ToPinName);
                if (outPin != null && inPin != null)
                {
                    try { inPin.Connect(outPin); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Wire error [{outPin.Name}->{inPin.Name}]: {ex.Message}"); }
                }
            }
        }
    }
}
