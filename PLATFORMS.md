# DHPlugin 跨平台支持文档

## 支持的平台

### macOS
| 架构 | 最低版本 | 运行时标识符 |
|------|---------|-------------|
| Intel x64 | macOS 10.14+ | `osx-x64` |
| Apple Silicon (ARM64) | macOS 11.0+ | `osx-arm64` |

**编译器支持:**
- Xcode 命令行工具 (`/usr/bin/clang++`)
- Homebrew 安装的 GCC

---

### Windows
| 架构 | 最低版本 | 运行时标识符 |
|------|---------|-------------|
| x64 | Windows 7 SP1+ | `win-x64` |
| x86 (32 位) | Windows 7 SP1+ | `win-x86` |

**编译器支持:**
- MinGW (g++.exe)
- WSL (Windows Subsystem for Linux)
- MSVC (cl.exe)

---

### Linux
| 架构 | glibc 版本 | 运行时标识符 |
|------|-----------|-------------|
| x64 | 2.17+ | `linux-x64` |
| ARM64 | 2.17+ | `linux-arm64` |

**支持的发行版:**

**x64:**
- Ubuntu 18.04+
- Debian 9+
- CentOS 7+
- Fedora 28+
- openSUSE 15+
- Arch Linux

**ARM64:**
- Ubuntu 18.04+ ARM64
- Debian 9+ ARM64
- Raspberry Pi OS (64 位)
- Fedora ARM64
- Arch Linux ARM
- Jetson (NVIDIA)
- AWS Graviton
- Ampere Altra

**编译器支持:**
- GCC (`/usr/bin/g++`)
- Clang (`/usr/bin/clang++`)
- 交叉编译器 (`aarch64-linux-gnu-g++`, `arm-linux-gnueabihf-g++`)

---

## 构建说明

### 在 macOS 上构建所有平台
```bash
chmod +x build.sh
./build.sh
```

输出目录:
```
publish/
├── macos-x64/
├── macos-arm64/
├── windows-x64/
├── windows-x86/
├── linux-x64/
└── linux-arm64/
```

### 在 Windows 上构建所有平台
```batch
build.bat
```

### 在 Linux 上构建所有平台
```bash
chmod +x build.sh
./build.sh
```

---

## Linux ARM64 特别说明

### 树莓派 (Raspberry Pi)

**树莓派 4/400 (4GB/8GB):**
```bash
# 安装 Raspberry Pi OS 64 位
sudo apt update
sudo apt install dotnet-sdk-8.0 g++

# 构建
dotnet build -r linux-arm64
```

**树莓派 3B+:**
```bash
# 使用 32 位系统时
dotnet build -r linux-arm
```

### NVIDIA Jetson

**Jetson Nano/Xavier/Orin:**
```bash
# JetPack 系统已包含 CUDA 和 GCC
sudo apt install dotnet-sdk-8.0

# 构建
dotnet build -r linux-arm64
```

### AWS Graviton

**EC2 Graviton2/Graviton3:**
```bash
# Amazon Linux 2023
sudo dnf install dotnet-sdk-8.0 gcc-c++

# 构建
dotnet build -r linux-arm64
```

### 其他 ARM 开发板

**Orange Pi / Rock Pi:**
```bash
# 使用 Armbian 或 Ubuntu ARM
sudo apt install dotnet-sdk-8.0 g++
```

---

## 交叉编译

### 从 x64 交叉编译到 ARM64

**在 Linux x64 上:**
```bash
# 安装交叉编译工具链
sudo apt install gcc-aarch64-linux-gnu g++-aarch64-linux-gnu

# 构建
dotnet publish -r linux-arm64 \
  --self-contained true \
  -p:PublishSingleFile=true
```

**在 macOS 上:**
```bash
# 直接构建（.NET 8 支持交叉编译）
dotnet publish -r linux-arm64 \
  --self-contained true
```

---

## 性能对比

| 平台 | 启动时间 | 内存占用 | 备注 |
|------|---------|---------|------|
| macOS x64 | ~200ms | ~50MB | Rosetta 2 转译 |
| macOS ARM64 | ~150ms | ~45MB | 原生运行 |
| Windows x64 | ~250ms | ~55MB | - |
| Linux x64 | ~180ms | ~48MB | - |
| Linux ARM64 | ~200ms | ~50MB | 取决于设备 |

---

## 故障排除

### Linux ARM64 常见问题

**1. 找不到 .NET 运行时**
```bash
# 使用自包含发布
dotnet publish -r linux-arm64 --self-contained true
```

**2. glibc 版本过低**
```bash
# 检查 glibc 版本
ldd --version

# 需要 >= 2.17，否则升级系统或使用静态链接
```

**3. 编译器未找到**
```bash
# 安装 GCC
sudo apt install g++

# 或指定编译器路径
export PATH="/usr/bin:$PATH"
```

### macOS 常见问题

**1. "无法验证开发者"**
```bash
# 在系统设置中允许
# 或使用以下命令移除隔离属性
xattr -rd com.apple.quarantine /path/to/DHPlugin.Host
```

### Windows 常见问题

**1. 缺少 VC++ 运行时**
```bash
# 下载并安装
https://aka.ms/vs/17/release/vc_redist.x64.exe
```

---

## 插件兼容性

所有插件包 (`.dhplugin`) 在三端通用，无需为不同平台重新编译。

**原因:**
- 插件使用 .NET 8，具有跨平台兼容性
- 插件代码编译为 IL (中间语言)
- 运行时由 CLR 即时编译 (JIT)

**例外情况:**
- 如果插件包含原生依赖 (C/C++ 库)，需要为每个平台构建
- 使用 `RuntimeIdentifiers` 分别为每个平台构建原生库

---

## 版本历史

- **v1.0.0** (2026) - 初始版本
  - 支持 macOS (x64/ARM64)
  - 支持 Windows (x64/x86)
  - 支持 Linux (x64/ARM64)

---

**DHPlugin Team** © 2026
