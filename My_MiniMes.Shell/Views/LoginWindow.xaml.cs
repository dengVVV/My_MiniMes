using My_MiniMes.Shell.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace My_MiniMes.Shell.Views
{
    public partial class LoginWindow : Window
    {
        // 构造函数注入 LoginViewModel
        public LoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();

            // [Prism 对比] Prism中有 ViewModelLocator.AutoWireViewModel="True"，能自动把ViewModel跟View绑在一起。
            // 这种不依赖重型框架的标准做法，就是我们在构造函数中把 ViewModel 接过来，然后手动赋值给 DataContext。
            this.DataContext = viewModel;

            // 监听登录成功事件，关闭窗口并返回结果
            viewModel.LoginSucceeded += () =>
            {
                this.DialogResult = true;
                this.Close();
            };

            // 去掉了原生边框，需要加这个代码才能让鼠标左键拖拽窗口
            this.MouseLeftButtonDown += (s, e) => { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); };
        }

        // 最小化按钮点击事件
        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // 关闭按钮点击事件
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            // 如果用户直接点 X 关闭窗口，而不是正常登录，我们要把 DialogResult 设为 false
            this.DialogResult = false;
            this.Close();
        }
    }
}
