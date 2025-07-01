using ControlShutter.Shared.Configuration;
using ControlShutter.Shared.Interfaces;
using ControlShutter.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace ControlShutter.Infrastructure.Services;

/// <summary>
/// 门控制服务实现
/// </summary>
public class DoorControlService : IDoorControlService
{
    private readonly IModbusService _modbusService;
    private readonly IExternalNotificationService _notificationService;
    private readonly DeviceConnectionOptions _deviceOptions;
    private readonly ILogger<DoorControlService> _logger;

    public DoorControlService(
        IModbusService modbusService,
        IExternalNotificationService notificationService,
        IOptions<DeviceConnectionOptions> deviceOptions,
        ILogger<DoorControlService> logger)
    {
        _modbusService = modbusService ?? throw new ArgumentNullException(nameof(modbusService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _deviceOptions = deviceOptions?.Value ?? throw new ArgumentNullException(nameof(deviceOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 执行门控制任务
    /// </summary>
    public async Task<TaskResponse> ExecuteTaskAsync(TaskReceive taskReceive, CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("ExecuteDoorTask");
        activity?.SetTag("deviceType", taskReceive.DeviceType.ToString());
        activity?.SetTag("taskType", taskReceive.TaskType.ToString());
        activity?.SetTag("taskId", taskReceive.TaskId?.ToString());

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("开始执行门控制任务: 设备={DeviceType}, 任务类型={TaskType}, 任务ID={TaskId}",
                taskReceive.DeviceType, taskReceive.TaskType, taskReceive.TaskId);

            // 验证任务参数
            var validationResult = ValidateTask(taskReceive);
            if (!validationResult.IsValid)
            {
                return TaskResponse.BadRequest(validationResult.ErrorMessage, taskReceive.TaskId, taskReceive.RobotId);
            }

            // 获取设备配置
            var deviceConfig = GetDeviceConfig(taskReceive.DeviceType);

            // 创建任务的取消令牌（带超时）
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(taskReceive.TimeoutSeconds));

            // 执行门控制操作
            var executionResult = await ExecuteDoorOperationAsync(
                taskReceive, 
                deviceConfig, 
                timeoutCts.Token);

            stopwatch.Stop();

            // 创建响应
            var response = executionResult.IsSuccess 
                ? TaskResponse.Success(executionResult.Message, taskReceive.TaskId, taskReceive.RobotId)
                : TaskResponse.Failed(executionResult.Message, taskReceive.TaskId, taskReceive.RobotId);

            response.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

            // 发送外部通知（异步执行，不等待结果）
            _ = Task.Run(async () =>
            {
                try
                {
                    await SendNotificationAsync(taskReceive, executionResult.IsSuccess, executionResult.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "发送外部通知失败: 任务ID={TaskId}", taskReceive.TaskId);
                }
            }, CancellationToken.None);

            _logger.LogInformation("门控制任务完成: 设备={DeviceType}, 任务ID={TaskId}, 状态={Status}, 耗时={ElapsedMs}ms",
                taskReceive.DeviceType, taskReceive.TaskId, response.Status, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning("门控制任务被用户取消: 任务ID={TaskId}", taskReceive.TaskId);
            
            var response = TaskResponse.Cancelled("任务被取消", taskReceive.TaskId, taskReceive.RobotId);
            response.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            return response;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("门控制任务超时: 任务ID={TaskId}, 超时时间={TimeoutSeconds}秒", 
                taskReceive.TaskId, taskReceive.TimeoutSeconds);
            
            var response = TaskResponse.Failed("任务执行超时", taskReceive.TaskId, taskReceive.RobotId);
            response.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "门控制任务执行失败: 任务ID={TaskId}", taskReceive.TaskId);
            
            var response = TaskResponse.Failed($"任务执行异常: {ex.Message}", taskReceive.TaskId, taskReceive.RobotId);
            response.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            return response;
        }
    }

    /// <summary>
    /// 检查设备连接状态
    /// </summary>
    public async Task<bool> CheckDeviceConnectionAsync(DeviceType deviceType, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("检查设备 {DeviceType} 连接状态", deviceType);
            return await _modbusService.ConnectAsync(deviceType, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查设备 {DeviceType} 连接状态失败", deviceType);
            return false;
        }
    }

    /// <summary>
    /// 获取设备状态
    /// </summary>
    public async Task<byte[]?> GetDeviceStatusAsync(DeviceType deviceType, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("获取设备 {DeviceType} 状态", deviceType);
            
            var deviceConfig = GetDeviceConfig(deviceType);
            var result = await _modbusService.ReadDigitalInputAsync(
                deviceType, 
                deviceConfig.DeviceAddress, 
                2, // 状态端口
                cancellationToken);

            return result.IsSuccess ? result.DeviceResponse : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取设备 {DeviceType} 状态失败", deviceType);
            return null;
        }
    }

    /// <summary>
    /// 执行门控制操作
    /// </summary>
    private async Task<TaskExecutionResult> ExecuteDoorOperationAsync(
        TaskReceive taskReceive,
        DeviceConfig deviceConfig,
        CancellationToken cancellationToken)
    {
        var deviceType = taskReceive.DeviceType;
        var taskType = taskReceive.TaskType;
        var deviceAddress = deviceConfig.DeviceAddress;

        _logger.LogInformation("执行门控制操作: 设备={DeviceType}, 操作={TaskType}", deviceType, taskType);

        try
        {
            // 确保设备连接
            var isConnected = await _modbusService.ConnectAsync(deviceType, cancellationToken);
            if (!isConnected)
            {
                return TaskExecutionResult.Failed($"无法连接到设备 {deviceType}");
            }

            // 根据任务类型和设备类型执行相应操作
            return taskType switch
            {
                TaskType.OpenDoor => await ExecuteOpenDoorAsync(deviceType, deviceAddress, cancellationToken),
                TaskType.CloseDoor => await ExecuteCloseDoorAsync(deviceType, deviceAddress, cancellationToken),
                _ => TaskExecutionResult.Failed($"不支持的任务类型: {taskType}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行门控制操作失败: 设备={DeviceType}, 操作={TaskType}", deviceType, taskType);
            return TaskExecutionResult.Failed($"操作执行失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 执行开门操作
    /// </summary>
    private async Task<TaskExecutionResult> ExecuteOpenDoorAsync(
        DeviceType deviceType,
        byte deviceAddress,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始执行开门操作: 设备={DeviceType}", deviceType);

        try
        {
            // 步骤1: 发送开门信号
            var openSignalResult = await _modbusService.WriteDigitalOutputAsync(
                deviceType, deviceAddress, GetOpenDoorPort(deviceType), true, cancellationToken);

            if (!openSignalResult.IsSuccess)
            {
                return TaskExecutionResult.Failed($"发送开门信号失败: {openSignalResult.Message}");
            }

            _logger.LogDebug("开门信号发送成功，等待2秒");
            await Task.Delay(2000, cancellationToken);

            // 步骤2: 取消开门信号
            var cancelSignalResult = await _modbusService.WriteDigitalOutputAsync(
                deviceType, deviceAddress, GetOpenDoorPort(deviceType), false, cancellationToken);

            if (!cancelSignalResult.IsSuccess)
            {
                _logger.LogWarning("取消开门信号失败，但开门操作可能已执行: {Message}", cancelSignalResult.Message);
            }
            else
            {
                _logger.LogDebug("开门信号已取消");
            }

            // 步骤3: 等待门完全打开 (根据原代码，开门后需要等待2分钟后发送关门信号)
            if (deviceType == DeviceType.PullDoor)
            {
                _logger.LogInformation("拉门开门操作完成，等待2分钟后自动关门");
                await Task.Delay(TimeSpan.FromMinutes(2), cancellationToken);

                // 发送关门信号
                var closeSignalResult = await _modbusService.WriteDigitalOutputAsync(
                    deviceType, deviceAddress, GetCloseDoorPort(deviceType), true, cancellationToken);

                if (closeSignalResult.IsSuccess)
                {
                    await Task.Delay(2000, cancellationToken);
                    await _modbusService.WriteDigitalOutputAsync(
                        deviceType, deviceAddress, GetCloseDoorPort(deviceType), false, cancellationToken);
                    _logger.LogInformation("拉门自动关门操作完成");
                }
            }

            _logger.LogInformation("开门操作成功完成: 设备={DeviceType}", deviceType);
            return TaskExecutionResult.Success("开门操作成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "开门操作失败: 设备={DeviceType}", deviceType);
            return TaskExecutionResult.Failed($"开门操作失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 执行关门操作
    /// </summary>
    private async Task<TaskExecutionResult> ExecuteCloseDoorAsync(
        DeviceType deviceType,
        byte deviceAddress,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始执行关门操作: 设备={DeviceType}", deviceType);

        try
        {
            // 步骤1: 发送关门信号
            var closeSignalResult = await _modbusService.WriteDigitalOutputAsync(
                deviceType, deviceAddress, GetCloseDoorPort(deviceType), true, cancellationToken);

            if (!closeSignalResult.IsSuccess)
            {
                return TaskExecutionResult.Failed($"发送关门信号失败: {closeSignalResult.Message}");
            }

            _logger.LogDebug("关门信号发送成功，等待2秒");
            await Task.Delay(2000, cancellationToken);

            // 步骤2: 取消关门信号
            var cancelSignalResult = await _modbusService.WriteDigitalOutputAsync(
                deviceType, deviceAddress, GetCloseDoorPort(deviceType), false, cancellationToken);

            if (!cancelSignalResult.IsSuccess)
            {
                _logger.LogWarning("取消关门信号失败，但关门操作可能已执行: {Message}", cancelSignalResult.Message);
            }
            else
            {
                _logger.LogDebug("关门信号已取消");
            }

            _logger.LogInformation("关门操作成功完成: 设备={DeviceType}", deviceType);
            return TaskExecutionResult.Success("关门操作成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "关门操作失败: 设备={DeviceType}", deviceType);
            return TaskExecutionResult.Failed($"关门操作失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 发送外部通知
    /// </summary>
    private async Task SendNotificationAsync(TaskReceive taskReceive, bool isSuccess, string message)
    {
        try
        {
            if (taskReceive.TaskType == TaskType.OpenDoor)
            {
                await _notificationService.SendOpenDoorNotificationAsync(
                    taskReceive, isSuccess, message);
            }
            else if (taskReceive.TaskType == TaskType.CloseDoor)
            {
                var startTime = DateTime.Now.AddMilliseconds(-1000).ToString("yyyy-MM-dd HH:mm:ss");
                var endTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                
                await _notificationService.SendCloseDoorNotificationAsync(
                    taskReceive, isSuccess, message, startTime, endTime);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "发送外部通知失败: 任务ID={TaskId}", taskReceive.TaskId);
        }
    }

    /// <summary>
    /// 验证任务参数
    /// </summary>
    private static (bool IsValid, string ErrorMessage) ValidateTask(TaskReceive taskReceive)
    {
        if (!Enum.IsDefined(typeof(TaskType), taskReceive.TaskType))
        {
            return (false, $"无效的任务类型: {taskReceive.TaskType}");
        }

        if (!Enum.IsDefined(typeof(DeviceType), taskReceive.DeviceType))
        {
            return (false, $"无效的设备类型: {taskReceive.DeviceType}");
        }

        if (taskReceive.TimeoutSeconds <= 0 || taskReceive.TimeoutSeconds > 300)
        {
            return (false, "超时时间必须在1-300秒之间");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// 获取设备配置
    /// </summary>
    private DeviceConfig GetDeviceConfig(DeviceType deviceType)
    {
        return deviceType switch
        {
            DeviceType.PullDoor => _deviceOptions.PullDoor,
            DeviceType.ShutterDoor => _deviceOptions.ShutterDoor,
            DeviceType.DefaultShutter => _deviceOptions.DefaultShutter,
            _ => throw new ArgumentException($"不支持的设备类型: {deviceType}")
        };
    }

    /// <summary>
    /// 获取开门端口号
    /// </summary>
    private static int GetOpenDoorPort(DeviceType deviceType)
    {
        return deviceType switch
        {
            DeviceType.PullDoor => 1,        // 拉门开门端口
            DeviceType.ShutterDoor => 1,     // 卷帘门开门端口
            DeviceType.DefaultShutter => 0,  // 默认卷帘门开门端口
            _ => throw new ArgumentException($"不支持的设备类型: {deviceType}")
        };
    }

    /// <summary>
    /// 获取关门端口号
    /// </summary>
    private static int GetCloseDoorPort(DeviceType deviceType)
    {
        return deviceType switch
        {
            DeviceType.PullDoor => 0,        // 拉门关门端口
            DeviceType.ShutterDoor => 0,     // 卷帘门关门端口  
            DeviceType.DefaultShutter => 1,  // 默认卷帘门关门端口
            _ => throw new ArgumentException($"不支持的设备类型: {deviceType}")
        };
    }
}