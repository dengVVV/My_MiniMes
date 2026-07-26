using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using My_MiniMes.Shell.Services;
using My_MiniMes.Shell.ViewModels.Dto;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace My_MiniMes.Shell.ViewModels
{
    public partial class MonitorViewModel : ObservableObject, IDisposable
    {
        private readonly IDataRepository _repository;
        private readonly ModbusPollingService _modbusService;

        [ObservableProperty]
        private ObservableCollection<DeviceDto> _deviceList = new();

        [ObservableProperty]
        private DeviceDto? _selectedDevice;

        [ObservableProperty]
        private int _runningCount;
        [ObservableProperty]
        private int _disconnectedCount;
        [ObservableProperty]
        private int _faultCount;
        [ObservableProperty]
        private int _disabledCount;

        // =====================================
        // LiveCharts2 图表绑定数据源
        // =====================================
        
        // 存储用于画图的点集合 (X:时间, Y:数值)
        private readonly ObservableCollection<DateTimePoint> _temperatureValues = new();
        private readonly ObservableCollection<DateTimePoint> _pressureValues = new();

        [ObservableProperty]
        private ObservableCollection<ISeries> _chartSeries = new();

        [ObservableProperty]
        private Axis[] _xAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private Axis[] _yAxes = Array.Empty<Axis>();

        public MonitorViewModel(IDataRepository repository, ModbusPollingService modbusService)
        {
            _repository = repository;
            _modbusService = modbusService;

            // 1. 初始化图表外观风格 (暗色系风格，杜绝过于花哨的 AI 味)
            InitChartStyle();

            // 2. 异步加载设备初始列表
            _ = LoadDevicesAsync();

            // 3. 订阅底层真实 Modbus 传来的数据事件
            _modbusService.DeviceDataUpdated += OnDeviceDataUpdated;
        }

        private void InitChartStyle()
        {
            // 配置两条折线
            ChartSeries = new ObservableCollection<ISeries>
            {
                new LineSeries<DateTimePoint>
                {
                    Values = _temperatureValues,
                    Name = "温度 (℃)",
                    // 工业沉稳蓝
                    Stroke = new SolidColorPaint(SKColor.Parse("#3b82f6")) { StrokeThickness = 3 },
                    GeometrySize = 0,
                    Fill = new SolidColorPaint(SKColor.Parse("#3b82f6").WithAlpha(40)), // 半透明面积填充
                    LineSmoothness = 0.65 // 让曲线更顺滑
                },
                new LineSeries<DateTimePoint>
                {
                    Values = _pressureValues,
                    Name = "压力 (Bar)",
                    // 警示橙色
                    Stroke = new SolidColorPaint(SKColor.Parse("#f97316")) { StrokeThickness = 3 },
                    GeometrySize = 0,
                    Fill = null,
                    LineSmoothness = 0.65
                }
            };

            // 配置 X 轴 (时间轴)
            XAxes = new[]
            {
                new Axis
                {
                    Labeler = value => new DateTime((long)value).ToString("HH:mm:ss"),
                    LabelsRotation = 0,
                    TextSize = 12,
                    LabelsPaint = new SolidColorPaint(SKColors.LightGray),
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#2e3440")) { StrokeThickness = 1 } // 深色网格线
                }
            };

            // 配置 Y 轴 (数值轴)
            YAxes = new[]
            {
                new Axis
                {
                    TextSize = 12,
                    LabelsPaint = new SolidColorPaint(SKColors.LightGray),
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#2e3440")) { StrokeThickness = 1 }
                }
            };
        }

        private async Task LoadDevicesAsync()
        {
            var dbDevices = await _repository.GetAllDevicesAsync();
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                DeviceList.Clear();
                foreach (var d in dbDevices)
                {
                    DeviceList.Add(new DeviceDto
                    {
                        DeviceId = d.DeviceId,
                        DeviceName = d.DeviceName,
                        IpAddress = d.IpAddress,
                        Port = d.Port,
                        SerialPort = d.SerialPort,
                        SlaveId = d.SlaveId,
                        DeviceState = d.DeviceState,
                        LastUpdateTime = d.LastUpdateTime
                    });
                }
                UpdateStats();
                
                // 默认选中第一台设备以展示图表
                if (DeviceList.Any())
                {
                    SelectedDevice = DeviceList.First();
                }
            });
        }

        private void OnDeviceDataUpdated(object? sender, DeviceDataUpdatedEventArgs e)
        {
            // Modbus 轮询是在后台线程触发的，必须切回主线程更新 UI
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 1. 找到对应的设备并更新其状态和数值
                var device = DeviceList.FirstOrDefault(x => x.DeviceId == e.DeviceId);
                if (device == null) return;

                if (e.IsOnline)
                {
                    device.DeviceState = "运行";
                    device.Temperature = e.Temperature;
                    device.Pressure = e.Pressure;
                    device.LastUpdateTime = DateTime.Now;

                    // 2. 如果这台设备恰好是当前正在看的那一台，那么把它画到图表上
                    if (SelectedDevice != null && SelectedDevice.DeviceId == e.DeviceId)
                    {
                        var now = DateTime.Now;
                        _temperatureValues.Add(new DateTimePoint(now, e.Temperature));
                        _pressureValues.Add(new DateTimePoint(now, e.Pressure));

                        // 保持图表最多显示最近的 60 个点，实现向左滑动的平滑滚动效果
                        if (_temperatureValues.Count > 60) _temperatureValues.RemoveAt(0);
                        if (_pressureValues.Count > 60) _pressureValues.RemoveAt(0);
                    }
                }
                else
                {
                    // 若掉线，且当前状态不是本来就停机的，标记为断连
                    device.DeviceState = device.DeviceState == "停机" || device.DeviceState == "故障" ? device.DeviceState : "断连";
                }

                UpdateStats();
            });
        }

        private void UpdateStats()
        {
            RunningCount = DeviceList.Count(x => x.DeviceState == "运行");
            DisconnectedCount = DeviceList.Count(x => x.DeviceState == "断连");
            FaultCount = DeviceList.Count(x => x.DeviceState == "故障");
            DisabledCount = DeviceList.Count(x => x.DeviceState == "停机");
        }

        partial void OnSelectedDeviceChanged(DeviceDto? value)
        {
            // 当用户点击切换左侧设备卡片时，清空当前图表的历史折线，重新开始绘制新设备的点
            _temperatureValues.Clear();
            _pressureValues.Clear();
        }

        public void Dispose()
        {
            if (_modbusService != null)
            {
                _modbusService.DeviceDataUpdated -= OnDeviceDataUpdated;
            }
        }
    }
}
