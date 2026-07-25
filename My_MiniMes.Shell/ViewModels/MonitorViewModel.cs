using CommunityToolkit.Mvvm.ComponentModel;

namespace My_MiniMes.Shell.ViewModels
{
    // 子页面的 ViewModel
    public partial class MonitorViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _welcomeMessage = "设备监控大屏数据正在接入... (来自 MonitorViewModel)";
    }
}
