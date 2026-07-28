using Dapper;
using Microsoft.Data.Sqlite;
using System.IO;

namespace My_MiniMes.Shell.Services
{
    /// <summary>
    /// 数据库初始化服务
    /// 负责在程序首次启动时检查 SQLite 文件是否存在，如果不存在则自动建表并插入默认数据。
    /// </summary>
    public static class DatabaseInitializer
    {
        // 数据库文件路径及连接字符串
        public static readonly string DbFileName = "MiniMes.db";
        public static readonly string ConnectionString = $"Data Source={DbFileName}";

        public static void Initialize()
        {
            // 如果数据库文件已经存在，说明不是第一次运行，直接跳过建表流程。
            if (File.Exists(DbFileName)) return;

            // 创建 SQLite 连接
            using var connection = new SqliteConnection(ConnectionString);
            
            // 使用 Dapper 提供的方法，执行原始 SQL 语句
            // 1. 创建 Users 表
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS Users (
                    UserId INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserName TEXT NOT NULL,
                    Role INTEGER NOT NULL DEFAULT 1,
                    Account TEXT NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    Salt TEXT NOT NULL,
                    Email TEXT,
                    IsActive INTEGER NOT NULL DEFAULT 1
                );
            ");

            // 2. 创建 Devices 表
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS Devices (
                    DeviceId INTEGER PRIMARY KEY AUTOINCREMENT,
                    DeviceName TEXT NOT NULL,
                    IpAddress TEXT NOT NULL,
                    Port INTEGER,
                    SerialPort TEXT,
                    SlaveId INTEGER,
                    BaudRate INTEGER DEFAULT 9600,
                    DataBits INTEGER DEFAULT 8,
                    StopBits INTEGER DEFAULT 1,
                    Parity INTEGER DEFAULT 0,
                    DeviceState TEXT DEFAULT '离线',
                    LastUpdateTime DATETIME
                );

                -- 创建历史数据表用于分钟级追溯
                CREATE TABLE IF NOT EXISTS DeviceHistoryData (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    DeviceId INTEGER NOT NULL,
                    Temperature REAL,
                    Pressure REAL,
                    Speed INTEGER,
                    StatusCode INTEGER,
                    RecordTime DATETIME DEFAULT (datetime('now', 'localtime'))
                );
            ");

            // 3. 插入默认的超级管理员账号 (admin / 123456)
            // 在实际企业级应用中，密码必须加盐(Salt)后进行 Hash 存储。这里由于是初始化默认数据，先写入硬编码。
            connection.Execute(@"
                INSERT INTO Users (UserName, Role, Account, PasswordHash, Salt, IsActive)
                VALUES ('超级管理员', 1, 'admin', 'e10adc3949ba59abbe56e057f20f883e', 'salt_str', 1);
            ");
            
            // 插入一些默认测试设备，方便后续直接在监控大屏调试
            connection.Execute(@"
                INSERT INTO Devices (DeviceName, IpAddress, Port, SerialPort, SlaveId, DeviceState, LastUpdateTime)
                VALUES 
                ('一号注塑机', '192.168.1.101', 502, 'COM1', 1, '运行', datetime('now', 'localtime')),
                ('二号冲压机', '192.168.1.102', 502, 'COM2', 1, '停机', datetime('now', 'localtime'));
            ");
        }
    }
}
