# Betrieb und Wiederherstellung

[Projekt](../../README.de.md) | [English](../en/operations.md)

Einen Prozess unter einem eigenen Betriebssystemkonto betreiben. Geheimnisse getrennt halten, Kestrel privat binden, HTTPS und explizites Proxy-Vertrauen konfigurieren. Uhren synchronisieren: SQL-Fenster verwenden Serverzeit, Sammelzeit und Aufbewahrung teilweise Anwendungszeit.

`/health/live` zeigt nur, dass HTTP lebt. Es bestätigt weder SQL-Erreichbarkeit noch aktuelle oder vollständige Daten. Jede Quelle hat eigene Statuswerte, Fehlercodes, Zeitstempel und Abschneidungskennzeichen. Bei fehlenden Rechten zuerst die minimalen Grants prüfen, nicht pauschal sysadmin vergeben.

Standardmäßig laufen pro Ziel serielle Abfragen mit fünf Sekunden Timeout, niedriger Deadlock-Priorität und einer Sekunde Lock-Timeout. Auch lesende Abfragen verursachen Aufwand. Der Zeitbedarf des Collectors ist nicht dessen gemessener SQL-CPU/I/O-Verbrauch. XEL wird erneut gescannt; Rotation und Unterbrechungen können Lücken hinterlassen.

Für eine Sicherung den Dienst stoppen und das komplette private Zustandsverzeichnis verschlüsselt sichern. Eine Wiederherstellung getrennt testen. Niemals zwei Prozesse auf dasselbe Verzeichnis richten. Data-Protection-Schlüssel unter `.keys` sind sensibel. Das Verzeichnis ist kein Backup der beobachteten Datenbanken.

48 Archivdateien je Ziel entsprechen bei 15-Minuten-Takt ungefähr zwölf Stunden. Eine Datei darf höchstens 32 MiB groß werden. Die aktuelle Deadlock-Liste enthält höchstens 500 normalisierte Ereignisse aus sieben Tagen; der erste XEL-Rückblick beträgt 24 Stunden. Vollständigkeit ist nicht garantiert.

Vor Updates sichern und CI prüfen. Unbekannte Schemas werden nicht automatisch migriert. Ein Austausch der Datenbank unter demselben Endpunkt erfordert die bewusste Prüfung alter Snapshots. Die eingebaute system_health-Sitzung niemals durch diese Anwendung ändern.
