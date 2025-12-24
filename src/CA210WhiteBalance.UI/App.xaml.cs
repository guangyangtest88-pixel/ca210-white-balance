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

        /// <summary>
        /// 检测是否在GitHub Actions环境中运行
        /// </summary>
        private static bool IsRunningInGitHubActions()
        {
            var githubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
            return !string.IsNullOrEmpty(githubActions) && githubActions.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

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
            bool isGitHubActions = IsRunningInGitHubActions();

            if (isGitHubActions)
            {
                // GitHub Actions环境：使用Mock服务进行编译
                services.AddSingleton<MockCA210Service>();
                services.AddSingleton<MockSerialPortService>();
                services.AddSingleton<MockWhiteBalanceAlgorithm>();

                // 注册Mock服务的别名，使ViewModel可以使用
                services.AddSingleton(sp => sp.GetRequiredService<MockCA210Service>());
                services.AddSingleton(sp => sp.GetRequiredService<MockSerialPortService>());
                services.AddSingleton(sp => sp.GetRequiredService<MockWhiteBalanceAlgorithm>());
            }
            else
            {
                // 本地Windows环境：使用真实的Core服务
#if !GITHUB_ACTIONS
                services.AddSingleton<CA210WhiteBalance.Core.CA210.ICA210Service, CA210WhiteBalance.Core.CA210.CA210Service>();
                services.AddSingleton<CA210WhiteBalance.Core.SerialPort.ISerialPortService, CA210WhiteBalance.Core.SerialPort.SerialPortService>();
                services.AddSingleton<CA210WhiteBalance.Core.SerialPort.ProtocolManager>();
                services.AddSingleton<CA210WhiteBalance.Core.Algorithm.IWhiteBalanceAlgorithm, CA210WhiteBalance.Core.Algorithm.WhiteBalanceAlgorithm>();
#endif
            }

            // UI服务
            services.AddSingleton<MainWindow>();

            // ViewModel - 使用工厂方法根据环境创建
            services.AddSingleton<ViewModels.MainWindowViewModel>(sp =>
            {
                if (isGitHubActions)
                {
                    // 使用Mock服务创建ViewModel
                    var ca210Service = sp.GetRequiredService<MockCA210Service>();
                    var serialService = sp.GetRequiredService<MockSerialPortService>();
                    var algorithm = sp.GetRequiredService<MockWhiteBalanceAlgorithm>();
                    var reportService = sp.GetRequiredService<Services.IReportService>();
                    var logger = sp.GetRequiredService<ILogger<ViewModels.MainWindowViewModel>>();

                    return new ViewModels.MainWindowViewModel(ca210Service, serialService, algorithm, reportService, logger);
                }
                else
                {
#if !GITHUB_ACTIONS
                    // 使用真实服务创建ViewModel
                    var ca210Service = sp.GetRequiredService<CA210WhiteBalance.Core.CA210.ICA210Service>();
                    var serialService = sp.GetRequiredService<CA210WhiteBalance.Core.SerialPort.ISerialPortService>();
                    var algorithm = sp.GetRequiredService<CA210WhiteBalance.Core.Algorithm.IWhiteBalanceAlgorithm>();
                    var reportService = sp.GetRequiredService<Services.IReportService>();
                    var logger = sp.GetRequiredService<ILogger<ViewModels.MainWindowViewModel>>();

                    return new ViewModels.MainWindowViewModel(ca210Service, serialService, algorithm, reportService, logger);
#else
                    // GitHub Actions编译时的fallback
                    var ca210Service = sp.GetRequiredService<MockCA210Service>();
                    var serialService = sp.GetRequiredService<MockSerialPortService>();
                    var algorithm = sp.GetRequiredService<MockWhiteBalanceAlgorithm>();
                    var reportService = sp.GetRequiredService<Services.IReportService>();
                    var logger = sp.GetRequiredService<ILogger<ViewModels.MainWindowViewModel>>();

                    return new ViewModels.MainWindowViewModel(ca210Service, serialService, algorithm, reportService, logger);
#endif
                }
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
