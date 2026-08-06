using My_MiniMes.Shell.Models;

namespace My_MiniMes.Shell.ViewModels.Dto
{
    /// <summary>
    /// 订单按调度设备分类查询时使用的筛选选项。
    /// 包含“全部订单”、每一台设备，以及“尚未调度”分类。
    /// </summary>
    public sealed class DeviceFilterOption
    {
        /// <summary>下拉框显示文本。</summary>
        public string DisplayName { get; }

        /// <summary>筛选的设备 ID；尚未调度时为 null。</summary>
        public int? DeviceId { get; }

        /// <summary>是否为“全部订单”选项。</summary>
        public bool IsAll { get; }

        private DeviceFilterOption(string displayName, int? deviceId, bool isAll)
        {
            DisplayName = displayName;
            DeviceId = deviceId;
            IsAll = isAll;
        }

        /// <summary>全部订单选项，用于取消设备分类筛选。</summary>
        public static DeviceFilterOption All { get; } = new("全部订单", null, true);

        /// <summary>尚未调度订单选项。</summary>
        public static DeviceFilterOption Unassigned { get; } = new("尚未调度", null, false);

        /// <summary>
        /// 根据设备创建对应的订单分类选项。
        /// </summary>
        /// <param name="device">设备实体。</param>
        /// <returns>设备分类筛选选项。</returns>
        public static DeviceFilterOption ForDevice(DeviceModel device)
        {
            return new DeviceFilterOption(device.DeviceName, device.DeviceId, false);
        }
    }
}
