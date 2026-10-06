# Getting started

[Project](../../README.md) | [Deutsch](../de/getting-started.md)

## Prerequisites
Install the .NET 10 SDK specified in [global.json](../../global.json). The pin is 10.0.401 (C# 14 / ASP.NET Core 10); NuGet restore needs access to your approved package source. Node 22+ and Python 3.11+ are optional for non-.NET tests. SQL Server is **not** required for the synthetic replay.

No `gh`, `winget`, npm package installation or frontend build is required to run the UI. The repository contains source, not SDKs, cached dependencies or a qualified compiled binary.

## Windows PowerShell
Extract the project so `D:\GITHUB\sqlserver-performance-observatory\Observatory.slnx` exists.

```powershell
Set-Location 'D:\GITHUB\sqlserver-performance-observatory'
dotnet --info
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
if ($LASTEXITCODE -ne 0) { throw 'Build or tests failed. Stop here.' }
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\run-demo.ps1
```

The script generates a random temporary workspace access key when none is supplied and prints it once. Open `http://localhost:5080` and use that key. The browser starts in English; choose Deutsch or Persian in the selector. The key is not saved to the repository. A new generated key invalidates old login sessions.

The process-level execution policy option does not modify machine-wide policy and does not override organizational Group Policy. Review scripts before execution.

## Linux/macOS
```bash
bash scripts/build.sh
bash scripts/run-demo.sh
```

Use `localhost`, not a different host alias: the origin/Host checks are deliberate. Stop with Ctrl+C. The replay banner, timestamps and figures stay synthetic; refreshing does not turn them into measurements.

## Docker replay
Generate and export a random `SPO_ACCESS_KEY`, then run `docker compose up --build`. Only `127.0.0.1:5080` is published. Docker execution has not been locally verified for this delivery. The image build restores NuGet packages and requires internet or a configured mirror.

For real SQL connections, read [permissions](permissions.md), [configuration](configuration.md), [security](security.md) and [acceptance](testing.md) first.
