# AGENTS.md

## Scope

This file helps AI coding agents work productively in this repository after the recent structure refactor.

For deep architecture diagrams and node-level flow details, prefer linking to `README.md` instead of duplicating content.

## Project Boundary Rule (Critical)

- Do not mix `VisualCutterForm` and `VisualMaster` as if they are the same project.
- `VisualCutterForm` refers to the solution/repository identity and the main executable app project (`VisualCutterForm/VisualCutterForm.csproj`).
- `VisualMaster.*` refers to component/library namespaces and project names (for example camera and communication libraries).
- When describing architecture, always state whether a path/class belongs to:
  - Main app project (`VisualCutterForm/...`)
  - VisualMaster library projects (`VisualMaster.CameraLink/...`, `VisualMaster.Communication/...`)
- Never say "VisualMaster project" to mean the whole solution.

## Current Solution Structure

- Tech stack: C# 7.3, .NET Framework 4.8, WinForms + WPF, solution format `.slnx`.
- Projects contained in solution `VisualCutterForm.slnx`:
  - `VisualCutterForm/VisualCutterForm.csproj` (main app shell + workflow + reusable WinForms controls)
  - `VisualMaster.CameraLink/VisualMaster.CameraLink.csproj` (WPF camera library)
  - `VisualMaster.Communication/VisualMaster.Communication.csproj` (WPF communication library)
  - `VisualMaster.CameraLink.App/VisualMaster.CameraLink.TestApp.csproj` (camera sample app)
  - `VisualMaster.CameraLink.TestApp/VisualMaster.CameraLink.TestApp.Viewer.csproj` (camera viewer sample)
  - `VisualMaster.Communication.TestApp/VisualMaster.Communication.TestApp.csproj` (communication sample app)
  - `SetupVisualCutter/SetupVisualCutter.vdproj` (installer)

## Important Refactor Reality (What Changed)

- There is no separate `VisualMaster.Forms` or `VisualMaster.WorkFlow` project anymore.
- `Api/`, `WorkFlow/`, `Forms/`, and `Legacy/` are now source folders inside `VisualCutterForm/VisualCutterForm.csproj`.
- Main app still references only two external project libraries:
  - `VisualMaster.CameraLink`
  - `VisualMaster.Communication`

## Build and Run

- Build all projects:
  - `msbuild VisualCutterForm.slnx /p:Configuration=Debug`
- Run main app:
  - `VisualCutterForm\bin\Debug\VisualCutterForm.exe`
- If build fails with file lock (`MSB3021`), stop `VisualCutterForm.exe` first.

## Testing Status

- No unit/integration test project.
- Existing "TestApp" projects are manual verification apps, not automated tests.

## Hard Constraints for Code Changes

- All major projects use old-style `.csproj` with explicit `<Compile Include="...">` entries.
  - Any new `.cs` file must be manually added to the corresponding `.csproj`.
- For WinForms/WPF designer code-behind entries, keep `<DependentUpon>` as filename only.
  - Example: `TriggerEditorForm.cs` (not `TriggerEditor\TriggerEditorForm.cs`).
- Do not hand-edit auto-generated designer/resource files.

## Runtime/Architecture Notes Agents Must Respect

### Trigger System

- `SubGraphTrigger` is removed.
- Triggers are `TriggerEntry` entries in `FlowGraph.Triggers`.
- `TriggerSourceType`: `Manual`, `CameraFrame`, `Timer`, `SerialMatch`.
- Trigger activation/deactivation is handled by `TriggerManager` from `FlowExecutor.Start()` / `Stop()`.
- Backward compatibility for legacy `.flow` trigger model is intentionally not provided.

### Communication (VisualMaster.Communication)

- Driver-based model: `CommunicationManager` + `ICommunicationDriver` + `ICommunicationBlock`.
- Current drivers include both UART and TCP (`UartDriver`, `TcpDriver`).
- In the main app, serial runtime bridge is `Forms/VisionSerialRuntime.cs`.
- `VisualMaster.Communication/SerialPortAdapter.cs` exists on disk but is not included in its `.csproj` (treat as stale/dead code).

### Camera (VisualMaster.CameraLink)

- Old slot APIs are obsolete; prefer `CameraDeviceConfig`/`CameraDeviceStatus` and device-based manager methods.
- Two access paths coexist:
  - Legacy: `MvsCamera` (`ICamera` style)
  - New: `HikrobotDevice` -> `ManagedCamera` -> `CameraManager`
- `CameraFrameBuffer`/`CameraFrameSnapshot` are ref-counted; dispose snapshots or clone before long-lived usage.

## Key Paths for Fast Navigation

- Main app shell: `VisualCutterForm/Form1.cs`
- App orchestrator/runtime bridge: `VisualCutterForm/Forms/VisionController.cs`
- Flow execution: `VisualCutterForm/WorkFlow/FlowExecutor.cs`
- Trigger models/runtime: `VisualCutterForm/WorkFlow/Triggers/`
- Camera library: `VisualMaster.CameraLink/`
- Communication library: `VisualMaster.Communication/`

## External Dependencies (Operational)

- Hikrobot MVS .NET SDK: `MvCameraControl.Net` expected at:
  - `C:\Program Files (x86)\MVS\Development\DotNet\AnyCpu\MvCameraControl.Net.dll`
- NuGet package style: `PackageReference`.

## Additional Reference

- Detailed architecture and execution flow diagrams: `README.md`
