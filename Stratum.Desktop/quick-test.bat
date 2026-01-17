@echo off
echo ========================================
echo Stratum Desktop - Quick Test Guide
echo ========================================
echo.
echo Step 1: Run the new test version
echo ----------------------------------------
echo Location: test-build\Stratum.exe
echo.
echo Please:
echo 1. Close any running Stratum instances
echo 2. Run: test-build\Stratum.exe
echo 3. Check if it shows Chinese or English
echo 4. Close the application
echo.
pause
echo.
echo Step 2: View logs
echo ----------------------------------------
echo Running log viewer...
echo.
powershell -ExecutionPolicy Bypass -File view-logs.ps1
