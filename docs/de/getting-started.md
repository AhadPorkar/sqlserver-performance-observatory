# Erste Schritte

[Projekt](../../README.de.md) | [English](../en/getting-started.md)

Benötigt wird das .NET-10-SDK aus [global.json](../../global.json), derzeit 10.0.401. Der erste Restore benötigt einen erreichbaren NuGet-Feed. Node 22+ und Python 3.11+ sind nur für zusätzliche Tests erforderlich. Für die synthetische Wiedergabe ist kein SQL Server notwendig.

```powershell
Set-Location 'D:\GITHUB\sqlserver-performance-observatory'
dotnet --info
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
if ($LASTEXITCODE -ne 0) { throw 'Build oder Tests fehlgeschlagen.' }
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\run-demo.ps1
```

Das Startskript erzeugt bei Bedarf einen zufälligen Zugangsschlüssel und zeigt ihn im Terminal. Unter `http://localhost:5080` anmelden. Englisch ist die Standardsprache; Deutsch und Persisch sind umschaltbar. Kein Schlüssel wird im Repository gespeichert.

Unter Linux/macOS `bash scripts/build.sh` und `bash scripts/run-demo.sh` ausführen. Der Hostname muss zur konfigurierten Origin passen. Ctrl+C beendet die Anwendung.

Die Ausführungsrichtlinie im gezeigten PowerShell-Aufruf gilt nur für diesen Prozess; Unternehmensrichtlinien bleiben vorrangig. Skripte vor Ausführung lesen.

Die Wiedergabe bleibt als synthetisch gekennzeichnet. Sie ist kein Benchmark. Das Archiv enthält weder das SDK noch NuGet-Caches oder eine bestätigte Binärdatei. Docker ist vorbereitet, aber in dieser Lieferung nicht ausgeführt worden. Vor Echtbetrieb [Berechtigungen](permissions.md), [Konfiguration](configuration.md) und [Sicherheit](security.md) lesen.
