using System;
using OpenCvSharp;

namespace VisualCutterForm.Lib.Flow
{
    public static class PinTypeResolver
    {
        public static Type Resolve(string name)
        {
            if (string.IsNullOrEmpty(name)) return typeof(object);

            switch (name)
            {
                case "Mat": return typeof(Mat);
                case "AcqResult": return typeof(Data.AcquisitionResult);
                case "double": return typeof(double);
                case "int": return typeof(int);
                case "bool": return typeof(bool);
                case "string": return typeof(string);
                case "Point2d": return typeof(Point2d);
                case "Bitmap": return typeof(System.Drawing.Bitmap);
                case "byte[]": return typeof(byte[]);
                default: return Type.GetType(name) ?? typeof(object);
            }
        }
    }
}
