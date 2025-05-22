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
        private void StopFrpc()
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
            SelectedConfig.Proxies.Add(new ProxyConfig
            {
                Name = $"proxy{SelectedConfig.Proxies.Count + 1}"
            });
            OnPropertyChanged(nameof(SelectedConfig));
        }

        /// <summary>
        /// 删除代理配置
        /// </summary>
        private void RemoveProxy(ProxyConfig proxy)
        {
            if (proxy != null)
            {
                SelectedConfig.Proxies.Remove(proxy);
                OnPropertyChanged(nameof(SelectedConfig));
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

        #endregion
    }
}
