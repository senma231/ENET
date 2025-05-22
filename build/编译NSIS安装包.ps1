# ENET客户端NSIS安装包编译脚本

Write-Host "正在编译ENET客户端安装包..." -ForegroundColor Cyan

# 检查NSIS是否安装
$nsisPath = $null
$nsisFound = $false

# 检查常见的NSIS安装路径
$possiblePaths = @(
    "${env:ProgramFiles(x86)}\NSIS\makensis.exe",
    "$env:ProgramFiles\NSIS\makensis.exe",
    "C:\Program Files (x86)\NSIS\makensis.exe",
    "C:\Program Files\NSIS\makensis.exe"
)

foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        $nsisPath = $path
        $nsisFound = $true
        break
    }
}

# 如果在常见路径找不到，尝试从PATH环境变量中查找
if (-not $nsisFound) {
    try {
        $nsisPath = (Get-Command "makensis.exe" -ErrorAction Stop).Source
        $nsisFound = $true
    }
    catch {
        # 找不到makensis.exe
    }
}

# 如果仍然找不到，报错并退出
if (-not $nsisFound) {
    Write-Host "错误：未找到NSIS安装。请先安装NSIS软件。" -ForegroundColor Red
    Write-Host "可从 https://nsis.sourceforge.io/Download 下载NSIS。" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "安装后，请确保NSIS的安装目录已添加到系统PATH环境变量中。" -ForegroundColor Yellow
    Read-Host "按Enter键退出"
    exit 1
}

# 检查发布文件是否存在
if (-not (Test-Path "publish\portable\FrpcGui.exe")) {
    Write-Host "错误：未找到已发布的应用程序文件。" -ForegroundColor Red
    Write-Host "请先运行以下命令生成应用程序文件：" -ForegroundColor Yellow
    Write-Host "dotnet publish -c Release -r win-x64 --self-contained -o ""publish/portable""" -ForegroundColor Yellow
    Read-Host "按Enter键退出"
    exit 1
}

# 编译NSIS脚本
Write-Host "使用NSIS路径: $nsisPath" -ForegroundColor Green
Write-Host "正在编译NSIS脚本..." -ForegroundColor Cyan

try {
    & $nsisPath "ENET_Installer.nsi"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "编译NSIS脚本时出错，错误代码: $LASTEXITCODE" -ForegroundColor Red
        Write-Host "请检查NSIS脚本是否有语法错误。" -ForegroundColor Yellow
        Read-Host "按Enter键退出"
        exit 1
    }
}
catch {
    Write-Host "编译NSIS脚本时出现异常: $_" -ForegroundColor Red
    Read-Host "按Enter键退出"
    exit 1
}

Write-Host ""
Write-Host "安装包编译完成！" -ForegroundColor Green
Write-Host "生成的安装文件：ENET_Client_Setup.exe" -ForegroundColor Yellow
Write-Host ""
Read-Host "按Enter键退出"