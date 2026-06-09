# IndustrialHmiDemo

一个基于 C# / WPF 的工业上位机数据采集与监控 Demo，用于展示客户端开发、异步采集、告警处理和工程分层能力。

## 功能

- 模拟设备连接、断开和状态展示
- 异步实时采集温度、压力、振动、转速等设备数据
- 实时温度曲线展示
- 告警规则判断和告警列表展示
- 运行日志记录
- CSV 数据导出
- MVVM 分层：View、ViewModel、Model、Service
- 设备通信接口抽象：后续可替换为串口、Socket、Modbus 或厂家 SDK

## 技术栈

- C# / .NET 8
- WPF
- MVVM
- `INotifyPropertyChanged`
- `ICommand` / 异步命令
- `ObservableCollection`
- `CancellationTokenSource`
- CSV 文件导出

## 项目结构

```text
IndustrialHmiDemo
├─ Models             # 数据模型：采样数据、告警记录
├─ Services           # 设备通信模拟、CSV 导出
├─ ViewModels         # MVVM 绑定、命令、采集逻辑
├─ App.xaml
├─ MainWindow.xaml    # WPF 界面
└─ IndustrialHmiDemo.csproj
```

## 运行方式

需要 Windows + .NET 8 SDK。

```powershell
cd IndustrialHmiDemo
dotnet restore
dotnet run
```

也可以使用 Visual Studio 打开 `IndustrialHmiDemo.csproj` 后直接运行。

## 演示建议

1. 点击“连接设备”。
2. 点击“开始采集”。
3. 观察实时数据、曲线、日志和告警。
4. 点击“导出 CSV”，查看导出的采样数据。

## 可扩展方向

- 增加 SerialPort 串口通信实现
- 增加 Modbus TCP/RTU 通信实现
- 增加 SQLite / SQL Server 数据落库
- 增加参数配置页和告警阈值配置
- 增加 Serilog / NLog 文件日志
- 增加单元测试和 GitHub Actions 构建

## 技术说明

项目采用设备通信接口抽象，界面层不直接依赖具体通信协议。当前 `SimulatedDeviceClient` 用于模拟设备数据流，后续可以替换为串口、Socket、Modbus TCP/RTU 或厂家 SDK，同时保留现有的界面展示、告警处理和数据导出逻辑。
