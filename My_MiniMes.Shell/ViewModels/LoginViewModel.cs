using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace My_MiniMes.Shell.ViewModels
{
    /// <summary>
    /// 登录窗口的视图模型 (ViewModel)。负责处理登录界面的所有业务逻辑和数据绑定。
    /// 
    /// 【核心概念 1：ObservableObject 与 partial】
    /// 1. 继承 ObservableObject: 这是 CommunityToolkit.Mvvm 提供的一个轻量级基类，
    ///    等同于 Prism 框架中的 BindableBase。它内部实现了 INotifyPropertyChanged 接口，用于通知 UI 刷新数据。
    /// 2. partial 关键字: 这是必须的！微软使用了“源生成器 (Source Generators)”技术。
    ///    当你敲下代码并保存时，编译器会在后台自动生成另一半的类代码，把你的逻辑补全。如果没有 partial，源生成器就会报错。
    /// </summary>
    public partial class LoginViewModel : ObservableObject
    {
        /// <summary>
        /// 自定义一个事件。
        /// 为什么要有这个事件？因为 ViewModel 不应该知道 View (窗口) 的存在，这是 MVVM 严格解耦的要求。
        /// ViewModel 不能直接写 "window.Close()"。
        /// 所以 ViewModel 只负责“大喊”一声 (Invoke 事件)："我登录成功啦！"
        /// 而窗口 (View) 的后台代码 (LoginWindow.xaml.cs) 负责监听这个声音，听到了就自己把窗口关掉。
        /// </summary>
        public event Action? LoginSucceeded;

        /// <summary>
        /// 【核心概念 2：[ObservableProperty] 魔法标签】
        /// 在传统 WPF 或 Prism 中，为了让 UI 知道数据改变了，你需要手写一长串代码：
        /// private string _account;
        /// public string Account { 
        ///     get => _account; 
        ///     set { SetProperty(ref _account, value); } // Prism 的写法
        /// }
        /// 
        /// 现在，你只需要写一个私有字段（必须是小写字母或下划线开头），并在上面打上 [ObservableProperty] 标签。
        /// 编译器会在后台自动帮你生成上面那一长串的大写 "Account" 属性代码。前端 XAML 直接绑定大写的 {Binding Account} 即可。
        /// </summary>
        [ObservableProperty] 
        private string _account = "";

        // 自动生成大写的 Password 属性供前端绑定
        [ObservableProperty] 
        private string _password = "";

        // 用于控制 MaterialDesign 框架中的 Transitioner (翻转动画组件) 显示哪一页。
        // 0 代表第一页(登录界面)，1 代表第二页(注册界面)。
        [ObservableProperty] 
        private int _slideIndex = 0; 

        /// <summary>
        /// 【核心概念 3：[RelayCommand] 魔法标签】
        /// 在 Prism 中，如果要在前端按钮点击时触发一个方法，你需要写 DelegateCommand，在构造函数里 new 它，非常繁琐。
        /// 
        /// 这里的 [RelayCommand] 标签，会自动把下面的 LoginAsync() 方法，包装成一个名为 "LoginCommand" 的公开命令。
        /// 前端 XAML 里的按钮直接写 Command="{Binding LoginCommand}" 即可触发这个方法。
        /// 
        /// 注意它的命名规律：方法名叫 LoginAsync，生成的命令叫 LoginCommand；
        /// 如果方法名叫 GoRegister，生成的命令叫 GoRegisterCommand。
        /// </summary>
        [RelayCommand]
        private async Task LoginAsync()
        {
            // await Task.Delay(500); 这是一个异步延迟，用来模拟真实的程序去数据库查询时耗费的 0.5 秒。
            // 这样能保证 UI 在这 0.5 秒内不会卡死冻结，这也是企业级软件避免卡顿的标准做法。
            await Task.Delay(500); 

            // 第一层校验：如果不填账号密码就点登录，直接拦住
            if (string.IsNullOrWhiteSpace(Account) || string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("账号或密码不能为空！");
                return;
            }

            // 第二层校验：账号密码对比。
            // 目前还没有连数据库，所以我们写死判断 account=="admin" 并且 password=="123456" 才算成功。
            // 以后这里会被替换成：bool isOk = await _dbService.VerifyUser(Account, Password);
            if (Account == "admin" && Password == "123456")
            {
                // 如果账号密码正确，触发 LoginSucceeded 事件，通知外面的 Window "你可以关闭了"
                LoginSucceeded?.Invoke(); 
            }
            else
            {
                // 账号密码错误，弹窗提示用户
                MessageBox.Show("账号密码错误！(测试账号: admin/123456)");
            }
        }

        /// <summary>
        /// 当用户点击 "去注册" 按钮时触发。
        /// 把 SlideIndex 设为 1，前端的 Transitioner 收到通知后，会自动播放动画翻转到注册界面。
        /// </summary>
        [RelayCommand]
        private void GoRegister() => SlideIndex = 1;

        /// <summary>
        /// 当用户在注册界面点击 "去登录" 按钮时触发。
        /// 翻转回登录界面。
        /// </summary>
        [RelayCommand]
        private void GoLogin() => SlideIndex = 0;
    }
}
