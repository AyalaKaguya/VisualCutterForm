using System.Collections.Generic;
using VisualMaster.Api;
using CameraDeviceConfig = VisualMaster.CameraLink.Api.CameraDeviceConfig;

namespace VisualMaster.WorkFlow
{
    public interface IDeviceConfigurationService
    {
        IReadOnlyList<CameraDeviceConfig> GetCameraDeviceConfigs();
        CameraDeviceConfig GetCameraDeviceConfig(string deviceId);
        IReadOnlyList<SerialDeviceConfig> GetSerialDeviceConfigs();
        SerialDeviceConfig GetSerialDeviceConfig(string deviceId);
    }
}