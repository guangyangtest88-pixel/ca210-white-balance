# 在macOS上编译Windows .NET程序指南

## 方法概述

由于项目使用了WPF（Windows-only）和CA-SDK COM组件，有几种方案：

### 方案对比

| 方案 | 难度 | 推荐度 | 说明 |
|------|------|--------|------|
| A. 在macOS上编译命令行版 | 中 | ⭐⭐⭐ | 只编译核心功能，不包含WPF界面 |
| B. 使用GitHub Actions自动编译 | 低 | ⭐⭐⭐⭐⭐ | 最简单，全自动 |
| C. 使用虚拟机运行Windows | 中 | ⭐⭐⭐ | 在Mac上运行Windows虚拟机 |
| D. 使用远程Windows编译 | 低 | ⭐⭐⭐⭐ | 使用GitHub Codespaces等 |

---

## 推荐方案：使用GitHub Actions自动编译

### 优点
- 完全免费
- 自动化
- 不需要在本地安装.NET
- 可以直接下载编译好的exe

### 操作步骤

#### 1. 在GitHub上创建仓库

```bash
# 初始化git仓库（如果还没有）
cd /Users/wangguangyang/Desktop/AI\ code/CA210上位机软件/CA210WhiteBalance
git init
git add .
git commit -m "Initial commit"

# 创建GitHub仓库后，推送代码
# （假设你的GitHub用户名是yourusername，仓库名是ca210-white-balance）
git remote add origin https://github.com/yourusername/ca210-white-balance.git
git branch -M main
git push -u origin main
```

#### 2. 创建GitHub Actions工作流

创建文件 `.github/workflows/build.yml`：

```yaml
name: Build Windows Application

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]
  workflow_dispatch:

jobs:
  build:
    runs-on: windows-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '6.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build solution
      run: dotnet build --configuration Release --no-restore

    - name: Publish as single file
      run: |
        dotnet publish src/CA210WhiteBalance.UI/CA210WhiteBalance.UI.csproj ^
          --configuration Release ^
          --runtime win-x64 ^
          --self-contained true ^
          -p:PublishSingleFile=true ^
          -p:IncludeNativeLibrariesForSelfExtract=true ^
          --output publish

    - name: Upload build artifacts
      uses: actions/upload-artifact@v4
      with:
        name: CA210WhiteBalance-win-x64
        path: publish/CA210WhiteBalance.exe
        retention-days: 30

    - name: Create Release Zip
      run: |
        cd publish
        7z a ../CA210WhiteBalance-win-x64.zip .
        cd ..

    - name: Upload Zip Artifact
      uses: actions/upload-artifact@v4
      with:
        name: CA210WhiteBalance-win-x64-zip
        path: CA210WhiteBalance-win-x64.zip
        retention-days: 30
```

#### 3. 提交并推送

```bash
git add .github/workflows/build.yml
git commit -m "Add GitHub Actions build workflow"
git push
```

#### 4. 下载编译好的exe

1. 访问你的GitHub仓库
2. 点击 "Actions" 标签
3. 选择最新的工作流运行
4. 在 "Artifacts" 部分下载 `CA210WhiteBalance-win-x64-zip`

---

## 备选方案A: 在macOS本地编译（命令行版本）

### 限制

由于WPF是Windows-only，需要创建一个不依赖WPF的控制台版本。

### 步骤

#### 1. 安装.NET SDK

```bash
brew install --cask dotnet-sdk
```

#### 2. 测试编译核心库

```bash
cd /Users/wangguangyang/Desktop/AI\ code/CA210上位机软件/CA210WhiteBalance

# 只编译Core库（不包含WPF）
dotnet build src/CA210WhiteBalance.Core/CA210WhiteBalance.Core.csproj

# 如果COM引用导致错误，需要注释掉COM相关代码
```

#### 3. 修改Core项目以支持跨平台

编辑 `CA210WhiteBalance.Core.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- 支持多平台 -->
    <TargetFrameworks>net6.0;net6.0-windows</TargetFrameworks>
    ...
  </PropertyGroup>

  <!-- Windows特定的包只在Windows平台引用 -->
  <ItemGroup Condition="$([MSBuild]::IsOSPlatform('Windows'))">
    <COMReference Include="CA200Srvr">...</COMReference>
  </ItemGroup>
</Project>
```

#### 4. 发布Windows版本

```bash
# 交叉编译为Windows可执行文件
dotnet publish src/CA210WhiteBalance.Core/CA210WhiteBalance.Core.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true
```

---

## 备选方案C: 使用Windows虚拟机

### 使用Parallels Desktop（推荐）

```bash
# 1. 安装Parallels Desktop
brew install --cask parallels-desktop

# 2. 下载Windows 11 ARM版（Apple Silicon Mac）或 x64版（Intel Mac）
# https://www.microsoft.com/software-download/windows11

# 3. 在Parallels中安装Windows

# 4. 在Windows虚拟机中安装.NET SDK
# winget install Microsoft.DotNet.SDK.6

# 5. 将项目文件夹共享给虚拟机

# 6. 在虚拟机中编译
cd Z:\CA210WhiteBalance
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained
```

### 使用VMware Fusion

```bash
brew install --cask vmware-fusion
```

---

## 备选方案D: 使用GitHub Codespaces（云端Windows）

```bash
# 1. 访问你的GitHub仓库
# 2. 点击 "Code" -> "Codespaces" -> "Create codespace on main"
# 3. 选择Windows镜像（如果可用）或在Linux中使用.NET交叉编译

# 在Codespace中
dotnet publish -c Release -r win-x64 --self-contained

# 下载编译好的文件
```

---

## 快速开始建议

**最简单的方法**：使用GitHub Actions

1. 创建GitHub仓库并推送代码
2. 添加 `.github/workflows/build.yml`
3. 推送后自动编译
4. 从Actions页面下载exe

**最灵活的方法**：使用Parallels Desktop + Windows 11

- 可以完整测试WPF界面
- 可以调试COM组件
- 一次性配置，长期使用

---

## 常见问题

### Q: 为什么不能用`dotnet build`直接编译？

A: 项目使用了：
- `net6.0-windows` 目标框架（WPF需要）
- COM引用（CA200Srvr.dll是Windows COM组件）
- System.IO.Ports在非Windows平台行为不同

### Q: 必须要在Windows上编译吗？

A:
- 要编译**完整版本**（包含WPF界面）：**是**
- 只编译**核心库**：可以跨平台编译
- 使用**GitHub Actions**：自动化，不需要本地Windows

### Q: 编译后的exe可以在没有.NET的电脑上运行吗？

A: 使用 `--self-contained true` 参数发布的exe包含.NET运行时，可以在没有安装.NET的Windows电脑上运行。
