using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace My_MiniMes.Shell.ViewModels.Dto
{
    public partial class DeviceDto : ObservableObject
    {
        [ObservableProperty] private int _deviceId;
        [ObservableProperty] private string _deviceName = string.Empty;
        [ObservableProperty] private string _ipAddress = string.Empty;
        [ObservableProperty] private int? _port;
        [ObservableProperty] private string _serialPort = string.Empty;
        [ObservableProperty] private byte? _slaveId;
        [ObservableProperty] private string _deviceState = "断连";
        [ObservableProperty] private DateTime _lastUpdateTime;

        // 实时遥测数据
        [ObservableProperty] private double _temperature;
        [ObservableProperty] private double _pressure;
    }
}
