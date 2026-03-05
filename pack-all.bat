@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"

if "%CONFIGURATION%"=="" set "CONFIGURATION=Release"
if "%VERSION%"=="" for /f %%I in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd"') do set "VERSION=%%I"
if "%OUTPUT_ROOT%"=="" set "OUTPUT_ROOT=%SCRIPT_DIR%dist"
if "%CLEAN%"=="" set "CLEAN=1"

echo ========================================
echo   DHPlugin Full Packaging
echo ========================================
echo Configuration: %CONFIGURATION%
echo Version: %VERSION%
echo Output root: %OUTPUT_ROOT%
echo.

call "%SCRIPT_DIR%pack-plugin.bat"
if errorlevel 1 exit /b 1

call "%SCRIPT_DIR%pack-host.bat"
if errorlevel 1 exit /b 1

echo.
echo ========================================
echo All packaging completed
echo Plugins: %OUTPUT_ROOT%\plugins
echo Host: %OUTPUT_ROOT%\host
echo ========================================
exit /b 0
