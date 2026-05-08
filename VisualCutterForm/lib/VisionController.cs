using VisualMaster.Api;
using VisualMaster.CameraLink;
using VisualMaster.Communication;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;

namespace VisualCutterForm.Lib
{
    public class VisionController : IDisposable
    {
        private readonly CameraManager _cameraManager;
        private readonly Dictionary<string, ISerialPort> _serialPorts;
        private volatile bool _disposed;

        public CameraManager CameraManager => _cameraManager;
        public IReadOnlyDictionary<string, ISerialPort> SerialPorts { get; }
        public bool IsInitialized { get; private set; }

        public event EventHandler<string> StatusChanged;
        public event EventHandler<Exception> ErrorOccurred;

        public VisionController(string configFilePath)
        {
            _cameraManager = new CameraManager();
            _serialPorts = new Dictionary<string, ISerialPort>();
            SerialPorts = new ReadOnlyDictionary<string, ISerialPort>(_serialPorts);
        }

        public VisionController(IniFile iniFile)
        {
            _cameraManager = new CameraManager();
            _serialPorts = new Dictionary<string, ISerialPort>();
            SerialPorts = new ReadOnlyDictionary<string, ISerialPort>(_serialPorts);
        }

        public ISerialPort SerialPort => _serialPorts.Values.FirstOrDefault();

        public void Initialize()
        {
            if (IsInitialized) return;
            IsInitialized = true;
            NotifyStatus("Vision system initialized.");
        }

        public int EnumerateCameras()
        {
            int count = _cameraManager.EnumerateCameras().Count;
            NotifyStatus($"Found {count} camera(s).");
            return count;
        }

        public CameraSettings GetSettings(string serialNumber)
        {
            var slot = _cameraManager.Slots.FirstOrDefault(s => s.AssignedSerial == serialNumber);
            return slot?.Settings;
        }

        public void SaveSettings(string serialNumber)
        {
            var slot = _cameraManager.Slots.FirstOrDefault(s => s.AssignedSerial == serialNumber);
            if (slot?.Settings != null)
            {
                slot.Settings.SerialNumber = serialNumber;
            }
        }

        public void SaveSettings(CameraSettings settings)
        {
            if (settings == null) return;
            var slot = _cameraManager.Slots.FirstOrDefault(s => s.AssignedSerial == settings.SerialNumber);
            if (slot != null)
                slot.Settings = settings;
        }

        public void SaveAllSettings()
        {
        }

        public ICamera OpenCamera(int index, CameraSettings settings = null)
        {
            var cameras = _cameraManager.DiscoveredCameras;
            if (index < 0 || index >= cameras.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var info = cameras[index];
            return OpenCameraByInfo(info, settings);
        }

        public ICamera OpenCameraByInfo(CameraInfo info, CameraSettings settings = null)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            var serial = info.SerialNumber;

            var existing = _cameraManager.Slots.FirstOrDefault(s => s.AssignedSerial == serial);
            if (existing != null)
            {
                if (!existing.IsConnected)
                    _cameraManager.OpenSlot(existing.SlotId, info);
                _cameraManager.StartGrabbing(existing.SlotId);
                NotifyStatus($"Camera re-opened: {serial}");
                return existing.Camera;
            }

            if (settings == null)
                settings = new CameraSettings { SerialNumber = serial, ModelName = info.ModelName };

            var slotName = string.IsNullOrEmpty(info.UserDefinedName) ? info.ModelName : info.UserDefinedName;
            var slot = _cameraManager.AddSlot(slotName, settings);
            _cameraManager.OpenSlot(slot.SlotId, info);
            _cameraManager.StartGrabbing(slot.SlotId);

            NotifyStatus($"Camera opened: {slotName}");
            return slot.Camera;
        }

        public void CloseCamera(string serialNumber)
        {
            var slot = _cameraManager.Slots.FirstOrDefault(s => s.AssignedSerial == serialNumber);
            if (slot != null)
                _cameraManager.CloseSlot(slot.SlotId);
            NotifyStatus($"Camera closed: {serialNumber}");
        }

        public void CloseAllCameras()
        {
            _cameraManager.CloseAllSlots();
            NotifyStatus("All cameras closed.");
        }

        public void StartAcquisition(string serialNumber)
        {
            _cameraManager.StartGrabbing(serialNumber);
        }

        public void StartAllAcquisitions()
        {
            foreach (var slot in _cameraManager.Slots)
                _cameraManager.StartGrabbing(slot.SlotId);
        }

        public void StopAcquisition(string serialNumber)
        {
            _cameraManager.StopGrabbing(serialNumber);
        }

        public void StopAllAcquisitions()
        {
            foreach (var slot in _cameraManager.Slots)
                _cameraManager.StopGrabbing(slot.SlotId);
        }

        public Bitmap TryDequeueFromFifo(string serialNumber, int timeoutMs = -1)
        {
            var slot = _cameraManager.Slots.FirstOrDefault(s => s.AssignedSerial == serialNumber);
            return slot?.Fifo?.TryDequeue(timeoutMs);
        }

