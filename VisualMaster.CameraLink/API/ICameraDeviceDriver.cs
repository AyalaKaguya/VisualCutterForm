using System;
using System.Collections.Generic;
using System.Drawing;
using VisualMaster.Api;

namespace VisualMaster.CameraLink.API
{
    /// <summary>
    /// 单个相机设备驱动接口，由各厂商适配器实现。
    /// 负责与 SDK 直接交互，对外暴露统一的操作契约。
    /// </summary>
    public interface ICameraDeviceDriver : IDisposable
    {
        /// <summary>相机的硬件唯一标识（通常为序列号）。</summary>
        string UniqueHardwareId { get; }

        /// <summary>相机已打开（SDK 连接建立）。</summary>
        bool IsOpen { get; }

        /// <summary>相机当前正在采集（抓取循环已启动）。</summary>
        bool IsGrabbing { get; }

        // ── 生命周期 ───────────────────────────────────────────────
        /// <summary>打开相机连接。</summary>
        void Open();

        /// <summary>关闭相机连接并释放 SDK 资源。</summary>
        void Close();

        // ── 采集控制 ──────────────────────────────────────────────
        /// <summary>启动抓取循环（相机开始向帧缓冲区推送图像）。</summary>
        void StartGrabbing();

        /// <summary>停止抓取循环。</summary>
        void StopGrabbing();

        /// <summary>向软件触发相机发送一次取像信号。</summary>
        void TriggerSoftware();

        /// <summary>阻塞等待下一帧（适用于软件触发模式）。</summary>
        bool TryGrabImage(out Bitmap bitmap, int timeoutMs);

        // ── 配置 ──────────────────────────────────────────────────
        /// <summary>将 CameraSettings 写入相机 SDK 参数。</summary>
        void ApplySettings(CameraSettings settings);

        /// <summary>从相机 SDK 读取当前参数并返回（首次接入时使用）。</summary>
        CameraSettings ReadSettingsFromDevice();

        /// <summary>获取相机支持的像素格式列表。</summary>
        string[] GetAvailablePixelFormats();
        /// <summary>获取相机支持的触发源列表。</summary>
        string[] GetAvailableTriggerSources();
        /// <summary>将设备信息转换为 Api 层通用的 CameraInfo。</summary>
        CameraInfo ToCameraInfo();

        // ── 事件 ──────────────────────────────────────────────────
        /// <summary>相机采集到一帧时触发（由抓取循环线程调用）。</summary>
        event EventHandler<FrameAcquiredEventArgs> FrameAcquired;

        /// <summary>相机连接意外断开时触发。</summary>
        event EventHandler Disconnected;
    }
}
