param(
    [Parameter(Mandatory)]
    [string]$Ejecutable,

    [string]$Datos,
    [string]$Escucha = '127.0.0.1:8090'
)

$ErrorActionPreference = 'Stop'
$ejecutableCompleto = (Resolve-Path -LiteralPath $Ejecutable).Path
$raizProyecto = Split-Path -Parent $PSScriptRoot

if (-not $env:PocketBase__Clave) {
    $proyecto = [xml](Get-Content -LiteralPath (Join-Path $raizProyecto 'VentasMVC.csproj') -Raw)
    $idSecretos = $proyecto.Project.PropertyGroup.UserSecretsId
    $archivo = Join-Path $env:APPDATA "Microsoft/UserSecrets/$idSecretos/secrets.json"
    if (Test-Path -LiteralPath $archivo) {
        $secretos = Get-Content -LiteralPath $archivo -Raw | ConvertFrom-Json
        $env:PocketBase__Clave = $secretos.'PocketBase:Clave'
    }
}

if (-not $env:PocketBase__Clave) {
    throw 'Configura PocketBase:Clave en los secretos de VentasMVC o define PocketBase__Clave.'
}

if (-not $Datos) {
    $Datos = Join-Path (Split-Path -Parent $ejecutableCompleto) 'pb_data'
}

& $ejecutableCompleto serve "--http=$Escucha" "--dir=$Datos" `
    "--hooksDir=$(Join-Path $PSScriptRoot 'pb_hooks')" "--migrationsDir=$(Join-Path $PSScriptRoot 'pb_migrations')"
exit $LASTEXITCODE
