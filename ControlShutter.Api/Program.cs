using System.Text.Json.Serialization;
using ControlShutter.Api.Extensions;
using ControlShutter.Api.HealthChecks;
using ControlShutter.Shared.Configuration;
using FluentValidation;
using FluentValidation.AspNetCore;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// 配置NLog
builder.Logging.ClearProviders();
builder.Host.UseNLog();

// 添加服务
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// 配置验证
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// 配置选项
builder.Services.Configure<DeviceConnectionOptions>(
    builder.Configuration.GetSection(DeviceConnectionOptions.SectionName));
builder.Services.Configure<ExternalNotificationOptions>(
    builder.Configuration.GetSection(ExternalNotificationOptions.SectionName));
builder.Services.Configure<LoggingOptions>(
    builder.Configuration.GetSection(LoggingOptions.SectionName));

// 添加基础设施服务
builder.Services.AddInfrastructureServices();

// 配置Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "门控制系统 API",
        Version = "v1",
        Description = "门控制系统的RESTful API接口"
    });
    
    // 包含XML文档
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// 配置CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
                           ?? new[] { "http://localhost:3000", "http://192.168.20.110:12581" };
        
        policy.WithOrigins(allowedOrigins)
              .WithExposedHeaders("x-custom-header")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true);
    });
});

// 添加健康检查
builder.Services.AddHealthChecks()
    .AddCheck<DeviceHealthCheck>("device-health");

// 添加问题详情
builder.Services.AddProblemDetails();

var app = builder.Build();

// 配置管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "门控制系统 API v1");
        options.RoutePrefix = string.Empty; // 设置Swagger UI为根路径
    });
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// 启用问题详情
app.UseStatusCodePages();

// 安全头部
app.UseSecurityHeaders();

app.UseHttpsRedirection();
app.UseCors("DefaultPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// 全局异常处理
app.UseGlobalExceptionHandler();

try
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("门控制系统 API 启动成功，监听端口: {Urls}", string.Join(", ", app.Urls));
    
    await app.RunAsync();
}
catch (Exception ex)
{
    var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
    logger.Error(ex, "应用程序启动失败");
    throw;
}
finally
{
    NLog.LogManager.Shutdown();
}