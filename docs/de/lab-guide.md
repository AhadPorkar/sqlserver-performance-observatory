# Reproduzierbares SQL-Server-Labor

[Projekt](../../README.de.md) | [English](../en/lab-guide.md)

## Ausschließlich isolierte Testumgebung
Das Labor schreibt absichtlich Daten. Es ist ein separates Programm und wird nicht vom Dashboard aufgerufen. Erforderlich sind `--accept-lab-only`, ein erlaubter lokaler Hostname, der exakte Datenbankname `ObservatoryLab`, Developer Edition und eine Projektmarkierung. Eine Weiterleitung eines lokalen Ports auf einen fremden Server bleibt in der Verantwortung des Betreibers.

Die SQL-Server-Developer-Lizenz muss für eine nicht produktive Umgebung akzeptiert werden. Mit Docker Compose startet `compose.lab.yml` eine auf `127.0.0.1:14333` begrenzte Instanz. Zufällige unabhängige Passwörter für `SPO_LAB_SA_PASSWORD` und `SPO_LAB_READER_PASSWORD` setzen (Reader mindestens 24 Zeichen), danach:

```powershell
$env:SPO_ACCEPT_SQL_EULA = 'Y'
docker compose -f compose.lab.yml up -d --wait --wait-timeout 300
if ($LASTEXITCODE -ne 0) { throw 'Laborstart fehlgeschlagen.' }
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\run-lab.ps1 -Command setup
if ($LASTEXITCODE -ne 0) { throw 'Setup fehlgeschlagen.' }
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\run-lab.ps1 -Command benchmark -AlignIntervals
if ($LASTEXITCODE -ne 0) { throw 'Benchmark fehlgeschlagen.' }
```

Die Passwörter für die Lebensdauer des Volumes behalten. Setup rotiert ein bestehendes Passwort nicht automatisch. Eine vorhandene Datenbank ohne Projektmarkierung wird nicht übernommen.

## Experiment
100.000 synthetische Aufträge, identische Parameterfolge, fünf Aufwärm- und dreißig Messausführungen je Phase: Index vorhanden, Index entfernt, Index wiederhergestellt. Die Ergebnisse müssen identisch bleiben. Der Index wird bei gewöhnlichen Fehlern in `finally` wiederhergestellt; ein harter Prozessabbruch kann das verhindern.

Gemessen werden individuelle Client-Laufzeiten und logische Reads aus `STATISTICS IO`. Median/p95 beruhen auf diesen Einzelmessungen, nicht auf Query-Store-Mittelwerten. Es gibt keinen globalen Cache-Flush. Mit `-AlignIntervals` werden abgeschlossene Minutenintervalle getrennt; ohne diese Option können sich Query-Store-Fenster überlagern. Der Bericht kennzeichnet dies.

Query-Store-Buckets und SQL-Version werden zusätzlich exportiert. Buckets können Aufwärmläufe und weitere Laborbefehle enthalten. Eine hohe Read-Verbesserung garantiert nicht, dass die 25-ms-Heuristik des Dashboards ausgelöst wird. Keine erfundenen Beschleunigungswerte veröffentlichen.

Mit `-Command blocking`, `-Command deadlock` und `-Command verify-reader` werden reale Wartezustände, genau ein Deadlock-Opfer samt XEL-Ereignis sowie minimale Reader-Rechte geprüft. Fehlende Beobachtungen sind Fehler, keine übersprungenen Tests. Ergebnisse liegen in `artifacts/` und werden nicht automatisch eingecheckt.

Zur Anzeige im Dashboard Live-Modus mit der Reader-Verbindung wählen. Die Zertifikatsausnahme ist nur für dieses lokale Labor zulässig. Die Standardfenster der UI sind 60+60 Minuten; das kurze Experiment ist kein kontinuierlicher Produktionsverlauf.

`docker compose -f compose.lab.yml down -v` entfernt nur die zugehörige Laborumgebung samt Volume. Der SQL-Image-Tag ist veränderlich; den tatsächlichen Digest dokumentieren. Docker und SQL Server wurden in der Lieferumgebung nicht ausgeführt. Die Screenshots sind keine Messresultate.
