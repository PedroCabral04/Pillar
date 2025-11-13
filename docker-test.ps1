#!/usr/bin/env pwsh
# Script para testar o build Docker localmente antes do deploy no Coolify

param(
    [Parameter()]
    [switch]$Build,
    
    [Parameter()]
    [switch]$Run,
    
    [Parameter()]
    [switch]$Stop,
    
    [Parameter()]
    [switch]$Clean,
    
    [Parameter()]
    [switch]$Logs,
    
    [Parameter()]
    [switch]$Shell
)

$ErrorActionPreference = "Stop"

function Show-Usage {
    Write-Host @"
🐳 Pillar ERP - Docker Test Script

Uso: .\docker-test.ps1 [opções]

Opções:
  -Build    Builda a imagem Docker
  -Run      Sobe os containers (docker-compose up)
  -Stop     Para os containers
  -Clean    Remove containers, volumes e imagens
  -Logs     Mostra logs em tempo real
  -Shell    Abre shell no container da aplicação

Exemplos:
  .\docker-test.ps1 -Build -Run    # Build e roda
  .\docker-test.ps1 -Logs          # Ver logs
  .\docker-test.ps1 -Clean         # Limpar tudo
"@
}

function Test-DockerInstalled {
    try {
        docker --version | Out-Null
        docker-compose --version | Out-Null
        return $true
    } catch {
        Write-Host "❌ Docker ou Docker Compose não encontrado!" -ForegroundColor Red
        Write-Host "Instale Docker Desktop: https://www.docker.com/products/docker-desktop" -ForegroundColor Yellow
        return $false
    }
}

function Build-Image {
    Write-Host "🔨 Building Docker image..." -ForegroundColor Cyan
    docker-compose build --no-cache
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Build concluído com sucesso!" -ForegroundColor Green
    } else {
        Write-Host "❌ Build falhou!" -ForegroundColor Red
        exit 1
    }
}

function Start-Containers {
    Write-Host "🚀 Iniciando containers..." -ForegroundColor Cyan
    docker-compose up -d
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Containers iniciados!" -ForegroundColor Green
        Write-Host ""
        Write-Host "📍 Aplicação disponível em:" -ForegroundColor Yellow
        Write-Host "   http://localhost:8080" -ForegroundColor White
        Write-Host ""
        Write-Host "🔐 Credenciais padrão:" -ForegroundColor Yellow
        Write-Host "   Email: admin@erp.local" -ForegroundColor White
        Write-Host "   Senha: Admin@123!" -ForegroundColor White
        Write-Host ""
        Write-Host "📊 Monitorar logs:" -ForegroundColor Yellow
        Write-Host "   .\docker-test.ps1 -Logs" -ForegroundColor White
    } else {
        Write-Host "❌ Falha ao iniciar containers!" -ForegroundColor Red
        exit 1
    }
}

function Stop-Containers {
    Write-Host "🛑 Parando containers..." -ForegroundColor Cyan
    docker-compose down
    Write-Host "✅ Containers parados!" -ForegroundColor Green
}

function Clean-Everything {
    Write-Host "🧹 Limpando containers, volumes e imagens..." -ForegroundColor Cyan
    Write-Host "⚠️  Isso irá remover TODOS os dados!" -ForegroundColor Yellow
    $confirm = Read-Host "Continuar? (s/N)"
    
    if ($confirm -eq 's' -or $confirm -eq 'S') {
        docker-compose down -v --rmi all
        Write-Host "✅ Limpeza concluída!" -ForegroundColor Green
    } else {
        Write-Host "❌ Operação cancelada." -ForegroundColor Yellow
    }
}

function Show-Logs {
    Write-Host "📋 Mostrando logs (Ctrl+C para sair)..." -ForegroundColor Cyan
    docker-compose logs -f
}

function Open-Shell {
    Write-Host "🐚 Abrindo shell no container da aplicação..." -ForegroundColor Cyan
    docker-compose exec app /bin/bash
}

# Main execution
if (-not (Test-DockerInstalled)) {
    exit 1
}

if (-not ($Build -or $Run -or $Stop -or $Clean -or $Logs -or $Shell)) {
    Show-Usage
    exit 0
}

# Verifica se .env existe, se não, copia do exemplo
if (-not (Test-Path ".env")) {
    if (Test-Path ".env.example") {
        Write-Host "⚠️  Arquivo .env não encontrado. Copiando de .env.example..." -ForegroundColor Yellow
        Copy-Item ".env.example" ".env"
        Write-Host "✅ Arquivo .env criado! Edite as configurações se necessário." -ForegroundColor Green
    } else {
        Write-Host "⚠️  Arquivo .env não encontrado e .env.example também não existe!" -ForegroundColor Yellow
        Write-Host "Continuando com valores padrão..." -ForegroundColor Yellow
    }
}

# Execute requested operations
if ($Build) { Build-Image }
if ($Run) { Start-Containers }
if ($Stop) { Stop-Containers }
if ($Clean) { Clean-Everything }
if ($Logs) { Show-Logs }
if ($Shell) { Open-Shell }

Write-Host ""
Write-Host "✨ Concluído!" -ForegroundColor Green
