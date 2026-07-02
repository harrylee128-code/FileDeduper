@echo off
REM ============================================================
REM FileDeduper self-test build - console exe using Core logic
REM No GUI dependency. Compiles Tests + Core + Models + Utils.
REM ============================================================
setlocal enableextensions

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe

set ROOT=%~dp0
set OUTDIR=%ROOT%bin\Test
if not exist "%OUTDIR%" mkdir "%OUTDIR%"

set REFDIR=C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8
if not exist "%REFDIR%\System.dll" set REFDIR=C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2
if not exist "%REFDIR%\System.dll" set REFDIR=C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6.2
if not exist "%REFDIR%\System.dll" set REFDIR=C:\Windows\Microsoft.NET\Framework64\v4.0.30319

echo Building test exe...

if not exist "%CSC%" (
    echo [FAILED] Cannot find .NET Framework csc.exe.
    exit /b 1
)

if not exist "%REFDIR%\System.dll" (
    echo [FAILED] Cannot find .NET Framework reference assemblies.
    exit /b 1
)

"%CSC%" /noconfig /nologo /target:exe /platform:anycpu /langversion:5 /optimize+ /nostdlib+ /out:"%OUTDIR%\FileDeduper.Test.exe" /reference:"%REFDIR%\mscorlib.dll" /reference:"%REFDIR%\System.dll" /reference:"%REFDIR%\System.Core.dll" "%ROOT%Tests\SelfTest.cs" "%ROOT%Core\FileScanner.cs" "%ROOT%Core\DuplicateDetector.cs" "%ROOT%Core\SmartMarker.cs" "%ROOT%Core\FileDeleter.cs" "%ROOT%Models\Enums.cs" "%ROOT%Models\FileEntry.cs" "%ROOT%Models\DuplicateGroup.cs" "%ROOT%Models\AppSettings.cs" "%ROOT%Utils\AppVersionInfo.cs" "%ROOT%Utils\ConfigStore.cs" "%ROOT%Utils\MiniJson.cs" "%ROOT%Utils\RecycleBinHelper.cs" "%ROOT%Utils\HardwareCapabilityDetector.cs" "%ROOT%Utils\HashEngine.cs" "%ROOT%Utils\CudaHashProvider.cs" "%ROOT%Utils\HashParallelism.cs" "%ROOT%Utils\HashBenchmark.cs" "%ROOT%Utils\HashHelper.cs"

if errorlevel 1 (
    echo [FAILED] Test build failed.
    exit /b 1
)
echo [OK] Built: %OUTDIR%\FileDeduper.Test.exe
endlocal
