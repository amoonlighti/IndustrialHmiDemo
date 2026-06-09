namespace IndustrialHmiDemo.Models;

/// <summary>
/// 告警记录。简历或面试时可以说明：告警规则可以改成配置化阈值。
/// </summary>
public sealed record AlarmItem(DateTime CreatedAt, string Title, string Message);
