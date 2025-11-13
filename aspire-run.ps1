#!/usr/bin/env pwsh
# Script para rodar o Pillar ERP com .NET Aspire

param(
    [Parameter()]
    [switch]$Build,
    
    [Parameter()]
    [switch]$Run,
    
    [Parameter()]
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

function Show-Usage {
    Write-Host @"
.NET Aspire - Pillar ERP

Uso: .\aspire-run.ps1 [opcoes]

Opcoes:
  -Build    Builda todos os projetos
  -Run      Roda o AppHost (inicia app + PostgreSQL + Dashboard)
  -Clean    Limpa builds anteriores

Exemplos:
  .\aspire-run.ps1 -Build -Run    # Build e roda
  .\aspire-run.ps1 -Run           # Apenas roda (build automatico)
  .\aspire-run.ps1 -Clean         # Limpar

O que o Aspire faz:
  - Inicia PostgreSQL automaticamente (Docker)
  - Configura connection string
  - Abre dashboard com logs/metrics/traces
  - Hot reload habilitado
"@
}

function Test-DotnetVersion {
    try {
        $version = dotnet --version
        if ($version -notmatch "^9\.") {
            Write-Host "[AVISO] .NET 9 recomendado, encontrado: $version" -ForegroundColor Yellow
        }
        return $true
    } catch {
        Write-Host "[ERRO] .NET SDK nao encontrado!" -ForegroundColor Red
        Write-Host "Instale: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
        return $false
    }
}

function Test-DockerRunning {
    try {
        $null = docker ps 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host "[AVISO] Docker nao esta rodando!" -ForegroundColor Yellow
            Write-Host "Aspire precisa do Docker para rodar PostgreSQL" -ForegroundColor Yellow
            Write-Host "Inicie o Docker Desktop e tente novamente" -ForegroundColor Yellow
            return $false
        }
        return $true
    } catch {
        Write-Host "[AVISO] Docker nao encontrado!" -ForegroundColor Yellow
        return $false
    }
}

function Build-Projects {
    Write-Host "[BUILD] Building solucao..." -ForegroundColor Cyan
    dotnet build erp.sln -c Release
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Build concluido!" -ForegroundColor Green
    } else {
        Write-Host "[ERRO] Build falhou!" -ForegroundColor Red
        exit 1
    }
}

function Clean-Projects {
    Write-Host "[CLEAN] Limpando builds..." -ForegroundColor Cyan
    dotnet clean erp.sln
    Write-Host "[OK] Limpeza concluida!" -ForegroundColor Green
}

function Start-AspireHost {
    Write-Host "[ASPIRE] Iniciando Aspire AppHost..." -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Aguarde o Aspire Dashboard abrir automaticamente..." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Servicos que serao iniciados:" -ForegroundColor Cyan
    Write-Host "  - PostgreSQL (porta 5432)" -ForegroundColor White
    Write-Host "  - PgAdmin (porta 5050)" -ForegroundColor White
    Write-Host "  - Pillar ERP (porta 8081)" -ForegroundColor White
    Write-Host "  - Aspire Dashboard (porta automatica)" -ForegroundColor White
    Write-Host ""
    Write-Host "Pressione Ctrl+C para parar" -ForegroundColor Yellow
    Write-Host ""
    
    dotnet run --project Pillar.AppHost/Pillar.AppHost.csproj
}

# Main execution
if (-not (Test-DotnetVersion)) {
    exit 1
}

if (-not ($Build -or $Run -or $Clean)) {
    Show-Usage
    exit 0
}

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host ".NET Aspire - Pillar ERP" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

if ($Clean) {
    Clean-Projects
}

if ($Build) {
    Build-Projects
}

if ($Run) {
    if (-not (Test-DockerRunning)) {
        Write-Host ""
        Write-Host "Continuar mesmo assim? (s/N)" -ForegroundColor Yellow
        $confirm = Read-Host
        if ($confirm -ne 's' -and $confirm -ne 'S') {
            Write-Host "Operacao cancelada." -ForegroundColor Yellow
            exit 0
        }
    }
    
    Start-AspireHost
}

Write-Host ""
Write-Host "Concluido!" -ForegroundColor Green
