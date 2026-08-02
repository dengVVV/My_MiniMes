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

        public async Task<IEnumerable<OrderModel>> GetAllOrdersAsync()
        {
            using var connection = new SqliteConnection(DatabaseInitializer.ConnectionString);
            return await connection.QueryAsync<OrderModel>("SELECT * FROM Orders ORDER BY CreateTime DESC");
        }

        public async Task<int> InsertOrderAsync(OrderModel order)
        {
            using var connection = new SqliteConnection(DatabaseInitializer.ConnectionString);
            var sql = @"
                INSERT INTO Orders (OrderNo, ProductName, CustomerName, TargetQuantity, ProducedQuantity, Status, AssignedDeviceId, Deadline)
                VALUES (@OrderNo, @ProductName, @CustomerName, @TargetQuantity, @ProducedQuantity, @Status, @AssignedDeviceId, @Deadline);
                SELECT last_insert_rowid();";
            return await connection.ExecuteScalarAsync<int>(sql, order);
        }

        public async Task<int> UpdateOrderAsync(OrderModel order)
        {
            using var connection = new SqliteConnection(DatabaseInitializer.ConnectionString);
            var sql = @"
                UPDATE Orders SET 
                    OrderNo = @OrderNo,
                    ProductName = @ProductName,
                    CustomerName = @CustomerName,
                    TargetQuantity = @TargetQuantity,
                    ProducedQuantity = @ProducedQuantity,
                    Status = @Status,
                    AssignedDeviceId = @AssignedDeviceId,
                    Deadline = @Deadline
                WHERE Id = @Id;";
            return await connection.ExecuteAsync(sql, order);
        }

        public async Task<int> DeleteOrderAsync(int orderId)
        {
            using var connection = new SqliteConnection(DatabaseInitializer.ConnectionString);
            return await connection.ExecuteAsync("DELETE FROM Orders WHERE Id = @Id", new { Id = orderId });
        }

        public async Task<int> DispatchOrderAsync(int orderId, int deviceId)
        {
            using var connection = new SqliteConnection(DatabaseInitializer.ConnectionString);
            // 调度订单时，将状态设为 1 (生产中)，并绑定下位机设备 Id
            return await connection.ExecuteAsync(
                "UPDATE Orders SET Status = 1, AssignedDeviceId = @DeviceId WHERE Id = @Id", 
                new { Id = orderId, DeviceId = deviceId });
        }

        public async Task<int> CompleteOrderAsync(int orderId)
        {
            using var connection = new SqliteConnection(DatabaseInitializer.ConnectionString);
            // 完成订单时同时把产量同步为目标数量，保证状态和进度在界面上保持一致
            return await connection.ExecuteAsync(
                "UPDATE Orders SET Status = 2, ProducedQuantity = TargetQuantity WHERE Id = @Id",
                new { Id = orderId });
        }

        public async Task<bool> OrderNoExistsAsync(string orderNo, int excludeId = 0)
        {
            using var connection = new SqliteConnection(DatabaseInitializer.ConnectionString);
            var count = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM Orders WHERE OrderNo = @OrderNo AND Id <> @ExcludeId",
                new { OrderNo = orderNo, ExcludeId = excludeId });
            return count > 0;
        }
    }
}
