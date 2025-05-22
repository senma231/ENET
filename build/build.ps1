# ENET 构建脚本

# 设置变量
$projectDir = Split-Path -Parent $PSScriptRoot
$outputDir = Join-Path $projectDir "build\output"
$portableDir = Join-Path $outputDir "ENET_Portable"
$version = "1.0.0"

# 创建输出目录
if (!(Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

# 清理旧文件
if (Test-Path $portableDir) {
    Remove-Item -Path $portableDir -Recurse -Force
}

# 编译项目
Write-Host "正在编译项目..." -ForegroundColor Cyan
dotnet publish "$projectDir\ENET\ENET.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$portableDir"

# 复制必要文件
Write-Host "正在复制必要文件..." -ForegroundColor Cyan
Copy-Item -Path "$projectDir\LICENSE" -Destination "$portableDir\LICENSE" -Force
Copy-Item -Path "$projectDir\README.md" -Destination "$portableDir\README.md" -Force

# 创建便携版压缩包
Write-Host "正在创建便携版压缩包..." -ForegroundColor Cyan
$portableZip = Join-Path $outputDir "ENET_Portable_v$version.zip"
if (Test-Path $portableZip) {
    Remove-Item -Path $portableZip -Force
}
Compress-Archive -Path "$portableDir\*" -DestinationPath $portableZip

Write-Host "构建完成！" -ForegroundColor Green
Write-Host "便携版: $portableZip" -ForegroundColor Yellow