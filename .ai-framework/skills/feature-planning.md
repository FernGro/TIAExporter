# Skill: Feature Planning

## Zweck
Neue Features strukturiert planen bevor implementiert wird.

## Wann verwenden
- Bei jeder neuen Feature-Anfrage
- Wenn Aufgabe mehr als 1 Datei betrifft
- Wenn Architekturauswirkung unklar

## Input
- `task-brief.md` mit Feature-Beschreibung
- `architecture-summary.md`
- `decisions.md` (bestehende ADRs)

## Vorgehen
1. Feature klar beschreiben: Was soll es tun? Was nicht?
2. Betroffene Module identifizieren
3. Datenmodell-Änderungen prüfen (`NormalizedModels.cs`)
4. Output-Format-Änderungen prüfen (`JsonWriterService.cs`)
5. CRA-Relevanz bewerten
6. Testbarkeit bewerten
7. Risiken identifizieren (Breaking Changes, CMDB-Auswirkung)
8. Umsetzungsschritte in `implementation-plan.md` dokumentieren

## Output
- `implementation-plan.md`
- Update `agent-handoff.md`

## Qualitätskriterien
- Plan ist konkret (Dateinamen, Methodennamen, nicht vage)
- Risiken sind benannt
- CRA-Auswirkung ist bewertet

## Abbruchkriterien
- Feature ist unklar → erst `task-brief.md` präzisieren
- Feature verletzt Read-Only-Grenze → ablehnen
