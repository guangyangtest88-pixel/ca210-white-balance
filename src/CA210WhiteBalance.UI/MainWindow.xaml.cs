using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CA210WhiteBalance.UI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MainWindow> _logger;

        public MainWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<MainWindow>>();

            // 初始化ViewModel
            var viewModel = serviceProvider.GetRequiredService<ViewModels.MainWindowViewModel>();
            DataContext = viewModel;

            // 订阅窗口事件
            this.Closing += MainWindow_Closing;

            _logger.LogInformation("CA210白平衡上位机软件已启动");
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _logger.LogInformation("CA210白平衡上位机软件正在关闭...");

            // 执行清理操作
            var viewModel = DataContext as ViewModels.MainWindowViewModel;
            viewModel?.Cleanup();

            _logger.LogInformation("CA210白平衡上位机软件已关闭");
        }
    }
}
