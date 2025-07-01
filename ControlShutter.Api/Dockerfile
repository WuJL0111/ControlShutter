# 多阶段构建的 Dockerfile for ControlShutter.Api

# ============================================
# 阶段1: 构建阶段
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# 设置工作目录
WORKDIR /src

# 复制解决方案文件
COPY ControlShutter.sln ./

# 复制所有项目文件
COPY ControlShutter.Shared/ControlShutter.Shared.csproj ./ControlShutter.Shared/
COPY ControlShutter.Infrastructure/ControlShutter.Infrastructure.csproj ./ControlShutter.Infrastructure/
COPY ControlShutter.Api/ControlShutter.Api.csproj ./ControlShutter.Api/

# 恢复 NuGet 包
RUN dotnet restore ControlShutter.Api/ControlShutter.Api.csproj

# 复制所有源代码
COPY . .

# 构建项目
RUN dotnet build ControlShutter.Api/ControlShutter.Api.csproj -c Release -o /app/build

# ============================================
# 阶段2: 发布阶段
# ============================================
FROM build AS publish

# 发布应用程序
RUN dotnet publish ControlShutter.Api/ControlShutter.Api.csproj -c Release -o /app/publish

# ============================================
# 阶段3: 运行时阶段
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# 设置工作目录
WORKDIR /app

# 安装必要的工具（可选）
RUN apt-get update && \
    apt-get install -y curl && \
    rm -rf /var/lib/apt/lists/*

# 创建非root用户以提高安全性
RUN adduser --disabled-password --gecos '' appuser && \
    chown -R appuser /app
USER appuser

# 复制发布的文件
COPY --from=publish --chown=appuser:appuser /app/publish .

# 设置环境变量
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV TZ=Asia/Shanghai

# 暴露端口
EXPOSE 8080

# 健康检查
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# 设置入口点
ENTRYPOINT ["dotnet", "ControlShutter.Api.dll"]

# 添加标签信息
LABEL maintainer="Door Control System Team" \
      version="1.0.0" \
      description="门控制系统 API 服务" \
      build-date="2024-12-19"