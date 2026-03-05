@echo off
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"

if "%CONFIGURATION%"=="" set "CONFIGURATION=Release"
if "%OUTPUT_ROOT%"=="" set "OUTPUT_ROOT=%SCRIPT_DIR%dist"
if "%CLEAN%"=="" set "CLEAN=1"
set "PLUGIN_OUTPUT_DIR=%OUTPUT_ROOT%\plugins"

if /I not "%CONFIGURATION%"=="Release" (
    echo ERROR: pack-plugin.bat requires CONFIGURATION=Release to generate .dhplugin packages.
    exit /b 1
)

if "%CLEAN%"=="1" (
    if exist "%PLUGIN_OUTPUT_DIR%" rmdir /s /q "%PLUGIN_OUTPUT_DIR%"
)
if not exist "%PLUGIN_OUTPUT_DIR%" mkdir "%PLUGIN_OUTPUT_DIR%"

echo ========================================
echo   DHPlugin Plugin Packaging
echo ========================================
echo Configuration: %CONFIGURATION%
echo Output: %PLUGIN_OUTPUT_DIR%
echo.

set /a PACKED_COUNT=0
set "FOUND_PROJECT=0"

for /d %%D in (src\Plugins\*) do (
    for %%F in ("%%D\*.csproj") do (
        if exist "%%~fF" (
            set "FOUND_PROJECT=1"
            call :build_plugin "%%~fF"
            if errorlevel 1 exit /b 1
        )
    )
)

if "%FOUND_PROJECT%"=="0" (
    echo ERROR: no plugin projects found under src\Plugins.
    exit /b 1
)

echo.
echo ========================================
echo Plugin packaging completed
echo Total plugins: %PACKED_COUNT%
echo Output dir: %PLUGIN_OUTPUT_DIR%
echo ========================================
exit /b 0

:build_plugin
set "PROJECT_PATH=%~1"
for %%I in ("%PROJECT_PATH%") do (
    set "PROJECT_DIR=%%~dpI"
    set "PLUGIN_NAME=%%~nI"
)

echo ----------------------------------------
echo Building plugin: %PLUGIN_NAME%
echo Project: %PROJECT_PATH%

dotnet build "%PROJECT_PATH%" -c "%CONFIGURATION%"
if errorlevel 1 exit /b 1

set "PACKAGE_PATH="
if exist "%PROJECT_DIR%%PLUGIN_NAME%.dhplugin" (
    set "PACKAGE_PATH=%PROJECT_DIR%%PLUGIN_NAME%.dhplugin"
) else (
    for /f "usebackq delims=" %%P in (`powershell -NoProfile -Command "$p=Get-ChildItem -Path '%PROJECT_DIR%' -Filter *.dhplugin -Recurse ^| Sort-Object LastWriteTime -Descending ^| Select-Object -First 1 -ExpandProperty FullName; if($p){$p}"`) do (
        set "PACKAGE_PATH=%%P"
    )
)

if "%PACKAGE_PATH%"=="" (
    echo ERROR: no .dhplugin package produced for %PLUGIN_NAME%.
    exit /b 1
)

copy /y "%PACKAGE_PATH%" "%PLUGIN_OUTPUT_DIR%\%PLUGIN_NAME%.dhplugin" >nul
if errorlevel 1 exit /b 1

echo Packed: %PLUGIN_OUTPUT_DIR%\%PLUGIN_NAME%.dhplugin
set /a PACKED_COUNT+=1
exit /b 0
