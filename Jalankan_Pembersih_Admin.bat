@echo off
:: ============================================================================
:: Ultimate Deep Cleaner Pro (Sandy Edition) - Admin Launcher
:: Secara otomatis meminta hak akses Administrator (UAC) jika belum elevated
:: ============================================================================

title Ultimate Deep Cleaner Pro (Sandy Edition)

:: Periksa hak Administrator
net session >nul 2>&1
if %errorLevel% == 0 (
    goto :RunApp
) else (
    goto :ElevateUAC
)

:ElevateUAC
echo Meminta izin Administrator (UAC)...
powershell -Command "Start-Process cmd -ArgumentList '/c \"\"%~dp0Jalankan_Pembersih_Admin.bat\"\"' -Verb RunAs"
exit /b

:RunApp
cd /d "%~dp0"

:: Cek apakah file EXE sudah ada
if exist "%~dp0dist\UltimateCleaner_Pro.exe" (
    start "" "%~dp0dist\UltimateCleaner_Pro.exe"
    exit /b
)

if exist "%~dp0UltimateCleaner_Pro.exe" (
    start "" "%~dp0UltimateCleaner_Pro.exe"
    exit /b
)

:: Jika EXE belum ada, jalankan via Python
"C:\Users\Lenovo\AppData\Local\Python\pythoncore-3.14-64\python.exe" "%~dp0main_gui.py"
exit /b

