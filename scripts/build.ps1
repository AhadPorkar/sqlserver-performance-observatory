$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw "Install the .NET SDK specified in global.json." }
dotnet restore Observatory.slnx
if ($LASTEXITCODE -ne 0) { throw "Restore failed." }
dotnet build Observatory.slnx -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "Build failed." }
dotnet test tests/Observatory.Tests/Observatory.Tests.csproj -c Release --no-build --logger trx --results-directory artifacts/tests
if ($LASTEXITCODE -ne 0) { throw "Tests failed." }
dotnet publish src/Observatory.Web/Observatory.Web.csproj -c Release --no-build -o artifacts/publish
if ($LASTEXITCODE -ne 0) { throw "Publish failed." }
