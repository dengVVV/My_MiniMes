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
    }
}
