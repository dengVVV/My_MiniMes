using System.Windows.Controls;
using My_MiniMes.Shell.ViewModels;

namespace My_MiniMes.Shell.Views
{
    /// <summary>
    /// 订单看板页面，仅承载 XAML 和初始化逻辑。
    /// </summary>
    public partial class OrderBoardView : UserControl
    {
        public OrderBoardView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 滚动接近底部时加载下一页数据。
        /// </summary>
        private void OrdersScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (DataContext is OrderBoardViewModel viewModel &&
                e.ExtentHeight > 0 &&
                e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 80)
            {
                _ = viewModel.LoadNextPageAsync();
            }
        }
    }
}
