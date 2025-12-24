using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace CA210WhiteBalance.UI.Mocks
{
    /// <summary>
    /// 模拟串口服务（用于GitHub Actions编译）
    /// </summary>
    public class MockSerialPortService
    {
        private readonly ILogger<MockSerialPortService> _logger;
        private bool _isOpen;

        public event EventHandler<bool> ConnectionChanged;
        public event EventHandler<string> DataReceived;

        public bool IsOpen => _isOpen;
        public string PortName { get; private set; } = "COM1";

        public MockSerialPortService(ILogger<MockSerialPortService> logger)
        {
            _logger = logger;
        }

        public bool Open(string portName, int baudRate)
        {
            _logger.LogInformation("模拟打开串口: {Port}, 波特率: {BaudRate}", portName, baudRate);
            _isOpen = true;
            PortName = portName;
            ConnectionChanged?.Invoke(this, true);
            _logger.LogInformation("模拟串口已打开");
            return true;
        }

        public void Close()
        {
            _isOpen = false;
            ConnectionChanged?.Invoke(this, false);
            _logger.LogInformation("模拟串口已关闭");
        }

        public void SendCommand(byte[] data)
        {
            _logger.LogDebug("模拟发送命令: {Data}", BitConverter.ToString(data));
        }

        public string[] GetAvailablePorts()
        {
            return new[] { "COM1", "COM2", "COM3" };
        }

        public void Dispose()
        {
            Close();
        }
    }
}
