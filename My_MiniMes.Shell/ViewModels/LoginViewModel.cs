using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dapper;
using Microsoft.Data.Sqlite;
using My_MiniMes.Shell.Models;
using My_MiniMes.Shell.Services;
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

        /// <summary>
        /// 确认密码
        /// </summary>
        [ObservableProperty]
        private string _passwordConfirm = "";

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

            if (string.IsNullOrWhiteSpace(Account) || string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("账号或密码不能为空！");
                return;
            }

            string sql = @"select Account,PasswordHash from Users where Account = @Account and IsActive = 1";
            using var conn = new SqliteConnection(DatabaseInitializer.ConnectionString);
            var user = await conn.QueryFirstOrDefaultAsync<UserModel>(sql, new
            {
                Account = Account
            });

            if(user == null)
            {
                MessageBox.Show("用户不存在");
                return;
            }

            if (user.Account == Account && user.PasswordHash == Password)
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
        /// 用户注册
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task RegisterAsync()
        {
            if (string.IsNullOrWhiteSpace(Account) || string.IsNullOrWhiteSpace(Password)
                || string.IsNullOrWhiteSpace(PasswordConfirm))
            {
                MessageBox.Show("账号和密码不能为空");
            }
            if (!(Password==PasswordConfirm))
            {
                MessageBox.Show("两次密码不一致");
            }
            //先查询账号是否存在
            using var conn = new SqliteConnection(DatabaseInitializer.ConnectionString);
            var existUser = await conn.QueryFirstOrDefaultAsync<UserModel>(
                @"select * from Users where Account = @Account", new
                {
                    Account = Account
                });
            if(existUser != null)
            {
                MessageBox.Show("该用户已存在");
            }
            string sql = @"insert into Users(UserName,Account,IsActive,PasswordHash,Salt) 
                         values(@UserName,@Account,@IsActive,@PasswordHash,@Salt)";
            int success = await conn.ExecuteAsync(sql, new { UserName = "操作人员", Account = Account, 
                IsActive = 1, PasswordHash = Password,Salt = Guid.NewGuid().ToString()
            });
            if(success >= 0)
            {
                MessageBox.Show("注册成功");
                GoLogin();
                return;
            }
            MessageBox.Show("注册失败");
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
