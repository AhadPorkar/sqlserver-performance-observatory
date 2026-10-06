#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
command -v dotnet >/dev/null || { echo "Install the .NET SDK in global.json." >&2; exit 1; }
dotnet restore Observatory.slnx
dotnet build Observatory.slnx -c Release --no-restore
dotnet test tests/Observatory.Tests/Observatory.Tests.csproj -c Release --no-build --logger trx --results-directory artifacts/tests
dotnet publish src/Observatory.Web/Observatory.Web.csproj -c Release --no-build -o artifacts/publish
