#!/usr/bin/env bash

set -e

echo "========================================"
echo "  DHPlugin Ubuntu 打包脚本"
echo "========================================"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# 配置
CONFIGURATION="${CONFIGURATION:-Release}"
OUTPUT_DIR="$SCRIPT_DIR/dist/ubuntu"
ARCH="${ARCH:-x64}"  # x64 或 arm64

# 检测架构
if [ "$ARCH" == "auto" ]; then
    ARCH=$(uname -m)
    case $ARCH in
        x86_64) ARCH="x64" ;;
        aarch64) ARCH="arm64" ;;
        *) echo "不支持的架构：$ARCH"; exit 1 ;;
    esac
fi

RUNTIME="linux-$ARCH"

echo "架构：$ARCH"
echo "运行时：$RUNTIME"
echo ""

# 清理旧输出
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# 发布应用
echo "正在发布应用..."
dotnet publish src/DHPlugin.Host/DHPlugin.Host.csproj \
    -c "$CONFIGURATION" \
    -r "$RUNTIME" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o "$OUTPUT_DIR"

# 复制插件包
echo "复制插件包..."
mkdir -p "$OUTPUT_DIR/plugins"
if [ -f "src/Plugins/CPlusPlusEditor/CPlusPlusEditor.dhplugin" ]; then
    cp "src/Plugins/CPlusPlusEditor/CPlusPlusEditor.dhplugin" "$OUTPUT_DIR/plugins/"
fi

# 创建启动脚本
echo "创建启动脚本..."
cat > "$OUTPUT_DIR/run.sh" << 'EOF'
#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"
export DOTNET_ROOT="$SCRIPT_DIR"
exec "$SCRIPT_DIR/DHPlugin.Host" "$@"
EOF
chmod +x "$OUTPUT_DIR/run.sh"

# 创建 README
cat > "$OUTPUT_DIR/README.txt" << EOF
DHPlugin for Ubuntu ($ARCH)
================================

运行方法:
  ./run.sh

或者直接运行:
  ./DHPlugin.Host

依赖:
  - 无 (自包含发布，已包含 .NET 运行时)

插件目录:
  ./plugins/
  将 .dhplugin 文件放入此目录，启动时会自动加载

系统要求:
  - Ubuntu 18.04+ (或其他 glibc 2.17+ 的 Linux 发行版)
  - $ARCH 架构处理器
  - X11 或 Wayland 显示服务器

C++ 编辑器插件要求:
  sudo apt install g++

EOF

# 创建安装包
PACKAGE_NAME="DHPlugin-Ubuntu-${ARCH}-$(date +%Y%m%d)"
PACKAGE_PATH="$OUTPUT_DIR/../${PACKAGE_NAME}.tar.gz"

echo "创建安装包..."
cd "$OUTPUT_DIR/.."
tar -czf "${PACKAGE_NAME}.tar.gz" "$(basename $OUTPUT_DIR)"

echo ""
echo "========================================"
echo "  打包完成!"
echo "========================================"
echo ""
echo "输出目录：$OUTPUT_DIR"
echo "安装包：$PACKAGE_PATH"
echo ""
echo "部署步骤:"
echo "  1. 将 ${PACKAGE_NAME}.tar.gz 传输到 Ubuntu 系统"
echo "  2. 解压：tar -xzf ${PACKAGE_NAME}.tar.gz"
echo "  3. 进入目录：cd ${PACKAGE_NAME}"
echo "  4. 运行：./run.sh"
echo ""
