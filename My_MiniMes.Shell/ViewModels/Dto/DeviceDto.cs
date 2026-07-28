using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace My_MiniMes.Shell.ViewModels.Dto
{
    /// <summary>
    /// 用于前端 UI 绑定的设备数据传输对象。
    /// 继承了 ObservableObject，确保这里面任何属性的修改都能立刻通知 WPF 的界面刷新。
    /// </summary>
    public partial class DeviceDto : ObservableObject
    {
        /// <summary>设备 ID</summary>
        [ObservableProperty] private int _deviceId;

        /// <summary>设备名称，双向绑定到界面</summary>
        [ObservableProperty] private string _deviceName = string.Empty;

        /// <summary>TCP 连接用的 IP 地址</summary>
        [ObservableProperty] private string _ipAddress = string.Empty;

        /// <summary>TCP 连接用的端口</summary>
        [ObservableProperty] private int? _port;

        /// <summary>RTU 连接用的串口名，如 COM3</summary>
        [ObservableProperty] private string _serialPort = string.Empty;

        /// <summary>Modbus 从站标识符</summary>
        [ObservableProperty] private byte? _slaveId;

        /// <summary>串口通信波特率</summary>
        [ObservableProperty] private int _baudRate = 9600;

        /// <summary>串口通信数据位</summary>
        [ObservableProperty] private int _dataBits = 8;

        /// <summary>串口通信停止位</summary>
        [ObservableProperty] private int _stopBits = 1;

        /// <summary>串口通信校验位</summary>
        [ObservableProperty] private int _parity = 0;

        /// <summary>当前设备状态（运行、故障、离线等），状态变化会触发前端图标变色</summary>
        [ObservableProperty] private string _deviceState = "断连";

        /// <summary>数据最后刷新时间</summary>
        [ObservableProperty] private DateTime _lastUpdateTime;

        // ==========================================
        // 以下是实时遥测数据，来自 Modbus 后台采集
        // ==========================================

        /// <summary>实时温度数据 (通过 Modbus 0号寄存器采集)，双向绑定驱动图表</summary>
        [ObservableProperty] private double _temperature;

        /// <summary>实时压力数据 (通过 Modbus 1号寄存器采集)，双向绑定驱动图表</summary>
        [ObservableProperty] private double _pressure;
    }
}
