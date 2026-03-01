#!/usr/bin/env bash

set -e

echo "========================================"
echo "  DHPlugin Ubuntu DEB 打包脚本"
echo "========================================"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# 配置
CONFIGURATION="${CONFIGURATION:-Release}"
ARCH="${ARCH:-amd64}"  # amd64 或 arm64
VERSION="${VERSION:-1.0.0}"

# 检测架构
if [ "$ARCH" == "auto" ]; then
    ARCH=$(uname -m)
    case $ARCH in
        x86_64) ARCH="amd64" ;;
        aarch64) ARCH="arm64" ;;
        *) echo "不支持的架构：$ARCH"; exit 1 ;;
    esac
fi

RUNTIME="linux-${ARCH/-/}"
if [ "$ARCH" == "amd64" ]; then
    RUNTIME="linux-x64"
elif [ "$ARCH" == "arm64" ]; then
    RUNTIME="linux-arm64"
fi

DEB_DIR="$SCRIPT_DIR/dist/deb"
PACKAGE_NAME="dhplugin"
DEB_ROOT="$DEB_DIR/${PACKAGE_NAME}_${VERSION}_${ARCH}"

echo "架构：$ARCH"
echo "运行时：$RUNTIME"
echo "版本：$VERSION"
echo ""

# 清理
rm -rf "$DEB_ROOT"
mkdir -p "$DEB_ROOT/DEBIAN"
mkdir -p "$DEB_ROOT/opt/dhplugin"
mkdir -p "$DEB_ROOT/usr/bin"
mkdir -p "$DEB_ROOT/usr/share/applications"
mkdir -p "$DEB_ROOT/usr/share/icons/hicolor/scalable/apps"

# 发布应用
echo "正在发布应用..."
dotnet publish src/DHPlugin.Host/DHPlugin.Host.csproj \
    -c "$CONFIGURATION" \
    -r "$RUNTIME" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o "$DEB_ROOT/opt/dhplugin"

# 创建启动脚本
cat > "$DEB_ROOT/usr/bin/dhplugin" << 'EOF'
#!/bin/bash
exec /opt/dhplugin/DHPlugin.Host "$@"
EOF
chmod +x "$DEB_ROOT/usr/bin/dhplugin"

# 复制插件包
mkdir -p "$DEB_ROOT/opt/dhplugin/plugins"
if [ -f "src/Plugins/CPlusPlusEditor/CPlusPlusEditor.dhplugin" ]; then
    cp "src/Plugins/CPlusPlusEditor/CPlusPlusEditor.dhplugin" "$DEB_ROOT/opt/dhplugin/plugins/"
fi

# 创建 control 文件
cat > "$DEB_ROOT/DEBIAN/control" << EOF
Package: dhplugin
Version: $VERSION
Section: utils
Priority: optional
Architecture: $ARCH
Maintainer: DHPlugin Team
Description: DHPlugin Cross-Platform Plugin Host
 DHPlugin is a cross-platform plugin host based on Avalonia UI.
 Features:
  - Plugin installation/uninstallation
  - Plugin enable/disable
  - Hot-pluggable plugins
  - Cross-platform (macOS/Windows/Linux)
Depends: libgtk-3-0, libx11-6, libxrandr2, libxi6, libxcursor1, libxinerama1
EOF

# 创建 conffiles
echo "/opt/dhplugin/plugins.config.json" > "$DEB_ROOT/DEBIAN/conffiles"

# 创建 postrm 脚本 (卸载时清理)
cat > "$DEB_ROOT/DEBIAN/postrm" << 'EOF'
#!/bin/bash
if [ "$1" = "remove" ]; then
    rm -rf ~/.local/share/DHPlugin
fi
exit 0
EOF
chmod +x "$DEB_ROOT/DEBIAN/postrm"

# 创建 postinst 脚本 (安装后设置权限)
cat > "$DEB_ROOT/DEBIAN/postinst" << 'EOF'
#!/bin/bash
chmod +x /opt/dhplugin/DHPlugin.Host
chmod +x /usr/bin/dhplugin
mkdir -p ~/.local/share/DHPlugin/Plugins
exit 0
EOF
chmod +x "$DEB_ROOT/DEBIAN/postinst"

# 创建 .desktop 文件 (应用程序菜单)
cat > "$DEB_ROOT/usr/share/applications/dhplugin.desktop" << EOF
[Desktop Entry]
Name=DHPlugin Host
Comment=Cross-Platform Plugin Host
Exec=/opt/dhplugin/DHPlugin.Host
Icon=dhplugin
Terminal=false
Type=Application
Categories=Development;Utility;
Keywords=plugin;development;avalonia;
EOF

# 创建图标 (简单的 SVG)
cat > "$DEB_ROOT/usr/share/icons/hicolor/scalable/apps/dhplugin.svg" << 'EOF'
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <rect width="100" height="100" rx="15" fill="#4CAF50"/>
  <text x="50" y="65" font-size="50" text-anchor="middle" fill="white" font-family="Arial">🧩</text>
</svg>
EOF

# 设置权限
chmod 755 "$DEB_ROOT/opt/dhplugin/DHPlugin.Host"

# 构建 DEB 包
echo "构建 DEB 包..."
cd "$DEB_DIR"
dpkg-deb --build "${PACKAGE_NAME}_${VERSION}_${ARCH}"

echo ""
echo "========================================"
echo "  DEB 打包完成!"
echo "========================================"
echo ""
echo "DEB 包位置：$DEB_DIR/${PACKAGE_NAME}_${VERSION}_${ARCH}.deb"
echo ""
echo "安装方法:"
echo "  sudo dpkg -i ${PACKAGE_NAME}_${VERSION}_${ARCH}.deb"
echo "  # 或"
echo "  sudo apt install ./dhplugin_${VERSION}_${ARCH}.deb"
echo ""
echo "卸载方法:"
echo "  sudo apt remove dhplugin"
echo ""
echo "运行方法:"
echo "  dhplugin          # 命令行启动"
echo "  # 或在应用程序菜单中找到 \"DHPlugin Host\""
echo ""
