# ADR 001: Separate observation from mutation / Beobachtung und Eingriffe trennen
Status: accepted for the preview.

The web service has only fixed metadata SELECT adapters plus session-local timeout/priority settings. It has no arbitrary SQL, KILL, index or plan-forcing endpoint. A separate executable performs writes only in a guarded local Developer laboratory. Query Store enablement and grants remain DBA decisions.

Consequence: diagnosis cannot repair production automatically. This is intentional. ReadOnly connection intent is not authorization; grants still matter. Administrator configuration and network topology remain trusted.

Deutsch: Der Webdienst beobachtet ausschließlich. Schreibende Experimente liegen in einem getrennten lokalen Labor. Damit ist automatische Optimierung ausgeschlossen; echte Berechtigungen sind weiterhin notwendig.
