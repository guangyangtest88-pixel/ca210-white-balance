using System;
using System.Collections.Generic;
using CA210WhiteBalance.Core.Models;

namespace CA210WhiteBalance.Core.SerialPort
{
    /// <summary>
    /// 协议配置
    /// </summary>
    public class ProtocolConfig
    {
        /// <summary>协议名称</summary>
        public string Name { get; set; }

        /// <summary>帧头</summary>
        public string Header { get; set; }

        /// <summary>帧尾</summary>
        public string Footer { get; set; }

        /// <summary>数据长度</summary>
        public int DataLength { get; set; }

        /// <summary>命令模板</summary>
        public string CommandTemplate { get; set; }

        /// <summary>响应延迟(ms)</summary>
        public int ResponseDelayMs { get; set; }

        /// <summary>是否需要校验和</summary>
        public bool UseChecksum { get; set; }

        /// <summary>R/G/B基准值</summary>
        public int RGBBaseValue { get; set; } = 128;
    }

    /// <summary>
    /// 协议管理器
    /// </summary>
    public class ProtocolManager
    {
        private ProtocolConfig _config;
        private Dictionary<string, ProtocolConfig> _presets;

        public ProtocolManager()
        {
            InitializePresets();
            _config = _presets["Default"];
        }

        private void InitializePresets()
        {
            _presets = new Dictionary<string, ProtocolConfig>
            {
                ["Default"] = new ProtocolConfig
                {
                    Name = "Default",
                    Header = "AA",
                    Footer = "55",
                    DataLength = 6,
                    CommandTemplate = "RRGGBB",
                    ResponseDelayMs = 100,
                    UseChecksum = false
                },
                ["Samsung"] = new ProtocolConfig
                {
                    Name = "Samsung",
                    Header = "A5",
                    Footer = "5A",
                    DataLength = 8,
                    CommandTemplate = "01 01 RR GG BB CS",
                    ResponseDelayMs = 150,
                    UseChecksum = true
                },
                ["LG"] = new ProtocolConfig
                {
                    Name = "LG",
                    Header = "[",
                    Footer = "]",
                    DataLength = 12,
                    CommandTemplate = "RGB:RR,GG,BB",
                    ResponseDelayMs = 200,
                    UseChecksum = false
                },
                ["Sony"] = new ProtocolConfig
                {
                    Name = "Sony",
                    Header = "00",
                    Footer = "FF",
                    DataLength = 7,
                    CommandTemplate = "50 RR GG BB CS",
                    ResponseDelayMs = 120,
                    UseChecksum = true
                }
            };
        }

        /// <summary>获取所有预设协议名称</summary>
        public string[] GetPresetNames()
        {
            var names = new string[_presets.Count];
            _presets.Keys.CopyTo(names, 0);
            return names;
        }

        /// <summary>设置协议</summary>
        public void SetProtocol(string protocolName)
        {
            if (_presets.ContainsKey(protocolName))
            {
                _config = _presets[protocolName];
            }
            else
            {
                throw new ArgumentException($"协议不存在: {protocolName}");
            }
        }

        /// <summary>设置自定义协议</summary>
        public void SetCustomProtocol(ProtocolConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>获取当前配置</summary>
        public ProtocolConfig GetConfig()
        {
            return _config;
        }

        /// <summary>根据RGB值生成命令帧</summary>
        public byte[] BuildCommand(RGBCommand rgb)
        {
            if (_config == null)
                throw new InvalidOperationException("协议未配置");

            // 限制RGB范围
            rgb.Clamp(0, 255);

            // 构建命令主体
            string command = _config.CommandTemplate;

            // 替换RGB占位符
            command = command.Replace("RR", rgb.R.ToString("X2"))
                           .Replace("GG", rgb.G.ToString("X2"))
                           .Replace("BB", rgb.B.ToString("X2"));

            // 计算校验和
            if (_config.UseChecksum)
            {
                byte checksum = CalculateChecksum(command);
                command = command.Replace("CS", checksum.ToString("X2"));
            }

            // 添加帧头帧尾
            string frame = $"{_config.Header}{command}{_config.Footer}";

            // 转换为字节数组
            return ParseHexString(frame);
        }

        /// <summary>获取响应延迟</summary>
        public int GetResponseDelay()
        {
            return _config?.ResponseDelayMs ?? 100;
        }

        /// <summary>计算校验和（简单的XOR校验）</summary>
        private byte CalculateChecksum(string hexData)
        {
            byte checksum = 0;

            // 移除空格
            hexData = hexData.Replace(" ", "").Replace("RR", "00")
                                           .Replace("GG", "00")
                                           .Replace("BB", "00")
                                           .Replace("CS", "00");

            for (int i = 0; i < hexData.Length; i += 2)
            {
                byte b = Convert.ToByte(hexData.Substring(i, 2), 16);
                checksum ^= b;
            }

            return checksum;
        }

        /// <summary>解析十六进制字符串为字节数组</summary>
        private byte[] ParseHexString(string hex)
        {
            // 移除空格和0x前缀
            hex = hex.Replace(" ", "").Replace("0x", "");

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        /// <summary>字节数组转十六进制字符串</summary>
        public static string ToHexString(byte[] bytes)
        {
            char[] hex = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                int value = bytes[i];
                hex[i * 2] = NibbleToHex(value >> 4);
                hex[i * 2 + 1] = NibbleToHex(value & 0x0F);
            }
            return new string(hex);
        }

        private static char NibbleToHex(int value)
        {
            return (char)(value < 10 ? '0' + value : 'A' + value - 10);
        }
    }
}
