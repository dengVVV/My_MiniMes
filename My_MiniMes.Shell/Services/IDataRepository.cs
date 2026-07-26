using My_MiniMes.Shell.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace My_MiniMes.Shell.Services
{
    public interface IDataRepository
    {
        Task<IEnumerable<DeviceModel>> GetAllDevicesAsync();
        Task UpdateDeviceStateAsync(int deviceId, string state);
    }
}
