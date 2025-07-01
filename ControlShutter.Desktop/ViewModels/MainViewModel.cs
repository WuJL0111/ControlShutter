using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlShutter.Desktop.Services;
using ControlShutter.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows;

namespace ControlShutter.Desktop.ViewModels;

/// <summary>
/// 主视图模型
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IApiClientService _apiClientService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<MainViewModel> _logger;
    private readonly System.Timers.Timer _statusUpdateTimer;

    [ObservableProperty]
    private string _title = "门控制系统管理中心";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _connectionStatus = "未连接";

    [ObservableProperty]
    private string _selectedDeviceType = "拉门";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "准备就绪";

    [ObservableProperty]
    private object? _currentView;

    /// <summary>
    /// 设备状态列表
    /// </summary>
    public ObservableCollection<DeviceStatusInfo> DeviceStatuses { get; } = new();

    /// <summary>
    /// 操作日志
    /// </summary>
    public ObservableCollection<OperationLog> OperationLogs { get; } = new();

    /// <summary>
    /// 设备类型列表
    /// </summary>
    public List<string> DeviceTypes { get; } = new() { "拉门", "卷帘门", "默认卷帘门" };

    public MainViewModel(
        IApiClientService apiClientService,
        INotificationService notificationService,
        ILogger<MainViewModel> logger)
    {
        _apiClientService = apiClientService ?? throw new ArgumentNullException(nameof(apiClientService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 初始化定时器
        _statusUpdateTimer = new System.Timers.Timer(TimeSpan.FromSeconds(30));
        _statusUpdateTimer.Elapsed += async (s, e) => await UpdateDeviceStatusesAsync();
        _statusUpdateTimer.AutoReset = true;
        _statusUpdateTimer.Start();

        // 初始化数据
        _ = Task.Run(InitializeAsync);
    }

    /// <summary>
    /// 异步初始化
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            await UpdateDeviceStatusesAsync();
            await CheckConnectionAsync();
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusMessage = "系统已就绪";
                AddOperationLog("系统启动", "系统初始化完成", true);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化失败");
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusMessage = "系统初始化失败";
                AddOperationLog("系统启动", $"初始化失败: {ex.Message}", false);
            });
        }
    }

    /// <summary>
    /// 开门命令
    /// </summary>
    [RelayCommand]
    private async Task OpenDoorAsync()
    {
        await ExecuteDoorOperationAsync(TaskType.OpenDoor, "开门");
    }

    /// <summary>
    /// 关门命令
    /// </summary>
    [RelayCommand]
    private async Task CloseDoorAsync()
    {
        await ExecuteDoorOperationAsync(TaskType.CloseDoor, "关门");
    }

    /// <summary>
    /// 刷新状态命令
    /// </summary>
    [RelayCommand]
    private async Task RefreshStatusAsync()
    {
        await UpdateDeviceStatusesAsync();
        await CheckConnectionAsync();
        
        StatusMessage = "状态已刷新";
        AddOperationLog("状态刷新", "设备状态已更新", true);
    }

    /// <summary>
    /// 检查连接命令
    /// </summary>
    [RelayCommand]
    private async Task CheckConnectionAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "正在检查连接...";

            var isConnected = await _apiClientService.CheckConnectionAsync();
            
            IsConnected = isConnected;
            ConnectionStatus = isConnected ? "已连接" : "连接失败";
            StatusMessage = isConnected ? "连接正常" : "API服务连接失败";

            if (!isConnected)
            {
                _notificationService.ShowWarning("API服务连接失败", "请检查服务是否正在运行");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查连接失败");
            IsConnected = false;
            ConnectionStatus = "连接错误";
            StatusMessage = $"连接检查失败: {ex.Message}";
            _notificationService.ShowError("连接检查失败", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 导航到门控制视图
    /// </summary>
    [RelayCommand]
    private void NavigateToDoorControl()
    {
        CurrentView = App.GetService<DoorControlViewModel>();
    }

    /// <summary>
    /// 导航到设备状态视图
    /// </summary>
    [RelayCommand]
    private void NavigateToDeviceStatus()
    {
        CurrentView = App.GetService<DeviceStatusViewModel>();
    }

    /// <summary>
    /// 导航到设置视图
    /// </summary>
    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentView = App.GetService<SettingsViewModel>();
    }

    /// <summary>
    /// 执行门控制操作
    /// </summary>
    private async Task ExecuteDoorOperationAsync(TaskType taskType, string operationName)
    {
        try
        {
            IsLoading = true;
            StatusMessage = $"正在执行{operationName}操作...";

            var deviceType = SelectedDeviceType switch
            {
                "拉门" => DeviceType.PullDoor,
                "卷帘门" => DeviceType.ShutterDoor,
                "默认卷帘门" => DeviceType.DefaultShutter,
                _ => DeviceType.DefaultShutter
            };

            var taskReceive = new TaskReceive
            {
                TaskType = taskType,
                DeviceType = deviceType,
                TaskId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                TimeoutSeconds = 90
            };

            var result = await _apiClientService.ExecuteTaskAsync(taskReceive);

            var isSuccess = result.Status == 200;
            StatusMessage = isSuccess ? $"{operationName}操作成功" : $"{operationName}操作失败";

            AddOperationLog(operationName, result.Message, isSuccess);

            if (isSuccess)
            {
                _notificationService.ShowSuccess($"{operationName}成功", result.Message);
            }
            else
            {
                _notificationService.ShowError($"{operationName}失败", result.Message);
            }

            // 操作完成后刷新设备状态
            await UpdateDeviceStatusesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{OperationName}操作失败", operationName);
            StatusMessage = $"{operationName}操作异常";
            AddOperationLog(operationName, $"操作异常: {ex.Message}", false);
            _notificationService.ShowError($"{operationName}异常", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 更新设备状态
    /// </summary>
    private async Task UpdateDeviceStatusesAsync()
    {
        try
        {
            var deviceTypes = new[] { DeviceType.PullDoor, DeviceType.ShutterDoor, DeviceType.DefaultShutter };
            var statusTasks = deviceTypes.Select(async deviceType =>
            {
                try
                {
                    var status = await _apiClientService.GetDeviceStatusAsync(deviceType);
                    return new DeviceStatusInfo
                    {
                        DeviceType = GetDeviceTypeName(deviceType),
                        IsConnected = status?.IsConnected ?? false,
                        StatusText = status?.IsConnected == true ? "在线" : "离线",
                        StatusData = status?.Status,
                        LastUpdateTime = DateTime.Now
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "获取设备 {DeviceType} 状态失败", deviceType);
                    return new DeviceStatusInfo
                    {
                        DeviceType = GetDeviceTypeName(deviceType),
                        IsConnected = false,
                        StatusText = "获取失败",
                        LastUpdateTime = DateTime.Now
                    };
                }
            });

            var statuses = await Task.WhenAll(statusTasks);

            Application.Current.Dispatcher.Invoke(() =>
            {
                DeviceStatuses.Clear();
                foreach (var status in statuses)
                {
                    DeviceStatuses.Add(status);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新设备状态失败");
        }
    }

    /// <summary>
    /// 添加操作日志
    /// </summary>
    private void AddOperationLog(string operation, string message, bool isSuccess)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var log = new OperationLog
            {
                Timestamp = DateTime.Now,
                Operation = operation,
                Message = message,
                IsSuccess = isSuccess,
                DeviceType = SelectedDeviceType
            };

            OperationLogs.Insert(0, log);

            // 保持最多100条日志
            while (OperationLogs.Count > 100)
            {
                OperationLogs.RemoveAt(OperationLogs.Count - 1);
            }
        });
    }

    /// <summary>
    /// 获取设备类型名称
    /// </summary>
    private static string GetDeviceTypeName(DeviceType deviceType)
    {
        return deviceType switch
        {
            DeviceType.PullDoor => "拉门",
            DeviceType.ShutterDoor => "卷帘门",
            DeviceType.DefaultShutter => "默认卷帘门",
            _ => "未知设备"
        };
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _statusUpdateTimer?.Stop();
        _statusUpdateTimer?.Dispose();
    }
}

/// <summary>
/// 设备状态信息
/// </summary>
public class DeviceStatusInfo
{
    public string DeviceType { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string? StatusData { get; set; }
    public DateTime LastUpdateTime { get; set; }
}

/// <summary>
/// 操作日志
/// </summary>
public class OperationLog
{
    public DateTime Timestamp { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public string StatusIcon => IsSuccess ? "✅" : "❌";
}

/// <summary>
/// API客户端配置选项
/// </summary>
public class ApiClientOptions
{
    public const string SectionName = "ApiClient";
    
    public string BaseUrl { get; set; } = "http://localhost:8080";
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}