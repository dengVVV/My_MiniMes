namespace My_MiniMes.Shell.Models
{
    /// <summary>
    /// 订单统计结果，用于数据看板顶部指标和订单分页加载时的总数判断。
    /// </summary>
    public class OrderStatistics
    {
        /// <summary>符合当前筛选条件的订单总数。</summary>
        public int TotalCount { get; set; }

        /// <summary>待生产订单数。</summary>
        public int PendingCount { get; set; }

        /// <summary>生产中订单数。</summary>
        public int InProgressCount { get; set; }

        /// <summary>已完成订单数。</summary>
        public int CompletedCount { get; set; }

        /// <summary>逾期订单数。</summary>
        public int OverdueCount { get; set; }
    }
}
