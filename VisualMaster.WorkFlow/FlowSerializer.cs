using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using VisualMaster.Api;

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
            return new SerializedGraph
            {
                Name = graph.Name,
                Version = graph.Version,
                CreatedAt = graph.CreatedAt,
                SubGraphs = graph.SubGraphs.Select(SerializeSubGraph).ToList(),
                CameraSlots = graph.CameraSlots.Select(SerializeCameraSlot).ToList(),
            };
        }

        private static FlowGraph DeserializeGraph(SerializedGraph s, List<string> warnings = null)
        {
            var graph = new FlowGraph
            {
                Name = s.Name,
                Version = FlowGraph.CurrentVersion,
                CreatedAt = s.CreatedAt,
            };

            if (s.Version != FlowGraph.CurrentVersion)
                warnings?.Add($"流程版本从 {s.Version ?? "?"} 升级到 {FlowGraph.CurrentVersion}");

            if (s.CameraSlots != null)
            {
                foreach (var cs in s.CameraSlots)
                {
                    graph.CameraSlots.Add(new CameraSlot
                    {
                        SlotId = cs.SlotId,
                        SlotName = cs.SlotName,
                        AssignedSerial = cs.AssignedSerial,
                        Settings = cs.Settings ?? new CameraSettings(),
                        Fifo = new ImageFifo(cs.Settings?.FifoCapacity ?? 10),
                    });
                }
            }

            foreach (var sg in s.SubGraphs)
            {
                var subGraph = DeserializeSubGraph(sg, warnings);
                graph.SubGraphs.Add(subGraph);
            }

            graph.WireAllConnections();
            return graph;
        }

        private static SerializedSubGraph SerializeSubGraph(FlowSubGraph sg)
        {
            return new SerializedSubGraph
            {
                Id = sg.Id,
                Name = sg.Name,
                Trigger = sg.Trigger.ToString(),
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
                Trigger = Enum.TryParse(s.Trigger, out SubGraphTrigger trigger)
                    ? trigger : SubGraphTrigger.SoftManualTrigger,
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

        private class SerializedGraph
        {
            public string Name;
            public string Version;
            public DateTime CreatedAt;
            public List<SerializedSubGraph> SubGraphs;
            public List<SerializedCameraSlot> CameraSlots;
        }

        private class SerializedSubGraph
        {
            public Guid Id;
            public string Name;
            public string Trigger;
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
    }
}
