@echo off
REM ============================================================
REM Build optional CUDA provider DLL.
REM Requires Visual Studio Build Tools C++ workload and CUDA Toolkit 13.3.
REM ============================================================
setlocal enableextensions

set "ROOT=%~dp0"
set "OUTDIR=%ROOT%bin\Cuda"
if not exist "%OUTDIR%" mkdir "%OUTDIR%"

set "CUDA_ROOT=C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v13.3"
set "NVCC=%CUDA_ROOT%\bin\nvcc.exe"
set "VCVARS=C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvars64.bat"

if not exist "%NVCC%" echo [FAILED] Cannot find nvcc: %NVCC%& exit /b 1

if not exist "%VCVARS%" echo [FAILED] Cannot find vcvars64.bat: %VCVARS%& exit /b 1

call "%VCVARS%" >nul
if errorlevel 1 exit /b 1

"%NVCC%" -shared -O2 -std=c++14 -Xcompiler "/MD /EHsc /O2 /utf-8" -gencode=arch=compute_75,code=sm_75 -gencode=arch=compute_75,code=compute_75 -o "%OUTDIR%\FileDeduperCuda.dll" "%ROOT%native\cuda\FileDeduperCuda.cu"
if errorlevel 1 (
    echo [FAILED] CUDA provider build failed.
    exit /b 1
)

copy "%OUTDIR%\FileDeduperCuda.dll" "%ROOT%bin\Release\" >nul
if exist "%ROOT%bin\Test" copy "%OUTDIR%\FileDeduperCuda.dll" "%ROOT%bin\Test\" >nul

echo.
echo [OK] Built CUDA provider: %OUTDIR%\FileDeduperCuda.dll
endlocal
