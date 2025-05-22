using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ENET.Services
{
    /// <summary>
    /// 资源管理器，用于处理嵌入资源
    /// </summary>
    public class ResourceManager
    {
        private static readonly string _appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ENET");

        private static readonly string _binariesPath = Path.Combine(_appDataPath, "Binaries");

        /// <summary>
        /// 初始化资源管理器
        /// </summary>
        public static void Initialize()
        {
            Directory.CreateDirectory(_appDataPath);
            Directory.CreateDirectory(_binariesPath);
        }

        /// <summary>
        /// 获取frpc可执行文件路径
        /// </summary>
        /// <returns>frpc可执行文件路径</returns>
        public static string GetFrpcExecutablePath()
        {
            string frpcPath = Path.Combine(_binariesPath, "frpc.exe");

            // 如果文件不存在，尝试从嵌入资源中提取
            if (!File.Exists(frpcPath))
            {
                try
                {
                    ExtractFrpcExecutable(frpcPath);
                }
                catch (Exception ex)
                {
                    // 如果提取失败，记录错误但不抛出异常
                    Console.WriteLine($"警告: 无法提取嵌入的frpc.exe: {ex.Message}");

                    // 尝试在应用程序目录中查找frpc.exe
                    // 使用AppContext.BaseDirectory代替Assembly.Location
                    string appDirFrpcPath = Path.Combine(
                        AppContext.BaseDirectory,
                        "frpc.exe");

                    if (File.Exists(appDirFrpcPath))
                    {
                        // 如果在应用程序目录中找到，复制到Binaries目录
                        try
                        {
                            File.Copy(appDirFrpcPath, frpcPath, true);
                            Console.WriteLine($"已从应用程序目录复制frpc.exe到: {frpcPath}");
                        }
                        catch (Exception copyEx)
                        {
                            Console.WriteLine($"复制frpc.exe时出错: {copyEx.Message}");
                            return appDirFrpcPath; // 返回应用程序目录中的frpc.exe路径
                        }
                    }
                }
            }

            return frpcPath;
        }

        /// <summary>
        /// 从嵌入资源中提取frpc可执行文件
        /// </summary>
        /// <param name="outputPath">输出路径</param>
        private static void ExtractFrpcExecutable(string outputPath)
        {
            // 获取嵌入资源
            var assembly = Assembly.GetExecutingAssembly();

            // 列出所有嵌入资源，用于调试
            var resourceNames = assembly.GetManifestResourceNames();
            foreach (var name in resourceNames)
            {
                Console.WriteLine($"找到嵌入资源: {name}");
            }

            using var stream = assembly.GetManifestResourceStream("ENET.Resources.frpc.exe");

            if (stream == null)
            {
                throw new Exception("找不到嵌入的frpc.exe资源");
            }

            // 将资源写入文件
            using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            stream.CopyTo(fileStream);

            Console.WriteLine($"已成功提取frpc.exe到: {outputPath}");
        }

        /// <summary>
        /// 检查frpc可执行文件是否存在
        /// </summary>
        /// <returns>是否存在</returns>
        public static bool FrpcExecutableExists()
        {
            return File.Exists(Path.Combine(_binariesPath, "frpc.exe"));
        }

        /// <summary>
        /// 获取应用程序数据目录
        /// </summary>
        /// <returns>应用程序数据目录</returns>
        public static string GetAppDataPath()
        {
            return _appDataPath;
        }

        /// <summary>
        /// 获取二进制文件目录
        /// </summary>
        /// <returns>二进制文件目录</returns>
        public static string GetBinariesPath()
        {
            return _binariesPath;
        }
    }
}
