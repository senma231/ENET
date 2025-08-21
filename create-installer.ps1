# ENET 安装包制作脚本
# 需要先安装 WiX Toolset: https://wixtoolset.org/

param(
    [string]$Version = "1.0.0"
)

Write-Host "开始制作 ENET 安装包..." -ForegroundColor Green

# 检查WiX工具是否安装
$wixPath = Get-Command "candle.exe" -ErrorAction SilentlyContinue
if (-not $wixPath) {
    Write-Host "错误: 未找到 WiX Toolset，请先安装 WiX Toolset" -ForegroundColor Red
    Write-Host "下载地址: https://wixtoolset.org/" -ForegroundColor Yellow
    exit 1
}

$publishDir = ".\publish"
$installerDir = "$publishDir\Installer"
$msiDir = "$publishDir\MSI"

# 检查安装版文件是否存在
if (-not (Test-Path $installerDir)) {
    Write-Host "错误: 未找到安装版文件，请先运行 build.ps1" -ForegroundColor Red
    exit 1
}

# 创建MSI目录
New-Item -Path $msiDir -ItemType Directory -Force | Out-Null

# 创建WiX配置文件
$wixConfig = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
  <Product Id="*" Name="ENET FRP客户端" Language="1033" Version="$Version" Manufacturer="ENET" UpgradeCode="12345678-1234-1234-1234-123456789012">
    <Package InstallerVersion="200" Compressed="yes" InstallScope="perMachine" />
    
    <MajorUpgrade DowngradeErrorMessage="A newer version of [ProductName] is already installed." />
    <MediaTemplate EmbedCab="yes" />
    
    <Feature Id="ProductFeature" Title="ENET FRP客户端" Level="1">
      <ComponentGroupRef Id="ProductComponents" />
    </Feature>
    
    <Directory Id="TARGETDIR" Name="SourceDir">
      <Directory Id="ProgramFilesFolder">
        <Directory Id="INSTALLFOLDER" Name="ENET" />
      </Directory>
      <Directory Id="ProgramMenuFolder">
        <Directory Id="ApplicationProgramsFolder" Name="ENET" />
      </Directory>
    </Directory>
    
    <ComponentGroup Id="ProductComponents" Directory="INSTALLFOLDER">
      <Component Id="MainExecutable" Guid="*">
        <File Id="ENET.exe" Source="$installerDir\ENET.exe" KeyPath="yes" />
      </Component>
      <!-- 添加其他文件 -->
    </ComponentGroup>
    
    <!-- 开始菜单快捷方式 -->
    <DirectoryRef Id="ApplicationProgramsFolder">
      <Component Id="ApplicationShortcut" Guid="*">
        <Shortcut Id="ApplicationStartMenuShortcut" Name="ENET FRP客户端" Description="ENET FRP客户端" Target="[#ENET.exe]" WorkingDirectory="INSTALLFOLDER" />
        <RemoveFolder Id="ApplicationProgramsFolder" On="uninstall" />
        <RegistryValue Root="HKCU" Key="Software\Microsoft\ENET" Name="installed" Type="integer" Value="1" KeyPath="yes" />
      </Component>
    </DirectoryRef>
  </Product>
</Wix>
"@

$wixFile = "$msiDir\ENET.wxs"
Set-Content -Path $wixFile -Value $wixConfig -Encoding UTF8

# 编译WiX文件
Write-Host "正在编译安装包..." -ForegroundColor Cyan
$wixObj = "$msiDir\ENET.wixobj"
$msiFile = "$msiDir\ENET-$Version.msi"

& candle.exe -out $wixObj $wixFile
if ($LASTEXITCODE -ne 0) {
    Write-Host "WiX编译失败" -ForegroundColor Red
    exit 1
}

& light.exe -out $msiFile $wixObj
if ($LASTEXITCODE -ne 0) {
    Write-Host "MSI生成失败" -ForegroundColor Red
    exit 1
}

Write-Host "安装包制作完成！" -ForegroundColor Green
Write-Host "MSI文件位于: $((Get-Item $msiFile).FullName)" -ForegroundColor Green

# 清理临时文件
Remove-Item -Path $wixObj -Force -ErrorAction SilentlyContinue
Remove-Item -Path $wixFile -Force -ErrorAction SilentlyContinue
