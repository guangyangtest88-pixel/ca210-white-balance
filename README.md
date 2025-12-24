# CA210白平衡上位机软件

## 项目简介

CA210白平衡上位机软件是一款配合KONICA MINOLTA CA-210色彩分析仪使用的Windows上位机软件，用于显示大屏的白平衡自动调试和色彩压测。

## 功能特点

- **CA-210设备通信**: 通过USB Type-B接口与CA-210色彩分析仪通信，实时获取测量数据
- **RS232串口控制**: 支持通过RS232串口发送RGB调整指令到显示大屏
- **自动白平衡调试**: 设置目标xy值后，自动调整RGB直到满足容差要求(默认±0.005)
- **实时数据显示**: 实时显示亮度、色度坐标、色温等测量数据
- **数据记录与报告**: 支持导出测量数据和调试报告

## 系统要求

- 操作系统: Windows 7/10/11 (64位)
- .NET 6.0 Runtime
- CA-SDK 4.50 (随CA-210设备提供)
- USB Type-B线缆
- RS232串口线缆

## 编译说明

### 前置条件

1. 安装Visual Studio 2022或更新版本
2. 安装.NET 6.0 SDK
3. 安装CA-SDK 4.50并注册COM组件

### 添加CA-SDK COM引用

由于CA-SDK是COM组件，需要在Visual Studio中手动添加引用：

1. 在解决方案资源管理器中，右键点击 `CA210WhiteBalance.Core` 项目
2. 选择"添加" -> "引用"
3. 选择"COM"选项卡
4. 查找并勾选 "CA200Srvr Type Library" 或类似名称
5. 点击"确定"

如果找不到COM引用，需要先注册CA-SDK的DLL：

```batch
regsvr32 /s "path\to\CA200Srvr.dll"
```

### 编译步骤

1. 打开 `CA210WhiteBalance.sln`
2. 选择 "Release" 配置
3. 右键点击解决方案，选择"生成解决方案"
4. 编译成功后，可执行文件位于 `src/CA210WhiteBalance.UI/bin/Release/net6.0-windows/`

## 安装说明

### 标准安装

1. 从 `setup/Output/` 目录运行 `CA210WhiteBalance.msi`
2. 按照安装向导完成安装
3. 首次运行需要安装CA-SDK驱动

### 手动安装

1. 将 `CA210WhiteBalance.exe` 及相关DLL复制到目标文件夹
2. 确保CA-SDK的 `CA200Srvr.dll` 在同一目录或已注册到系统
3. 运行 `CA210WhiteBalance.exe`

## 使用说明

### 基本操作流程

1. **连接设备**
   - 使用USB Type-B线连接CA-210到电脑
   - 点击"连接设备"按钮连接CA-210
   - 选择正确的COM端口并打开串口

2. **零校准**
   - 点击"零校准"按钮进行零点校准
   - 确保CA-210探头处于无光照状态

3. **测量**
   - 点击"单次测量"进行一次测量
   - 或点击"连续测量"进行连续测量

4. **白平衡调试**
   - 设置目标x值和y值
   - 设置容差范围(默认±0.005)
   - 点击"开始调试"按钮
   - 等待自动调试完成

### 协议配置

软件内置了几种常用的显示大屏协议：

- **Default**: 默认协议，帧头AA，帧尾55
- **Samsung**: 三星协议
- **LG**: LG协议
- **Sony**: 索尼协议

如需使用自定义协议，请参考技术方案文档。

## 文件结构

```
CA210WhiteBalance/
├── src/
│   ├── CA210WhiteBalance.Core/       # 核心业务逻辑
│   │   ├── CA210/                    # CA-210通信
│   │   ├── SerialPort/               # RS232串口通信
│   │   ├── Algorithm/                # 白平衡算法
│   │   └── Models/                   # 数据模型
│   ├── CA210WhiteBalance.UI/         # WPF界面
│   │   ├── Views/                    # XAML视图
│   │   ├── ViewModels/               # ViewModel
│   │   └── Controls/                 # 自定义控件
│   └── CA210WhiteBalance.Services/   # 服务层
├── lib/CA-SDK/                       # CA-SDK库文件
├── docs/                             # 文档
└── setup/                            # 安装脚本
```

## 配置文件

`appsettings.json`:

```json
{
  "CA210": {
    "AutoConnect": true,
    "RemoteMode": true,
    "MeasureInterval": 500
  },
  "WhiteBalance": {
    "DefaultTargetX": 0.3130,
    "DefaultTargetY": 0.3290,
    "DefaultTolerance": 0.005,
    "MaxIterations": 50
  }
}
```

## 故障排除

### CA-210连接失败

1. 检查USB线缆是否正确连接
2. 检查CA-SDK是否正确安装
3. 尝试重新安装CA-SDK驱动

### 串口打开失败

1. 检查COM端口是否被其他程序占用
2. 检查串口线缆是否连接
3. 尝试更改波特率设置

### 调试不收敛

1. 检查目标xy值是否在合理范围内
2. 增加容差范围
3. 检查显示大屏的RGB控制协议是否正确

## 开发团队

本项目基于以下开源项目开发：

- [Ca210Sample](https://github.com/dwatow/Ca210Sample) - CA-210 SDK示例代码
- [AutoWBAdjustTool](https://github.com/heray1990/AutoWBAdjustTool) - 自动白平衡工具参考

## 许可证

本项目仅供学习和研究使用。

## 版本历史

| 版本 | 日期 | 说明 |
|------|------|------|
| v1.0 | 2024-12-24 | 初始版本 |

## 联系方式

如有问题或建议，请联系开发者。
