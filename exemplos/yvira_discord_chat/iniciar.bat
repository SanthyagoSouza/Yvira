@echo off
title Yvira Discord Chat - Inicializador
cls
echo ============================================================
echo           YVIRA DISCORD CHAT - REALTIME MULTIPLAYER
echo ============================================================
echo.
echo [1/3] Iniciando Servidor Central de Canais na porta 7080...
start "Yvira Chat Server" "..\..\dist\bin\yvira.exe" run server.yv

timeout /t 2 /nobreak >nul

echo [2/3] Abrindo Cliente 1 (Usuario Alpha)...
start "Yvira Chat - Cliente 1" "..\..\dist\bin\yvira.exe" run main.yv

timeout /t 1 /nobreak >nul

echo [3/3] Abrindo Cliente 2 (Usuario Beta)...
start "Yvira Chat - Cliente 2" "..\..\dist\bin\yvira.exe" run main.yv

echo.
echo Pronto! O Servidor e 2 Clientes foram iniciados.
echo Divirta-se conversando entre as salas!
echo ============================================================
pause
