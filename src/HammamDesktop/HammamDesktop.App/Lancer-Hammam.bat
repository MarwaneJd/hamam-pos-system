@echo off
title Hammam Desktop - Lancement

echo.
echo ========================================
echo    HAMMAM DESKTOP - Point de Vente
echo ========================================
echo.
echo Demarrage de l'application...
echo.

cd /d "%~dp0bin\Debug\net8.0-windows"
start HammamPOS.exe

timeout /t 3
