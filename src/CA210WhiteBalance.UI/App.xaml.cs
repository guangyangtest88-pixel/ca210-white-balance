using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using CA210WhiteBalance.UI.Mocks;

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
            // 注册Mock服务（用于GitHub Actions编译）
            // TODO: 本地Windows开发时需要替换为真实的CA210Service等
            services.AddSingleton<MockCA210Service>();
            services.AddSingleton<MockSerialPortService>();
            services.AddSingleton<MockWhiteBalanceAlgorithm>();

            // UI服务
            services.AddSingleton<MainWindow>();

            // ViewModel - 使用Mock服务创建
            services.AddSingleton<ViewModels.MainWindowViewModel>(sp =>
            {
                var ca210Service = sp.GetRequiredService<MockCA210Service>();
                var serialService = sp.GetRequiredService<MockSerialPortService>();
                var algorithm = sp.GetRequiredService<MockWhiteBalanceAlgorithm>();
                var reportService = sp.GetRequiredService<Services.IReportService>();
                var logger = sp.GetRequiredService<ILogger<ViewModels.MainWindowViewModel>>();

                return new ViewModels.MainWindowViewModel(ca210Service, serialService, algorithm, reportService, logger);
            });

            // 应用服务
            services.AddSingleton<Services.ILogService, Services.LogService>();
            services.AddSingleton<Services.IReportService, Services.ReportService>();

            // 日志服务
            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            services.AddLogging();
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
