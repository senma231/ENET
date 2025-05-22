using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrpcGui.Models;
using FrpcGui.Services;
using Microsoft.Win32;

namespace FrpcGui.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// 通知属性变更
        /// </summary>
        public new void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);
        }
        private readonly FrpcService _frpcService;
        private FrpcConfig _selectedConfig;
        private string _logContent = "";
        private bool _isRunning = false;
        private string _statusText = "已停止";
        private string _selectedConfigName;

        public MainViewModel()
        {
            _frpcService = new FrpcService();
            _frpcService.LogReceived += (sender, log) =>
            {
                LogContent += log + Environment.NewLine;
            };
            _frpcService.StatusChanged += (sender, status) =>
            {
                IsRunning = status;
                StatusText = status ? "运行中" : "已停止";
            };

            // 初始化命令
            StartCommand = new RelayCommand(StartFrpc, () => !IsRunning && SelectedConfig != null);
            StopCommand = new RelayCommand(StopFrpc, () => IsRunning);
            NewConfigCommand = new RelayCommand(CreateNewConfig);
            SaveConfigCommand = new RelayCommand(SaveConfig, () => SelectedConfig != null);
            DeleteConfigCommand = new RelayCommand(DeleteConfig, () => SelectedConfig != null);
            BrowseFrpcCommand = new RelayCommand(BrowseFrpcExecutable);
            AddProxyCommand = new RelayCommand(AddProxy, () => SelectedConfig != null);
            RemoveProxyCommand = new RelayCommand<ProxyConfig>(RemoveProxy, (proxy) => SelectedConfig != null && proxy != null);
            ClearLogCommand = new RelayCommand(ClearLog);
            MinimizeToTrayCommand = new RelayCommand(MinimizeToTray);
            ExitCommand = new RelayCommand(Exit);
            ShowWindowCommand = new RelayCommand(ShowWindow);

            // 加载配置列表
            LoadConfigList();

            // 如果有配置，选择第一个
            if (ConfigNames.Count > 0)
            {
                SelectedConfigName = ConfigNames[0];
                SelectedConfig = _frpcService.LoadConfig(ConfigNames[0]);
            }
            else
            {
                // 创建默认配置
                SelectedConfig = new FrpcConfig();
            }
        }

        #region 属性

        /// <summary>
        /// 配置名称列表
        /// </summary>
        public ObservableCollection<string> ConfigNames { get; } = new ObservableCollection<string>();

        /// <summary>
        /// 当前选中的配置名称
        /// </summary>
        public string SelectedConfigName
        {
            get => _selectedConfigName;
            set => SetProperty(ref _selectedConfigName, value);
        }

        /// <summary>
        /// 当前选中的配置
        /// </summary>
        public FrpcConfig SelectedConfig
        {
            get => _selectedConfig;
            set
            {
                SetProperty(ref _selectedConfig, value);
                OnPropertyChanged(nameof(CanEditProxy));
            }
        }

        /// <summary>
        /// 日志内容
        /// </summary>
        public string LogContent
        {
            get => _logContent;
            set => SetProperty(ref _logContent, value);
        }

        /// <summary>
        /// frpc是否正在运行
        /// </summary>
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                SetProperty(ref _isRunning, value);
                OnPropertyChanged(nameof(CanEditProxy));
                ((RelayCommand)StartCommand).NotifyCanExecuteChanged();
                ((RelayCommand)StopCommand).NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// 状态文本
        /// </summary>
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        /// <summary>
        /// 是否可以编辑代理配置
        /// </summary>
        public bool CanEditProxy => SelectedConfig != null && !IsRunning;

        /// <summary>
        /// 是否开机自动启动
        /// </summary>
        public bool AutoStartWithWindows
        {
            get => IsAutoStartEnabled();
            set
            {
                if (value)
                {
                    // 如果启用自启动，检查是否已注册为系统服务
                    if (IsRegisteredAsService)
                    {
                        var result = MessageBox.Show(
                            "已将frpc注册为系统服务，不建议同时启用开机自启动，这可能导致两个frpc进程同时运行。\n\n是否取消系统服务并启用开机自启动？",
                            "功能冲突",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Yes)
                        {
                            // 取消系统服务
                            UnregisterService();
                            // 启用自启动
                            EnableAutoStart();
                            OnPropertyChanged(nameof(IsRegisteredAsService));
                        }
                        else
                        {
                            // 用户取消，恢复UI状态
                            OnPropertyChanged(nameof(AutoStartWithWindows));
                            return;
                        }
                    }
                    else
                    {
                        // 正常启用自启动
                        EnableAutoStart();
                    }
                }
                else
                {
                    DisableAutoStart();
                }
                OnPropertyChanged(nameof(AutoStartWithWindows));
            }
        }

        /// <summary>
        /// 是否已注册为系统服务
        /// </summary>
        public bool IsRegisteredAsService
        {
            get => IsServiceRegistered();
            set
            {
                if (value)
                {
                    // 如果注册系统服务，检查是否已启用开机自启动
                    if (AutoStartWithWindows)
                    {
                        var result = MessageBox.Show(
                            "已启用开机自启动，不建议同时注册为系统服务，这可能导致两个frpc进程同时运行。\n\n是否取消开机自启动并注册为系统服务？",
                            "功能冲突",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Yes)
                        {
                            // 取消自启动
                            DisableAutoStart();
                            // 注册系统服务
                            RegisterService();
                            OnPropertyChanged(nameof(AutoStartWithWindows));
                        }
                        else
                        {
                            // 用户取消，恢复UI状态
                            OnPropertyChanged(nameof(IsRegisteredAsService));
                            return;
                        }
                    }
                    else
                    {
                        // 正常注册系统服务
                        RegisterService();
                    }
                }
                else
                {
                    UnregisterService();
                }
                OnPropertyChanged(nameof(IsRegisteredAsService));
            }
        }

        #endregion

        #region 命令

        /// <summary>
        /// 启动frpc命令
        /// </summary>
        public ICommand StartCommand { get; }

        /// <summary>
        /// 停止frpc命令
        /// </summary>
        public ICommand StopCommand { get; }

        /// <summary>
        /// 新建配置命令
        /// </summary>
        public ICommand NewConfigCommand { get; }

        /// <summary>
        /// 保存配置命令
        /// </summary>
        public ICommand SaveConfigCommand { get; }

        /// <summary>
        /// 删除配置命令
        /// </summary>
        public ICommand DeleteConfigCommand { get; }

        /// <summary>
        /// 浏览frpc可执行文件命令
        /// </summary>
        public ICommand BrowseFrpcCommand { get; }

        /// <summary>
        /// 添加代理配置命令
        /// </summary>
        public ICommand AddProxyCommand { get; }

        /// <summary>
        /// 删除代理配置命令
        /// </summary>
        public ICommand RemoveProxyCommand { get; }

        /// <summary>
        /// 清除日志命令
        /// </summary>
        public ICommand ClearLogCommand { get; }

        /// <summary>
        /// 最小化到托盘命令
        /// </summary>
        public ICommand MinimizeToTrayCommand { get; }

        /// <summary>
        /// 退出命令
        /// </summary>
        public ICommand ExitCommand { get; }

        /// <summary>
        /// 显示窗口命令
        /// </summary>
        public ICommand ShowWindowCommand { get; }

        #endregion

        #region 方法

        /// <summary>
        /// 加载配置列表
        /// </summary>
        private void LoadConfigList()
        {
            ConfigNames.Clear();
            foreach (var name in _frpcService.GetConfigFiles())
            {
                ConfigNames.Add(name);
            }
        }

        /// <summary>
        /// 启动frpc
        /// </summary>
        private void StartFrpc()
        {
            if (string.IsNullOrEmpty(SelectedConfig.FrpcPath))
            {
                MessageBox.Show("请先选择frpc可执行文件路径", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!File.Exists(SelectedConfig.FrpcPath))
            {
                MessageBox.Show("frpc可执行文件不存在，请重新选择", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 先保存配置
            SaveConfig();

            // 启动frpc
            _frpcService.StartFrpc(SelectedConfig);
        }

        /// <summary>
        /// 停止frpc
        /// </summary>
        public void StopFrpc()
        {
            _frpcService.StopFrpc();
        }

        /// <summary>
        /// 创建新配置
        /// </summary>
        private void CreateNewConfig()
        {
            SelectedConfig = new FrpcConfig();
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        private void SaveConfig()
        {
            _frpcService.SaveConfig(SelectedConfig);
            LoadConfigList();

            // 确保配置名称在列表中
            if (!ConfigNames.Contains(SelectedConfig.Name))
            {
                ConfigNames.Add(SelectedConfig.Name);
            }
        }

        /// <summary>
        /// 删除配置
        /// </summary>
        private void DeleteConfig()
        {
            if (MessageBox.Show($"确定要删除配置 \"{SelectedConfig.Name}\" 吗？", "确认删除",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _frpcService.DeleteConfig(SelectedConfig.Name);
                ConfigNames.Remove(SelectedConfig.Name);

                if (ConfigNames.Count > 0)
                {
                    SelectedConfig = _frpcService.LoadConfig(ConfigNames[0]);
                }
                else
                {
                    SelectedConfig = new FrpcConfig();
                }
            }
        }

        /// <summary>
        /// 浏览frpc可执行文件
        /// </summary>
        private void BrowseFrpcExecutable()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*",
                Title = "选择frpc可执行文件"
            };

            if (dialog.ShowDialog() == true)
            {
                SelectedConfig.FrpcPath = dialog.FileName;
                OnPropertyChanged(nameof(SelectedConfig));
            }
        }

        /// <summary>
        /// 添加代理配置
        /// </summary>
        private void AddProxy()
        {
            if (SelectedConfig == null)
            {
                MessageBox.Show("请先选择或创建一个配置", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 添加新的代理配置
            var newProxy = new ProxyConfig
            {
                Name = $"proxy{SelectedConfig.ProxiesCollection.Count + 1}",
                Type = "tcp",
                LocalIp = "127.0.0.1",
                LocalPort = 80,
                RemotePort = 8080
            };

            SelectedConfig.ProxiesCollection.Add(newProxy);

            // 输出调试信息
            Console.WriteLine($"添加了新代理: {newProxy.Name}, 当前代理数量: {SelectedConfig.ProxiesCollection.Count}");
        }

        /// <summary>
        /// 删除代理配置
        /// </summary>
        private void RemoveProxy(ProxyConfig proxy)
        {
            Console.WriteLine($"尝试删除代理: {proxy?.Name}");

            if (proxy != null && SelectedConfig != null)
            {
                // 直接从ObservableCollection中删除
                if (SelectedConfig.ProxiesCollection.Remove(proxy))
                {
                    Console.WriteLine($"已删除代理: {proxy.Name}, 当前代理数量: {SelectedConfig.ProxiesCollection.Count}");
                }
                else
                {
                    Console.WriteLine($"代理不存在于列表中: {proxy.Name}");
                }
            }
            else
            {
                Console.WriteLine("代理为空或配置为空，无法删除");
            }
        }

        /// <summary>
        /// 清除日志
        /// </summary>
        private void ClearLog()
        {
            LogContent = "";
        }

        /// <summary>
        /// 最小化到托盘
        /// </summary>
        private void MinimizeToTray()
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
            Application.Current.MainWindow.Hide();
        }

        /// <summary>
        /// 退出应用
        /// </summary>
        private void Exit()
        {
            if (IsRunning)
            {
                if (MessageBox.Show("frpc正在运行，确定要退出吗？", "确认退出",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                {
                    return;
                }

                _frpcService.StopFrpc();
            }

            Application.Current.Shutdown();
        }

        /// <summary>
        /// 显示窗口
        /// </summary>
        private void ShowWindow()
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Show();
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();
            }
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        /// <param name="configName">配置名称</param>
        public void LoadConfig(string configName)
        {
            if (string.IsNullOrEmpty(configName))
                return;

            SelectedConfig = _frpcService.LoadConfig(configName);
        }

        #region 开机自启动和系统服务

        /// <summary>
        /// 检查是否已设置开机自动启动
        /// </summary>
        private bool IsAutoStartEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    return key?.GetValue("FrpcGui") != null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查开机自启动设置时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 启用开机自动启动
        /// </summary>
        private void EnableAutoStart()
        {
            try
            {
                // 获取应用程序路径，使用更可靠的方法
                string exePath = GetApplicationPath();

                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    // 添加静默模式和自动连接参数，使自启动时最小化到托盘并自动连接服务器
                    key?.SetValue("FrpcGui", $"\"{exePath}\" --silent --autoconnect");
                }

                Console.WriteLine($"已启用开机自动启动，路径: {exePath}");
                MessageBox.Show("已设置开机自动启动", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置开机自启动时出错: {ex.Message}");
                MessageBox.Show($"设置开机自启动时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 获取应用程序路径
        /// </summary>
        /// <returns>应用程序路径</returns>
        private string GetApplicationPath()
        {
            try
            {
                // 首先尝试使用Environment.ProcessPath（.NET 5+）
                string? processPath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(processPath))
                {
                    Console.WriteLine($"使用Environment.ProcessPath获取路径: {processPath}");
                    return processPath;
                }

                // 回退到使用Assembly.Location
                string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(assemblyLocation))
                {
                    Console.WriteLine($"使用Assembly.Location获取路径: {assemblyLocation}");
                    return assemblyLocation;
                }

                // 尝试使用AppContext.BaseDirectory
                string baseDirectory = AppContext.BaseDirectory;
                string exePath = Path.Combine(baseDirectory, "FrpcGui.exe");
                if (File.Exists(exePath))
                {
                    Console.WriteLine($"使用AppContext.BaseDirectory获取路径: {exePath}");
                    return exePath;
                }

                // 尝试使用应用程序域的BaseDirectory
                string domainPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FrpcGui.exe");
                if (File.Exists(domainPath))
                {
                    Console.WriteLine($"使用AppDomain.BaseDirectory获取路径: {domainPath}");
                    return domainPath;
                }

                // 最后回退到使用进程的MainModule.FileName
                string? moduleFileName = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(moduleFileName))
                {
                    Console.WriteLine($"使用Process.MainModule.FileName获取路径: {moduleFileName}");
                    return moduleFileName;
                }

                // 如果所有方法都失败，使用当前执行的应用程序名称
                string currentExe = System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe";
                string currentExePath = Path.Combine(AppContext.BaseDirectory, currentExe);
                Console.WriteLine($"使用当前进程名称获取路径: {currentExePath}");
                return currentExePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取应用程序路径时出错: {ex.Message}");
                MessageBox.Show($"获取应用程序路径时出错: {ex.Message}\n\n请手动设置开机自启动。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return "FrpcGui.exe"; // 返回一个基本的可执行文件名
            }
        }

        /// <summary>
        /// 禁用开机自动启动
        /// </summary>
        private void DisableAutoStart()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    key?.DeleteValue("FrpcGui", false);
                }

                Console.WriteLine("已禁用开机自动启动");
                MessageBox.Show("已取消开机自动启动", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"取消开机自启动时出错: {ex.Message}");
                MessageBox.Show($"取消开机自启动时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 检查是否已注册为系统服务
        /// </summary>
        private bool IsServiceRegistered()
        {
            try
            {
                // 使用sc query命令检查服务是否存在
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "sc",
                        Arguments = "query FrpcService",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return !output.Contains("指定的服务未安装") && !output.Contains("The specified service does not exist");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查系统服务时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 注册为系统服务
        /// </summary>
        private void RegisterService()
        {
            try
            {
                if (SelectedConfig == null || string.IsNullOrEmpty(SelectedConfig.FrpcPath))
                {
                    MessageBox.Show("请先选择frpc可执行文件路径", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    OnPropertyChanged(nameof(IsRegisteredAsService));
                    return;
                }

                if (!File.Exists(SelectedConfig.FrpcPath))
                {
                    MessageBox.Show("frpc可执行文件不存在，请重新选择", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    OnPropertyChanged(nameof(IsRegisteredAsService));
                    return;
                }

                // 先保存配置
                SaveConfig();

                // 生成配置文件
                string configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "FrpcGui", "Configs", $"{SelectedConfig.Name}.ini");

                File.WriteAllText(configPath, SelectedConfig.GenerateConfigFileContent());

                // 使用sc create命令创建服务
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "sc",
                        Arguments = $"create FrpcService binPath= \"{SelectedConfig.FrpcPath} -c \"{configPath}\"\" start= auto DisplayName= \"Frpc Service\"",
                        UseShellExecute = true,
                        Verb = "runas", // 以管理员权限运行
                        CreateNoWindow = false
                    }
                };

                process.Start();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    // 启动服务
                    var startProcess = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "sc",
                            Arguments = "start FrpcService",
                            UseShellExecute = true,
                            Verb = "runas", // 以管理员权限运行
                            CreateNoWindow = false
                        }
                    };

                    startProcess.Start();
                    startProcess.WaitForExit();

                    MessageBox.Show("已成功注册并启动系统服务", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("注册系统服务失败，请确保以管理员身份运行", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    OnPropertyChanged(nameof(IsRegisteredAsService));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"注册系统服务时出错: {ex.Message}");
                MessageBox.Show($"注册系统服务时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                OnPropertyChanged(nameof(IsRegisteredAsService));
            }
        }

        /// <summary>
        /// 取消注册系统服务
        /// </summary>
        private void UnregisterService()
        {
            try
            {
                // 先停止服务
                var stopProcess = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "sc",
                        Arguments = "stop FrpcService",
                        UseShellExecute = true,
                        Verb = "runas", // 以管理员权限运行
                        CreateNoWindow = false
                    }
                };

                stopProcess.Start();
                stopProcess.WaitForExit();

                // 删除服务
                var deleteProcess = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "sc",
                        Arguments = "delete FrpcService",
                        UseShellExecute = true,
                        Verb = "runas", // 以管理员权限运行
                        CreateNoWindow = false
                    }
                };

                deleteProcess.Start();
                deleteProcess.WaitForExit();

                if (deleteProcess.ExitCode == 0)
                {
                    MessageBox.Show("已成功删除系统服务", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("删除系统服务失败，请确保以管理员身份运行", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    OnPropertyChanged(nameof(IsRegisteredAsService));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"删除系统服务时出错: {ex.Message}");
                MessageBox.Show($"删除系统服务时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                OnPropertyChanged(nameof(IsRegisteredAsService));
            }
        }

        #endregion

        #endregion
    }
}
