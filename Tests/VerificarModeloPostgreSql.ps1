param([string]$ProjectRoot = (Split-Path $PSScriptRoot -Parent),
    [ValidateSet('Modelo','Persistencia','Migracion','Completa','Integracion','UsuariosLaboratorio')][string]$Comprobacion = 'Modelo',
    [switch]$SoloCompilar)
$ErrorActionPreference = 'Stop'
$ProjectRoot = [IO.Path]::GetFullPath($ProjectRoot)
$outputDirectory = Join-Path $ProjectRoot 'obj/verificacion-supabase'

# Compila con los paquetes que el equipo ya haya instalado. Nunca restaura.
& dotnet build (Join-Path $ProjectRoot 'Vuelos_Backend.csproj') --no-restore --verbosity minimal --output $outputDirectory -p:UseAppHost=false
if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar. Compruebe que Npgsql esté instalado y restaurado manualmente.' }
$sdk = @(& dotnet --list-sdks | Where-Object { $_ -match '^10\.' })[-1]
if (-not $sdk) { throw 'Se requiere SDK .NET 10.' }
$sdkVersion = ($sdk -split ' ')[0]
$sdkDirectory = [regex]::Match($sdk, '\[(.+)\]').Groups[1].Value
$dotnetDirectory = Split-Path $sdkDirectory -Parent
$references = @{}
foreach ($pack in 'Microsoft.NETCore.App.Ref', 'Microsoft.AspNetCore.App.Ref') {
    $packRoot = Join-Path $dotnetDirectory "packs/$pack"
    $version = Get-ChildItem -LiteralPath $packRoot -Directory | Where-Object Name -Match '^10\.' | Sort-Object { [version]$_.Name } | Select-Object -Last 1
    Get-ChildItem -LiteralPath (Join-Path $version.FullName 'ref/net10.0') -Filter '*.dll' | ForEach-Object { $references[$_.Name] = $_.FullName }
}
Get-ChildItem -LiteralPath $outputDirectory -Filter '*.dll' | Where-Object Name -notin @('ModeloPostgreSql.dll','PersistenciaPostgreSql.dll','MigracionPostgreSql.dll','CompletaPostgreSql.dll','IntegracionPostgreSql.dll','UsuariosLaboratorioPostgreSql.dll') | ForEach-Object {
    if (-not $references.ContainsKey($_.Name)) { $references[$_.Name] = $_.FullName }
}
$entryPoint = $Comprobacion + 'PostgreSql'
$responseFile = Join-Path $outputDirectory ($entryPoint + '.rsp')
$testDll = Join-Path $outputDirectory ($entryPoint + '.dll')
$arguments = @('-nologo', '-target:exe', '-nullable:enable', '-langversion:preview', '-nowarn:1701', ('-main:' + $entryPoint), ('-out:"' + $testDll + '"'))
$arguments += @($references.Values | ForEach-Object { '-r:"' + $_ + '"' })
$arguments += '"' + (Join-Path $ProjectRoot "Tests/$entryPoint.cs.txt") + '"'
$arguments += '"' + (Join-Path $ProjectRoot 'Tests/SoportePostgreSql.cs.txt') + '"'
if ($Comprobacion -in @('Persistencia','Completa')) {
    # Reutiliza fixtures y pruebas independientes del proveedor. No ejecuta el Main SQL Server.
    $arguments += @(Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'Tests') -Filter '*Checks.cs.txt' | ForEach-Object { '"' + $_.FullName + '"' })
}
if ($Comprobacion -eq 'Completa') {
    $arguments += @('ModeloPostgreSql','MigracionPostgreSql','PersistenciaPostgreSql' | ForEach-Object { '"' + (Join-Path $ProjectRoot "Tests/$_.cs.txt") + '"' })
}
[IO.File]::WriteAllLines($responseFile, $arguments)
& dotnet (Join-Path $sdkDirectory "$sdkVersion/Roslyn/bincore/csc.dll") "@$responseFile"
if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación de las comprobaciones PostgreSQL.' }
if ($SoloCompilar) { Write-Output "Compilación de $entryPoint correcta; no se ejecutaron pruebas."; return }
& dotnet exec --runtimeconfig (Join-Path $outputDirectory 'Vuelos_Backend.runtimeconfig.json') --depsfile (Join-Path $outputDirectory 'Vuelos_Backend.deps.json') $testDll
if ($LASTEXITCODE -eq 2 -and $Comprobacion -eq 'Integracion') { throw 'Integración NO EJECUTADA: faltan PNA_TEST_CONNECTION y/o PNA_TESTS_CONFIRM=BASE_DE_PRUEBAS. Revisar docs/SUPABASE_ETAPA_5.md.' }
if ($LASTEXITCODE -ne 0) { throw 'Fallaron las comprobaciones PostgreSQL.' }
