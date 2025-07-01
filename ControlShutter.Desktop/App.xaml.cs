using ControlShutter.Desktop.Services;
using ControlShutter.Desktop.ViewModels;
using ControlShutter.Desktop.Views;
using ControlShutter.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Windows;

namespace ControlShutter.Desktop;

/// <summary>
/// App.xaml 的交互逻辑
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    /// <summary>
    /// 获取当前应用程序的服务提供者
    /// </summary>
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    /// <summary>
    /// 应用程序启动时执行
    /// </summary>
    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            // 创建应用程序主机
            _host = CreateHostBuilder(e.Args).Build();
            ServiceProvider = _host.Services;

            // 启动主机
            await _host.StartAsync();

            // 显示主窗口
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"应用程序启动失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            Current.Shutdown(1);
        }
    }

    /// <summary>
    /// 应用程序退出时执行
    /// </summary>
    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
        }
        catch (Exception ex)
        {
            // 记录日志，但不显示错误消息
            System.Diagnostics.Debug.WriteLine($"应用程序退出时发生错误：{ex.Message}");
        }
        finally
        {
            base.OnExit(e);
        }
    }

    /// <summary>
    /// 创建应用程序主机
    /// </summary>
    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                // 添加配置文件
                config.SetBasePath(Directory.GetCurrentDirectory())
                      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables()
                      .AddCommandLine(args);
            })
            .ConfigureLogging((context, logging) =>
            {
                // 配置日志
                logging.ClearProviders()
                       .AddConsole()
                       .AddDebug();

                // 根据配置设置日志级别
                if (context.HostingEnvironment.IsDevelopment())
                {
                    logging.SetMinimumLevel(LogLevel.Debug);
                }
                else
                {
                    logging.SetMinimumLevel(LogLevel.Information);
                }
            })
            .ConfigureServices((context, services) =>
            {
                // 注册配置选项
                services.Configure<ApiClientOptions>(
                    context.Configuration.GetSection(ApiClientOptions.SectionName));

                // 注册HTTP客户端
                services.AddHttpClient<IApiClientService, ApiClientService>((serviceProvider, client) =>
                {
                    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiClientOptions>>().Value;
                    client.BaseAddress = new Uri(options.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                });

                // 注册服务
                services.AddSingleton<IApiClientService, ApiClientService>();
                services.AddSingleton<INotificationService, NotificationService>();
                services.AddSingleton<IDialogService, DialogService>();

                // 注册视图模型
                services.AddTransient<MainViewModel>();
                services.AddTransient<DoorControlViewModel>();
                services.AddTransient<DeviceStatusViewModel>();
                services.AddTransient<SettingsViewModel>();

                // 注册视图
                services.AddTransient<MainWindow>();
                services.AddTransient<DoorControlView>();
                services.AddTransient<DeviceStatusView>();
                services.AddTransient<SettingsView>();

                // 注册其他服务
                services.AddSingleton<ViewModelLocator>();
            })
            .UseConsoleLifetime(); // 使用控制台生命周期
    }

    /// <summary>
    /// 获取服务实例
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例</returns>
    public static T GetService<T>() where T : class
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// 尝试获取服务实例
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例或null</returns>
    public static T? GetServiceOrDefault<T>() where T : class
    {
        return ServiceProvider.GetService<T>();
    }

    /// <summary>
    /// 全局异常处理
    /// </summary>
    private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            var logger = GetServiceOrDefault<ILogger<App>>();
            logger?.LogError(e.Exception, "未处理的异常：{Message}", e.Exception.Message);

            MessageBox.Show($"应用程序发生未处理的异常：\n{e.Exception.Message}\n\n详细信息已记录到日志中。", 
                           "错误", MessageBoxButton.OK, MessageBoxImage.Error);

            e.Handled = true;
        }
        catch
        {
            // 如果连错误处理都失败了，就让应用程序正常崩溃
            e.Handled = false;
        }
    }
}