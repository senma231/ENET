using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ENET.Models
{
    /// <summary>
    /// 表示frpc的配置
    /// </summary>
    public class FrpcConfig
    {
        /// <summary>
        /// 配置名称
        /// </summary>
        public string Name { get; set; } = "默认配置";

        /// <summary>
        /// frpc可执行文件路径
        /// </summary>
        public string FrpcPath { get; set; } = "";

        /// <summary>
        /// 配置文件路径
        /// </summary>
        public string ConfigFilePath { get; set; } = "";

        /// <summary>
        /// 服务器地址
        /// </summary>
        public string ServerAddress { get; set; } = "127.0.0.1";

        /// <summary>
        /// 服务器端口
        /// </summary>
        public int ServerPort { get; set; } = 7000;

        /// <summary>
        /// 认证令牌
        /// </summary>
        public string? Token { get; set; } = "";

        /// <summary>
        /// 是否启用TLS
        /// </summary>
        public bool EnableTls { get; set; } = false;

        /// <summary>
        /// 代理配置列表
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<ProxyConfig> ProxiesCollection { get; set; } = new ObservableCollection<ProxyConfig>();

        /// <summary>
        /// 代理配置列表 (用于JSON序列化)
        /// </summary>
        public List<ProxyConfig> Proxies
        {
            get => new List<ProxyConfig>(ProxiesCollection);
            set
            {
                ProxiesCollection.Clear();
                if (value != null)
                {
                    foreach (var proxy in value)
                    {
                        ProxiesCollection.Add(proxy);
                    }
                }
            }
        }

        /// <summary>
        /// 保存配置到文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public void SaveToFile(string filePath)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string jsonString = JsonSerializer.Serialize(this, options);
            File.WriteAllText(filePath, jsonString);
        }

        /// <summary>
        /// 从文件加载配置
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>加载的配置</returns>
        public static FrpcConfig LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return new FrpcConfig();

            string jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<FrpcConfig>(jsonString) ?? new FrpcConfig();
        }

        /// <summary>
        /// 生成frpc配置文件内容
        /// </summary>
        /// <returns>配置文件内容</returns>
        public string GenerateConfigFileContent()
        {
            var content = new System.Text.StringBuilder();

            // 通用配置
            content.AppendLine("[common]");
            content.AppendLine($"server_addr = {ServerAddress}");
            content.AppendLine($"server_port = {ServerPort}");

            if (!string.IsNullOrEmpty(Token))
            {
                content.AppendLine($"token = {Token}");
            }

            if (EnableTls)
            {
                content.AppendLine("tls_enable = true");
            }

            content.AppendLine();

            // 代理配置
            foreach (var proxy in ProxiesCollection)
            {
                content.AppendLine($"[{proxy.Name}]");
                content.AppendLine($"type = {proxy.Type}");
                content.AppendLine($"local_ip = {proxy.LocalIp}");
                content.AppendLine($"local_port = {proxy.LocalPort}");

                if (proxy.Type == "tcp" || proxy.Type == "udp")
                {
                    content.AppendLine($"remote_port = {proxy.RemotePort}");
                }
                else if (proxy.Type == "http" || proxy.Type == "https")
                {
                    content.AppendLine($"custom_domains = {proxy.CustomDomains}");
                }

                content.AppendLine();
            }

            return content.ToString();
        }
    }

    /// <summary>
    /// 代理配置
    /// </summary>
    public class ProxyConfig
    {
        /// <summary>
        /// 代理名称
        /// </summary>
        public string Name { get; set; } = "proxy";

        /// <summary>
        /// 代理类型 (tcp, udp, http, https, stcp)
        /// </summary>
        public string Type { get; set; } = "tcp";

        /// <summary>
        /// 本地IP
        /// </summary>
        public string LocalIp { get; set; } = "127.0.0.1";

        /// <summary>
        /// 本地端口
        /// </summary>
        public int LocalPort { get; set; } = 80;

        /// <summary>
        /// 远程端口 (用于tcp/udp)
        /// </summary>
        public int RemotePort { get; set; } = 0;

        /// <summary>
        /// 自定义域名 (用于http/https)
        /// </summary>
        public string CustomDomains { get; set; } = "";
    }
}
