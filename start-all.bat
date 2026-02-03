@echo off
echo ========================================
echo    DEMARRAGE DU SYSTEME HAMMAM
echo ========================================
echo.

REM Vérifier si .NET est installé
where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo [ERREUR] .NET SDK n'est pas installe.
    echo Telecharger sur: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

REM Vérifier si Node.js est installé
where node >nul 2>nul
if %errorlevel% neq 0 (
    echo [ERREUR] Node.js n'est pas installe.
    echo Telecharger sur: https://nodejs.org/
    pause
    exit /b 1
)

echo [1/4] Restauration des packages .NET...
cd /d "%~dp0src\HammamAPI\HammamAPI.WebAPI"
dotnet restore

echo.
echo [2/4] Demarrage du Backend API...
start "Hammam API" cmd /k "dotnet run"

echo.
echo [3/4] Installation des dependances React...
cd /d "%~dp0src\hammam-dashboard"
call npm install

echo.
echo [4/4] Demarrage du Dashboard...
start "Hammam Dashboard" cmd /k "npm run dev"

echo.
echo ========================================
echo    SYSTEME DEMARRE AVEC SUCCES!
echo ========================================
echo.
echo API:        http://localhost:5000
echo Swagger:    http://localhost:5000/swagger
echo Dashboard:  http://localhost:3000
echo.
echo Appuyez sur une touche pour fermer cette fenetre...
pause >nul
