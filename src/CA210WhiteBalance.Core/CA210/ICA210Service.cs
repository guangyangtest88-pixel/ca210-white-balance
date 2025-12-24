using System;
using System.Threading;
using System.Threading.Tasks;
using CA210WhiteBalance.Core.Models;
using Microsoft.Extensions.Logging;

namespace CA210WhiteBalance.Core.CA210
{
    /// <summary>
    /// CA-210设备服务接口
    /// </summary>
    public interface ICA210Service : IDisposable
    {
        /// <summary>连接状态变化事件</summary>
        event EventHandler<ConnectionStatus> ConnectionChanged;

        /// <summary>测量完成事件</summary>
        event EventHandler<CA210Data> MeasurementComplete;

        /// <summary>是否已连接</summary>
        bool IsConnected { get; }

        /// <summary>是否处于远程模式</summary>
        bool IsRemoteMode { get; }

        /// <summary>通道信息</summary>
        string ChannelInfo { get; }

        /// <summary>连接设备</summary>
        Task<bool> ConnectAsync();

        /// <summary>断开连接</summary>
        void Disconnect();

        /// <summary>设置远程模式</summary>
        void SetRemoteMode(bool enable);

        /// <summary>零校准</summary>
        Task<bool> CalibrateZeroAsync();

        /// <summary>单次测量</summary>
        Task<CA210Data> MeasureAsync();

        /// <summary>开始连续测量</summary>
        Task StartContinuousMeasurementAsync(int intervalMs, CancellationToken cancellationToken);
    }

    /// <summary>
    /// CA-210设备服务实现
    /// </summary>
    public class CA210Service : ICA210Service
    {
        private readonly ILogger<CA210Service> _logger;
        private ICa200 _ca200;
        private ICa _ca;
        private IProbe _probe;
        private IMemory _memory;
        private bool _isConnected;
        private bool _isRemoteMode;
        private readonly object _lock = new object();

        public CA210Service(ILogger<CA210Service> logger)
        {
            _logger = logger;
        }

        public event EventHandler<ConnectionStatus> ConnectionChanged;
        public event EventHandler<CA210Data> MeasurementComplete;

        public bool IsConnected => _isConnected;
        public bool IsRemoteMode => _isRemoteMode;
        public string ChannelInfo { get; private set; }

        public async Task<bool> ConnectAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("正在连接CA-210设备...");

                    // 创建COM对象
                    _ca200 = new Ca200Class();
                    _ca200.AutoConnect();

                    _ca = (ICa)_ca200.GetSingleCa();
                    _probe = (IProbe)_ca.GetSingleProbe();
                    _memory = (IMemory)_ca.GetMemory();

                    // 获取通道信息
                    ChannelInfo = _memory.GetChannelID();

                    _isConnected = true;
                    ConnectionChanged?.Invoke(this, ConnectionStatus.Connected);

                    _logger.LogInformation("CA-210设备连接成功, 通道: {Channel}", ChannelInfo);
                    return true;
                }
                catch (Exception ex)
                {
                    _isConnected = false;
                    ConnectionChanged?.Invoke(this, ConnectionStatus.Failed);
                    _logger.LogError(ex, "CA-210设备连接失败");
                    return false;
                }
            });
        }

        public void Disconnect()
        {
            lock (_lock)
            {
                if (_isRemoteMode)
                {
                    SetRemoteMode(false);
                }

                ReleaseComObjects();

                _isConnected = false;
                ConnectionChanged?.Invoke(this, ConnectionStatus.Disconnected);

                _logger.LogInformation("CA-210设备已断开连接");
            }
        }

        public void SetRemoteMode(bool enable)
        {
            lock (_lock)
            {
                if (_ca != null && _isConnected)
                {
                    _ca.SetRemoteMode(enable);
                    _isRemoteMode = enable;
                    _logger.LogInformation("远程模式已{Status}", enable ? "启用" : "禁用");
                }
            }
        }

        public async Task<bool> CalibrateZeroAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("开始零校准...");

                    lock (_lock)
                    {
                        _ca?.CalZero();
                    }

                    // 等待校准完成
                    await Task.Delay(2000);

                    _logger.LogInformation("零校准完成");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "零校准失败");
                    return false;
                }
            });
        }

        public async Task<CA210Data> MeasureAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (_lock)
                    {
                        if (_ca == null || _probe == null || !_isConnected)
                        {
                            _logger.LogWarning("设备未连接，无法测量");
                            return null;
                        }

                        _ca.Measure(0);

                        var data = new CA210Data
                        {
                            Timestamp = DateTime.Now,
                            Lv = _probe.GetLv(),
                            Sx = _probe.GetSx(),
                            Sy = _probe.GetSy(),
                            T = _probe.GetT(),
                            Duv = _probe.GetDuv(),
                            Ud = _probe.GetUd(),
                            Vd = _probe.GetVd(),
                            X = _probe.GetX(),
                            Y = _probe.GetY(),
                            Z = _probe.GetZ()
                        };

                        MeasurementComplete?.Invoke(this, data);
                        _logger.LogDebug("测量完成: {Data}", data);

                        return data;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "测量失败");
                    return null;
                }
            });
        }

        public async Task StartContinuousMeasurementAsync(int intervalMs, CancellationToken cancellationToken)
        {
            await Task.Run(async () =>
            {
                _logger.LogInformation("开始连续测量, 间隔: {Interval}ms", intervalMs);

                while (!cancellationToken.IsCancellationRequested && _isConnected)
                {
                    await MeasureAsync();
                    await Task.Delay(intervalMs, cancellationToken);
                }

                _logger.LogInformation("连续测量已停止");
            }, cancellationToken);
        }

        private void ReleaseComObjects()
        {
            if (_probe != null)
            {
                Marshal.ReleaseComObject(_probe);
                _probe = null;
            }
            if (_memory != null)
            {
                Marshal.ReleaseComObject(_memory);
                _memory = null;
            }
            if (_ca != null)
            {
                Marshal.ReleaseComObject(_ca);
                _ca = null;
            }
            if (_ca200 != null)
            {
                Marshal.ReleaseComObject(_ca200);
                _ca200 = null;
            }
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
