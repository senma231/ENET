# ENET 项目同步到 GitHub 脚本
Write-Host "开始同步 ENET 项目到 GitHub..." -ForegroundColor Green

# 检查是否在正确的目录
if (-not (Test-Path "ENET.csproj")) {
    Write-Host "错误: 请在 ENET 项目目录下运行此脚本" -ForegroundColor Red
    Write-Host "当前目录: $(Get-Location)" -ForegroundColor Yellow
    Write-Host "请切换到包含 ENET.csproj 的目录" -ForegroundColor Yellow
    exit 1
}

Write-Host "当前项目目录: $(Get-Location)" -ForegroundColor Cyan

# 检查Git状态
$gitStatus = git status --porcelain 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "错误: 当前目录不是Git仓库" -ForegroundColor Red
    exit 1
}

# 检查远程仓库
$remoteUrl = git remote get-url origin 2>$null
if (-not $remoteUrl) {
    Write-Host "错误: 未配置远程仓库" -ForegroundColor Red
    exit 1
}

Write-Host "远程仓库: $remoteUrl" -ForegroundColor Cyan

# 清理本地构建文件
Write-Host "清理本地构建文件..." -ForegroundColor Cyan
if (Test-Path "bin") { Remove-Item -Path "bin" -Recurse -Force }
if (Test-Path "obj") { Remove-Item -Path "obj" -Recurse -Force }
if (Test-Path "publish") { Remove-Item -Path "publish" -Recurse -Force }

# 显示当前状态
Write-Host "检查Git状态..." -ForegroundColor Cyan
git status

# 添加所有文件到 Git
Write-Host "添加文件到 Git..." -ForegroundColor Cyan
git add .

# 检查是否有更改
$status = git status --porcelain
if (-not $status) {
    Write-Host "没有检测到更改，无需提交" -ForegroundColor Yellow
    exit 0
}

# 显示将要提交的更改
Write-Host "将要提交的更改:" -ForegroundColor Cyan
git status --short

# 提交更改
$commitMessage = "feat: 重构项目结构并完善功能

主要更新:
- 修复系统服务注册问题，使用NSSM包装frpc.exe
- 完善打包脚本，支持便携版和安装版自动构建
- 添加GitHub Actions自动构建和发布流程
- 优化项目结构，移除冗余文件
- 更新README和项目文档
- 改进错误处理和用户体验

技术改进:
- 使用.NET 8.0和现代化WPF架构
- 支持单文件发布和自包含部署
- 集成NSSM工具实现可靠的系统服务功能"

Write-Host "提交更改..." -ForegroundColor Cyan
git commit -m $commitMessage

# 推送到远程仓库
Write-Host "推送到 GitHub..." -ForegroundColor Yellow
Write-Host "注意: 这将推送所有更改到远程仓库" -ForegroundColor Yellow
$confirm = Read-Host "确定要继续吗？(y/N)"

if ($confirm -eq "y" -or $confirm -eq "Y") {
    git push origin master
    if ($LASTEXITCODE -eq 0) {
        Write-Host "项目已成功同步到 GitHub!" -ForegroundColor Green
        Write-Host "仓库地址: https://github.com/senma231/ENET" -ForegroundColor Green
        Write-Host "GitHub Actions将自动开始构建..." -ForegroundColor Cyan
    } else {
        Write-Host "推送失败，请检查网络连接和权限" -ForegroundColor Red
    }
} else {
    Write-Host "操作已取消" -ForegroundColor Yellow
}

Write-Host "同步完成!" -ForegroundColor Green
