# Skill: Code Review

## Zweck
Systematische Prüfung von Änderungen vor Abnahme.

## Wann verwenden
- Nach jeder Implementierung
- Bei Pull Requests / Merge-Anfragen

## Input
- Geänderte Dateien (Diff)
- `task-brief.md`, `implementation-plan.md`
- Alle Rules-Dateien

## Vorgehen
1. Anforderungen aus `task-brief.md` gegen Implementierung prüfen
2. Layer-Grenzen prüfen (`architecture-rules.md`)
3. Coding-Style prüfen (`coding-rules.md`)
4. Fehlerbehandlung prüfen (Try-Catch + Logging in Openness-Calls)
5. Security-Check (`security-rules.md`): Pfade, Secrets, Read-Only
6. CRA-Check: Output-Format stabil? Evidence-Hashing vollständig?
7. Tests vorhanden? (`testing-rules.md`)
8. Dokumentation aktuell? (`documentation-rules.md`)

## Output
- `validation-report.md`
- Review-Ergebnis: APPROVED / APPROVED WITH MINOR NOTES / REVISION REQUIRED

## Qualitätskriterien
- Alle 8 Prüfpunkte adressiert
- Findings konkret mit Datei + Zeile
- Empfehlungen umsetzbar

## Abbruchkriterien
- Kein `task-brief.md` vorhanden → Review nicht möglich
