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
    /// 订单维护 ViewModel：负责订单新增、编辑、删除、调度和完成操作。
    /// 列表采用分页加载，首次只加载一页，滚动到底部时再加载下一页。
    /// </summary>
    public partial class OrderMaintenanceViewModel : ObservableObject
    {
        /// <summary>每页加载的订单数量。</summary>
        private const int PageSize = 50;

        private readonly IDataRepository _repository;

        /// <summary>当前设备列表，用于设备名称映射。</summary>
        private List<DeviceModel> _devices = new();

        /// <summary>已加载的订单 DTO 集合，防止重复添加。</summary>
        private List<OrderDto> _loadedOrders = new();

        /// <summary>当前分页偏移量。</summary>
        private int _currentOffset;

        /// <summary>当前筛选条件下是否还有更多数据。</summary>
        private bool _hasMoreItems = true;

        /// <summary>是否正在加载下一页，避免滚动事件重复触发查询。</summary>
        private bool _isLoadingMore;

        /// <summary>分页版本号，筛选条件变化时使旧查询失效。</summary>
        private int _loadVersion;

        /// <summary>当前筛选条件下的订单总数，用于判断分页是否结束。</summary>
        private int _totalCount;

        /// <summary>设置筛选条件时用于抑制 OnSelectedDeviceFilterChanged 重复加载。</summary>
        private bool _suppressFilterAutoLoad;

        /// <summary>订单维护列表，当前显示的是已加载的分页数据。</summary>
        [ObservableProperty]
        private ObservableCollection<OrderDto> _orders = new();

        /// <summary>当前选中的订单。</summary>
        [ObservableProperty]
        private OrderDto? _selectedOrder;

        /// <summary>正在编辑的订单实体，供新增/编辑弹窗双向绑定。</summary>
        [ObservableProperty]
        private OrderModel _editingOrder = new();

        /// <summary>可参与调度的设备列表。</summary>
        [ObservableProperty]
        private ObservableCollection<DeviceModel> _availableDevices = new();

        /// <summary>订单维护中当前选中的调度设备。</summary>
        [ObservableProperty]
        private DeviceModel? _selectedDispatchDevice;

        /// <summary>按调度设备分类的筛选选项。</summary>
        [ObservableProperty]
        private ObservableCollection<DeviceFilterOption> _deviceFilterOptions = new();

        /// <summary>当前选中的设备分类筛选条件。</summary>
        [ObservableProperty]
        private DeviceFilterOption? _selectedDeviceFilter;

        /// <summary>订单状态下拉框选项。</summary>
        public IReadOnlyList<KeyValuePair<int, string>> StatusOptions { get; } = OrderStatusCatalog.GetOptions();

        public OrderMaintenanceViewModel(IDataRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// 刷新订单列表，重置分页并从第一页开始加载。
        /// </summary>
        public async Task RefreshAsync()
        {
            var devices = await _repository.GetAllDevicesAsync();
            _devices = devices.ToList();

            var previousFilter = SelectedDeviceFilter;
            var previousOrderId = SelectedOrder?.Id;
            var previousDeviceId = SelectedDispatchDevice?.DeviceId;

            Application.Current.Dispatcher.Invoke(() =>
            {
                AvailableDevices.Clear();
                foreach (var device in _devices)
                {
                    AvailableDevices.Add(device);
                }

                DeviceFilterOptions.Clear();
                DeviceFilterOptions.Add(DeviceFilterOption.All);
                foreach (var device in _devices)
                {
                    DeviceFilterOptions.Add(DeviceFilterOption.ForDevice(device));
                }
                DeviceFilterOptions.Add(DeviceFilterOption.Unassigned);

                _suppressFilterAutoLoad = true;
                SelectedDeviceFilter = DeviceFilterOptions.FirstOrDefault(
                    option => option.IsAll == previousFilter?.IsAll &&
                              option.DeviceId == previousFilter?.DeviceId) ?? DeviceFilterOptions.FirstOrDefault();
                _suppressFilterAutoLoad = false;
            });

            await ResetAndLoadAsync();

            Application.Current.Dispatcher.Invoke(() =>
            {
                SelectedOrder = Orders.FirstOrDefault(order => order.Id == previousOrderId) ?? Orders.FirstOrDefault();
                SelectedDispatchDevice = AvailableDevices.FirstOrDefault(device => device.DeviceId == previousDeviceId) ?? AvailableDevices.FirstOrDefault();
            });
        }

        /// <summary>
        /// 筛选条件变化时重置分页并重新加载。
        /// </summary>
        partial void OnSelectedDeviceFilterChanged(DeviceFilterOption? value)
        {
            if (_suppressFilterAutoLoad) return;

            ResetPaging();
            _ = ResetAndLoadAsync();
        }

        /// <summary>
        /// 重置分页状态，并加载第一页。
        /// </summary>
        private async Task ResetAndLoadAsync()
        {
            ResetPaging();
            await LoadTotalCountAsync();
            await LoadNextPageAsync();
        }

        /// <summary>
        /// 重置分页偏移和已加载列表。
        /// </summary>
        private void ResetPaging()
        {
            _loadVersion++;
            _isLoadingMore = false;
            _currentOffset = 0;
            _hasMoreItems = true;
            _loadedOrders.Clear();
            Orders.Clear();
        }

        /// <summary>
        /// 获取当前筛选条件下的订单总数。
        /// </summary>
        private async Task LoadTotalCountAsync()
        {
            var filter = SelectedDeviceFilter;
            var deviceId = filter == null || filter.IsAll ? (int?)null : filter.DeviceId;
            var onlyUnassigned = filter != null && !filter.IsAll && filter.DeviceId == null;

            var count = await _repository.GetOrderCountAsync(deviceId, onlyUnassigned);

            Application.Current.Dispatcher.Invoke(() =>
            {
                _totalCount = count;
                _hasMoreItems = _loadedOrders.Count < _totalCount;
            });
        }

        /// <summary>
        /// 加载下一页订单，供前端滚动事件调用。
        /// </summary>
        public async Task LoadNextPageAsync()
        {
            if (_isLoadingMore || !_hasMoreItems) return;

            var version = _loadVersion;
            _isLoadingMore = true;

            try
            {
                var filter = SelectedDeviceFilter;
                var deviceId = filter == null || filter.IsAll ? (int?)null : filter.DeviceId;
                var onlyUnassigned = filter != null && !filter.IsAll && filter.DeviceId == null;

                var page = await _repository.GetOrdersPageAsync(_currentOffset, PageSize, deviceId, onlyUnassigned);
                var pageOrders = page.ToList();
                var deviceNames = _devices.ToDictionary(d => d.DeviceId, d => d.DeviceName);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    // 如果筛选条件已经变化，丢弃这次旧查询的结果
                    if (version != _loadVersion) return;

                    foreach (var order in pageOrders)
                    {
                        var dto = OrderDto.FromModel(order, deviceNames);
                        _loadedOrders.Add(dto);
                        Orders.Add(dto);
                    }

                    _currentOffset += pageOrders.Count;
                    _hasMoreItems = _loadedOrders.Count < _totalCount;

                    if (SelectedOrder == null)
                    {
                        SelectedOrder = Orders.FirstOrDefault();
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"订单维护分页加载失败: {ex.Message}");
            }
            finally
            {
                if (version == _loadVersion)
                {
                    _isLoadingMore = false;
                }
            }
        }

        /// <summary>
        /// 手动刷新订单列表。
        /// </summary>
        [RelayCommand]
        private async Task Refresh()
        {
            await RefreshAsync();
        }

        /// <summary>
        /// 打开新增订单弹窗，自动生成一个基于当前时间的订单号。
        /// </summary>
        [RelayCommand]
        private async Task AddOrder()
        {
            EditingOrder = new OrderModel
            {
                OrderNo = $"PO-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmmss}",
                Status = OrderStatusCatalog.Pending,
                Deadline = DateTime.Now.AddDays(7),
                TargetQuantity = 1
            };

            var dialog = new My_MiniMes.Shell.UserControls.OrderEditDialogView { DataContext = this };
            await MaterialDesignThemes.Wpf.DialogHost.Show(dialog, "RootDialog");
        }

        /// <summary>
        /// 打开编辑订单弹窗，将选中订单的数据回填到编辑对象。
        /// </summary>
        [RelayCommand]
        private async Task EditOrder()
        {
            if (SelectedOrder == null)
            {
                MessageBox.Show("请先选择需要编辑的订单。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            EditingOrder = new OrderModel
            {
                Id = SelectedOrder.Id,
                OrderNo = SelectedOrder.OrderNo,
                ProductName = SelectedOrder.ProductName,
                CustomerName = SelectedOrder.CustomerName,
                TargetQuantity = SelectedOrder.TargetQuantity,
                ProducedQuantity = SelectedOrder.ProducedQuantity,
                Status = SelectedOrder.Status,
                AssignedDeviceId = SelectedOrder.AssignedDeviceId,
                Deadline = SelectedOrder.Deadline,
                CreateTime = SelectedOrder.CreateTime
            };

            var dialog = new My_MiniMes.Shell.UserControls.OrderEditDialogView { DataContext = this };
            await MaterialDesignThemes.Wpf.DialogHost.Show(dialog, "RootDialog");
        }

        /// <summary>
        /// 保存新增或编辑的订单。
        /// 校验订单号、数量、重复订单号，并自动维护产量与状态的一致性。
        /// </summary>
        [RelayCommand]
        private async Task SaveOrder()
        {
            var order = EditingOrder;

            if (string.IsNullOrWhiteSpace(order.OrderNo) ||
                string.IsNullOrWhiteSpace(order.ProductName) ||
                string.IsNullOrWhiteSpace(order.CustomerName))
            {
                MessageBox.Show("订单号、产品名称和客户名称不能为空。", "校验失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (order.TargetQuantity <= 0)
            {
                MessageBox.Show("目标生产数量必须大于 0。", "校验失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (order.ProducedQuantity < 0)
            {
                MessageBox.Show("已生产数量不能小于 0。", "校验失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (await _repository.OrderNoExistsAsync(order.OrderNo, order.Id))
            {
                MessageBox.Show("当前订单号已经存在，请更换订单号。", "校验失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 业务规则：产量达到目标后自动完成；手动完成时补足目标产量
            if (order.ProducedQuantity >= order.TargetQuantity && order.Status != OrderStatusCatalog.Completed)
            {
                order.Status = OrderStatusCatalog.Completed;
            }

            if (order.Status == OrderStatusCatalog.Completed && order.ProducedQuantity < order.TargetQuantity)
            {
                order.ProducedQuantity = order.TargetQuantity;
            }

            if (order.Id == 0)
            {
                await _repository.InsertOrderAsync(order);
            }
            else
            {
                await _repository.UpdateOrderAsync(order);
            }

            MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
            await RefreshAsync();
        }

        /// <summary>
        /// 删除当前选中的订单。
        /// </summary>
        [RelayCommand]
        private async Task DeleteOrder()
        {
            if (SelectedOrder == null)
            {
                MessageBox.Show("请先选择需要删除的订单。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var message = SelectedOrder.Status == OrderStatusCatalog.InProgress
                ? "该订单正在生产中，删除后会中断当前调度记录，确认删除吗？"
                : "确认删除选中的订单吗？";

            var result = MessageBox.Show(message, "删除确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            await _repository.DeleteOrderAsync(SelectedOrder.Id);
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
    }
}
