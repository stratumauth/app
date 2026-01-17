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
echo [1/5] Cleaning old release files...
if exist "releases\v%VERSION%" (
    rmdir /s /q "releases\v%VERSION%"
)
mkdir "releases\v%VERSION%"

REM Publish Windows x64 version (without trimming - WPF not supported)
echo.
echo [2/5] Publishing Windows x64 version...
dotnet publish -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true

if %ERRORLEVEL% NEQ 0 (
    echo Error: Publish failed!
    pause
    exit /b 1
)

REM Copy executable
echo.
echo [3/5] Copying executable...
copy "bin\Release\net9.0-windows\win-x64\publish\Stratum.exe" ^
     "releases\v%VERSION%\Stratum-Windows-x64-v%VERSION%.exe"

REM Create ZIP archive
echo.
echo [4/5] Creating ZIP archive...
powershell -Command "Compress-Archive -Path 'releases\v%VERSION%\Stratum-Windows-x64-v%VERSION%.exe' -DestinationPath 'releases\v%VERSION%\Stratum-Windows-x64-v%VERSION%.zip' -Force"

REM Display file info
echo.
echo [5/5] Publish completed!
echo.
echo ========================================
echo Release files location:
echo ----------------------------------------
dir "releases\v%VERSION%" /b
echo ----------------------------------------
echo.

REM Calculate file sizes
setlocal enabledelayedexpansion
for %%F in ("releases\v%VERSION%\Stratum-Windows-x64-v%VERSION%.exe") do (
    set SIZE=%%~zF
    set /a SIZE_MB=!SIZE! / 1048576
    echo EXE file size: !SIZE_MB! MB
)

for %%F in ("releases\v%VERSION%\Stratum-Windows-x64-v%VERSION%.zip") do (
    set SIZE=%%~zF
    set /a SIZE_MB=!SIZE! / 1048576
    echo ZIP file size: !SIZE_MB! MB
)

echo.
echo ========================================
echo Next steps:
echo ----------------------------------------
echo 1. Test releases\v%VERSION%\Stratum-Windows-x64-v%VERSION%.exe
echo 2. Create GitHub Release
echo 3. Upload release files
echo ========================================
echo.

pause
