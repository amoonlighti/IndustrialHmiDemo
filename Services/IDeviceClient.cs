using IndustrialHmiDemo.Models;

namespace IndustrialHmiDemo.Services;

/// <summary>
/// 设备通信接口。
/// 初学者可以理解为“上位机和设备之间的适配层”：界面不关心底层是串口还是 TCP。
/// </summary>
public interface IDeviceClient
{
    bool IsConnected { get; }

    Task ConnectAsync(string address, CancellationToken cancellationToken);

    Task DisconnectAsync();

    Task<DeviceSnapshot> ReadSnapshotAsync(CancellationToken cancellationToken);
}
