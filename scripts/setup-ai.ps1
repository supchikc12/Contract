# Contract — первичная настройка AI-ассистента (OpenCode + MCP для Unity)
#
# Запуск:
#   powershell -ExecutionPolicy Bypass -File .\scripts\setup-ai.ps1
#
# Скрипт идемпотентен: можно запускать повторно.

#requires -Version 5.1

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent

Write-Host ""
Write-Host "=== Contract: настройка AI-ассистента ===" -ForegroundColor Cyan
Write-Host ""

# --- 1. uv -------------------------------------------------------------------
$uvBin = Join-Path $env:USERPROFILE ".local\bin"
$uvExe = Join-Path $uvBin "uv.exe"
$uvxExe = Join-Path $uvBin "uvx.exe"

if (-not (Test-Path $uvExe)) {
    $cmd = Get-Command uv -ErrorAction SilentlyContinue
    if ($cmd) {
        $uvExe = $cmd.Source
        $uvxExe = Join-Path (Split-Path $uvExe) "uvx.exe"
    }
}

if (-not (Test-Path $uvExe)) {
    Write-Host "[1/4] uv не найден — устанавливаю..." -ForegroundColor Yellow
    $ProgressPreference = "SilentlyContinue"
    Invoke-RestMethod https://astral.sh/uv/install.ps1 | Invoke-Expression
} else {
    Write-Host "[1/4] uv уже установлен" -ForegroundColor Green
}

if (-not (Test-Path $uvxExe)) {
    throw "uvx не найден. Установи uv вручную: https://docs.astral.sh/uv/"
}
Write-Host ("      " + (& $uvExe --version))

# uv ставится в %USERPROFILE%\.local\bin — добавляем в пользовательский PATH,
# иначе OpenCode не сможет запустить `uvx` из opencode.json.
$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($userPath -notlike "*$uvBin*") {
    [Environment]::SetEnvironmentVariable("Path", "$uvBin;$userPath", "User")
    Write-Host "      $uvBin добавлен в PATH (нужен перезапуск OpenCode)" -ForegroundColor Yellow
}
$env:Path = "$uvBin;$env:Path"

# --- 2. MCP-сервер -----------------------------------------------------------
Write-Host "[2/4] Проверяю MCP-сервер (первый запуск скачивает пакет)..." -ForegroundColor Yellow
try {
    & $uvxExe --from mcpforunityserver mcp-for-unity --help *> $null
} catch {
    # --help завершается с кодом 1 на некоторых сборках — это не ошибка
}
Write-Host "      mcp-for-unity доступен" -ForegroundColor Green

# --- 3. вендорный пакет Unity ------------------------------------------------
$pkg = Join-Path $root "Packages\com.coplaydev.unity-mcp\package.json"
if (-not (Test-Path $pkg)) {
    throw "Не найден Packages\com.coplaydev.unity-mcp — репозиторий склонирован не полностью"
}
$pkgVersion = (Get-Content $pkg -Raw | ConvertFrom-Json).version
Write-Host "[3/4] Пакет MCP в проекте (v$pkgVersion)" -ForegroundColor Green

# --- 4. Unity: транспорт stdio ----------------------------------------------
# По умолчанию плагин использует HTTP и порт 8080, который часто занят.
# Переключаем EditorPrefs на stdio (TCP 6400) — тот же режим, что в opencode.json.
function Get-UnityPrefHash([string]$name) {
    $h = [uint64]5381
    foreach ($ch in $name.ToCharArray()) {
        $h = ((($h * 33) -bxor [uint64][int][char]$ch) % 4294967296)
    }
    return $h
}

$unity = Get-Process -Name Unity -ErrorAction SilentlyContinue
if ($unity) {
    Write-Host "[4/4] Unity запущен — переключи транспорт вручную:" -ForegroundColor Yellow
    Write-Host "      Window -> MCP for Unity -> Connection -> Stdio" -ForegroundColor Yellow
} else {
    $hive = "HKCU:\Software\Unity Technologies\Unity Editor 5.x"
    if (-not (Test-Path $hive)) { New-Item -Path $hive -Force | Out-Null }
    $prefName = "MCPForUnity.UseHttpTransport_h$(Get-UnityPrefHash 'MCPForUnity.UseHttpTransport')"
    New-ItemProperty -Path $hive -Name $prefName -PropertyType DWord -Value 0 -Force | Out-Null
    Write-Host "[4/4] Транспорт Unity переключён на stdio (TCP 6400)" -ForegroundColor Green
}

Write-Host ""
Write-Host "Готово. Дальше:" -ForegroundColor Cyan
Write-Host "  1. Открой папку проекта в OpenCode: $root"
Write-Host "  2. Войди в аккаунт — команда /connect"
Write-Host "  3. Открой проект в Unity 6000.0.22f1 (если был открыт — перезапусти)"
Write-Host "  4. Напиши в чат: «Проверь связь с Unity»"
Write-Host ""
