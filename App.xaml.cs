using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace ENET
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// 是否以静默模式启动
        /// </summary>
        public static bool SilentMode { get; private set; } = false;

        /// <summary>
        /// 是否自动连接服务器
        /// </summary>
        public static bool AutoConnect { get; private set; } = false;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 检查启动参数
            if (e.Args.Length > 0)
            {
                foreach (var arg in e.Args)
                {
                    if (arg.Equals("-silent", StringComparison.OrdinalIgnoreCase) ||
                        arg.Equals("/silent", StringComparison.OrdinalIgnoreCase) ||
                        arg.Equals("--silent", StringComparison.OrdinalIgnoreCase))
                    {
                        SilentMode = true;
                    }
                    else if (arg.Equals("-autoconnect", StringComparison.OrdinalIgnoreCase) ||
                             arg.Equals("/autoconnect", StringComparison.OrdinalIgnoreCase) ||
                             arg.Equals("--autoconnect", StringComparison.OrdinalIgnoreCase))
                    {
                        AutoConnect = true;
                    }
                }
            }

            // 设置全局异常处理
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            try
            {
                // 创建应用程序数据目录
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ENET");

                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }

                // 创建日志目录
                string logPath = Path.Combine(appDataPath, "Logs");

                if (!Directory.Exists(logPath))
                {
                    Directory.CreateDirectory(logPath);
                }

                // 记录应用程序启动日志
                string logFile = Path.Combine(logPath, $"log_{DateTime.Now:yyyyMMdd}.txt");
                File.AppendAllText(
                    logFile,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 应用程序启动 {(SilentMode ? "(静默模式)" : "")}\r\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化应用程序时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                // 记录异常
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ENET");
                string logPath = Path.Combine(appDataPath, "Logs");
                string logFile = Path.Combine(logPath, $"error_{DateTime.Now:yyyyMMdd}.txt");

                File.AppendAllText(logFile,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] UI线程异常: {e.Exception}\r\n");

                MessageBox.Show($"应用程序发生错误: {e.Exception.Message}\r\n\r\n详细信息已记录到日志文件: {logFile}",
                    "错误", MessageBoxButton.OK, MessageBoxImage.Error);

                // 标记异常为已处理
                e.Handled = true;
            }
            catch
            {
                MessageBox.Show("应用程序发生严重错误，无法记录详细信息。", "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Exception? ex = e.ExceptionObject as Exception;

                // 记录异常
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ENET");
                string logPath = Path.Combine(appDataPath, "Logs");
                string logFile = Path.Combine(logPath, $"error_{DateTime.Now:yyyyMMdd}.txt");

                File.AppendAllText(logFile,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 未处理异常: {ex}\r\n");

                MessageBox.Show($"应用程序发生严重错误: {ex?.Message}\r\n\r\n详细信息已记录到日志文件: {logFile}",
                    "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
                MessageBox.Show("应用程序发生严重错误，无法记录详细信息。", "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
