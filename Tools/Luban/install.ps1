[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$release = 'v4.11.0'
$expectedHash = '899703281BCCA3E22D78C44045FB08AC05A72ABB29428650A149ECA612779892'
$archive = Join-Path $PSScriptRoot 'Luban.7z'
$destination = Join-Path $PSScriptRoot 'official'
$sevenZip = 'C:\Program Files\7-Zip\7z.exe'

if (-not (Test-Path -LiteralPath $sevenZip)) {
    throw '7-Zip is required to extract Luban. Install 7zip.7zip, then run this script again.'
}

Invoke-WebRequest -Uri "https://github.com/focus-creative-games/luban/releases/download/$release/Luban.7z" -OutFile $archive
$actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $archive).Hash
if ($actualHash -ne $expectedHash) {
    throw "Unexpected Luban archive hash: $actualHash"
}

New-Item -ItemType Directory -Force -Path $destination | Out-Null
& $sevenZip x $archive ("-o" + $destination) -y | Out-Host
$luban = Join-Path $destination 'Luban/Luban.exe'
if (-not (Test-Path -LiteralPath $luban)) { throw 'Luban.exe was not found after extraction.' }
& $luban --version
