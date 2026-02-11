# Script para reinicializar la base de datos
# Esto eliminará todos los datos existentes y creará una base de datos nueva con todos los problemas

Write-Host "⚠️  ADVERTENCIA: Este script eliminará todos los datos de la base de datos." -ForegroundColor Yellow
Write-Host "Presiona Ctrl+C para cancelar, o Enter para continuar..." -ForegroundColor Yellow
Read-Host

# Ruta completa a la base de datos
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$dbPath = Join-Path $scriptPath "dorja.db"
$dbBackupPath = Join-Path $scriptPath "dorja.db.backup"

Write-Host "Buscando base de datos en: $dbPath" -ForegroundColor Cyan

# Backup de la base de datos actual
if (Test-Path $dbPath) {
    Write-Host "Creando backup de la base de datos actual..." -ForegroundColor Cyan
    Copy-Item $dbPath $dbBackupPath -Force
    Write-Host "Backup creado: $dbBackupPath" -ForegroundColor Green
    
    Write-Host "Eliminando base de datos actual..." -ForegroundColor Cyan
    Remove-Item $dbPath -Force
    Write-Host "✅ Base de datos eliminada exitosamente." -ForegroundColor Green
} else {
    Write-Host "⚠️  No se encontró la base de datos en: $dbPath" -ForegroundColor Yellow
    Write-Host "   Se creará una nueva cuando inicies el servidor." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "✅ La base de datos se reinicializará automáticamente cuando inicies el servidor." -ForegroundColor Green
Write-Host "   Ejecuta el servidor y la base de datos se creará con todos los problemas." -ForegroundColor Green
Write-Host ""
Write-Host "📝 IMPORTANTE: Asegúrate de que el servidor esté detenido antes de ejecutar este script." -ForegroundColor Yellow

