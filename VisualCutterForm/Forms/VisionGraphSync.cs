using System.Linq;
using VisualMaster.Api;
using VisualMaster.WorkFlow;
using CameraDeviceConfig = VisualMaster.CameraLink.Api.CameraDeviceConfig;

namespace VisualMaster.Forms
{
    internal static class VisionGraphSync
    {
        public static void SyncToGraph(FlowGraph graph, VisionCameraRuntime cameraRuntime, VisionSerialRuntime serialRuntime)
        {
            if (graph == null)
                return;

            graph.Project.Resources.CameraDevices.Clear();
            foreach (var device in cameraRuntime.GetCameraDeviceConfigs())
            {
                graph.Project.Resources.CameraDevices.Add(device.Clone());
            }

            graph.Project.Resources.SerialDevices.Clear();
            foreach (var device in serialRuntime.GetSerialDeviceConfigs())
            {
                graph.Project.Resources.SerialDevices.Add(device.Clone());
            }
        }

        public static void SyncFromGraph(FlowGraph graph, VisionCameraRuntime cameraRuntime, VisionSerialRuntime serialRuntime)
        {
            if (graph == null)
                return;

            var cameraConfigs = graph.GetCameraDevicesOrLegacy()
                .Select(device => device.Clone())
                .ToList();

            cameraRuntime.LoadCameraDevices(cameraConfigs);

            var serialConfigs = graph.GetSerialDevicesOrLegacy()
                .Select(device => device.Clone())
                .ToList();

            serialRuntime.LoadSerialDevices(serialConfigs);
        }
    }
}