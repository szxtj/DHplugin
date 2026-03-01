# DHPlugin Ubuntu 部署完整指南

## 📦 已打包好的版本

### 位置
打包好的文件位于：
```
/Users/justinxie/Projects/DHplugin/dist/DHPlugin-Ubuntu-x64-20260301.tar.gz
```

文件大小：约 36MB

### 内容
```
ubuntu/
├── DHPlugin.Host              # 主程序
├── DHPlugin.Host.pdb          # 调试符号
├── DHPlugin.Core.pdb          # 核心库符号
├── DHPlugin.Abstractions.pdb  # 抽象层符号
├── plugins/
│   └── CPlusPlusEditor.dhplugin  # C++ 编辑器插件
├── run.sh                     # 启动脚本
└── README.txt                 # 说明文件
```

---

## 🚀 快速部署（3 步完成）

### 步骤 1：传输到 Ubuntu 系统

**方法 A: 使用 SCP**
```bash
# 在 macOS 上执行
scp dist/DHPlugin-Ubuntu-x64-*.tar.gz user@ubuntu-ip:~/
```

**方法 B: 使用 U 盘**
1. 将 `dist/DHPlugin-Ubuntu-x64-*.tar.gz` 复制到 U 盘
2. 在 Ubuntu 上挂载 U 盘并复制文件

**方法 C: 使用 HTTP 下载**
```bash
# 如果有 Web 服务器
wget http://your-server.com/DHPlugin-Ubuntu-x64-*.tar.gz
```

### 步骤 2：解压

```bash
# 在 Ubuntu 上执行
tar -xzf DHPlugin-Ubuntu-x64-*.tar.gz
cd ubuntu
```

### 步骤 3：运行

```bash
# 直接运行
./run.sh

# 或直接执行主程序
./DHPlugin.Host
```

---

## 📋 详细部署方式

### 方式 1：自包含发布（推荐）✅

**优点：**
- 无需安装 .NET
- 无需依赖系统库
- 开箱即用

**步骤：**
```bash
# 1. 解压
tar -xzf DHPlugin-Ubuntu-x64-*.tar.gz
cd ubuntu

# 2. 运行
./run.sh
```

### 方式 2：使用一键部署脚本

**步骤：**
```bash
# 1. 传输部署脚本到 Ubuntu
scp deploy-to-ubuntu.sh user@ubuntu-ip:~/

# 2. 在 Ubuntu 上执行
chmod +x deploy-to-ubuntu.sh
./deploy-to-ubuntu.sh
```

此脚本会：
- 自动检测系统架构
- 安装 .NET 8.0 SDK
- 安装 G++ 编译器
- 下载并构建项目
- 创建启动脚本

### 方式 3：手动安装 .NET 后运行

**步骤：**
```bash
# 1. 安装 .NET 8.0
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0

# 添加到 PATH
export PATH="$HOME/.dotnet:$PATH"

# 2. 安装依赖
sudo apt update
sudo apt install -y g++ libgtk-3-0

# 3. 克隆或传输代码
git clone <your-repo-url> DHPlugin
cd DHPlugin

# 4. 运行
dotnet run --project src/DHPlugin.Host/DHPlugin.Host.csproj
```

---

## 🖥️ 不同 Ubuntu 版本的部署

### Ubuntu 24.04 LTS (Noble)
```bash
# 完全兼容，直接使用打包版本
tar -xzf DHPlugin-Ubuntu-x64-*.tar.gz
cd ubuntu && ./run.sh
```

### Ubuntu 22.04 LTS (Jammy)
```bash
# 完全兼容
tar -xzf DHPlugin-Ubuntu-x64-*.tar.gz
cd ubuntu && ./run.sh
```

### Ubuntu 20.04 LTS (Focal)
```bash
# 完全兼容
tar -xzf DHPlugin-Ubuntu-x64-*.tar.gz
cd ubuntu && ./run.sh
```

