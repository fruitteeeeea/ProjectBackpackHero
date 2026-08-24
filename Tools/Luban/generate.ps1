[CmdletBinding()]
param(
    [string]$LubanCommand = $env:LUBAN_COMMAND,
    [switch]$Compatibility,
    [switch]$Native
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$tableRoot = Join-Path $PSScriptRoot 'tables'
$outputRoot = Join-Path $projectRoot 'Assets/Resources/Configs'
$nativeOutputRoot = Join-Path $outputRoot 'Luban'
$nativeCodeOutputRoot = Join-Path $projectRoot 'Assets/Scripts/Generated/Luban'
$officialLuban = Join-Path $PSScriptRoot 'official/Luban/Luban.exe'
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null

# Luban v4.11.0 is pinned by install.ps1. The game runtime consumes its generated C#
# tables and JSON payloads, so native generation is the normal authoring path.
if (Test-Path -LiteralPath $officialLuban) {
    & $officialLuban --version
    & node (Join-Path $PSScriptRoot 'build-schema-workbooks.mjs')
    if ($LASTEXITCODE -ne 0) { throw "Native schema preparation failed with exit code $LASTEXITCODE." }
    New-Item -ItemType Directory -Force -Path $nativeOutputRoot | Out-Null
    New-Item -ItemType Directory -Force -Path $nativeCodeOutputRoot | Out-Null
    & $officialLuban -t client -c cs-simple-json -d json --conf (Join-Path $PSScriptRoot 'luban.conf') `
        -x outputCodeDir=$nativeCodeOutputRoot -x outputDataDir=$nativeOutputRoot --validationFailAsError
    if ($LASTEXITCODE -ne 0) { throw "Official Luban generation failed with exit code $LASTEXITCODE." }
} elseif (-not [string]::IsNullOrWhiteSpace($LubanCommand)) {
    & $LubanCommand --version
    & node (Join-Path $PSScriptRoot 'build-schema-workbooks.mjs')
    if ($LASTEXITCODE -ne 0) { throw "Native schema preparation failed with exit code $LASTEXITCODE." }
    New-Item -ItemType Directory -Force -Path $nativeOutputRoot | Out-Null
    New-Item -ItemType Directory -Force -Path $nativeCodeOutputRoot | Out-Null
    & $LubanCommand -t client -c cs-simple-json -d json --conf (Join-Path $PSScriptRoot 'luban.conf') `
        -x outputCodeDir=$nativeCodeOutputRoot -x outputDataDir=$nativeOutputRoot --validationFailAsError
    if ($LASTEXITCODE -ne 0) { throw "Official Luban generation failed with exit code $LASTEXITCODE." }
} else {
    throw 'Native generation requires the official tool. Run Tools/Luban/install.ps1 first.'
}

if ($Compatibility) {
    Get-ChildItem -LiteralPath $tableRoot -Filter '*.csv' | ForEach-Object {
        $target = Join-Path $outputRoot ($_.BaseName + '.bytes')
        [System.IO.File]::WriteAllText($target, [System.IO.File]::ReadAllText($_.FullName), [System.Text.UTF8Encoding]::new($false))
        Write-Host "Generated compatibility payload $target"
    }
}
