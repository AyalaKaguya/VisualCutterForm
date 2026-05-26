using System;
using System.Collections.Generic;
using System.Linq;
using VisualMaster.Api;
using VisualMaster.WorkFlow.Triggers;
using VisualCutterForm.Legacy;
using CameraDeviceConfig = VisualMaster.CameraLink.Api.CameraDeviceConfig;

namespace VisualMaster.WorkFlow
{
    public class FlowGraph
    {
        public const string CurrentVersion = "1.2";

        private FlowProjectDefinition _project = FlowProjectDefinition.CreateDefault(CurrentVersion);

        public FlowProjectDefinition Project
        {
            get
            {
                if (_project == null)
                    _project = FlowProjectDefinition.CreateDefault(CurrentVersion);

                _project.EnsureInitialized(CurrentVersion);
                return _project;
            }
            set
            {
                _project = value ?? FlowProjectDefinition.CreateDefault(CurrentVersion);
                _project.EnsureInitialized(CurrentVersion);
            }
        }

        public string Name
        {
            get => Project.Metadata.Name;
            set => Project.Metadata.Name = string.IsNullOrWhiteSpace(value) ? "流程图" : value;
        }

        public string Version
        {
            get => Project.Metadata.Version;
            set => Project.Metadata.Version = string.IsNullOrWhiteSpace(value) ? CurrentVersion : value;
        }

        public DateTime CreatedAt
        {
            get => Project.Metadata.CreatedAt;
            set => Project.Metadata.CreatedAt = value;
        }

        public List<FlowSubGraph> SubGraphs
        {
            get => Project.SubGraphs;
            set => Project.SubGraphs = value ?? new List<FlowSubGraph>();
        }

        public List<CameraDeviceConfig> CameraDevices
        {
            get => Project.Resources.CameraDevices;
            set => Project.Resources.CameraDevices = value ?? new List<CameraDeviceConfig>();
        }

        public List<SerialDeviceConfig> SerialDevices
        {
            get => Project.Resources.SerialDevices;
            set => Project.Resources.SerialDevices = value ?? new List<SerialDeviceConfig>();
        }

        public List<TriggerEntry> Triggers
        {
            get => Project.Routing.Triggers;
            set => Project.Routing.Triggers = value ?? new List<TriggerEntry>();
        }

        public IReadOnlyList<CameraDeviceConfig> GetCameraDevicesOrLegacy()
        {
            return CameraDevices ?? new List<CameraDeviceConfig>();
        }

        public IReadOnlyList<SerialDeviceConfig> GetSerialDevicesOrLegacy()
        {
            return SerialDevices ?? new List<SerialDeviceConfig>();
        }

        public IReadOnlyList<CameraSlot> CreateLegacyCameraSlots()
        {
            return GetCameraDevicesOrLegacy()
                .Select(device => new CameraSlot
                {
                    SlotId = device.DeviceId,
                    SlotName = device.DisplayName,
                    AssignedSerial = device.AssignedSerial,
                    Settings = null,
                })
                .ToList();
        }

        public IReadOnlyList<SerialSlot> CreateLegacySerialSlots()
        {
            return GetSerialDevicesOrLegacy()
                .Select(device => new SerialSlot
                {
                    SlotId = device.DeviceId,
                    SlotName = device.DisplayName,
                    PortName = device.PortName,
                    BaudRate = device.BaudRate,
                    DataBits = device.DataBits,
                    Parity = device.Parity,
                    StopBits = device.StopBits,
                })
                .ToList();
        }

        public FlowSubGraph FindSubGraph(Guid id)
        {
            return SubGraphs.Find(s => s.Id == id);
        }

        public FlowSubGraph FindSubGraphByName(string name)
        {
            return SubGraphs.Find(s => s.Name == name);
        }

        public FlowSubGraph AddSubGraph(string name)
        {
            var sg = new FlowSubGraph
            {
                Name = name ?? $"子图{SubGraphs.Count + 1}",
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
