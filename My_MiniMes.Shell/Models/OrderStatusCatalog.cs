using System.Collections.Generic;

namespace My_MiniMes.Shell.Models
{
    /// <summary>
    /// 订单状态的集中定义，避免业务代码和 XAML 中散落魔法数字。
    /// 数据库中只保存整数状态，界面显示文本统一从这里获取。
    /// </summary>
    public static class OrderStatusCatalog
    {
        /// <summary>待生产：订单已创建，但尚未调度到设备。</summary>
        public const int Pending = 0;

        /// <summary>生产中：订单已经调度到指定设备并开始执行。</summary>
        public const int InProgress = 1;

        /// <summary>已完成：订单达到目标产量，或由用户显式完成。</summary>
        public const int Completed = 2;

        /// <summary>
        /// 将数据库中的状态值转换为用户可读的中文文本。
        /// </summary>
        /// <param name="status">订单状态值。</param>
        /// <returns>状态显示文本。</returns>
        public static string GetDisplayName(int status)
        {
            return status switch
            {
                Pending => "待生产",
                InProgress => "生产中",
                Completed => "已完成",
                _ => "未知状态"
            };
        }

        /// <summary>
        /// 返回订单维护界面可绑定的状态选项。
        /// Key 是数据库状态值，Value 是下拉框显示的文本。
        /// </summary>
        public static IReadOnlyList<KeyValuePair<int, string>> GetOptions()
        {
            return new[]
            {
                new KeyValuePair<int, string>(Pending, GetDisplayName(Pending)),
                new KeyValuePair<int, string>(InProgress, GetDisplayName(InProgress)),
                new KeyValuePair<int, string>(Completed, GetDisplayName(Completed))
            };
        }
    }
}
