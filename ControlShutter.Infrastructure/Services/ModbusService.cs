using ControlShutter.Infrastructure.Modbus;
using ControlShutter.Shared.Configuration;
using ControlShutter.Shared.Interfaces;
using ControlShutter.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ControlShutter.Infrastructure.Services;

/// <summary>
/// Modbus通信服务实现
/// </summary>
public class ModbusService : IModbusService
{
    private readonly IConnectionPoolManager _connectionPool;
    private readonly DeviceConnectionOptions _options;
    private readonly ILogger<ModbusService> _logger;
    private readonly Dictionary<DeviceType, DeviceConfig> _deviceConfigs;

    public ModbusService(
        IConnectionPoolManager connectionPool,
        IOptions<DeviceConnectionOptions> options,
        ILogger<ModbusService> logger)
    {
        _connectionPool = connectionPool ?? throw new ArgumentNullException(nameof(connectionPool));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 构建设备配置映射
        _deviceConfigs = new Dictionary<DeviceType, DeviceConfig>
        {
            [DeviceType.PullDoor] = _options.PullDoor,
            [DeviceType.ShutterDoor] = _options.ShutterDoor,
            [DeviceType.DefaultShutter] = _options.DefaultShutter
        };
    }

    /// <summary>
    /// 连接到设备
    /// </summary>
    public async Task<bool> ConnectAsync(DeviceType deviceType, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("正在连接到设备 {DeviceType}", deviceType);
            
            var connection = await _connectionPool.GetConnectionAsync(deviceType, cancellationToken);
            await _connectionPool.ReleaseConnectionAsync(deviceType, connection);
            
            _logger.LogInformation("成功连接到设备 {DeviceType}", deviceType);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "连接到设备 {DeviceType} 失败", deviceType);
            return false;
        }
    }

    /// <summary>
    /// 断开设备连接
    /// </summary>
    public async Task DisconnectAsync(DeviceType deviceType, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("断开设备 {DeviceType} 连接", deviceType);
            // 连接池会自动管理连接的生命周期
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "断开设备 {DeviceType} 连接时发生异常", deviceType);
        }
    }

    /// <summary>
    /// 写入数字输出
    /// </summary>
    public async Task<TaskExecutionResult> WriteDigitalOutputAsync(
        DeviceType deviceType, 
        byte address, 
        int ioPort, 
        bool value, 
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("向设备 {DeviceType} 写入数字输出: 地址={Address}, 端口={IoPort}, 值={Value}",
                deviceType, address, ioPort, value);

            var connection = await _connectionPool.GetConnectionAsync(deviceType, cancellationToken);
            
            try
            {
                // 创建Modbus写单个线圈命令
                var command = ModbusProtocol.CreateWriteSingleCoilCommand(address, (ushort)ioPort, value);
                
                // 发送命令并接收响应
                var response = await connection.SendDataAsync(command, cancellationToken);
                
                stopwatch.Stop();

                if (response == null)
                {
                    _logger.LogWarning("设备 {DeviceType} 未返回响应", deviceType);
                    return TaskExecutionResult.Failed("设备未响应");
                }

                // 解析响应
                var parsedResponse = ModbusProtocol.ParseResponse(response);
                
                if (!parsedResponse.IsValid)
                {
                    _logger.LogError("设备 {DeviceType} 返回无效响应: {Error}", deviceType, parsedResponse.ErrorMessage);
                    return TaskExecutionResult.Failed($"设备响应无效: {parsedResponse.ErrorMessage}");
                }

                _logger.LogDebug("成功向设备 {DeviceType} 写入数字输出，耗时: {ElapsedMs}ms", 
                    deviceType, stopwatch.ElapsedMilliseconds);

                var result = TaskExecutionResult.Success("写入成功", parsedResponse.Data);
                result.ExecutionTime = stopwatch.Elapsed;
                return result;
            }
            finally
            {
                await _connectionPool.ReleaseConnectionAsync(deviceType, connection);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "向设备 {DeviceType} 写入数字输出失败", deviceType);
            
            var result = TaskExecutionResult.Failed($"写入失败: {ex.Message}", ex);
            result.ExecutionTime = stopwatch.Elapsed;
            return result;
        }
    }

    /// <summary>
    /// 读取数字输入
    /// </summary>
    public async Task<TaskExecutionResult> ReadDigitalInputAsync(
        DeviceType deviceType, 
        byte address, 
        int ioPort, 
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("从设备 {DeviceType} 读取数字输入: 地址={Address}, 端口={IoPort}",
                deviceType, address, ioPort);

            var connection = await _connectionPool.GetConnectionAsync(deviceType, cancellationToken);
            
            try
            {
                // 创建Modbus读数字输入命令
                var command = ModbusProtocol.CreateReadDiscreteInputsCommand(address, (ushort)ioPort, 1);
                
                // 发送命令并接收响应
                var response = await connection.SendDataAsync(command, cancellationToken);
                
                stopwatch.Stop();

                if (response == null)
                {
                    _logger.LogWarning("设备 {DeviceType} 未返回响应", deviceType);
                    return TaskExecutionResult.Failed("设备未响应");
                }

                // 解析响应
                var parsedResponse = ModbusProtocol.ParseResponse(response);
                
                if (!parsedResponse.IsValid)
                {
                    _logger.LogError("设备 {DeviceType} 返回无效响应: {Error}", deviceType, parsedResponse.ErrorMessage);
                    return TaskExecutionResult.Failed($"设备响应无效: {parsedResponse.ErrorMessage}");
                }

                _logger.LogDebug("成功从设备 {DeviceType} 读取数字输入，耗时: {ElapsedMs}ms", 
                    deviceType, stopwatch.ElapsedMilliseconds);

                var result = TaskExecutionResult.Success("读取成功", parsedResponse.Data);
                result.ExecutionTime = stopwatch.Elapsed;
                return result;
            }
            finally
            {
                await _connectionPool.ReleaseConnectionAsync(deviceType, connection);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "从设备 {DeviceType} 读取数字输入失败", deviceType);
            
            var result = TaskExecutionResult.Failed($"读取失败: {ex.Message}", ex);
            result.ExecutionTime = stopwatch.Elapsed;
            return result;
        }
    }

    /// <summary>
    /// 检查连接状态
    /// </summary>
    public bool IsConnected(DeviceType deviceType)
    {
        try
        {
            var poolStatus = _connectionPool.GetPoolStatus();
            return poolStatus.ConnectionsByDevice.ContainsKey(deviceType) && 
                   poolStatus.ConnectionsByDevice[deviceType] > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 批量写入数字输出
    /// </summary>
    public async Task<TaskExecutionResult> WriteMultipleDigitalOutputsAsync(
        DeviceType deviceType, 
        byte address, 
        int startPort, 
        bool[] values, 
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("向设备 {DeviceType} 批量写入数字输出: 地址={Address}, 起始端口={StartPort}, 数量={Count}",
                deviceType, address, startPort, values.Length);

            var connection = await _connectionPool.GetConnectionAsync(deviceType, cancellationToken);
            
            try
            {
                // 创建Modbus写多个线圈命令
                var command = ModbusProtocol.CreateWriteMultipleCoilsCommand(
                    address, 
                    (ushort)startPort, 
                    (ushort)values.Length, 
                    values);
                
                // 发送命令并接收响应
                var response = await connection.SendDataAsync(command, cancellationToken);
                
                stopwatch.Stop();

                if (response == null)
                {
                    return TaskExecutionResult.Failed("设备未响应");
                }

                var parsedResponse = ModbusProtocol.ParseResponse(response);
                
                if (!parsedResponse.IsValid)
                {
                    return TaskExecutionResult.Failed($"设备响应无效: {parsedResponse.ErrorMessage}");
                }

                _logger.LogDebug("成功向设备 {DeviceType} 批量写入数字输出，耗时: {ElapsedMs}ms", 
                    deviceType, stopwatch.ElapsedMilliseconds);

                var result = TaskExecutionResult.Success("批量写入成功", parsedResponse.Data);
                result.ExecutionTime = stopwatch.Elapsed;
                return result;
            }
            finally
            {
                await _connectionPool.ReleaseConnectionAsync(deviceType, connection);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "向设备 {DeviceType} 批量写入数字输出失败", deviceType);
            
            var result = TaskExecutionResult.Failed($"批量写入失败: {ex.Message}", ex);
            result.ExecutionTime = stopwatch.Elapsed;
            return result;
        }
    }

    /// <summary>
    /// 读取多个数字输入
    /// </summary>
    public async Task<TaskExecutionResult> ReadMultipleDigitalInputsAsync(
        DeviceType deviceType, 
        byte address, 
        int startPort, 
        int count, 
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var connection = await _connectionPool.GetConnectionAsync(deviceType, cancellationToken);
            
            try
            {
                var command = ModbusProtocol.CreateReadDiscreteInputsCommand(
                    address, 
                    (ushort)startPort, 
                    (ushort)count);
                
                var response = await connection.SendDataAsync(command, cancellationToken);
                
                stopwatch.Stop();

                if (response == null)
                {
                    return TaskExecutionResult.Failed("设备未响应");
                }

                var parsedResponse = ModbusProtocol.ParseResponse(response);
                
                if (!parsedResponse.IsValid)
                {
                    return TaskExecutionResult.Failed($"设备响应无效: {parsedResponse.ErrorMessage}");
                }

                var result = TaskExecutionResult.Success("批量读取成功", parsedResponse.Data);
                result.ExecutionTime = stopwatch.Elapsed;
                return result;
            }
            finally
            {
                await _connectionPool.ReleaseConnectionAsync(deviceType, connection);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "从设备 {DeviceType} 批量读取数字输入失败", deviceType);
            
            var result = TaskExecutionResult.Failed($"批量读取失败: {ex.Message}", ex);
            result.ExecutionTime = stopwatch.Elapsed;
            return result;
        }
    }
}