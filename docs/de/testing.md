# Prüfstatus und Abnahme

[Projekt](../../README.de.md) | [English](../en/testing.md)

## CI-Nachtrag: 6. Oktober 2026
Bereitgestellte GitHub-Protokolle bestätigen Build/Publish und jeweils 51 bestandene
xUnit-Tests auf Windows/Linux sowie 20 echte ASP.NET-API-Prüfungen. Der native
Browsertest scheiterte an einer falsch angesetzten CSP-Assertion. Diese wurde
korrigiert, ohne CSP abzuschalten. [Diagnose und neue Prüfgrenzen](ci-csp-fix.md).
Die folgenden Ergebnisse beschreiben weiterhin die ursprüngliche Lieferumgebung.

## Lieferstatus vom 5. Oktober 2026
Version `0.1.0-preview.1` ist eine Quellcode-Vorschau. In der Lieferumgebung fehlten .NET-SDK, Docker und SQL Server; der SDK-Download scheiterte an Netzwerk/DNS. Deshalb werden **keine C#-Kompilierung, xUnit-Ausführung, ASP.NET-Laufzeitprüfung, echten SQL-Messungen, Windows- oder Container-Tests** behauptet.

Ausgeführt wurden Node-Tests, Python-Hilfstests, Quellenprüfungen und Chromium-Interaktionen mit echter UI, aber synthetischen Antworten über eine isolierte DOM/HTTP-Brücke. Die Umgebung blockierte native localhost-Navigation. Dieser Test bestätigt keine echten Browser-Cookies, CSP, Origin- oder CSRF-Regeln, Downloads oder Reload-Sitzungen. Seine Anmeldung ist kein ASP.NET-Nachweis.

Konkrete Ergebnisse: [Lieferprüfung](../qa/delivery-validation.json) und [UI-Bericht](../qa/ui-fixture-report.json). Keine gemessene SQL-Beschleunigung wird mitgeliefert.

```bash
python scripts/check_repository.py
npm run check
npm test
python tests/python_test.py
dotnet test tests/Observatory.Tests/Observatory.Tests.csproj -c Release
```

API- und native Browsertests benötigen eine tatsächlich gestartete ASP.NET-Anwendung mit gesetztem `SPO_ACCESS_KEY`. Nach negativen Login-Tests die Rate-Limit-Periode abwarten.

Der Verify-Workflow baut und testet .NET auf Windows/Linux und startet für Browserprüfungen eine echte Anwendung. Der manuelle SQL-Workflow erfordert Developer-Lizenzbestätigung und führt echte Labor-, Blocking-, Deadlock- und Berechtigungstests aus. Nicht durch CSP-Abschaltung, übersprungene Assertions oder heimlichen Fixture-Fallback ersetzen.

Vor einem Pilotbetrieb sind vollständiger Build, native Authentifizierung, minimale SQL-Rechte, Ausfall/Wiederanlauf, fehlende/abgeschnittene Quellen, identische Workloadergebnisse, echte Reads, Speicherausfälle, Backup/Restore und Monitoring-Aufwand nachzuweisen. Ein grüner Replay-Test ist noch keine SQL-Abnahme.
