# Veröffentlichung auf GitHub

[Projekt](../../README.de.md) | [English](../en/publishing.md)

Ein leeres Repository `sqlserver-performance-observatory` anlegen. Die Dateien einschließlich `.github` gehören in dessen Wurzel; nicht lediglich das ZIP hochladen. Git reicht, `gh` und `winget` sind nicht erforderlich.

```powershell
Set-Location 'D:\GITHUB\sqlserver-performance-observatory'
git init -b main
git config user.name 'AhadPorkar'
git config user.email '<bestaetigte-oder-exakte-noreply-adresse>'
git add --dry-run .
# Dateiliste auf Geheimnisse und Zustandsdaten pruefen.
git add .
git diff --cached --check
git diff --cached --stat
git commit -m 'feat: add read-only SQL Server performance observatory'
git remote add origin https://github.com/AhadPorkar/sqlserver-performance-observatory.git
git push -u origin main
```

Bei Fehlern stoppen. Ein bestehendes Repository mit Historie nicht gewaltsam ersetzen. Keine Tokens in URLs schreiben. Die Zieladresse ist vorgeschlagen, nicht als bereits vorhandenes Repository bestätigt.

Unter Actions den Verify-Lauf des neuen Commits prüfen. Er baut und testet auf Windows/Linux sowie gegen eine echte ASP.NET-Instanz. Das SQL-Labor ist ein manueller Workflow mit ausdrücklicher Developer-Lizenzbestätigung. Ein erfolgreicher Push beweist keinen erfolgreichen Build. Vorschaukennzeichnung behalten, bis alle Abnahmekriterien erfüllt sind.
