#!/usr/bin/env bash

set -e

echo "========================================"
echo "  DHPlugin 跨平台构建脚本"
echo "========================================"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# 配置
CONFIGURATION="${CONFIGURATION:-Release}"
OUTPUT_DIR="$SCRIPT_DIR/publish"

# 清理旧输出
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# 构建目标平台
build_platform() {
    local runtime=$1
    local platform_name=$2
    
    echo ""
    echo "----------------------------------------"
    echo "  构建 $platform_name ($runtime)"
    echo "----------------------------------------"
    
    local platform_output="$OUTPUT_DIR/$platform_name"
    mkdir -p "$platform_output"
    
    dotnet publish src/DHPlugin.Host/DHPlugin.Host.csproj \
        -c "$CONFIGURATION" \
        -r "$runtime" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -o "$platform_output"
    
    echo "  ✓ $platform_name 构建完成：$platform_output"
}

echo "开始构建所有平台..."

# macOS 平台 (Intel + Apple Silicon)
echo ""
echo "=== 构建 macOS 平台 ==="
build_platform "osx-x64" "macos-x64"
build_platform "osx-arm64" "macos-arm64"

# Windows 平台 (x64 + x86)
echo ""
echo "=== 构建 Windows 平台 ==="
build_platform "win-x64" "windows-x64"
build_platform "win-x86" "windows-x86"

# Linux 平台
echo ""
echo "=== 构建 Linux 平台 ==="
build_platform "linux-x64" "linux-x64"
build_platform "linux-arm64" "linux-arm64"

# 创建插件 SDK 包
echo ""
echo "----------------------------------------"
echo "  创建插件 SDK"
echo "----------------------------------------"

SDK_OUTPUT="$OUTPUT_DIR/sdk"
mkdir -p "$SDK_OUTPUT"

# 复制抽象层 DLL
cp src/DHPlugin.Abstractions/bin/$CONFIGURATION/net8.0/DHPlugin.Abstractions.dll "$SDK_OUTPUT/" 2>/dev/null || true

# 创建插件模板
TEMPLATE_DIR="$SDK_OUTPUT/templates"
mkdir -p "$TEMPLATE_DIR/SimplePlugin"

cat > "$TEMPLATE_DIR/SimplePlugin/SimplePlugin.csproj" << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="DHPlugin.Abstractions">
      <HintPath>../lib/DHPlugin.Abstractions.dll</HintPath>
    </Reference>
  </ItemGroup>

  <Target Name="PackPlugin" AfterTargets="Build">
    <PropertyGroup>
      <PluginOutputDir>$(OutputDir)PluginPackage</PluginOutputDir>
    </PropertyGroup>
    <MakeDir Directories="$(PluginOutputDir)" />
    <Copy SourceFiles="$(OutputPath)$(AssemblyName).dll" DestinationFolder="$(PluginOutputDir)" />
    <Copy SourceFiles="manifest.json" DestinationFolder="$(PluginOutputDir)" />
    <ZipDirectory SourceDirectory="$(PluginOutputDir)" DestinationFile="$(OutputDir)$(AssemblyName).dhplugin" Overwrite="true" />
    <Message Importance="high" Text="插件包已创建：$(OutputDir)$(AssemblyName).dhplugin" />
  </Target>
</Project>
EOF

cat > "$TEMPLATE_DIR/SimplePlugin/manifest.json" << 'EOF'
{
  "id": "MyPlugin",
  "name": "我的插件",
  "version": "1.0.0",
  "description": "这是一个示例插件",
  "author": "你的名字",
  "minimumHostVersion": "1.0.0",
  "dependencies": []
}
EOF

cat > "$TEMPLATE_DIR/SimplePlugin/MyPlugin.cs" << 'EOF'
using DHPlugin.Abstractions;

namespace MyPlugin;

public class MyPlugin : IPlugin
{
    public PluginManifest Manifest { get; } = new()
    {
        Id = "MyPlugin",
        Name = "我的插件",
        Version = "1.0.0",
        Description = "这是一个示例插件",
        Author = "你的名字"
    };

    public PluginState State { get; private set; } = PluginState.Unloaded;

    public Task InitializeAsync(IPluginContext context, CancellationToken cancellationToken = default)
    {
        State = PluginState.Loaded;
        context.GetLogger().Info("插件已初始化");
        return Task.CompletedTask;
    }

    public Task EnableAsync(CancellationToken cancellationToken = default)
    {
        State = PluginState.Enabled;
        return Task.CompletedTask;
    }

    public Task DisableAsync(CancellationToken cancellationToken = default)
    {
        State = PluginState.Disabled;
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        State = PluginState.Unloaded;
        return Task.CompletedTask;
    }
}
EOF

echo "  ✓ 插件 SDK 创建完成：$SDK_OUTPUT"

# 打包示例插件
echo ""
echo "----------------------------------------"
echo "  打包示例插件"
echo "----------------------------------------"

dotnet build src/Plugins/CPlusPlusEditor/CPlusPlusEditor.csproj -c Release

echo ""
echo "========================================"
echo "  构建完成!"
echo "========================================"
echo ""
echo "  输出目录：$OUTPUT_DIR"
echo ""
echo "  平台版本:"
echo "  ┌─────────────────────────────────────────┐"
echo "  │ macOS (Intel):    macos-x64/            │"
echo "  │ macOS (M1/M2/M3): macos-arm64/          │"
echo "  │ Windows (64 位):   windows-x64/          │"
echo "  │ Windows (32 位):   windows-x86/          │"
echo "  │ Linux (x64):      linux-x64/            │"
echo "  │ Linux (ARM64):    linux-arm64/          │"
echo "  └─────────────────────────────────────────┘"
echo ""
echo "  插件 SDK: $SDK_OUTPUT"
echo ""
echo "  运行方式:"
echo "  - macOS Intel:    ./publish/macos-x64/DHPlugin.Host"
echo "  - macOS ARM64:    ./publish/macos-arm64/DHPlugin.Host"
echo "  - Linux x64:      ./publish/linux-x64/DHPlugin.Host"
echo "  - Linux ARM64:    ./publish/linux-arm64/DHPlugin.Host"
echo "  - Windows x64:    publish\\windows-x64\\DHPlugin.Host.exe"
echo "  - Windows x86:    publish\\windows-x86\\DHPlugin.Host.exe"
echo ""
