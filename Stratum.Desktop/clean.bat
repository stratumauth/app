@echo off
chcp 65001 >nul
echo ========================================
echo Stratum Desktop - Clean Build Outputs
echo ========================================
echo.

echo Cleaning build directories...
echo.

if exist "bin" (
    echo [1/4] Removing bin/...
    rmdir /s /q "bin"
    echo OK Removed bin/
) else (
    echo [1/4] bin/ does not exist
)

if exist "obj" (
    echo [2/4] Removing obj/...
    rmdir /s /q "obj"
    echo OK Removed obj/
) else (
    echo [2/4] obj/ does not exist
)

if exist "test-build" (
    echo [3/4] Removing test-build/...
    rmdir /s /q "test-build"
    echo OK Removed test-build/
) else (
    echo [3/4] test-build/ does not exist
)

echo [4/4] Checking releases/...
if exist "releases" (
    echo INFO releases/ exists (kept for version history)
) else (
    echo INFO releases/ does not exist
)

echo.
echo ========================================
echo Clean complete!
echo ========================================
echo.
echo Kept directories:
echo - releases/     (version history)
echo.
echo Removed directories:
echo - bin/          (auto-generated on build)
echo - obj/          (auto-generated on build)
echo - test-build/   (temporary test builds)
echo.

pause
