using System;
using System.Runtime.InteropServices;
using System.Text;

namespace VisualCutterForm.Lib
{
    public class IniFile
    {
        private readonly string _path;
        private readonly object _writeLock = new object();

        public IniFile(string path)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public string Read(string section, string key, string defaultValue = "")
        {
            var sb = new StringBuilder(4096);
            GetPrivateProfileString(section, key, defaultValue, sb, (uint)sb.Capacity, _path);
            return sb.ToString();
        }

        public int ReadInt(string section, string key, int defaultValue = 0)
        {
            int.TryParse(Read(section, key, defaultValue.ToString()), out int result);
            return result;
        }

        public float ReadFloat(string section, string key, float defaultValue = 0f)
        {
            float.TryParse(Read(section, key, defaultValue.ToString()), out float result);
            return result;
        }

        public bool ReadBool(string section, string key, bool defaultValue = false)
        {
            bool.TryParse(Read(section, key, defaultValue.ToString()), out bool result);
            return result;
        }

        public void Write(string section, string key, string value)
        {
            lock (_writeLock)
            {
                WritePrivateProfileString(section, key, value, _path);
            }
        }

        public void WriteInt(string section, string key, int value)
        {
            Write(section, key, value.ToString());
        }

        public void WriteFloat(string section, string key, float value)
        {
            Write(section, key, value.ToString());
        }

        public void WriteBool(string section, string key, bool value)
        {
            Write(section, key, value.ToString());
        }

        public string[] ReadSection(string section)
        {
            var buffer = new StringBuilder(32768);
            GetPrivateProfileSection(section, buffer, (uint)buffer.Capacity, _path);
            return buffer.ToString().Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public void DeleteKey(string section, string key)
        {
            lock (_writeLock)
            {
                WritePrivateProfileString(section, key, null, _path);
            }
        }

        public void DeleteSection(string section)
        {
            lock (_writeLock)
            {
                WritePrivateProfileString(section, null, null, _path);
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern uint GetPrivateProfileString(
            string lpAppName,
            string lpKeyName,
            string lpDefault,
            StringBuilder lpReturnedString,
            uint nSize,
            string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern uint GetPrivateProfileSection(
            string lpAppName,
            StringBuilder lpReturnedString,
            uint nSize,
            string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WritePrivateProfileString(
            string lpAppName,
            string lpKeyName,
            string lpString,
            string lpFileName);
    }
}
