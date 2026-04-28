# Skill: Release Check

## Zweck
Vor einem Release prüfen ob alles release-bereit ist.

## Wann verwenden
- Vor dem Erstellen eines Release-Builds
- Vor Weitergabe des Tools an Dorst oder Kunden

## Input
- Aktueller Repository-Stand
- `current-state.md`
- `open-questions.md`
- `validation-report.md` der letzten Änderungen

## Vorgehen
1. Build erfolgreich? (`dotnet build -c Release`)
2. Keine offenen BLOCKING FINDINGS in `security-report.md`?
3. Keine offenen `[CRA-OFFEN]` Punkte, die Release blockieren?
4. README korrekt und vollständig?
5. `docs/` aktuell?
6. Keine Debug-Code oder Test-Artefakte im Release?
7. `app.manifest`: `asInvoker` korrekt?
8. TIA Portal V20 DLL-Referenzen korrekt?
9. Keine Secrets/Tokens im Code?
10. `manifest.json` Output enthält korrekte Versionsinformation?

## Output
- Release-Check-Bericht in `agent-handoff.md`:
  - GO / NO-GO mit Begründung
  - Offene Punkte die noch geschlossen werden müssen

## Qualitätskriterien
- Alle 10 Punkte adressiert
- GO nur wenn keine Blocker

## Abbruchkriterien
- Build schlägt fehl → NO-GO, Fix erforderlich
- BLOCKING Security/CRA Findings → NO-GO
