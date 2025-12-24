using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Ports;
using Microsoft.Extensions.Logging;

namespace CA210WhiteBalance.Core.SerialPort
{
    /// <summary>
    /// 串口服务接口
    /// </summary>
    public interface ISerialPortService : IDisposable
    {
        /// <summary>连接状态变化事件</summary>
        event EventHandler<bool> ConnectionChanged;

        /// <summary>数据接收事件</summary>
        event EventHandler<string> DataReceived;

        /// <summary>是否已打开</summary>
        bool IsOpen { get; }

        /// <summary>获取可用串口列表</summary>
        static string[] GetAvailablePorts();

        /// <summary>打开串口</summary>
        bool Open(string portName, int baudRate = 9600, Parity parity = Parity.None,
                 int dataBits = 8, StopBits stopBits = StopBits.One);

        /// <summary>关闭串口</summary>
        void Close();

        /// <summary>发送数据</summary>
        Task<bool> SendAsync(byte[] data);

        /// <summary>发送文本命令</summary>
        Task<bool> SendCommandAsync(string command);
    }

    /// <summary>
    /// 串口服务实现
    /// </summary>
    public class SerialPortService : ISerialPortService
    {
        private readonly ILogger<SerialPortService> _logger;
        private SerialPort _serialPort;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        public SerialPortService(ILogger<SerialPortService> logger)
        {
            _logger = logger;
        }

        public event EventHandler<bool> ConnectionChanged;
        public event EventHandler<string> DataReceived;

        public bool IsOpen => _serialPort?.IsOpen ?? false;

        public static string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }

        public bool Open(string portName, int baudRate = 9600,
            Parity parity = Parity.None, int dataBits = 8,
            StopBits stopBits = StopBits.One)
        {
            try
            {
                Close();

                _logger.LogInformation("正在打开串口: {Port}, 波特率: {Baud}", portName, baudRate);

                _serialPort = new SerialPort
                {
                    PortName = portName,
                    BaudRate = baudRate,
                    Parity = parity,
                    DataBits = dataBits,
                    StopBits = stopBits,
                    ReadTimeout = 1000,
                    WriteTimeout = 1000,
                    Handshake = Handshake.None
                };

                _serialPort.DataReceived += OnDataReceived;
                _serialPort.Open();

                ConnectionChanged?.Invoke(this, true);
                _logger.LogInformation("串口已打开: {Port}", portName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "串口打开失败: {Port}", portName);
                ConnectionChanged?.Invoke(this, false);
                return false;
            }
        }

        public void Close()
        {
            if (_serialPort != null)
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                    _logger.LogInformation("串口已关闭");
                }
                _serialPort.DataReceived -= OnDataReceived;
                _serialPort.Dispose();
                _serialPort = null;
                ConnectionChanged?.Invoke(this, false);
            }
        }

        public async Task<bool> SendAsync(byte[] data)
        {
            return await _sendLock.ThrottleAsync(async () =>
            {
                try
                {
                    if (_serialPort?.IsOpen != true)
                    {
                        _logger.LogWarning("串口未打开，无法发送数据");
                        return false;
                    }

                    await _serialPort.BaseStream.WriteAsync(data, 0, data.Length);
                    _logger.LogDebug("发送数据: {Data} 字节", data.Length);

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "串口发送失败");
                    return false;
                }
            });
        }

        public async Task<bool> SendCommandAsync(string command)
        {
            var data = System.Text.Encoding.ASCII.GetBytes(command);
            return await SendAsync(data);
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_serialPort?.IsOpen == true)
                {
                    var data = _serialPort.ReadExisting();
                    DataReceived?.Invoke(this, data);
                    _logger.LogDebug("接收数据: {Data}", data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "串口接收失败");
            }
        }

        public void Dispose()
        {
            Close();
            _sendLock?.Dispose();
        }
    }
}

/// <summary>
/// SemaphoreSlim扩展方法
/// </summary>
public static class SemaphoreSlimExtensions
{
    public static async Task<TResult> ThrottleAsync<TResult>(
        this SemaphoreSlim semaphore,
        Func<Task<TResult>> task)
    {
        await semaphore.WaitAsync();
        try
        {
            return await task();
        }
        finally
        {
            semaphore.Release();
        }
    }
}
