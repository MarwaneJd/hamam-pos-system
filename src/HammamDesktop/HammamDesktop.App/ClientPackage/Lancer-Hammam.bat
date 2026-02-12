@echo off
title Hammam Desktop - Lancement

echo.
echo ========================================
echo    HAMMAM DESKTOP - Point de Vente
echo ========================================
echo.
echo Demarrage de l'application...
echo.

REM Lancer via VBScript pour éviter la fenêtre de terminal
wscript.exe "%~dp0Lancer-Hammam.vbs"

echo Application lancee avec succes!
timeout /t 2 >nul
