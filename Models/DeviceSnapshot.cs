namespace IndustrialHmiDemo.Models;

/// <summary>
/// 一次设备采样结果。
/// 在真实项目中，这类数据通常来自串口、Socket、Modbus、OPC 或设备 SDK。
/// </summary>
public sealed record DeviceSnapshot(
    DateTime Timestamp,
    double Temperature,
    double Pressure,
    double Vibration,
    int Speed,
    bool MotorRunning)
{
    public string MotorState => MotorRunning ? "运行" : "停止";
}
