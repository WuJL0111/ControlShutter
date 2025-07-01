using System.Diagnostics;
using ControlShutter.Shared.Interfaces;
using ControlShutter.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace ControlShutter.Core.Controllers;

/// <summary>
/// 卷帘门控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ShutterController : ControllerBase
{
    private readonly IDoorControlService _doorControlService;
    private readonly ILogger<ShutterController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="doorControlService">门控制服务</param>
    /// <param name="logger">日志记录器</param>
    public ShutterController(
        IDoorControlService doorControlService,
        ILogger<ShutterController> logger)
    {
        _doorControlService = doorControlService ?? throw new ArgumentNullException(nameof(doorControlService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 控制卷帘门（异步）
    /// </summary>
    /// <param name="taskReceive">任务信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务执行结果</returns>
    /// <response code="200">任务执行成功</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="500">任务执行失败</response>
    [HttpPost("control")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TaskResponse>> ControlShutterAsync(
        [FromBody] TaskReceive taskReceive,
        CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("ControlShutter");
        activity?.SetTag("taskType", taskReceive.TaskType.ToString());
        activity?.SetTag("robotId", taskReceive.RobotId?.ToString());
        activity?.SetTag("taskId", taskReceive.TaskId?.ToString());

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("接收到卷帘门控制任务: {TaskType}, 任务ID: {TaskId}, 机器人ID: {RobotId}",
                taskReceive.TaskType, taskReceive.TaskId, taskReceive.RobotId);

            // 强制设置为默认卷帘门类型
            taskReceive.DeviceType = DeviceType.DefaultShutter;

            var result = await _doorControlService.ExecuteTaskAsync(taskReceive, cancellationToken);
            
            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("卷帘门控制任务完成: {TaskId}, 状态: {Status}, 耗时: {ElapsedMs}ms",
                taskReceive.TaskId, result.Status, stopwatch.ElapsedMilliseconds);

            return result.Status switch
            {
                200 => Ok(result),
                400 => BadRequest(result),
                499 => StatusCode(499, result),
                _ => StatusCode(500, result)
            };
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("卷帘门控制任务被取消: {TaskId}", taskReceive.TaskId);
            
            var cancelledResponse = TaskResponse.Cancelled("任务被取消", taskReceive.TaskId, taskReceive.RobotId);
            cancelledResponse.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            
            return StatusCode(499, cancelledResponse);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "卷帘门控制任务执行失败: {TaskId}", taskReceive.TaskId);
            
            var errorResponse = TaskResponse.Failed("任务执行异常", taskReceive.TaskId, taskReceive.RobotId);
            errorResponse.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// 开门操作
    /// </summary>
    /// <param name="request">开门请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpPost("open")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TaskResponse>> OpenDoorAsync(
        [FromBody] DoorOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        var taskReceive = new TaskReceive
        {
            TaskType = TaskType.OpenDoor,
            DeviceType = DeviceType.DefaultShutter,
            TaskId = request.TaskId,
            RobotId = request.RobotId,
            TimeoutSeconds = request.TimeoutSeconds ?? 90
        };

        return await ControlShutterAsync(taskReceive, cancellationToken);
    }

    /// <summary>
    /// 关门操作
    /// </summary>
    /// <param name="request">关门请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpPost("close")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TaskResponse>> CloseDoorAsync(
        [FromBody] DoorOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        var taskReceive = new TaskReceive
        {
            TaskType = TaskType.CloseDoor,
            DeviceType = DeviceType.DefaultShutter,
            TaskId = request.TaskId,
            RobotId = request.RobotId,
            TimeoutSeconds = request.TimeoutSeconds ?? 90
        };

        return await ControlShutterAsync(taskReceive, cancellationToken);
    }

    /// <summary>
    /// 获取卷帘门状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设备状态</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetShutterStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var isConnected = await _doorControlService.CheckDeviceConnectionAsync(
                DeviceType.DefaultShutter, cancellationToken);
            var deviceStatus = await _doorControlService.GetDeviceStatusAsync(
                DeviceType.DefaultShutter, cancellationToken);

            return Ok(new
            {
                DeviceType = DeviceType.DefaultShutter,
                IsConnected = isConnected,
                Status = deviceStatus != null ? Convert.ToHexString(deviceStatus) : null,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查卷帘门状态失败");
            return StatusCode(500, new { Error = "检查设备状态失败", Message = ex.Message });
        }
    }

    /// <summary>
    /// 健康检查
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>健康状态</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await _doorControlService.CheckDeviceConnectionAsync(
                DeviceType.DefaultShutter, cancellationToken);

            var status = isHealthy ? "Healthy" : "Unhealthy";
            var statusCode = isHealthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;

            return StatusCode(statusCode, new
            {
                Status = status,
                Service = "ControlShutter.Core",
                DeviceType = DeviceType.DefaultShutter,
                IsConnected = isHealthy,
                Timestamp = DateTime.UtcNow,
                Version = GetType().Assembly.GetName().Version?.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "健康检查失败");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                Status = "Unhealthy",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}

/// <summary>
/// 门操作请求模型
/// </summary>
public class DoorOperationRequest
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public long? TaskId { get; set; }

    /// <summary>
    /// 机器人ID
    /// </summary>
    public long? RobotId { get; set; }

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    public int? TimeoutSeconds { get; set; }
}