### Ubuntu 18.04 LTS (Bionic)
```bash
# 需要更新 glibc 或使用兼容版本
# 检查 glibc 版本
ldd --version  # 需要 >= 2.17

# 如果版本过低，使用自包含发布
tar -xzf DHPlugin-Ubuntu-x64-*.tar.gz
cd ubuntu && ./run.sh
```

---

## 🔧 ARM64 架构部署（树莓派等）

### 树莓派 4/400/5 (64 位系统)

```bash
# 1. 打包 ARM64 版本（在 macOS 上）
cd /Users/justinxie/Projects/DHplugin
ARCH=arm64 ./package-ubuntu.sh

# 2. 传输到树莓派
scp dist/DHPlugin-Ubuntu-arm64-*.tar.gz pi@raspberrypi:~/

# 3. 在树莓派上解压运行
tar -xzf DHPlugin-Ubuntu-arm64-*.tar.gz
cd ubuntu && ./run.sh
```

### NVIDIA Jetson

```bash
# JetPack 系统已包含 CUDA 和 GCC
# 直接运行打包版本
tar -xzf DHPlugin-Ubuntu-arm64-*.tar.gz
cd ubuntu && ./run.sh
```

---

## 🛠️ 故障排除

### 问题 1: "Permission denied"

```bash
# 添加执行权限
chmod +x DHPlugin.Host
chmod +x run.sh
```

### 问题 2: "Cannot open display"

```bash
# 确保有 X11 显示服务器
echo $DISPLAY  # 应该显示 :0 或类似

# 如果是空的，设置它
export DISPLAY=:0

# 或使用 Wayland
export GDK_BACKEND=wayland
```

### 问题 3: 缺少 GTK 库

```bash
sudo apt install libgtk-3-0 libx11-6 libxrandr2 libxi6 libxcursor1 libxinerama1
```

### 问题 4: 找不到 .NET 运行时

```bash
# 使用自包含发布版本（推荐）
# 或安装 .NET
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0
export PATH="$HOME/.dotnet:$PATH"
```

### 问题 5: C++ 编译器未找到

```bash
# 安装 G++
sudo apt install g++

# 验证
g++ --version
```

---

## 📊 部署方式对比

| 方式 | 大小 | 依赖 | 启动速度 | 推荐场景 |
|------|------|------|---------|---------|
| 自包含发布 | 36MB | 无 | 快 | 生产环境 |
| .NET 安装 | 5MB | .NET 8 | 中 | 开发环境 |
| 源码构建 | 2MB | .NET 8+SDK | 慢 | 定制开发 |

---

## 📁 文件结构说明

```
ubuntu/
├── DHPlugin.Host          # 主程序可执行文件
├── *.pdb                  # 调试符号文件（可选）
├── plugins/               # 插件目录
│   └── CPlusPlusEditor.dhplugin
├── run.sh                 # 启动脚本
└── README.txt             # 说明文档
```

### 插件安装位置

将 `.dhplugin` 文件放入以下目录之一：

1. **启动时自动加载：**
   ```
   ~/.local/share/DHPlugin/Plugins/
   ```

2. **程序内安装：**
   - 运行程序
   - 点击 "➕ 安装插件"
   - 选择 `.dhplugin` 文件

---

## 🔐 安全说明

### 验证文件完整性

```bash
# 检查 SHA256
sha256sum DHPlugin-Ubuntu-x64-*.tar.gz

# 应该匹配（示例）
# abc123...  DHPlugin-Ubuntu-x64-20260301.tar.gz
```

### 防火墙设置

如果需要网络功能：
```bash
sudo ufw allow from any to any port 8080 proto tcp
```

---

## 📞 获取帮助

遇到问题？检查以下文件：

- `README.md` - 项目总览
- `PLATFORMS.md` - 跨平台支持详情
- `UBUNTU_DEPLOY.md` - 详细部署指南
- `QUICKSTART.md` - 快速入门

---

**DHPlugin Team** © 2026
