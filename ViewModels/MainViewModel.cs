using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using IndustrialHmiDemo.Models;
using IndustrialHmiDemo.Services;

namespace IndustrialHmiDemo.ViewModels;

/// <summary>
/// 主界面的业务逻辑。
/// 这里集中处理连接、采集、告警、日志、导出，不直接操作界面控件。
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private const int MaxSamples = 120;
    private const int MaxLogs = 120;

    private readonly IDeviceClient _deviceClient;
    private readonly CsvExportService _csvExportService;
    private CancellationTokenSource? _collectCts;

    private string _deviceAddress = "SIM://PLC-01";
    private double _samplingIntervalMs = 800;
    private bool _isConnected;
    private bool _isCollecting;
    private double _temperature;
    private double _pressure;
    private double _vibration;
    private int _speed;
    private PointCollection _temperatureChartPoints = new();

    public MainViewModel()
        : this(new SimulatedDeviceClient(), new CsvExportService())
    {
    }

    public MainViewModel(IDeviceClient deviceClient, CsvExportService csvExportService)
    {
        _deviceClient = deviceClient;
        _csvExportService = csvExportService;

        ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => !IsConnected);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync, () => IsConnected);
        StartCollectCommand = new AsyncRelayCommand(StartCollectAsync, () => IsConnected && !IsCollecting);
        StopCollectCommand = new RelayCommand(StopCollect, () => IsCollecting);
        ExportCommand = new AsyncRelayCommand(ExportAsync, () => Samples.Count > 0);

        AddLog("系统就绪。可点击“连接设备”开始演示。");
    }

    public ObservableCollection<DeviceSnapshot> Samples { get; } = new();

    public ObservableCollection<AlarmItem> Alarms { get; } = new();

    public ObservableCollection<string> Logs { get; } = new();

    public AsyncRelayCommand ConnectCommand { get; }

    public AsyncRelayCommand DisconnectCommand { get; }

    public AsyncRelayCommand StartCollectCommand { get; }

    public RelayCommand StopCollectCommand { get; }

    public AsyncRelayCommand ExportCommand { get; }

    public string DeviceAddress
    {
        get => _deviceAddress;
        set => SetProperty(ref _deviceAddress, value);
    }

    public double SamplingIntervalMs
    {
        get => _samplingIntervalMs;
        set => SetProperty(ref _samplingIntervalMs, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (SetProperty(ref _isConnected, value))
            {
                OnPropertyChanged(nameof(ConnectionState));
                OnPropertyChanged(nameof(StatusBrush));
                RefreshCommandStates();
            }
        }
    }

    public bool IsCollecting
    {
        get => _isCollecting;
        private set
        {
            if (SetProperty(ref _isCollecting, value))
            {
                OnPropertyChanged(nameof(ConnectionState));
                OnPropertyChanged(nameof(StatusBrush));
                RefreshCommandStates();
            }
        }
    }

    public double Temperature
    {
        get => _temperature;
        private set => SetProperty(ref _temperature, value);
    }

    public double Pressure
    {
        get => _pressure;
        private set => SetProperty(ref _pressure, value);
    }

    public double Vibration
    {
        get => _vibration;
        private set => SetProperty(ref _vibration, value);
    }

    public int Speed
    {
        get => _speed;
        private set => SetProperty(ref _speed, value);
    }

    public int AlarmCount => Alarms.Count;

    public string ConnectionState
    {
        get
        {
            if (IsCollecting)
            {
                return "采集中";
            }

            return IsConnected ? "已连接" : "未连接";
        }
    }

    public Brush StatusBrush
    {
        get
        {
            if (IsCollecting)
            {
                return Brushes.LimeGreen;
            }

            return IsConnected ? Brushes.DeepSkyBlue : Brushes.OrangeRed;
        }
    }

    public PointCollection TemperatureChartPoints
    {
        get => _temperatureChartPoints;
        private set => SetProperty(ref _temperatureChartPoints, value);
    }

    private async Task ConnectAsync()
    {
        try
        {
            AddLog($"正在连接设备：{DeviceAddress}");
            await _deviceClient.ConnectAsync(DeviceAddress, CancellationToken.None);
            IsConnected = true;
            AddLog("设备连接成功。");
        }
        catch (Exception ex)
        {
            AddLog($"连接失败：{ex.Message}");
        }
    }

    private async Task DisconnectAsync()
    {
        StopCollect();
        await _deviceClient.DisconnectAsync();
        IsConnected = false;
        AddLog("设备已断开。");
    }

    private Task StartCollectAsync()
    {
        if (!IsConnected || IsCollecting)
        {
            return Task.CompletedTask;
        }

        _collectCts = new CancellationTokenSource();
        IsCollecting = true;
        AddLog("开始采集设备数据。");

        // 采集循环单独启动，不阻塞界面按钮响应。
        _ = CollectLoopAsync(_collectCts.Token);
        return Task.CompletedTask;
    }

    private void StopCollect()
    {
        if (!IsCollecting)
        {
            return;
        }

        _collectCts?.Cancel();
        _collectCts?.Dispose();
        _collectCts = null;
        IsCollecting = false;
        AddLog("采集已停止。");
    }

    private async Task CollectLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var snapshot = await _deviceClient.ReadSnapshotAsync(cancellationToken);
                AddSample(snapshot);
                EvaluateAlarms(snapshot);
                await Task.Delay(TimeSpan.FromMilliseconds(SamplingIntervalMs), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // 用户主动停止采集时会进入这里，不需要当作错误处理。
        }
        catch (Exception ex)
        {
            AddLog($"采集异常：{ex.Message}");
            IsCollecting = false;
        }
    }

    private void AddSample(DeviceSnapshot snapshot)
    {
        Samples.Insert(0, snapshot);

        while (Samples.Count > MaxSamples)
        {
            Samples.RemoveAt(Samples.Count - 1);
        }

        Temperature = snapshot.Temperature;
        Pressure = snapshot.Pressure;
        Vibration = snapshot.Vibration;
        Speed = snapshot.Speed;
        BuildTemperatureChart();
        ExportCommand.RaiseCanExecuteChanged();
    }

    private void EvaluateAlarms(DeviceSnapshot snapshot)
    {
        if (snapshot.Temperature >= 58)
        {
            AddAlarm("温度偏高", $"当前温度 {snapshot.Temperature:F1} ℃，建议检查冷却或负载情况。");
        }

        if (snapshot.Pressure >= 0.82)
        {
            AddAlarm("压力偏高", $"当前压力 {snapshot.Pressure:F2} MPa，建议检查管路阀门状态。");
        }

        if (snapshot.Vibration >= 2.2)
        {
            AddAlarm("振动偏高", $"当前振动 {snapshot.Vibration:F2} mm/s，建议检查轴承或固定结构。");
        }
    }

    private void AddAlarm(string title, string message)
    {
        // 简单去重：连续同类告警不刷屏。
        if (Alarms.FirstOrDefault()?.Title == title)
        {
            return;
        }

        Alarms.Insert(0, new AlarmItem(DateTime.Now, title, message));

        while (Alarms.Count > 30)
        {
            Alarms.RemoveAt(Alarms.Count - 1);
        }

        OnPropertyChanged(nameof(AlarmCount));
        AddLog($"告警：{title}");
    }

    private void BuildTemperatureChart()
    {
        var points = new PointCollection();
        var ordered = Samples.Reverse().Take(MaxSamples).ToList();

        if (ordered.Count == 0)
        {
            TemperatureChartPoints = points;
            return;
        }

        var min = Math.Min(35, ordered.Min(x => x.Temperature) - 2);
        var max = Math.Max(70, ordered.Max(x => x.Temperature) + 2);
        var width = 695d;
        var height = 150d;
        var left = 40d;
        var top = 20d;

        for (var index = 0; index < ordered.Count; index++)
        {
            var x = left + (ordered.Count == 1 ? 0 : index * width / (ordered.Count - 1));
            var ratio = (ordered[index].Temperature - min) / (max - min);
            var y = top + height - ratio * height;
            points.Add(new System.Windows.Point(x, y));
        }

        TemperatureChartPoints = points;
    }

    private async Task ExportAsync()
    {
        try
        {
            var folder = Path.Combine(AppContext.BaseDirectory, "exports");
            Directory.CreateDirectory(folder);
            var fileName = $"hmi-samples-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
            var path = Path.Combine(folder, fileName);

            await _csvExportService.ExportAsync(Samples.Reverse(), path, CancellationToken.None);
            AddLog($"CSV 导出成功：{path}");
        }
        catch (Exception ex)
        {
            AddLog($"CSV 导出失败：{ex.Message}");
        }
    }

    private void AddLog(string message)
    {
        Logs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");

        while (Logs.Count > MaxLogs)
        {
            Logs.RemoveAt(Logs.Count - 1);
        }
    }

    private void RefreshCommandStates()
    {
        ConnectCommand.RaiseCanExecuteChanged();
        DisconnectCommand.RaiseCanExecuteChanged();
        StartCollectCommand.RaiseCanExecuteChanged();
        StopCollectCommand.RaiseCanExecuteChanged();
        ExportCommand.RaiseCanExecuteChanged();
    }
}
