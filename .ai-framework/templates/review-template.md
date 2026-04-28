# Review Template

> Für manuelles Code-Review. Kopieren und ausfüllen → `exchange/validation-report.md`.

---

## Geprüfte Änderungen
[Welche Dateien / Commits werden geprüft?]

## Checkliste

### Funktionalität
- [ ] Anforderungen aus `task-brief.md` erfüllt?
- [ ] Akzeptanzkriterien alle grün?
- [ ] Keine unbeabsichtigten Nebeneffekte?

### Architektur
- [ ] Layer-Grenzen eingehalten?
- [ ] Kein Schreiben auf TIA-Projektdaten?
- [ ] Zero-Dependency (ADR-001)?
- [ ] Output-Format stabil (ADR-002)?
- [ ] SHA256-ID stabil (ADR-004)?

### Code-Qualität
- [ ] Sprechende Namen?
- [ ] Methoden unter 60 Zeilen oder begründet?
- [ ] Keine Magic Numbers?
- [ ] Keine duplizierte Logik?
- [ ] Fehlerbehandlung: Try-Catch + Logging?
- [ ] Keine toten Codeblöcke?

### Security
- [ ] Pfade sanitisiert (`FileNameSanitizer`)?
- [ ] Keine Secrets hardkodiert?
- [ ] Keine Netzwerkkommunikation?
- [ ] `asInvoker` unverändert?
- [ ] `EvidenceHasher` deckt neue Output-Dateien ab?

### Tests
- [ ] Testbare Logik getestet?
- [ ] Edge Cases abgedeckt?
- [ ] Negativtests für Security-Funktionen?
- [ ] Kein Test entfernt?

### Dokumentation
- [ ] README aktuell?
- [ ] `current-state.md` aktualisiert?
- [ ] Neue Entscheidung in `decisions.md`?

---

## Findings
[Konkrete Findings mit Datei + Zeile + Empfehlung]

## Ergebnis
- [ ] APPROVED
- [ ] APPROVED WITH MINOR NOTES
- [ ] REVISION REQUIRED
