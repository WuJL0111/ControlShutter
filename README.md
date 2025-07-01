# 门控制系统 (Door Control System)

## 项目概述

这是一个基于 .NET 的门控制系统，用于控制卷帘门和拉门的开关操作。系统采用模块化架构，包含三个主要项目：Web API 服务、WPF 桌面应用程序和门控制核心服务。

## 项目架构

### 项目结构
```
ControlShutter.sln
├── ControlDoors/          # WPF 桌面应用程序
├── ControlDoorsApi/       # Web API 服务 (.NET 6.0)
└── ControlShutter/        # 门控制核心服务 (.NET 8.0)
```

### 核心组件关系图
```
┌─────────────────┐    HTTP    ┌─────────────────┐    Modbus/TCP    ┌─────────────────┐
│   WPF Client    │◄──────────►│   Web API       │◄────────────────►│  Door Hardware  │
│  (ControlDoors) │            │(ControlDoorsApi)│                  │   Controllers   │
└─────────────────┘            └─────────────────┘                  └─────────────────┘
                                        │
                                        │ HTTP
                                        ▼
                               ┌─────────────────┐    Modbus/TCP    ┌─────────────────┐
                               │ Control Service │◄────────────────►│  Shutter Door   │
                               │(ControlShutter) │                  │   Hardware      │
                               └─────────────────┘                  └─────────────────┘
```

## 函数接口分析

### 1. Web API 接口 (ControlDoorsApi)

#### 主要控制器：`ControlDoors`
- **端点路径**: `[controller]/[action]`
- **CORS 支持**: 允许来自 `http://192.168.20.110:12581` 的跨域请求

##### 接口方法：

**拉门控制接口**
```csharp
[HttpPost]
public TaskRequest ControlPullDoor([FromBody] TaskReceive receive)
```
- **功能**: 控制拉门的开关操作
- **输入**: `TaskReceive` - 包含任务类型
- **输出**: `TaskRequest` - 任务执行状态

**卷帘门控制接口**
```csharp
[HttpPost]
public TaskRequest ControlShutterDoor([FromBody] TaskReceive receive)
```
- **功能**: 控制卷帘门的开关操作
- **输入**: `TaskReceive` - 包含任务类型
- **输出**: `TaskRequest` - 任务执行状态

### 2. 门控制服务接口 (ControlShutter)

#### 主要控制器：`ShutterController`

**卷帘门异步控制接口**
```csharp
[HttpPost]
public TaskRequest ControlShutterAsync([FromBody] TaskReceive receive)
```
- **功能**: 异步控制卷帘门操作
- **输入**: `TaskReceive` - 任务信息
- **输出**: `TaskRequest` - 执行结果

### 3. 数据传输对象 (DTOs)

#### TaskReceive
```csharp
public class TaskReceive
{
    public int taskType { get; set; }    // 任务类型 (0=开门, 1=关门)
    public long robotId { get; set; }     // 机器人ID (仅在某些实现中)
    public long taskId { get; set; }      // 任务ID (仅在某些实现中)
}
```

#### TaskRequest
```csharp
public class TaskRequest
{
    public int status { get; set; }       // 状态码 (200=成功, 500=失败)
    public string msg { get; set; }       // 状态消息
}
```

#### OpenShutterTaskRequest
```csharp
public class OpenShutterTaskRequest
{
    public int code { get; set; }
    public string msg { get; set; }
    public long robotId { get; set; }
    public int robotType { get; set; }
    public long taskId { get; set; }
}
```

#### CloseShutterTaskRequest
```csharp
public class CloseShutterTaskRequest
{
    public long robotId { get; set; }
    public long taskId { get; set; }
    public double weight { get; set; }
    public string scanInfo { get; set; }
    public bool visionResult { get; set; }
    public List<string> rfidResult { get; set; }
    public int executionStatus { get; set; }
    public string feedbackMsg { get; set; }
    public string startTime { get; set; }
    public string endTime { get; set; }
}
```

## 核心功能模块

### 1. Modbus 通信模块

#### ShutterClass (单例模式)
**主要功能**:
- TCP 连接管理
- Modbus RTU 协议通信
- 数字输入/输出控制

**核心方法**:
```csharp
// 连接方法
public bool ConnetPullDoor()           // 连接拉门设备 (192.168.20.91:10000)
public bool ConnetShutterDoor()        // 连接卷帘门设备 (192.168.20.93:10000)
public bool Connet()                   // 连接默认设备 (192.168.10.23:10000)

// 控制方法
public void OpenDO(int addr, int io)   // 打开数字输出
public void CloseDO(int addr, int io)  // 关闭数字输出
public byte[] ReadDI(int addr, int io) // 读取数字输入
```

