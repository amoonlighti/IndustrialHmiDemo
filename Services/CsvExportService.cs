using System.Globalization;
using System.IO;
using System.Text;
using IndustrialHmiDemo.Models;

namespace IndustrialHmiDemo.Services;

/// <summary>
/// CSV 导出服务。
/// 使用 UTF-8 BOM 是为了让 Excel 打开中文时不乱码。
/// </summary>
public sealed class CsvExportService
{
    public async Task ExportAsync(IEnumerable<DeviceSnapshot> samples, string filePath, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine("时间,温度,压力,振动,转速,电机状态");

        foreach (var item in samples)
        {
            cancellationToken.ThrowIfCancellationRequested();
            builder.AppendLine(string.Join(
                ",",
                item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                item.Temperature.ToString("F1", CultureInfo.InvariantCulture),
                item.Pressure.ToString("F2", CultureInfo.InvariantCulture),
                item.Vibration.ToString("F2", CultureInfo.InvariantCulture),
                item.Speed.ToString(CultureInfo.InvariantCulture),
                item.MotorState));
        }

        await File.WriteAllTextAsync(filePath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), cancellationToken);
    }
}
