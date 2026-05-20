using System.Drawing;
using VisualMaster.Api;

namespace VisualMaster.WorkFlow
{
    public interface ICameraRuntimeService
    {
        ImageFifo GetFifo(string deviceId);
        CameraFrameSnapshot PeekLatestFrameSnapshot(string deviceId);
        CameraFrameSnapshot WaitForNextFrameSnapshot(string deviceId, long afterSequenceNumber, int timeoutMs);
        long GetLatestFrameSequenceNumber(string deviceId);
        string GetFirstCameraDeviceId();
        string GetCameraDisplayName(string deviceId);
        string GetCameraAssignedSerial(string deviceId);
        bool IsCameraConnected(string deviceId);
        void TriggerSoftware(string deviceId);
        bool TryGrabImage(string deviceId, int timeoutMs, out Bitmap bitmap);
        Bitmap PeekLatestFromFifo(string deviceId);
        Bitmap PeekLatestNoClone(string deviceId);
        CameraSettings GetCameraSettings(string deviceId);
        void UpdateCameraSettings(string deviceId, CameraSettings settings);
    }
}