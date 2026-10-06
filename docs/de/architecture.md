# Architektur und Grenzen

[Projekt](../../README.de.md) | [English](../en/architecture.md)

## Ziel
Das Verhalten von SQL Server erklären, ohne der Webanwendung die Berechtigung zur automatischen Optimierung von Produktionssystemen zu geben. Die Anwendung ist ein modularer Monolith mit einem Prozess und einem begrenzten lokalen Zustandsverzeichnis.

| Modul | Aufgabe |
|---|---|
| `Observatory.Core` | Gewichtete Kennzahlen, Signale, Blockierungsketten, XML-Normalisierung und Snapshots. |
| `Observatory.SqlServer` | Vier eingebettete SELECT-Abfragen mit typisierten Parametern und Grenzen. |
| `Observatory.Web` | Anmeldung, Konfiguration, Sammler, API und dreisprachige Oberfläche. |
| `Observatory.Lab` | Getrenntes Werkzeug mit Schreibzugriff ausschließlich für die lokale Laborinstanz. |

Pro Ziel existiert eine serielle Schleife. Abfrage-, Blocking- und XEL-Daten haben eigene Intervalle, Fehlerzustände und Zeitstempel. Fehler bleiben sichtbar; alte Daten werden nicht als frische Messung ausgegeben. Query-Store-Zähler werden ersetzt und nicht bei jedem Abruf erneut addiert.

Lokale JSON-Dateien werden über temporäre Dateien ersetzt. Eine Dateisperre verhindert mehrere schreibende Prozesse. Standardmäßig werden alle 15 Minuten bis zu 48 historische Dateien pro Ziel behalten. Beim Neustart wird der letzte Snapshot geladen. Eine Historienansicht in der Oberfläche ist noch nicht implementiert.

Das ist weder eine replizierte Datenbank noch ein unveränderliches Auditprotokoll. Bei einem Austausch der Datenbank unter demselben Namen muss der Zielzustand kontrolliert archiviert oder gelöscht werden. Query-IDs sind nicht global eindeutig.

Entwurfsziel sind primäre Benutzerdatenbanken auf SQL Server 2019/2022 und ein vertrauenswürdiger Analyst pro Installation. Das Referenzlabor verwendet SQL Server 2022 Developer. Azure-Varianten, HA, AG-Routing und versionsspezifische Funktionen von SQL Server 2025 sind nicht qualifiziert. Siehe [Prüfstatus](testing.md).
