@echo off
REM Build script for NAVΛ Dashboard WebGL (Windows)

echo Building NAVΛ Dashboard for WebGL...

REM Check if Unity is installed
set UNITY_PATH=
if exist "C:\Program Files\Unity\Hub\Editor\*\Editor\Unity.exe" (
    for /f "delims=" %%i in ('dir /b /ad /o-d "C:\Program Files\Unity\Hub\Editor"') do (
        set UNITY_PATH=C:\Program Files\Unity\Hub\Editor\%%i\Editor\Unity.exe
        goto :found
    )
)

if exist "C:\Program Files\Unity\Editor\Unity.exe" (
    set UNITY_PATH=C:\Program Files\Unity\Editor\Unity.exe
)

if "%UNITY_PATH%"=="" (
    echo ERROR: Unity Editor not found!
    echo Please install Unity Editor or set UNITY_PATH environment variable
    exit /b 1
)

echo Using Unity: %UNITY_PATH%

REM Build directory
set BUILD_DIR=%~dp0build\webgl
if not exist "%BUILD_DIR%" mkdir "%BUILD_DIR%"

REM Build WebGL
echo Starting WebGL build...
"%UNITY_PATH%" ^
    -batchmode ^
    -quit ^
    -projectPath "%~dp0nava-ai" ^
    -buildTarget WebGL ^
    -buildPath "%BUILD_DIR%" ^
    -logFile "%BUILD_DIR%\build.log"

if %ERRORLEVEL% EQU 0 (
    echo Build successful!
    echo Build output: %BUILD_DIR%
    echo.
    echo To run locally:
    echo   cd %BUILD_DIR%
    echo   python -m http.server 8000
    echo   Then open: http://localhost:8000
) else (
    echo Build failed. Check %BUILD_DIR%\build.log
    exit /b 1
)
