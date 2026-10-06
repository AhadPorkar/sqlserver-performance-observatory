# HTTP-API-Vertrag

[Projekt](../../README.de.md) | [English](../en/api.md)

Alle Aufrufe sind Same-Origin. Außer statischer UI, `/auth/csrf` und Liveness ist eine Sitzung erforderlich.

| Aufruf | Zweck |
|---|---|
| GET `/health/live` | Nur HTTP-Liveness. |
| GET `/auth/csrf` | Antiforgery-Token und Cookie. |
| POST `/auth/login` | JSON accessKey, exakte Origin, X-CSRF-TOKEN; setzt Sitzung. |
| POST `/auth/logout` | Frischer Token und Sitzung; Abmeldung. |
| GET `/api/targets` | Verfügbare Ziele und Modus. |
| GET `/api/targets/{id}/snapshot` | Snapshot Schema 1 mit Quellenstatus. |
| GET `/api/targets/{id}/analysis` | Gewichteter Vergleich, optional baselineMinutes/recentMinutes. |
| GET `/api/targets/{id}/blocking` | Abgeleitete Blockierungsketten. |
| GET `/api/targets/{id}/evidence` | Nicht signierter JSON-Diagnoseexport. |

SQL-bigint-Werte werden als Dezimalstrings übertragen. In JavaScript nicht in ungenaue Numbers umwandeln. Fehlende Werte sind null, Zeitstempel ISO 8601. `stable` bedeutet nur: Heuristikschwelle nicht überschritten. Es ist kein Gesundheitsurteil.

Die Oberfläche verwendet 60+60 Minuten. API-Fenster dürfen jeweils 1..1440 Minuten umfassen und gemeinsam nicht größer als der konfigurierte Rückblick sein. Es gibt keine API für beliebiges SQL, Rohtexte, Plan-XML, XEL-Uploads, KILL, Indexänderungen, Planforcing, Benutzerverwaltung oder historische Archivsuche. Exporte sind potenziell sensibel und keine signierten Attestierungen.
