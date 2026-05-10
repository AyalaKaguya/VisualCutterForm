using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualMaster.WorkFlow
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

        private Dictionary<Guid, FlowNode> _nodeIndex;
        private List<FlowNode> _cachedTopoOrder;
        private List<List<FlowNode>> _cachedLevels;
        private bool _topoDirty = true;

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

        public void RebuildNodeIndex()
        {
            _nodeIndex = Nodes.ToDictionary(n => n.Id);
            _topoDirty = true;
        }

        public FlowNode FindNode(Guid nodeId)
        {
            if (_nodeIndex == null) RebuildNodeIndex();
            _nodeIndex.TryGetValue(nodeId, out var node);
            return node;
        }

        public List<FlowNode> GetTopologicalOrder()
        {
            if (!_topoDirty && _cachedTopoOrder != null)
                return _cachedTopoOrder;

            if (_nodeIndex == null) RebuildNodeIndex();

            var sorted = new List<FlowNode>();
            var visited = new HashSet<Guid>();
            var visiting = new HashSet<Guid>();

            foreach (var node in Nodes)
            {
                if (!visited.Contains(node.Id))
                    TopoVisit(node, sorted, visited, visiting);
            }

            _cachedTopoOrder = sorted;
            _topoDirty = false;
            return sorted;
        }

        public List<List<FlowNode>> GetTopologicalLevels()
        {
            if (!_topoDirty && _cachedLevels != null)
                return _cachedLevels;

            if (_nodeIndex == null) RebuildNodeIndex();

            var depth = new Dictionary<Guid, int>();
            var queue = new Queue<FlowNode>();

            foreach (var node in Nodes)
            {
                int inDegree = Connections.Count(c => c.ToNodeId == node.Id);
                if (inDegree == 0)
                {
                    depth[node.Id] = 0;
                    queue.Enqueue(node);
                }
            }

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                int currentDepth = depth[node.Id];

                foreach (var conn in Connections)
                {
                    if (conn.FromNodeId == node.Id)
                    {
                        if (_nodeIndex.TryGetValue(conn.ToNodeId, out var downstream))
                        {
                            int newDepth = currentDepth + 1;
                            if (!depth.ContainsKey(downstream.Id) || depth[downstream.Id] < newDepth)
                            {
                                depth[downstream.Id] = newDepth;
                                queue.Enqueue(downstream);
                            }
                        }
                    }
                }
            }

            var maxLevel = depth.Values.DefaultIfEmpty(0).Max();
            var levels = new List<List<FlowNode>>();
            for (int i = 0; i <= maxLevel; i++)
                levels.Add(new List<FlowNode>());

            foreach (var node in Nodes)
            {
                int level = depth.TryGetValue(node.Id, out int d) ? d : 0;
                levels[level].Add(node);
            }

            _cachedLevels = levels;
            return levels;
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
                    if (_nodeIndex.TryGetValue(conn.FromNodeId, out var src))
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

            _topoDirty = true;
        }
    }
}