#### CModbusDll (静态工具类)
**功能**: Modbus 协议数据包生成
```csharp
public static byte[] WriteDO(int addr, int io, bool openclose)     // 写单个数字输出
public static byte[] WriteAllDO(int addr, int ionum, bool openclose) // 写多个数字输出
public static byte[] ReadDO(int addr, int donum)                   // 读数字输出
public static byte[] ReadDI(int addr, int dinum)                   // 读数字输入
public static byte[] ReadAIInfo(int addr, int regstart, int regnum) // 读模拟输入
public static byte[] WriteAOInfo(int addr, int regstart, short ao)  // 写模拟输出
```

#### CMBRTU (静态工具类)
**功能**: CRC 校验计算
```csharp
public static ushort CalculateCrc(byte[] data)
public static ushort CalculateCrc(byte[] data, int len)
public static byte[] ModbusRTU(byte[] src)
```

### 2. HTTP 通信模块

#### Http 类
**功能**: HTTP POST 请求发送
```csharp
public string PostJson(string url, string postInfo)
```

### 3. WPF 客户端模块

#### MainViewModel (MVVM 模式)
**功能**: 主界面视图模型，负责页面导航
```csharp
public FrameworkElement MainContent { get; set; }     // 主要内容区域
public CommandBase NavChangedCommand { get; set; }    // 导航命令
```

#### CommandBase (命令模式)
**功能**: 实现 ICommand 接口，支持 WPF 数据绑定
```csharp
public Action<object> DoExecute { get; set; }
public Func<object,bool> DoCanExecute { get; set; }
```

## 代码优化建议

### 1. 架构层面优化

#### 问题分析：
1. **代码重复**: `ControlDoorsApi` 和 `ControlShutter` 项目中存在大量重复代码
2. **硬编码**: IP 地址和端口号硬编码在代码中
3. **异常处理**: 异常处理不够完善，有些地方直接忽略异常
4. **数据传输**: TaskReceive 类在不同项目中定义不一致

#### 优化方案：

**1. 创建共享类库**
```csharp
// ControlDoors.Shared 项目
namespace ControlDoors.Shared
{
    public class TaskReceive
    {
        public int taskType { get; set; }
        public long? robotId { get; set; }
        public long? taskId { get; set; }
    }
    
    public class TaskRequest
    {
        public int status { get; set; }
        public string msg { get; set; }
        public DateTime timestamp { get; set; } = DateTime.Now;
    }
}
```

**2. 配置化连接信息**
```csharp
// appsettings.json
{
  "DeviceConnections": {
    "PullDoor": {
      "Host": "192.168.20.91",
      "Port": 10000
    },
    "ShutterDoor": {
      "Host": "192.168.20.93", 
      "Port": 10000
    },
    "DefaultShutter": {
      "Host": "192.168.10.23",
      "Port": 10000
    }
  }
}
```

**3. 依赖注入重构**
```csharp
// IShutterService 接口
public interface IShutterService
{
    Task<bool> ConnectAsync(string deviceType);
    Task<TaskRequest> OpenDoorAsync(int deviceId, CancellationToken cancellationToken = default);
    Task<TaskRequest> CloseDoorAsync(int deviceId, CancellationToken cancellationToken = default);
    Task<byte[]> ReadDIAsync(int addr, int io);
}

// 在 Program.cs 中注册
builder.Services.AddSingleton<IShutterService, ShutterService>();
builder.Services.Configure<DeviceConnectionOptions>(
    builder.Configuration.GetSection("DeviceConnections"));
```

### 2. 代码质量优化

#### 异常处理优化
```csharp
public async Task<TaskRequest> ControlDoorAsync(TaskReceive receive, CancellationToken cancellationToken = default)
{
    var taskRequest = new TaskRequest();
    
    try
    {
        using var activity = ActivitySource.StartActivity("ControlDoor");
        activity?.SetTag("taskType", receive.taskType.ToString());
        activity?.SetTag("robotId", receive.robotId?.ToString());
        
        var result = await ProcessDoorCommandAsync(receive, cancellationToken);
        
        taskRequest.status = result.Success ? 200 : 500;
        taskRequest.msg = result.Message;
        
        _logger.LogInformation("任务 {TaskId} 执行完成，状态: {Status}", 
            receive.taskId, taskRequest.status);
            
        return taskRequest;
    }
    catch (OperationCanceledException)
    {
        _logger.LogWarning("任务 {TaskId} 被取消", receive.taskId);
        taskRequest.status = 499;
        taskRequest.msg = "任务被取消";
        return taskRequest;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "任务 {TaskId} 执行失败", receive.taskId);
        taskRequest.status = 500;
        taskRequest.msg = $"任务执行失败: {ex.Message}";
        return taskRequest;
    }
}
```

