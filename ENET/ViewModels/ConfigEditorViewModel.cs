using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using ENET.Models;

namespace ENET.ViewModels
{
    public class ConfigEditorViewModel : ViewModelBase
    {
        private FrpcConfig _config;
        private ProxyConfig? _selectedProxy;

        public ConfigEditorViewModel(FrpcConfig config)
        {
            _config = config;

            // 初始化命令
            AddProxyCommand = new RelayCommand(AddProxy);
            RemoveProxyCommand = new RelayCommand(RemoveProxy, () => SelectedProxy != null);
        }

        /// <summary>
        /// 配置
        /// </summary>
        public FrpcConfig Config
        {
            get => _config;
            set => SetProperty(ref _config, value);
        }

        /// <summary>
        /// 选中的代理配置
        /// </summary>
        public ProxyConfig? SelectedProxy
        {
            get => _selectedProxy;
            set
            {
                SetProperty(ref _selectedProxy, value);
                ((RelayCommand)RemoveProxyCommand).NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// 添加代理命令
        /// </summary>
        public ICommand AddProxyCommand { get; }

        /// <summary>
        /// 删除代理命令
        /// </summary>
        public ICommand RemoveProxyCommand { get; }

        /// <summary>
        /// 添加代理
        /// </summary>
        private void AddProxy()
        {
            Config.Proxies.Add(new ProxyConfig
            {
                Name = $"proxy{Config.Proxies.Count + 1}"
            });
            OnPropertyChanged(nameof(Config));
        }

        /// <summary>
        /// 删除代理
        /// </summary>
        private void RemoveProxy()
        {
            if (SelectedProxy != null)
            {
                Config.Proxies.Remove(SelectedProxy);
                SelectedProxy = null!;
                OnPropertyChanged(nameof(Config));
            }
        }
    }
}
