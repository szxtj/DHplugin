@echo off
setlocal enabledelayedexpansion

echo ========================================
echo   DHPlugin 跨平台构建脚本
echo ========================================

set SCRIPT_DIR=%~dp0
set CONFIGURATION=Release

set OUTPUT_DIR=%SCRIPT_DIR%publish
if exist "%OUTPUT_DIR%" rmdir /s /q "%OUTPUT_DIR%"
mkdir "%OUTPUT_DIR%"

echo.
echo === 构建 macOS 平台 ===

echo.
echo ----------------------------------------
echo   构建 macOS x64 (Intel)
echo ----------------------------------------
set PLATFORM_OUTPUT=%OUTPUT_DIR%\macos-x64
mkdir "%PLATFORM_OUTPUT%"
dotnet publish src\DHPlugin.Host\DHPlugin.Host.csproj ^
    -c %CONFIGURATION% ^
    -r osx-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "%PLATFORM_OUTPUT%"
echo   ✓ macOS x64 构建完成

echo.
echo ----------------------------------------
echo   构建 macOS ARM64 (Apple Silicon)
echo ----------------------------------------
set PLATFORM_OUTPUT=%OUTPUT_DIR%\macos-arm64
mkdir "%PLATFORM_OUTPUT%"
dotnet publish src\DHPlugin.Host\DHPlugin.Host.csproj ^
    -c %CONFIGURATION% ^
    -r osx-arm64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "%PLATFORM_OUTPUT%"
echo   ✓ macOS ARM64 构建完成

echo.
echo === 构建 Windows 平台 ===

echo.
echo ----------------------------------------
echo   构建 Windows x64
echo ----------------------------------------
set PLATFORM_OUTPUT=%OUTPUT_DIR%\windows-x64
mkdir "%PLATFORM_OUTPUT%"
dotnet publish src\DHPlugin.Host\DHPlugin.Host.csproj ^
    -c %CONFIGURATION% ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "%PLATFORM_OUTPUT%"
echo   ✓ Windows x64 构建完成

echo.
echo ----------------------------------------
echo   构建 Windows x86
echo ----------------------------------------
set PLATFORM_OUTPUT=%OUTPUT_DIR%\windows-x86
mkdir "%PLATFORM_OUTPUT%"
dotnet publish src\DHPlugin.Host\DHPlugin.Host.csproj ^
    -c %CONFIGURATION% ^
    -r win-x86 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "%PLATFORM_OUTPUT%"
echo   ✓ Windows x86 构建完成

echo.
echo === 构建 Linux 平台 ===

echo.
echo ----------------------------------------
echo   构建 Linux x64
echo ----------------------------------------
set PLATFORM_OUTPUT=%OUTPUT_DIR%\linux-x64
mkdir "%PLATFORM_OUTPUT%"
dotnet publish src\DHPlugin.Host\DHPlugin.Host.csproj ^
    -c %CONFIGURATION% ^
    -r linux-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "%PLATFORM_OUTPUT%"
echo   ✓ Linux x64 构建完成

echo.
echo ----------------------------------------
echo   构建 Linux ARM64
echo ----------------------------------------
set PLATFORM_OUTPUT=%OUTPUT_DIR%\linux-arm64
mkdir "%PLATFORM_OUTPUT%"
dotnet publish src\DHPlugin.Host\DHPlugin.Host.csproj ^
    -c %CONFIGURATION% ^
    -r linux-arm64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "%PLATFORM_OUTPUT%"
echo   ✓ Linux ARM64 构建完成

echo.
echo ----------------------------------------
echo   创建插件 SDK
echo ----------------------------------------

set SDK_OUTPUT=%OUTPUT_DIR%\sdk
mkdir "%SDK_OUTPUT%"

if exist "src\DHPlugin.Abstractions\bin\%CONFIGURATION%\net8.0\DHPlugin.Abstractions.dll" (
    copy "src\DHPlugin.Abstractions\bin\%CONFIGURATION%\net8.0\DHPlugin.Abstractions.dll" "%SDK_OUTPUT%\" >nul
)

echo   ✓ 插件 SDK 创建完成

echo.
echo ----------------------------------------
echo   打包示例插件
echo ----------------------------------------

dotnet build src\Plugins\CPlusPlusEditor\CPlusPlusEditor.csproj -c Release

echo.
echo ========================================
echo   构建完成!
echo ========================================
echo.
echo   输出目录：%OUTPUT_DIR%
echo.
echo   平台版本:
echo   +-----------------------------------------+
echo   | macOS (Intel):    macos-x64\            |
echo   | macOS (M1/M2/M3): macos-arm64\          |
echo   | Windows (64 位):   windows-x64\          |
echo   | Windows (32 位):   windows-x86\          |
echo   | Linux (x64):      linux-x64\            |
echo   | Linux (ARM64):    linux-arm64\          |
echo   +-----------------------------------------+
echo.
echo   插件 SDK: %SDK_OUTPUT%
echo.
echo   运行方式:
echo   - macOS Intel:   ./publish/macos-x64/DHPlugin.Host
echo   - macOS ARM64:   ./publish/macos-arm64/DHPlugin.Host
echo   - Linux x64:     ./publish/linux-x64/DHPlugin.Host
echo   - Linux ARM64:   ./publish/linux-arm64/DHPlugin.Host
echo   - Windows x64:   publish\windows-x64\DHPlugin.Host.exe
echo   - Windows x86:   publish\windows-x86\DHPlugin.Host.exe
echo.

pause
