# Bugfix Pipeline — TIAExporter

## Zweck
Strukturierter Ablauf für Bug-Fixes.

---

## Pipeline-Schritte

### Schritt 1: Bug reproduzieren
- Bug in `task-brief.md` beschreiben
- Reproduktionsschritte dokumentieren
- Erwartetes vs. tatsächliches Verhalten
- Betroffene Output-Dateien identifizieren

### Schritt 2: Ursachenanalyse
- Betroffene Quelldatei(en) lesen
- Root Cause identifizieren
- Ist der Bug in einem testbaren Modul?
- Haben andere Module das gleiche Problem?

### Schritt 3: Minimaler Fix (Coder Agent)
- Kleinstmögliche Änderung
- Kein Refactoring als Nebenprodukt
- Keine unnötigen Änderungen an nicht-betroffenen Dateien

### Schritt 4: Regressionstest (Test Engineer Agent)
- Test schreiben der den Bug reproduziert (und vorher rot ist)
- Fix anwenden → Test grün
- Bestehende Tests nicht gebrochen

### Schritt 5: Security Check
- Hatte der Bug Security-Auswirkung? (z.B. Path-Traversal, falsches Hashing)
- Wenn ja: `security-report.md` mit Schwere-Bewertung
- Wenn nein: kurzer Vermerk "kein Security-Impact"

### Schritt 6: Dokumentation
- War Bug ein bekanntes Problem? → `EXPORT_LIMITATIONS.md` aktualisieren (entfernen)
- War es unbekannt? → Root-Cause in `agent-handoff.md` dokumentieren
- `current-state.md` aktualisieren

### Schritt 7: Validator Review
- Fix gegen Bug-Beschreibung prüfen
- Regression geprüft?
- `validation-report.md`

---

## Besondere Sorgfalt bei
- Bugs in `EvidenceHasher` → SHA256-Korrektheit kritisch (Audit Trail)
- Bugs in `CraPostProcessor` → CRA-fachliche Korrektheit
- Bugs in `JsonWriterService` → Output-Format-Stabilität
- Bugs in `FileNameSanitizer` → Security-Impact möglich