#### 资源管理优化
```csharp
public class ShutterService : IShutterService, IDisposable
{
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
    private TcpClient _tcpClient;
    private bool _disposed;
    
    public async Task<bool> ConnectAsync(string deviceType)
    {
        await _connectionSemaphore.WaitAsync();
        try
        {
            // 连接逻辑
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _tcpClient?.Dispose();
            _connectionSemaphore?.Dispose();
            _disposed = true;
        }
    }
}
```

### 3. 性能优化

#### 连接池管理
```csharp
public class ModbusConnectionPool
{
    private readonly ConcurrentDictionary<string, TcpClient> _connections = new();
    private readonly ILogger<ModbusConnectionPool> _logger;
    
    public async Task<TcpClient> GetConnectionAsync(string deviceKey)
    {
        return _connections.GetOrAdd(deviceKey, async key =>
        {
            var client = new TcpClient();
            var config = GetDeviceConfig(key);
            await client.ConnectAsync(config.Host, config.Port);
            return client;
        });
    }
}
```

#### 缓存机制
```csharp
public class CachedShutterService : IShutterService
{
    private readonly IMemoryCache _cache;
    private readonly IShutterService _innerService;
    
    public async Task<byte[]> ReadDIAsync(int addr, int io)
    {
        var cacheKey = $"DI_{addr}_{io}";
        
        if (_cache.TryGetValue(cacheKey, out byte[] cachedValue))
        {
            return cachedValue;
        }
        
        var result = await _innerService.ReadDIAsync(addr, io);
        
        _cache.Set(cacheKey, result, TimeSpan.FromSeconds(1)); // 1秒缓存
        
        return result;
    }
}
```

## 技术栈

### 后端技术
- **.NET 6.0/8.0**: Web API 框架
- **ASP.NET Core**: Web 服务框架
- **NLog**: 日志记录
- **Newtonsoft.Json**: JSON 序列化
- **Swagger**: API 文档生成
- **System.IO.Ports**: 串口通信

### 前端技术
- **WPF (.NET 6.0)**: 桌面应用程序框架
- **MVVM 模式**: 数据绑定和命令模式
- **LiveCharts.Wpf**: 图表组件

### 通信协议
- **Modbus TCP**: 设备通信协议
- **HTTP/HTTPS**: Web API 通信
- **TCP Socket**: 底层网络通信

## 部署建议

### 1. 容器化部署
```dockerfile
# Dockerfile for ControlDoorsApi
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app
COPY bin/Release/net6.0/publish/ .
EXPOSE 80
ENTRYPOINT ["dotnet", "ControlDoorsApi.dll"]
```

### 2. 配置管理
- 使用环境变量管理不同环境的配置
- 敏感信息使用 Azure Key Vault 或类似服务
- 设备连接信息支持动态配置

### 3. 监控和日志
- 集成 Application Insights 或 Prometheus
- 结构化日志记录
- 健康检查端点

## 安全考虑

1. **API 安全**: 实现 JWT 认证和授权
2. **网络安全**: 使用 HTTPS 和 VPN
3. **设备安全**: Modbus 通信加密
4. **输入验证**: 严格的参数验证
5. **审计日志**: 记录所有门控制操作

## 扩展建议

1. **支持更多设备类型**: 可配置的设备驱动程序
2. **实时监控**: SignalR 实时状态推送
3. **移动端支持**: Xamarin 或 MAUI 应用
4. **云端集成**: Azure IoT Hub 集成
5. **AI 功能**: 异常检测和预测性维护

## 维护指南

### 常见问题排查
1. **连接失败**: 检查网络连接和设备状态
2. **Modbus 通信错误**: 验证 CRC 校验和地址配置
3. **任务超时**: 调整超时设置和重试机制

### 性能监控指标
- API 响应时间
- 设备连接成功率
- 任务执行成功率
- 系统资源使用率

---

## 开发团队

如需技术支持或功能扩展，请联系开发团队。

**版本**: 1.0.0  
**最后更新**: 2024年12月