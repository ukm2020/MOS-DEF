@echo off
REM MOS-DEF Build Script
REM This script builds MOS-DEF using CMake and Visual Studio tools

echo MOS-DEF Build Script
echo ===================

REM Check if we're in the right directory
if not exist "CMakeLists.txt" (
    echo Error: CMakeLists.txt not found. Please run this script from the MOS-DEF root directory.
    pause
    exit /b 1
)

REM Create build directory if it doesn't exist
if not exist "build" (
    mkdir build
)

REM Change to build directory
cd build

echo Configuring build with CMake...
cmake .. -DCMAKE_BUILD_TYPE=Release

if %ERRORLEVEL% neq 0 (
    echo Error: CMake configuration failed. Please ensure:
    echo - CMake is installed and in PATH
    echo - Visual Studio Build Tools or Visual Studio is installed
    echo - You have the C++ workload installed
    pause
    exit /b 1
)

echo Building MOS-DEF...
cmake --build . --config Release

if %ERRORLEVEL% neq 0 (
    echo Error: Build failed. Please check the error messages above.
    pause
    exit /b 1
)

echo.
echo Build completed successfully!
echo Executable created at: %~dp0artifacts\mos-def.exe
echo.

REM Copy executable to artifacts directory if it exists
if exist "Release\mos-def.exe" (
    if not exist "..\artifacts" (
        mkdir ..\artifacts
    )
    copy Release\mos-def.exe ..\artifacts\
    echo Copied executable to artifacts directory.
)

pause
