using CommunityToolkit.Mvvm.ComponentModel;
using My_MiniMes.Shell.Models;
using System;
using System.Collections.Generic;

namespace My_MiniMes.Shell.ViewModels.Dto
{
    /// <summary>
    /// 订单看板和订单维护共用的前端订单对象。
    /// 继承 ObservableObject，后续设备产量变化时可以实时刷新进度和状态。
    /// </summary>
    public partial class OrderDto : ObservableObject
    {
        /// <summary>数据库主键。</summary>
        [ObservableProperty]
        private int _id;

        /// <summary>订单号，例如 PO-20231024-001。</summary>
        [ObservableProperty]
        private string _orderNo = string.Empty;

        /// <summary>产品名称。</summary>
        [ObservableProperty]
        private string _productName = string.Empty;

        /// <summary>客户名称。</summary>
        [ObservableProperty]
        private string _customerName = string.Empty;

        /// <summary>目标生产数量。</summary>
        [ObservableProperty]
        private int _targetQuantity;

        /// <summary>已生产数量。</summary>
        [ObservableProperty]
        private int _producedQuantity;

        /// <summary>订单状态：0 待生产，1 生产中，2 已完成。</summary>
        [ObservableProperty]
        private int _status;

        /// <summary>调度到的设备 ID，未调度时为空。</summary>
        [ObservableProperty]
        private int? _assignedDeviceId;

        /// <summary>交付截止日期。</summary>
        [ObservableProperty]
        private DateTime _deadline;

        /// <summary>订单创建时间。</summary>
        [ObservableProperty]
        private DateTime _createTime;

        /// <summary>调度设备名称，由设备表关联后填充。</summary>
        [ObservableProperty]
        private string _assignedDeviceName = string.Empty;

        /// <summary>
        /// 状态的中文显示文本。
        /// </summary>
        public string StatusText => OrderStatusCatalog.GetDisplayName(Status);

        /// <summary>
        /// 生产进度百分比，范围限制在 0 到 100。
        /// </summary>
        public int ProgressPercent
        {
            get
            {
                if (TargetQuantity <= 0) return 0;
                var percent = ProducedQuantity * 100.0 / TargetQuantity;
                return Math.Min(100, Math.Max(0, (int)Math.Round(percent)));
            }
        }

        /// <summary>
        /// 进度文本，例如 15000 / 50000。
        /// </summary>
        public string ProgressText => $"{ProducedQuantity:N0} / {TargetQuantity:N0}";

        /// <summary>
        /// 是否已经逾期。已完成订单不视为逾期。
        /// </summary>
        public bool IsOverdue => Status != OrderStatusCatalog.Completed && Deadline < DateTime.Now;

        /// <summary>
        /// 设备显示文本，未调度时显示“未调度”。
        /// </summary>
        public string DeviceDisplay => string.IsNullOrWhiteSpace(AssignedDeviceName) ? "未调度" : AssignedDeviceName;

        /// <summary>
        /// 交付期限的显示文本。
        /// </summary>
        public string DeadlineText => Deadline.ToString("yyyy-MM-dd");

        /// <summary>
        /// 已生产数量变化时，同步刷新进度相关显示属性。
        /// </summary>
        partial void OnProducedQuantityChanged(int value)
        {
            OnPropertyChanged(nameof(ProgressPercent));
            OnPropertyChanged(nameof(ProgressText));
        }

        /// <summary>
        /// 目标数量变化时，同步刷新进度相关显示属性。
        /// </summary>
        partial void OnTargetQuantityChanged(int value)
        {
            OnPropertyChanged(nameof(ProgressPercent));
            OnPropertyChanged(nameof(ProgressText));
        }

        /// <summary>
        /// 状态变化时，同步刷新状态文本和逾期标记。
        /// </summary>
        partial void OnStatusChanged(int value)
        {
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(IsOverdue));
        }

        /// <summary>
        /// 将数据库订单实体转换为前端展示对象，并补充设备名称。
        /// </summary>
        /// <param name="model">数据库订单实体。</param>
        /// <param name="deviceNames">设备 ID 到设备名称的映射。</param>
        /// <returns>前端展示订单对象。</returns>
        public static OrderDto FromModel(OrderModel model, IReadOnlyDictionary<int, string> deviceNames)
        {
            deviceNames.TryGetValue(model.AssignedDeviceId ?? 0, out var deviceName);

            return new OrderDto
            {
                Id = model.Id,
                OrderNo = model.OrderNo,
                ProductName = model.ProductName,
                CustomerName = model.CustomerName,
                TargetQuantity = model.TargetQuantity,
                ProducedQuantity = model.ProducedQuantity,
                Status = model.Status,
                AssignedDeviceId = model.AssignedDeviceId,
                Deadline = model.Deadline,
                CreateTime = model.CreateTime,
                AssignedDeviceName = deviceName ?? string.Empty
            };
        }
    }
}
