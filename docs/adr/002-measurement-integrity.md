# ADR 002: Measurement integrity / Messintegrität
Status: accepted for the preview.

Group duplicated plan/interval rows, weight means by execution count, replace repeated samples and exclude open or boundary-crossing intervals. Keep low-sample and unavailable states explicit. Do not manufacture percentiles from aggregated means. Compare plan identities but do not label correlation as root cause.

Consequence: some windows have no candidate even when a user experiences slowness. That is preferable to false precision; capture coverage and configuration must be investigated.

Deutsch: Gewichtung, abgeschlossene Intervalle und sichtbare Datenlücken haben Vorrang vor scheinbar präzisen Kennzahlen. Fehlende Daten sind keine Nullmessung. Ein Planwechsel beweist keine Ursache.
