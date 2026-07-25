using System;

namespace My_MiniMes.Shell.Models
{
    /// <summary>
    /// 设备实体类 (数据库映射模型)
    /// 注意：它和 ViewModel 里的 DeviceDto 不同，这里没有任何 ObservableProperty，只用于通过 Dapper 进行数据库存取。
    /// </summary>
    public class DeviceModel
    {
        public int DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int? Port { get; set; }
        public string SerialPort { get; set; } = string.Empty;
        public byte? SlaveId { get; set; }
        public string DeviceState { get; set; } = string.Empty;
        public DateTime LastUpdateTime { get; set; }
    }
}
