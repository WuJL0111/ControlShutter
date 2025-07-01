using System.ComponentModel.DataAnnotations;

namespace ControlShutter.Shared.Configuration;

/// <summary>
/// 设备连接配置选项
/// </summary>
public class DeviceConnectionOptions
{
    public const string SectionName = "DeviceConnections";
    
    /// <summary>
    /// 拉门设备配置
    /// </summary>
    public DeviceConfig PullDoor { get; set; } = new();
    
    /// <summary>
    /// 卷帘门设备配置
    /// </summary>
    public DeviceConfig ShutterDoor { get; set; } = new();
    
    /// <summary>
    /// 默认卷帘门配置
    /// </summary>
    public DeviceConfig DefaultShutter { get; set; } = new();
    
    /// <summary>
    /// 连接池配置
    /// </summary>
    public ConnectionPoolConfig ConnectionPool { get; set; } = new();
}

/// <summary>
/// 设备配置
/// </summary>
public class DeviceConfig
{
    /// <summary>
    /// 主机地址
    /// </summary>
    [Required]
    public string Host { get; set; } = string.Empty;
    
    /// <summary>
    /// 端口号
    /// </summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 10000;
    
    /// <summary>
    /// 连接超时时间（毫秒）
    /// </summary>
    [Range(1000, 30000)]
    public int ConnectTimeoutMs { get; set; } = 5000;
    
    /// <summary>
    /// 读取超时时间（毫秒）
    /// </summary>
    [Range(100, 10000)]
    public int ReadTimeoutMs { get; set; } = 3000;
    
    /// <summary>
    /// 写入超时时间（毫秒）
    /// </summary>
    [Range(100, 10000)]
    public int WriteTimeoutMs { get; set; } = 3000;
    
    /// <summary>
    /// 重试次数
    /// </summary>
    [Range(0, 10)]
    public int RetryCount { get; set; } = 3;
    
    /// <summary>
    /// 重试间隔（毫秒）
    /// </summary>
    [Range(100, 5000)]
    public int RetryDelayMs { get; set; } = 1000;
    
    /// <summary>
    /// 设备地址
    /// </summary>
    [Range(1, 255)]
    public byte DeviceAddress { get; set; } = 254;
}

/// <summary>
/// 连接池配置
/// </summary>
public class ConnectionPoolConfig
{
    /// <summary>
    /// 最大连接数
    /// </summary>
    [Range(1, 100)]
    public int MaxConnections { get; set; } = 10;
    
    /// <summary>
    /// 连接空闲超时时间（分钟）
    /// </summary>
    [Range(1, 60)]
    public int IdleTimeoutMinutes { get; set; } = 5;
    
    /// <summary>
    /// 连接检查间隔（分钟）
    /// </summary>
    [Range(1, 30)]
    public int HealthCheckIntervalMinutes { get; set; } = 2;
}

/// <summary>
/// 外部系统通知配置
/// </summary>
public class ExternalNotificationOptions
{
    public const string SectionName = "ExternalNotification";
    
    /// <summary>
    /// 是否启用外部通知
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// 开门任务完成通知URL
    /// </summary>
    public string OpenDoorNotificationUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// 关门任务完成通知URL
    /// </summary>
    public string CloseDoorNotificationUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// HTTP超时时间（毫秒）
    /// </summary>
    [Range(1000, 30000)]
    public int HttpTimeoutMs { get; set; } = 10000;
    
    /// <summary>
    /// 重试次数
    /// </summary>
    [Range(0, 5)]
    public int RetryCount { get; set; } = 3;
    
    /// <summary>
    /// 重试间隔（毫秒）
    /// </summary>
    [Range(1000, 10000)]
    public int RetryDelayMs { get; set; } = 2000;
}

/// <summary>
/// 日志配置
/// </summary>
public class LoggingOptions
{
    public const string SectionName = "Logging";
    
    /// <summary>
    /// 是否启用性能日志
    /// </summary>
    public bool EnablePerformanceLogging { get; set; } = true;
    
    /// <summary>
    /// 是否启用设备通信日志
    /// </summary>
    public bool EnableDeviceCommunicationLogging { get; set; } = true;
    
    /// <summary>
    /// 慢操作阈值（毫秒）
    /// </summary>
    [Range(100, 10000)]
    public int SlowOperationThresholdMs { get; set; } = 1000;
}