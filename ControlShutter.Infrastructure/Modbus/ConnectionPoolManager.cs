using System.Collections.Concurrent;
using ControlShutter.Infrastructure.Services;
using ControlShutter.Shared.Configuration;
using ControlShutter.Shared.Interfaces;
using ControlShutter.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ControlShutter.Infrastructure.Modbus;

/// <summary>
/// 连接池管理器实现
/// </summary>
public class ConnectionPoolManager : IConnectionPoolManager
{
    private readonly DeviceConnectionOptions _options;
    private readonly ILogger<ConnectionPoolManager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConcurrentDictionary<DeviceType, ConcurrentQueue<DeviceConnection>> _connectionPools;
    private readonly ConcurrentDictionary<DeviceType, SemaphoreSlim> _connectionSemaphores;
    private readonly Timer _cleanupTimer;
    private bool _disposed;

    public ConnectionPoolManager(
        IOptions<DeviceConnectionOptions> options,
        ILogger<ConnectionPoolManager> logger,
        ILoggerFactory loggerFactory)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

        _connectionPools = new ConcurrentDictionary<DeviceType, ConcurrentQueue<DeviceConnection>>();
        _connectionSemaphores = new ConcurrentDictionary<DeviceType, SemaphoreSlim>();

        // 初始化设备类型的连接池
        foreach (DeviceType deviceType in Enum.GetValues<DeviceType>())
        {
            _connectionPools[deviceType] = new ConcurrentQueue<DeviceConnection>();
            _connectionSemaphores[deviceType] = new SemaphoreSlim(_options.ConnectionPool.MaxConnections, _options.ConnectionPool.MaxConnections);
        }

        // 启动清理定时器
        _cleanupTimer = new Timer(
            CleanupCallback,
            null,
            TimeSpan.FromMinutes(_options.ConnectionPool.HealthCheckIntervalMinutes),
            TimeSpan.FromMinutes(_options.ConnectionPool.HealthCheckIntervalMinutes));

        _logger.LogInformation("连接池管理器已初始化，最大连接数: {MaxConnections}", _options.ConnectionPool.MaxConnections);
    }

    /// <summary>
    /// 获取或创建连接
    /// </summary>
    public async Task<IDeviceConnection> GetConnectionAsync(DeviceType deviceType, CancellationToken cancellationToken = default)
    {
        var semaphore = _connectionSemaphores[deviceType];
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            var pool = _connectionPools[deviceType];

            // 尝试从池中获取可用连接
            while (pool.TryDequeue(out var connection))
            {
                if (await connection.CheckHealthAsync(cancellationToken))
                {
                    _logger.LogDebug("从连接池获取到设备 {DeviceType} 的连接", deviceType);
                    return connection;
                }
                else
                {
                    _logger.LogDebug("连接池中的设备 {DeviceType} 连接不健康，已丢弃", deviceType);
                    connection.Dispose();
                }
            }

            // 创建新连接
            var deviceConfig = GetDeviceConfig(deviceType);
            var newConnection = new DeviceConnection(
                deviceType,
                deviceConfig,
                _loggerFactory.CreateLogger<DeviceConnection>());

            if (await newConnection.ConnectAsync(cancellationToken))
            {
                _logger.LogDebug("为设备 {DeviceType} 创建了新连接", deviceType);
                return newConnection;
            }
            else
            {
                newConnection.Dispose();
                throw new InvalidOperationException($"无法连接到设备 {deviceType}");
            }
        }
        catch
        {
            semaphore.Release();
            throw;
        }
    }

    /// <summary>
    /// 释放连接
    /// </summary>
    public async Task ReleaseConnectionAsync(DeviceType deviceType, IDeviceConnection connection)
    {
        try
        {
            if (connection is DeviceConnection deviceConnection && 
                await deviceConnection.CheckHealthAsync())
            {
                // 连接健康，放回池中
                var pool = _connectionPools[deviceType];
                pool.Enqueue(deviceConnection);
                _logger.LogTrace("设备 {DeviceType} 连接已返回连接池", deviceType);
            }
            else
            {
                // 连接不健康，直接释放
                connection.Dispose();
                _logger.LogTrace("设备 {DeviceType} 连接不健康，已释放", deviceType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "释放设备 {DeviceType} 连接时发生异常", deviceType);
            connection.Dispose();
        }
        finally
        {
            _connectionSemaphores[deviceType].Release();
        }
    }

    /// <summary>
    /// 清理空闲连接
    /// </summary>
    public async Task CleanupIdleConnectionsAsync()
    {
        var idleThreshold = DateTime.UtcNow.AddMinutes(-_options.ConnectionPool.IdleTimeoutMinutes);
        var totalCleaned = 0;

        foreach (var deviceType in _connectionPools.Keys)
        {
            var pool = _connectionPools[deviceType];
            var tempList = new List<DeviceConnection>();

            // 将连接取出到临时列表
            while (pool.TryDequeue(out var connection))
            {
                tempList.Add(connection);
            }

            // 检查连接并决定是否保留
            foreach (var connection in tempList)
            {
                if (connection.LastUsedTime > idleThreshold &&
                    await connection.CheckHealthAsync())
                {
                    // 连接仍然有效，放回池中
                    pool.Enqueue(connection);
                }
                else
                {
                    // 连接空闲太久或不健康，释放它
                    connection.Dispose();
                    totalCleaned++;
                    _logger.LogDebug("清理了设备 {DeviceType} 的空闲连接", deviceType);
                }
            }
        }

        if (totalCleaned > 0)
        {
            _logger.LogInformation("连接池清理完成，释放了 {Count} 个空闲连接", totalCleaned);
        }
    }

    /// <summary>
    /// 获取连接池状态
    /// </summary>
    public ConnectionPoolStatus GetPoolStatus()
    {
        var status = new ConnectionPoolStatus();
        
        foreach (var kvp in _connectionPools)
        {
            var deviceType = kvp.Key;
            var pool = kvp.Value;
            
            var connectionCount = pool.Count;
            status.ConnectionsByDevice[deviceType] = connectionCount;
            status.TotalConnections += connectionCount;
            status.IdleConnections += connectionCount;
        }

        // 活跃连接数 = 总连接限制 - 可用信号量
        foreach (var kvp in _connectionSemaphores)
        {
            status.ActiveConnections += _options.ConnectionPool.MaxConnections - kvp.Value.CurrentCount;
        }

        return status;
    }

    /// <summary>
    /// 获取设备配置
    /// </summary>
    private DeviceConfig GetDeviceConfig(DeviceType deviceType)
    {
        return deviceType switch
        {
            DeviceType.PullDoor => _options.PullDoor,
            DeviceType.ShutterDoor => _options.ShutterDoor,
            DeviceType.DefaultShutter => _options.DefaultShutter,
            _ => throw new ArgumentException($"不支持的设备类型: {deviceType}")
        };
    }

    /// <summary>
    /// 清理定时器回调
    /// </summary>
    private async void CleanupCallback(object? state)
    {
        try
        {
            await CleanupIdleConnectionsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "定时清理连接池时发生异常");
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            _cleanupTimer?.Dispose();

            // 释放所有连接
            foreach (var pool in _connectionPools.Values)
            {
                while (pool.TryDequeue(out var connection))
                {
                    connection.Dispose();
                }
            }

            // 释放信号量
            foreach (var semaphore in _connectionSemaphores.Values)
            {
                semaphore.Dispose();
            }

            _logger.LogInformation("连接池管理器已释放");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放连接池管理器时发生异常");
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 析构函数
    /// </summary>
    ~ConnectionPoolManager()
    {
        Dispose();
    }
}