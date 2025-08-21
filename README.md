# ENET - FRP 图形客户端

[![Build](https://github.com/senma231/ENET/actions/workflows/build.yml/badge.svg)](https://github.com/senma231/ENET/actions/workflows/build.yml)
[![Release](https://img.shields.io/github/v/release/senma231/ENET)](https://github.com/senma231/ENET/releases)
[![License](https://img.shields.io/github/license/senma231/ENET)](LICENSE)

基于 .NET 8.0 开发的 FRP (Fast Reverse Proxy) 图形客户端，提供简洁易用的界面来管理和配置 FRP 代理连接。

## ✨ 功能特性

### 🎯 核心功能
- **多配置管理**：支持创建、保存、删除和切换多个配置文件
- **可视化配置**：直观的图形界面，无需手动编辑配置文件
- **代理管理**：支持 TCP、UDP、HTTP、HTTPS、STCP 等多种代理类型
- **实时监控**：实时显示连接状态和日志信息

### 🔧 系统集成
- **系统托盘**：最小化到系统托盘，后台运行
- **开机自启**：支持开机自动启动功能
- **系统服务**：可注册为 Windows 系统服务，无需登录即可运行
- **静默模式**：支持命令行参数静默启动和自动连接

### 🛠️ 技术特性
- **单文件部署**：便携版无需安装，包含完整 .NET 运行时
- **现代化界面**：基于 WPF 的现代化用户界面
- **MVVM 架构**：清晰的代码结构，易于维护和扩展

## 📦 下载安装

### 便携版（推荐）
从 [Releases](https://github.com/senma231/ENET/releases) 页面下载最新的便携版：
- 下载 `ENET-Portable-vX.X.X.zip`
- 解压到任意目录
- 直接运行 `ENET.exe`

### 安装版
- 下载 `ENET-Setup-vX.X.X.msi`
- 双击安装包进行安装
- 从开始菜单启动程序

## 🚀 快速开始

1. **启动程序**：运行 `ENET.exe`
2. **配置服务器**：填写 FRP 服务器地址、端口和认证令牌
3. **添加代理**：点击"添加代理"按钮，配置需要代理的服务
4. **启动连接**：点击"启动"按钮开始连接

## 📋 系统要求

- **操作系统**：Windows 10 / Windows 11 / Windows Server 2019+
- **架构**：x64 (64位)
- **.NET 运行时**：便携版已包含，无需额外安装

## 🔧 高级功能

### 系统服务模式
1. 下载并放置 `nssm.exe` 到程序目录
2. 以管理员身份运行程序
3. 勾选"注册为系统服务"
4. 服务将在系统启动时自动运行

### 命令行参数
```bash
ENET.exe --silent --autoconnect  # 静默启动并自动连接
```

## 🏗️ 开发构建

### 环境要求
- .NET 8.0 SDK
- Visual Studio 2022 或 VS Code

### 构建步骤
```bash
# 克隆仓库
git clone https://github.com/senma231/ENET.git
cd ENET

# 构建便携版和安装版
.\build.ps1

# 制作 MSI 安装包（需要 WiX Toolset）
.\create-installer.ps1
```

## 📄 许可证

本项目采用 [MIT 许可证](LICENSE)。

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📞 支持

如果您在使用过程中遇到问题，请：
1. 查看 [Issues](https://github.com/senma231/ENET/issues) 中是否有相似问题
2. 提交新的 Issue 描述您的问题
3. 提供详细的错误信息和操作步骤

---

⭐ 如果这个项目对您有帮助，请给个 Star 支持一下！
