using My_MiniMes.Shell.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace My_MiniMes.Shell.Services
{
    public interface IDataRepository
    {
        Task<IEnumerable<DeviceModel>> GetAllDevicesAsync();
        Task UpdateDeviceStateAsync(int deviceId, string state);

        // 设备配置管理
        Task<int> InsertDeviceAsync(DeviceModel device);
        Task<int> UpdateDeviceAsync(DeviceModel device);
        
        // 历史数据插入
        Task InsertDeviceHistoryAsync(int deviceId, double temp, double pressure, int speed, int statusCode);

        // 订单管理
        Task<IEnumerable<OrderModel>> GetAllOrdersAsync();
        Task<int> InsertOrderAsync(OrderModel order);
        Task<int> UpdateOrderAsync(OrderModel order);
        Task<int> DeleteOrderAsync(int orderId);
        
        // 订单调度
        Task<int> DispatchOrderAsync(int orderId, int deviceId);

        // 订单完成：将订单置为已完成，并把已生产数量同步为目标数量
        Task<int> CompleteOrderAsync(int orderId);

        // 订单号查重：excludeId 用于编辑时排除当前订单自身
        Task<bool> OrderNoExistsAsync(string orderNo, int excludeId = 0);
    }
}
