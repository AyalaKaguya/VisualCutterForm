# Architecture Refactoring Plan

## Phase 1: Infrastructure Foundation (low risk)

### 1.1 PinTypeResolver — deduplicate type resolution
- New file: `Lib/PinTypeResolver.cs`
- `FlowSerializer.ResolveType()` and `FlowPropertyInspector.ResolveType()` share identical switch statements
- Extract into `public static Type Resolve(string name)` in a shared utility
- Both callers use the shared method

### 1.2 NodeFactory thread safety
- `_cachedTypes` field changed from `List<NodeTypeInfo>` to `Lazy<List<NodeTypeInfo>>`
- Uses `LazyThreadSafetyMode.ExecutionAndPublication`
- Eliminates TOCTOU race on first call

### 1.3 FlowNode metadata caching
- New static `ConcurrentDictionary<Type, NodeMetadata>` in FlowNode
- `DiscoverPins()`, `GetNodeProperties()`, `BindInputsToProperties()`, `WriteOutputsFromProperties()` cache per-type
- PropertyInfo arrays computed once per FlowNode subclass, never rescanned

### 1.4 ServiceProvider DI container
- New file: `Lib/Flow/ServiceProvider.cs`
- Simple `Dictionary<Type, object>` container
- Form1 registers dependencies at startup
- Phases 2-4 use it for decoupling

---

## Phase 2: Execution Engine Hardening

### 2.1 FlowExecutor thread safety
- `_runningTasks` changed from `Dictionary<Guid, Task>` to `ConcurrentDictionary<Guid, Task>`
- `TryAdd` replaces `ContainsKey` + `[]` (TOCTOU fix)

### 2.2 FlowExecutor CancellationToken support
- New `ConcurrentDictionary<Guid, CancellationTokenSource> _runningCts`
- `StartSubGraph` creates CTS, passes token into `Task.Run`
- `Stop()` -> `StopAsync()` calls `cts.Cancel()` and awaits with 3s grace period

### 2.3 FlowNode.ExecuteAsync signature change
- `ExecuteAsync(FlowContext)` -> `ExecuteAsync(FlowContext, CancellationToken)`
- All 5 node implementations support cancellation
- CameraAcquisitionNode checks token during FIFO wait

### 2.4 Stop async
- `Form1.OnFormClosing` becomes async, awaits `StopAsync()`

---

## Phase 3: ComputationNode Refactoring

### 3.1 Extract CSharpScriptCompiler
- New file: `Lib/Flow/CSharpScriptCompiler.cs`
- Takes source code, references, NuGet packages, debug flag
- Returns `CompileResult { Assembly, Type, Instance, Errors }`
- ComputationNode delegates to it

### 3.2 Compilation result cache
- `ConcurrentDictionary<int, CompileResult>` keyed by source hash
- Avoids recompilation on repeated executes

### 3.3 NuGet cache with LRU cap
- Replace unbounded `ConcurrentDictionary<string, string> _nugetCache`
- LRU with `Dictionary<string, Node>` + `LinkedList<string>` + `lock`, max 64 entries

### 3.4 HttpClient lifecycle
- Remove `static HttpClient _http`
- Use `using var http = new HttpClient()` per request

---

## Phase 4: Main Form Decomposition

### 4.1 IFlowServiceProvider interface
- `AcquireFrameAsync(cameraSerial, timeoutMs)` -> `Task<Bitmap>`
- `SendSerialAsync(port, data)` -> `Task<bool>`
- `IsSerialConnected(port)` -> `bool`
- `ConnectSerialAsync(port, baudRate)` -> `Task`
- VisionController implements this interface
- Nodes reference `IFlowServiceProvider` instead of `VisionController`

### 4.2 Node decoupling
- Context stores `IFlowServiceProvider` instead of `VisionController`
- All node `GetVariable<VisionController>` -> `GetVariable<IFlowServiceProvider>`

### 4.3 CameraPreviewController
- New file: `Form/CameraPreviewController.cs`
- Encapsulates `StartPreview`/`StopPreview`/`SwitchCamera`
- Uses CancellationToken properly

### 4.4 FlowFileManager
- New file: `Form/FlowFileManager.cs`
- Encapsulates open/load/close flow file logic

### 4.5 MainMenuBuilder
- New file: `Form/MainMenuBuilder.cs`
- Encapsulates menu strip, status strip, preview area construction
- Dynamic camera/serial submenu rebuild

### 4.6 FlowEditorForm shared executor
- Constructor accepts `FlowExecutor` instead of creating its own
- Editor pauses external executor on open, resumes on close

---

## Phase 5: Miscellaneous Fixes

### 5.1 ImageFifo.TickCount64
- `Environment.TickCount` -> `Environment.TickCount64` (avoids 49.7-day wraparound)

### 5.2 Log ring buffer
- New helper: `Lib/LogRingBuffer.cs`
- Caps at 10,000 lines, trims head
- Used by Form1 and FlowEditorForm

### 5.3 Empty catch blocks
- 8 locations: replace `catch { }` with `catch (Exception ex) { Debug.WriteLine(ex); }`

### 5.4 PropertyChanged null -> named event
- `PropertyChanged?.Invoke(_selectedNode, null, null)` -> separate `PinsChanged` event

### 5.5 Remove redundant SetValue in CameraAcquisitionNode
- `WriteOutputsFromProperties` already handles this

### 5.6 FlowSubGraph.WireConnections logging
- Record pin names and exception info instead of silent swallow

### 5.7 Password hashing
- SHA256 + fixed salt for stored passwords

### 5.8 TypeDisplayName fix
- `string.Contains("AcquisitionResult")` -> `typeof(AcquisitionResult).IsAssignableFrom(DataType)`
