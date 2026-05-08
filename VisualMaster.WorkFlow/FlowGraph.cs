using System;
using System.Collections.Generic;
using System.Linq;
using VisualMaster.Api;

namespace VisualMaster.WorkFlow
{
    public class FlowGraph
    {
        public const string CurrentVersion = "2.0";

        public string Name { get; set; } = "流程图";
        public string Version { get; set; } = CurrentVersion;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<FlowSubGraph> SubGraphs { get; set; } = new List<FlowSubGraph>();
        public List<CameraSlot> CameraSlots { get; set; } = new List<CameraSlot>();

        public FlowSubGraph FindSubGraph(Guid id)
        {
            return SubGraphs.Find(s => s.Id == id);
        }

        public FlowSubGraph FindSubGraphByName(string name)
        {
            return SubGraphs.Find(s => s.Name == name);
        }

        public FlowSubGraph AddSubGraph(string name, SubGraphTrigger trigger = SubGraphTrigger.SoftManualTrigger)
        {
            var sg = new FlowSubGraph
            {
                Name = name ?? $"子图{SubGraphs.Count + 1}",
                Trigger = trigger,
            };
            SubGraphs.Add(sg);
            return sg;
        }

        public void RemoveSubGraph(Guid id)
        {
            SubGraphs.RemoveAll(s => s.Id == id);
        }

        public void ResetAllConnections()
        {
            foreach (var sg in SubGraphs)
            {
                foreach (var node in sg.Nodes)
                {
                    foreach (var input in node.Inputs)
                    {
                        input.Disconnect();
                    }
                }
            }
        }

        public void WireAllConnections()
        {
            foreach (var sg in SubGraphs)
            {
                sg.WireConnections();
            }
        }
    }
}
