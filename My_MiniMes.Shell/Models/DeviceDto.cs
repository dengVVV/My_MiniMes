using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace My_MiniMes.Shell.Models
{
    /// <summary>
    /// 继承ObservableObject是为了以后能实现实时刷新
    /// 当后台修改了某个设备的温度，界面可以瞬间自动改变
    /// </summary>
    public partial class DeviceDto : ObservableObject
    {
        [ObservableProperty]
        private int _deviceId;

        [ObservableProperty]
        private string _deviceName = string.Empty;

        [ObservableProperty]
        private string _ipAdress = string.Empty;

        [ObservableProperty]
        private string _deviceState = "运行"; //状态：运行，停机，故障

        [ObservableProperty]
        private double _temperature;

        [ObservableProperty]
        private double _pressure;

    }
}
