using ControlShutter.Shared.Models;

namespace ControlShutter.Desktop.Services;

/// <summary>
/// API客户端服务接口
/// </summary>
public interface IApiClientService
{
    /// <summary>
    /// 执行门控制任务
    /// </summary>
    /// <param name="taskReceive">任务信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务响应</returns>
    Task<TaskResponse> ExecuteTaskAsync(TaskReceive taskReceive, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取设备状态
    /// </summary>
    /// <param name="deviceType">设备类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设备状态</returns>
    Task<DeviceStatusResponse?> GetDeviceStatusAsync(DeviceType deviceType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查API连接状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否连接成功</returns>
    Task<bool> CheckConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取API健康状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>健康状态</returns>
    Task<HealthCheckResponse?> GetHealthStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 设备状态响应
/// </summary>
public class DeviceStatusResponse
{
    public DeviceType DeviceType { get; set; }
    public bool IsConnected { get; set; }
    public string? Status { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// 健康检查响应
/// </summary>
public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public DeviceType DeviceType { get; set; }
    public bool IsConnected { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Version { get; set; }
    public string? Error { get; set; }
}