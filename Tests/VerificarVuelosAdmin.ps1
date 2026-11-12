param([string]$ProjectRoot = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference = 'Stop'
$ProjectRoot = [IO.Path]::GetFullPath($ProjectRoot)
# Alias compatible del comando anterior: ejecuta toda la suite PostgreSQL offline.
$script = Get-Content -LiteralPath (Join-Path $ProjectRoot 'Tests/VerificarModeloPostgreSql.ps1') -Raw -Encoding UTF8
& ([scriptblock]::Create($script)) -ProjectRoot $ProjectRoot -Comprobacion Completa
