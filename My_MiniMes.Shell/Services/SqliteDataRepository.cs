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

        public async Task<int> InsertDeviceAsync(DeviceModel device)
        {
            using var connection = new SqliteConnection(DatabaseInitializer.ConnectionString);
            var sql = @"
                INSERT INTO Devices (DeviceName, IpAddress, Port, SerialPort, SlaveId, BaudRate, DataBits, StopBits, Parity, LastUpdateTime)
                VALUES (@DeviceName, @IpAddress, @Port, @SerialPort, @SlaveId, @BaudRate, @DataBits, @StopBits, @Parity, datetime('now', 'localtime'));
                SELECT last_insert_rowid();";
            return await connection.ExecuteScalarAsync<int>(sql, device);
        }

        public async Task<int> UpdateDeviceAsync(DeviceModel device)
        {
            using var connection = new SqliteConnection(DatabaseInitializer.ConnectionString);
            var sql = @"
                UPDATE Devices SET 
                    DeviceName = @DeviceName,
                    IpAddress = @IpAddress,
                    Port = @Port,
                    SerialPort = @SerialPort,
                    SlaveId = @SlaveId,
                    BaudRate = @BaudRate,
                    DataBits = @DataBits,
                    StopBits = @StopBits,
                    Parity = @Parity,
                    LastUpdateTime = datetime('now', 'localtime')
                WHERE DeviceId = @DeviceId;";
            return await connection.ExecuteAsync(sql, device);
        }

        public async Task InsertDeviceHistoryAsync(int deviceId, double temp, double pressure, int speed, int statusCode)
        {
            using var connection = new SqliteConnection(DatabaseInitializer.ConnectionString);
            var sql = @"
                INSERT INTO DeviceHistoryData (DeviceId, Temperature, Pressure, Speed, StatusCode)
                VALUES (@DeviceId, @Temperature, @Pressure, @Speed, @StatusCode);";
            await connection.ExecuteAsync(sql, new { DeviceId = deviceId, Temperature = temp, Pressure = pressure, Speed = speed, StatusCode = statusCode });
        }
    }
}
