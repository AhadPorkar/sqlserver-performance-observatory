# Umfang und Weiterentwicklung

[Projekt](../../README.de.md) | [English](../en/roadmap.md)

Implementiert sind lesende Adapter, gewichtete Vergleiche, Planidentitäten, Blockierungsketten, normalisierte Deadlocks, lokale Snapshots, Export, Anmeldung, dreisprachige UI und ein getrenntes Labor. Tests und CI sind vorbereitet; ihr tatsächlicher Ausführungsstatus steht im [Prüfbericht](testing.md).

Nicht implementiert: Roh-SQL-/Planansicht, Planbaum-Diff, Query-Store-Wait-Analyse, Alarmversand, Historien-UI, automatische Optimierung, HA, verteilte Collector, RBAC/SSO, Azure-Konnektoren, persistente XEL-Cursor und KI-Ursachendiagnose.

Zuerst vollständige Laufzeit- und SQL-Abnahme. Danach realen Monitoring-Aufwand und Wiederherstellung messen. Erst anschließend umfangreichere Evidenz, individuelle Identitäten, Historie und Skalierung ergänzen. Jeder Schritt braucht klare Fehlerszenarien und Abnahmetests statt bloßer Technologie-Listen. Keine Termine oder Leistungsversprechen werden hier zugesichert.
