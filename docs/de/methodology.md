# Messmethodik

[Projekt](../../README.de.md) | [English](../en/methodology.md)

Query Store liefert aggregierte Laufzeitstatistiken, keine vollständige Liste einzelner Ausführungen. Dauer und CPU werden einmal von Mikrosekunden in Millisekunden umgerechnet. Logische Lesevorgänge zählen 8-KB-Seiten und sind nicht mit physischem I/O gleichzusetzen. Mehrere Laufzeitzeilen desselben Plans und Intervalls werden zusammengefasst. [Microsoft-Referenz](https://learn.microsoft.com/en-us/sql/relational-databases/system-catalog-views/sys-query-store-runtime-stats-transact-sql?view=sql-server-ver17).

```text
Gewichteter Mittelwert = SUM(Anzahl * mittlere Dauer) / SUM(Anzahl)
Faktor = aktueller gewichteter Mittelwert / Referenzmittelwert
Geschätzte zusätzliche Laufzeit = MAX(0, Differenz) * aktuelle Anzahl
```

100 Ausführungen mit 10 ms und eine mit 1.000 ms ergeben 19,802 ms, nicht 505 ms. Die zusätzliche Laufzeit ist eine Schätzung, keine gemessene CPU-Ersparnis und kein Ursachenbeweis.

Verglichen werden standardmäßig zwei aufeinanderfolgende 60-Minuten-Fenster bis zum letzten abgeschlossenen Intervall. Ein Intervall wird nie anteilig aufgeteilt. Grenzüberschreitende oder offene Intervalle werden ausgeschlossen. Wiederholtes Sammeln ersetzt Zähler statt sie erneut zu addieren.

Ein Kandidat braucht mindestens zehn erfolgreiche Ausführungen pro Fenster, mindestens 1 ms Referenzmittelwert, Faktor 2 und mindestens 25 ms Differenz. Unvollständige Quellen oder abgeschnittene Ergebnisse unterdrücken die Klassifikation. Das sind Heuristiken und keine statistischen Konfidenzintervalle.

Ein Planwechsel ist kein Kausalitätsbeweis. Parameter, Last, Cache, Wartezeiten und Erfassungsregeln können sich ebenfalls ändern. Nur erfolgreich abgeschlossene, erfasste Query-Store-Ausführungen werden ausgewertet. Abgebrochene und fehlgeschlagene Ausführungen fehlen hier; AUTO/CUSTOM kann Abfragen auslassen. Leere Daten bedeuten nicht automatisch einen gesunden Server.

Aus Mittelwert und Maximum lässt sich p95 nicht seriös rekonstruieren. Nur das getrennte Labor berechnet Perzentile aus individuellen clientseitigen Zeitmessungen.

Blocking ist eine Momentaufnahme. Schlafende Sitzungen können die Wurzel sein; negative Blocker-IDs sind Sonderbesitzer. XEL-Daten sind begrenzt und können Lücken enthalten. Der Zähler ist kein vollständiges Auditprotokoll. Siehe [Query-Store-Verwaltung](https://learn.microsoft.com/en-us/sql/relational-databases/performance/manage-the-query-store?view=sql-server-ver17).
