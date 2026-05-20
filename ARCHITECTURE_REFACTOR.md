# 架构重构记录

目前相机系统存在一个问题，我期望的是对相机做一个取像 FIFO 的抽象，允许硬件软件触发，在通过相机 SDK 监听取像时，如果是软件触发，则会调用 SDK 拍照（可以设定单次或连续），然后由监听线程队列写入，并输出触发信号，触发和接收是异步执行的，触发信号最终可以指定某个流程，这个流程的相机就是从 FIFO 队列中取像。由于当相机设置为硬件触发时可能存在多个流程被触发，取像需要保存一个最新图像的快照，以供来自相机 FIFO 同步触发多个流程时这些流程都能获取到同一张图像。此外相机节点可以设置为软件触发，此时需要由软件发送拍照信号并等待 FIFO 中可以取像，所以这个节点运行会转变为阻塞，等待取像结果后再执行后续节点。所谓相机管理器就是管理的相机连接参数，而相机槽位也只不过是要保存这个相机的参数，其根本没有任何作用，还不如改名叫添加相机，所以相机槽位添加的相机是不能重复的，每套相机都有其独特的设置。由此引申出来一个流程触发的配置，我可以指定哪个触发器可以执行哪个流程，触发器不一定来自于相机，也有可能来自于通信，只是结果不一样罢了，一个返回图像，另一个返回文字。相机节点说白了就是取像 FIFO 的另一重抽象，其本身只是在监听、获取、调用取像 FIFO 或者相机管理器中抽象的一套接口罢了，只不过由于在流程中多了些与流程绑定的状态管理，比如软件触发时可以动态设置曝光增益等参数。我需要你重新审视整个相机管理器、通信管理器、流程、以及重要的触发配置，对整个项目的数据结构、框架、UI 界面提出一个破坏性的重构方案。

## 重构进度 CheckList

### A. 基础运行时切片

- [x] 让触发系统从“只发启动信号”变成“携带触发载荷进入流程上下文”
- [x] 新增运行时触发上下文，支持相机帧、串口文本、串口字节、规则 ID、关联 ID
- [x] 打通 TriggerManager -> FlowExecutor -> FlowContext 的触发载荷传递链
- [x] 让相机节点在硬触发模式下优先消费本次触发帧，而不是默认竞争 FIFO
- [x] 让串口触发的文本/字节进入 FlowContext，供流程节点直接读取

### B. 服务边界收紧

- [x] 给 IFlowServiceProvider 增补相机/串口节点需要的强类型访问能力
- [x] 移除 CameraAcquisitionNode 对 dynamic VisionController 的依赖
- [x] 移除 SerialSendNode 对 dynamic VisionController 的依赖
- [x] 移除 SerialReceiveNode 对 dynamic VisionController 的依赖
- [x] 修正串口发送节点在槽位解析后仍使用原始端口字段发送的问题

### C. 串口节点语义调整

- [x] 确认 SerialReceiveNode 当前没有真正的后台启动路径
- [x] 将 SerialReceiveNode 从“悬空后台监听节点”调整为“读取当前触发载荷的流程节点”
- [x] 删除 SerialReceiveNode 剩余的过渡兼容监听代码

### D. 顶层数据模型重构

- [x] 用新的顶层项目模型替代 FlowGraph 的 CameraSlots/SerialSlots/Triggers 平铺结构
- [x] 废弃 CameraSlot 作为“配置 + 运行时”的混合概念
- [x] 废弃 SerialSlot 作为“配置 + 运行时”的混合概念
- [x] 引入稳定唯一的设备定义模型，替代“槽位”命名和语义

### E. 相机采集架构重构

- [x] 用新的帧缓冲/快照模型替代 ImageFifo
- [x] 支持同一帧被多个流程共享读取，而不是单消费者竞争
- [x] 统一硬触发和软触发，让二者都走同一采集写入链路
- [x] 支持软件触发后阻塞等待目标帧到达
- [x] 支持流程级曝光、增益等动态参数覆盖

### F. 触发器与流程路由重构

- [x] 将 TriggerEntry 从“单触发器 -> 单流程”改为“单触发器 -> 多流程绑定”
- [ ] 明确 TriggerDefinition、TriggerBinding、TriggerContext 的职责分层
- [x] 支持相机、串口、手动、定时触发走统一路由分发
- [x] 为每次触发和每次流程执行引入稳定 CorrelationId
- [ ] 为多流程共享同一帧快照建立显式策略

### G. 流程执行上下文重构

- [x] 将 FlowContext 从变量字典升级为流程实例作用域
- [x] 明确 Trigger、Snapshots、Services、RunMetadata、Logs 五类上下文入口
- [x] 清理节点层对 UI 门面对象的直接认知
- [x] 将 FlowExecutor 从无参子图执行改成带触发上下文的流程实例执行

### H. VisionController 拆分

- [x] 把 VisionController 拆成设备配置服务
- [x] 把 VisionController 拆成相机运行时服务
- [x] 把 VisionController 拆成串口运行时服务
- [x] 把 VisionController 拆成流程编排服务或更薄的应用外观

### I. UI 信息架构重做

