# AGENTS.md

## Scope

This file helps AI coding agents work productively in this repository after the dependency-oriented refactor.

For deep architecture diagrams and node-level flow details, prefer linking to `README.md` instead of duplicating content.

## Project Boundary Rule (Critical)

- Do not mix repository identity, product scope, and library namespaces as if they are the same project.
- The repository and solution file are still named `VisualCutterForm`, but the old `VisualCutterForm/VisualCutterForm.csproj` application module is deprecated.
- The active architecture now has three scopes:
  - Deprecated legacy app scope: `VisualCutterForm/...`
  - New dependency-oriented VisualMaster product scope: `VisualMaster.Application/...`, `VisualMaster.Api/...`, `VisualMaster.Impl/...`, `VisualMaster.UI/...`
  - Independent VisualMaster capability modules: `VisualMaster.CameraLink/...`, `VisualMaster.Communication/...`, and other sibling module projects
- `VisualMaster.Application`, `VisualMaster.Api`, `VisualMaster.Impl`, and `VisualMaster.UI` are not just ordinary sibling capability modules; together they are the new dependency-oriented application/product layer.
- `VisualMaster.CameraLink` and `VisualMaster.Communication` remain capability modules and should stay distinct from the new product-layer projects.
- When describing architecture, always state whether a path/class belongs to:
  - Deprecated legacy app module (`VisualCutterForm/...`)
  - New product-layer projects (`VisualMaster.Application/...`, `VisualMaster.Api/...`, `VisualMaster.Impl/...`, `VisualMaster.UI/...`)
  - VisualMaster capability modules (`VisualMaster.CameraLink/...`, `VisualMaster.Communication/...`)
- Never say "VisualMaster project" to mean the whole repository or solution.

## Current Solution Structure

- Tech stack: C# 7.3, .NET Framework 4.8, WinForms + WPF, solution format `.slnx`.
- Projects contained in solution `VisualCutterForm.slnx`:
  - `VisualMaster.Application/VisualMaster.Application.csproj` (new application composition root; owns host building, startup configuration, and module assembly)
  - `VisualMaster.Api/VisualMaster.Api.csproj` (new API/abstraction boundary for dependency-oriented product layer)
  - `VisualMaster.Impl/VisualMaster.Impl.csproj` (new implementation layer; depends on `VisualMaster.Api`)
  - `VisualMaster.UI/VisualMaster.UI.csproj` (new WPF UI component library; depends on `VisualMaster.Api`)
  - `VisualMaster.Config.Abstractions/VisualMaster.Config.Abstractions.csproj` (configuration abstractions)
  - `VisualMaster.CameraLink/VisualMaster.CameraLink.csproj` (WPF camera capability module)
  - `VisualMaster.Communication/VisualMaster.Communication.csproj` (WPF communication capability module)
  - `VisualMaster.CameraLink.TestApp/VisualMaster.CameraLink.TestApp.csproj` (camera sample app)
  - `VisualMaster.CameraLink.TestApp.Viewer/VisualMaster.CameraLink.TestApp.Viewer.csproj` (camera viewer sample)
  - `VisualMaster.Communication.TestApp/VisualMaster.Communication.TestApp.csproj` (communication sample app)
  - `VisualCutterForm/VisualCutterForm.csproj` (deprecated legacy WinForms app module)
  - `SetupVisualCutter/SetupVisualCutter.vdproj` (installer)

## Important Refactor Reality

- The old `VisualCutterForm` app module is deprecated. Do not treat it as the active main application unless the user explicitly asks for legacy work.
- New feature work should default to the dependency-oriented product layer:
  - Put application startup, Host construction, bootstrap configuration, and module assembly in `VisualMaster.Application`.
  - Put public contracts, interfaces, DTOs, and composition-facing abstractions in `VisualMaster.Api`.
  - Put concrete services, orchestration, adapters, and non-UI implementation in `VisualMaster.Impl`.
  - Put reusable WPF controls, views, view models, resources, and themes in `VisualMaster.UI`.
- Keep capability-specific hardware/runtime logic in the relevant capability modules:
  - Camera-specific code belongs in `VisualMaster.CameraLink`.
  - Communication-specific code belongs in `VisualMaster.Communication`.
- Do not collapse the new product layer and capability modules into one conceptual "VisualMaster" bucket when discussing ownership or making edits.
- There is no separate `VisualMaster.Forms` or `VisualMaster.WorkFlow` project.

## Dependency Direction

- Preferred product-layer direction:
  - `VisualMaster.Application` -> `VisualMaster.Api`, `VisualMaster.Impl`, `VisualMaster.UI`, and selected capability modules
  - `VisualMaster.Impl` -> `VisualMaster.Api`
  - `VisualMaster.UI` -> `VisualMaster.Api`
- `VisualMaster.Api` should stay lightweight and should not depend on UI or implementation projects.
- `VisualMaster.UI` is a component library and should not own Host construction, application startup, or module assembly.
- Capability modules should remain reusable and should not depend on the deprecated `VisualCutterForm` module.
- Avoid introducing references from capability modules back into `VisualMaster.UI` unless the user explicitly asks for a larger architecture change.

