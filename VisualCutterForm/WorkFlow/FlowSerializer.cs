using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using VisualMaster.Api;
using VisualMaster.WorkFlow.Nodes;
using VisualMaster.WorkFlow.Triggers;
using VisualCutterForm.Legacy;

namespace VisualMaster.WorkFlow
{
    public static class FlowSerializer
    {
        public static string Serialize(FlowGraph graph)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Auto,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            };

            return JsonConvert.SerializeObject(SerializeGraph(graph), settings);
        }

        public static void SerializeToFile(FlowGraph graph, string filePath)
        {
            var json = Serialize(graph);
            File.WriteAllText(filePath, json);
        }

        public static FlowGraph Deserialize(string json, List<string> warnings = null)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
            };

            var sGraph = JsonConvert.DeserializeObject<SerializedGraph>(json, settings);
            return DeserializeGraph(sGraph, warnings);
        }

        public static FlowGraph DeserializeFromFile(string filePath, List<string> warnings = null)
        {
            var json = File.ReadAllText(filePath);
            return Deserialize(json, warnings);
        }

        private static SerializedGraph SerializeGraph(FlowGraph graph)
        {
            var project = graph.Project;
            var cameraDevices = graph.GetCameraDevicesOrLegacy();
            var serialDevices = graph.GetSerialDevicesOrLegacy();
            var legacyCameraSlots = graph.CreateLegacyCameraSlots();
            var legacySerialSlots = graph.CreateLegacySerialSlots();

            return new SerializedGraph
            {
                Project = SerializeProject(project),
                Name = graph.Name,
                Version = graph.Version,
                CreatedAt = graph.CreatedAt,
                SubGraphs = project.SubGraphs.Select(SerializeSubGraph).ToList(),
                CameraDevices = cameraDevices.Select(SerializeCameraDevice).ToList(),
                SerialDevices = serialDevices.Select(SerializeSerialDevice).ToList(),
                CameraSlots = legacyCameraSlots.Select(SerializeCameraSlot).ToList(),
                SerialSlots = legacySerialSlots.Select(SerializeSerialSlot).ToList(),
                Triggers = graph.Project.Routing.Triggers?.Select(SerializeTrigger).ToList() ?? new List<SerializedTrigger>(),
            };
        }

        private static FlowGraph DeserializeGraph(SerializedGraph s, List<string> warnings = null)
        {
            var graph = new FlowGraph();

            if (s.Project != null)
            {
                graph.Project = DeserializeProject(s.Project);
            }
            else
            {
                graph.Project = new FlowProjectDefinition
                {
                    Metadata = new FlowProjectMetadata
                    {
                        Name = s.Name,
                        Version = FlowGraph.CurrentVersion,
                        CreatedAt = s.CreatedAt,
                    },
                };
            }

            var sourceVersion = s.Project?.Metadata?.Version ?? s.Version;
            if (sourceVersion != FlowGraph.CurrentVersion)
                warnings?.Add($"流程版本从 {sourceVersion ?? "?"} 升级到 {FlowGraph.CurrentVersion}");

            if (graph.Project.Resources.CameraDevices.Count == 0 && s.CameraDevices != null)
            {
                foreach (var cd in s.CameraDevices)
                {
                    graph.Project.Resources.CameraDevices.Add(new CameraDeviceConfig
                    {
                        DeviceId = cd.DeviceId,
                        DisplayName = cd.DisplayName,
                        AssignedSerial = cd.AssignedSerial,
                        Settings = cd.Settings ?? new CameraSettings(),
                    });
                }
            }
            else if (graph.Project.Resources.CameraDevices.Count == 0 && s.CameraSlots != null)
            {
                foreach (var cs in s.CameraSlots)
                {
                    graph.Project.Resources.CameraDevices.Add(new CameraDeviceConfig
                    {
                        DeviceId = cs.SlotId,
                        DisplayName = cs.SlotName,
                        AssignedSerial = cs.AssignedSerial,
                        Settings = cs.Settings ?? new CameraSettings(),
                    });
                }
            }

            if (graph.Project.Resources.SerialDevices.Count == 0 && s.SerialDevices != null)
            {
                foreach (var sd in s.SerialDevices)
                {
                    graph.Project.Resources.SerialDevices.Add(new SerialDeviceConfig
                    {
                        DeviceId = sd.DeviceId,
                        DisplayName = sd.DisplayName,
                        PortName = sd.PortName,
                        BaudRate = sd.BaudRate,
                        DataBits = sd.DataBits,
                        Parity = sd.Parity,
                        StopBits = sd.StopBits,
                    });
                }
            }
            else if (graph.Project.Resources.SerialDevices.Count == 0 && s.SerialSlots != null)
            {
                foreach (var ss in s.SerialSlots)
                {
                    graph.Project.Resources.SerialDevices.Add(new SerialDeviceConfig
                    {
                        DeviceId = ss.SlotId,
                        DisplayName = ss.SlotName,
                        PortName = ss.PortName,
                        BaudRate = ss.BaudRate,
                        DataBits = ss.DataBits,
                        Parity = ss.Parity,
                        StopBits = ss.StopBits,
                    });
                }
            }

            if (graph.Project.SubGraphs.Count == 0)
            {
                foreach (var sg in s.SubGraphs ?? Enumerable.Empty<SerializedSubGraph>())
                {
                    var subGraph = DeserializeSubGraph(sg, warnings);
                    graph.Project.SubGraphs.Add(subGraph);
                }
            }

            if (graph.Project.Routing.Triggers.Count == 0 && s.Triggers != null)
            {
                foreach (var st in s.Triggers)
                    graph.Project.Routing.Triggers.Add(DeserializeTrigger(st));
            }

            graph.WireAllConnections();
            return graph;
        }

        private static SerializedProject SerializeProject(FlowProjectDefinition project)
        {
            project = project ?? FlowProjectDefinition.CreateDefault(FlowGraph.CurrentVersion);
            project.EnsureInitialized(FlowGraph.CurrentVersion);

            return new SerializedProject
            {
                Metadata = new SerializedProjectMetadata
                {
                    Name = project.Metadata.Name,
                    Version = project.Metadata.Version,
                    CreatedAt = project.Metadata.CreatedAt,
                },
                Resources = new SerializedProjectResources
                {
                    CameraDevices = project.Resources.CameraDevices.Select(SerializeCameraDevice).ToList(),
                    SerialDevices = project.Resources.SerialDevices.Select(SerializeSerialDevice).ToList(),
                },
                Routing = new SerializedProjectRouting
                {
                    Triggers = project.Routing.Triggers.Select(SerializeTrigger).ToList(),
                },
                SubGraphs = project.SubGraphs.Select(SerializeSubGraph).ToList(),
            };
        }

        private static FlowProjectDefinition DeserializeProject(SerializedProject project)
        {
            var definition = FlowProjectDefinition.CreateDefault(FlowGraph.CurrentVersion);
            if (project == null)
                return definition;

            definition.Metadata = new FlowProjectMetadata
            {
                Name = project.Metadata?.Name ?? "流程图",
                Version = project.Metadata?.Version ?? FlowGraph.CurrentVersion,
                CreatedAt = project.Metadata?.CreatedAt ?? DateTime.Now,
            };

            if (project.Resources?.CameraDevices != null)
            {
                definition.Resources.CameraDevices = project.Resources.CameraDevices
                    .Select(cd => new CameraDeviceConfig
                    {
                        DeviceId = cd.DeviceId,
                        DisplayName = cd.DisplayName,
                        AssignedSerial = cd.AssignedSerial,
                        Settings = cd.Settings ?? new CameraSettings(),
                    })
                    .ToList();
            }

            if (project.Resources?.SerialDevices != null)
            {
                definition.Resources.SerialDevices = project.Resources.SerialDevices
                    .Select(sd => new SerialDeviceConfig
                    {
                        DeviceId = sd.DeviceId,
                        DisplayName = sd.DisplayName,
                        PortName = sd.PortName,
                        BaudRate = sd.BaudRate,
                        DataBits = sd.DataBits,
                        Parity = sd.Parity,
                        StopBits = sd.StopBits,
                    })
                    .ToList();
            }

            if (project.Routing?.Triggers != null)
            {
                definition.Routing.Triggers = project.Routing.Triggers
                    .Select(DeserializeTrigger)
                    .ToList();
            }

            if (project.SubGraphs != null)
            {
                definition.SubGraphs = project.SubGraphs
                    .Select(sg => DeserializeSubGraph(sg))
                    .ToList();
            }

            definition.EnsureInitialized(FlowGraph.CurrentVersion);
            return definition;
        }

        private static SerializedSubGraph SerializeSubGraph(FlowSubGraph sg)
        {
            return new SerializedSubGraph
            {
                Id = sg.Id,
                Name = sg.Name,
                Nodes = sg.Nodes.Select(SerializeNode).ToList(),
                Connections = sg.Connections.Select(c => new SerializedConnection
                {
                    Id = c.Id,
                    FromNodeId = c.FromNodeId,
                    FromPinName = c.FromPinName,
                    ToNodeId = c.ToNodeId,
                    ToPinName = c.ToPinName,
                }).ToList(),
            };
        }

        private static FlowSubGraph DeserializeSubGraph(SerializedSubGraph s, List<string> warnings = null)
        {
            var sg = new FlowSubGraph
            {
                Id = s.Id,
                Name = s.Name,
            };

            foreach (var sn in s.Nodes)
            {
                var node = NodeFactory.CreateNode(sn.NodeTypeName);
                node.Id = sn.Id;
                node.Name = sn.Name ?? node.Name;
                node.NodeX = sn.NodeX;
                node.NodeY = sn.NodeY;

                foreach (var pv in sn.Properties)
                {
                    try { node.SetNodeProperty(pv.Key, pv.Value); }
                    catch (Exception ex) { warnings?.Add($"属性 [{pv.Key}] 加载失败 [{node.Name}]: {ex.Message}"); }
                }

                if (sn.UserPins != null)
                {
                    foreach (var sp in sn.UserPins)
                    {
                        if (sp == null || string.IsNullOrEmpty(sp.Name)) continue;

                        var type = PinTypeResolver.Resolve(sp.TypeName);
                        if (type == null) continue;

                        if (sp.IsInput)
                        {
                            var pin = node.AddInputPin(sp.Name, type);
                            if (pin is InputPin inp && sp.DefaultValue != null)
                            {
                                try { inp.DefaultValue = Convert.ChangeType(sp.DefaultValue, type); }
                                catch (Exception ex) { warnings?.Add($"Pin默认值 [{sp.Name}] 加载失败 [{node.Name}]: {ex.Message}"); }
                            }
                        }
                        else
                        {
                            node.AddOutputPin(sp.Name, type);
                        }
                    }
                }

                sg.Nodes.Add(node);
            }

            foreach (var sc in s.Connections)
            {
                sg.Connections.Add(new NodeConnection
                {
                    Id = sc.Id,
                    FromNodeId = sc.FromNodeId,
                    FromPinName = sc.FromPinName,
                    ToNodeId = sc.ToNodeId,
                    ToPinName = sc.ToPinName,
                });
            }

            return sg;
        }

        private static SerializedNode SerializeNode(FlowNode node)
        {
            var props = new Dictionary<string, object>();
            foreach (var pd in node.GetNodeProperties())
            {
                var val = pd.Getter();
                if (val != null)
                {
                    var isDefault = pd.DefaultValue != null && val.Equals(pd.DefaultValue);
                    if (!isDefault)
                        props[pd.Name] = val;
                }
                else if (pd.DefaultValue != null)
                {
                    props[pd.Name] = val;
                }
            }

            var userPins = new List<SerializedPin>();
            foreach (var pin in node.Inputs.Where(p => p.UserDefined))
            {
                userPins.Add(new SerializedPin
                {
                    Name = pin.Name,
                    TypeName = pin.TypeDisplayName,
                    IsInput = true,
                    DefaultValue = (pin is InputPin inp) ? inp.DefaultValue : null,
                });
            }
            foreach (var pin in node.Outputs.Where(p => p.UserDefined))
            {
                userPins.Add(new SerializedPin
                {
                    Name = pin.Name,
                    TypeName = pin.TypeDisplayName,
                    IsInput = false,
                });
            }

            return new SerializedNode
            {
                Id = node.Id,
                NodeTypeName = node.GetType().FullName,
                Name = node.Name,
                NodeX = node.NodeX,
                NodeY = node.NodeY,
                Properties = props,
                UserPins = userPins.Count > 0 ? userPins : null,
            };
        }

        private static SerializedCameraSlot SerializeCameraSlot(CameraSlot slot)
        {
            return new SerializedCameraSlot
            {
                SlotId = slot.SlotId,
                SlotName = slot.SlotName,
                AssignedSerial = slot.AssignedSerial,
                Settings = slot.Settings,
            };
        }

        private static SerializedCameraDevice SerializeCameraDevice(CameraDeviceConfig device)
        {
            return new SerializedCameraDevice
            {
                DeviceId = device.DeviceId,
                DisplayName = device.DisplayName,
                AssignedSerial = device.AssignedSerial,
                Settings = device.Settings,
            };
        }

        private static SerializedSerialSlot SerializeSerialSlot(SerialSlot slot)
        {
            return new SerializedSerialSlot
            {
                SlotId = slot.SlotId,
                SlotName = slot.SlotName,
                PortName = slot.PortName,
                BaudRate = slot.BaudRate,
                DataBits = slot.DataBits,
                Parity = slot.Parity,
                StopBits = slot.StopBits,
            };
        }

        private static SerializedSerialDevice SerializeSerialDevice(SerialDeviceConfig device)
        {
            return new SerializedSerialDevice
            {
                DeviceId = device.DeviceId,
                DisplayName = device.DisplayName,
                PortName = device.PortName,
                BaudRate = device.BaudRate,
                DataBits = device.DataBits,
                Parity = device.Parity,
                StopBits = device.StopBits,
            };
        }

        private static SerializedTrigger SerializeTrigger(TriggerEntry trigger)
        {
            return new SerializedTrigger
            {
                Id = trigger.Id,
                Name = trigger.Name,
                SourceType = trigger.SourceType.ToString(),
                TargetSubGraphId = trigger.TargetSubGraphId,
                TargetSubGraphIds = new List<Guid>(trigger.GetTargetSubGraphIds()),
                Enabled = trigger.Enabled,
                CameraSlotId = trigger.CameraDeviceId,
                MaxConcurrent = trigger.MaxConcurrent,
                TimerIntervalMs = trigger.TimerIntervalMs,
                SerialSlotId = trigger.SerialDeviceId,
                MatchRule = trigger.MatchRule,
            };
        }

        private static TriggerEntry DeserializeTrigger(SerializedTrigger st)
        {
            var entry = new TriggerEntry
            {
                Id = st.Id,
                Name = st.Name ?? "新触发器",
                TargetSubGraphIds = st.TargetSubGraphIds != null && st.TargetSubGraphIds.Count > 0
                    ? new List<Guid>(st.TargetSubGraphIds)
                    : (st.TargetSubGraphId != Guid.Empty ? new List<Guid> { st.TargetSubGraphId } : new List<Guid>()),
                Enabled = st.Enabled,
                CameraDeviceId = st.CameraSlotId ?? "",
                MaxConcurrent = st.MaxConcurrent > 0 ? st.MaxConcurrent : 1,
                TimerIntervalMs = st.TimerIntervalMs > 0 ? st.TimerIntervalMs : 100,
                SerialDeviceId = st.SerialSlotId ?? "",
                MatchRule = st.MatchRule,
            };

            if (Enum.TryParse(st.SourceType, out TriggerSourceType sourceType))
                entry.SourceType = sourceType;

            return entry;
        }

        private class SerializedGraph
        {
            public SerializedProject Project;
            public string Name;
            public string Version;
            public DateTime CreatedAt;
            public List<SerializedSubGraph> SubGraphs;
            public List<SerializedCameraDevice> CameraDevices;
            public List<SerializedSerialDevice> SerialDevices;
            public List<SerializedCameraSlot> CameraSlots;
            public List<SerializedSerialSlot> SerialSlots;
            public List<SerializedTrigger> Triggers;
        }

        private class SerializedProject
        {
            public SerializedProjectMetadata Metadata;
            public SerializedProjectResources Resources;
            public SerializedProjectRouting Routing;
            public List<SerializedSubGraph> SubGraphs;
        }

        private class SerializedProjectMetadata
        {
            public string Name;
            public string Version;
            public DateTime CreatedAt;
        }

        private class SerializedProjectResources
        {
            public List<SerializedCameraDevice> CameraDevices;
            public List<SerializedSerialDevice> SerialDevices;
        }

        private class SerializedProjectRouting
        {
            public List<SerializedTrigger> Triggers;
        }

        private class SerializedSubGraph
        {
            public Guid Id;
            public string Name;
            public List<SerializedNode> Nodes;
            public List<SerializedConnection> Connections;
        }

        private class SerializedNode
        {
            public Guid Id;
            public string NodeTypeName;
            public string Name;
            public double NodeX;
            public double NodeY;
            public Dictionary<string, object> Properties;
            public List<SerializedPin> UserPins;
        }

        private class SerializedConnection
        {
            public Guid Id;
            public Guid FromNodeId;
            public string FromPinName;
            public Guid ToNodeId;
            public string ToPinName;
        }

        private class SerializedPin
        {
            public string Name;
            public string TypeName;
            public bool IsInput;
            public object DefaultValue;
        }

        private class SerializedCameraSlot
        {
            public string SlotId;
            public string SlotName;
            public string AssignedSerial;
            public CameraSettings Settings;
        }

        private class SerializedCameraDevice
        {
            public string DeviceId;
            public string DisplayName;
            public string AssignedSerial;
            public CameraSettings Settings;
        }

        private class SerializedSerialSlot
        {
            public string SlotId;
            public string SlotName;
            public string PortName;
            public int BaudRate;
            public int DataBits;
            public string Parity;
            public string StopBits;
        }

        private class SerializedSerialDevice
        {
            public string DeviceId;
            public string DisplayName;
            public string PortName;
            public int BaudRate;
            public int DataBits;
            public string Parity;
            public string StopBits;
        }

        private class SerializedTrigger
        {
            public Guid Id;
            public string Name;
            public string SourceType;
            public Guid TargetSubGraphId;
            public List<Guid> TargetSubGraphIds;
            public bool Enabled;
            public string CameraSlotId;
            public int MaxConcurrent;
            public int TimerIntervalMs;
            public string SerialSlotId;
            public Data.SerialTriggerRule MatchRule;
        }
    }
}
