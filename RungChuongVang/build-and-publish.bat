@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

echo ========================================================
echo   DONG GOI PHAN MEM RUNG CHUONG VANG (.NET 8 WIN-X64)
echo ========================================================
echo Thu muc hien tai: %CD%
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [LOI] Khong tim thay .NET SDK tren may nay!
    echo Vui long tai va cai dat .NET 8.0 SDK ^(x64^) tu:
    echo   https://dotnet.microsoft.com/en-us/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

dotnet --list-sdks | findstr /B "8." >nul
if errorlevel 1 (
    echo [CANH BAO] Khong thay .NET 8 SDK trong danh sach SDK da cai:
    dotnet --list-sdks
    echo Neu qua trinh build bao loi TargetFramework net8.0-windows, hay cai .NET 8.0 SDK ^(x64^).
    echo.
)

echo [1/4] Khoi phuc goi NuGet ^(ClosedXML, System.Data.OleDb^)...
dotnet restore RungChuongVang.csproj
if errorlevel 1 (
    echo.
    echo [LOI] dotnet restore that bai! Hay kiem tra ket noi Internet de tai ClosedXML va System.Data.OleDb.
    pause
    exit /b 1
)

echo.
echo [2/4] Bien dich Release ^(x64^)...
dotnet build RungChuongVang.csproj -c Release --no-restore
if errorlevel 1 (
    echo.
    echo [LOI] Bien dich that bai! Xem thong bao loi C#/MSBuild o tren.
    pause
    exit /b 1
)

echo.
echo [3/4] Xuat ban goi doc lap offline ^(win-x64 self-contained^)...
dotnet publish RungChuongVang.csproj -c Release -r win-x64 --self-contained true -p:PlatformTarget=x64 -p:PublishReadyToRun=true -o publish\RungChuongVang_Win64
if errorlevel 1 (
    echo.
    echo [LOI] Xuat ban that bai! Xem thong bao loi o tren.
    pause
    exit /b 1
)

echo.
echo [4/4] Chuan bi thu muc Data va Tai lieu huong dan...
if not exist "publish\RungChuongVang_Win64\Data\Images\Questions" mkdir "publish\RungChuongVang_Win64\Data\Images\Questions"
if not exist "publish\RungChuongVang_Win64\Data\Images\Winners" mkdir "publish\RungChuongVang_Win64\Data\Images\Winners"
if not exist "publish\RungChuongVang_Win64\Data\Images\Teams" mkdir "publish\RungChuongVang_Win64\Data\Images\Teams"
if not exist "publish\RungChuongVang_Win64\Data\Images\Contestants" mkdir "publish\RungChuongVang_Win64\Data\Images\Contestants"
if not exist "publish\RungChuongVang_Win64\Data\Images\Logo" mkdir "publish\RungChuongVang_Win64\Data\Images\Logo"
if not exist "publish\RungChuongVang_Win64\Data\Backup" mkdir "publish\RungChuongVang_Win64\Data\Backup"
if not exist "publish\RungChuongVang_Win64\Data\Exports" mkdir "publish\RungChuongVang_Win64\Data\Exports"
if not exist "publish\RungChuongVang_Win64\Data\Logs" mkdir "publish\RungChuongVang_Win64\Data\Logs"

if exist "Docs" xcopy /E /I /Y "Docs" "publish\RungChuongVang_Win64\Docs" >nul
if exist "README.md" copy /Y "README.md" "publish\RungChuongVang_Win64\README.md" >nul

echo.
echo ========================================================
echo [HOAN TAT] Phan mem da duoc xuat ban tai:
echo   %CD%\publish\RungChuongVang_Win64\RungChuongVang.exe
echo.
echo Luu y: Truoc khi chay RungChuongVang.exe, may Windows can co
echo   Microsoft Access Database Engine 2016 Redistributable ^(x64^).
echo ========================================================
pause
