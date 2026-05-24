using System.Collections.Generic;
using System.Threading;

namespace VisualMaster.CameraLink.API
{
    /// <summary>
    /// 相机厂商适配器接口。
    /// 每种相机品牌提供一个实现，负责 SDK 的初始化、相机扫描和设备实例化。
    /// </summary>
    public interface ICameraAdapter
    {
        /// <summary>适配器标识名称（e.g. "Hikrobot MVS"）。</summary>
        string AdapterName { get; }

        /// <summary>当前运行环境中适配器是否可用（SDK DLL 是否存在）。</summary>
        bool IsAvailable { get; }

        /// <summary>初始化 SDK 运行时（应在扫描前调用一次）。</summary>
        void InitializeSdk();

        /// <summary>释放 SDK 运行时。</summary>
        void FinalizeSdk();

        /// <summary>扫描当前网络/总线上所有可见的相机。</summary>
        IReadOnlyList<DiscoveredCamera> Scan(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>根据已发现的相机描述创建并返回一个设备驱动实例（尚未 Open）。</summary>
        ICameraDeviceDriver CreateDevice(DiscoveredCamera discovered);

        /// <summary>判断此适配器是否能打开指定的已发现相机。</summary>
        bool CanHandle(DiscoveredCamera discovered);
    }
}
