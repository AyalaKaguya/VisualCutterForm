# VisualCutterForm

WinForms 视觉检测平台，支持海康工业相机、OpenCV 算子流程编排、C# 脚本运算节点、串口通信触发。

## 架构总览

```mermaid
graph TB
    subgraph UI["🎨 UI 层"]
        Form1["Form1<br/>主窗口"]
        FlowEditor["FlowEditorForm<br/>流程编辑器"]
        CodeEditor["CodeEditorForm<br/>脚本编辑器"]
        LoginForm["LoginForm<br/>角色登录"]
        CamDialog["CameraSettingsDialog<br/>相机设置"]
    end

    subgraph Editor["✏️ 编辑器组件"]
        Canvas["FlowCanvas<br/>节点画布"]
        NodeView["FlowNodeView<br/>节点渲染"]
        Inspector["FlowPropertyInspector<br/>属性面板"]
        Toolbox["FlowToolbox<br/>节点工具箱"]
    end

    subgraph FlowCore["⚙️ Flow 核心"]
        FlowGraph["FlowGraph<br/>流程根容器"]
        FlowSubGraph["FlowSubGraph<br/>子图(节点+连线)"]
        FlowExecutor["FlowExecutor<br/>执行引擎"]
        FlowContext["FlowContext<br/>运行时上下文"]
        FlowSerializer["FlowSerializer<br/>JSON 序列化"]
        NodeFactory["NodeFactory<br/>反射发现节点"]
    end

    subgraph Nodes["🔲 Flow Nodes"]
        direction LR
        CamNode["取相机"]
        FileNode["取文件"]
        ToMat["转Mat"]
        CompNode["代码运算"]
        CvNodes["OpenCV算子"]
        DispNode["图像展示"]
        SerialRx["串口接收"]
        SerialTx["串口发送"]
    end

    subgraph Data["📦 Flow Data"]
        AcqResult["AcquisitionResult"]
        TriggerRule["SerialTriggerRule"]
    end

    subgraph HW["🖥️ 硬件层"]
        VisionCtrl["VisionController"]
        CameraMgr["CameraManager"]
        ImageFifo["ImageFifo<br/>帧队列"]
        MvsCam["MvsCamera"]
        SerialPort["SerialPortAdapter"]
    end

    subgraph Config["🔧 配置"]
        AppConfig["AppConfig"]
        IniFile["IniFile<br/>kernel32 P/Invoke"]
        CamStore["CameraSettingsStore"]
    end

    Form1 --> FlowEditor
    Form1 --> VisionCtrl
    Form1 --> FlowExecutor
    Form1 --> AppConfig
    FlowEditor --> Canvas
    FlowEditor --> Inspector
    FlowEditor --> Toolbox
    Canvas --> NodeView
    Canvas --> Toolbox
    FlowExecutor --> FlowGraph
    FlowExecutor --> FlowContext
    FlowGraph --> FlowSubGraph
    FlowSubGraph --> Nodes
    Nodes --> Data
    Nodes --> FlowCore
    FlowSerializer --> FlowGraph
    NodeFactory --> Nodes
    VisionCtrl --> CameraMgr
    VisionCtrl --> ImageFifo
    VisionCtrl --> SerialPort
    CameraMgr --> MvsCam
    AppConfig --> IniFile
    AppConfig --> CamStore

    style Form1 fill:#2c3e50,color:#fff
    style FlowExecutor fill:#8e44ad,color:#fff
    style VisionCtrl fill:#c0392b,color:#fff
    style Nodes fill:#27ae60,color:#fff
```

## 数据/事件流

