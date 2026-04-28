# Feature Pipeline — TIAExporter

## Zweck
Standardablauf für neue Features von Anforderung bis Abnahme.

---

## Pipeline-Schritte

### Schritt 1: Aufgabe erfassen (Planner Agent)
- `task-brief.md` ausfüllen
- Anforderung klar und vollständig formulieren
- Akzeptanzkriterien definieren

### Schritt 2: Plan erstellen (Planner Agent)
- `implementation-plan.md` erstellen
- Betroffene Dateien identifizieren
- Risiken und Abhängigkeiten benennen
- Reihenfolge festlegen

### Schritt 3: Architektur-Review (Architect Agent)
- Layer-Grenzen prüfen
- Design-Entscheidungen bewerten
- ADR schreiben wenn nötig
- `agent-handoff.md` aktualisieren

### Schritt 4: Security-Check (Cybersecurity Engineer Agent)
- Neue Eingaben / Ausgaben prüfen
- Path-Traversal, Secrets, Read-Only-Grenze
- `security-report.md` erstellen
- Bei BLOCKING FINDINGS: zurück zu Schritt 2

### Schritt 5: CRA-Impact-Check (CRA Compliance Expert Agent)
- CRA-Relevanz bewerten
- SBOM-Auswirkung prüfen
- `cra-impact-report.md` erstellen
- Offene Punkte als `[CRA-OFFEN]` markieren

### Schritt 6: Implementierung (Coder Agent)
- Gemäß `implementation-plan.md` umsetzen
- Bestehenden Stil einhalten
- Build muss grün sein

### Schritt 7: Tests (Test Engineer Agent)
- Testbare Logik testen
- Edge Cases und Negativtests
- `test-report.md` erstellen

### Schritt 8: Validation (Validator Agent)
- Gegen `task-brief.md` prüfen
- Architektur, Security, Tests, Doku prüfen
- `validation-report.md` mit Review-Ergebnis

### Schritt 9: Dokumentation (Documentation Engineer Agent)
- README, docs/, context/ aktualisieren
- `current-state.md` aktualisieren
- `agent-handoff.md` abschließen

### Schritt 10: Final Review
- `agent-handoff.md` vollständig?
- Alle Reports vorhanden?
- Aufgabe abgeschlossen.

---

## Abkürzungen (kleine Features)
Bei kleinen, klar abgegrenzten Änderungen (< 20 Zeilen, kein Architektur-Impact):
- Schritte 3, 5 können als "N/A" dokumentiert werden wenn kein Impact
- Schritte 7 kann entfallen wenn keine testbaren Klassen betroffen
- Schritt 9 immer: `current-state.md` update ist Minimum
