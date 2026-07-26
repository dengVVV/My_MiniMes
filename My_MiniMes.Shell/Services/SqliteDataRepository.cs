using Dapper;
using Microsoft.Data.Sqlite;
using My_MiniMes.Shell.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace My_MiniMes.Shell.Services
{
    public class SqliteDataRepository : IDataRepository
    {
        public async Task<IEnumerable<DeviceModel>> GetAllDevicesAsync()
        {
            using var connection = new SqliteConnection(DatabaseInitializer.ConnectionString);
            return await connection.QueryAsync<DeviceModel>("SELECT * FROM Devices");
        }

        public async Task UpdateDeviceStateAsync(int deviceId, string state)
        {
            using var connection = new SqliteConnection(DatabaseInitializer.ConnectionString);
            await connection.ExecuteAsync(
                "UPDATE Devices SET DeviceState = @State, LastUpdateTime = datetime('now', 'localtime') WHERE DeviceId = @Id", 
                new { State = state, Id = deviceId });
        }
    }
}
