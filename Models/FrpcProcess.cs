using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ENET.Models
{
    /// <summary>
    /// 管理frpc进程的类
    /// </summary>
    public class FrpcProcess
    {
        private Process? _process;
        private StringBuilder _outputBuffer = new StringBuilder();
        private StringBuilder _errorBuffer = new StringBuilder();

        /// <summary>
        /// frpc进程是否正在运行
        /// </summary>
        public bool IsRunning => _process != null && !_process.HasExited;

        /// <summary>
        /// 进程输出事件
        /// </summary>
        public event EventHandler<string>? OutputReceived;

        /// <summary>
        /// 进程错误输出事件
        /// </summary>
        public event EventHandler<string>? ErrorReceived;

        /// <summary>
        /// 进程退出事件
        /// </summary>
        public event EventHandler<int>? ProcessExited;

        /// <summary>
        /// 启动frpc进程
        /// </summary>
        /// <param name="frpcPath">frpc可执行文件路径</param>
        /// <param name="configPath">配置文件路径</param>
        /// <returns>是否成功启动</returns>
        public bool Start(string frpcPath, string configPath)
        {
            if (IsRunning)
                return false;

            if (!File.Exists(frpcPath))
                throw new FileNotFoundException("找不到frpc可执行文件", frpcPath);

            if (!File.Exists(configPath))
                throw new FileNotFoundException("找不到配置文件", configPath);

            try
            {
                // 获取frpc可执行文件所在的目录作为工作目录
                string workingDirectory = Path.GetDirectoryName(frpcPath) ?? Directory.GetCurrentDirectory();
                Console.WriteLine($"使用工作目录: {workingDirectory}");

                _process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = frpcPath,
                        Arguments = $"-c \"{configPath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = workingDirectory // 设置工作目录为frpc所在目录
                    },
                    EnableRaisingEvents = true
                };

                _process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        _outputBuffer.AppendLine(e.Data);
                        OutputReceived?.Invoke(this, e.Data);
                    }
                };

                _process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        _errorBuffer.AppendLine(e.Data);
                        ErrorReceived?.Invoke(this, e.Data);
                    }
                };

                _process.Exited += (sender, e) =>
                {
                    ProcessExited?.Invoke(this, _process.ExitCode);
                    _process.Dispose();
                    _process = null;
                };

                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();

                return true;
            }
            catch (Exception ex)
            {
                _errorBuffer.AppendLine($"启动frpc进程时出错: {ex.Message}");
                ErrorReceived?.Invoke(this, $"启动frpc进程时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 停止frpc进程
        /// </summary>
        public void Stop()
        {
            if (!IsRunning)
                return;

            try
            {
                _process?.Kill();
                _process?.WaitForExit(1000);
            }
            catch (Exception ex)
            {
                _errorBuffer.AppendLine($"停止frpc进程时出错: {ex.Message}");
                ErrorReceived?.Invoke(this, $"停止frpc进程时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取输出日志
        /// </summary>
        /// <returns>输出日志</returns>
        public string GetOutput()
        {
            return _outputBuffer.ToString();
        }

        /// <summary>
        /// 获取错误日志
        /// </summary>
        /// <returns>错误日志</returns>
        public string GetError()
        {
            return _errorBuffer.ToString();
        }

        /// <summary>
        /// 清除日志缓冲区
        /// </summary>
        public void ClearBuffers()
        {
            _outputBuffer.Clear();
            _errorBuffer.Clear();
        }
    }
}
