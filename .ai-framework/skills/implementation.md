# Skill: Implementation

## Zweck
Präzise, minimalinvasive Code-Änderungen gemäß Plan.

## Wann verwenden
- Wenn `implementation-plan.md` vorliegt und abgestimmt ist
- Wenn Architect, Security und CRA-Check abgeschlossen

## Input
- `implementation-plan.md`
- `agent-handoff.md`
- `coding-rules.md`, `architecture-rules.md`, `security-rules.md`

## Vorgehen
1. Betroffene Dateien lesen (aktueller Stand)
2. Änderungen gemäß Plan umsetzen — nicht mehr, nicht weniger
3. Bestehenden Stil einhalten
4. Fehlerbehandlung für alle Openness-Calls
5. `FileNameSanitizer` für neue Pfade
6. Neue Klassen als `internal sealed class` im richtigen Namespace
7. `NormalizedModels.cs` erweitern wenn neue Datenstrukturen nötig
8. Lokalen Testlauf ausführen (Build)

## Output
- Geänderte Quelldateien
- Build muss erfolgreich sein (`dotnet build`)
- Update `agent-handoff.md`: Abschnitt Änderungen

## Qualitätskriterien
- Build erfolgreich
- Kein bestehender Code unbeabsichtigt geändert
- Alle Openness-Calls in Try-Catch
- Keine NuGet-Pakete hinzugefügt (ohne ADR)
- Output-Format kompatibel

## Abbruchkriterien
- Plan ist unklar → zurück zu Planner
- Architekturgrenze muss verletzt werden → zurück zu Architect
