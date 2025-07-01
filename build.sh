#!/bin/bash

# 门控制系统构建脚本
echo "开始构建门控制系统..."

# 设置错误时退出
set -e

# 定义颜色
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 日志函数
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# 检查dotnet是否安装
if ! command -v dotnet &> /dev/null; then
    log_error "dotnet CLI 未安装，请先安装 .NET 8.0 SDK"
    exit 1
fi

# 检查.NET版本
DOTNET_VERSION=$(dotnet --version)
log_info "当前 .NET 版本: $DOTNET_VERSION"

# 清理之前的构建
log_info "清理之前的构建..."
dotnet clean ControlShutter.sln

# 恢复NuGet包
log_info "恢复 NuGet 包..."
dotnet restore ControlShutter.sln

# 构建解决方案
log_info "构建解决方案..."
dotnet build ControlShutter.sln --configuration Release --no-restore

# 运行测试（如果存在）
if [ -d "tests" ]; then
    log_info "运行测试..."
    dotnet test ControlShutter.sln --configuration Release --no-build --verbosity normal
else
    log_warning "未找到测试项目，跳过测试"
fi

# 发布API项目
log_info "发布 API 项目..."
dotnet publish ControlShutter.Api/ControlShutter.Api.csproj \
    --configuration Release \
    --output ./publish/api \
    --no-build

# 发布核心服务项目
log_info "发布核心服务项目..."
dotnet publish ControlShutter.Core/ControlShutter.Core.csproj \
    --configuration Release \
    --output ./publish/core \
    --no-build

# 发布桌面应用（如果存在）
if [ -f "ControlShutter.Desktop/ControlShutter.Desktop.csproj" ]; then
    log_info "发布桌面应用..."
    dotnet publish ControlShutter.Desktop/ControlShutter.Desktop.csproj \
        --configuration Release \
        --output ./publish/desktop \
        --no-build
fi

# 创建Docker镜像（如果Dockerfile存在）
if [ -f "ControlShutter.Api/Dockerfile" ]; then
    log_info "构建 Docker 镜像..."
    docker build -t control-shutter-api:latest ControlShutter.Api/
fi

# 创建部署包
log_info "创建部署包..."
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
PACKAGE_NAME="ControlShutter_${TIMESTAMP}.tar.gz"

tar -czf "$PACKAGE_NAME" \
    publish/ \
    ControlShutter.Api/appsettings.json \
    ControlShutter.Api/appsettings.Production.json \
    README.md \
    OPTIMIZATION_SUMMARY.md

log_info "构建完成！"
log_info "发布文件位置: ./publish/"
log_info "部署包: $PACKAGE_NAME"

# 显示构建摘要
echo ""
echo "==================== 构建摘要 ===================="
echo "✅ 共享库: ControlShutter.Shared"
echo "✅ 基础设施: ControlShutter.Infrastructure"
echo "✅ API 服务: ControlShutter.Api"
echo "✅ 核心服务: ControlShutter.Core"
if [ -f "ControlShutter.Desktop/ControlShutter.Desktop.csproj" ]; then
    echo "✅ 桌面应用: ControlShutter.Desktop"
fi
echo "=================================================="

# 运行健康检查（如果API正在运行）
if command -v curl &> /dev/null; then
    log_info "可以使用以下命令启动API服务："
    echo "cd publish/api && dotnet ControlShutter.Api.dll"
    echo ""
    echo "然后访问 http://localhost:5000/health 进行健康检查"
else
    log_warning "curl 未安装，无法进行健康检查"
fi

log_info "构建脚本执行完成！"