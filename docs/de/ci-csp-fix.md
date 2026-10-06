# CI-Korrektur: Dokument-CSP statt Inspector-Ausnahme prüfen

[Projekt](../../README.de.md) | [English](../en/ci-csp-fix.md) | [Prüfstatus](testing.md)

## Diagnose der eingereichten Protokolle
Das Archiv `logs_101358504729.zip` gehört zu
`AhadPorkar/sqlserver-performance-observatory`, Commit
`60dc9a465d326ccc8d0835187e305f80c8af2a83`, vom 6. Oktober 2026.
Es betrifft nicht das Projekt transactional-messaging-lab.

Beide .NET-Jobs bauten und publizierten erfolgreich. Auf Ubuntu 24.04 und
Windows 2022 bestanden jeweils **51 xUnit-Tests**, ohne Fehler oder übersprungene
Tests. Der echte ASP.NET-Prozess startete; **20 API-Prüfungen bestanden**.
Der Quellen-/Frontend-Job war ebenfalls erfolgreich. Der native Browsertest
scheiterte an `tests/ui_test.py:88`, `assert blocked`.
Die [bereinigte Zusammenfassung](../qa/ci-log-summary-2026-10-06.json) enthält
keine Rohprotokolle oder Zugangsdaten. Bestehende xUnit- und Node-Warnungen waren
nicht die Fehlerursache und werden durch dieses Update nicht unterdrückt.

## Ursache
`page.evaluate` benutzt den Inspector-Ausführungspfad. Dieser kann dynamische
Kompilierung zulassen, obwohl die CSP der Seite sie verbietet. Im
[Chrome DevTools Protocol](https://raw.githubusercontent.com/ChromeDevTools/devtools-protocol/master/json/js_protocol.json)
ist `Runtime.evaluate.allowUnsafeEvalBlockedByCSP` standardmäßig true.
Die bisherige Assertion bewies daher nicht, dass die CSP der Anwendung defekt war.

## Korrektur
`tests/csp_probe.py` setzt ausdrücklich
**`allowUnsafeEvalBlockedByCSP: false`**. Nur `EvalError` bei `eval` und beim
Function-Konstruktor gilt als Sperrnachweis. Protokollfehler, fehlende Ergebnisse
oder andere Ausnahmen führen zum Testfehler. Der UI-Test prüft den erzwingenden
CSP-Header der tatsächlich geladenen Seite, speichert die Probe-Ergebnisse und
schreibt auch bei Fehlern einen Teilbericht.

Zwölf Kontrollen prüfen strikte, fehlende und ausdrücklich erlaubende Policies,
Direktivenvorrang und ungültige Ergebnisse. Ein echter Browserklick löst einen
Nonce-autorisierten Seitenhandler aus und bestätigt den CDP-Befund unabhängig.
Diese Nonce und die erlaubenden Policies existieren ausschließlich in isolierten
Testdokumenten. CI führt anschließend weiterhin den nativen Test gegen ASP.NET aus.

**C#-Anwendung, SQL, produktive CSP, Anmeldung, Paketversionen und Berechtigungen
bleiben unverändert.** Es gibt keinen `bypass_csp`-Schalter und keinen automatischen
Fixture-Fallback. Die Probe ist wegen CDP auf Chromium begrenzt.

## Verifikation und Grenzen
[Kontrollbericht](../qa/csp-regression-controls.json): 12 Tests bestanden auf
Chromium 144.0.7559.96 mit Playwright 1.57.0. Die alte Assertion meldete bei
strikter CSP fälschlich keine Sperre; die korrigierte Probe und der native
Klickhandler stellten die Sperre fest. Fehlende CSP und `unsafe-eval` wurden
korrekt nicht als Schutz anerkannt.

Zusätzlich bestanden 14 Frontend-Tests, 6 Python-Tests und 31 reine
UI-Präsentationsprüfungen mit synthetischer DOM/HTTP-Brücke. In dieser Umgebung
sind localhost-Navigation und die vollständige ASP.NET-Ausführung nicht verfügbar;
.NET-SDK, Docker und SQL Server fehlen. **Der neue komplette native CI-Lauf ist
noch zu prüfen.** Die erfolgreichen .NET/API-Ergebnisse stammen aus den
bereitgestellten Protokollen des vorherigen Commits.

```bash
pip install -r tests/requirements.txt
python -m playwright install chromium
python tests/csp_test.py
# Danach mit gestartetem ASP.NET-Replay-Host und SPO_ACCESS_KEY:
python tests/ui_test.py --base-url http://localhost:5080
```

Den Patch committen und pushen, danach den Verify-Lauf des neuen Commits prüfen.
Ein erneuter Lauf des alten Commits verwendet weiterhin den alten Test. Grüne
Replay-Tests ersetzen keine Abnahme gegen einen echten SQL Server.
