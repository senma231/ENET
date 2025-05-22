@echo off
cd /d "%~dp0"
"D:\Program Files\Inno Setup 6\ISCC.exe" ENET_Simple.iss
if %ERRORLEVEL% neq 0 (
    echo 编译失败，请检查错误信息。
) else (
    echo 编译成功，生成的安装文件：ENET_Client_Setup.exe
)
pause