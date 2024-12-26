using Microsoft.Extensions.Configuration;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);
IConfiguration configuration;

// Add services to the container.
builder.Logging.AddNLog(@"Config\Nlog.config");

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(cor =>
{
    var cors = new[]
    {
        "http://192.168.20.110:12581"
    };
    cor.AddPolicy("ControlDoor", policy =>
    {
        policy.WithOrigins(cors)//设置允许的请求头
        .WithExposedHeaders("x-custom-header")//设置公开的响应头
        .AllowAnyHeader()//允许所有请求头
        .AllowAnyMethod()//允许任何方法
        .AllowCredentials()//允许跨源凭据----服务器必须允许凭据
        .SetIsOriginAllowed(_ => true);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
