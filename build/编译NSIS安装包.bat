@echo off
chcp 65001 > nul
echo 正在编译ENET客户端安装包...

REM 检查NSIS是否安装
set NSIS_FOUND=0

REM 检查常见的NSIS安装路径
if exist "%PROGRAMFILES(X86)%\NSIS\makensis.exe" (
    set NSIS_PATH="%PROGRAMFILES(X86)%\NSIS\makensis.exe"
    set NSIS_FOUND=1
) else if exist "%PROGRAMFILES%\NSIS\makensis.exe" (
    set NSIS_PATH="%PROGRAMFILES%\NSIS\makensis.exe"
    set NSIS_FOUND=1
) else if exist "C:\Program Files (x86)\NSIS\makensis.exe" (
    set NSIS_PATH="C:\Program Files (x86)\NSIS\makensis.exe"
    set NSIS_FOUND=1
) else if exist "C:\Program Files\NSIS\makensis.exe" (
    set NSIS_PATH="C:\Program Files\NSIS\makensis.exe"
    set NSIS_FOUND=1
)

REM 如果在常见路径找不到，尝试从PATH环境变量中查找
setlocal EnableDelayedExpansion
if %NSIS_FOUND% EQU 0 (
    for %%X in (makensis.exe) do (
        set MAKENSIS_COMMAND=%%~$PATH:X
        if not "!MAKENSIS_COMMAND!" == "" (
            set NSIS_PATH="!MAKENSIS_COMMAND!"
            set NSIS_FOUND=1
        )
    )
)

REM 如果仍然找不到，报错并退出
if %NSIS_FOUND% EQU 0 (
    echo 错误：未找到NSIS安装。请先安装NSIS软件。
    echo 可从 https://nsis.sourceforge.io/Download 下载NSIS。
    echo.
    echo 安装后，请确保NSIS的安装目录已添加到系统PATH环境变量中。
    pause
    exit /b 1
)

REM 检查发布文件是否存在
if not exist "publish\portable\FrpcGui.exe" (
    echo 错误：未找到已发布的应用程序文件。
    echo 请先运行以下命令生成应用程序文件：
    echo dotnet publish -c Release -r win-x64 --self-contained -o "publish/portable"
    pause
    exit /b 1
)

REM 编译NSIS脚本
echo 使用NSIS路径: %NSIS_PATH%
echo 正在编译NSIS脚本...

%NSIS_PATH% "ENET_Installer.nsi"

if %ERRORLEVEL% neq 0 (
    echo 编译NSIS脚本时出错，错误代码: %ERRORLEVEL%
    echo 请检查NSIS脚本是否有语法错误。
    pause
    exit /b 1
)

echo.
echo 安装包编译完成！
echo 生成的安装文件：ENET_Client_Setup.exe
echo.
pause