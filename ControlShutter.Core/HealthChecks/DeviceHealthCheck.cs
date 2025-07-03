using ControlShutter.Shared.Interfaces;
using ControlShutter.Shared.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ControlShutter.Core.HealthChecks;

/// <summary>
/// 设备健康检查
/// </summary>
public class DeviceHealthCheck : IHealthCheck
{
    private readonly IDoorControlService _doorControlService;
    private readonly ILogger<DeviceHealthCheck> _logger;

    public DeviceHealthCheck(
        IDoorControlService doorControlService,
        ILogger<DeviceHealthCheck> logger)
    {
        _doorControlService = doorControlService ?? throw new ArgumentNullException(nameof(doorControlService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 检查健康状态
    /// </summary>
    /// <param name="context">健康检查上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>健康检查结果</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var healthData = new Dictionary<string, object>();
            var allHealthy = true;
            var messages = new List<string>();

            // 检查所有设备类型的连接状态
            foreach (DeviceType deviceType in Enum.GetValues<DeviceType>())
            {
                try
                {
                    var isConnected = await _doorControlService.CheckDeviceConnectionAsync(deviceType, cancellationToken);
                    healthData[$"device_{deviceType}"] = isConnected ? "connected" : "disconnected";
                    
                    if (!isConnected)
                    {
                        allHealthy = false;
                        messages.Add($"设备 {deviceType} 连接失败");
                    }
                }
                catch (Exception ex)
                {
                    allHealthy = false;
                    healthData[$"device_{deviceType}"] = "error";
                    messages.Add($"设备 {deviceType} 检查异常: {ex.Message}");
                    _logger.LogWarning(ex, "检查设备 {DeviceType} 健康状态时发生异常", deviceType);
                }
            }

            var status = allHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy;
            var description = allHealthy ? "所有设备连接正常" : string.Join("; ", messages);

            return new HealthCheckResult(status, description, data: healthData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设备健康检查失败");
            
            return new HealthCheckResult(
                HealthStatus.Unhealthy, 
                $"设备健康检查异常: {ex.Message}",
                ex);
        }
    }
}