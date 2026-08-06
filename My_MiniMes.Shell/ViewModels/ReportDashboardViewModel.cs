using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Win32;
using My_MiniMes.Shell.Models;
using My_MiniMes.Shell.Services;
using My_MiniMes.Shell.ViewModels.Dto;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace My_MiniMes.Shell.ViewModels
{
    /// <summary>
    /// 数据看板 ViewModel：负责汇总订单数据，并生成柱形图、折线图、饼图，以及导出 Excel。
    /// </summary>
    public partial class ReportDashboardViewModel : ObservableObject
    {
        private readonly IDataRepository _repository;

        /// <summary>当前看板使用的订单实体列表，导出 Excel 时使用。</summary>
        private List<OrderModel> _orderModels = new();

        /// <summary>当前看板使用的设备列表，用于设备和“尚未调度”分类统计。</summary>
        private List<DeviceModel> _devices = new();

        /// <summary>总订单数。</summary>
        [ObservableProperty]
        private int _totalOrderCount;

        /// <summary>目标生产总量。</summary>
        [ObservableProperty]
        private long _targetQuantity;

        /// <summary>已生产总量。</summary>
        [ObservableProperty]
        private long _producedQuantity;

        /// <summary>整体完成率文本。</summary>
        [ObservableProperty]
        private string _completionRateText = "0.0%";

        /// <summary>逾期订单数量。</summary>
        [ObservableProperty]
        private int _overdueCount;

        /// <summary>最近一次刷新时间。</summary>
        [ObservableProperty]
        private string _lastRefreshTime = string.Empty;

        /// <summary>柱形图数据源。</summary>
        [ObservableProperty]
        private ObservableCollection<ISeries> _barSeries = new();

        /// <summary>柱形图 X 轴。</summary>
        [ObservableProperty]
        private Axis[] _barXAxes = Array.Empty<Axis>();

        /// <summary>柱形图 Y 轴。</summary>
        [ObservableProperty]
        private Axis[] _barYAxes = Array.Empty<Axis>();

        /// <summary>折线图数据源。</summary>
        [ObservableProperty]
        private ObservableCollection<ISeries> _lineSeries = new();

        /// <summary>折线图 X 轴。</summary>
        [ObservableProperty]
        private Axis[] _lineXAxes = Array.Empty<Axis>();

        /// <summary>折线图 Y 轴。</summary>
        [ObservableProperty]
        private Axis[] _lineYAxes = Array.Empty<Axis>();

        /// <summary>饼图数据源。</summary>
        [ObservableProperty]
        private ObservableCollection<ISeries> _pieSeries = new();

        public ReportDashboardViewModel(IDataRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// 从数据库加载订单和设备，并重新生成统计指标和图表。
        /// </summary>
        public async Task RefreshAsync()
        {
            var orders = await _repository.GetAllOrdersAsync();
            var devices = await _repository.GetAllDevicesAsync();

            _orderModels = orders.ToList();
            _devices = devices.ToList();

            BuildStatistics();
            BuildCharts();

            LastRefreshTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 刷新数据看板。
        /// </summary>
        [RelayCommand]
        private async Task Refresh()
        {
            await RefreshAsync();
        }

        /// <summary>
        /// 将当前订单数据导出为 Excel 文件。
        /// </summary>
        [RelayCommand]
        private async Task ExportExcel()
        {
            if (_orderModels.Count == 0)
            {
                MessageBox.Show("暂无可导出的订单数据。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Excel 工作簿 (*.xlsx)|*.xlsx",
                FileName = $"数据看板_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                Title = "导出订单数据"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                var deviceNames = _devices.ToDictionary(d => d.DeviceId, d => d.DeviceName);
                await ExcelExportService.ExportOrdersAsync(_orderModels, deviceNames, dialog.FileName);

                MessageBox.Show($"导出成功：{dialog.FileName}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 根据订单数据计算顶部统计卡片。
        /// </summary>
        private void BuildStatistics()
        {
            TotalOrderCount = _orderModels.Count;
            TargetQuantity = _orderModels.Sum(o => (long)o.TargetQuantity);
            ProducedQuantity = _orderModels.Sum(o => (long)o.ProducedQuantity);
            OverdueCount = _orderModels.Count(o => o.Status != OrderStatusCatalog.Completed && o.Deadline < DateTime.Now);

            var rate = TargetQuantity <= 0
                ? 0
                : ProducedQuantity * 100.0 / TargetQuantity;

            CompletionRateText = $"{rate:F1}%";
        }

        /// <summary>
        /// 构建柱形图、折线图和饼图。
        /// </summary>
        private void BuildCharts()
        {
            BuildDeviceBarChart();
            BuildProductLineChart();
            BuildStatusPieChart();
        }

        /// <summary>
        /// 柱形图：各设备分配的订单数量，包含“尚未调度”。
        /// </summary>
        private void BuildDeviceBarChart()
        {
            var labels = _devices.Select(d => d.DeviceName).ToList();
            var values = _devices
                .Select(d => (double)_orderModels.Count(o => o.AssignedDeviceId == d.DeviceId))
                .ToList();

            var unassignedCount = _orderModels.Count(o => o.AssignedDeviceId == null);
            if (unassignedCount > 0)
            {
                labels.Add("尚未调度");
                values.Add(unassignedCount);
            }

            if (labels.Count == 0)
            {
                labels.Add("暂无数据");
                values.Add(0);
            }

            BarSeries = new ObservableCollection<ISeries>
            {
                new ColumnSeries<double>
                {
                    Values = values,
                    Name = "订单数量",
                    Fill = new SolidColorPaint(SKColor.Parse("#409EFF"))
                }
            };

            BarXAxes = new[]
            {
                new Axis
                {
                    Labels = labels,
                    LabelsRotation = 15,
                    TextSize = 12,
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#606266")),
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#E4E7ED")) { StrokeThickness = 1 }
                }
            };

            BarYAxes = new[]
            {
                new Axis
                {
                    MinLimit = 0,
                    TextSize = 12,
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#606266")),
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#E4E7ED")) { StrokeThickness = 1 }
                }
            };
        }

        /// <summary>
        /// 折线图：各产品的目标产量与已生产产量对比。
        /// </summary>
        private void BuildProductLineChart()
        {
            var groups = _orderModels
                .GroupBy(o => o.ProductName)
                .Select(g => new
                {
                    Name = g.Key,
                    Target = g.Sum(o => (long)o.TargetQuantity),
                    Produced = g.Sum(o => (long)o.ProducedQuantity)
                })
                .ToList();

            var labels = groups.Select(g => g.Name).ToList();
            var targetValues = groups.Select(g => (double)g.Target).ToList();
            var producedValues = groups.Select(g => (double)g.Produced).ToList();

            if (labels.Count == 0)
            {
                labels.Add("暂无数据");
                targetValues.Add(0);
                producedValues.Add(0);
            }

            LineSeries = new ObservableCollection<ISeries>
            {
                new LineSeries<double>
                {
                    Values = targetValues,
                    Name = "目标产量",
                    Stroke = new SolidColorPaint(SKColor.Parse("#409EFF")) { StrokeThickness = 3 },
                    GeometrySize = 8,
                    Fill = null,
                    LineSmoothness = 0.5
                },
                new LineSeries<double>
                {
                    Values = producedValues,
                    Name = "已生产产量",
                    Stroke = new SolidColorPaint(SKColor.Parse("#67C23A")) { StrokeThickness = 3 },
                    GeometrySize = 8,
                    Fill = null,
                    LineSmoothness = 0.5
                }
            };

            LineXAxes = new[]
            {
                new Axis
                {
                    Labels = labels,
                    LabelsRotation = 15,
                    TextSize = 12,
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#606266")),
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#E4E7ED")) { StrokeThickness = 1 }
                }
            };

            // 计算目标产量和已生产产量中的最大值，并把 Y 轴上限向上抬高 20%，让折线图在垂直方向有更多余量
            var verticalMax = targetValues.Concat(producedValues).DefaultIfEmpty(0).Max();
            var lineYMax = verticalMax <= 0 ? 10 : Math.Ceiling(verticalMax * 1.2);

            LineYAxes = new[]
            {
                new Axis
                {
                    MinLimit = 0,
                    MaxLimit = lineYMax,
                    TextSize = 12,
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#606266")),
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#E4E7ED")) { StrokeThickness = 1 }
                }
            };
        }

        /// <summary>
        /// 饼图：订单状态分布。
        /// </summary>
        private void BuildStatusPieChart()
        {
            var pending = _orderModels.Count(o => o.Status == OrderStatusCatalog.Pending);
            var inProgress = _orderModels.Count(o => o.Status == OrderStatusCatalog.InProgress);
            var completed = _orderModels.Count(o => o.Status == OrderStatusCatalog.Completed);

            PieSeries = new ObservableCollection<ISeries>
            {
                new PieSeries<double>
                {
                    Values = new[] { (double)pending },
                    Name = "待生产",
                    Fill = new SolidColorPaint(SKColor.Parse("#E6A23C"))
                },
                new PieSeries<double>
                {
                    Values = new[] { (double)inProgress },
                    Name = "生产中",
                    Fill = new SolidColorPaint(SKColor.Parse("#409EFF"))
                },
                new PieSeries<double>
                {
                    Values = new[] { (double)completed },
                    Name = "已完成",
                    Fill = new SolidColorPaint(SKColor.Parse("#67C23A"))
                }
            };
        }
    }
}
