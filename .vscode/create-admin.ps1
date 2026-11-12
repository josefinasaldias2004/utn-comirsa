$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)

$env:PATH = "$HOME\.dotnet-local;$env:PATH"
$env:Bootstrap__Email = Read-Host 'Email de la cuenta local a activar'
$env:Conecta__LocalDatabase = Join-Path (Get-Location) 'backend\Conecta.Api\conecta.local.db'

try {
    $securePassword = Read-Host 'Nueva contraseña de administradora (12-128 caracteres)' -AsSecureString
    $env:Bootstrap__Password = [System.Net.NetworkCredential]::new('', $securePassword).Password
    dotnet run --project .vscode\PromoteLocalAdmin.csproj
    if ($LASTEXITCODE -ne 0) {
        throw "No se pudo activar la cuenta; no se confirmó ningún cambio."
    }
}
finally {
    Remove-Item Env:Bootstrap__Password, Env:Bootstrap__Email, Env:Conecta__LocalDatabase -ErrorAction SilentlyContinue
}
