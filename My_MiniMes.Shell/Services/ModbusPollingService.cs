using NModbus;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace My_MiniMes.Shell.Services
{
    public class DeviceDataUpdatedEventArgs : EventArgs
    {
        public int DeviceId { get; set; }
        public double Temperature { get; set; }
        public double Pressure { get; set; }
        public bool IsOnline { get; set; }
    }

    /// <summary>
    /// 后台 Modbus 轮询服务。继承自 BackgroundService，随 IHost 启动而自动运行在后台线程。
    /// 负责定时从真实的 Modbus TCP 仿真器读取数据，并通过事件抛出。
    /// </summary>
    public class ModbusPollingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ModbusFactory _modbusFactory;
        
        public event EventHandler<DeviceDataUpdatedEventArgs>? DeviceDataUpdated;

        // 使用 IServiceProvider 注入，避免在单例 BackgroundService 中长期占用 Scoped/Transient 资源
        public ModbusPollingService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _modbusFactory = new ModbusFactory();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 延迟一小会儿等 UI 启动完再开始轮询
            await Task.Delay(2000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                // 创建一个局部的 scope 来获取仓储服务，这是企业级标准的后台服务 DI 做法
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IDataRepository>();

                // 1. 从数据库获取所有需要监控的设备配置
                var devices = await repository.GetAllDevicesAsync();

                foreach (var device in devices)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    // 简单过滤：如果没有 IP 或端口，说明配置不正确，直接跳过
                    if (string.IsNullOrWhiteSpace(device.IpAddress) || device.Port == null || device.Port == 0)
                        continue;

                    try
                    {
                        // 2. 建立 TCP 连接
                        using var client = new TcpClient();
                        // 设置超时时间防止卡死，因为网络不通时不能一直阻塞主循环
                        client.ReceiveTimeout = 1000;
                        client.SendTimeout = 1000;
                        
                        var connectTask = client.ConnectAsync(device.IpAddress, device.Port.Value);
                        // 等待连接，最多等 1.5 秒
                        if (await Task.WhenAny(connectTask, Task
                            .Delay(1500, stoppingToken)) == connectTask && client.Connected)
                        {
                            // 3. 创建 Modbus TCP Master
                            using var master = _modbusFactory.CreateMaster(client);
                            byte slaveId = device.SlaveId ?? 1;

                            // 4. 读取保持寄存器 (Holding Registers)
                            // 规约设计：寄存器 0 存温度(放大10倍)，寄存器 1 存压力(放大10倍)
                            ushort[] registers = await master.ReadHoldingRegistersAsync(slaveId, 0, 2);

                            double temp = registers[0] / 10.0;
                            double press = registers[1] / 10.0;

                            // 抛出事件给 ViewModel 更新 UI
                            DeviceDataUpdated?.Invoke(this, new DeviceDataUpdatedEventArgs
                            {
                                DeviceId = device.DeviceId,
                                Temperature = temp,
                                Pressure = press,
                                IsOnline = true
                            });

                            // 如果之前是离线状态，现在连上了，更新数据库状态
                            if (device.DeviceState != "运行")
                                await repository.UpdateDeviceStateAsync(device.DeviceId, "运行");
                        }
                        else
                        {
                            // 连接失败或超时
                            DeviceDataUpdated?.Invoke(this, new DeviceDataUpdatedEventArgs
                            {
                                DeviceId = device.DeviceId,
                                IsOnline = false
                            });
                            
                            if (device.DeviceState != "断连")
                                await repository.UpdateDeviceStateAsync(device.DeviceId, "断连");
                        }
                    }
                    catch (Exception)
                    {
                        // 通讯异常 (如仿真器未开、端口被拒绝等)
                        DeviceDataUpdated?.Invoke(this, new DeviceDataUpdatedEventArgs
                        {
                            DeviceId = device.DeviceId,
                            IsOnline = false
                        });
                        
                        if (device.DeviceState != "故障")
                            await repository.UpdateDeviceStateAsync(device.DeviceId, "故障");
                    }
                }

                // 每隔 1 秒轮询一遍所有设备
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
