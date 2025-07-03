using ControlShutter.Shared.Configuration;
using ControlShutter.Shared.Interfaces;
using ControlShutter.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace ControlShutter.Infrastructure.Services;

/// <summary>
/// 外部通知服务实现
/// </summary>
public class ExternalNotificationService : IExternalNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ExternalNotificationOptions _options;
    private readonly ILogger<ExternalNotificationService> _logger;

    public ExternalNotificationService(
        HttpClient httpClient,
        IOptions<ExternalNotificationOptions> options,
        ILogger<ExternalNotificationService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 发送任务完成通知
    /// </summary>
    public async Task<bool> SendTaskCompletionNotificationAsync(ExternalNotification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_options.Enabled)
            {
                _logger.LogDebug("外部通知已禁用，跳过发送");
                return true;
            }

            var jsonContent = JsonSerializer.Serialize(notification, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            using var response = await _httpClient.PostAsync(_options.NotificationUrl, content, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("外部通知发送成功: {TaskId}", notification.TaskId);
                return true;
            }
            else
            {
                _logger.LogWarning("外部通知发送失败: {TaskId}, 状态码: {StatusCode}", 
                    notification.TaskId, response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送外部通知时发生异常: {TaskId}", notification.TaskId);
            return false;
        }
    }

    /// <summary>
    /// 发送开门任务完成通知
    /// </summary>
    public async Task<bool> SendOpenDoorNotificationAsync(TaskReceive taskReceive, bool isSuccess, string message, CancellationToken cancellationToken = default)
    {
        var notification = new ExternalNotification
        {
            TaskId = taskReceive.TaskId,
            RobotId = taskReceive.RobotId,
            DeviceType = taskReceive.DeviceType,
            TaskType = taskReceive.TaskType,
            IsSuccess = isSuccess,
            Message = message,
            CompletedAt = DateTime.Now,
            Data = new Dictionary<string, object>
            {
                ["action"] = "open_door",
                ["deviceType"] = taskReceive.DeviceType.ToString()
            }
        };

        return await SendTaskCompletionNotificationAsync(notification, cancellationToken);
    }

    /// <summary>
    /// 发送关门任务完成通知
    /// </summary>
    public async Task<bool> SendCloseDoorNotificationAsync(TaskReceive taskReceive, bool isSuccess, string message, string startTime, string endTime, CancellationToken cancellationToken = default)
    {
        var notification = new ExternalNotification
        {
            TaskId = taskReceive.TaskId,
            RobotId = taskReceive.RobotId,
            DeviceType = taskReceive.DeviceType,
            TaskType = taskReceive.TaskType,
            IsSuccess = isSuccess,
            Message = message,
            CompletedAt = DateTime.Now,
            Data = new Dictionary<string, object>
            {
                ["action"] = "close_door",
                ["deviceType"] = taskReceive.DeviceType.ToString(),
                ["startTime"] = startTime,
                ["endTime"] = endTime
            }
        };

        return await SendTaskCompletionNotificationAsync(notification, cancellationToken);
    }
}