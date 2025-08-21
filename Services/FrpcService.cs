using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ENET.Models;

namespace ENET.Services
{
    /// <summary>
    /// frpc服务类，管理配置和进程
    /// </summary>
    public class FrpcService
    {
        private readonly string _appDataPath;
        private readonly string _configsPath;
        private readonly string _tempPath;
        private FrpcProcess? _currentProcess;
        private FrpcConfig? _currentConfig;

        /// <summary>
        /// 日志事件
        /// </summary>
        public event EventHandler<string>? LogReceived;

        /// <summary>
        /// 状态变更事件
        /// </summary>
        public event EventHandler<bool>? StatusChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        public FrpcService()
        {
            // 初始化资源管理器
            ResourceManager.Initialize();

            // 创建应用数据目录
            _appDataPath = ResourceManager.GetAppDataPath();
            _configsPath = Path.Combine(_appDataPath, "Configs");
            _tempPath = Path.Combine(_appDataPath, "Temp");

            Directory.CreateDirectory(_configsPath);
            Directory.CreateDirectory(_tempPath);
        }

        /// <summary>
        /// 获取所有配置文件
        /// </summary>
        /// <returns>配置文件列表</returns>
        public List<string> GetConfigFiles()
        {
            var files = Directory.GetFiles(_configsPath, "*.json");
            var result = new List<string>();

            foreach (var file in files)
            {
                result.Add(Path.GetFileNameWithoutExtension(file));
            }

            return result;
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        /// <param name="configName">配置名称</param>
        /// <returns>加载的配置</returns>
        public FrpcConfig LoadConfig(string configName)
        {
            var filePath = Path.Combine(_configsPath, $"{configName}.json");
            _currentConfig = FrpcConfig.LoadFromFile(filePath);

            // 如果配置中没有设置frpc路径，或者路径不存在，则使用嵌入的frpc
            if (string.IsNullOrEmpty(_currentConfig.FrpcPath) || !File.Exists(_currentConfig.FrpcPath))
            {
                _currentConfig.FrpcPath = ResourceManager.GetFrpcExecutablePath();
            }

            return _currentConfig;
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        /// <param name="config">要保存的配置</param>
        public void SaveConfig(FrpcConfig config)
        {
            // 如果配置中没有设置frpc路径，则使用嵌入的frpc
            if (string.IsNullOrEmpty(config.FrpcPath))
            {
                config.FrpcPath = ResourceManager.GetFrpcExecutablePath();
            }

            var filePath = Path.Combine(_configsPath, $"{config.Name}.json");
            config.SaveToFile(filePath);
            _currentConfig = config;
        }

        /// <summary>
        /// 删除配置
        /// </summary>
        /// <param name="configName">配置名称</param>
        public void DeleteConfig(string configName)
        {
            var filePath = Path.Combine(_configsPath, $"{configName}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        /// <summary>
        /// 启动frpc
        /// </summary>
        /// <param name="config">要使用的配置</param>
        /// <returns>是否成功启动</returns>
        public bool StartFrpc(FrpcConfig config)
        {
            if (_currentProcess != null && _currentProcess.IsRunning)
            {
                LogReceived?.Invoke(this, "已有frpc进程正在运行，请先停止");
                return false;
            }

            try
            {
                // 确保使用嵌入的frpc可执行文件
                if (string.IsNullOrEmpty(config.FrpcPath) || !File.Exists(config.FrpcPath))
                {
                    config.FrpcPath = ResourceManager.GetFrpcExecutablePath();
                    LogReceived?.Invoke(this, $"使用内置frpc: {config.FrpcPath}");
                }

                // 生成临时配置文件
                var tempConfigPath = Path.Combine(_tempPath, $"{config.Name}_{DateTime.Now:yyyyMMddHHmmss}.ini");
                File.WriteAllText(tempConfigPath, config.GenerateConfigFileContent());

                // 创建并启动进程
                _currentProcess = new FrpcProcess();
                _currentProcess.OutputReceived += (sender, message) => LogReceived?.Invoke(this, message);
                _currentProcess.ErrorReceived += (sender, message) => LogReceived?.Invoke(this, $"错误: {message}");
                _currentProcess.ProcessExited += (sender, exitCode) =>
                {
                    LogReceived?.Invoke(this, $"frpc进程已退出，退出代码: {exitCode}");
                    StatusChanged?.Invoke(this, false);
                };

                var result = _currentProcess.Start(config.FrpcPath, tempConfigPath);
                if (result)
                {
                    _currentConfig = config;
                    LogReceived?.Invoke(this, "frpc进程已启动");
                    StatusChanged?.Invoke(this, true);
                }
                else
                {
                    LogReceived?.Invoke(this, "启动frpc进程失败");
                }

                return result;
            }
            catch (Exception ex)
            {
                LogReceived?.Invoke(this, $"启动frpc时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 停止frpc
        /// </summary>
        public void StopFrpc()
        {
            if (_currentProcess == null || !_currentProcess.IsRunning)
            {
                LogReceived?.Invoke(this, "没有正在运行的frpc进程");
                return;
            }

            _currentProcess.Stop();
            LogReceived?.Invoke(this, "已发送停止命令到frpc进程");
        }

        /// <summary>
        /// 获取frpc状态
        /// </summary>
        /// <returns>是否正在运行</returns>
        public bool GetFrpcStatus()
        {
            return _currentProcess != null && _currentProcess.IsRunning;
        }

        /// <summary>
        /// 获取当前配置
        /// </summary>
        /// <returns>当前配置</returns>
        public FrpcConfig? GetCurrentConfig()
        {
            return _currentConfig;
        }
    }
}
