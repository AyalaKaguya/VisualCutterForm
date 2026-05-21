using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows;
using VisualMaster.Communication.Api;

namespace VisualMaster.Communication.TestApp
{
    internal sealed class CommunicationTestConfigFile
    {
        public int version { get; set; } = 1;
        public List<CommunicationDeviceConfig> devices { get; set; } = new List<CommunicationDeviceConfig>();
        public List<CommunicationInputEventConfig> inputEvents { get; set; } = new List<CommunicationInputEventConfig>();
        public List<CommunicationOutputEventConfig> outputEvents { get; set; } = new List<CommunicationOutputEventConfig>();
        public List<CommunicationHeartbeatConfig> heartbeats { get; set; } = new List<CommunicationHeartbeatConfig>();

        public List<CommunicationDeviceConfig> Devices
        {
            get => devices ?? (devices = new List<CommunicationDeviceConfig>());
            set => devices = value;
        }

        public List<CommunicationInputEventConfig> InputEvents
        {
            get => inputEvents ?? (inputEvents = new List<CommunicationInputEventConfig>());
            set => inputEvents = value;
        }

        public List<CommunicationOutputEventConfig> OutputEvents
        {
            get => outputEvents ?? (outputEvents = new List<CommunicationOutputEventConfig>());
            set => outputEvents = value;
        }

        public List<CommunicationHeartbeatConfig> Heartbeats
        {
            get => heartbeats ?? (heartbeats = new List<CommunicationHeartbeatConfig>());
            set => heartbeats = value;
        }

        public static CommunicationTestConfigFile Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return new CommunicationTestConfigFile();

            try
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                return new JavaScriptSerializer().Deserialize<CommunicationTestConfigFile>(json)
                    ?? new CommunicationTestConfigFile();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"读取通信配置失败，将使用空配置：{ex.Message}", "警告",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return new CommunicationTestConfigFile();
            }
        }

        public void Save(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = new JavaScriptSerializer().Serialize(this);
            File.WriteAllText(path, json, new UTF8Encoding(true));
        }
    }
}
