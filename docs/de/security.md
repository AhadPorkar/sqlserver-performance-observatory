# Sicherheit und Datenschutz

[Projekt](../../README.de.md) | [English](../en/security.md)

Diese Vorschau ist ein interner Dienst für einen vertrauenswürdigen Analysten. Wer den Zugangsschlüssel besitzt, sieht alle konfigurierten Ziele. Individuelle Konten, Rollen, Mandanten, SSO, MFA und personenbezogene Auditspuren sind nicht implementiert.

Umgesetzt sind zufällige Zugangsschlüssel mit zeitkonstantem Hash-Vergleich, HttpOnly/SameSite-Cookies, acht Stunden Sitzungsdauer, CSRF- und exakte Origin-Prüfung, Login-Limitierung, authentifizierte API-Lesezugriffe und eine CSP ohne unsafe-eval/unsafe-inline. Außerhalb von Loopback ist HTTPS vorgeschrieben. SQL-Werte sind typisierte Parameter; es gibt keinen Endpunkt für beliebiges SQL.

Query-Store-Texte und Plan-XML werden nicht ausgewählt. IDs, Hashes und aggregierte Werte bleiben erhalten. **Rohes Deadlock-XEL-XML wird kurzzeitig im Arbeitsspeicher verarbeitet und kann sensible SQL-Werte enthalten.** Gespeichert wird nur ein begrenzter normalisierter Graph ohne Eingabepuffer, Login-, Host- und Objektnamen. Dies ist Datenminimierung, keine garantierte Anonymisierung.

DTD und externe XML-Resolver sind deaktiviert. XML-Größe, Graph, Zeilenanzahl und gespeicherte Snapshots sind begrenzt. Ein Zeitfilter garantiert jedoch keine geringe XEL-I/O-Last.

Data-Protection-Schlüssel liegen in `state/.keys`. Unter Linux verschlüsselt die reine Dateispeicherung sie nicht automatisch. Private ACLs, verschlüsselte Datenträger und eine geeignete Schlüsselverwaltung sind Betreiberaufgaben. State-Dateien, Dumps, Exporte und echte XEL-Dateien niemals in ein öffentliches Repository hochladen.

Serverweite Diagnoseberechtigungen geben Einblick auf Instanzebene. Das Abweisen des Namens `sa` verhindert nicht alle privilegierten Konten. Die Berechtigungsprüfung bleibt notwendig. Es gibt keinen Nachweis durch Penetrationstest oder Produktionslasttest. Sicherheitsprobleme privat melden, nicht mit Details in öffentlichen Issues.
