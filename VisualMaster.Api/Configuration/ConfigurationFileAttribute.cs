using System;

namespace VisualMaster.Api.Configuration
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ConfigurationFileAttribute : Attribute
    {
        public ConfigurationFileAttribute(string relativePath)
        {
            RelativePath = relativePath;
        }

        public string RelativePath { get; }
    }
}
