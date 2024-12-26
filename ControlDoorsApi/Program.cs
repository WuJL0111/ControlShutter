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
        policy.WithOrigins(cors)//�������������ͷ
        .WithExposedHeaders("x-custom-header")//���ù�������Ӧͷ
        .AllowAnyHeader()//������������ͷ
        .AllowAnyMethod()//�����κη���
        .AllowCredentials()//�����Դƾ��----��������������ƾ��
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
