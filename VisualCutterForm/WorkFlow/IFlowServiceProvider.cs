using VisualMaster.Api;

namespace VisualMaster.WorkFlow
{
    public interface IFlowServiceProvider :
        IDeviceConfigurationService,
        ICameraRuntimeService,
        ISerialRuntimeService
    {
        RuntimeDiagnosticsHub RuntimeDiagnostics { get; }
    }
}
