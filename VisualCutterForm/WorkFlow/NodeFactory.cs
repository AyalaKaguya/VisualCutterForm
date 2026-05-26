using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace VisualMaster.WorkFlow
{
    public class NodeTypeInfo
    {
        public Type NodeType { get; set; }
        public string Category { get; set; }
        public string DisplayName { get; set; }
        public bool IsBackground { get; set; }

        public override string ToString()
        {
            return $"[{Category}] {DisplayName}";
        }
    }

    public static class NodeFactory
    {
        private static volatile Lazy<List<NodeTypeInfo>> _cachedTypes = CreateLazy();

        private static Lazy<List<NodeTypeInfo>> CreateLazy()
        {
            return new Lazy<List<NodeTypeInfo>>(DiscoverNodeTypes, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public static List<NodeTypeInfo> GetAvailableNodeTypes()
        {
            return _cachedTypes.Value;
        }

        private static List<NodeTypeInfo> DiscoverNodeTypes()
        {
            var list = new List<NodeTypeInfo>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var asm in assemblies)
            {
                try
                {
                    var types = asm.GetTypes()
                        .Where(t => t.IsSubclassOf(typeof(FlowNode)) && !t.IsAbstract);

                    foreach (var t in types)
                    {
                        var catAttr = t.GetCustomAttribute<NodeCategoryAttribute>();
                        var bgAttr = t.GetCustomAttribute<NodeBackgroundAttribute>();

                        list.Add(new NodeTypeInfo
                        {
                            NodeType = t,
                            Category = catAttr?.Category ?? "通用",
                            DisplayName = catAttr?.DisplayName ?? t.Name,
                            IsBackground = bgAttr != null,
                        });
                    }
                }
                catch
                {
                    // skip assemblies that fail to load
                }
            }

            return list;
        }

        public static FlowNode CreateNode(Type nodeType)
        {
            if (nodeType == null) throw new ArgumentNullException(nameof(nodeType));
            if (!typeof(FlowNode).IsAssignableFrom(nodeType))
                throw new ArgumentException($"Type {nodeType.Name} is not a FlowNode.");

            return (FlowNode)Activator.CreateInstance(nodeType);
        }

        public static FlowNode CreateNode(string typeName)
        {
            var types = GetAvailableNodeTypes();
            var info = types.FirstOrDefault(t =>
                t.NodeType.Name == typeName || t.NodeType.FullName == typeName);

            if (info == null)
                throw new ArgumentException($"Node type '{typeName}' not found.");

            return CreateNode(info.NodeType);
        }

        public static void ResetCache()
        {
            _cachedTypes = CreateLazy();
        }
    }
}
