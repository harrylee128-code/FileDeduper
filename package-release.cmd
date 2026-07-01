@echo off
REM ============================================================
REM Build FileDeduper and create a portable release zip.
REM Output: dist\FileDeduper-v2.0.0.zip
REM ============================================================
setlocal enableextensions

set ROOT=%~dp0
set VERSION=2.0.0
set DIST=%ROOT%dist
set PACKAGE=%DIST%\FileDeduper-v%VERSION%
set ZIP=%DIST%\FileDeduper-v%VERSION%.zip

call "%ROOT%build.cmd"
if errorlevel 1 exit /b 1

if exist "%PACKAGE%" rmdir /s /q "%PACKAGE%"
if not exist "%DIST%" mkdir "%DIST%"
mkdir "%PACKAGE%"

copy "%ROOT%bin\Release\FileDeduper.exe" "%PACKAGE%\" >nul
copy "%ROOT%README.md" "%PACKAGE%\" >nul
copy "%ROOT%README.txt" "%PACKAGE%\" >nul
copy "%ROOT%LICENSE" "%PACKAGE%\" >nul
copy "%ROOT%CHANGELOG.md" "%PACKAGE%\" >nul

if exist "%ZIP%" del "%ZIP%"
powershell -NoProfile -ExecutionPolicy Bypass -Command "Compress-Archive -Path '%PACKAGE%\*' -DestinationPath '%ZIP%' -Force"
if errorlevel 1 exit /b 1

echo.
echo [OK] Packaged: %ZIP%
endlocal
