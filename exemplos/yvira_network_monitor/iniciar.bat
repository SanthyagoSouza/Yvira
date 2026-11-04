@echo off
title Yvira Pulse - Network Sentinel (Vue 3 + @time)
cd /d "%~dp0"
echo ========================================================
echo   Iniciando Yvira Pulse - Painel de Rede e Switches
echo ========================================================
echo.
dotnet run --project "..\..\Yvira.Cli" -- "main.yv"
pause
