using VisualMaster.Api;
using VisualMaster.CameraLink;
using System;
using System.IO;
using System.Threading;

namespace VisualCutterForm.Lib
{
    public class AppConfig
    {
        private readonly IniFile _ini;
        private CameraSettingsStore _cameraStore;
        private Timer _debounceTimer;

        private const string SECTION_GENERAL = "General";
        private const string SECTION_SERIAL = "Serial";
        private const string SECTION_FLOW = "Flow";
        private const string SECTION_LOGIN = "Login";

        public string FilePath { get; }
        public CameraSettingsStore CameraStore => _cameraStore;

        private const string DEFAULT_ADMIN_PASSWORD = "admin";
        private const string DEFAULT_ENG_PASSWORD = "engineer";
        private const string DEFAULT_USER_PASSWORD = "user";

        public AppConfig(string filePath)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _ini = new IniFile(filePath);
            _cameraStore = new CameraSettingsStore(_ini);
        }

        public void Load()
        {
            if (!File.Exists(FilePath))
                Save();
        }

        public void Save()
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            _ini.Write(SECTION_GENERAL, "LastCameraSerial", LastCameraSerial ?? "");
            _ini.Write(SECTION_GENERAL, "LastCameraModel", LastCameraModel ?? "");
            _ini.WriteInt(SECTION_GENERAL, "LastCameraIndex", LastCameraIndex);
            _ini.Write(SECTION_GENERAL, "LastCameraList", LastCameraList ?? "");
            _ini.WriteInt(SECTION_GENERAL, "AutoStart", AutoStart ? 1 : 0);

            _ini.Write(SECTION_SERIAL, "PortName", SerialPortName ?? "");
            _ini.WriteInt(SECTION_SERIAL, "BaudRate", SerialBaudRate);
            _ini.WriteInt(SECTION_SERIAL, "DataBits", SerialDataBits);
            _ini.Write(SECTION_SERIAL, "Parity", SerialParity);
            _ini.Write(SECTION_SERIAL, "StopBits", SerialStopBits);

            _ini.Write(SECTION_FLOW, "FlowFile", FlowFilePath ?? "");
            _ini.WriteInt(SECTION_FLOW, "AutoRun", AutoRunFlow ? 1 : 0);
            _ini.Write(SECTION_FLOW, "ViewSource", ViewSource ?? "");
            _ini.Write(SECTION_FLOW, "ViewNode", ViewNode ?? "");

            _ini.Write(SECTION_LOGIN, "AdminPassword", AdminPassword);
            _ini.Write(SECTION_LOGIN, "EngineerPassword", EngineerPassword);
            _ini.Write(SECTION_LOGIN, "UserPassword", UserPassword);
            _ini.WriteInt(SECTION_LOGIN, "DefaultRole", (int)DefaultRole);
        }

        public void SaveDebounced(int delayMs = 300)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(_ =>
            {
                _debounceTimer?.Dispose();
                _debounceTimer = null;
                Save();
            }, null, delayMs, Timeout.Infinite);
        }

        public void Reload()
        {
            LastCameraSerial = _ini.Read(SECTION_GENERAL, "LastSerial", "");
            LastCameraModel = _ini.Read(SECTION_GENERAL, "LastModel", "");
            LastCameraIndex = _ini.ReadInt(SECTION_GENERAL, "LastIndex", -1);
            LastCameraList = _ini.Read(SECTION_GENERAL, "LastCameraList", "");
            AutoStart = _ini.ReadInt(SECTION_GENERAL, "AutoStart", 0) == 1;

            SerialPortName = _ini.Read(SECTION_SERIAL, "PortName", "COM1");
            SerialBaudRate = _ini.ReadInt(SECTION_SERIAL, "BaudRate", 9600);
            SerialDataBits = _ini.ReadInt(SECTION_SERIAL, "DataBits", 8);
            SerialParity = _ini.Read(SECTION_SERIAL, "Parity", "None");
            SerialStopBits = _ini.Read(SECTION_SERIAL, "StopBits", "One");

            FlowFilePath = _ini.Read(SECTION_FLOW, "FlowFile", "");
            AutoRunFlow = _ini.ReadInt(SECTION_FLOW, "AutoRun", 0) == 1;
            ViewSource = _ini.Read(SECTION_FLOW, "ViewSource", "");
            ViewNode = _ini.Read(SECTION_FLOW, "ViewNode", "");

            AdminPassword = _ini.Read(SECTION_LOGIN, "AdminPassword", DEFAULT_ADMIN_PASSWORD);
            EngineerPassword = _ini.Read(SECTION_LOGIN, "EngineerPassword", DEFAULT_ENG_PASSWORD);
            UserPassword = _ini.Read(SECTION_LOGIN, "UserPassword", DEFAULT_USER_PASSWORD);
            DefaultRole = (UserRole)_ini.ReadInt(SECTION_LOGIN, "DefaultRole", 0);
        }

        public string LastCameraSerial { get; set; }
        public string LastCameraModel { get; set; }
        public int LastCameraIndex { get; set; } = -1;
        public string LastCameraList { get; set; } = "";
        public bool AutoStart { get; set; }

        public string SerialPortName { get; set; } = "COM1";
        public int SerialBaudRate { get; set; } = 9600;
        public int SerialDataBits { get; set; } = 8;
        public string SerialParity { get; set; } = "None";
        public string SerialStopBits { get; set; } = "One";

        public string FlowFilePath { get; set; } = "";
        public bool AutoRunFlow { get; set; }
        public string ViewSource { get; set; } = "";
        public string ViewNode { get; set; } = "";

        public string AdminPassword { get; set; } = DEFAULT_ADMIN_PASSWORD;
        public string EngineerPassword { get; set; } = DEFAULT_ENG_PASSWORD;
        public string UserPassword { get; set; } = DEFAULT_USER_PASSWORD;
        public UserRole DefaultRole { get; set; } = UserRole.None;

        public UserRole VerifyPassword(string password)
        {
            if (password == AdminPassword) return UserRole.Admin;
            if (password == EngineerPassword) return UserRole.Engineer;
            if (password == UserPassword) return UserRole.Operator;
            return UserRole.None;
        }

        public static AppConfig CreateDefault(string directory)
        {
            var path = Path.Combine(directory, "config.ini");
            var config = new AppConfig(path);
            config.Load();
            config.Reload();
            return config;
        }
    }
}
