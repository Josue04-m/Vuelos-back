param([string]$ProjectRoot = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference = 'Stop'
$ProjectRoot = [IO.Path]::GetFullPath($ProjectRoot)
$outputDirectory = Join-Path $ProjectRoot 'obj/verificacion-vuelos'
& dotnet build (Join-Path $ProjectRoot 'Vuelos_Backend.csproj') --no-restore --verbosity minimal --output $outputDirectory -p:UseAppHost=false
if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación de la API.' }
$sdk = @(& dotnet --list-sdks | Where-Object { $_ -match '^10\.' })[-1]
if (-not $sdk) { throw 'Se requiere el SDK .NET 10.' }
$sdkVersion = ($sdk -split ' ')[0]
$sdkDirectory = [regex]::Match($sdk, '\[(.+)\]').Groups[1].Value
$dotnetDirectory = Split-Path $sdkDirectory -Parent
$references = @{}
foreach ($pack in 'Microsoft.NETCore.App.Ref', 'Microsoft.AspNetCore.App.Ref') {
    $packRoot = Join-Path $dotnetDirectory "packs/$pack"
    $version = Get-ChildItem -LiteralPath $packRoot -Directory | Where-Object Name -Match '^10\.' | Sort-Object { [version]$_.Name } | Select-Object -Last 1
    Get-ChildItem -LiteralPath (Join-Path $version.FullName 'ref/net10.0') -Filter '*.dll' | ForEach-Object { $references[$_.Name] = $_.FullName }
}
Get-ChildItem -LiteralPath $outputDirectory -Filter '*.dll' | ForEach-Object {
    if (-not $references.ContainsKey($_.Name) -and $_.Name -ne 'VerificarVuelosAdmin.dll') { $references[$_.Name] = $_.FullName }
}
$responseFile = Join-Path $outputDirectory 'checks.rsp'
$testDll = Join-Path $outputDirectory 'VerificarVuelosAdmin.dll'
$arguments = @('-nologo', '-target:exe', '-nullable:enable', '-langversion:preview', '-nowarn:1701', ('-out:"' + $testDll + '"'))
$arguments += @($references.Values | ForEach-Object { '-r:"' + $_ + '"' })
$arguments += @(Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'Tests') -Filter '*Checks.cs.txt' | ForEach-Object { '"' + $_.FullName + '"' })
[IO.File]::WriteAllLines($responseFile, $arguments)
& dotnet (Join-Path $sdkDirectory "$sdkVersion/Roslyn/bincore/csc.dll") "@$responseFile"
if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación de las comprobaciones.' }
& dotnet exec --runtimeconfig (Join-Path $outputDirectory 'Vuelos_Backend.runtimeconfig.json') --depsfile (Join-Path $outputDirectory 'Vuelos_Backend.deps.json') $testDll
if ($LASTEXITCODE -ne 0) { throw 'Fallaron las comprobaciones.' }
