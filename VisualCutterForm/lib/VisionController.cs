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
        private readonly Dictionary<string, CameraSlot> _slots;
        private readonly Dictionary<ICamera, string> _cameraToSerial = new Dictionary<ICamera, string>();
        private readonly Dictionary<string, ISerialPort> _serialPorts;
        private readonly CameraSettingsStore _settingsStore;
        private volatile bool _disposed;

        public CameraManager CameraManager => _cameraManager;
        public IReadOnlyDictionary<string, ISerialPort> SerialPorts { get; }
        public bool IsInitialized { get; private set; }
        public IReadOnlyDictionary<string, CameraSlot> Slots => _slots;
        public int CameraCount => _slots.Count;

        public event EventHandler<string> StatusChanged;
        public event EventHandler<Exception> ErrorOccurred;

        public VisionController(string configFilePath)
        {
            _cameraManager = new CameraManager();
            _slots = new Dictionary<string, CameraSlot>();
            _serialPorts = new Dictionary<string, ISerialPort>();
            SerialPorts = new ReadOnlyDictionary<string, ISerialPort>(_serialPorts);
            var iniFile = new IniFile(configFilePath);
            _settingsStore = new CameraSettingsStore(iniFile);
        }

        public VisionController(IniFile iniFile)
        {
            _cameraManager = new CameraManager();
            _slots = new Dictionary<string, CameraSlot>();
            _serialPorts = new Dictionary<string, ISerialPort>();
            SerialPorts = new ReadOnlyDictionary<string, ISerialPort>(_serialPorts);
            _settingsStore = new CameraSettingsStore(iniFile);
        }

        // ---- backward compat: first serial port ----
        public ISerialPort SerialPort => _serialPorts.Values.FirstOrDefault();

        public void Initialize()
        {
            if (IsInitialized) return;
            _cameraManager.Initialize();
            IsInitialized = true;
            NotifyStatus("Vision system initialized.");
        }

        public int EnumerateCameras()
        {
            if (!IsInitialized) Initialize();
            int count = _cameraManager.EnumerateDevices();
            NotifyStatus($"Found {count} camera(s).");
            return count;
        }

        public CameraSettings GetSettings(string serialNumber)
        {
            return _settingsStore.Load(serialNumber);
        }

        public void SaveSettings(string serialNumber)
        {
            if (_slots.TryGetValue(serialNumber, out var slot))
            {
                _settingsStore.Save(slot.Settings);
            }
        }

        public void SaveSettings(CameraSettings settings)
        {
            _settingsStore.Save(settings);
        }

        public void SaveAllSettings()
        {
            foreach (var slot in _slots.Values)
            {
                _settingsStore.Save(slot.Settings);
            }
        }

        public ICamera OpenCamera(int index, CameraSettings settings = null)
        {
            if (index < 0 || index >= _cameraManager.Cameras.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var info = _cameraManager.Cameras[index];
            return OpenCameraByInfo(info, settings);
        }

        public ICamera OpenCameraByInfo(CameraInfo info, CameraSettings settings = null)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            var serial = info.SerialNumber;
            if (string.IsNullOrEmpty(serial))
                serial = Guid.NewGuid().ToString();

            if (_slots.ContainsKey(serial))
                throw new InvalidOperationException($"Camera {serial} is already open.");

            if (settings == null)
                settings = _settingsStore.Load(serial);

            if (string.IsNullOrEmpty(settings.SerialNumber))
                settings.SerialNumber = serial;

            if (string.IsNullOrEmpty(settings.ModelName))
                settings.ModelName = info.ModelName;

            var camera = _cameraManager.OpenCamera(info);
            var fifo = new ImageFifo(settings.FifiCapacity);

            camera.ImageGrabbed += OnCameraImageGrabbed;

            var slot = new CameraSlot
            {
                Camera = camera,
                Fifo = fifo,
                Settings = settings,
                Info = info,
                IsGrabbing = false,
            };

            _slots[serial] = slot;
            _cameraToSerial[camera] = serial;

            ApplySettings(camera, settings);
            NotifyStatus($"Camera opened: {camera.Name}");
            return camera;
        }

        public void CloseCamera(string serialNumber)
        {
            if (!_slots.TryGetValue(serialNumber, out var slot))
                return;

            slot.Camera.StopGrabbing();
            slot.Camera.ImageGrabbed -= OnCameraImageGrabbed;
            slot.Camera.Dispose();

            _cameraToSerial.Remove(slot.Camera);
            slot.Fifo.Dispose();
            _slots.Remove(serialNumber);

            NotifyStatus($"Camera closed: {serialNumber}");
        }

        public void CloseAllCameras()
        {
            foreach (var key in new List<string>(_slots.Keys))
            {
                CloseCamera(key);
            }
        }

        public void StartAcquisition(string serialNumber)
        {
            if (!_slots.TryGetValue(serialNumber, out var slot))
                throw new InvalidOperationException($"Camera {serialNumber} is not open.");

            ApplySettings(slot.Camera, slot.Settings);
            slot.Camera.StartGrabbing();
            slot.IsGrabbing = true;
            NotifyStatus($"Acquisition started: {serialNumber}");
        }

        public void StartAllAcquisitions()
        {
            foreach (var key in _slots.Keys)
            {
                StartAcquisition(key);
            }
        }

        public void StopAcquisition(string serialNumber)
        {
            if (!_slots.TryGetValue(serialNumber, out var slot))
                return;

            slot.Camera.StopGrabbing();
            slot.IsGrabbing = false;
            NotifyStatus($"Acquisition stopped: {serialNumber}");
        }

        public void StopAllAcquisitions()
        {
            foreach (var key in _slots.Keys)
            {
                StopAcquisition(key);
            }
        }

        public Bitmap TryDequeueFromFifo(string serialNumber, int timeoutMs = -1)
        {
            if (!_slots.TryGetValue(serialNumber, out var slot))
                return null;
            return slot.Fifo.TryDequeue(timeoutMs);
        }

        public Bitmap PeekLatestFromFifo(string serialNumber)
        {
            if (!_slots.TryGetValue(serialNumber, out var slot))
                return null;
            return slot.Fifo.PeekLatest();
        }

        public ImageFifo GetFifo(string serialNumber)
        {
            if (_slots.TryGetValue(serialNumber, out var slot))
                return slot.Fifo;
            return null;
        }

        public CameraSettings GetSlotSettings(string serialNumber)
        {
            if (_slots.TryGetValue(serialNumber, out var slot))
                return slot.Settings;
            return null;
        }

        public void UpdateSlotSettings(string serialNumber, CameraSettings settings)
        {
            if (!_slots.TryGetValue(serialNumber, out var slot))
                return;

            var fifoCapacityChanged = slot.Settings.FifiCapacity != settings.FifiCapacity;
            slot.Settings = settings.Clone() as CameraSettings;
            slot.Fifo.Capacity = settings.FifiCapacity;

            ApplySettings(slot.Camera, slot.Settings);
            _settingsStore.Save(settings);
            NotifyStatus($"Settings updated: {serialNumber}");
        }

        // ---- Multi-serial port ----

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

        public string GetFirstActiveSerial()
        {
            foreach (var kv in _slots)
            {
                return kv.Key;
            }
            return null;
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

        private void OnCameraImageGrabbed(object sender, Bitmap bitmap)
        {
            if (_cameraToSerial.TryGetValue((ICamera)sender, out var serial)
                && _slots.TryGetValue(serial, out var slot))
            {
                slot.Fifo.Enqueue(bitmap);
            }
        }

        private static void ApplySettings(ICamera camera, CameraSettings settings)
        {
            if (camera is MvsCamera mvs)
            {
                mvs.ApplySettings(settings);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            CloseAllCameras();
            DisconnectAllSerials();
            _cameraManager.Dispose();
        }

        private void NotifyStatus(string message)
        {
            StatusChanged?.Invoke(this, message);
        }

        public class CameraSlot
        {
            public ICamera Camera { get; set; }
            public ImageFifo Fifo { get; set; }
            public CameraSettings Settings { get; set; }
            public CameraInfo Info { get; set; }
            public bool IsGrabbing { get; set; }
        }
    }
}
