# Skill: Documentation Update

## Zweck
Dokumentation nach Implementierung aktuell halten.

## Wann verwenden
- Nach jeder Implementierung (Pflicht)
- Nach Refactoring
- Nach Bug-Fix wenn Verhalten sich geändert hat

## Input
- `validation-report.md` (was wurde abgenommen?)
- Geänderte Quelldateien
- `documentation-rules.md`

## Vorgehen
1. README prüfen: CLI-Optionen noch korrekt? Output-Dateien aktuell?
2. `docs/CRA_EXPORT_SCOPE.md` prüfen: neue Funktionen dokumentiert?
3. `docs/EXPORT_LIMITATIONS.md` prüfen: neue Einschränkungen?
4. `current-state.md` "Aktive Funktionen" aktualisieren
5. `open-questions.md` beantwortete Fragen schließen
6. Neuen ADR in `decisions.md` wenn Entscheidung getroffen
7. `architecture-summary.md` wenn Architektur geändert

## Output
- Aktualisierte Dokumentationsdateien
- Update `agent-handoff.md`: finaler Eintrag

## Qualitätskriterien
- Kein veralteter Stand in README
- `current-state.md` reflektiert tatsächlichen Stand
- Keine offenen Fragen, die beantwortet wurden, noch als offen markiert

## Abbruchkriterien
- Kein tatsächlicher Dokumentationsbedarf → kurzer Vermerk "Dokumentation aktuell"
