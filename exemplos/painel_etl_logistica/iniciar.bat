@echo off
title Yvira Logistics Dashboard - ETL Realtime
cls
echo ============================================================
echo      YVIRA WMS LOGISTICS - ETL PIPELINE & ANALYTICS
echo ============================================================
echo.
echo Iniciando Painel com Streaming e Pipeline ETL Ativo...
"..\..\dist\bin\yvira.exe" run main.yv
pause
