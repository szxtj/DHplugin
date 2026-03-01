#!/bin/bash

# DHPlugin Ubuntu 一键部署脚本
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
NC='\033[0m' # No Color

# 检测架构
ARCH=$(uname -m)
case $ARCH in
    x86_64) 
        ARCH_NAME="x64"
        echo -e "${GREEN}✓${NC} 检测到架构：x86_64 (x64)"
        ;;
    aarch64) 
        ARCH_NAME="arm64"
        echo -e "${GREEN}✓${NC} 检测到架构：ARM64"
        ;;
    *) 
        echo -e "${RED}✗${NC} 不支持的架构：$ARCH"
        exit 1
        ;;
esac

# 检测 Ubuntu 版本
if [ -f /etc/os-release ]; then
    . /etc/os-release
    echo -e "${GREEN}✓${NC} 检测到系统：$NAME $VERSION_ID"
else
    echo -e "${YELLOW}⚠${NC} 无法检测系统版本，继续执行..."
fi

# 检查网络
echo ""
echo "检查网络连接..."
if ! ping -c 1 -W 2 github.com > /dev/null 2>&1; then
    echo -e "${RED}✗${NC} 无法连接到互联网，请检查网络设置"
    exit 1
fi
echo -e "${GREEN}✓${NC} 网络连接正常"

# 安装依赖
echo ""
echo "正在安装依赖..."
sudo apt update -qq

# 检查是否已安装 .NET
if command -v dotnet &> /dev/null; then
    echo -e "${GREEN}✓${NC} .NET 已安装：$(dotnet --version)"
else
    echo "正在安装 .NET 8.0 SDK..."
    
    # 使用官方脚本安装
    wget -q https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
    chmod +x dotnet-install.sh
    ./dotnet-install.sh --channel 8.0 --install-dir ~/.dotnet
    
    # 添加到 PATH
    if ! grep -q "export PATH=\$HOME/.dotnet:\$PATH" ~/.bashrc; then
        echo 'export PATH=$HOME/.dotnet:$PATH' >> ~/.bashrc
    fi
    
    export PATH="$HOME/.dotnet:$PATH"
    rm dotnet-install.sh
    
    echo -e "${GREEN}✓${NC} .NET 8.0 安装完成：$(dotnet --version)"
fi

# 检查是否已安装 g++
if command -v g++ &> /dev/null; then
    echo -e "${GREEN}✓${NC} G++ 已安装"
else
    echo "正在安装 G++ (C++ 编译器)..."
    sudo apt install -y -qq g++
    echo -e "${GREEN}✓${NC} G++ 安装完成"
fi

# 创建项目目录
PROJECT_DIR="$HOME/DHPlugin"
echo ""
echo "创建项目目录：$PROJECT_DIR"
mkdir -p "$PROJECT_DIR"
cd "$PROJECT_DIR"

# 下载或更新代码
if [ -d "src" ]; then
    echo "检测到已有代码，跳过下载..."
else
    echo "正在下载项目代码..."
    # 这里可以替换为你的 Git 仓库地址
    # git clone <your-repo-url> .
    
    # 或者从压缩包解压
    # wget <your-package-url>/DHPlugin.tar.gz
    # tar -xzf DHPlugin.tar.gz
    
    echo "请将项目代码传输到此目录：$PROJECT_DIR"
    echo ""
fi

# 构建项目
echo ""
echo "正在构建项目..."
export PATH="$HOME/.dotnet:$PATH"
dotnet build src/DHPlugin.Host/DHPlugin.Host.csproj -c Release

# 创建启动脚本
cat > "$PROJECT_DIR/run.sh" << 'EOF'
#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
dotnet run --project src/DHPlugin.Host/DHPlugin.Host.csproj "$@"
EOF
chmod +x "$PROJECT_DIR/run.sh"

# 创建发布版本
echo ""
echo "正在创建发布版本..."
dotnet publish src/DHPlugin.Host/DHPlugin.Host.csproj \
    -c Release \
    --self-contained true \
    -p:PublishSingleFile=true \
    -o "$PROJECT_DIR/publish"

# 复制插件
mkdir -p "$PROJECT_DIR/publish/plugins"
if [ -f "src/Plugins/CPlusPlusEditor/CPlusPlusEditor.dhplugin" ]; then
    cp "src/Plugins/CPlusPlusEditor/CPlusPlusEditor.dhplugin" "$PROJECT_DIR/publish/plugins/"
fi

# 创建发布版启动脚本
cat > "$PROJECT_DIR/publish/run.sh" << EOF
#!/bin/bash
SCRIPT_DIR="\$(cd "\$(dirname "\${BASH_SOURCE[0]}")" && pwd)"
cd "\$SCRIPT_DIR"
export PATH="\$HOME/.dotnet:\$PATH"
export DOTNET_ROOT="\$HOME/.dotnet"
exec "\$SCRIPT_DIR/DHPlugin.Host" "\$@"
EOF
chmod +x "$PROJECT_DIR/publish/run.sh"

# 完成
echo ""
echo "========================================"
echo -e "${GREEN}  部署完成!${NC}"
echo "========================================"
echo ""
echo "项目目录：$PROJECT_DIR"
echo ""
echo "运行方法:"
echo "  开发模式：cd $PROJECT_DIR && ./run.sh"
echo "  发布版本：cd $PROJECT_DIR/publish && ./run.sh"
echo ""
echo "或者直接运行发布版本:"
echo "  $PROJECT_DIR/publish/DHPlugin.Host"
echo ""
echo "插件目录:"
echo "  $PROJECT_DIR/publish/plugins/"
echo "  将 .dhplugin 文件放入此目录自动加载"
echo ""
echo "========================================"
