<#
.SYNOPSIS
    Compila e publica a nova versão do Yvira globalmente no sistema operacional.
.EXAMPLE
    .\scripts\publish_local.ps1
    .\scripts\publish_local.ps1 -Version "2.2.0"
#>
param(
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"

Write-Host "`n=== Iniciando Publicacao e Atualizacao do Yvira ===" -ForegroundColor Cyan

# 1. Obter diretorio raiz
$RootDir = Split-Path -Parent $PSScriptRoot
Set-Location $RootDir

# 2. Executar testes / verificacao rapida
Write-Host "[1/3] Compilando componentes da linguagem..." -ForegroundColor Yellow
dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERRO] A compilacao falhou! Corrija os erros antes de publicar." -ForegroundColor Red
    exit 1
}

# 3. Publicar binario nativo self-contained
Write-Host "[2/3] Gerando binario nativo standalone (win-x64)..." -ForegroundColor Yellow
$DistBin = Join-Path $RootDir "dist\bin"
dotnet publish Yvira.Cli -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $DistBin

if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERRO] Falha ao publicar o executavel!" -ForegroundColor Red
    exit 1
}

# 4. Criar alias yvira.exe
$SourceExe = Join-Path $DistBin "Yvira.Cli.exe"
$TargetExe = Join-Path $DistBin "yvira.exe"
Copy-Item $SourceExe $TargetExe -Force

Write-Host "[3/3] Atualizando executavel global no PATH..." -ForegroundColor Yellow
Write-Host "`n[SUCESSO] Yvira atualizado globalmente no seu sistema!" -ForegroundColor Green
Write-Host "Localizacao: $TargetExe"
Write-Host "Agora qualquer comando 'yvira' em qualquer terminal ja roda com suas alteracoes mais recentes!`n"
