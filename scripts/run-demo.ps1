$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw "Install .NET 10 SDK first." }
if ([string]::IsNullOrWhiteSpace($env:SPO_ACCESS_KEY)) {
    $bytes = New-Object byte[] 32
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    $env:SPO_ACCESS_KEY = [Convert]::ToBase64String($bytes)
    Write-Host "Temporary analyst access key (do not commit): $env:SPO_ACCESS_KEY"
}
$env:Observatory__Mode = "Replay"
$env:Observatory__PublicOrigin = "http://localhost:5080"
$env:ASPNETCORE_URLS = "http://localhost:5080"
Write-Host "Open http://localhost:5080 (synthetic replay, no SQL connection)."
dotnet run --project src/Observatory.Web --no-launch-profile
if ($LASTEXITCODE -ne 0) { throw "Application stopped with an error." }
