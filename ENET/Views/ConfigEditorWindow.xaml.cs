using System.Windows;
using ENET.Models;
using ENET.ViewModels;

namespace ENET.Views
{
    /// <summary>
    /// ConfigEditorWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigEditorWindow : Window
    {
        private readonly ConfigEditorViewModel _viewModel;
        private bool _result = false;

        public ConfigEditorWindow(FrpcConfig config)
        {
            InitializeComponent();

            // 创建视图模型
            _viewModel = new ConfigEditorViewModel(config);
            DataContext = _viewModel;
        }

        /// <summary>
        /// 获取编辑后的配置
        /// </summary>
        public FrpcConfig Config => _viewModel.Config;

        /// <summary>
        /// 获取对话框结果
        /// </summary>
        public bool Result => _result;

        /// <summary>
        /// 确定按钮点击事件
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            _result = true;
            Close();
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _result = false;
            Close();
        }
    }
}
