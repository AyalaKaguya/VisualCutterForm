using VisualMaster.Api;
using VisualMaster.CameraLink;
using VisualMaster.Communication;
using VisualMaster.WorkFlow;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;

namespace VisualMaster.Forms
{
    public class VisionController : IDisposable
    {
        private readonly CameraManager _cameraManager;
        private readonly Dictionary<string, ISerialPort> _serialPorts;
        private volatile bool _disposed;

        public CameraManager CameraManager => _cameraManager;
        public IReadOnlyDictionary<string, ISerialPort> SerialPorts { get; }
        public bool IsInitialized => _cameraManager.IsInitialized;

        public event EventHandler<string> StatusChanged;
        public event EventHandler<Exception> ErrorOccurred;

        public VisionController()
        {
            _cameraManager = new CameraManager();
            _serialPorts = new Dictionary<string, ISerialPort>();
            SerialPorts = new ReadOnlyDictionary<string, ISerialPort>(_serialPorts);
        }

        public ISerialPort SerialPort => _serialPorts.Values.FirstOrDefault();

        public void Initialize()
        {
            if (IsInitialized) return;
            _cameraManager.Initialize();
            NotifyStatus("Vision system initialized.");
        }

        public int EnumerateCameras()
        {
            if (!IsInitialized) Initialize();
            int count = _cameraManager.EnumerateCameras().Count;
            NotifyStatus($"Found {count} camera(s).");
            return count;
        }

        public CameraSlot AddSlot(string name, CameraSettings settings = null)
        {
            return _cameraManager.AddSlot(name, settings);
        }

        public void RemoveSlot(string slotId)
        {
            _cameraManager.RemoveSlot(slotId);
        }

        public void OpenSlot(string slotId, CameraInfo info)
        {
            _cameraManager.OpenSlot(slotId, info);
            NotifyStatus($"Camera slot opened: {slotId}");
        }

        public void CloseSlot(string slotId)
        {
            _cameraManager.CloseSlot(slotId);
        }

        public void StartAcquisition(string slotId)
        {
            _cameraManager.StartGrabbing(slotId);
            NotifyStatus($"Acquisition started: {slotId}");
        }

        public void StopAcquisition(string slotId)
        {
            _cameraManager.StopGrabbing(slotId);
        }

        public void StartAllAcquisitions()
        {
            foreach (var slot in _cameraManager.Slots)
            {
                if (slot.IsConnected)
                    _cameraManager.StartGrabbing(slot.SlotId);
            }
        }

        public void StopAllAcquisitions()
        {
            foreach (var slot in _cameraManager.Slots)
            {
                _cameraManager.StopGrabbing(slot.SlotId);
            }
        }

        public ImageFifo GetFifo(string slotId)
        {
            var slot = _cameraManager.Slots.FirstOrDefault(s => s.SlotId == slotId);
            return slot?.Fifo;
        }

        public CameraSlot GetSlotById(string slotId)
        {
            return _cameraManager.Slots.FirstOrDefault(s => s.SlotId == slotId);
        }

        public CameraSlot GetFirstSlot()
        {
            return _cameraManager.Slots.Count > 0 ? _cameraManager.Slots[0] : null;
        }

        public string GetFirstSlotId()
        {
            var s = GetFirstSlot();
            return s?.SlotId;
        }

        public string GetFirstActiveSerial()
        {
            foreach (var slot in _cameraManager.Slots)
            {
                if (slot.IsConnected)
                    return slot.AssignedSerial;
            }
            return null;
        }

        public Bitmap TryDequeueFromFifo(string slotId, int timeoutMs = -1)
        {
            var fifo = GetFifo(slotId);
            return fifo?.TryDequeue(timeoutMs);
        }

        public Bitmap PeekLatestFromFifo(string slotId)
        {
            var fifo = GetFifo(slotId);
            return fifo?.PeekLatest();
        }

        public Bitmap PeekLatestNoClone(string slotId)
        {
            var fifo = GetFifo(slotId);
            return fifo?.PeekLatestNoClone();
        }

        public CameraSettings GetSlotSettings(string slotId)
        {
            var slot = GetSlotById(slotId);
            return slot?.Settings;
        }

        public void UpdateSlotSettings(string slotId, CameraSettings settings)
        {
            var slot = GetSlotById(slotId);
            if (slot == null) return;

            slot.Settings = settings.Clone() as CameraSettings;
            slot.Fifo.Capacity = settings.FifoCapacity;

            if (slot.Camera != null)
                slot.Camera.ApplySettings(settings);

            NotifyStatus($"Settings updated: {slotId}");
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

        public void SyncToGraph(FlowGraph graph)
        {
            if (graph == null) return;
            graph.CameraSlots.Clear();
            foreach (var slot in _cameraManager.Slots)
            {
                graph.CameraSlots.Add(new CameraSlot
                {
                    SlotId = slot.SlotId,
                    SlotName = slot.SlotName,
                    Settings = slot.Settings?.Clone() as CameraSettings ?? new CameraSettings(),
                    AssignedSerial = slot.AssignedSerial,
                    Fifo = new ImageFifo(slot.Settings?.FifoCapacity ?? 10),
                });
            }
        }

        public void SyncFromGraph(FlowGraph graph)
        {
            if (graph == null) return;

            foreach (var slot in _cameraManager.Slots.ToList())
                _cameraManager.RemoveSlot(slot.SlotId);

            foreach (var cs in graph.CameraSlots)
            {
                var slot = _cameraManager.AddSlot(cs.SlotName ?? "相机", cs.Settings?.Clone() as CameraSettings ?? new CameraSettings());
                slot.SlotId = cs.SlotId;
                slot.SlotName = cs.SlotName;
                slot.AssignedSerial = cs.AssignedSerial;

                if (!string.IsNullOrEmpty(cs.AssignedSerial))
                {
                    var cameras = _cameraManager.Cameras;
                    if (cameras.Count == 0) _cameraManager.EnumerateCameras();
                    var info = _cameraManager.Cameras.FirstOrDefault(c => c.SerialNumber == cs.AssignedSerial);
                    if (info != null)
                    {
                        try { _cameraManager.OpenSlot(slot.SlotId, info); }
                        catch { }
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            DisconnectAllSerials();
            _cameraManager.Dispose();
        }

        private void NotifyStatus(string message)
        {
            StatusChanged?.Invoke(this, message);
        }
    }
}