- [x] CameraManagerForm 改成“相机资源管理”界面
- [x] SerialManagerForm 改成“通信资源管理”界面
- [x] TriggerEditorForm 改成“触发路由编辑器”并支持多流程绑定
- [x] FlowEditorForm 增加“流程被哪些触发器绑定”的可视化入口
- [x] 增加运行时诊断界面，能追踪触发、快照、流程实例和输出

### J. 切换与清理

- [x] 删除旧的无参 TriggerSubGraph 路径
- [x] 删除旧的单目标触发器字段
- [x] 删除或替换 ImageFifo、CameraSlot、SerialSlot 的旧语义入口
- [x] 明确旧 flow 文件不兼容策略和新版本持久化格式


## 方案结论

这次不应该在现有结构上继续补丁式修修补补，而是直接做 V2 级别的破坏性重构。根因已经很明确：

1. 触发系统现在只传“启动信号”，不传“触发数据”，所以相机帧和串口消息都会在流程启动后被二次读取，天然存在时序错乱和竞争。
2. 相机软触发、硬触发走了两条不同路径，CameraAcquisitionNode.cs 里直接分叉，无法形成统一的 FIFO 和快照语义。
3. 设备配置、运行态、UI 编排、流程服务都被压在 VisionController.cs 里，导致相机、串口、触发、流程四套概念耦合在一起。
4. 现有触发器还是单触发器绑定单流程，TriggerEntry.cs 和 TriggerEditorForm.cs 都体现了这个限制，和你要的“一个触发器可驱动多个流程”直接冲突。

你已经确认三件事：允许放弃旧 flow 兼容，允许彻底拆分 VisionController，允许整体重做 UI。基于这个边界，推荐方案就是把系统改成“设备资源 + 触发路由 + 流程定义 + 运行实例”四层。

## 分阶段计划

1. 重建顶层数据模型。把 FlowGraph.cs 从当前的 CameraSlots、SerialSlots、Triggers 平铺结构，重构成新的项目模型，建议拆为 DeviceCatalog、TriggerCatalog、FlowDefinitions、FlowBindings。相机槽位和串口槽位都废弃，改为全局唯一的设备定义。

2. 重写相机采集架构。用新的帧缓冲和快照服务替代 ImageFifo.cs。目标不是单纯 FIFO，而是同时支持顺序消费、最新帧快照、同一帧多流程共享引用。硬触发由 SDK 监听线程写入缓冲并生成触发事件，软触发也必须走同一条链路：发出软件拍照命令，等待对应帧进入缓冲后返回。

3. 重写触发和流程执行。把 TriggerManager.cs 从“收到事件就调用某个子图 ID”改成真正的 TriggerRouter。每次触发都要生成 TriggerContext，携带源设备、时间、载荷、关联 ID、快照引用。再把 FlowExecutor.cs 改成基于 FlowInvocation 运行，而不是无参 TriggerSubGraph。

4. 重写流程上下文和节点契约。把 FlowContext.cs 从普通变量字典升级为执行作用域，至少包含 Trigger、Snapshots、Services、RunMetadata。相机节点重写后需要支持三种模式：消费触发帧、等待下一帧、软件触发并等待结果。串口接收节点建议取消后台监听职责，因为它现在和触发系统重复，SerialReceiveNode.cs 应降级为读取本次触发载荷或最近消息，而不是自己再挂事件。

5. 拆分应用服务层。把 VisionController.cs 拆成设备配置服务、相机运行时服务、串口运行时服务、流程编排服务。节点不再通过 dynamic 间接访问窗体层对象，而是走强类型服务接口，IFlowServiceProvider.cs 需要整体重做。

6. 整体重做 UI 信息架构。CameraManagerForm.cs 不再叫“槽位管理”，而是“相机资源管理”；SerialManagerForm.cs 改为“通信资源管理”；TriggerEditorForm.cs 改为“触发路由编辑器”，必须支持一个触发器绑定多个流程，并配置共享快照、并发策略、失败隔离、自动响应；FlowEditorForm.cs 要显示流程被哪些触发器绑定，以及新的手动触发入口。

7. 切换与清理。最终删除或废弃旧入口，包括 CameraSlot.cs、SerialSlot.cs、ImageFifo.cs、TriggerEntry.cs 的单目标字段，以及 FlowExecutor.cs 里的无参子图触发路径。

## 关键验证

1. 一台相机硬触发，同时绑定两个流程，两条流程都拿到同一个帧快照引用，图像一致，不发生 FIFO 竞争。
2. 相机节点设为软件触发模式时，节点阻塞等待对应帧返回，成功、超时、取消三条路径都可观测。
3. 一个串口触发器绑定多个流程，文本或字节载荷进入 TriggerContext，流程内节点可直接读取，不再二次监听串口。
4. 删除或修改设备定义时，UI 必须列出受影响的触发器和流程，不能静默破坏配置。
5. 运行时要能用关联 ID 串起“触发来源 -> 帧快照 -> 流程实例 -> 输出结果”。

会话计划已经保存到 session plan。这个方案的核心不是“把相机节点改聪明一点”，而是把系统的主语从“槽位”改成“设备资源和触发事件”。如果你认可，我下一步建议继续把它细化成一份实施蓝图，按“先改模型、再改运行时、再改节点、最后改 UI”的顺序拆成具体落地任务。
