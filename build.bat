@echo off
setlocal

rem ============================================================================
rem LEVCAN Tools Build Script
rem Usage:
rem   build.bat               (Builds Release by default)
rem   build.bat release       (Builds Release)
rem   build.bat debug         (Builds Debug)
rem ============================================================================

set "CONFIG=Release"
if /I "%~1"=="debug" set "CONFIG=Debug"
if /I "%~1"=="release" set "CONFIG=Release"

echo ======================================================================
echo  Building LEVCAN Tools [%CONFIG%]
echo ======================================================================

rem Locate vswhere.exe
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" (
    echo [ERROR] vswhere.exe not found at "%VSWHERE%".
    echo Please make sure Visual Studio 2022 is installed.
    exit /b 1
)

rem Find MSBuild path using vswhere
for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\amd64\MSBuild.exe`) do (
    set "MSBUILD=%%i"
)

if not defined MSBUILD (
    for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
        set "MSBUILD=%%i"
    )
)

if not exist "%MSBUILD%" (
    echo [ERROR] MSBuild.exe could not be located via vswhere.
    exit /b 1
)

echo [INFO] Using MSBuild: "%MSBUILD%"
echo.

set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"

rem ----------------------------------------------------------------------------
rem 1. Build Native LEVCAN Lib (x64)
rem ----------------------------------------------------------------------------
echo [1/3] Building native LEVCAN Lib [x64 - %CONFIG%]...
"%MSBUILD%" "LEVCAN Lib\LEVCAN Lib.vcxproj" /p:Configuration=%CONFIG% /p:Platform=x64 /p:SolutionDir="%CD%\\" /v:m /nologo
if errorlevel 1 goto :build_fail_native_x64

rem ----------------------------------------------------------------------------
rem 2. Build Native LEVCAN Lib (Win32 / x86)
rem ----------------------------------------------------------------------------
echo [2/3] Building native LEVCAN Lib [Win32 - %CONFIG%]...
"%MSBUILD%" "LEVCAN Lib\LEVCAN Lib.vcxproj" /p:Configuration=%CONFIG% /p:Platform=Win32 /p:SolutionDir="%CD%\\" /v:m /nologo
if errorlevel 1 goto :build_fail_native_x86

rem ----------------------------------------------------------------------------
rem 3. Build Solution (.NET Managed Projects & Configurator)
rem ----------------------------------------------------------------------------
echo [3/3] Building LEVCAN Tools Solution [%CONFIG%]...
"%MSBUILD%" "LEVCAN Tools.sln" /p:Configuration=%CONFIG% "/p:Platform=Any CPU" /v:m /nologo
if errorlevel 1 goto :build_fail_sln

rem Clean up any stale plugin DLLs from plugins folder if present
if exist "Bin\%CONFIG%\Plugins\DebugProbeTabLib.dll" del /q "Bin\%CONFIG%\Plugins\DebugProbeTabLib.*"

echo.
echo ======================================================================
echo  Build SUCCESS [%CONFIG%]!
echo  Binaries available in: Bin\%CONFIG%\
echo ======================================================================
exit /b 0

:build_fail_native_x64
echo [ERROR] Failed to build LEVCAN Lib (x64).
exit /b 1

:build_fail_native_x86
echo [ERROR] Failed to build LEVCAN Lib (Win32).
exit /b 1

:build_fail_sln
echo [ERROR] Failed to build LEVCAN Tools Solution.
exit /b 1
