using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using My_MiniMes.Shell.Models;
using My_MiniMes.Shell.Services;
using My_MiniMes.Shell.ViewModels.Dto;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace My_MiniMes.Shell.ViewModels
{
    /// <summary>
    /// 订单看板 ViewModel：负责订单统计、执行进度、订单调度和完成操作。
    /// 数据通过 IDataRepository 从 SQLite 读取，所有 UI 集合只允许在主线程修改。
    /// </summary>
    public partial class OrderBoardViewModel : ObservableObject
    {
        private readonly IDataRepository _repository;

        /// <summary>订单看板中的订单列表。</summary>
        [ObservableProperty]
        private ObservableCollection<OrderDto> _orders = new();

        /// <summary>当前选中的订单。</summary>
        [ObservableProperty]
        private OrderDto? _selectedOrder;

        /// <summary>待生产订单数量。</summary>
        [ObservableProperty]
        private int _pendingCount;

        /// <summary>生产中订单数量。</summary>
        [ObservableProperty]
        private int _inProgressCount;

        /// <summary>已完成订单数量。</summary>
        [ObservableProperty]
        private int _completedCount;

        /// <summary>逾期订单数量。</summary>
        [ObservableProperty]
        private int _overdueCount;

        /// <summary>可用于订单调度的设备列表。</summary>
        [ObservableProperty]
        private ObservableCollection<DeviceModel> _availableDevices = new();

        /// <summary>订单看板中当前选中的调度设备。</summary>
        [ObservableProperty]
        private DeviceModel? _selectedDispatchDevice;

        public OrderBoardViewModel(IDataRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// 从数据库重新加载订单和设备，并在主线程刷新集合。
        /// </summary>
        public async Task RefreshAsync()
        {
            var orders = await _repository.GetAllOrdersAsync();
            var devices = await _repository.GetAllDevicesAsync();
            var deviceNames = devices.ToDictionary(d => d.DeviceId, d => d.DeviceName);

            Application.Current.Dispatcher.Invoke(() =>
            {
                // 保留刷新前选中的订单和设备，避免每次刷新都跳回第一行
                var previousOrderId = SelectedOrder?.Id;
                var previousDeviceId = SelectedDispatchDevice?.DeviceId;

                Orders.Clear();
                foreach (var order in orders)
                {
                    Orders.Add(OrderDto.FromModel(order, deviceNames));
                }

                AvailableDevices.Clear();
                foreach (var device in devices)
                {
                    AvailableDevices.Add(device);
                }

                SelectedOrder = Orders.FirstOrDefault(o => o.Id == previousOrderId) ?? Orders.FirstOrDefault();
                SelectedDispatchDevice = AvailableDevices.FirstOrDefault(d => d.DeviceId == previousDeviceId) ?? AvailableDevices.FirstOrDefault();

                UpdateStats();
            });
        }

        /// <summary>
        /// 刷新看板数据。
        /// </summary>
        [RelayCommand]
        private async Task Refresh()
        {
            await RefreshAsync();
        }

        /// <summary>
        /// 将当前选中的订单调度到当前选中的设备。
        /// </summary>
        [RelayCommand]
        private async Task DispatchSelected()
        {
            if (SelectedOrder == null)
            {
                MessageBox.Show("请先选择需要调度的订单。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (SelectedDispatchDevice == null)
            {
                MessageBox.Show("请先选择目标设备。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (SelectedOrder.Status == OrderStatusCatalog.Completed)
            {
                MessageBox.Show("已完成订单不能再次调度。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await _repository.DispatchOrderAsync(SelectedOrder.Id, SelectedDispatchDevice.DeviceId);
            await RefreshAsync();
        }

        /// <summary>
        /// 完成当前选中的生产订单。
        /// </summary>
        [RelayCommand]
        private async Task CompleteSelected()
        {
            if (SelectedOrder == null)
            {
                MessageBox.Show("请先选择需要完成的订单。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (SelectedOrder.Status != OrderStatusCatalog.InProgress)
            {
                MessageBox.Show("只有生产中的订单才能执行完成操作。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await _repository.CompleteOrderAsync(SelectedOrder.Id);
            await RefreshAsync();
        }

        /// <summary>
        /// 根据当前订单集合重新统计看板顶部的四项指标。
        /// </summary>
        private void UpdateStats()
        {
            PendingCount = Orders.Count(o => o.Status == OrderStatusCatalog.Pending);
            InProgressCount = Orders.Count(o => o.Status == OrderStatusCatalog.InProgress);
            CompletedCount = Orders.Count(o => o.Status == OrderStatusCatalog.Completed);
            OverdueCount = Orders.Count(o => o.IsOverdue);
        }
    }
}
