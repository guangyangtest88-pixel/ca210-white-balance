# Windows编译和打包说明

## 环境准备

### 1. 安装开发工具

在Windows电脑上安装以下工具：

- Visual Studio 2022 (Community/Professional/Enterprise)
- .NET 6.0 SDK
- WiX Toolset (用于生成MSI安装包)

### 2. 获取CA-SDK

将CA-SDK相关文件复制到 `lib/CA-SDK/` 目录：

```
lib/CA-SDK/
├── CA200Srvr.dll           # CA-SDK核心DLL
├── CA200Srvr.tlb          # 类型库文件
└── CA200Srvr.ini          # 配置文件
```

## 编译步骤

### 方法一: 使用Visual Studio

1. 双击打开 `CA210WhiteBalance.sln`

2. 添加COM引用:
   - 右键点击 `CA210WhiteBalance.Core` 项目
   - 选择"添加" → "引用"
   - 点击"COM"选项卡
   - 勾选 "CA200Srvr Type Library"
   - 点击"确定"

3. 选择 "Release" 配置和 "Any CPU" 平台

4. 右键点击解决方案，选择"生成解决方案"

5. 编译成功后，可执行文件位于:
   ```
   src/CA210WhiteBalance.UI/bin/Release/net6.0-windows/
   ```

### 方法二: 使用命令行

```batch
# 进入项目目录
cd CA210WhiteBalance

# 还原NuGet包
dotnet restore

# 编译Release版本
dotnet build -c Release

# 发布为单文件
dotnet publish src/CA210WhiteBalance.UI/CA210WhiteBalance.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true
```

发布后的文件位于:
```
src/CA210WhiteBalance.UI/bin/Release/net6.0-windows/win-x64/publish/
```

## 打包为安装程序

### 使用WiX Toolset

1. 安装WiX Toolset v3.11或更新版本

2. 创建WiX项目或使用提供的Product.wxs

3. 修改Product.wxs中的GUID和文件路径

4. 使用命令行编译:
```batch
candle.exe setup/Product.wxs
light.exe -out setup/Output/CA210WhiteBalance.msi Product.wixobj
```

### 使用Inno Setup (可选)

创建 `setup.iss` 文件:

```iss
[Setup]
AppName=CA210白平衡上位机软件
AppVersion=1.0.0
DefaultDirName={pf}\CA210WhiteBalance
DefaultGroupName=CA210白平衡上位机
OutputDir=setup\Output
OutputBaseFilename=CA210WhiteBalance-Setup

[Files]
Source: "src\CA210WhiteBalance.UI\bin\Release\net6.0-windows\*"; DestDir: "{app}"
Source: "lib\CA-SDK\CA200Srvr.dll"; DestDir: "{app}"

[Icons]
Name: "{group}\CA210白平衡上位机"; Filename: "{app}\CA210WhiteBalance.exe"
Name: "{commondesktop}\CA210白平衡上位机"; Filename: "{app}\CA210WhiteBalance.exe"

[Run]
Filename: "{app}\CA210WhiteBalance.exe"; Description: "启动应用程序"; Flags: postinstall nowait skipifsilent
```

编译:
```batch
iscc.exe setup.iss
```

## 发布检查清单

- [ ] 编译无错误
- [ ] CA-SDK COM引用已正确添加
- [ ] CA200Srvr.dll已包含在输出目录
- [ ] NLog.config已复制到输出目录
- [ ] appsettings.json已复制到输出目录
- [ ] 可执行文件可以在干净的Windows系统上运行
- [ ] 安装程序可以正确安装和卸载

## 运行时依赖

程序运行时需要以下文件位于程序目录:

- CA210WhiteBalance.exe
- CA200Srvr.dll (CA-SDK)
- NLog.dll
- OxyPlot.Wpf.dll
- 其他依赖DLL
- NLog.config
- appsettings.json

## 分发包结构

建议的分发包结构:

```
CA210WhiteBalance_v1.0/
├── CA210WhiteBalance-Setup.msi    # 安装程序
├── 使用说明书.pdf                   # 用户手册
├── CA-SDK驱动/
│   ├── CA200Srvr.dll
│   └── 驱动安装说明.txt
└── README.txt                       # 简要说明
```
