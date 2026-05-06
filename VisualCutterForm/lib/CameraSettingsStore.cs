using System.Collections.Generic;

namespace VisualCutterForm.Lib
{
    public class CameraSettingsStore
    {
        private readonly IniFile _ini;
        private const string SECTION_PREFIX = "Camera_";

        public CameraSettingsStore(IniFile ini)
        {
            _ini = ini;
        }

        public CameraSettings Load(string serialNumber)
        {
            var section = SectionName(serialNumber);
            var settings = new CameraSettings();

            settings.SerialNumber = _ini.Read(section, "SerialNumber", serialNumber);
            settings.ModelName = _ini.Read(section, "ModelName", "");

            settings.TriggerEnabled = _ini.ReadBool(section, "TriggerEnabled");
            settings.TriggerSource = _ini.Read(section, "TriggerSource", "Software");
            settings.TriggerActivation = _ini.Read(section, "TriggerActivation", "RisingEdge");

            settings.ExposureTimeUs = _ini.ReadFloat(section, "ExposureTimeUs", 5000f);
            settings.Gain = _ini.ReadFloat(section, "Gain", 0f);

            settings.Width = _ini.ReadInt(section, "Width", 0);
            settings.Height = _ini.ReadInt(section, "Height", 0);
            settings.OffsetX = _ini.ReadInt(section, "OffsetX", 0);
            settings.OffsetY = _ini.ReadInt(section, "OffsetY", 0);

            settings.FifiCapacity = _ini.ReadInt(section, "FifiCapacity", 10);

            return settings;
        }

        public void Save(CameraSettings settings)
        {
            var section = SectionName(settings.SerialNumber);

            _ini.Write(section, "SerialNumber", settings.SerialNumber ?? "");
            _ini.Write(section, "ModelName", settings.ModelName ?? "");

            _ini.WriteBool(section, "TriggerEnabled", settings.TriggerEnabled);
            _ini.Write(section, "TriggerSource", settings.TriggerSource ?? "Software");
            _ini.Write(section, "TriggerActivation", settings.TriggerActivation ?? "RisingEdge");

            _ini.WriteFloat(section, "ExposureTimeUs", settings.ExposureTimeUs);
            _ini.WriteFloat(section, "Gain", settings.Gain);

            _ini.WriteInt(section, "Width", settings.Width);
            _ini.WriteInt(section, "Height", settings.Height);
            _ini.WriteInt(section, "OffsetX", settings.OffsetX);
            _ini.WriteInt(section, "OffsetY", settings.OffsetY);

            _ini.WriteInt(section, "FifiCapacity", settings.FifiCapacity);
        }

        public List<string> ListSavedCameras()
        {
            var result = new List<string>();
            var allKeys = _ini.ReadSection(SECTION_PREFIX.TrimEnd('_'));
            foreach (var entry in allKeys)
            {
                if (!string.IsNullOrEmpty(entry))
                {
                    var kv = entry.Split('=');
                    if (kv.Length == 2 && kv[0].Trim() == "SerialNumber" && !string.IsNullOrEmpty(kv[1].Trim()))
                        result.Add(kv[1].Trim());
                }
            }
            return result;
        }

        public void DeleteSettings(string serialNumber)
        {
            _ini.DeleteSection(SectionName(serialNumber));
        }

        private static string SectionName(string serialNumber)
        {
            return SECTION_PREFIX + serialNumber;
        }
    }
}
