@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
pushd "%SCRIPT_DIR%" >nul

for /f %%i in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd_HHmmss"') do set "TS=%%i"
set "ARCHIVE=script_%TS%.rar"
set "RAR_EXE="

if exist "%ProgramFiles%\WinRAR\WinRAR.exe" set "RAR_EXE=%ProgramFiles%\WinRAR\WinRAR.exe"
if not defined RAR_EXE (
    for /f "delims=" %%r in ('where rar.exe 2^>nul') do (
        set "RAR_EXE=%%r"
        goto :rar_found
    )
)

:rar_found
if not defined RAR_EXE (
    echo RAR araci bulunamadi. WinRAR veya rar.exe yuklu olmali.
    popd >nul
    exit /b 1
)

if not exist "*.cs" (
    echo Bu klasorde .cs dosyasi bulunamadi.
    popd >nul
    exit /b 1
)

"%RAR_EXE%" a -ep1 "%ARCHIVE%" "*.cs"
if errorlevel 1 (
    echo RAR olusturma basarisiz.
    popd >nul
    exit /b 1
)

echo Olusturuldu: "%SCRIPT_DIR%%ARCHIVE%"
popd >nul
exit /b 0

