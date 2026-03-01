# DHPlugin - 跨平台插件系统

基于 Avalonia UI 构建的高度解耦的跨平台插件系统，支持 macOS、Windows (最低 Windows 7) 和主流 Linux 发行版。

## 特性

- 🧩 **高度解耦** - 未安装任何插件时，主程序是一个空壳
- 📦 **插件管理** - 支持插件的安装、卸载、启用、禁用
- 🔌 **热插拔** - 插件可动态加载和卸载
- 🌍 **跨平台** - 一套代码，三端通用 (macOS/Windows/Linux)
- 🎨 **现代 UI** - 基于 Avalonia UI，支持 Fluent 设计
- 📝 **SDK 支持** - 提供插件开发 SDK 和模板

## 系统要求

### 运行时要求
- **.NET 8.0 Runtime** (已包含在自包含发布中)
- **操作系统**:
  - **macOS**: 10.14+ (Intel x64 / Apple Silicon ARM64)
  - **Windows**: 7 SP1+ (x64 / x86)
  - **Linux**: 
    - x64: glibc 2.17+ (Ubuntu 18.04+, CentOS 7+, Debian 9+)
    - ARM64: glibc 2.17+ (树莓派 4+, Jetson, ARM 服务器)

### 开发要求
- .NET 8.0 SDK
- IDE: Visual Studio 2022 / Rider / VS Code

## 项目结构

```
DHplugin/
├── src/
│   ├── DHPlugin.Host/          # 主程序 (空壳应用)
│   ├── DHPlugin.Core/          # 插件管理核心
│   ├── DHPlugin.Abstractions/  # 插件抽象接口
│   ├── DHPlugin.Sdk/           # 插件开发 SDK
│   └── Plugins/
│       └── CPlusPlusEditor/    # 示例插件：C++ 编辑器
├── build.sh                    # macOS/Linux 构建脚本
├── build.bat                   # Windows 构建脚本
└── DHPlugin.sln                # 解决方案文件
```

## 快速开始

### 构建

**macOS / Linux:**
```bash
chmod +x build.sh
./build.sh
```

**Windows:**
```batch
build.bat
```

> 💡 **注意**: 构建脚本会在**当前系统**上交叉编译所有平台的版本
> - 在 macOS 上可以构建 Windows 和 Linux 版本
> - 在 Windows 上可以构建 macOS 和 Linux 版本
> - 在 Linux 上可以构建 macOS 和 Windows 版本

### 运行

构建完成后，在 `publish` 目录找到对应平台的可执行文件：

| 平台 | 架构 | 路径 | 说明 |
|------|------|------|------|
| macOS | Intel (x64) | `publish/macos-x64/DHPlugin.Host` | Intel Mac |
| macOS | Apple Silicon (ARM64) | `publish/macos-arm64/DHPlugin.Host` | M1/M2/M3 Mac |
| Linux | x64 | `publish/linux-x64/DHPlugin.Host` | 主流发行版 |
| Linux | ARM64 | `publish/linux-arm64/DHPlugin.Host` | 树莓派/ARM 服务器 |
| Windows | x64 | `publish\windows-x64\DHPlugin.Host.exe` | Win7+ 64 位 |
| Windows | x86 | `publish\windows-x86\DHPlugin.Host.exe` | Win7+ 32 位 |

## 插件开发

### 创建插件

1. 复制 `publish/sdk/templates/SimplePlugin` 目录
2. 修改 `manifest.json` 配置
3. 实现 `IPlugin` 接口
4. 运行 `dotnet build` 生成 `.dhplugin` 包

### manifest.json 格式

```json
{
  "id": "MyPlugin",              // 插件唯一标识
  "name": "我的插件",             // 显示名称
  "version": "1.0.0",            // 版本号
  "description": "插件描述",      // 详细描述
  "author": "作者名",            // 作者
  "iconPath": "icon.png",        // 图标路径 (可选)
  "minimumHostVersion": "1.0.0", // 最低主机版本
  "dependencies": []             // 依赖插件 ID 列表
}
```

### IPlugin 接口

```csharp
public interface IPlugin
{
    PluginManifest Manifest { get; }
    PluginState State { get; }
    
    Task InitializeAsync(IPluginContext context, CancellationToken cancellationToken = default);
    Task EnableAsync(CancellationToken cancellationToken = default);
    Task DisableAsync(CancellationToken cancellationToken = default);
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}
```

### 插件上下文 (IPluginContext)

```csharp
public interface IPluginContext
{
    string HostVersion { get; }
    T? GetService<T>() where T : class;
    void RegisterService<T>(T service) where T : class;
    string GetDataDirectory();
    string GetConfigDirectory();
    ILogger GetLogger();
    void RegisterUiContribution(UiContribution contribution);
    IPlugin? GetPlugin(string pluginId);
    void PublishEvent(string eventName, object? data = null);
    void SubscribeEvent(string eventName, Action<string, object?> handler);
}
```

## 示例插件：C++ 代码编辑器

功能特性:
- 📝 C++ 代码编辑
- 🔨 调用系统 GCC/Clang 编译器
- ▶️ 一键编译运行
- 📋 输出查看

### 使用要求

需要系统安装 C++ 编译器:

**macOS:**
```bash
xcode-select --install
```

**Ubuntu/Debian (x64/ARM64):**
```bash
sudo apt install g++
```

**CentOS/RHEL/Fedora:**
```bash
sudo yum install gcc-c++
# 或
sudo dnf install gcc-c++
```

**Linux ARM64 (树莓派/开发板):**
```bash
# 树莓派/Debian ARM
sudo apt install g++

# 使用交叉编译器
sudo apt install gcc-aarch64-linux-gnu
sudo apt install gcc-arm-linux-gnueabihf
```

**Windows:**
- 安装 MinGW 或 WSL
- 或使用 MSVC 编译器

## 插件包格式

插件包是 `.dhplugin` 格式的 ZIP 文件，包含:

```
MyPlugin.dhplugin/
├── manifest.json           # 插件元数据
├── MyPlugin.dll           # 插件程序集
├── MyPlugin.pdb           # 调试符号 (可选)
└── assets/                # 资源文件 (可选)
```

## API 参考

### UI 贡献点

```csharp
public static class UiContributionPoints
{
    public const string MainMenu = "mainMenu";      // 主菜单
    public const string MainContent = "mainContent"; // 主内容区
    public const string ToolBar = "toolBar";        // 工具栏
    public const string StatusBar = "statusBar";    // 状态栏
    public const string SideBar = "sideBar";        // 侧边栏
    public const string SettingsPage = "settingsPage"; // 设置页
}
```

### 插件状态

```csharp
public enum PluginState
{
    Unloaded,   // 未加载
    Loaded,     // 已加载
    Enabled,    // 已启用
    Disabled,   // 已禁用
    Error       // 错误
}
```

## 许可证

MIT License

## 贡献

欢迎提交 Issue 和 Pull Request!

---

**DHPlugin Team** © 2026
