using ControlShutter.Infrastructure.Services;
using ControlShutter.Infrastructure.Modbus;
using ControlShutter.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ControlShutter.Api.Extensions;

/// <summary>
/// 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加基础设施服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // 注册基础设施服务
        services.AddSingleton<IConnectionPoolManager, ConnectionPoolManager>();
        services.AddScoped<IModbusService, ModbusService>();
        services.AddScoped<IDoorControlService, DoorControlService>();
        services.AddScoped<IExternalNotificationService, ExternalNotificationService>();
        
        // 添加HTTP客户端
        services.AddHttpClient();

        return services;
    }
}