        public Bitmap PeekLatestFromFifo(string serialNumber)
        {
            var slot = _cameraManager.Slots.FirstOrDefault(s => s.AssignedSerial == serialNumber);
            return slot?.Fifo?.PeekLatest();
        }

        public Bitmap PeekLatestNoClone(string serialNumber)
        {
            var slot = _cameraManager.Slots.FirstOrDefault(s => s.AssignedSerial == serialNumber);
            return slot?.Fifo?.PeekLatestNoClone();
        }

        public ImageFifo GetFifo(string serialNumber)
        {
            var slot = _cameraManager.Slots.FirstOrDefault(s => s.AssignedSerial == serialNumber);
            return slot?.Fifo;
        }

        public CameraSettings GetSlotSettings(string serialNumber)
        {
            var slot = _cameraManager.Slots.FirstOrDefault(s => s.AssignedSerial == serialNumber);
            return slot?.Settings;
        }

        public void UpdateSlotSettings(string serialNumber, CameraSettings settings)
        {
            var slot = _cameraManager.Slots.FirstOrDefault(s => s.AssignedSerial == serialNumber);
            if (slot == null) return;

            slot.Settings = settings?.Clone() as CameraSettings ?? slot.Settings;
            slot.Fifo.Capacity = slot.Settings.FifoCapacity;

            if (slot.Camera != null)
                slot.Camera.ApplySettings(slot.Settings);

            NotifyStatus($"Settings updated: {serialNumber}");
        }

        public string GetFirstActiveSerial()
        {
            var firstSlot = _cameraManager.Slots.FirstOrDefault(s => s.IsConnected);
            return firstSlot?.AssignedSerial;
        }

        public void ConnectSerial(string portName, int baudRate = 9600,
            int dataBits = 8, string parity = "None", string stopBits = "One")
        {
            if (_serialPorts.ContainsKey(portName))
            {
                NotifyStatus($"Serial port already connected: {portName}");
                return;
            }

            var sp = new SerialPortAdapter(portName, baudRate,
                ParseParity(parity), dataBits, ParseStopBits(stopBits));
            sp.ErrorOccurred += (s, e) => ErrorOccurred?.Invoke(this, e);
            sp.Open();

            _serialPorts[portName] = sp;
            NotifyStatus($"Serial port opened: {portName} @ {baudRate}");
        }

        public void DisconnectSerial(string portName)
        {
            if (!_serialPorts.TryGetValue(portName, out var sp))
                return;

            sp.Close();
            sp.Dispose();
            _serialPorts.Remove(portName);
            NotifyStatus($"Serial port disconnected: {portName}");
        }

        public void DisconnectAllSerials()
        {
            foreach (var name in new List<string>(_serialPorts.Keys))
            {
                DisconnectSerial(name);
            }
        }

        public bool IsSerialOpen(string portName)
        {
            return _serialPorts.TryGetValue(portName, out var sp) && sp.IsOpen;
        }

        public void OutputResult(string portName, string result)
        {
            if (string.IsNullOrEmpty(portName))
            {
                portName = _serialPorts.Keys.FirstOrDefault();
                if (portName == null)
                    throw new InvalidOperationException("No serial port connected.");
            }

            if (!_serialPorts.TryGetValue(portName, out var sp) || !sp.IsOpen)
                throw new InvalidOperationException($"Serial port {portName} is not connected.");
            sp.SendLine(result);
        }

        public void OutputResult(string portName, byte[] data)
        {
            if (string.IsNullOrEmpty(portName))
            {
                portName = _serialPorts.Keys.FirstOrDefault();
                if (portName == null)
                    throw new InvalidOperationException("No serial port connected.");
            }

            if (!_serialPorts.TryGetValue(portName, out var sp) || !sp.IsOpen)
                throw new InvalidOperationException($"Serial port {portName} is not connected.");
            sp.Send(data);
        }

        private static System.IO.Ports.Parity ParseParity(string parity)
        {
            switch (parity)
            {
                case "Even": return System.IO.Ports.Parity.Even;
                case "Odd": return System.IO.Ports.Parity.Odd;
                case "Mark": return System.IO.Ports.Parity.Mark;
                case "Space": return System.IO.Ports.Parity.Space;
                default: return System.IO.Ports.Parity.None;
            }
        }

        private static System.IO.Ports.StopBits ParseStopBits(string stopBits)
        {
            switch (stopBits)
            {
                case "Two": return System.IO.Ports.StopBits.Two;
                case "OnePointFive": return System.IO.Ports.StopBits.OnePointFive;
                default: return System.IO.Ports.StopBits.One;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cameraManager.CloseAllSlots();
            DisconnectAllSerials();
            _cameraManager.Dispose();
        }

        private void NotifyStatus(string message)
        {
            StatusChanged?.Invoke(this, message);
        }
    }
}
