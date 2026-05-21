# AGENTS.md

## Architecture
- WinForms C# desktop app, .NET Framework 4.8, C# 7.3
- Solution uses `.slnx` format (VS 2022+)
- 6 projects, layered:
  ```
  VisualCutterForm (app shell) → VisualMaster.Forms (UI) → VisualMaster.WorkFlow (engine)
                                                  ↘ VisualMaster.Communication (serial)
                                                  ↘ VisualMaster.CameraLink (cameras) ← WPF
  ← All depend on VisualMaster.Api (interfaces + data types)
  ```
- `VisualMaster.Forms` is the UI library (ImageViewer, VisionController, AppConfig, Camera/* forms, FlowEditor/* forms, TriggerEditor/*, CodeEditor/*) — all reusable WinForms controls live here
- `VisualMaster.CameraLink` is a **WPF** project — contains camera HW abstractions, MVS SDK adapter, and WPF camera UI (CameraManagerWindow, CameraImageViewer, CameraPreviewControl). Has its own ViewModels with MVVM pattern.
- `VisualCutterForm` only has shell forms: `Form1.cs`, `LoginForm.cs`, `DefaultLoginForm.cs`, `Program.cs`

## Dev Environment
VS Developer Command Prompt (Insiders):
```
C:\Program Files\Microsoft Visual Studio\18\Insiders\Common7\Tools\VsDevCmd.bat
```
PowerShell:
```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Insiders\Common7\Tools\Launch-VsDevShell.ps1"
```

## Build & Run
```bash
msbuild VisualCutterForm.slnx /p:Configuration=Debug
VisualCutterForm\bin\Debug\VisualCutterForm.exe
```

## Testing
- No test project.

## Dependencies
- **OpenCvSharp4.Windows** (NuGet) — `VisualMaster.WorkFlow` + `VisualMaster.Forms`
- **MvCameraControl.Net** — Hikrobot MVS SDK, external ref at `C:\Program Files (x86)\MVS\`
- **System.IO.Ports** (NuGet) — `VisualMaster.Communication` + `VisualMaster.Forms`
- **Microsoft.CodeAnalysis.CSharp** 4.8 (NuGet) — `VisualMaster.WorkFlow` (ComputationNode Roslyn compilation)
- **Newtonsoft.Json** 13.0 (NuGet) — `VisualMaster.WorkFlow` (FlowSerializer)
- **FCTB** 2.16.24 (NuGet) — `VisualMaster.Forms` (CodeEditor)
- **NuGet.Protocol** 6.12 (NuGet) — `VisualMaster.Forms` (CodeEditor NuGet package resolution)
- Project uses `PackageReference` format (not `packages.config`)

## Critical Constraints

### .csproj format
**ALL 6 projects** use old-style `.csproj` with explicit `<Compile Include="...">` entries. Every new `.cs` file must be manually added or it won't compile.

### Designer files
- `<DependentUpon>` paths use **filename only** (no subfolder prefix) — resolves relative to file's directory:
  ```xml
  <!-- Correct -->
  <Compile Include="TriggerEditor\TriggerEditorForm.Designer.cs">
    <DependentUpon>TriggerEditorForm.cs</DependentUpon>
  </Compile>
  <!-- Wrong: <DependentUpon>TriggerEditor\TriggerEditorForm.cs</DependentUpon> -->
  ```
- Do NOT hand-edit auto-generated files: `Form1.Designer.cs`, `Properties/Resources.Designer.cs`, `Properties/Settings.Designer.cs`

### Programmatic UI layout
When building WinForms controls in code (not Designer), you **must** call `SuspendLayout()`/`ResumeLayout()` on every container Panel. If controls use `Dock.Top` and `Dock.Fill` together, without SuspendLayout the Dock order is sensitive to `Controls.Add()` order and causes overlapping. Prefer Designer files over programmatic construction for forms with multiple nested panels.

### Trigger system (post-refactor)
- `SubGraphTrigger` enum has been **deleted**. Do not reference it.
- Triggers are now `TriggerEntry` objects in `FlowGraph.Triggers` list, persisted to `.flow` JSON.
- `TriggerSourceType`: `Manual`, `CameraFrame`, `Timer`, `SerialMatch`
- `TriggerManager` handles activation/deactivation in `FlowExecutor.Start()`/`Stop()`
- `FlowExecutor` constructor takes `IFlowServiceProvider` (NOT `dynamic`). `VisionController` implements it (11→6 methods after pruning).
- `TriggerEditorForm` accessible from `FlowEditorForm → 输入输出 → 触发器编辑器...`
- Manual triggers have a toolbar dropdown: `▶ 手动触发 ▾` in FlowEditorForm
- `FlowSubGraph` no longer has a `Trigger` property
- Backward compat for old `.flow` files is intentionally NOT supported

### CameraLink refactoring — critical changes

#### CameraSlot is obsolete
`CameraSlot` is marked `[Obsolete]`. Use `CameraDeviceConfig` / `CameraDeviceStatus` instead. The old slot-based API (`Slots`, `AddSlot`, `RemoveSlot`, `OpenSlot`, `CloseSlot`, `IsSlotOpen`) on `ICameraManager` is `[Obsolete]`. New API uses `Device`-based naming:
- `CameraDevices` / `AddDevice` / `RemoveDevice` / `OpenDevice` / `CloseDevice` / `IsDeviceOpen`

#### Two parallel camera access paths
1. **Legacy path**: `MvsCamera` implements `ICamera` directly — uses `CameraInfo.RawInfo` as `IDeviceInfo`. Has `GetLatestFrame()`, internal `_latestFrame` cache, and `TriggerSoftware()` that reconfigures TriggerMode before commanding.
2. **New path**: `HikrobotDevice` (implements `ICameraDeviceDriver`) → wrapped by `ManagedCamera` (aggregates driver + `CameraDeviceConfig` + `CameraFrameBuffer`) → managed by `CameraManager` (implements `ICameraManager`). Uses `HikrobotAdapter` for discovery.

#### CameraFrameBuffer / CameraFrameSnapshot
New thread-safe frame queue with `Publish()`, `PeekLatestSnapshot()`, `WaitForNextSnapshot()`. Snapshots are **ref-counted** — call `Dispose()` or use `CloneFrame()` for a standalone Bitmap. `ImageFifo` now has a `CameraFrameBuffer`-backed constructor overload.

#### CameraSettings additions
- `PixelFormat` (string) — camera pixel format, applied via `SetEnumValueByString("PixelFormat", ...)`
- `MonochromeOutput` (bool, default false) — if true, `ManagedCamera.OnFrameAcquired` converts color frames to grayscale via ColorMatrix before publishing

#### GetAvailablePixelFormats()
Available on `ICamera`, `ICameraDeviceDriver`, `ManagedCamera`, `CameraManager`. Queries camera hardware via MVS SDK:
```csharp
_device.Parameters.GetEnumValue("PixelFormat", out IEnumValue enumValue);
enumValue.SupportEnumEntries.Select(e => e.Symbolic).ToArray();
```

#### RuntimeDiagnosticsHub
Thread-safe diagnostic event ring buffer on `ICameraManager.Diagnostics` and `ManagedCamera.Diagnostics`. Events: `SnapshotPublished`, `TriggerDispatched`, `FlowStarted/Completed/Failed`. Inject into `RuntimeDiagnosticEvent` with device/flow/trigger correlation IDs.

#### MVS SDK calling conventions (HikrobotDevice / MvsCamera)
```
SetEnumValueByString("ParameterName", "EnumEntry")
SetFloatValue("ExposureTime", ...) / SetFloatValue("Gain", ...)
SetIntValue("Width"/"Height"/"OffsetX"/"OffsetY")
SetCommandValue("TriggerSoftware")
GetEnumValue("PixelFormat", out IEnumValue)
GetFloatValue("ExposureTime", out IFloatValue) → ev.CurValue
GetIntValue("Width", out IIntValue) → w.CurValue
StreamGrabber.SetImageNodeNum(5u); StreamGrabber.StartGrabbing()
StreamGrabber.GetImageBuffer(timeoutMs, out IFrameOut); frame.Image.ToBitmap()
StreamGrabber.FrameGrabedEvent += OnFrameGrabbed
```

### Namespace structure
```
VisualMaster.Forms              — VisionController, AppConfig, DarkTheme, DisplayItem, ImageViewer
VisualMaster.Forms.Camera       — CameraDiscoveryControl, CameraSettingsControl, CameraManagerForm, etc.
VisualMaster.Forms.FlowEditor   — FlowEditorForm, FlowCanvas, FlowPropertyInspector, FlowToolbox
VisualMaster.Forms.CodeEditor   — CodeEditorForm, ReferenceManager
VisualMaster.Forms.TriggerEditor — TriggerEditorForm
VisualMaster.WorkFlow           — FlowGraph, FlowExecutor, FlowSerializer, FlowNode, etc.
VisualMaster.WorkFlow.Nodes     — All node types
VisualMaster.WorkFlow.Triggers  — TriggerEntry, TriggerManager
VisualMaster.WorkFlow.Data      — AcquisitionResult, SerialTriggerRule
VisualMaster.CameraLink         — CameraManager, MvsCamera (legacy), HikrobotAdapter, HikrobotDevice
VisualMaster.CameraLink.API     — ICameraAdapter, ICameraDeviceDriver, DiscoveredCamera
VisualMaster.CameraLink.Core    — ManagedCamera
VisualMaster.CameraLink.Adapter — HikrobotAdapter, HikrobotDevice
VisualMaster.CameraLink.UI      — CameraManagerWindow, CameraManagerPanel, CameraImageViewer
VisualMaster.CameraLink.UI.ViewModels — CameraManagerViewModel, CameraItemViewModel, etc.
```

### Shared helpers
- `DarkTheme` — static colors, fonts, factory methods (Label, Button, ComboBox, TextBox)
- `DisplayItem` — replaces per-form SlotEntry/ComboItem/SlotDisplayItem patterns. Has `Id`, `Display`, `Tag`, `ToString()`.

### Slot-based HW pattern
Both camera and serial use device/slot-ID-based configuration persisted in `FlowGraph` and serialized to `.flow` JSON. Runtime connections restored by serial/port name matching via `VisionController.SyncToGraph`/`SyncFromGraph`.

### Known gotchas
- `FlowCanvas.OnPaint` draws connections BEFORE nodes (z-order: connections behind). Pin locations are pre-computed in `RebuildViews` via `ComputePinLocations`.
- `FlowEditorForm` has NO subgraph-level trigger dropdown. All trigger config is in `TriggerEditorForm`.
- Running process locks `VisualMaster.Forms.dll` copy — kill `VisualCutterForm.exe` before building when getting `MSB3021` errors.
- `VisualMaster.CameraLink` is a WPF project — VS Designer for XAML requires WPF workload installed.
