@echo off
chcp 65001 >nul
echo ========================================
echo Stratum Desktop - Diagnostic Tool
echo ========================================
echo.

echo [1] Checking data directory...
set DATA_DIR=%APPDATA%\Stratum
echo Data directory: %DATA_DIR%
echo.

if exist "%DATA_DIR%" (
    echo OK Data directory exists
) else (
    echo X Data directory does not exist
    echo Creating directory...
    mkdir "%DATA_DIR%"
)
echo.

echo [2] Checking settings file...
set SETTINGS_FILE=%DATA_DIR%\settings.json
if exist "%SETTINGS_FILE%" (
    echo OK Settings file exists
    echo.
    echo Settings content:
    echo ----------------------------------------
    type "%SETTINGS_FILE%"
    echo.
    echo ----------------------------------------
) else (
    echo X Settings file does not exist
    echo This is normal for first run
)
echo.

echo [3] Checking log files...
set LOG_DIR=%DATA_DIR%\logs
if exist "%LOG_DIR%" (
    echo OK Log directory exists
    echo.
    echo Recent log files:
    echo ----------------------------------------
    dir /b /o-d "%LOG_DIR%\stratum-*.log" 2>nul
    echo.
    echo Latest log content (last 50 lines):
    echo ----------------------------------------
    powershell -Command "Get-ChildItem '%LOG_DIR%\stratum-*.log' | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | Get-Content -Tail 50"
    echo ----------------------------------------
) else (
    echo X Log directory does not exist
)
echo.

echo [4] Checking database file...
set DB_FILE=%DATA_DIR%\authenticator.db3
if exist "%DB_FILE%" (
    echo OK Database file exists
    for %%F in ("%DB_FILE%") do echo Size: %%~zF bytes
) else (
    echo X Database file does not exist
    echo This is normal for first run
)
echo.

echo ========================================
echo Diagnostic complete!
echo ========================================
echo.
echo To view full logs, open:
echo %LOG_DIR%
echo.
echo To view settings, open:
echo %SETTINGS_FILE%
echo.

pause
