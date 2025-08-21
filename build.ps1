# ENET FRP客户端打包脚本
Write-Host "开始打包 ENET FRP客户端..." -ForegroundColor Green

# 检查是否存在Resources目录下的frpc.exe
$frpcPath = ".\Resources\frpc.exe"
if (-not (Test-Path $frpcPath)) {
    Write-Host "警告: $frpcPath 不存在，将不会被嵌入到应用程序中。" -ForegroundColor Yellow
    Write-Host "请确保在发布前将frpc.exe放置在正确的位置。" -ForegroundColor Yellow
}

# 清理之前的发布文件
$publishDir = ".\publish"
$portableDir = "$publishDir\Portable"
$installerDir = "$publishDir\Installer"

if (Test-Path $publishDir) {
    Write-Host "清理之前的发布文件..." -ForegroundColor Cyan
    Remove-Item -Path $publishDir -Recurse -Force
}

# 创建发布目录
New-Item -Path $publishDir -ItemType Directory -Force | Out-Null
New-Item -Path $portableDir -ItemType Directory -Force | Out-Null
New-Item -Path $installerDir -ItemType Directory -Force | Out-Null

# 发布便携版（单文件）
Write-Host "正在发布便携版..." -ForegroundColor Cyan
dotnet publish -c Release -o $portableDir --self-contained true -r win-x64 /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true

# 检查发布是否成功
if ($LASTEXITCODE -eq 0) {
    Write-Host "便携版发布成功！" -ForegroundColor Green
} else {
    Write-Host "便携版发布失败，请检查错误信息。" -ForegroundColor Red
    exit 1
}

# 发布安装版（文件夹形式）
Write-Host "正在发布安装版..." -ForegroundColor Cyan
dotnet publish -c Release -o $installerDir --self-contained true -r win-x64 /p:PublishSingleFile=false

# 检查发布是否成功
if ($LASTEXITCODE -eq 0) {
    Write-Host "安装版发布成功！" -ForegroundColor Green
} else {
    Write-Host "安装版发布失败，请检查错误信息。" -ForegroundColor Red
    exit 1
}

# 下载NSSM工具到便携版
Write-Host "正在下载NSSM工具..." -ForegroundColor Cyan
$nssmUrl = "https://nssm.cc/release/nssm-2.24.zip"
$nssmZip = "$env:TEMP\nssm.zip"
$nssmExtract = "$env:TEMP\nssm"

try {
    Invoke-WebRequest -Uri $nssmUrl -OutFile $nssmZip -UseBasicParsing
    Expand-Archive -Path $nssmZip -DestinationPath $nssmExtract -Force

    # 复制64位版本的nssm.exe到便携版目录
    $nssmExe = Get-ChildItem -Path $nssmExtract -Name "nssm.exe" -Recurse | Where-Object { $_.FullName -like "*win64*" } | Select-Object -First 1
    if ($nssmExe) {
        Copy-Item -Path $nssmExe.FullName -Destination "$portableDir\nssm.exe"
        Write-Host "NSSM工具已添加到便携版" -ForegroundColor Green
    }

    # 清理临时文件
    Remove-Item -Path $nssmZip -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $nssmExtract -Recurse -Force -ErrorAction SilentlyContinue
} catch {
    Write-Host "下载NSSM失败: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "系统服务功能可能无法正常使用" -ForegroundColor Yellow
}

# 创建版本信息文件
$versionInfo = @"
ENET FRP客户端
版本: 1.0.0
发布日期: $(Get-Date -Format "yyyy-MM-dd")

便携版说明:
- 单文件可执行程序，包含完整的.NET运行时
- 无需安装，直接运行ENET.exe即可
- 包含NSSM工具，支持系统服务功能

安装版说明:
- 多文件形式，适合制作安装包
- 包含完整的.NET运行时和依赖库
- 可使用第三方工具制作MSI安装包
"@

Set-Content -Path "$publishDir\README.txt" -Value $versionInfo -Encoding UTF8
Copy-Item -Path "$publishDir\README.txt" -Destination "$portableDir\README.txt"
Copy-Item -Path "$publishDir\README.txt" -Destination "$installerDir\README.txt"

Write-Host "打包完成！" -ForegroundColor Green
Write-Host "便携版位于: $((Get-Item $portableDir).FullName)" -ForegroundColor Green
Write-Host "安装版位于: $((Get-Item $installerDir).FullName)" -ForegroundColor Green