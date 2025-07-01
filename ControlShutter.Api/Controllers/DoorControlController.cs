using System.Diagnostics;
using ControlShutter.Shared.Interfaces;
using ControlShutter.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace ControlShutter.Api.Controllers;

/// <summary>
/// 门控制API控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DoorControlController : ControllerBase
{
    private readonly IDoorControlService _doorControlService;
    private readonly ILogger<DoorControlController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="doorControlService">门控制服务</param>
    /// <param name="logger">日志记录器</param>
    public DoorControlController(
        IDoorControlService doorControlService,
        ILogger<DoorControlController> logger)
    {
        _doorControlService = doorControlService ?? throw new ArgumentNullException(nameof(doorControlService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 控制拉门
    /// </summary>
    /// <param name="taskReceive">任务信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务执行结果</returns>
    /// <response code="200">任务执行成功</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="500">任务执行失败</response>
    [HttpPost("pull-door")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TaskResponse>> ControlPullDoorAsync(
        [FromBody] TaskReceive taskReceive,
        CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("ControlPullDoor");
        activity?.SetTag("taskType", taskReceive.TaskType.ToString());
        activity?.SetTag("robotId", taskReceive.RobotId?.ToString());
        activity?.SetTag("taskId", taskReceive.TaskId?.ToString());

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("接收到拉门控制任务: {TaskType}, 任务ID: {TaskId}, 机器人ID: {RobotId}",
                taskReceive.TaskType, taskReceive.TaskId, taskReceive.RobotId);

            // 设置设备类型为拉门
            taskReceive.DeviceType = DeviceType.PullDoor;

            var result = await _doorControlService.ExecuteTaskAsync(taskReceive, cancellationToken);
            
            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("拉门控制任务完成: {TaskId}, 状态: {Status}, 耗时: {ElapsedMs}ms",
                taskReceive.TaskId, result.Status, stopwatch.ElapsedMilliseconds);

            return result.Status switch
            {
                200 => Ok(result),
                400 => BadRequest(result),
                499 => StatusCode(499, result), // Client Closed Request
                _ => StatusCode(500, result)
            };
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("拉门控制任务被取消: {TaskId}", taskReceive.TaskId);
            
            var cancelledResponse = TaskResponse.Cancelled("任务被取消", taskReceive.TaskId, taskReceive.RobotId);
            cancelledResponse.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            
            return StatusCode(499, cancelledResponse);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "拉门控制任务执行失败: {TaskId}", taskReceive.TaskId);
            
            var errorResponse = TaskResponse.Failed("任务执行异常", taskReceive.TaskId, taskReceive.RobotId);
            errorResponse.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// 控制卷帘门
    /// </summary>
    /// <param name="taskReceive">任务信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务执行结果</returns>
    /// <response code="200">任务执行成功</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="500">任务执行失败</response>
    [HttpPost("shutter-door")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TaskResponse>> ControlShutterDoorAsync(
        [FromBody] TaskReceive taskReceive,
        CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("ControlShutterDoor");
        activity?.SetTag("taskType", taskReceive.TaskType.ToString());
        activity?.SetTag("robotId", taskReceive.RobotId?.ToString());
        activity?.SetTag("taskId", taskReceive.TaskId?.ToString());

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("接收到卷帘门控制任务: {TaskType}, 任务ID: {TaskId}, 机器人ID: {RobotId}",
                taskReceive.TaskType, taskReceive.TaskId, taskReceive.RobotId);

            // 设置设备类型为卷帘门
            taskReceive.DeviceType = DeviceType.ShutterDoor;

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
    /// 控制默认卷帘门
    /// </summary>
    /// <param name="taskReceive">任务信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务执行结果</returns>
    /// <response code="200">任务执行成功</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="500">任务执行失败</response>
    [HttpPost("default-shutter")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TaskResponse>> ControlDefaultShutterAsync(
        [FromBody] TaskReceive taskReceive,
        CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("ControlDefaultShutter");
        activity?.SetTag("taskType", taskReceive.TaskType.ToString());
        activity?.SetTag("robotId", taskReceive.RobotId?.ToString());
        activity?.SetTag("taskId", taskReceive.TaskId?.ToString());

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("接收到默认卷帘门控制任务: {TaskType}, 任务ID: {TaskId}, 机器人ID: {RobotId}",
                taskReceive.TaskType, taskReceive.TaskId, taskReceive.RobotId);

            // 设置设备类型为默认卷帘门
            taskReceive.DeviceType = DeviceType.DefaultShutter;

            var result = await _doorControlService.ExecuteTaskAsync(taskReceive, cancellationToken);
            
            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("默认卷帘门控制任务完成: {TaskId}, 状态: {Status}, 耗时: {ElapsedMs}ms",
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
            _logger.LogWarning("默认卷帘门控制任务被取消: {TaskId}", taskReceive.TaskId);
            
            var cancelledResponse = TaskResponse.Cancelled("任务被取消", taskReceive.TaskId, taskReceive.RobotId);
            cancelledResponse.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            
            return StatusCode(499, cancelledResponse);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "默认卷帘门控制任务执行失败: {TaskId}", taskReceive.TaskId);
            
            var errorResponse = TaskResponse.Failed("任务执行异常", taskReceive.TaskId, taskReceive.RobotId);
            errorResponse.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// 检查设备连接状态
    /// </summary>
    /// <param name="deviceType">设备类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>连接状态</returns>
    /// <response code="200">检查成功</response>
    /// <response code="400">参数错误</response>
    [HttpGet("status/{deviceType}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CheckDeviceStatusAsync(
        DeviceType deviceType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isConnected = await _doorControlService.CheckDeviceConnectionAsync(deviceType, cancellationToken);
            var deviceStatus = await _doorControlService.GetDeviceStatusAsync(deviceType, cancellationToken);

            return Ok(new
            {
                DeviceType = deviceType,
                IsConnected = isConnected,
                Status = deviceStatus != null ? Convert.ToHexString(deviceStatus) : null,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查设备 {DeviceType} 状态失败", deviceType);
            return StatusCode(500, new { Error = "检查设备状态失败", Message = ex.Message });
        }
    }

    /// <summary>
    /// 获取所有设备状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>所有设备状态</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetAllDeviceStatusAsync(CancellationToken cancellationToken = default)
    {
        var deviceTypes = Enum.GetValues<DeviceType>();
        var deviceStatuses = new List<object>();

        foreach (var deviceType in deviceTypes)
        {
            try
            {
                var isConnected = await _doorControlService.CheckDeviceConnectionAsync(deviceType, cancellationToken);
                var deviceStatus = await _doorControlService.GetDeviceStatusAsync(deviceType, cancellationToken);

                deviceStatuses.Add(new
                {
                    DeviceType = deviceType,
                    IsConnected = isConnected,
                    Status = deviceStatus != null ? Convert.ToHexString(deviceStatus) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "无法获取设备 {DeviceType} 状态", deviceType);
                deviceStatuses.Add(new
                {
                    DeviceType = deviceType,
                    IsConnected = false,
                    Status = (string?)null,
                    Error = ex.Message
                });
            }
        }

        return Ok(new
        {
            Devices = deviceStatuses,
            Timestamp = DateTime.UtcNow
        });
    }
}