```mermaid
sequenceDiagram
    participant SDK as 海康 SDK
    participant VC as VisionController<br/>(ImageFifo)
    participant FE as FlowExecutor
    participant Cam as CameraAcqNode
    participant ToMat as AcqResultToMat
    participant CV as OpenCV Node
    participant Comp as ComputationNode
    participant Disp as ImageDisplayNode
    participant Timer as PreviewTimer
    participant Serial as SerialPort

    Note over SDK,Disp: HardCameraTrigger 路径
    SDK->>VC: FrameGrabbed
    VC->>VC: fifo.Enqueue(bitmap)
    VC->>FE: FrameEnqueued 事件
    FE->>FE: RunSubGraphOnce()
    FE->>Cam: ① BindInputs→Execute
    Cam->>VC: fifo.TryDequeue()
    VC-->>Cam: Bitmap → Mat
    Cam->>Cam: new AcquisitionResult(mat)
    Cam->>FE: ② WriteOutputs

    FE->>ToMat: ① BindInputs→Execute
    ToMat->>ToMat: Image.Clone() + W/H
    ToMat->>FE: ② WriteOutputs(Mat, int, int)

    FE->>CV: ① BindInputs→Execute
    CV->>CV: Cv2.GaussianBlur/Canny/Resize...
    CV->>FE: ② WriteOutputs(Mat)

    FE->>Comp: ① BindInputs→Execute
    Comp->>Comp: 编译缓存命中<br/>实例.Execute();
    Comp->>FE: ② WriteOutputs(Mat)

    FE->>Disp: ① BindInputs→Execute
    Disp->>Disp: MatToBitmap<br/>IsModified=true
    FE->>FE: 捕获 LastValue

    Timer->>Disp: IsModified?
    Disp-->>Timer: GetPreviewBitmap()
    Timer->>Timer: ShowPreview(bmp)

    Note over SDK,Disp: SerialTrigger 路径
    Serial->>FE: DataReceived → RuleTriggered
    FE->>FE: TriggerSubGraph(sg.Id)
    FE->>Cam: RunSubGraphOnce...
```

## 节点继承树

```mermaid
classDiagram
    FlowNode <|-- CameraAcquisitionNode : 取像
    FlowNode <|-- FileAcquisitionNode : 取像
    FlowNode <|-- AcqResultToMatNode : 转换
    FlowNode <|-- ComputationNode : 运算
    FlowNode <|-- CvGaussianBlurNode : OpenCV
    FlowNode <|-- CvMedianBlurNode : OpenCV
    FlowNode <|-- CvCannyNode : OpenCV
    FlowNode <|-- CvThresholdNode : OpenCV
    FlowNode <|-- CvCvtColorNode : OpenCV
    FlowNode <|-- CvResizeNode : OpenCV
    FlowNode <|-- CvDilateNode : OpenCV
    FlowNode <|-- CvErodeNode : OpenCV
    FlowNode <|-- ImageDisplayNode : 显示
    FlowNode <|-- SerialSendNode : 通信
    FlowNode <|-- SerialReceiveNode : 通信(后台)

    NodePin <|-- InputPin : 输入引脚
    NodePin <|-- OutputPin : 输出引脚

    class FlowNode {
        +Guid Id
        +string Name
        +string Category
        +List~InputPin~ Inputs
        +List~OutputPin~ Outputs
        +double LastExecutionTimeMs
        +ExecuteAsync(ctx, ct)
        +GetNodeProperties()
    }

    class InputPin {
        +OutputPin Source
        +object DefaultValue
        +GetValue(context) object
    }

    class OutputPin {
        +List~InputPin~ Targets
        +SetValue(context, val)
    }

    class ComputationNode {
        -CSharpScriptCompiler _compiler
        -Action _executeDelegate
        -object _compiledInstance
        +string SourceCode
    }
```

## 主窗口组件树

```mermaid
graph LR
    Form1["Form1"]
    Splitter["_mainSplit<br/>(Vertical)"]
    PreviewPanel["previewContainer"]
    LogPanel["logPanel"]
    Selector["selectorBar"]
    ImageView["_previewBox<br/>ImageViewer"]
    CameraCombo["_cameraComboBox<br/>(hidden)"]
    LogBox["_logBox<br/>RichTextBox"]
    LogHeader["logHeader"]

    Form1 --> Splitter
    Splitter -->|Panel1| PreviewPanel
    Splitter -->|Panel2| LogPanel
    PreviewPanel --> Selector
    PreviewPanel --> CameraCombo
    PreviewPanel --> ImageView
    LogPanel --> LogHeader
    LogPanel --> LogBox
    ImageView -->|"浮动按钮"| Toolbar["− zoom + ⊡ 1:1"]
```

