using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CA210WhiteBalance.UI.Mocks
{
    /// <summary>
    /// 模拟CA-210服务实现（用于GitHub Actions编译）
    /// 实际使用时需要使用真实的CA210Service
    /// </summary>
    public class MockCA210Service : IDisposable
    {
        private readonly ILogger<MockCA210Service> _logger;
        private bool _isConnected;
        private bool _isRemoteMode;
        private readonly Random _random = new Random();

        public event EventHandler<bool> ConnectionChanged;
        public event EventHandler<MockCA210Data> MeasurementComplete;

        public bool IsConnected => _isConnected;
        public bool IsRemoteMode => _isRemoteMode;
        public string ChannelInfo { get; private set; } = "模拟通道";

        public MockCA210Service(ILogger<MockCA210Service> logger)
        {
            _logger = logger;
        }

        public Task<bool> ConnectAsync()
        {
            return Task.Run(() =>
            {
                _logger.LogInformation("模拟CA-210设备连接...");
                _isConnected = true;
                ChannelInfo = "CH01";
                ConnectionChanged?.Invoke(this, true);
                _logger.LogInformation("模拟CA-210设备已连接");
                return true;
            });
        }

        public void Disconnect()
        {
            _isConnected = false;
            ConnectionChanged?.Invoke(this, false);
            _logger.LogInformation("模拟CA-210设备已断开");
        }

        public void SetRemoteMode(bool enable)
        {
            _isRemoteMode = enable;
            _logger.LogInformation("模拟远程模式: {Enabled}", enable);
        }

        public Task<bool> CalibrateZeroAsync()
        {
            return Task.Run(() =>
            {
                _logger.LogInformation("模拟零校准...");
                Thread.Sleep(500);
                _logger.LogInformation("模拟零校准完成");
                return true;
            });
        }

        public Task<MockCA210Data> MeasureAsync()
        {
            return Task.Run(() =>
            {
                var data = new MockCA210Data
                {
                    Timestamp = DateTime.Now,
                    Lv = 100 + (float)_random.NextDouble() * 50,
                    Sx = 0.31f + (float)_random.NextDouble() * 0.01f,
                    Sy = 0.32f + (float)_random.NextDouble() * 0.01f,
                    T = 6500 + _random.Next(500),
                    Ud = 0.18f + (float)_random.NextDouble() * 0.02f,
                    Vd = 0.43f + (float)_random.NextDouble() * 0.02f,
                    X = 80 + (float)_random.NextDouble() * 20,
                    Y = 90 + (float)_random.NextDouble() * 20,
                    Z = 70 + (float)_random.NextDouble() * 20
                };

                MeasurementComplete?.Invoke(this, data);
                _logger.LogDebug("模拟测量: {Data}", data);
                return data;
            });
        }

        public Task StartContinuousMeasurementAsync(int intervalMs, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                _logger.LogInformation("开始模拟连续测量");
                while (!cancellationToken.IsCancellationRequested && _isConnected)
                {
                    await MeasureAsync();
                    await Task.Delay(intervalMs, cancellationToken);
                }
            }, cancellationToken);
        }

        public void Dispose()
        {
            Disconnect();
        }
    }

    /// <summary>
    /// 模拟测量数据
    /// </summary>
    public class MockCA210Data
    {
        public DateTime Timestamp { get; set; }
        public float Lv { get; set; }
        public float Sx { get; set; }
        public float Sy { get; set; }
        public float T { get; set; }
        public float Duv { get; set; }
        public float Ud { get; set; }
        public float Vd { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public float DeltaX(float targetX) => Math.Abs(Sx - targetX);
        public float DeltaY(float targetY) => Math.Abs(Sy - targetY);
        public bool IsInTolerance(float targetX, float targetY, float tolerance = 0.005f)
        {
            return DeltaX(targetX) <= tolerance && DeltaY(targetY) <= tolerance;
        }

        public override string ToString() => $"Lv={Lv:F2}, x={Sx:F4}, y={Sy:F4}, T={T:F0}K";
    }
}
