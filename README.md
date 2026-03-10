# DHPlugin

DHPlugin 是一个基于 Avalonia UI 的插件式宿主程序。当前仓库已经统一为新的打包体系：

- 主程序安装包和插件安装包独立发布。
- 只支持 `win-x64`、`osx-arm64`、`linux-x64`、`linux-arm64`。
- 所有开发者和用户文档已归并到本 README。

## 功能概览

- 空壳主程序，插件决定功能。
- 插件安装、启用、禁用、卸载。
- 插件包格式为 `.dhplugin`。
- 示例插件 `CPlusPlusEditor` 提供代码编辑、编译和运行功能。
- 宿主程序内置 **Zig 编译器服务**（`ICompilerService`），为插件的 C++ 代码编译提供跨平台底层支持。

## 平台与产物格式

| 平台 | 架构 | RID | 主程序包格式 |
|---|---|---|---|
| Windows | x64 | `win-x64` | `.zip` |
| macOS | ARM64 | `osx-arm64` | `.dmg` |
| Linux | x64 | `linux-x64` | `.tar.gz` |
| Linux | ARM64 | `linux-arm64` | `.tar.gz` |

标识映射说明：

- Linux `uname -m` 的 `x86_64` 对应 x64。
- Linux `uname -m` 的 `aarch64` 对应 ARM64。

## 项目结构

```text
DHplugin/
├── src/
│   ├── DHPlugin.Host/
│   ├── DHPlugin.Core/
│   ├── DHPlugin.Abstractions/
│   ├── DHPlugin.Sdk/
│   └── Plugins/
│       └── CPlusPlusEditor/
├── pack-plugin.sh
├── pack-plugin.bat
├── pack-host.sh
├── pack-host.bat
├── pack-all.sh
├── pack-all.bat
└── README.md
```

## 快速开始（开发模式）

在仓库根目录运行主程序：

```bash
dotnet run
```

说明：根目录新增了 `DHPlugin.Runner.csproj`，用于将入口转发到 `src/DHPlugin.Host`。

## 统一打包流程

### 1. 打插件安装包

Unix:

```bash
./pack-plugin.sh
```

Windows:

```bat
pack-plugin.bat
```

行为：

- 扫描 `src/Plugins/*/*.csproj`。
- 使用 `Release` 构建插件项目。
- 收集 `.dhplugin` 到 `dist/plugins/`。

### 2. 打主程序安装包

Unix:

```bash
./pack-host.sh
```

Windows:

```bat
pack-host.bat
```

行为：

- 发布四个平台主程序。
- 输出到 `dist/host/<platform>/`。
- 主程序包不附带插件包。

### 3. 一次性全量打包

Unix:

```bash
./pack-all.sh
```

Windows:

```bat
pack-all.bat
```

## 产物目录

```text
dist/
├── plugins/
│   └── *.dhplugin
└── host/
    ├── windows-x64/
    │   └── *.zip
    ├── macos-arm64/
    │   └── *.dmg
    ├── linux-x64/
    │   └── *.tar.gz
    └── linux-arm64/
        └── *.tar.gz
```

## 插件包生成机制

以 `src/Plugins/CPlusPlusEditor` 为例：

- `.dhplugin` 由 `src/Plugins/CPlusPlusEditor/CPlusPlusEditor.csproj` 中的 `PackPlugin` 目标生成。
- 触发条件是 `Release` 构建。

稳定命令：

```bash
dotnet build src/Plugins/CPlusPlusEditor/CPlusPlusEditor.csproj -c Release
```

产物：

- 源项目目录：`src/Plugins/CPlusPlusEditor/CPlusPlusEditor.dhplugin`
- 统一发布目录：`dist/plugins/CPlusPlusEditor.dhplugin`

是否全平台通用：

- 当前插件包是一份全平台通用（托管代码）。
- 如果插件未来引入原生库（`.so/.dylib/.dll`），则需按平台分包。

## Zig 编译器服务（ICompilerService）

宿主程序在启动时自动注册 `ZigCompilerService`，插件可通过插件上下文获取该服务来编译 C++ 代码：

```csharp
var compilerService = context.GetService<ICompilerService>();
if (compilerService != null && compilerService.IsAvailable)
{
    var result = await compilerService.CompileCppAsync(sourceFile, outputFile);
}
```

### 设计说明

