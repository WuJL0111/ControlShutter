using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using System.Text.Json;

namespace ControlShutter.Core.Extensions;

/// <summary>
/// Web应用程序扩展方法
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// 使用安全头部中间件
    /// </summary>
    /// <param name="app">Web应用程序</param>
    /// <returns>Web应用程序</returns>
    public static WebApplication UseSecurityHeaders(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            // 添加安全相关的HTTP头部
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");
            
            await next.Invoke();
        });

        return app;
    }

    /// <summary>
    /// 使用全局异常处理中间件
    /// </summary>
    /// <param name="app">Web应用程序</param>
    /// <returns>Web应用程序</returns>
    public static WebApplication UseGlobalExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";

                if (exceptionHandlerFeature?.Error != null)
                {
                    var exception = exceptionHandlerFeature.Error;
                    
                    logger.LogError(exception, "未处理的异常: {Message}", exception.Message);

                    var response = new
                    {
                        error = new
                        {
                            message = app.Environment.IsDevelopment() 
                                ? exception.Message 
                                : "服务器内部错误",
                            type = exception.GetType().Name,
                            traceId = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier
                        }
                    };

                    var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    await context.Response.WriteAsync(jsonResponse);
                }
            });
        });

        return app;
    }
}