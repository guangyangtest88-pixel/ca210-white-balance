using System;
using Microsoft.Extensions.Logging;

namespace CA210WhiteBalance.Services
{
    /// <summary>
    /// 日志服务接口
    /// </summary>
    public interface ILogService
    {
        ILogger<T> GetLogger<T>();
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message);
        void LogError(string message, Exception exception);
    }

    /// <summary>
    /// 日志服务实现
    /// </summary>
    public class LogService : ILogService
    {
        private readonly ILoggerFactory _loggerFactory;

        public LogService(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public ILogger<T> GetLogger<T>()
        {
            return _loggerFactory.CreateLogger<T>();
        }

        public void LogInformation(string message)
        {
            GetLogger<LogService>().LogInformation(message);
        }

        public void LogWarning(string message)
        {
            GetLogger<LogService>().LogWarning(message);
        }

        public void LogError(string message)
        {
            GetLogger<LogService>().LogError(message);
        }

        public void LogError(string message, Exception exception)
        {
            GetLogger<LogService>().LogError(exception, message);
        }
    }
}
