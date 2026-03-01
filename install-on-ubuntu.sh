#!/bin/bash

# DHPlugin Ubuntu 一键部署脚本 (含插件安装)
# 使用方法：在 Ubuntu 系统上运行此脚本

set -e

echo "========================================"
echo "  DHPlugin Ubuntu 一键部署"
echo "========================================"
echo ""

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 检测架构
ARCH=$(uname -m)
case $ARCH in
    x86_64) 
        ARCH_NAME="x64"
        ARCH_FILE="x64"
        echo -e "${GREEN}✓${NC} 检测到架构：x86_64 (x64)"
        ;;
    aarch64) 
        ARCH_NAME="arm64"
        ARCH_FILE="arm64"
        echo -e "${GREEN}✓${NC} 检测到架构：ARM64"
        ;;
    *) 
        echo -e "${RED}✗${NC} 不支持的架构：$ARCH"
        exit 1
        ;;
esac

echo ""
echo -e "${BLUE}📦 准备部署 DHPlugin${NC}"
echo ""

# 检查是否有打包文件
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# 查找打包文件（使用 find 而不是 glob）
PACKAGE_PATH=""
if ls "$SCRIPT_DIR"/DHPlugin-Ubuntu-${ARCH_FILE}-*.tar.gz 1> /dev/null 2>&1; then
    PACKAGE_PATH=$(ls "$SCRIPT_DIR"/DHPlugin-Ubuntu-${ARCH_FILE}-*.tar.gz | head -1)
    echo -e "${GREEN}✓${NC} 找到本地安装包：$PACKAGE_PATH"
elif ls "$HOME/Downloads"/DHPlugin-Ubuntu-${ARCH_FILE}-*.tar.gz 1> /dev/null 2>&1; then
    PACKAGE_PATH=$(ls "$HOME/Downloads"/DHPlugin-Ubuntu-${ARCH_FILE}-*.tar.gz | head -1)
    echo -e "${GREEN}✓${NC} 找到下载的安装包：$PACKAGE_PATH"
else
    echo -e "${RED}✗${NC} 未找到安装包 DHPlugin-Ubuntu-${ARCH_FILE}-*.tar.gz"
    echo ""
    echo "请先将打包文件传输到当前目录或 ~/Downloads/:"
    echo "  - DHPlugin-Ubuntu-${ARCH_FILE}-*.tar.gz"
    echo ""
    echo "传输方法:"
    echo "  # 在 macOS 上执行"
    echo "  scp /path/to/DHPlugin-Ubuntu-${ARCH_FILE}-*.tar.gz user@ubuntu:~/"
    echo ""
    exit 1
fi

# 解压
echo ""
echo "正在解压安装包..."
cd "$HOME"
tar -xzf "$PACKAGE_PATH"

# 获取解压后的目录名
INSTALL_DIR=$(tar -tzf "$PACKAGE_PATH" | head -1 | cut -f1 -d/)

# 重命名为固定名称
if [ -d "$HOME/DHPlugin" ]; then
    rm -rf "$HOME/DHPlugin"
fi
mv "$HOME/$INSTALL_DIR" "$HOME/DHPlugin"

echo -e "${GREEN}✓${NC} 解压完成：$HOME/DHPlugin"

# 创建符号链接（固定名称）
if [ -L "$HOME/dhplugin" ]; then
    rm "$HOME/dhplugin"
fi
ln -s "$HOME/DHPlugin" "$HOME/dhplugin"
echo -e "${GREEN}✓${NC} 创建快捷方式：~/dhplugin"

# 检查依赖
echo ""
echo "检查系统依赖..."

# 检查 glibc
GLIBC_VERSION=$(ldd --version | head -1 | awk '{print $NF}')
echo -e "${GREEN}✓${NC} glibc 版本：$GLIBC_VERSION"

# 检查 GTK
if ldconfig -p | grep -q libgtk-3; then
    echo -e "${GREEN}✓${NC} GTK3 已安装"
else
    echo -e "${YELLOW}⚠${NC} GTK3 未安装，正在安装..."
    sudo apt update -qq
    sudo apt install -y -qq libgtk-3-0 libx11-6 libxrandr2 libxi6
    echo -e "${GREEN}✓${NC} GTK3 安装完成"
fi

# 检查 G++
if command -v g++ &> /dev/null; then
    echo -e "${GREEN}✓${NC} G++ 已安装：$(g++ --version | head -1)"
else
    echo -e "${YELLOW}⚠${NC} G++ 未安装 (C++ 编辑器需要)"
    read -p "是否安装 G++? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        sudo apt install -y -qq g++
        echo -e "${GREEN}✓${NC} G++ 安装完成"
    fi
fi

# 创建桌面快捷方式
echo ""
echo "创建桌面快捷方式..."
cat > "$HOME/.local/share/applications/dhplugin.desktop" << EOF
[Desktop Entry]
Name=DHPlugin Host
Comment=Cross-Platform Plugin Host
Exec=$HOME/DHPlugin/DHPlugin.Host
Icon=utilities-terminal
Terminal=false
Type=Application
Categories=Development;
EOF
chmod +x "$HOME/.local/share/applications/dhplugin.desktop"
echo -e "${GREEN}✓${NC} 桌面快捷方式已创建 (在应用程序菜单中查找 \"DHPlugin Host\")"

# 完成
echo ""
echo "========================================"
echo -e "${GREEN}✓ 部署完成!${NC}"
echo "========================================"
echo ""
echo "安装目录：$HOME/DHPlugin"
echo "快捷方式：~/dhplugin"
echo ""
echo -e "${BLUE}运行方法:${NC}"
echo "  1. 命令行：$HOME/DHPlugin/run.sh"
echo "  2. 快捷方式：~/dhplugin/run.sh"
echo "  3. 桌面：在应用程序菜单中查找 \"DHPlugin Host\""
echo ""
echo -e "${BLUE}插件说明:${NC}"
echo "  - 已预装 C++ 代码编辑器插件"
echo "  - 启动后在左侧选择插件，点击\"▶️ 启用\""
echo "  - 安装新插件：点击\"➕ 安装插件\"按钮"
echo ""
echo -e "${BLUE}下一步:${NC}"
echo "  1. 运行程序：$HOME/$INSTALL_DIR/run.sh"
echo "  2. 点击\"▶️ 启用\"启用 C++ 编辑器"
echo "  3. 开始使用！"
echo ""
