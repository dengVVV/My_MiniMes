using System;

namespace My_MiniMes.Shell.Models
{
    /// <summary>
    /// 订单实体类，对应数据库中的 Orders 表
    /// </summary>
    public class OrderModel
    {
        /// <summary>
        /// 自增主键 ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 订单号（业务上的唯一标识，如 PO-20231024-001）
        /// </summary>
        public string OrderNo { get; set; } = string.Empty;

        /// <summary>
        /// 产品名称
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// 客户名称
        /// </summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// 目标生产数量
        /// </summary>
        public int TargetQuantity { get; set; }

        /// <summary>
        /// 实际已生产数量（可由下位机反馈更新）
        /// </summary>
        public int ProducedQuantity { get; set; }

        /// <summary>
        /// 订单状态：
        /// 0 - 待生产 (Pending)
        /// 1 - 生产中 (In Progress)
        /// 2 - 已完成 (Completed)
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 绑定的下位机设备ID（如果是待生产状态，此值可为 null 或 0）
        /// 调度时即将该字段更新为实际执行的 Modbus 设备 ID
        /// </summary>
        public int? AssignedDeviceId { get; set; }

        /// <summary>
        /// 交货截止日期
        /// </summary>
        public DateTime Deadline { get; set; }

        /// <summary>
        /// 订单创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }
}
