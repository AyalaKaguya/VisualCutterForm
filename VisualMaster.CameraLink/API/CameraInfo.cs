namespace VisualMaster.CameraLink.Api
{
    public class CameraInfo
    {
        public string ModelName { get; set; }
        public string SerialNumber { get; set; }
        public string UserDefinedName { get; set; }
        public string ManufacturerName { get; set; }
        public string TransportTypeName { get; set; }
        public string AdapterName { get; set; }
        public uint TransportTypeRaw { get; set; }
        public uint IpAddress { get; set; }
        public string DeviceVersion { get; set; }
        public object RawInfo { get; set; }

        public override string ToString()
        {
            var label = string.IsNullOrEmpty(UserDefinedName) ? ModelName : UserDefinedName;
            return $"{label} [{SerialNumber}] ({TransportTypeName})";
        }
    }
}
