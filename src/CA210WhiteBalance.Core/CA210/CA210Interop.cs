using System;
using System.Runtime.InteropServices;

namespace CA210WhiteBalance.Core.CA210
{
    /// <summary>
    /// CA-SDK COM接口定义
    /// 注意：实际GUID需要从CA-SDK类型库中获取
    /// 使用Visual Studio的"添加引用" -> "COM" -> "CA200Srvr Type Library"自动生成
    /// </summary>

    // CA200主接口
    [ComImport]
    [Guid("000209F6-0000-0000-C000-000000000046")] // 示例GUID，需替换
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface ICa200
    {
        /// <summary>自动连接设备</summary>
        void AutoConnect();

        /// <summary>获取单个CA设备</summary>
        object GetSingleCa();

        /// <summary>获取CA设备列表</summary>
        object GetCas();
    }

    // CA设备接口
    [ComImport]
    [Guid("000209F7-0000-0000-C000-000000000046")] // 示例GUID，需替换
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface ICa
    {
        /// <summary>执行测量 (mode: 0=正常, 1=快速)</summary>
        void Measure(int mode);

        /// <summary>零校准</summary>
        void CalZero();

        /// <summary>设置远程模式</summary>
        void SetRemoteMode([MarshalAs(UnmanagedType.Bool)] bool isOnline);

        /// <summary>获取单个探头</summary>
        object GetSingleProbe();

        /// <summary>获取内存数据</summary>
        object GetMemory();

        /// <summary>获取探头列表</summary>
        object GetProbes();
    }

    // 探头接口
    [ComImport]
    [Guid("000209F8-0000-0000-C000-000000000046")] // 示例GUID，需替换
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IProbe
    {
        /// <summary>获取亮度值</summary>
        float GetLv();

        /// <summary>获取色度x值</summary>
        float GetSx();

        /// <summary>获取色度y值</summary>
        float GetSy();

        /// <summary>获取色温</summary>
        float GetT();

        /// <summary>获取Duv值</summary>
        float GetDuv();

        /// <summary>获取u'值</summary>
        float GetUd();

        /// <summary>获取v'值</summary>
        float GetVd();

        /// <summary>获取X值</summary>
        float GetX();

        /// <summary>获取Y值</summary>
        float GetY();

        /// <summary>获取Z值</summary>
        float GetZ();
    }

    // 内存接口
    [ComImport]
    [Guid("000209F9-0000-0000-C000-000000000046")] // 示例GUID，需替换
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IMemory
    {
        /// <summary>获取通道ID</summary>
        string GetChannelID();

        /// <summary>获取通道号</summary>
        int GetChannelNO();

        /// <summary>获取校准数据</summary>
        object GetCalData();
    }

    // CA200 COM类
    [ComImport]
    [Guid("000209FA-0000-0000-C000-000000000046")] // 示例GUID，需替换
    [ClassInterfaceType(ClassInterfaceType.None)]
    public class Ca200Class { }

    /// <summary>
    /// 连接状态枚举
    /// </summary>
    public enum ConnectionStatus
    {
        /// <summary>已连接</summary>
        Connected,

        /// <summary>未连接</summary>
        Disconnected,

        /// <summary>连接失败</summary>
        Failed,

        /// <summary>测量中</summary>
        Measuring
    }
}
