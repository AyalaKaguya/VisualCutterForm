using System;
using System.Collections.Generic;
using VisualMaster.Api;
using VisualMaster.WorkFlow.Triggers;

namespace VisualMaster.WorkFlow
{
    public class FlowProjectDefinition
    {
        public FlowProjectMetadata Metadata { get; set; } = new FlowProjectMetadata();
        public FlowProjectResources Resources { get; set; } = new FlowProjectResources();
        public FlowProjectRouting Routing { get; set; } = new FlowProjectRouting();
        public List<FlowSubGraph> SubGraphs { get; set; } = new List<FlowSubGraph>();

        public static FlowProjectDefinition CreateDefault(string version)
        {
            return new FlowProjectDefinition
            {
                Metadata = new FlowProjectMetadata
                {
                    Version = version,
                },
            };
        }

        public void EnsureInitialized(string version)
        {
            Metadata = Metadata ?? new FlowProjectMetadata();
            Resources = Resources ?? new FlowProjectResources();
            Routing = Routing ?? new FlowProjectRouting();
            SubGraphs = SubGraphs ?? new List<FlowSubGraph>();

            if (string.IsNullOrWhiteSpace(Metadata.Version))
                Metadata.Version = version;
        }
    }

    public class FlowProjectMetadata
    {
        public string Name { get; set; } = "流程图";
        public string Version { get; set; } = FlowGraph.CurrentVersion;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class FlowProjectResources
    {
        public List<CameraDeviceConfig> CameraDevices { get; set; } = new List<CameraDeviceConfig>();
        public List<SerialDeviceConfig> SerialDevices { get; set; } = new List<SerialDeviceConfig>();
    }

    public class FlowProjectRouting
    {
        public List<TriggerEntry> Triggers { get; set; } = new List<TriggerEntry>();
    }
}