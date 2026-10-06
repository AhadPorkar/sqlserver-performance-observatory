param([ValidateSet("setup", "benchmark", "blocking", "deadlock", "verify-reader")][string]$Command = "setup", [switch]$AlignIntervals)
$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)
if ($env:SPO_ACCEPT_SQL_EULA -ne "Y") { throw "Read the SQL Server Developer license and set SPO_ACCEPT_SQL_EULA=Y for a dedicated non-production lab." }
if ([string]::IsNullOrWhiteSpace($env:SPO_LAB_SA_PASSWORD) -or [string]::IsNullOrWhiteSpace($env:SPO_LAB_READER_PASSWORD)) { throw "Set independent strong SPO_LAB_SA_PASSWORD and SPO_LAB_READER_PASSWORD secrets (reader: 24+ characters)." }
$env:SPO_LAB_CONNECTION = "Server=127.0.0.1,14333;Database=ObservatoryLab;User ID=sa;Password=$($env:SPO_LAB_SA_PASSWORD);Encrypt=True;TrustServerCertificate=True"
$env:SPO_LAB_READER_CONNECTION = "Server=127.0.0.1,14333;Database=ObservatoryLab;User ID=spo_lab_reader;Password=$($env:SPO_LAB_READER_PASSWORD);Encrypt=True;TrustServerCertificate=True"
$Arguments = @("run", "--project", "tools/Observatory.Lab", "--", $Command, "--accept-lab-only")
if ($AlignIntervals) { $Arguments += "--align-intervals" }
& dotnet @Arguments
if ($LASTEXITCODE -ne 0) { throw "Lab command failed. Keep logs; do not report the experiment as passed." }
