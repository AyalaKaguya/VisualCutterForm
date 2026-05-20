using VisualMaster.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace VisualMaster.CameraLink.App
{
    /// <summary>
    /// 相机管理独立应用的配置文件模型（JSON 格式）。
    /// 字段名小写以匹配 JavaScriptSerializer 默认序列化。
    /// </summary>
    internal sealed class CameraConfigFile
    {
        /// <summary>已配置的相机设备列表。</summary>
        public List<CameraDeviceConfig> cameras { get; set; } = new List<CameraDeviceConfig>();

        /// <summary>文件格式版本号。</summary>
        public int version { get; set; } = 1;

        /// <summary>Pascal-case 便捷访问。</summary>
        public List<CameraDeviceConfig> Cameras
        {
            get => cameras ?? (cameras = new List<CameraDeviceConfig>());
            set => cameras = value;
        }

        // ── I/O ──────────────────────────────────────────────────────

        /// <summary>
        /// 从文件加载配置。文件不存在时返回空配置，不抛出异常。
        /// </summary>
        public static CameraConfigFile Load(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return new CameraConfigFile();

            try
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                var js = new JavaScriptSerializer();
                return js.Deserialize<CameraConfigFile>(json) ?? new CameraConfigFile();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"读取配置文件失败，将使用空配置：\n{ex.Message}",
                    "警告", System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return new CameraConfigFile();
            }
        }

        /// <summary>
        /// 将配置保存到文件（UTF-8 with BOM）。
        /// </summary>
        public void Save(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var js = new JavaScriptSerializer();
            var json = js.Serialize(this);
            File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }
    }
}

