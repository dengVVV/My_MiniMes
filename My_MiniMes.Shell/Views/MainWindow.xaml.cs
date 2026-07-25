using My_MiniMes.Shell.ViewModels;
using System.Windows;

namespace My_MiniMes.Shell.Views
{
    public partial class MainWindow : Window
    {
        // 同样，通过构造函数注入 MainViewModel
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }
    }
}