using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace My_MiniMes.Shell.ViewModels
{
    /// <summary>
    /// 主界面的 ViewModel (核心中枢)。
    /// 负责管理页面路由、侧边栏状态、顶部账户信息等全局状态。
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        // ==========================================
        // 1. 页面路由 (导航) 相关属性
        // ==========================================
        
        /// <summary>
        /// 当前正显示在右侧核心区域的 ViewModel。
        /// 只要改变这个值，WPF 的 DataTemplate 就会自动将对应的界面渲染出来。
        /// </summary>
        [ObservableProperty]
        private object? _currentView;

        /// <summary>
        /// 顶部标题，随页面切换而动态改变
        /// </summary>
        [ObservableProperty]
        private string _pageTitle = "设备状态监控";

        // ==========================================
        // 2. 侧边栏与菜单状态控制属性
        // ==========================================

        /// <summary>
        /// 控制整个左侧侧边栏是否折叠（收起）
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MenuToggleToolTip))] // 当折叠状态改变时，连带更新提示文本
        private bool _isMenuCollapsed = false;

        public string MenuToggleToolTip => IsMenuCollapsed ? "展开侧边栏" : "收起侧边栏";

        /// <summary>
        /// 控制“生产订单”子菜单是否展开 (利用 ToggleButton 双向绑定)
        /// </summary>
        [ObservableProperty]
        private bool _isOrderMenuOpen = false;

        // ==========================================
        // 3. 顶部 Header 用户信息与设置状态
        // ==========================================

        /// <summary>
        /// 控制右上角设置的 Popup 是否弹出
        /// </summary>
        [ObservableProperty]
        private bool _isSettingsMenuOpen = false;

        [ObservableProperty]
        private string _currentUserName = "超级管理员";

        [ObservableProperty]
        private string _currentRoleGreeting = "你好, Admin";

        // ==========================================
        // 4. 依赖注入与初始化
        // ==========================================

        private readonly MonitorViewModel _monitorViewModel;
        // 等后续写了别的页面再继续注入，例如：
        // private readonly OrderBoardViewModel _orderBoardViewModel;

        public MainViewModel(MonitorViewModel monitorViewModel)
        {
            _monitorViewModel = monitorViewModel;
            
            // 默认启动页显示为设备监控
            ShowMonitor();
        }

        // ==========================================
        // 5. RelayCommands (供界面按钮绑定的命令)
        // ==========================================

        /// <summary>
        /// 汉堡包按钮触发：折叠/展开侧边栏
        /// </summary>
        [RelayCommand]
        private void ToggleMenu()
        {
            IsMenuCollapsed = !IsMenuCollapsed;
        }

        /// <summary>
        /// 导航：显示设备监控页面
        /// </summary>
        [RelayCommand]
        private void ShowMonitor()
        {
            PageTitle = "设备状态监控";
            CurrentView = _monitorViewModel;
        }

        /// <summary>
        /// 导航：显示生产订单 - 订单看板 (待实现)
        /// </summary>
        [RelayCommand]
        private void ShowOrderBoard()
        {
            PageTitle = "生产订单看板 (模块开发中...)";
            // CurrentView = _orderBoardViewModel;
        }

        /// <summary>
        /// 导航：显示生产订单 - 订单维护 (待实现)
        /// </summary>
        [RelayCommand]
        private void ShowOrderMaintenance()
        {
            PageTitle = "生产订单维护 (模块开发中...)";
        }

        /// <summary>
        /// 导航：显示数据报表 (待实现)
        /// </summary>
        [RelayCommand]
        private void ShowReport()
        {
            PageTitle = "系统数据报表 (模块开发中...)";
        }

        /// <summary>
        /// 退出登录功能
        /// 企业级做法通常是清空本地 Session，然后重启或者退回到登录页。
        /// </summary>
        [RelayCommand]
        private void SignOut()
        {
            // 关闭右上角的 Popup
            IsSettingsMenuOpen = false; 

            var result = MessageBox.Show("确定要退出当前账号并返回登录界面吗？", "退出确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                // 用重启程序的方式模拟退出登录。实际业务可能需要调用注销接口清理 Token
                System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
                Application.Current.Shutdown();
            }
        }
    }
}
