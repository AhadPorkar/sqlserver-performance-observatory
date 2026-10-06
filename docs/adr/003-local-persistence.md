# ADR 003: Bounded local JSON / Begrenzter lokaler Zustand
Status: accepted for the preview, revisit after measurement.

A small single-process evaluation stores latest snapshots and a bounded periodic archive using atomic replacement and a writer lease. This avoids installing another database merely to inspect an existing SQL Server. IDs are strings on the wire to preserve bigint precision.

Consequence: no distributed HA, no historical query API and no cross-process writes. Full snapshot serialization costs memory/I/O and must be measured. A future storage engine requires evidence that current constraints are insufficient; it is not added for a longer technology list.

Deutsch: Lokale JSON-Snapshots vereinfachen die Auswertung, sind aber kein Ersatz für HA oder eine beliebig skalierende Datenbank. Grenzen sind dokumentiert und müssen gemessen werden.
