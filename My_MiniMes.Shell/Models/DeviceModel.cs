using System;

namespace My_MiniMes.Shell.Models
{
    /// <summary>
    /// 设备实体类 (数据库映射模型)
    /// 注意：它和 ViewModel 里的 DeviceDto 不同，这里没有任何 ObservableProperty，只用于通过 Dapper 进行数据库存取。
    /// </summary>
    public class DeviceModel
    {
        /// <summary>
        /// 设备的唯一标识 ID，数据库自增主键
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// 设备名称（如 "1号注塑机"）
        /// </summary>
        public string DeviceName { get; set; } = string.Empty;

        /// <summary>
        /// 设备的 IP 地址，如果配置了此项，系统优先通过 Modbus TCP 连接
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 网络连接端口，TCP 模式下有效（通常 Modbus TCP 默认端口为 502）
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// 串口名称（如 "COM1" 或 "/dev/ttyS0"），如果 IP 为空且配置了此项，系统走 Modbus RTU
        /// </summary>
        public string? SerialPort { get; set; }

        /// <summary>
        /// Modbus 从站 ID (Slave ID/Unit ID)，用于区分同一总线下的不同设备
        /// </summary>
        public byte? SlaveId { get; set; }
        
        /// <summary>
        /// 通信波特率（仅 RTU 模式有效，默认 9600）
        /// </summary>
        public int BaudRate { get; set; } = 9600;

        /// <summary>
        /// 数据位（仅 RTU 模式有效，默认 8）
        /// </summary>
        public int DataBits { get; set; } = 8;

        /// <summary>
        /// 停止位（仅 RTU 模式有效，0=None, 1=One, 2=Two, 3=OnePointFive）
        /// </summary>
        public int StopBits { get; set; } = 1;

        /// <summary>
        /// 校验位（仅 RTU 模式有效，0=None, 1=Odd奇校验, 2=Even偶校验, 3=Mark, 4=Space）
        /// </summary>
        public int Parity { get; set; } = 0;

        /// <summary>
        /// 设备的当前运行状态 ("运行", "停机", "故障", "断连", "离线")
        /// </summary>
        public string DeviceState { get; set; } = "离线";

        /// <summary>
        /// 最后一次成功轮询获取数据的时间
        /// </summary>
        public DateTime LastUpdateTime { get; set; }
    }
}
