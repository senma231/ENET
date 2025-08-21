using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ENET.Models;
using ENET.ViewModels;

namespace ENET
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel = null!;
        private bool _isExiting = false;

        public MainWindow()
        {
            try
            {
                Console.WriteLine("正在初始化主窗口...");

                // 记录窗口初始化开始
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ENET", "Logs");
                string logFile = Path.Combine(logPath, $"log_{DateTime.Now:yyyyMMdd}.txt");

                Console.WriteLine($"正在写入日志: {logFile}");

                File.AppendAllText(
                    logFile,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 窗口初始化开始\r\n");

                Console.WriteLine("正在初始化组件...");
                InitializeComponent();
                Console.WriteLine("组件初始化完成");

                // 创建视图模型
                Console.WriteLine("正在创建视图模型...");
                _viewModel = new MainViewModel();
                DataContext = _viewModel;

                // 注册视图模型属性变更事件，用于更新密码框
                _viewModel.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(_viewModel.SelectedConfig) && _viewModel.SelectedConfig != null)
                    {
                        TokenPasswordBox.Password = _viewModel.SelectedConfig.Token ?? string.Empty;
                    }
                };

                Console.WriteLine("视图模型创建完成");

                // 注册窗口状态变更事件
                Console.WriteLine("正在注册窗口事件...");
                StateChanged += (s, e) => MainWindow_StateChanged(s!, e);
                Console.WriteLine("窗口事件注册完成");

                // 记录窗口初始化完成
                Console.WriteLine("正在写入完成日志...");
                File.AppendAllText(
                    logFile,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 窗口初始化完成\r\n");
                Console.WriteLine("日志写入完成");

                // 显示窗口，如果是静默模式则最小化到托盘
                Console.WriteLine("正在显示窗口...");
                if (App.SilentMode)
                {
                    // 静默模式下，直接最小化到托盘
                    Console.WriteLine("静默模式启动，最小化到托盘");
                    Show();
                    WindowState = WindowState.Minimized;
                    Hide();

                    // 显示托盘气泡提示
                    TrayIcon.ShowBalloonTip("ENET", "应用程序已启动并最小化到系统托盘", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                }
                else
                {
                    // 正常模式下，显示并激活窗口
                    Show();
                    Activate();
                }
                Console.WriteLine("窗口显示完成");

                // 如果设置了自动连接，则尝试启动frpc
                if (App.AutoConnect)
                {
                    Console.WriteLine("检测到自动连接参数，尝试自动启动frpc...");

                    // 延迟一段时间再启动，确保界面已完全加载
                    System.Threading.Tasks.Task.Delay(1000).ContinueWith(t =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                // 获取配置列表
                                var configNames = _viewModel.ConfigNames;
                                if (configNames.Count > 0)
                                {
                                    // 加载第一个配置
                                    string firstConfig = configNames[0];
                                    Console.WriteLine($"自动加载配置: {firstConfig}");
                                    _viewModel.LoadConfig(firstConfig);

                                    // 启动frpc
                                    Console.WriteLine("自动启动frpc...");
                                    _viewModel.StartCommand.Execute(null);

                                    // 显示通知
                                    TrayIcon.ShowBalloonTip("ENET", $"已自动连接到服务器 ({firstConfig})",
                                        Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                                }
                                else
                                {
                                    Console.WriteLine("没有找到可用的配置，无法自动连接");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"自动连接时出错: {ex.Message}");
                            }
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"窗口初始化时出错: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                MessageBox.Show($"窗口初始化时出错: {ex.Message}\r\n\r\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 窗口状态变更事件处理
        /// </summary>
        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            // 当窗口最小化时，隐藏窗口
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
        }

        /// <summary>
        /// 配置列表选择变更事件
        /// </summary>
        private void ConfigListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is string configName)
            {
                // 加载选中的配置
                _viewModel.LoadConfig(configName);

                // 更新认证令牌密码框
                if (_viewModel.SelectedConfig != null)
                {
                    TokenPasswordBox.Password = _viewModel.SelectedConfig.Token ?? string.Empty;
                }
            }
        }

        /// <summary>
        /// 认证令牌密码框内容变更事件
        /// </summary>
        private void TokenPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && _viewModel.SelectedConfig != null)
            {
                // 将密码框的内容同步到配置对象
                _viewModel.SelectedConfig.Token = TokenPasswordBox.Password;
            }
        }

        /// <summary>
        /// 删除代理按钮点击事件
        /// </summary>
        private void DeleteProxy_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is ENET.Models.ProxyConfig proxy)
            {
                Console.WriteLine($"删除代理按钮点击: {proxy.Name}");

                // 直接调用ViewModel的方法
                if (_viewModel != null && _viewModel.SelectedConfig != null)
                {
                    _viewModel.SelectedConfig.ProxiesCollection.Remove(proxy);
                    Console.WriteLine($"已通过Click事件删除代理: {proxy.Name}");
                }
            }
        }

        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // 如果是用户点击关闭按钮，则隐藏窗口而不是关闭应用程序
            if (!_isExiting)
            {
                e.Cancel = true;
                Hide();

                // 显示托盘气泡提示
                TrayIcon.ShowBalloonTip("ENET", "应用程序已最小化到系统托盘", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                return;
            }

            // 如果frpc正在运行，询问是否确定退出
            if (_viewModel.IsRunning)
            {
                var result = MessageBox.Show("frpc正在运行，确定要退出吗？", "确认退出",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    _isExiting = false;
                    return;
                }

                // 停止frpc
                _viewModel.StopFrpc();
            }
        }

        /// <summary>
        /// 托盘图标左键单击事件
        /// </summary>
        private void TrayIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            // 显示窗口
            ShowWindow();
        }

        /// <summary>
        /// 托盘图标双击事件
        /// </summary>
        private void TrayIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            // 显示窗口
            ShowWindow();
        }

        /// <summary>
        /// 显示窗口菜单项点击事件
        /// </summary>
        private void ShowWindow_Click(object sender, RoutedEventArgs e)
        {
            // 显示窗口
            ShowWindow();
        }

        /// <summary>
        /// 退出菜单项点击事件
        /// </summary>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            // 设置退出标志
            _isExiting = true;

            // 关闭窗口
            Close();
        }

        /// <summary>
        /// 显示窗口
        /// </summary>
        private void ShowWindow()
        {
            // 显示窗口
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }
    }

    /// <summary>
    /// 空值转布尔值转换器
    /// </summary>
    public class NullToBoolConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 运行状态颜色转换器
    /// </summary>
    public class RunningStatusColorConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isRunning)
            {
                return isRunning ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green)
                                 : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
            }
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}