## Build and Run

- Build all projects:
  - `msbuild VisualCutterForm.slnx /p:Configuration=Debug`
- Build focused targets when possible:
  - `msbuild VisualMaster.Application\VisualMaster.Application.csproj /p:Configuration=Debug`
  - `msbuild VisualMaster.Api\VisualMaster.Api.csproj /p:Configuration=Debug`
  - `msbuild VisualMaster.Impl\VisualMaster.Impl.csproj /p:Configuration=Debug`
  - `msbuild VisualMaster.UI\VisualMaster.UI.csproj /p:Configuration=Debug`
- Existing "TestApp" projects are manual verification apps, not automated tests.
- If build fails with file lock (`MSB3021`), stop the corresponding running `.exe` before rebuilding.

## Hard Constraints for Code Changes

- All major projects use old-style `.csproj` with explicit `<Compile Include="...">` entries.
  - Any new `.cs` file must be manually added to the corresponding `.csproj`.
- WPF files also need the correct `<Page Include="...">` or related project entries when added to old-style projects.
- For WinForms/WPF designer code-behind entries, keep `<DependentUpon>` as filename only.
  - Example: `TriggerEditorForm.cs` (not `TriggerEditor\TriggerEditorForm.cs`).
- Do not hand-edit auto-generated designer/resource files.

## Runtime/Architecture Notes Agents Must Respect

### Product Layer (`VisualMaster.Application`, `VisualMaster.Api`, `VisualMaster.Impl`, `VisualMaster.UI`)

- Treat these projects as the new dependency-oriented product surface.
- Keep `ApplicationHostBuilder` in `VisualMaster.Application` root as the application startup facade.
- Keep contracts stable and implementation-free in `VisualMaster.Api`.
- Keep composition and concrete service wiring out of capability modules when it belongs to product orchestration.
- Keep UI component ownership in `VisualMaster.UI`; do not add new active UI work to deprecated `VisualCutterForm` unless requested.

### Deprecated Legacy App (`VisualCutterForm`)

- `VisualCutterForm/VisualCutterForm.csproj` is deprecated but still exists in the solution.
- Legacy paths such as `VisualCutterForm/Form1.cs`, `VisualCutterForm/Forms/`, and `VisualCutterForm/WorkFlow/` may still be useful for reference or migration.
- Avoid adding new product behavior here unless the user explicitly asks for compatibility or migration work.

### Trigger System

- `SubGraphTrigger` is removed.
- Triggers are `TriggerEntry` entries in `FlowGraph.Triggers`.
- `TriggerSourceType`: `Manual`, `CameraFrame`, `Timer`, `SerialMatch`.
- Trigger activation/deactivation is handled by `TriggerManager` from `FlowExecutor.Start()` / `Stop()`.
- Backward compatibility for legacy `.flow` trigger model is intentionally not provided.

### Communication (VisualMaster.Communication)

- Driver-based model: `CommunicationManager` + `ICommunicationDriver` + `ICommunicationBlock`.
- Current drivers include both UART and TCP (`UartDriver`, `TcpDriver`).
- Legacy main-app serial runtime bridge: `VisualCutterForm/Forms/VisionSerialRuntime.cs`.
- `VisualMaster.Communication/SerialPortAdapter.cs` exists on disk but is not included in its `.csproj` (treat as stale/dead code).
- The communication module owns communication capability behavior and reusable communication UI, not product-level orchestration.

### Camera (VisualMaster.CameraLink)

- Old slot APIs are obsolete; prefer `CameraDeviceConfig`/`CameraDeviceStatus` and device-based manager methods.
- Two access paths coexist:
  - Legacy: `MvsCamera` (`ICamera` style)
  - New: `HikrobotDevice` -> `ManagedCamera` -> `CameraManager`
- `CameraFrameBuffer`/`CameraFrameSnapshot` are ref-counted; dispose snapshots or clone before long-lived usage.
- The camera module owns camera capability behavior and reusable camera UI, not product-level orchestration.

## Key Paths for Fast Navigation

- New API/product contracts: `VisualMaster.Api/`
- New application composition root: `VisualMaster.Application/`
- New implementation layer: `VisualMaster.Impl/`
- New WPF UI component library: `VisualMaster.UI/`
- Deprecated legacy app shell: `VisualCutterForm/Form1.cs`
- Deprecated legacy app orchestrator/runtime bridge: `VisualCutterForm/Forms/VisionController.cs`
- Deprecated legacy flow execution: `VisualCutterForm/WorkFlow/FlowExecutor.cs`
- Camera capability module: `VisualMaster.CameraLink/`
- Communication capability module: `VisualMaster.Communication/`

## External Dependencies (Operational)

- Hikrobot MVS .NET SDK: `MvCameraControl.Net` expected at:
  - `C:\Program Files (x86)\MVS\Development\DotNet\AnyCpu\MvCameraControl.Net.dll`
- NuGet package style: `PackageReference`.

## Additional Reference

- Detailed architecture and execution flow diagrams: `README.md`
