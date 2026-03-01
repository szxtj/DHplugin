# DHPlugin Ubuntu 部署指南

## 方式一：使用预打包的 DEB 文件（推荐）

### 1. 下载部署包

首先在 macOS 上打包成 tar.gz 格式：

```bash
cd /Users/justinxie/Projects/DHplugin
chmod +x package-ubuntu.sh
./package-ubuntu.sh
```

这会在 `dist/` 目录创建 `DHPlugin-Ubuntu-*.tar.gz` 文件。

### 2. 传输到 Ubuntu 系统

```bash
# 使用 scp 传输
scp dist/DHPlugin-Ubuntu-*.tar.gz user@ubuntu-server:~/

# 或使用 U 盘等其他方式
```

### 3. 在 Ubuntu 上解压运行

```bash
# 解压
tar -xzf DHPlugin-Ubuntu-*.tar.gz

# 进入目录
cd DHPlugin-Ubuntu-*

# 运行
./run.sh
```

---

## 方式二：在 Ubuntu 上直接构建 DEB 包

将以下脚本传输到 Ubuntu 系统执行：

### 1. 安装依赖

```bash
sudo apt update
sudo apt install -y dotnet-sdk-8.0 dpkg-dev
```

### 2. 创建打包脚本

```bash
cat > build-deb.sh << 'SCRIPT'
#!/bin/bash
set -e

VERSION="1.0.0"
ARCH=$(dpkg --print-architecture)
DEB_ROOT="dhplugin_${VERSION}_${ARCH}"

# 创建目录结构
mkdir -p "$DEB_ROOT/DEBIAN"
mkdir -p "$DEB_ROOT/opt/dhplugin"
mkdir -p "$DEB_ROOT/usr/bin"
mkdir -p "$DEB_ROOT/usr/share/applications"
mkdir -p "$DEB_ROOT/usr/share/icons/hicolor/scalable/apps"

# 发布应用
dotnet publish src/DHPlugin.Host/DHPlugin.Host.csproj \
    -c Release \
    --self-contained true \
    -p:PublishSingleFile=true \
    -o "$DEB_ROOT/opt/dhplugin"

# 创建启动脚本
cat > "$DEB_ROOT/usr/bin/dhplugin" << 'EOF'
#!/bin/bash
exec /opt/dhplugin/DHPlugin.Host "$@"
EOF
chmod +x "$DEB_ROOT/usr/bin/dhplugin"

# 创建 control 文件
cat > "$DEB_ROOT/DEBIAN/control" << EOF
Package: dhplugin
Version: $VERSION
Section: utils
Priority: optional
Architecture: $ARCH
Maintainer: DHPlugin Team
Description: DHPlugin Cross-Platform Plugin Host
Depends: libgtk-3-0, libx11-6, libxrandr2, libxi6, libxcursor1, libxinerama1
EOF

# 创建 postinst
cat > "$DEB_ROOT/DEBIAN/postinst" << 'EOF'
#!/bin/bash
chmod +x /opt/dhplugin/DHPlugin.Host
mkdir -p ~/.local/share/DHPlugin/Plugins
exit 0
EOF
chmod +x "$DEB_ROOT/DEBIAN/postinst"

# 创建 .desktop 文件
cat > "$DEB_ROOT/usr/share/applications/dhplugin.desktop" << EOF
[Desktop Entry]
Name=DHPlugin Host
Comment=Cross-Platform Plugin Host
Exec=/opt/dhplugin/DHPlugin.Host
Terminal=false
Type=Application
Categories=Development;
EOF

# 构建 DEB
dpkg-deb --build "$DEB_ROOT"

echo "DEB 包已创建：${DEB_ROOT}.deb"
SCRIPT

chmod +x build-deb.sh
```

### 3. 执行打包

```bash
./build-deb.sh
```

### 4. 安装 DEB

```bash
sudo dpkg -i dhplugin_*.deb
```

### 5. 运行

```bash
dhplugin
# 或在应用程序菜单中找到 "DHPlugin Host"
```

---

## 方式三：直接运行（无需打包）

### 1. 在 Ubuntu 上安装 .NET SDK

```bash
# Ubuntu 20.04+
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt update
sudo apt install -y dotnet-sdk-8.0
```

### 2. 克隆或传输代码

```bash
# 克隆仓库
git clone <your-repo-url> DHplugin
cd DHplugin

# 或传输代码压缩包后解压
```

### 3. 构建并运行

```bash
dotnet run --project src/DHPlugin.Host/DHPlugin.Host.csproj
```

---

## 系统要求

### Ubuntu 版本支持

| Ubuntu 版本 | 代号 | 支持架构 | 状态 |
|------------|------|---------|------|
| 24.04 LTS | Noble | x64/ARM64 | ✅ 完全支持 |
| 22.04 LTS | Jammy | x64/ARM64 | ✅ 完全支持 |
| 20.04 LTS | Focal | x64/ARM64 | ✅ 完全支持 |
| 18.04 LTS | Bionic | x64/ARM64 | ✅ 支持 |

### 硬件要求

**x64 (amd64):**
- Intel/AMD 64 位处理器
- 2GB+ RAM
- 500MB+ 可用空间

**ARM64:**
- Raspberry Pi 4/400/5 (4GB+)
- NVIDIA Jetson Nano/Xavier/Orin
- AWS Graviton
- 其他 ARM64 开发板

---

## C++ 编辑器插件要求

如果需要使用 C++ 编辑器插件，安装编译器：

```bash
# Ubuntu/Debian
sudo apt install g++

# 验证安装
g++ --version
```

---

## 故障排除

### 问题 1: 无法启动 GUI

```bash
# 确保有 X11 或 Wayland 显示服务器
# 检查 DISPLAY 环境变量
echo $DISPLAY

# 如果是空的，设置它
export DISPLAY=:0
```

### 问题 2: 缺少 GTK 库

```bash
sudo apt install libgtk-3-0 libx11-6 libxrandr2 libxi6
```

### 问题 3: 权限问题

```bash
# 添加执行权限
chmod +x DHPlugin.Host
chmod +x run.sh
```

### 问题 4: ARM64 设备上找不到 .NET

```bash
# 使用自包含发布，不依赖系统 .NET
dotnet publish -r linux-arm64 --self-contained true
```

---

## 快速参考卡

```bash
# ================================
# 一键部署命令 (x64)
# ================================

# 1. 安装 .NET
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0

# 2. 安装编译器 (可选)
sudo apt install -y g++

# 3. 运行应用
dotnet run --project src/DHPlugin.Host/DHPlugin.Host.csproj
```

---

**DHPlugin Team** © 2026