| 组件 | 位置 | 作用 |
|---|---|---|
| `ICompilerService` | `DHPlugin.Abstractions` | 跨平台 C++ 编译器抽象接口，供插件调用 |
| `ZigCompilerService` | `DHPlugin.Core` | 基于 `zig c++` 的实现，支持全部4个目标平台 |
| 注册点 | `DHPlugin.Host/App.axaml.cs` | 宿主启动时自动注册，插件无需关心实现细节 |

### 为什么选择 Zig

- Zig 内置了完整的 C/C++ 工具链（基于 LLVM/Clang），无需单独安装 GCC。
- `zig c++` 命令天然支持 `win-x64`、`osx-arm64`、`linux-x64`、`linux-arm64` 四个平台。
- 同一个 `zig` 二进制可以交叉编译到任意目标，便于跨平台构建场景。

### 安装 Zig

从 [https://ziglang.org/download/](https://ziglang.org/download/) 下载对应平台的预编译包，解压后将 `zig`（Windows 为 `zig.exe`）所在目录加入系统 `PATH` 即可。

常见安装路径（宿主程序会自动探测）：

| 平台 | 自动探测路径（示例） |
|---|---|
| Windows | `%LOCALAPPDATA%\zig\zig.exe`、`C:\zig\zig.exe` |
| macOS | `/opt/homebrew/bin/zig`、`/usr/local/bin/zig` |
| Linux | `~/.local/bin/zig`、`/usr/local/bin/zig` |

若 Zig 不可用，`CPlusPlusEditor` 插件会自动回退到系统 `g++` / `clang++`。

## 插件安装与管理

### 推荐方式：程序内安装

1. 启动主程序。
2. 点击 `Install Plugin`。
3. 选择 `dist/plugins/*.dhplugin`。
4. 在插件列表中启用。

### 手动安装路径

| 系统 | 路径 |
|---|---|
| Linux | `~/.local/share/DHPlugin/Plugins/` |
| macOS | `~/Library/Application Support/DHPlugin/Plugins/` |
| Windows | `%APPDATA%\DHPlugin\Plugins\` |

手动安装后建议重启主程序。

### 卸载与清理

- UI 中选择插件后执行卸载。
- 或手动删除插件目录和同名 `.dhplugin` 文件。

Linux 示例：

```bash
rm -rf ~/.local/share/DHPlugin/Plugins/CPlusPlusEditor/
rm -f ~/.local/share/DHPlugin/Plugins/CPlusPlusEditor.dhplugin
```

## Ubuntu / Linux 部署

### x64 部署

在构建机：

```bash
./pack-host.sh
scp dist/host/linux-x64/*.tar.gz user@ubuntu:~/
```

在目标机：

```bash
tar -xzf DHPlugin-Host-linux-x64-*.tar.gz
cd DHPlugin-Host-linux-x64-*
./run.sh
```

### ARM64 部署

在构建机：

```bash
./pack-host.sh
scp dist/host/linux-arm64/*.tar.gz user@ubuntu-arm:~/
```

在目标机：

```bash
tar -xzf DHPlugin-Host-linux-arm64-*.tar.gz
cd DHPlugin-Host-linux-arm64-*
./run.sh
```

### Linux 依赖

宿主程序是 self-contained，但 GUI 依赖仍需系统库：

```bash
sudo apt update
sudo apt install -y libgtk-3-0 libx11-6 libxrandr2 libxi6
```

若使用 C++ 编辑器插件：

推荐安装 Zig（提供内置 C++ 工具链，无需额外依赖）：

```bash
# 从 https://ziglang.org/download/ 下载对应平台二进制，解压后将 zig 加入 PATH
# 或使用包管理器（如 snap）：
sudo snap install zig --classic --beta
```

也可以继续使用系统 GCC（Zig 不可用时自动回退）：

```bash
sudo apt install -y g++
```

## 常见问题

### 1. 插件安装后看不到

- 检查插件是否安装到正确路径。
- 重启宿主程序。
- 确认插件在 UI 中已启用。

### 2. Linux 启动权限问题

```bash
chmod +x DHPlugin.Host run.sh
```

### 3. Linux 图形环境报错

```bash
echo "$DISPLAY"
export DISPLAY=:0
```

### 4. macOS 无法产出 DMG

- `pack-host.sh` 使用 `hdiutil`，需要在 macOS 运行。

## 可配置参数

脚本支持环境变量：

- `CONFIGURATION`，默认 `Release`。
- `VERSION`，默认当天日期（`YYYYMMDD`）。
- `OUTPUT_ROOT`，默认 `dist`。
- `CLEAN`，默认 `1`。

示例：

```bash
VERSION=1.0.0 OUTPUT_ROOT=dist ./pack-all.sh
```

## 许可证

MIT License
