# Contract — настройка Unity SmartMerge (UnityYAMLMerge) для git
#
# Запуск из корня проекта:
#   powershell -ExecutionPolicy Bypass -File .\scripts\setup-git-merge.ps1
#
# Скрипт идемпотентен: можно запускать повторно.
# Настраивает ТОЛЬКО локальный git-конфиг этой машины; .gitattributes лежит в репозитории.

#requires -Version 5.1

param(
    [string]$UnityEditorPath = "E:\project\unt\6000.6.8f1\Editor\Data\Tools\UnityYAMLMerge.exe"
)

$ErrorActionPreference = "Stop"
$repo = Split-Path $PSScriptRoot -Parent

if (-not (Test-Path $UnityEditorPath)) {
    $candidates = Get-ChildItem "E:\project\unt" -Directory -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending |
        ForEach-Object { Join-Path $_.FullName "Editor\Data\Tools\UnityYAMLMerge.exe" } |
        Where-Object { Test-Path $_ }

    $UnityEditorPath = $candidates | Select-Object -First 1
}

if (-not $UnityEditorPath -or -not (Test-Path $UnityEditorPath)) {
    throw "UnityYAMLMerge.exe не найден. Укажи путь: -UnityEditorPath `"<...>\UnityYAMLMerge.exe`""
}

$exe = $UnityEditorPath -replace '\\', '/'
$driver = 'merge -p %O %B %A %A'
$mergeTool = 'merge -p $BASE $REMOTE $LOCAL $MERGED'

# PowerShell 5.1 съедает кавычки при передаче нативных аргументов,
# поэтому git вызывается через ProcessStartInfo с готовой командной строкой.
function Set-GitConfigRaw {
    param([string]$ArgumentLine)

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = "git.exe"
    $psi.Arguments = $ArgumentLine
    $psi.WorkingDirectory = $repo
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true

    $process = [System.Diagnostics.Process]::Start($psi)
    $stdout = $process.StandardOutput.ReadToEnd()
    $stderr = $process.StandardError.ReadToEnd()
    $process.WaitForExit()

    if ($process.ExitCode -ne 0) {
        throw "git $ArgumentLine`n$stderr"
    }
    if ($stdout.Trim()) { Write-Host $stdout.TrimEnd() }
}

Write-Host "UnityYAMLMerge: $UnityEditorPath" -ForegroundColor Cyan

Set-GitConfigRaw 'config merge.unityyamlmerge.name "Unity SmartMerge"'
Set-GitConfigRaw ('config merge.unityyamlmerge.driver "\"' + $exe + '\" ' + $driver + '"')
Set-GitConfigRaw 'config merge.unityyamlmerge.recursive binary'
Set-GitConfigRaw ('config mergetool.unityyamlmerge.cmd "\"' + $exe + '\" ' + $mergeTool + '"')
Set-GitConfigRaw 'config mergetool.unityyamlmerge.trustExitCode true'

if (-not (Test-Path (Join-Path $repo ".gitattributes"))) {
    Write-Host "ВНИМАНИЕ: в репозитории нет .gitattributes — правила merge не применятся." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Текущая конфигурация:" -ForegroundColor Green
& git -C $repo config --get-regexp '^merge\.unityyamlmerge|^mergetool\.unityyamlmerge'
