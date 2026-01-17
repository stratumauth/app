@echo off
chcp 65001 >nul
echo ========================================
echo Version Test - Check Assembly Version
echo ========================================
echo.

echo This script will:
echo 1. Build with a test version number
echo 2. Extract and display the assembly version
echo.

set TEST_VERSION=1.2.3
echo Test version: %TEST_VERSION%
echo.

echo [1/2] Building with version %TEST_VERSION%...
dotnet build -c Release -p:Version=%TEST_VERSION% >nul 2>&1

if %ERRORLEVEL% NEQ 0 (
    echo Error: Build failed!
    pause
    exit /b 1
)

echo OK Build successful
echo.

echo [2/2] Checking assembly version...
powershell -Command "$assembly = [System.Reflection.Assembly]::LoadFile('%CD%\bin\Release\net9.0-windows\win-x64\Stratum.dll'); $version = $assembly.GetName().Version; Write-Host 'Assembly Version:' $version.Major'.'$version.Minor'.'$version.Build"

echo.
echo ========================================
echo Test complete!
echo ========================================
echo.
echo If the assembly version matches %TEST_VERSION%, the fix is working correctly.
echo.

pause
