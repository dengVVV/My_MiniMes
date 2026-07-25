using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace My_MiniMes.Shell.ViewModels
{
    /// <summary>
    /// 主界面的 ViewModel (核心中枢)。
    /// 它的作用就像是一个“路由器”，负责管理当前主屏幕到底应该显示哪个子页面。
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        /// <summary>
        /// 【核心概念：企业级 SPA (单页应用) 的路由原理】
        /// 在 Prism 中，你可能会用 RegionManager.RequestNavigate("MainRegion", "MonitorView") 来切换页面。
        /// 但是在不需要那么庞大的框架时，最原汁原味、最优雅的做法就是使用这个 CurrentView 属性。
        /// 
        /// 1. CurrentView 声明为一个 object (或者 ViewModel 基类)。
        /// 2. 前端 MainWindow.xaml 有一个 <ContentControl Content="{Binding CurrentView}" />
        /// 3. 当你把 CurrentView = _monitorViewModel 时，WPF 就会去查找资源字典里，有没有说明书告诉它怎么渲染 MonitorViewModel。
        /// 4. MainWindow 的 <DataTemplate> 就是这本说明书，它告诉 WPF 把 MonitorViewModel 渲染成 MonitorView。
        /// </summary>
        [ObservableProperty]
        private object? _currentView;

        // 页面的顶部标题，随着切换左侧菜单而动态改变
        [ObservableProperty]
        private string _pageTitle = "首页";

        // ==========================================
        // 依赖注入 (DI) 的私有字段，用来保存所有子页面的 ViewModel
        // ==========================================
        private readonly MonitorViewModel _monitorViewModel;
        // 未来如果加了订单模块：
        // private readonly OrderViewModel _orderViewModel;

        /// <summary>
        /// 构造函数。
        /// 当 App.xaml.cs 中 DI 容器 (AppHost.Services.GetRequiredService) 尝试创建 MainViewModel 时，
        /// 容器会发现：MainViewModel 需要一个 MonitorViewModel。
        /// 容器会自动先去创建一个 MonitorViewModel，然后再塞到这里来！这就是依赖注入 (DI) 的强大之处，全自动。
        /// </summary>
        /// <param name="monitorViewModel">被 DI 容器自动注入进来的设备监控模块 ViewModel</param>
        public MainViewModel(MonitorViewModel monitorViewModel)
        {
            _monitorViewModel = monitorViewModel;
            
            // 程序一启动，默认先调用 ShowMonitor 方法，把“设备监控”设为默认主页
            ShowMonitor();
        }

        /// <summary>
        /// 绑定给“设备监控”按钮的命令。
        /// 当点击左侧菜单的“设备监控”时，触发此方法。
        /// </summary>
        [RelayCommand]
        private void ShowMonitor()
        {
            PageTitle = "设备监控概览";
            
            // 魔法在这里发生！
            // 只要把 _monitorViewModel 赋给 CurrentView，触发了属性改变通知，
            // 界面上的 ContentControl 就会瞬间把内部的 UI 替换成监控大屏的 UI。
            CurrentView = _monitorViewModel; 
        }

        /// <summary>
        /// 绑定给“生产订单”按钮的命令。
        /// </summary>
        [RelayCommand]
        private void ShowOrder()
        {
            PageTitle = "生产订单 (开发中)";
            
            // 目前因为还没有开发 OrderViewModel，所以这里先空着。
            // 等你写了 OrderViewModel，把它的实例赋给 CurrentView，界面就能瞬间切过去。
            // CurrentView = _orderViewModel;
        }
    }
}
