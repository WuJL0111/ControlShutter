using System.ComponentModel.DataAnnotations;

namespace ControlShutter.Shared.Models;

/// <summary>
/// 任务类型枚举
/// </summary>
public enum TaskType
{
    /// <summary>
    /// 开门
    /// </summary>
    OpenDoor = 0,
    
    /// <summary>
    /// 关门
    /// </summary>
    CloseDoor = 1
}

/// <summary>
/// 设备类型枚举
/// </summary>
public enum DeviceType
{
    /// <summary>
    /// 拉门
    /// </summary>
    PullDoor = 1,
    
    /// <summary>
    /// 卷帘门
    /// </summary>
    ShutterDoor = 2,
    
    /// <summary>
    /// 默认卷帘门
    /// </summary>
    DefaultShutter = 3
}

/// <summary>
/// 任务接收模型
/// </summary>
public class TaskReceive
{
    /// <summary>
    /// 任务类型
    /// </summary>
    [Required]
    public TaskType TaskType { get; set; }
    
    /// <summary>
    /// 机器人ID
    /// </summary>
    public long? RobotId { get; set; }
    
    /// <summary>
    /// 任务ID
    /// </summary>
    public long? TaskId { get; set; }
    
    /// <summary>
    /// 设备类型
    /// </summary>
    public DeviceType DeviceType { get; set; } = DeviceType.DefaultShutter;
    
    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 90;
}

/// <summary>
/// 任务响应模型
/// </summary>
public class TaskResponse
{
    /// <summary>
    /// 状态码 (200=成功, 400=参数错误, 500=执行失败, 499=取消)
    /// </summary>
    public int Status { get; set; }
    
    /// <summary>
    /// 响应消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// 任务ID
    /// </summary>
    public long? TaskId { get; set; }
    
    /// <summary>
    /// 机器人ID
    /// </summary>
    public long? RobotId { get; set; }
    
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 执行耗时（毫秒）
    /// </summary>
    public long ExecutionTimeMs { get; set; }
    
    /// <summary>
    /// 创建成功响应
    /// </summary>
    public static TaskResponse Success(string message = "任务执行成功", long? taskId = null, long? robotId = null)
    {
        return new TaskResponse
        {
            Status = 200,
            Message = message,
            TaskId = taskId,
            RobotId = robotId
        };
    }
    
    /// <summary>
    /// 创建失败响应
    /// </summary>
    public static TaskResponse Failed(string message = "任务执行失败", long? taskId = null, long? robotId = null)
    {
        return new TaskResponse
        {
            Status = 500,
            Message = message,
            TaskId = taskId,
            RobotId = robotId
        };
    }
    
    /// <summary>
    /// 创建参数错误响应
    /// </summary>
    public static TaskResponse BadRequest(string message = "参数错误", long? taskId = null, long? robotId = null)
    {
        return new TaskResponse
        {
            Status = 400,
            Message = message,
            TaskId = taskId,
            RobotId = robotId
        };
    }
    
    /// <summary>
    /// 创建取消响应
    /// </summary>
    public static TaskResponse Cancelled(string message = "任务被取消", long? taskId = null, long? robotId = null)
    {
        return new TaskResponse
        {
            Status = 499,
            Message = message,
            TaskId = taskId,
            RobotId = robotId
        };
    }
}

/// <summary>
/// 任务执行结果
/// </summary>
public class TaskExecutionResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// 结果消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// 异常信息
    /// </summary>
    public Exception? Exception { get; set; }
    
    /// <summary>
    /// 设备响应数据
    /// </summary>
    public byte[]? DeviceResponse { get; set; }
    
    /// <summary>
    /// 执行耗时
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }
    
    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static TaskExecutionResult Success(string message = "操作成功", byte[]? deviceResponse = null)
    {
        return new TaskExecutionResult
        {
            IsSuccess = true,
            Message = message,
            DeviceResponse = deviceResponse
        };
    }
    
    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static TaskExecutionResult Failed(string message, Exception? exception = null)
    {
        return new TaskExecutionResult
        {
            IsSuccess = false,
            Message = message,
            Exception = exception
        };
    }
}

/// <summary>
/// 外部系统通知模型
/// </summary>
public class ExternalNotification
{
    /// <summary>
    /// 状态码
    /// </summary>
    public int Code { get; set; }
    
    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// 机器人ID
    /// </summary>
    public long RobotId { get; set; }
    
    /// <summary>
    /// 机器人类型
    /// </summary>
    public int RobotType { get; set; } = 2;
    
    /// <summary>
    /// 任务ID
    /// </summary>
    public long TaskId { get; set; }
    
    /// <summary>
    /// 开始时间
    /// </summary>
    public string? StartTime { get; set; }
    
    /// <summary>
    /// 结束时间
    /// </summary>
    public string? EndTime { get; set; }
    
    /// <summary>
    /// 执行状态
    /// </summary>
    public int ExecutionStatus { get; set; }
    
    /// <summary>
    /// 反馈消息
    /// </summary>
    public string FeedbackMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// 重量（仅关门时使用）
    /// </summary>
    public double Weight { get; set; }
    
    /// <summary>
    /// 扫描信息
    /// </summary>
    public string? ScanInfo { get; set; }
    
    /// <summary>
    /// 视觉结果
    /// </summary>
    public bool VisionResult { get; set; }
    
    /// <summary>
    /// RFID结果
    /// </summary>
    public List<string> RfidResult { get; set; } = new();
}