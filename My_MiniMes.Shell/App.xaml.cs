using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using My_MiniMes.Shell.ViewModels;
using My_MiniMes.Shell.Views;
using My_MiniMes.Shell.Services;
using System.Windows;
using System.Threading;

namespace My_MiniMes.Shell
{
    /// <summary>
    /// 程序的入口。
    /// 我们拦截了默认的启动行为（在 App.xaml 中删除了 StartupUri="MainWindow.xaml"），
    /// 并在下面接管了一切，包括配置依赖注入(DI)、控制页面跳转流程（先登录，后主页）。
    /// </summary>
    public partial class App : Application
    {
        // 全局通用的“主机后台 (Host)”，它负责管理所有的依赖注入(DI)、配置和日志。
        //在WPF整个生命周期跑后台任务
        // 这也是 ASP.NET Core 和现代企业级 .NET 项目的标准基座。
        public static IHost AppHost { get; private set; }

        public App()
        {
            // 初始化主机并注册依赖服务
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // 【什么是依赖注入 (DI)？】
                    // 以前你需要自己写：var loginVm = new LoginViewModel(); var loginWindow = new LoginWindow(loginVm);
                    // 现在，你把所有需要的类都“注册”给这个容器 (services)。
                    // 当你想要一个 LoginWindow 时，容器会自动帮你去找 LoginWindow 需要什么（它发现需要 LoginViewModel），然后自动帮你 new 好送过来。

                    // ==========================================
                    // 1. 注册所有的前端界面 (Views / Windows)
                    // ==========================================
                    
                    // AddSingleton (单例模式): 整个程序运行期间只创建一个实例。主窗口全局唯一，所以用 Singleton。
                    services.AddSingleton<MainWindow>();     
                    
                    // AddTransient (瞬态模式): 每次向容器要的时候，都创建一个全新的实例。
                    // 登录窗口用完就会被销毁，下次再要时应该是个新的，所以用 Transient。
                    services.AddTransient<LoginWindow>();    

                    services.AddSingleton<IDataRepository, SqliteDataRepository>();
                    // 将 ModbusPollingService 作为单例注册，并通过 AddHostedService 交给主机托管其生命周期(以支持优雅停机)
                    services.AddSingleton<ModbusPollingService>();
                    services.AddHostedService(provider => provider.GetRequiredService<ModbusPollingService>());

                    // ==========================================
                    // 2. 注册所有的视图模型 (ViewModels)
                    // ==========================================
                    services.AddTransient<LoginViewModel>(); 
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<MonitorViewModel>();
                })
                .Build();
        }

        /// <summary>
        /// 程序启动时触发的方法
        /// 我们重写这个方法，来实现先显示 LoginWindow，成功后再显示 MainWindow 的逻辑。
        /// </summary>
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 第一步：初始化本地 SQLite 数据库（自动建表和插入默认数据）
            DatabaseInitializer.Initialize();

            // 启动后台服务环境（比如后续接入 Serilog 日志收集、后台定时任务，都是通过这句启动的）
            await AppHost!.StartAsync();

            // 【极其关键的修复】
            // WPF 的默认退出模式是 "OnLastWindowClose"（最后一个窗口关闭时退出程序）。
            // 当我们的 LoginWindow 被关闭时，WPF 发现当前没有任何打开的窗口了，就会立马启动自毁程序(Shutdown)。
            // 此时下面的代码再去尝试设置 ShutdownMode，就会引发“正在关闭时无法设置”的报错。
            // 解决方案：在弹出登录框前，先暂时剥夺 WPF 的自动关闭权力，改成“除非我显式调用 Shutdown，否则绝对不许退出”。
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            //尝试从 DI 容器获取 LoginWindow
            var loginWindow = AppHost.Services.GetRequiredService<LoginWindow>();
            
            // ShowDialog() 方法会以“模态对话框”的方式打开窗口。
            //代码会停在这一行不往下走，直到登录窗口被关闭！
            // loginResult 会接收到 LoginWindow.xaml.cs 里面赋给 `this.DialogResult` 的值。
            var loginResult = loginWindow.ShowDialog();

            // ====================================================
            // 流程控制 2：根据登录结果决定接下来的行动
            // ====================================================
            if (loginResult == true)
            {
                // 如果 DialogResult 是 true，说明密码验证通过了。
                
                
                var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
                
                // 显示主界面
                mainWindow.Show();

                // 在用户登录成功并进入主界面后，开启 Modbus 轮询服务的执行开关
                var modbusService = AppHost.Services.GetRequiredService<ModbusPollingService>();
                modbusService.StartPolling();

                // 登录成功并且主界面成功呼出后，把程序的退出模式改回正常的“最后一个主窗口关闭时自动退出”。
                ShutdownMode = ShutdownMode.OnLastWindowClose;
            }
            else
            {
                // 如果用户没有点登录按钮，而是直接点了右上角的 [X] 关闭了登录框。
                // 此时 loginResult 默认是 false 或 null。我们直接调用 Shutdown() 结束程序进程。
                Shutdown();
            }
        }

        /// <summary>
        /// 程序完全退出时触发的方法
        /// </summary>
        protected override async void OnExit(ExitEventArgs e)
        {
            var modbusService = AppHost.Services.GetRequiredService<ModbusPollingService>();
            modbusService.StopPolling();

            // 优雅地停止主机，释放所有占用的内存、文件句柄、数据库连接池等。
            await AppHost!.StopAsync();
            AppHost.Dispose();

            base.OnExit(e);
        }
    }
}
