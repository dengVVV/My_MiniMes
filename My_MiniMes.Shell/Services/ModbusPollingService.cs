using NModbus;
using My_MiniMes.Shell.ViewModels.Dto;
using My_MiniMes.Shell.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace My_MiniMes.Shell.Services
{
    // 事件参数扩展，增加转速和状态码
    public class DeviceDataUpdatedEventArgs : EventArgs
    {
        public int DeviceId { get; set; }
        public double Temperature { get; set; }
        public double Pressure { get; set; }
        public int Speed { get; set; }
        public int StatusCode { get; set; }
        public bool IsOnline { get; set; }
    }

    // 历史记录消息结构 (通道消息载体)
    public class DeviceHistoryRecord
    {
        public int DeviceId { get; set; }
        public double Temperature { get; set; }
        public double Pressure { get; set; }
        public int Speed { get; set; }
        public int StatusCode { get; set; }
    }

    // 适配器：将 System.IO.Ports.SerialPort 转换为 NModbus 需要的 IStreamResource
    public class SerialPortAdapter : NModbus.IO.IStreamResource
    {
        private readonly SerialPort _serialPort;
        public SerialPortAdapter(SerialPort serialPort) => _serialPort = serialPort;
        public void DiscardInBuffer() => _serialPort.DiscardInBuffer();
        public int Read(byte[] buffer, int offset, int count) => _serialPort.Read(buffer, offset, count);
        public void Write(byte[] buffer, int offset, int count) => _serialPort.Write(buffer, offset, count);
        public int InfiniteTimeout => SerialPort.InfiniteTimeout;
        public int ReadTimeout { get => _serialPort.ReadTimeout; set => _serialPort.ReadTimeout = value; }
        public int WriteTimeout { get => _serialPort.WriteTimeout; set => _serialPort.WriteTimeout = value; }
        public void Dispose() => _serialPort.Dispose();
    }

    /// <summary>
    /// 封装设备长连接上下文，持有通信链路资源
    /// </summary>
    public class DeviceConnectionContext : IDisposable
    {
        public DeviceModel Config { get; set; }
        public IModbusMaster Master { get; set; }
        public TcpClient? TcpClient { get; set; }
        public SerialPort? SerialPort { get; set; }
        public int ReadCount { get; set; } = 0; // 计频器

        public DeviceConnectionContext(DeviceModel config, IModbusMaster master, TcpClient? tcpClient, SerialPort? serialPort)
        {
            Config = config;
            Master = master;
            TcpClient = tcpClient;
            SerialPort = serialPort;
        }

        public void Dispose()
        {
            TcpClient?.Dispose();
            SerialPort?.Dispose();
        }
    }

    public class ModbusPollingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ModbusFactory _modbusFactory;
        
        // 【核心】连接池：存储各设备的活跃长连接，线程安全
        private readonly ConcurrentDictionary<int, DeviceConnectionContext> _connectionPool = new();

        // 缓存底层查出来的数据库设备列表
        private List<DeviceModel> _cachedDevices = new();
        
        // 用于控制配置同步频率的时间戳
        private DateTime _lastDbCheckTime = DateTime.MinValue;

        // 【核心】生产者-消费者模型：用于无锁化、平滑地将数据刷入 SQLite，彻底解决写库争抢
        private readonly Channel<DeviceHistoryRecord> _historyChannel = Channel.CreateUnbounded<DeviceHistoryRecord>();

        public event EventHandler<DeviceDataUpdatedEventArgs>? DeviceDataUpdated;

        public ModbusPollingService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _modbusFactory = new ModbusFactory();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 1. 启动专用的独立写库消费者线程 (后台死循环监听 Channel 队列)
            _ = Task.Run(() => ProcessHistoryQueueAsync(stoppingToken), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    //第一次初始化连接也检查
                    //如果大于等于60s也就是到了入库时间就检查内存和数据库中设备信息的差异，主要用于检查用户是否修改或删除了下位机的信息
                    if ((DateTime.Now - _lastDbCheckTime).TotalSeconds >= 60 || !_cachedDevices.Any())
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var repository = scope.ServiceProvider.GetRequiredService<IDataRepository>();
                        var devices = await repository.GetAllDevicesAsync();
                        _cachedDevices = devices.ToList();
                        _lastDbCheckTime = DateTime.Now;

                        // 清理已被用户在前端删除，或者配置发生修改的旧连接
                        SyncConnectionPool(_cachedDevices);
                    }

                    if (!_cachedDevices.Any())
                    {
                        await Task.Delay(1000, stoppingToken);
                        continue;
                    }

                    // 3. 【多线程高并发核心】
                    // 不再使用阻塞式的 foreach。这里使用 Parallel.ForEachAsync 让所有设备并发拉取。
                    // 设置 MaxDegreeOfParallelism，防止瞬间发起成百上千个网络请求导致本地网卡或系统句柄耗尽。
                    var options = 
                        new ParallelOptions { MaxDegreeOfParallelism = 50, CancellationToken = stoppingToken };
                    await Parallel.ForEachAsync(_cachedDevices, options, async (device, token) =>
                    {
                        await PollSingleDeviceAsync(device, token);
                    });
                }
                catch (TaskCanceledException) { }
                catch (Exception ex)
                {
                    // 这里可以加入日志记录严重异常
                    System.Diagnostics.Debug.WriteLine($"Modbus 总线异常: {ex.Message}");
                }

                // 一轮并发拉取结束后，主循环休息 1 秒
                await Task.Delay(1000, stoppingToken);
            }

            // 服务停止时释放连接池所有资源
            foreach (var kvp in _connectionPool) kvp.Value.Dispose();
            _connectionPool.Clear();
        }

        /// <summary>
        /// 比对数据库最新配置，更新或移除连接池中的过期对象
        /// </summary>
        private void SyncConnectionPool(List<DeviceModel> latestDevices)
        {
            var latestIds = latestDevices.Select(d => d.DeviceId).ToHashSet();
            
            foreach (var kvp in _connectionPool)
            {
                var id = kvp.Key;
                var context = kvp.Value;
                
                var latestConfig = latestDevices.FirstOrDefault(d => d.DeviceId == id);
                
                // 如果数据库中不存在此设备，或者连接的核心参数发生了改变，主动销毁旧连接释放句柄
                if (latestConfig == null || 
                    latestConfig.IpAddress != context.Config.IpAddress ||
                    latestConfig.Port != context.Config.Port ||
                    latestConfig.SerialPort != context.Config.SerialPort ||
                    latestConfig.BaudRate != context.Config.BaudRate)
                {
                    if (_connectionPool.TryRemove(id, out var removedContext))
                    {
                        removedContext.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// 轮询单一设备的核心逻辑（此方法会被多线程并发调用）
        /// </summary>
        private async Task PollSingleDeviceAsync(DeviceModel device, CancellationToken token)
        {
            // 尝试从池中获取长连接。如果没有（新加入或者刚才断线被销毁了），则现场建立
            if (!_connectionPool.TryGetValue(device.DeviceId, out var context))
            {
                context = await TryEstablishConnectionAsync(device, token);
                if (context != null)
                {
                    _connectionPool.TryAdd(device.DeviceId, context);
                }
            }

            if (context != null)
            {
                try
                {
                    byte slaveId = device.SlaveId ?? 1;

                    // 一次性批量读取 100 个保持寄存器 (极大降低总线通信次数)
                    ushort[] registers = await context.Master.ReadHoldingRegistersAsync(slaveId, 0, 100);

                    // 规约解析
                    double temp = (short)registers[0] / 10.0;
                    double press = (short)registers[1] / 10.0;
                    int speed = registers[2];
                    int statusCode = registers[3];

                    // 通过事件抛给 UI 线程刷新前端图表
                    DeviceDataUpdated?.Invoke(this, new DeviceDataUpdatedEventArgs
                    {
                        DeviceId = device.DeviceId,
                        Temperature = temp,
                        Pressure = press,
                        Speed = speed,
                        StatusCode = statusCode,
                        IsOnline = true
                    });

                    // UI层级的数据库状态刷新：如果本来是故障，现在读取成功了，修正状态为"运行"
                    if (device.DeviceState != "运行" && statusCode == 1)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var repo = scope.ServiceProvider.GetRequiredService<IDataRepository>();
                        await repo.UpdateDeviceStateAsync(device.DeviceId, "运行");
                        device.DeviceState = "运行";
                    }

                    // 计频器，大约 60 次（约 1 分钟）作为一次有效历史记录存入数据库
                    context.ReadCount++;
                    if (context.ReadCount >= 60)
                    {
                        // 零阻塞写库：把实体记录直接塞给系统通道 Channel，立刻返回，不会卡住网络读取
                        _historyChannel.Writer.TryWrite(new DeviceHistoryRecord
                        {
                            DeviceId = device.DeviceId,
                            Temperature = temp,
                            Pressure = press,
                            Speed = speed,
                            StatusCode = statusCode
                        });
                        context.ReadCount = 0;
                    }
                }
                catch
                {
                    // 读取异常，说明这根长连接网线断了或串口挂了
                    // 我们主动把它从并发字典里移除并释放资源。下个 1 秒循环到来时会自动触发重连
                    if (_connectionPool.TryRemove(device.DeviceId, out var badContext))
                    {
                        badContext.Dispose();
                    }
                    DeviceDataUpdated?.Invoke(this, new DeviceDataUpdatedEventArgs { DeviceId = device.DeviceId, IsOnline = false });
                    
                    if (device.DeviceState != "断连")
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var repo = scope.ServiceProvider.GetRequiredService<IDataRepository>();
                        await repo.UpdateDeviceStateAsync(device.DeviceId, "断连");
                        device.DeviceState = "断连";
                    }
                }
            }
            else
            {
                // 建连失败
                DeviceDataUpdated?.Invoke(this, new DeviceDataUpdatedEventArgs { DeviceId = device.DeviceId, IsOnline = false });
                if (device.DeviceState != "断连")
                {
                    using var scope = _serviceProvider.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IDataRepository>();
                    await repo.UpdateDeviceStateAsync(device.DeviceId, "断连");
                    device.DeviceState = "断连";
                }
            }
        }

        /// <summary>
        /// 尝试建立物理层连接并返回包装的长连接上下文对象
        /// </summary>
        private async Task<DeviceConnectionContext?> TryEstablishConnectionAsync(DeviceModel device, CancellationToken token)
        {
            try
            {
                // 1. TCP 模式
                if (!string.IsNullOrWhiteSpace(device.IpAddress) && device.Port > 0)
                {
                    var tcpClient = new TcpClient();
                    tcpClient.ReceiveTimeout = 1500;
                    tcpClient.SendTimeout = 1500;
                    
                    var connectTask = tcpClient.ConnectAsync(device.IpAddress, device.Port.Value);
                    if (await Task.WhenAny(connectTask, Task.Delay(2000, token)) == connectTask && tcpClient.Connected)
                    {
                        var master = _modbusFactory.CreateMaster(tcpClient);
                        return new DeviceConnectionContext(device, master, tcpClient, null);
                    }
                    tcpClient.Dispose(); // 连接超时失败，释放游离套接字
                }
                // 2. RTU 模式
                else if (!string.IsNullOrWhiteSpace(device.SerialPort))
                {
                    var serialPort = new SerialPort(device.SerialPort)
                    {
                        BaudRate = device.BaudRate,
                        DataBits = device.DataBits,
                        StopBits = device.StopBits switch { 0 => StopBits.None, 1 => StopBits.One, 2 => StopBits.Two, 3 => StopBits.OnePointFive, _ => StopBits.One },
                        Parity = device.Parity switch { 0 => Parity.None, 1 => Parity.Odd, 2 => Parity.Even, 3 => Parity.Mark, 4 => Parity.Space, _ => Parity.None },
                        ReadTimeout = 1500,
                        WriteTimeout = 1500
                    };
                    serialPort.Open();
                    if (serialPort.IsOpen)
                    {
                        var master = _modbusFactory.CreateRtuMaster(new SerialPortAdapter(serialPort));
                        return new DeviceConnectionContext(device, master, null, serialPort);
                    }
                }
            }
            catch
            {
                // 建连报错（如 COM 口被占用或 IP 拒绝访问）直接吞没并返回 null，不使服务崩溃
            }
            return null;
        }

        /// <summary>
        /// [消费者线程]：该方法在后台挂起循环。持续消费 Channel 中的记录，平滑地串行写入 SQLite
        /// </summary>
        private async Task ProcessHistoryQueueAsync(CancellationToken token)
        {
            // 通过 CreateScope 获取单例仓库服务用于专门的写库操作
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IDataRepository>();

            try
            {
                // WaitToReadAsync 会在没有数据时异步休眠挂起，彻底0消耗 CPU
                //如果通道里没有数据，这根后台线程就进入深度休眠，完全不消耗 CPU。
                // 一旦上面第四步有任何一根读线程往通道里扔了一条数据，这根线程就会瞬间苏醒
                while (await _historyChannel.Reader.WaitToReadAsync(token))
                {
                    while (_historyChannel.Reader.TryRead(out var record))
                    {
                        try
                        {
                            // 串行插入 SQLite（即便外层有 50 个读线程在 1 毫秒内同时向通道丢入 50 条数据，这里也是逐一单排插入）
                            // 这个机制从根源上消除了 SQLite 的写锁冲突 Database is locked
                            await repository.InsertDeviceHistoryAsync(record.DeviceId, record.Temperature, record.Pressure, record.Speed, record.StatusCode);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"后台写入历史数据失败: {ex.Message}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 服务安全退出
            }
        }
    }
}
