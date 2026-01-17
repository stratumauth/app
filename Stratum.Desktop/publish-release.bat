@echo off
chcp 65001 >nul
REM Stratum Desktop - Quick Release Build Script
REM Purpose: One-click compile and package Release version

echo ========================================
echo Stratum Desktop - Release Publisher
echo ========================================
echo.

REM Set version number
set VERSION=1.0.0
set /p VERSION="Enter version (default %VERSION%): " || set VERSION=%VERSION%

echo.
echo Version: v%VERSION%
echo.

REM Clean old release files
echo [1/4] Cleaning old release files...
if exist "releases\v%VERSION%" (
    rmdir /s /q "releases\v%VERSION%"
)
mkdir "releases\v%VERSION%"

REM Publish Windows x64 version (without trimming - WPF not supported)
echo.
echo [2/4] Publishing Windows x64 version...
echo Building with version: %VERSION%
dotnet publish -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -p:Version=%VERSION%

if %ERRORLEVEL% NEQ 0 (
    echo Error: Publish failed!
    pause
    exit /b 1
)

REM Copy executable
echo.
echo [3/4] Copying executable...
copy "bin\Release\net9.0-windows\win-x64\publish\Stratum.exe" ^
     "releases\v%VERSION%\Stratum-Windows-x64-v%VERSION%.exe"

REM Display file info
echo.
echo [4/4] Publish completed!
echo.
echo ========================================
echo Release file location:
echo ----------------------------------------
dir "releases\v%VERSION%" /b
echo ----------------------------------------
echo.

REM Calculate file size
setlocal enabledelayedexpansion
for %%F in ("releases\v%VERSION%\Stratum-Windows-x64-v%VERSION%.exe") do (
    set SIZE=%%~zF
    set /a SIZE_MB=!SIZE! / 1048576
    echo EXE file size: !SIZE_MB! MB
)

echo.
echo ========================================
echo Next steps:
echo ----------------------------------------
echo 1. Test releases\v%VERSION%\Stratum-Windows-x64-v%VERSION%.exe
echo 2. Create GitHub Release
echo 3. Upload release file
echo ========================================
echo.

pause
