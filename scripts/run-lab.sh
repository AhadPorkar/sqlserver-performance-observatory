#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
[[ "${SPO_ACCEPT_SQL_EULA:-}" == Y ]] || { echo "Read and accept the SQL Server Developer license for a dedicated non-production lab." >&2; exit 1; }
: "${SPO_LAB_SA_PASSWORD:?Set a lab-only password}"
: "${SPO_LAB_READER_PASSWORD:?Set a random 24+ character reader password}"
export SPO_LAB_CONNECTION="Server=127.0.0.1,14333;Database=ObservatoryLab;User ID=sa;Password=$SPO_LAB_SA_PASSWORD;Encrypt=True;TrustServerCertificate=True"
export SPO_LAB_READER_CONNECTION="Server=127.0.0.1,14333;Database=ObservatoryLab;User ID=spo_lab_reader;Password=$SPO_LAB_READER_PASSWORD;Encrypt=True;TrustServerCertificate=True"
command="${1:-setup}"
shift || true
dotnet run --project tools/Observatory.Lab -- "$command" --accept-lab-only "$@"
