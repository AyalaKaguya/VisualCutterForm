using VisualMaster.Api;
using VisualMaster.CameraLink;
using VisualMaster.Communication;
using VisualMaster.WorkFlow;
using VisualMaster.WorkFlow.Data;
using VisualCutterForm.Legacy;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;

namespace VisualMaster.Forms
{
    public class VisionController : IDisposable, IFlowServiceProvider
    {
        private readonly VisionCameraRuntime _cameraRuntime;
        private readonly VisionSerialRuntime _serialRuntime;
        private volatile bool _disposed;

        public CameraManager CameraManager => _cameraRuntime.CameraManager;
        public RuntimeDiagnosticsHub RuntimeDiagnostics { get; }
        public IReadOnlyDictionary<string, ISerialPort> SerialPorts => _serialRuntime.SerialPorts;
        public bool IsInitialized => _cameraRuntime.IsInitialized;

        public event EventHandler<string> StatusChanged;
        public event EventHandler<Exception> ErrorOccurred;

        public VisionController()
        {
            RuntimeDiagnostics = new RuntimeDiagnosticsHub();
            _cameraRuntime = new VisionCameraRuntime(new CameraManager());
            _serialRuntime = new VisionSerialRuntime(ex => ErrorOccurred?.Invoke(this, ex));
            _cameraRuntime.CameraManager.Diagnostics = RuntimeDiagnostics;
        }

        public ISerialPort SerialPort => _serialRuntime.SerialPort;

        public void Initialize()
        {
            if (IsInitialized) return;
            _cameraRuntime.Initialize();
            NotifyStatus("Vision system initialized.");
        }

        public int EnumerateCameras()
        {
            int count = _cameraRuntime.EnumerateCameras();
            NotifyStatus($"Found {count} camera(s).");
            return count;
        }

        public IReadOnlyList<CameraInfo> GetDiscoveredCameras()
        {
            return _cameraRuntime.GetDiscoveredCameras();
        }

        public CameraDeviceConfig AddCameraDevice(string displayName, CameraSettings settings = null)
        {
            return _cameraRuntime.AddCameraDevice(displayName, settings);
        }

        public void RemoveCameraDevice(string deviceId)
        {
            _cameraRuntime.RemoveCameraDevice(deviceId);
        }

        public void OpenSlot(string slotId, CameraInfo info)
        {
            _cameraRuntime.OpenSlot(slotId, info);
            NotifyStatus($"Camera slot opened: {slotId}");
        }

        public void CloseSlot(string slotId)
        {
            _cameraRuntime.CloseSlot(slotId);
        }

        public void StartAcquisition(string slotId)
        {
            _cameraRuntime.StartAcquisition(slotId);
            NotifyStatus($"Acquisition started: {slotId}");
        }

        public void StopAcquisition(string slotId)
        {
            _cameraRuntime.StopAcquisition(slotId);
        }

        public void StartAllAcquisitions()
        {
            _cameraRuntime.StartAllAcquisitions();
        }

        public void StopAllAcquisitions()
        {
            _cameraRuntime.StopAllAcquisitions();
        }

        public ImageFifo GetFifo(string slotId)
        {
            return _cameraRuntime.GetFifo(slotId);
        }

        public CameraFrameSnapshot PeekLatestFrameSnapshot(string slotId)
        {
            return _cameraRuntime.PeekLatestFrameSnapshot(slotId);
        }

        public CameraFrameSnapshot WaitForNextFrameSnapshot(string slotId, long afterSequenceNumber, int timeoutMs)
        {
            return _cameraRuntime.WaitForNextFrameSnapshot(slotId, afterSequenceNumber, timeoutMs);
        }

        public long GetLatestFrameSequenceNumber(string slotId)
        {
            return _cameraRuntime.GetLatestFrameSequenceNumber(slotId);
        }

        public IReadOnlyList<CameraDeviceConfig> GetCameraDeviceConfigs()
        {
            return _cameraRuntime.GetCameraDeviceConfigs();
        }

        public CameraDeviceConfig GetCameraDeviceConfig(string deviceId)
        {
            return _cameraRuntime.GetCameraDeviceConfig(deviceId);
        }

        public CameraDeviceStatus GetCameraDeviceStatus(string deviceId)
        {
            return _cameraRuntime.GetDeviceStatuses().FirstOrDefault(s => s.DeviceId == deviceId);
        }

        public IReadOnlyList<CameraDeviceStatus> GetAllCameraDeviceStatuses()
        {
            return _cameraRuntime.GetDeviceStatuses();
        }

        public string GetFirstCameraDeviceId()
        {
            return _cameraRuntime.GetFirstDeviceId();
        }

        public string GetCameraDisplayName(string slotId)
        {
            return _cameraRuntime.GetDeviceDisplayName(slotId);
        }

        public void UpdateCameraDeviceDisplayName(string deviceId, string displayName)
        {
            _cameraRuntime.UpdateCameraDeviceDisplayName(deviceId, displayName);
        }

        public string GetCameraAssignedSerial(string slotId)
        {
            return _cameraRuntime.GetAssignedSerial(slotId);
        }

        public bool IsCameraConnected(string slotId)
        {
            return _cameraRuntime.IsCameraConnected(slotId);
        }

