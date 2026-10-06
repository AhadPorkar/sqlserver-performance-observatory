#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
command -v dotnet >/dev/null || { echo "Install .NET 10 SDK first." >&2; exit 1; }
if [[ -z "${SPO_ACCESS_KEY:-}" ]]; then
  command -v openssl >/dev/null || { echo "Set SPO_ACCESS_KEY to a random 32+ character secret." >&2; exit 1; }
  export SPO_ACCESS_KEY="$(openssl rand -hex 32)"
  printf '\nTemporary analyst access key (do not commit):\n%s\n\n' "$SPO_ACCESS_KEY"
fi
export Observatory__Mode=Replay
export Observatory__PublicOrigin=http://localhost:5080
export ASPNETCORE_URLS=http://localhost:5080
printf 'Open http://localhost:5080 (synthetic replay, no SQL connection).\n'
dotnet run --project src/Observatory.Web --no-launch-profile
