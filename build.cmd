@echo off
REM ============================================================
REM FileDeduper build script - uses built-in .NET Framework csc.exe
REM No SDK / Visual Studio required. Produces a single GUI exe.
REM Explicitly lists source files (excludes Tests\).
REM ============================================================
setlocal enableextensions

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe

set ROOT=%~dp0
set OUTDIR=%ROOT%bin\Release
if not exist "%OUTDIR%" mkdir "%OUTDIR%"

set REFDIR=C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8
if not exist "%REFDIR%\System.dll" set REFDIR=C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2
if not exist "%REFDIR%\System.dll" set REFDIR=C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6.2
if not exist "%REFDIR%\System.dll" set REFDIR=C:\Windows\Microsoft.NET\Framework64\v4.0.30319

echo Using compiler: %CSC%
echo Reference dir:  %REFDIR%
echo Output dir:     %OUTDIR%
echo.

if not exist "%CSC%" (
    echo [FAILED] Cannot find .NET Framework csc.exe.
    exit /b 1
)

if not exist "%REFDIR%\System.dll" (
    echo [FAILED] Cannot find .NET Framework reference assemblies.
    exit /b 1
)

"%CSC%" /noconfig /nologo /target:winexe /platform:anycpu /langversion:5 /optimize+ /nostdlib+ /out:"%OUTDIR%\FileDeduper.exe" /win32icon:"%ROOT%Trash.ico" /reference:"%REFDIR%\mscorlib.dll" /reference:"%REFDIR%\System.dll" /reference:"%REFDIR%\System.Core.dll" /reference:"%REFDIR%\System.Drawing.dll" /reference:"%REFDIR%\System.Windows.Forms.dll" "%ROOT%Program.cs" "%ROOT%Properties\AssemblyInfo.cs" "%ROOT%Forms\MainForm.cs" "%ROOT%Forms\SettingsForm.cs" "%ROOT%Core\FileScanner.cs" "%ROOT%Core\DuplicateDetector.cs" "%ROOT%Core\SmartMarker.cs" "%ROOT%Core\FileDeleter.cs" "%ROOT%Models\Enums.cs" "%ROOT%Models\FileEntry.cs" "%ROOT%Models\DuplicateGroup.cs" "%ROOT%Models\AppSettings.cs" "%ROOT%Utils\ConfigStore.cs" "%ROOT%Utils\MiniJson.cs" "%ROOT%Utils\RecycleBinHelper.cs" "%ROOT%Utils\HashHelper.cs"

if errorlevel 1 (
    echo.
    echo [FAILED] Compilation failed.
    exit /b 1
)

echo.
echo [OK] Built: %OUTDIR%\FileDeduper.exe
endlocal