## RunSubGraphOnce 执行流程

```mermaid
flowchart LR
    A[FlowContext 创建] --> B[拓扑排序<br/>排除 Background]
    B --> C{遍历节点}
    C --> D[BindInputsToProperties<br/>InputPin.GetValue]
    D --> E[ExecuteAsync<br/>异步执行]
    E --> F[WriteOutputsFromProperties<br/>OutputPin.SetValue]
    F --> G["捕获 LastValue<br/>(输入+输出)"]
    G --> H["计时<br/>LastExecutionTimeMs"]
    H --> C
    C --> I[结束]
```

## Pin 值解析链

```mermaid
flowchart TB
    A["InputPin.GetValue(ctx)"]
    A --> B{"Source != null?"}
    B -->|是| C["context.GetPinValue(Source)<br/>来自上游 OutputPin"]
    B -->|否| D{"context.TryGetPinValue(this)"}
    D -->|命中| E[返回上下文缓存值]
    D -->|未命中| F[返回 DefaultValue<br/>属性面板设置]
```

## 技术栈

| 分类 | 技术 |
|------|------|
| 平台 | .NET Framework 4.8, WinForms |
| 图像 | OpenCvSharp4.Windows 4.8 |
| 相机 | MvCameraControl.Net (Hikrobot MVS SDK) |
| 脚本 | Microsoft.CodeAnalysis.CSharp 4.8 (Roslyn) |
| 序列化 | Newtonsoft.Json 13.0 |
| 串口 | System.IO.Ports 8.0 |
| 编辑器 | FastColoredTextBox 2.16 (FCTB) |
| 配置 | kernel32.dll P/Invoke INI 文件 |
| 构建 | MSBuild / VS 2022 Insiders (`.slnx`) |

## 节点清单

| 分类 | 节点 | 输入 | 输出 |
|------|------|------|------|
| 取像 | 相机取像 | — | AcquisitionResult |
| 取像 | 文件取像 | — | AcquisitionResult |
| 转换 | 取像结果转Mat | AcquisitionResult | Mat + Width + Height |
| OpenCV | 高斯模糊 | Mat | Mat |
| OpenCV | 中值模糊 | Mat | Mat |
| OpenCV | 边缘检测 | Mat | Mat |
| OpenCV | 阈值二值化 | Mat | Mat |
| OpenCV | 颜色转换 | Mat | Mat |
| OpenCV | 图像缩放 | Mat | Mat |
| OpenCV | 膨胀 | Mat | Mat |
| OpenCV | 腐蚀 | Mat | Mat |
| 运算 | 代码运算 | 动态引脚 | 动态引脚 |
| 显示 | 图像展示 | Mat | — |
| 通信 | 串口发送 | 待发数据 | — |
| 通信 | 串口接收(后台) | — | 接收数据 |

## 构建与运行

```powershell
# 启动 VS Developer Command Prompt
& "C:\Program Files\Microsoft Visual Studio\18\Insiders\Common7\Tools\Launch-VsDevShell.ps1"

# 构建
msbuild VisualCutterForm.slnx /p:Configuration=Debug

# 运行
VisualCutterForm\bin\Debug\VisualCutterForm.exe
```

## 约束

- `.csproj` 使用显式 `<Compile Include="...">` 条目，新增 `.cs` 文件需手动添加到项目文件
- `Form1.Designer.cs` 和 `Properties/` 下自动生成文件不可手改
- `NodeFactory` 通过反射自动发现 `FlowNode` 子类，无需手动注册节点
- Git 仓库: `git@github.com:AyalaKaguya/VisualCutterForm.git`
