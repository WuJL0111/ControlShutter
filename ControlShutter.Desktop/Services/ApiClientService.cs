using ControlShutter.Desktop.ViewModels;
using ControlShutter.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace ControlShutter.Desktop.Services;

/// <summary>
/// API客户端服务实现
/// </summary>
public class ApiClientService : IApiClientService
{
    private readonly HttpClient _httpClient;
    private readonly ApiClientOptions _options;
    private readonly ILogger<ApiClientService> _logger;

    public ApiClientService(
        HttpClient httpClient,
        IOptions<ApiClientOptions> options,
        ILogger<ApiClientService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 执行门控制任务
    /// </summary>
    public async Task<TaskResponse> ExecuteTaskAsync(TaskReceive taskReceive, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("执行门控制任务: 设备={DeviceType}, 操作={TaskType}", 
                taskReceive.DeviceType, taskReceive.TaskType);

            var endpoint = GetControlEndpoint(taskReceive.DeviceType);
            var json = JsonConvert.SerializeObject(taskReceive);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await ExecuteWithRetryAsync(async () =>
            {
                return await _httpClient.PostAsync(endpoint, content, cancellationToken);
            });

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<TaskResponse>(responseJson);
                _logger.LogInformation("门控制任务执行成功: {TaskId}", taskReceive.TaskId);
                return result ?? TaskResponse.Failed("响应解析失败", taskReceive.TaskId, taskReceive.RobotId);
            }
            else
            {
                _logger.LogWarning("门控制任务执行失败: {StatusCode}, {Response}", 
                    response.StatusCode, responseJson);
                
                // 尝试解析错误响应
                try
                {
                    var errorResponse = JsonConvert.DeserializeObject<TaskResponse>(responseJson);
                    return errorResponse ?? TaskResponse.Failed($"HTTP {response.StatusCode}: {response.ReasonPhrase}", 
                        taskReceive.TaskId, taskReceive.RobotId);
                }
                catch
                {
                    return TaskResponse.Failed($"HTTP {response.StatusCode}: {response.ReasonPhrase}", 
                        taskReceive.TaskId, taskReceive.RobotId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("门控制任务被取消: {TaskId}", taskReceive.TaskId);
            return TaskResponse.Cancelled("任务被取消", taskReceive.TaskId, taskReceive.RobotId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "门控制任务执行异常: {TaskId}", taskReceive.TaskId);
            return TaskResponse.Failed($"请求失败: {ex.Message}", taskReceive.TaskId, taskReceive.RobotId);
        }
    }

    /// <summary>
    /// 获取设备状态
    /// </summary>
    public async Task<DeviceStatusResponse?> GetDeviceStatusAsync(DeviceType deviceType, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("获取设备状态: {DeviceType}", deviceType);

            var endpoint = GetStatusEndpoint(deviceType);
            
            var response = await ExecuteWithRetryAsync(async () =>
            {
                return await _httpClient.GetAsync(endpoint, cancellationToken);
            });

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonConvert.DeserializeObject<DeviceStatusResponse>(json);
                
                _logger.LogDebug("设备状态获取成功: {DeviceType}", deviceType);
                return result;
            }
            else
            {
                _logger.LogWarning("获取设备状态失败: {DeviceType}, StatusCode={StatusCode}", 
                    deviceType, response.StatusCode);
                return null;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("获取设备状态被取消: {DeviceType}", deviceType);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取设备状态异常: {DeviceType}", deviceType);
            return null;
        }
    }

    /// <summary>
    /// 检查API连接状态
    /// </summary>
    public async Task<bool> CheckConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("检查API连接状态");

            var response = await _httpClient.GetAsync("/health", cancellationToken);
            var isConnected = response.IsSuccessStatusCode;

            _logger.LogDebug("API连接状态检查完成: {IsConnected}", isConnected);
            return isConnected;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("API连接检查被取消");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API连接检查失败");
            return false;
        }
    }

    /// <summary>
    /// 获取API健康状态
    /// </summary>
    public async Task<HealthCheckResponse?> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("获取API健康状态");

            var response = await _httpClient.GetAsync("/health", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonConvert.DeserializeObject<HealthCheckResponse>(json);
                
                _logger.LogDebug("API健康状态获取成功");
                return result;
            }
            else
            {
                _logger.LogWarning("获取API健康状态失败: StatusCode={StatusCode}", response.StatusCode);
                return null;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("获取API健康状态被取消");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取API健康状态异常");
            return null;
        }
    }

    /// <summary>
    /// 获取控制端点
    /// </summary>
    private static string GetControlEndpoint(DeviceType deviceType)
    {
        return deviceType switch
        {
            DeviceType.PullDoor => "/api/DoorControl/pull-door",
            DeviceType.ShutterDoor => "/api/DoorControl/shutter-door", 
            DeviceType.DefaultShutter => "/api/Shutter/control",
            _ => throw new ArgumentException($"不支持的设备类型: {deviceType}")
        };
    }

    /// <summary>
    /// 获取状态端点
    /// </summary>
    private static string GetStatusEndpoint(DeviceType deviceType)
    {
        return deviceType switch
        {
            DeviceType.PullDoor => "/api/DoorControl/pull-door/status",
            DeviceType.ShutterDoor => "/api/DoorControl/shutter-door/status",
            DeviceType.DefaultShutter => "/api/Shutter/status",
            _ => throw new ArgumentException($"不支持的设备类型: {deviceType}")
        };
    }

    /// <summary>
    /// 带重试的HTTP请求执行
    /// </summary>
    private async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        Func<Task<HttpResponseMessage>> operation,
        int maxRetries = -1)
    {
        var retryCount = maxRetries == -1 ? _options.RetryCount : maxRetries;
        var retryDelay = _options.RetryDelayMs;

        for (int attempt = 0; attempt <= retryCount; attempt++)
        {
            try
            {
                var response = await operation();
                
                // 如果是服务器错误且还有重试次数，则重试
                if (response.IsSuccessStatusCode || 
                    (int)response.StatusCode < 500 || 
                    attempt == retryCount)
                {
                    return response;
                }

                _logger.LogWarning("HTTP请求失败，准备重试: 尝试={Attempt}, StatusCode={StatusCode}", 
                    attempt + 1, response.StatusCode);
            }
            catch (Exception ex) when (attempt < retryCount)
            {
                _logger.LogWarning(ex, "HTTP请求异常，准备重试: 尝试={Attempt}", attempt + 1);
            }

            // 等待后重试
            if (attempt < retryCount)
            {
                await Task.Delay(retryDelay * (attempt + 1));
            }
        }

        // 如果所有重试都失败了，抛出最后一个异常
        return await operation();
    }
}