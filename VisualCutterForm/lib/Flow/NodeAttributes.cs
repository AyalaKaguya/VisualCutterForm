using System;

namespace VisualCutterForm.Lib.Flow
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class NodeInputAttribute : Attribute
    {
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public bool Required { get; set; }

        public NodeInputAttribute(string displayName = null)
        {
            DisplayName = displayName;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class NodeOutputAttribute : Attribute
    {
        public string DisplayName { get; set; }
        public string Description { get; set; }

        public NodeOutputAttribute(string displayName = null)
        {
            DisplayName = displayName;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class NodePropertyAttribute : Attribute
    {
        public string DisplayName { get; set; }
        public string Category { get; set; }
        public object DefaultValue { get; set; }
        public double Min { get; set; } = double.MinValue;
        public double Max { get; set; } = double.MaxValue;
        public string Description { get; set; }

        public NodePropertyAttribute(string displayName = null)
        {
            DisplayName = displayName;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class NodeCategoryAttribute : Attribute
    {
        public string Category { get; set; }
        public string DisplayName { get; set; }

        public NodeCategoryAttribute(string category, string displayName = null)
        {
            Category = category;
            DisplayName = displayName;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class NodeBackgroundAttribute : Attribute
    {
        public string Description { get; set; }
    }
}
