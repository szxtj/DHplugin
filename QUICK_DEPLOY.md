# 快速部署指南

## 📦 打包好的版本

### x64 版本 (Intel/AMD)
```
位置：/Users/justinxie/Projects/DHplugin/dist/DHPlugin-Ubuntu-x64-20260301.tar.gz
大小：36MB
```

### ARM64 版本 (树莓派/Jetson/Graviton)
```
位置：/Users/justinxie/Projects/DHplugin/dist/DHPlugin-Ubuntu-arm64-20260301.tar.gz
大小：35MB
```

---

## 🚀 3 步快速部署

### 步骤 1：传输到 Ubuntu

```bash
# 在 macOS 上执行
scp dist/DHPlugin-Ubuntu-*.tar.gz user@ubuntu-server:~/
```

### 步骤 2：在 Ubuntu 上运行一键安装脚本

```bash
# 传输安装脚本
scp install-on-ubuntu.sh user@ubuntu-server:~/

# 在 Ubuntu 上执行
chmod +x install-on-ubuntu.sh
./install-on-ubuntu.sh
```

### 步骤 3：运行程序

```bash
# 方法 1：命令行
~/dhplugin/run.sh

# 方法 2：桌面菜单
# 在应用程序菜单中查找 "DHPlugin Host"
```

---

## 📋 详细部署方式

### 方式 A：一键安装脚本（推荐）

**优点：**
- 自动检测架构
- 自动安装依赖
- 创建桌面快捷方式
- 配置插件目录

**步骤：**
```bash
# 1. 传输脚本
scp install-on-ubuntu.sh user@ubuntu:~/

# 2. 执行脚本
chmod +x install-on-ubuntu.sh
./install-on-ubuntu.sh
```

### 方式 B：手动解压

**步骤：**
```bash
# 1. 解压
tar -xzf DHPlugin-Ubuntu-*.tar.gz
cd DHPlugin-Ubuntu-*

# 2. 运行
./run.sh
```

### 方式 C：DEB 包安装（需在 Ubuntu 上构建）

**步骤：**
```bash
# 在 Ubuntu 上执行打包脚本
./package-ubuntu-deb.sh

# 安装 DEB
sudo dpkg -i dhplugin_*.deb

# 运行
dhplugin
```

---

## 🔌 插件安装

### 预装插件

打包版本已预装 **C++ 代码编辑器** 插件。

**使用方法：**
1. 运行程序
2. 在左侧选择 "C++ 代码编辑器"
3. 点击 "▶️ 启用"

### 安装新插件

**方法 1：程序内安装（推荐）**
```
1. 运行程序
2. 点击 "➕ 安装插件"
3. 选择 .dhplugin 文件
4. 启用插件
```

**方法 2：手动复制**
```bash
# Linux
cp MyPlugin.dhplugin ~/.local/share/DHPlugin/Plugins/

# 重启程序
~/dhplugin/run.sh
```

---

## 🖥️ 不同设备部署

### Ubuntu PC/笔记本 (x64)

```bash
# 使用 x64 版本
scp dist/DHPlugin-Ubuntu-x64-*.tar.gz user@pc:~/
scp install-on-ubuntu.sh user@pc:~/

# 在 PC 上执行
./install-on-ubuntu.sh
```

### 树莓派 4/400/5 (ARM64)

```bash
# 使用 ARM64 版本
scp dist/DHPlugin-Ubuntu-arm64-*.tar.gz pi@raspberrypi:~/
scp install-on-ubuntu.sh pi@raspberrypi:~/

# 在树莓派上执行
./install-on-ubuntu.sh
```

### NVIDIA Jetson (ARM64)

```bash
# 使用 ARM64 版本
scp dist/DHPlugin-Ubuntu-arm64-*.tar.gz user@jetson:~/

# 在 Jetson 上执行
./install-on-ubuntu.sh
```

### AWS Graviton (ARM64)

```bash
# 使用 ARM64 版本
scp dist/DHPlugin-Ubuntu-arm64-*.tar.gz ec2-user@instance:~/

# 在 EC2 上执行
./install-on-ubuntu.sh
```

---

## 🛠️ 故障排除

### 问题 1：找不到安装包

```bash
# 确保传输了正确的文件
ls -lh ~/DHPlugin-Ubuntu-*.tar.gz

# 如果没有，从 macOS 传输
scp dist/DHPlugin-Ubuntu-$(uname -m | sed 's/x86_64/x64/;s/aarch64/arm64/')-*.tar.gz user@ubuntu:~/
```

### 问题 2：无法显示 GUI

```bash
# 检查 DISPLAY
echo $DISPLAY

# 如果是空的
export DISPLAY=:0

# 或使用 X11 转发
ssh -X user@ubuntu
```

### 问题 3：缺少依赖

```bash
# 安装 GTK
sudo apt install libgtk-3-0 libx11-6 libxrandr2 libxi6

# 安装 G++ (C++ 编辑器需要)
sudo apt install g++
```

---

## 📁 文件结构

```
~/
├── dhplugin -> DHPlugin-Ubuntu-*/     # 符号链接
├── DHPlugin-Ubuntu-*/                  # 解压目录
│   ├── DHPlugin.Host                  # 主程序
│   ├── run.sh                         # 启动脚本
│   ├── plugins/
│   │   └── CPlusPlusEditor.dhplugin   # 预装插件
│   └── README.txt                     # 说明
└── .local/share/DHPlugin/Plugins/     # 插件目录
```

---

## 📞 获取帮助

- `README.md` - 项目总览
- `DEPLOY_UBUNTU.md` - Ubuntu 部署详解
- `PLUGIN_INSTALL.md` - 插件安装指南
- `PLATFORMS.md` - 跨平台支持

---

**DHPlugin Team** © 2026
