@echo off
echo ========================================
echo Lancement de l'application Hammam Desktop
echo ========================================
echo.

cd /d "%~dp0"

echo Construction de l'application...
dotnet build
if %ERRORLEVEL% NEQ 0 (
    echo ERREUR lors de la compilation!
    pause
    exit /b 1
)

echo.
echo Lancement de l'application...
echo.
dotnet run

pause
