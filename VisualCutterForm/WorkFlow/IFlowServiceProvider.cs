using VisualMaster.Api;
// RuntimeDiagnosticsHub here refers to VisualMaster.Api.RuntimeDiagnosticsHub (used for serial/workflow diagnostics)

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
