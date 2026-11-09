$ErrorActionPreference = 'Stop'
$root = $PWD.Path
$spec = Get-Content (Join-Path $root 'obj/verificacion-vuelos/swagger-frontend.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$documentPath = Join-Path $root 'API_FRONTEND.md'
$document = [IO.File]::ReadAllText($documentPath)
$marker = '<!-- CONTRATOS_GENERADOS -->'
$prefix = $document.Substring(0, $document.IndexOf($marker))
$builder = [Text.StringBuilder]::new()
function Line([string]$value = '') { [void]$builder.AppendLine($value) }
function Escape([object]$value) { return ([string]$value).Replace('|','\|').Replace("`r",'').Replace("`n",' ') }
function TypeName($schema) {
    if ($null -eq $schema) { return 'Sin cuerpo' }
    if ($schema.'$ref') {
        $name = ($schema.'$ref' -split '/')[-1]
        $type = '[' + $name + '](#dto-' + $name.ToLowerInvariant() + ')'
    } elseif ($schema.type -eq 'array') { $type = (TypeName $schema.items) + '[]' }
    elseif ($schema.oneOf) { $type = ($schema.oneOf | ForEach-Object { TypeName $_ }) -join ' / ' }
    elseif ($schema.allOf) { $type = ($schema.allOf | ForEach-Object { TypeName $_ }) -join ' / ' }
    elseif ($schema.type) { $type = [string]$schema.type; if ($schema.format) { $type += ' (' + $schema.format + ')' } }
    else { $type = 'object' }
    if ($schema.nullable) { $type += ' o null' }
    return $type
}
function Rules($schema) {
    $result = @()
    foreach ($pair in @(@('minimum','mínimo'), @('maximum','máximo'), @('minLength','longitud mínima'), @('maxLength','longitud máxima'), @('minItems','mínimo de elementos'), @('maxItems','máximo de elementos'))) {
        $prop = $schema.PSObject.Properties[$pair[0]]
        if ($null -ne $prop -and -not ($pair[0] -eq 'minLength' -and $prop.Value -eq 0)) { $result += $pair[1] + ': ' + $prop.Value }
    }
    if ($schema.pattern) { $result += 'patrón: `' + $schema.pattern + '`' }
    if ($schema.enum) { $result += 'valores: ' + ($schema.enum -join ', ') }
    return Escape ($result -join '; ')
}
function JsonSchema($content) {
    if ($content.'application/json') { return $content.'application/json'.schema }
    $first = @($content.PSObject.Properties)[0]
    if ($first) { return $first.Value.schema }
    return $null
}
function Access([string]$path, [string]$method) {
    if ($path -eq '/api/pagos/paypal/webhook') { return 'Firma PayPal verificada; sin sesión' }
    if ($path -in @('/api/autenticacion/login','/api/usuarios/registro')) { return 'Público' }
    if ($path.StartsWith('/api/admin/')) { return 'Solo admin' }
    if ($path.StartsWith('/api/reservas/')) { return 'Cliente/admin; propietario' }
    if ($path -eq '/api/reservas' -and $method -eq 'get') { return 'Cliente/admin; solo propias' }
    return 'Cliente/admin'
}
$operations = @()
foreach ($path in $spec.paths.PSObject.Properties) {
    foreach ($operation in $path.Value.PSObject.Properties) {
        if ($operation.Name -notmatch '^(get|post|put|patch|delete)$') { continue }
        $operations += [pscustomobject]@{ Path=$path.Name; Method=$operation.Name; Data=$operation.Value; Tag=$operation.Value.tags[0] }
    }
}
$operations = @($operations | Sort-Object Tag,Path,Method)
Line '| Método | Endpoint | Acceso | Éxito |'
Line '|---|---|---|---|'
foreach ($op in $operations) {
    $success = @($op.Data.responses.PSObject.Properties | Where-Object Name -Match '^2' | ForEach-Object Name) -join ', '
    Line ('| ' + $op.Method.ToUpperInvariant() + ' | `' + $op.Path + '` | ' + (Access $op.Path $op.Method) + ' | ' + $success + ' |')
}
Line
Line '## 9. Contrato HTTP por operación'
Line
Line 'Los nombres de query y propiedades siguientes proceden del Swagger de esta versión. Se muestran los contratos completos en el apartado 10. Las validaciones condicionales, propiedad, vigencia y dependencias se describen en los apartados anteriores y también se aplican aunque un campo no figure como requerido estático.'
Line
foreach ($op in $operations) {
    Line ('### ' + $op.Method.ToUpperInvariant() + ' ' + $op.Path)
    Line
    Line ('**Acceso:** ' + (Access $op.Path $op.Method) + '. **Grupo Swagger:** ' + $op.Tag + '.')
    Line
    if ($op.Data.parameters.Count -gt 0) {
        Line '| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |'
        Line '|---|---|---|---|---|'
        foreach ($p in $op.Data.parameters) {
            $required = if ($p.required) { 'Sí' } else { 'No' }
            Line ('| `' + $p.name + '` | ' + $p.in + ' | ' + (TypeName $p.schema) + ' | ' + $required + ' | ' + (Rules $p.schema) + ' |')
        }
        Line
    }
    if ($op.Data.requestBody) {
        $multipart = $op.Data.requestBody.content.'multipart/form-data'
        if ($multipart) {
            Line '**Cuerpo:** multipart/form-data (archivo y campos de formulario; no JSON).'
            Line
            Line '| Campo de formulario | Tipo | Obligatorio | Restricciones |'
            Line '|---|---|---|---|'
            foreach ($field in $multipart.schema.properties.PSObject.Properties) {
                $required = if ($multipart.schema.required -contains $field.Name -or $field.Value.minimum -ge 1) { 'Sí' } else { 'Opcional' }
                Line ('| `' + $field.Name + '` | ' + (TypeName $field.Value) + ' | ' + $required + ' | ' + (Rules $field.Value) + ' |')
            }
        } else {
            Line ('**Cuerpo JSON:** ' + (TypeName (JsonSchema $op.Data.requestBody.content)) + '.')
        }
    } else { Line '**Cuerpo:** no enviar cuerpo.' }
    Line
    Line '| HTTP declarado | Respuesta |'
    Line '|---|---|'
    foreach ($response in @($op.Data.responses.PSObject.Properties | Sort-Object Name)) {
        $shape = if ($response.Name -eq '204') { 'Sin cuerpo' } else { TypeName (JsonSchema $response.Value.content) }
        Line ('| ' + $response.Name + ' | ' + $shape + ' |')
    }
    Line
}
Line '## 10. Contratos JSON: campos y restricciones'
Line
Line 'Los contratos siguientes se obtuvieron del Swagger generado. En solicitudes, «Sí» significa campo requerido del esquema; «Sí por validación» identifica valores positivos no anulables cuyo valor omitido sería rechazado. Hay requisitos condicionales adicionales: por ejemplo, rowVersion al cambiar/cancelar reservas y tarifas por clase al crear un vuelo. Un string requerido no debe estar vacío ni compuesto solo por espacios.'
Line
Line 'Para respuestas, «Devuelto» describe propiedades del DTO, no campos que deba enviar el cliente. Valores anulables, colecciones vacías y comprobante ausente son casos válidos. ProblemDetails/ValidationProblemDetails pueden omitir propiedades y añadir extensiones. Los formatos numéricos son los publicados por Swagger; los importes del servidor utilizan decimal.'
Line
foreach ($entry in @($spec.components.schemas.PSObject.Properties | Sort-Object Name)) {
    $name = $entry.Name; $schema = $entry.Value
    Line ('### DTO ' + $name)
    Line
    $isRequest = $name.EndsWith('Request')
    Line '| Campo JSON | Tipo | Presencia | Restricciones estáticas |'
    Line '|---|---|---|---|'
    foreach ($property in $schema.properties.PSObject.Properties) {
        $p = $property.Value
        $presence = if (-not $isRequest) { 'Devuelto' } elseif ($schema.required -contains $property.Name) { 'Sí' } elseif ($p.minimum -ge 1 -and -not $p.nullable) { 'Sí por validación' } else { 'Opcional/condicional' }
        $type = TypeName $p
        if ($name -eq 'ReservaResponse' -and $property.Name -eq 'compra') { $type += ' o null' }
        Line ('| `' + $property.Name + '` | ' + $type + ' | ' + $presence + ' | ' + (Rules $p) + ' |')
    }
    Line
}
Line '## 11. Validación de esta entrega'
Line
Line ('Se verificaron ' + $operations.Count + ' operaciones y ' + @($spec.components.schemas.PSObject.Properties).Count + ' contratos contra el documento Swagger generado por la configuración real de Program.cs, sin conectar a SQL Server. Los Controllers y servicios se revisaron para documentar reglas no expresadas completamente por el esquema. No se instalaron dependencias ni se modificó la base. Este archivo no sustituye las pruebas de integración con datos y tokens reales.')
$output = $prefix + $builder.ToString()
[IO.File]::WriteAllText($documentPath, $output, [Text.UTF8Encoding]::new($false))
$routeLines = @($output -split "`r?`n" | Where-Object { $_ -match '^### (GET|POST|PUT|PATCH|DELETE) /api/' })
if ($routeLines.Count -ne $operations.Count) { throw 'El inventario no coincide con las operaciones' }
foreach ($op in $operations) {
    if (-not $output.Contains('### ' + $op.Method.ToUpperInvariant() + ' ' + $op.Path)) { throw ('Falta endpoint: ' + $op.Path) }
}
foreach ($schema in $spec.components.schemas.PSObject.Properties.Name) {
    if (-not $output.Contains('### DTO ' + $schema)) { throw ('Falta contrato: ' + $schema) }
}
$fences = @($output -split "`r?`n" | Where-Object { $_ -match '^```' }).Count
if ($fences % 2 -ne 0) { throw 'Bloques de código desbalanceados' }
Write-Output ('Validado: ' + $operations.Count + ' endpoints, ' + @($spec.components.schemas.PSObject.Properties).Count + ' contratos; ' + ($output -split "`r?`n").Count + ' líneas.')
