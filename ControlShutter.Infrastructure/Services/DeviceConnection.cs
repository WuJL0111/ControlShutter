using System.Net.Sockets;
using ControlShutter.Shared.Configuration;
using ControlShutter.Shared.Interfaces;
using ControlShutter.Shared.Models;
using Microsoft.Extensions.Logging;

namespace ControlShutter.Infrastructure.Services;

/// <summary>
/// 设备连接实现
/// </summary>
public class DeviceConnection : IDeviceConnection
{
    private readonly DeviceConfig _config;
    private readonly ILogger<DeviceConnection> _logger;
    private TcpClient? _tcpClient;
    private NetworkStream? _networkStream;
    private readonly SemaphoreSlim _sendSemaphore = new(1, 1);
    private bool _disposed;

    public DeviceType DeviceType { get; }
    public bool IsConnected => _tcpClient?.Connected == true;
    public DateTime LastUsedTime { get; private set; } = DateTime.UtcNow;

    public DeviceConnection(DeviceType deviceType, DeviceConfig config, ILogger<DeviceConnection> logger)
    {
        DeviceType = deviceType;
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 建立连接
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否连接成功</returns>
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
            return true;

        try
        {
            _logger.LogDebug("正在连接到设备 {DeviceType} ({Host}:{Port})", DeviceType, _config.Host, _config.Port);

            _tcpClient?.Dispose();
            _tcpClient = new TcpClient();

            // 设置连接超时
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_config.ConnectTimeoutMs);

            await _tcpClient.ConnectAsync(_config.Host, _config.Port, timeoutCts.Token);

            // 配置TCP客户端
            _tcpClient.ReceiveTimeout = _config.ReadTimeoutMs;
            _tcpClient.SendTimeout = _config.WriteTimeoutMs;
            _tcpClient.NoDelay = true; // 禁用Nagle算法，减少延迟

            _networkStream = _tcpClient.GetStream();
            LastUsedTime = DateTime.UtcNow;

            _logger.LogInformation("成功连接到设备 {DeviceType} ({Host}:{Port})", DeviceType, _config.Host, _config.Port);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "连接到设备 {DeviceType} ({Host}:{Port}) 失败", DeviceType, _config.Host, _config.Port);
            
            _networkStream?.Dispose();
            _networkStream = null;
            _tcpClient?.Dispose();
            _tcpClient = null;
            
            return false;
        }
    }

    /// <summary>
    /// 发送数据并接收响应
    /// </summary>
    /// <param name="data">要发送的数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应数据</returns>
    public async Task<byte[]?> SendDataAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("设备 {DeviceType} 未连接，尝试重新连接", DeviceType);
            if (!await ConnectAsync(cancellationToken))
            {
                throw new InvalidOperationException($"设备 {DeviceType} 连接失败");
            }
        }

        await _sendSemaphore.WaitAsync(cancellationToken);
        try
        {
            LastUsedTime = DateTime.UtcNow;

            // 发送数据
            await _networkStream!.WriteAsync(data, cancellationToken);
            await _networkStream.FlushAsync(cancellationToken);

            _logger.LogTrace("已向设备 {DeviceType} 发送 {ByteCount} 字节: {Data}", 
                DeviceType, data.Length, Convert.ToHexString(data));

            // 接收响应
            var response = await ReceiveResponseAsync(cancellationToken);
            
            if (response != null)
            {
                _logger.LogTrace("从设备 {DeviceType} 接收到 {ByteCount} 字节: {Data}", 
                    DeviceType, response.Length, Convert.ToHexString(response));
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "与设备 {DeviceType} 通信失败", DeviceType);
            
            // 通信失败时断开连接
            await DisconnectAsync();
            throw;
        }
        finally
        {
            _sendSemaphore.Release();
        }
    }

    /// <summary>
    /// 接收响应数据
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应数据</returns>
    private async Task<byte[]?> ReceiveResponseAsync(CancellationToken cancellationToken)
    {
        const int maxResponseSize = 2048;
        var buffer = new byte[maxResponseSize];
        var totalReceived = 0;
        var retryCount = 0;
        const int maxRetries = 3;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_config.ReadTimeoutMs);

        while (totalReceived < 5 && retryCount < maxRetries) // 至少需要5个字节才能判断完整性
        {
            try
            {
                if (!_networkStream!.DataAvailable)
                {
                    await Task.Delay(10, timeoutCts.Token); // 短暂等待数据到达
                    continue;
                }

                var bytesRead = await _networkStream.ReadAsync(
                    buffer.AsMemory(totalReceived, maxResponseSize - totalReceived), 
                    timeoutCts.Token);

                if (bytesRead == 0)
                {
                    retryCount++;
                    await Task.Delay(50, timeoutCts.Token);
                    continue;
                }

                totalReceived += bytesRead;
                retryCount = 0; // 重置重试计数

                // 检查是否接收到完整响应
                if (totalReceived >= 5)
                {
                    var expectedLength = GetExpectedResponseLength(buffer, totalReceived);
                    if (totalReceived >= expectedLength)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        if (totalReceived == 0)
        {
            _logger.LogWarning("从设备 {DeviceType} 未接收到任何数据", DeviceType);
            return null;
        }

        var result = new byte[totalReceived];
        Array.Copy(buffer, result, totalReceived);
        return result;
    }

    /// <summary>
    /// 获取期望的响应长度
    /// </summary>
    /// <param name="buffer">缓冲区</param>
    /// <param name="receivedBytes">已接收字节数</param>
    /// <returns>期望的响应长度</returns>
    private static int GetExpectedResponseLength(byte[] buffer, int receivedBytes)
    {
        if (receivedBytes < 3)
            return 5; // 最小响应长度

        var functionCode = buffer[1];
        
        return functionCode switch
        {
            0x01 or 0x02 or 0x03 or 0x04 => buffer[2] + 5, // 数据长度 + 地址 + 功能码 + 长度字节 + CRC(2字节)
            0x05 or 0x06 or 0x0F or 0x10 => 8, // 固定8字节响应
            _ => receivedBytes >= 8 ? receivedBytes : 8 // 默认期望8字节或当前已接收的字节数
        };
    }

    /// <summary>
    /// 检查连接健康状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否健康</returns>
    public Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            return Task.FromResult(false);

        try
        {
            // 使用轻量级的TCP探测
            var tcpClient = _tcpClient!;
            if (tcpClient.Client.Poll(1000, SelectMode.SelectRead) && tcpClient.Available == 0)
            {
                // 连接已断开
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public async Task DisconnectAsync()
    {
        try
        {
            if (_networkStream != null)
            {
                await _networkStream.FlushAsync();
                _networkStream.Close();
                _networkStream.Dispose();
                _networkStream = null;
            }

            if (_tcpClient != null)
            {
                _tcpClient.Close();
                _tcpClient.Dispose();
                _tcpClient = null;
            }

            _logger.LogDebug("已断开与设备 {DeviceType} 的连接", DeviceType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "断开设备 {DeviceType} 连接时发生异常", DeviceType);
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
            DisconnectAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // 忽略异常
        }

        _sendSemaphore?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 析构函数
    /// </summary>
    ~DeviceConnection()
    {
        Dispose();
    }
}