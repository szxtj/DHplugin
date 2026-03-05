@echo off
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"

if "%CONFIGURATION%"=="" set "CONFIGURATION=Release"
if "%VERSION%"=="" for /f %%I in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd"') do set "VERSION=%%I"
if "%OUTPUT_ROOT%"=="" set "OUTPUT_ROOT=%SCRIPT_DIR%dist"
if "%CLEAN%"=="" set "CLEAN=1"

set "HOST_OUTPUT_ROOT=%OUTPUT_ROOT%\host"
set "STAGING_ROOT=%OUTPUT_ROOT%\.staging-host"

if /I not "%CONFIGURATION%"=="Release" (
    echo ERROR: pack-host.bat requires CONFIGURATION=Release for distribution builds.
    exit /b 1
)

if "%CLEAN%"=="1" (
    if exist "%HOST_OUTPUT_ROOT%" rmdir /s /q "%HOST_OUTPUT_ROOT%"
    if exist "%STAGING_ROOT%" rmdir /s /q "%STAGING_ROOT%"
)

if not exist "%HOST_OUTPUT_ROOT%" mkdir "%HOST_OUTPUT_ROOT%"
if not exist "%STAGING_ROOT%" mkdir "%STAGING_ROOT%"

echo ========================================
echo   DHPlugin Host Packaging
echo ========================================
echo Configuration: %CONFIGURATION%
echo Version: %VERSION%
echo Output root: %HOST_OUTPUT_ROOT%
echo.

call :pack_rid win-x64 windows-x64 zip
if errorlevel 1 exit /b 1

call :pack_rid linux-x64 linux-x64 targz
if errorlevel 1 exit /b 1

call :pack_rid linux-arm64 linux-arm64 targz
if errorlevel 1 exit /b 1

echo ----------------------------------------
echo Publishing host for macos-arm64 (osx-arm64)
dotnet publish src\DHPlugin.Host\DHPlugin.Host.csproj -c %CONFIGURATION% -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "%STAGING_ROOT%\publish\macos-arm64"
if errorlevel 1 exit /b 1

echo WARNING: macOS DMG creation is not supported in pack-host.bat.
echo WARNING: run pack-host.sh on macOS to create dist/host/macos-arm64/*.dmg.

echo.
echo ========================================
echo Host packaging completed
echo Output dir: %HOST_OUTPUT_ROOT%
echo ========================================
exit /b 0

:pack_rid
set "RID=%~1"
set "PLATFORM=%~2"
set "FORMAT=%~3"
set "BINARY=DHPlugin.Host"
if /I "%RID%"=="win-x64" set "BINARY=DHPlugin.Host.exe"

set "PLATFORM_OUTPUT_DIR=%HOST_OUTPUT_ROOT%\%PLATFORM%"
set "PUBLISH_DIR=%STAGING_ROOT%\publish\%PLATFORM%"
set "PACKAGE_PARENT=%STAGING_ROOT%\package\%PLATFORM%"
set "PACKAGE_NAME=DHPlugin-Host-%PLATFORM%-%VERSION%"
set "PACKAGE_DIR=%PACKAGE_PARENT%\%PACKAGE_NAME%"

echo ----------------------------------------
echo Publishing host for %PLATFORM% (%RID%)

if exist "%PUBLISH_DIR%" rmdir /s /q "%PUBLISH_DIR%"
if exist "%PACKAGE_PARENT%" rmdir /s /q "%PACKAGE_PARENT%"
mkdir "%PUBLISH_DIR%"
mkdir "%PACKAGE_PARENT%"

dotnet publish src\DHPlugin.Host\DHPlugin.Host.csproj -c %CONFIGURATION% -r %RID% --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "%PUBLISH_DIR%"
if errorlevel 1 exit /b 1

mkdir "%PACKAGE_DIR%"
xcopy "%PUBLISH_DIR%\*" "%PACKAGE_DIR%\" /E /I /Y >nul

if not exist "%PACKAGE_DIR%\%BINARY%" (
    echo ERROR: missing host binary for %PLATFORM%: %BINARY%
    exit /b 1
)

if /I not "%RID%"=="win-x64" (
    > "%PACKAGE_DIR%\run.sh" echo #!/bin/bash
    >> "%PACKAGE_DIR%\run.sh" echo SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" ^&^& pwd)"
    >> "%PACKAGE_DIR%\run.sh" echo cd "$SCRIPT_DIR"
    >> "%PACKAGE_DIR%\run.sh" echo export DOTNET_ROOT="$SCRIPT_DIR"
    >> "%PACKAGE_DIR%\run.sh" echo exec "$SCRIPT_DIR/DHPlugin.Host" "$@"
)

> "%PACKAGE_DIR%\README.txt" echo DHPlugin Host Package
>> "%PACKAGE_DIR%\README.txt" echo =====================
>> "%PACKAGE_DIR%\README.txt" echo.
>> "%PACKAGE_DIR%\README.txt" echo Runtime: %RID%
>> "%PACKAGE_DIR%\README.txt" echo Version: %VERSION%
>> "%PACKAGE_DIR%\README.txt" echo.
>> "%PACKAGE_DIR%\README.txt" echo This package contains only the host program.
>> "%PACKAGE_DIR%\README.txt" echo Plugin packages are distributed separately.

if not exist "%PLATFORM_OUTPUT_DIR%" mkdir "%PLATFORM_OUTPUT_DIR%"

if /I "%FORMAT%"=="zip" (
    powershell -NoProfile -Command "Compress-Archive -Path '%PACKAGE_DIR%' -DestinationPath '%PLATFORM_OUTPUT_DIR%\%PACKAGE_NAME%.zip' -Force"
    if errorlevel 1 exit /b 1
    echo Packed: %PLATFORM_OUTPUT_DIR%\%PACKAGE_NAME%.zip
) else (
    tar -czf "%PLATFORM_OUTPUT_DIR%\%PACKAGE_NAME%.tar.gz" -C "%PACKAGE_PARENT%" "%PACKAGE_NAME%"
    if errorlevel 1 exit /b 1
    echo Packed: %PLATFORM_OUTPUT_DIR%\%PACKAGE_NAME%.tar.gz
)

exit /b 0
