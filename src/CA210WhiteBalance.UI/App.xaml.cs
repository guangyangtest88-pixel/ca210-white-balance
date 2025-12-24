using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace CA210WhiteBalance.UI
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 配置服务
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();

            // 配置NLog
            ConfigureLogging();

            // 显示主窗口
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Core服务
            services.AddSingleton<CA210WhiteBalance.Core.CA210.ICA210Service, CA210WhiteBalance.Core.CA210.CA210Service>();
            services.AddSingleton<CA210WhiteBalance.Core.SerialPort.ISerialPortService, CA210WhiteBalance.Core.SerialPort.SerialPortService>();
            services.AddSingleton<CA210WhiteBalance.Core.SerialPort.ProtocolManager>();
            services.AddSingleton<CA210WhiteBalance.Core.Algorithm.IWhiteBalanceAlgorithm, CA210WhiteBalance.Core.Algorithm.WhiteBalanceAlgorithm>();

            // UI服务
            services.AddSingleton<MainWindow>();

            // ViewModel
            services.AddSingleton<ViewModels.MainWindowViewModel>();

            // 应用服务
            services.AddSingleton<Services.ILogService, Services.LogService>();
            services.AddSingleton<Services.IReportService, Services.ReportService>();
        }

        private void ConfigureLogging()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // 配置NLog
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                builder.AddNLog(config);
                builder.AddConsole();
            });

            ServiceProvider = loggerFactory.CreateServiceProvider();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 清理资源
            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
            base.OnExit(e);
        }
    }
}