        public void TriggerSoftware(string slotId)
        {
            _cameraRuntime.TriggerSoftware(slotId);
        }

        public bool IsCameraGrabbing(string slotId)
        {
            return _cameraRuntime.IsCameraGrabbing(slotId);
        }

        public CameraInfo GetAssignedCameraInfo(string slotId)
        {
            return _cameraRuntime.GetAssignedCameraInfo(slotId);
        }

        public bool TryGrabImage(string slotId, int timeoutMs, out Bitmap bitmap)
        {
            return _cameraRuntime.TryGrabImage(slotId, timeoutMs, out bitmap);
        }

        public string[] GetAvailablePixelFormats(string deviceId)
        {
            return _cameraRuntime.GetAvailablePixelFormats(deviceId);
        }

        public Bitmap TryDequeueFromFifo(string slotId, int timeoutMs = -1)
        {
            return _cameraRuntime.TryDequeueFromFifo(slotId, timeoutMs);
        }

        public SerialSlot GetSerialSlot(string slotId)
        {
            return _serialRuntime.GetSerialSlot(slotId);
        }

        public SerialDeviceConfig AddSerialDevice(string displayName, string portName = null)
        {
            return _serialRuntime.AddSerialDevice(displayName, portName);
        }

        public void UpdateSerialDevice(SerialDeviceConfig config)
        {
            _serialRuntime.UpdateSerialDevice(config);
        }

        public void RemoveSerialDevice(string deviceId)
        {
            _serialRuntime.RemoveSerialDevice(deviceId);
        }

        public bool IsSerialDeviceConnected(string deviceId)
        {
            return _serialRuntime.IsSerialDeviceConnected(deviceId);
        }

        public void ConnectSerialDevice(string deviceId)
        {
            _serialRuntime.ConnectSerialDevice(deviceId);
        }

        public void DisconnectSerialDevice(string deviceId)
        {
            _serialRuntime.DisconnectSerialDevice(deviceId);
        }

        public string GetSerialPortName(string slotId)
        {
            return _serialRuntime.GetSerialPortName(slotId);
        }

        public int GetSerialBaudRate(string slotId)
        {
            return _serialRuntime.GetSerialBaudRate(slotId);
        }

        public IReadOnlyList<SerialDeviceConfig> GetSerialDeviceConfigs()
        {
            return _serialRuntime.GetSerialDeviceConfigs();
        }

        public SerialDeviceConfig GetSerialDeviceConfig(string deviceId)
        {
            return _serialRuntime.GetSerialDeviceConfig(deviceId);
        }

        public List<SerialSlot> GetSerialSlots()
        {
            return _serialRuntime.GetSerialSlots();
        }

        public Bitmap PeekLatestFromFifo(string slotId)
        {
            return _cameraRuntime.PeekLatestFromFifo(slotId);
        }

        public Bitmap PeekLatestNoClone(string slotId)
        {
            return _cameraRuntime.PeekLatestNoClone(slotId);
        }

        public CameraSettings GetCameraSettings(string slotId)
        {
            return _cameraRuntime.GetCameraSettings(slotId);
        }

        public void UpdateCameraSettings(string slotId, CameraSettings settings)
        {
            _cameraRuntime.UpdateCameraSettings(slotId, settings);

            NotifyStatus($"Settings updated: {slotId}");
        }

        public void ConnectSerial(string portName, int baudRate = 9600,
            int dataBits = 8, string parity = "None", string stopBits = "One")
        {
            if (_serialRuntime.IsSerialOpen(portName))
            {
                NotifyStatus($"Serial port already connected: {portName}");
                return;
            }

            _serialRuntime.ConnectSerial(portName, baudRate, dataBits, parity, stopBits);
            NotifyStatus($"Serial port opened: {portName} @ {baudRate}");
        }

        void ISerialRuntimeService.ConnectSerial(string portName, int baudRate)
        {
            ConnectSerial(portName, baudRate);
        }

        public void DisconnectSerial(string portName)
        {
            if (!_serialRuntime.IsSerialOpen(portName))
                return;

            _serialRuntime.DisconnectSerial(portName);
            NotifyStatus($"Serial port disconnected: {portName}");
        }

        public void DisconnectAllSerials()
        {
            foreach (var name in SerialPorts.Keys.ToList())
                DisconnectSerial(name);
        }

        public bool IsSerialOpen(string portName)
        {
            return _serialRuntime.IsSerialOpen(portName);
        }

        public void OutputResult(string portName, string result)
        {
            _serialRuntime.OutputResult(portName, result);
        }

        public void OutputResult(string portName, byte[] data)
        {
            _serialRuntime.OutputResult(portName, data);
        }

        void ISerialRuntimeService.OutputResult(string portName, object data)
        {
            _serialRuntime.OutputResult(portName, data);
        }

        public void SyncToGraph(FlowGraph graph)
        {
            VisionGraphSync.SyncToGraph(graph, _cameraRuntime, _serialRuntime);
        }

        public void SyncFromGraph(FlowGraph graph)
        {
            VisionGraphSync.SyncFromGraph(graph, _cameraRuntime, _serialRuntime);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _serialRuntime.Dispose();
            CameraManager.Dispose();
        }

        private void NotifyStatus(string message)
        {
            StatusChanged?.Invoke(this, message);
        }
    }
}
