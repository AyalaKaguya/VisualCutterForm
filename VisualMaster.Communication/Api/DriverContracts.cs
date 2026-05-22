using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace VisualMaster.Communication.Api
{
    public interface ICommunicationBlock
    {
        CommunicationBlockConfig Config { get; }
        byte[] CurrentValue { get; }
        event EventHandler<CommunicationBlockUpdatedEventArgs> Updated;
        Task<byte[]> ReadAsync(int timeoutMs, CancellationToken cancellationToken);
        Task WriteAsync(byte[] data, int timeoutMs, CancellationToken cancellationToken);
        void UpdateConfig(CommunicationBlockConfig config);
    }

    public interface ICommunicationDriver : IDisposable
    {
        string DeviceId { get; }
        string DriverName { get; }
        bool IsEnabled { get; set; }
        bool IsConnected { get; }
        IReadOnlyList<ICommunicationBlock> Blocks { get; }
        event EventHandler<CommunicationBlockUpdatedEventArgs> BlockUpdated;

        void Initialize(CommunicationDeviceConfig config);
        Task ConnectAsync(CancellationToken cancellationToken);
        Task PollAsync(CancellationToken cancellationToken);
        Task ReconnectAsync(CancellationToken cancellationToken);
        Task CloseAsync();
        ICommunicationBlock CreateBlock(CommunicationBlockConfig config);
        void UpdateBlock(CommunicationBlockConfig config);
        void RemoveBlock(string blockId);
        UserControl CreateConfigurationView();
    }

    public interface ICommunicationDriverFactory
    {
        string DriverName { get; }
        string DisplayName { get; }
        CommunicationDeviceConfig CreateDefaultConfig(IReadOnlyList<ICommunicationDriver> existingDevices);
        ICommunicationDriver CreateDriver();
        UserControl CreateConfigurationView(CommunicationDeviceConfig config);
    }
}
