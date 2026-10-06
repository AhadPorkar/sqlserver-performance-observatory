# Konfiguration

[Projekt](../../README.de.md) | [English](../en/configuration.md)

ASP.NET-Konfiguration kann durch Umgebungsvariablen mit doppeltem Unterstrich überschrieben werden. SQL-Verbindungswerte liegen nur auf dem Server; die HTTP-API nimmt keine Verbindungszeichenfolgen entgegen.

```powershell
$env:SPO_ACCESS_KEY = '<zufaelliger-geheimer-Wert-mit-mindestens-32-Zeichen>'
$env:SPO_SQL_PRIMARY = 'Server=sql.internal.example;Database=YourDatabase;Integrated Security=True;Encrypt=True;TrustServerCertificate=False'
$env:Observatory__Mode = 'Live'
$env:Observatory__Targets__0__CaptureBlocking = 'false'
$env:Observatory__Targets__0__CaptureDeadlocks = 'false'
dotnet run --project src/Observatory.Web --no-launch-profile
```

Standardwerte: Query Store alle 120 Sekunden, Blocking alle 10 Sekunden, XEL alle 120 Sekunden, 24 Stunden Rückblick, 5 Sekunden Befehls-Timeout, maximal 5.000 Abfrage-Buckets, 200 Blocking-Zeilen und 200 XEL-Ereignisse je Abfrage. Die Grenzen werden in [TargetOptions.cs](../../src/Observatory.SqlServer/TargetOptions.cs) validiert.

Der Zeitfilter und TOP begrenzen nicht zwingend den physischen XEL-Leseaufwand. Eine zusätzliche Ergebniszeile erkennt Abschneidung. Abgeschnittene Abfragedaten erzeugen keine vermeintlich sicheren Regressionsurteile.

`PublicOrigin` muss exakt zu Scheme, Host und Port des Browsers passen. Nicht lokale Origins brauchen HTTPS. Hinter einem Proxy zusätzlich `AllowedHosts` und die konkrete IP in `Observatory__TrustedProxies__0` setzen. Kestrel nicht direkt veröffentlichen. Siehe [Nginx-Beispiel](../../deploy/nginx.conf.example).

`StateDirectory` muss privat und schreibbar sein. `HistoryLimit=48` begrenzt Archivdateien pro Ziel. Das Programm erlaubt 1..8 Ziele, aber nur einen Prozess pro Verzeichnis.

Integrierte Windows-Authentifizierung nutzt das Dienstkonto. Unter Linux ist eine getrennte Kerberos-Konfiguration notwendig. Die Anwendung richtet sie nicht ein. Die Zertifikatsausnahme ist ausschließlich für erlaubte lokale Adressen mit Datenbank `ObservatoryLab` vorgesehen. XEL-Pfade sind SQL-Server-seitig und dürfen nur Administratoren konfigurieren. Keine Geheimnisse oder echten Diagnosedateien in Git speichern.
