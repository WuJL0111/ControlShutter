using ControlShutter.Shared.Models;

namespace ControlShutter.Shared.Interfaces;

/// <summary>
/// 门控制服务接口
/// </summary>
public interface IDoorControlService
{
    /// <summary>
    /// 执行门控制任务
    /// </summary>
    /// <param name="taskReceive">任务信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务响应</returns>
    Task<TaskResponse> ExecuteTaskAsync(TaskReceive taskReceive, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 检查设备连接状态
    /// </summary>
    /// <param name="deviceType">设备类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>连接状态</returns>
    Task<bool> CheckDeviceConnectionAsync(DeviceType deviceType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取设备状态
    /// </summary>
    /// <param name="deviceType">设备类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设备状态数据</returns>
    Task<byte[]?> GetDeviceStatusAsync(DeviceType deviceType, CancellationToken cancellationToken = default);
}

/// <summary>
/// Modbus通信服务接口
/// </summary>
public interface IModbusService
{
    /// <summary>
    /// 连接到设备
    /// </summary>
    /// <param name="deviceType">设备类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否连接成功</returns>
    Task<bool> ConnectAsync(DeviceType deviceType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 断开设备连接
    /// </summary>
    /// <param name="deviceType">设备类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DisconnectAsync(DeviceType deviceType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 写入数字输出
    /// </summary>
    /// <param name="deviceType">设备类型</param>
    /// <param name="address">设备地址</param>
    /// <param name="ioPort">IO端口</param>
    /// <param name="value">输出值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<TaskExecutionResult> WriteDigitalOutputAsync(DeviceType deviceType, byte address, int ioPort, bool value, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 读取数字输入
    /// </summary>
    /// <param name="deviceType">设备类型</param>
    /// <param name="address">设备地址</param>
    /// <param name="ioPort">IO端口</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>读取结果</returns>
    Task<TaskExecutionResult> ReadDigitalInputAsync(DeviceType deviceType, byte address, int ioPort, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 检查连接状态
    /// </summary>
    /// <param name="deviceType">设备类型</param>
    /// <returns>是否已连接</returns>
    bool IsConnected(DeviceType deviceType);
}

/// <summary>
/// 外部通知服务接口
/// </summary>
public interface IExternalNotificationService
{
    /// <summary>
    /// 发送任务完成通知
    /// </summary>
    /// <param name="notification">通知信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否发送成功</returns>
    Task<bool> SendTaskCompletionNotificationAsync(ExternalNotification notification, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 发送开门任务完成通知
    /// </summary>
    /// <param name="taskReceive">原始任务信息</param>
    /// <param name="isSuccess">是否成功</param>
    /// <param name="message">消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否发送成功</returns>
    Task<bool> SendOpenDoorNotificationAsync(TaskReceive taskReceive, bool isSuccess, string message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 发送关门任务完成通知
    /// </summary>
    /// <param name="taskReceive">原始任务信息</param>
    /// <param name="isSuccess">是否成功</param>
    /// <param name="message">消息</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否发送成功</returns>
    Task<bool> SendCloseDoorNotificationAsync(TaskReceive taskReceive, bool isSuccess, string message, string startTime, string endTime, CancellationToken cancellationToken = default);
}

/// <summary>
/// HTTP客户端服务接口
/// </summary>
public interface IHttpClientService
{
    /// <summary>
    /// 发送POST请求
    /// </summary>
    /// <param name="url">请求URL</param>
    /// <param name="jsonContent">JSON内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应内容</returns>
    Task<string> PostJsonAsync(string url, string jsonContent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 发送POST请求（带重试）
    /// </summary>
    /// <param name="url">请求URL</param>
    /// <param name="jsonContent">JSON内容</param>
    /// <param name="retryCount">重试次数</param>
    /// <param name="retryDelay">重试间隔</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应内容</returns>
    Task<string> PostJsonWithRetryAsync(string url, string jsonContent, int retryCount = 3, TimeSpan? retryDelay = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// 连接池管理接口
/// </summary>
public interface IConnectionPoolManager : IDisposable
{
    /// <summary>
    /// 获取或创建连接
    /// </summary>
    /// <param name="deviceType">设备类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>TCP客户端连接</returns>
    Task<IDeviceConnection> GetConnectionAsync(DeviceType deviceType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 释放连接
    /// </summary>
    /// <param name="deviceType">设备类型</param>
    /// <param name="connection">连接对象</param>
    Task ReleaseConnectionAsync(DeviceType deviceType, IDeviceConnection connection);
    
    /// <summary>
    /// 清理空闲连接
    /// </summary>
    Task CleanupIdleConnectionsAsync();
    
    /// <summary>
    /// 获取连接池状态
    /// </summary>
    /// <returns>连接池状态信息</returns>
    ConnectionPoolStatus GetPoolStatus();
}

/// <summary>
/// 设备连接接口
/// </summary>
public interface IDeviceConnection : IDisposable
{
    /// <summary>
    /// 设备类型
    /// </summary>
    DeviceType DeviceType { get; }
    
    /// <summary>
    /// 是否已连接
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// 最后使用时间
    /// </summary>
    DateTime LastUsedTime { get; }
    
    /// <summary>
    /// 发送数据
    /// </summary>
    /// <param name="data">要发送的数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应数据</returns>
    Task<byte[]?> SendDataAsync(byte[] data, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 检查连接健康状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否健康</returns>
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 连接池状态
/// </summary>
public class ConnectionPoolStatus
{
    /// <summary>
    /// 总连接数
    /// </summary>
    public int TotalConnections { get; set; }
    
    /// <summary>
    /// 活跃连接数
    /// </summary>
    public int ActiveConnections { get; set; }
    
    /// <summary>
    /// 空闲连接数
    /// </summary>
    public int IdleConnections { get; set; }
    
    /// <summary>
    /// 各设备类型的连接数
    /// </summary>
    public Dictionary<DeviceType, int> ConnectionsByDevice { get; set; } = new();
}