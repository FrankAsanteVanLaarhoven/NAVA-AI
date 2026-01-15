@echo off
REM Build script for NAVΛ Lambda Core Rust library (Windows)

echo Building NAVΛ Lambda Core...

REM Build release version
cargo build --release

REM Copy DLL to Unity
if exist "target\release\nav_lambda_core.dll" (
    copy "target\release\nav_lambda_core.dll" "..\nava-ai\Assets\Plugins\x86_64\"
    echo Library copied to Unity Plugins folder
) else (
    echo ERROR: Library not found in target\release\
    exit /b 1
)

echo.
echo Build complete! Library is ready for Unity integration.
