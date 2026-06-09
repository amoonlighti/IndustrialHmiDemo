using IndustrialHmiDemo.Models;

namespace IndustrialHmiDemo.Services;

/// <summary>
/// 模拟设备客户端。
/// 真实上位机项目里，这个类可以替换成 SerialPort、TcpClient、Modbus 或厂家 SDK 实现。
/// </summary>
public sealed class SimulatedDeviceClient : IDeviceClient
{
    private readonly Random _random = new();
    private DateTime _startedAt;
    private string _address = "SIM://PLC-01";

    public bool IsConnected { get; private set; }

    public async Task ConnectAsync(string address, CancellationToken cancellationToken)
    {
        // 模拟连接耗时，例如打开串口、建立 TCP 连接、握手登录等。
        await Task.Delay(450, cancellationToken);
        _address = string.IsNullOrWhiteSpace(address) ? "SIM://PLC-01" : address.Trim();
        _startedAt = DateTime.Now;
        IsConnected = true;
    }

    public Task DisconnectAsync()
    {
        IsConnected = false;
        return Task.CompletedTask;
    }

    public async Task<DeviceSnapshot> ReadSnapshotAsync(CancellationToken cancellationToken)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("设备未连接，无法读取数据。");
        }

        // 模拟协议往返延迟。真实项目中这里会读报文、校验 CRC、解析寄存器数据。
        await Task.Delay(80, cancellationToken);

        var seconds = (DateTime.Now - _startedAt).TotalSeconds;
        var wave = Math.Sin(seconds / 4.0);
        var slowWave = Math.Cos(seconds / 7.0);

        var temperature = 48 + wave * 9 + _random.NextDouble() * 2.4;
        var pressure = 0.72 + slowWave * 0.08 + _random.NextDouble() * 0.03;
        var vibration = 1.4 + Math.Abs(wave) * 0.9 + _random.NextDouble() * 0.25;
        var speed = 1450 + (int)(slowWave * 90) + _random.Next(-18, 18);

        // 让模拟地址参与一下逻辑，方便演示不同设备地址会影响随机趋势。
        if (_address.Contains("HOT", StringComparison.OrdinalIgnoreCase))
        {
            temperature += 12;
        }

        return new DeviceSnapshot(
            DateTime.Now,
            Math.Round(temperature, 1),
            Math.Round(pressure, 2),
            Math.Round(vibration, 2),
            speed,
            MotorRunning: true);
    }
}
