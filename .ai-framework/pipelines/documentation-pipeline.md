# Documentation Pipeline — TIAExporter

## Zweck
Gezielte Dokumentations-Aktualisierung (z.B. nach mehreren Änderungen oder auf Anfrage).

---

## Pipeline-Schritte

### Schritt 1: Delta identifizieren
- Was hat sich seit letzter Dokumentations-Aktualisierung geändert?
- Welche Dateien wurden geändert?
- Was steht in `agent-handoff.md` der letzten Tasks?

### Schritt 2: README prüfen
- CLI-Optionen korrekt?
- Output-Struktur aktuell?
- Prerequisites (TIA Portal V20) korrekt?
- Bekannte Einschränkungen aktuell?

### Schritt 3: docs/ prüfen
- `CRA_EXPORT_SCOPE.md`: alle Export-Bereiche dokumentiert?
- `EXPORT_LIMITATIONS.md`: alle bekannten Grenzen dokumentiert?
- `TROUBLESHOOTING_BLOCK_EXPORT.md`: bekannte Fehler aktuell?

### Schritt 4: .ai-framework/context/ aktualisieren
- `current-state.md`: Funktions-Status aktuell?
- `architecture-summary.md`: Architektur-Diagramm korrekt?
- `decisions.md`: alle ADRs dokumentiert?
- `open-questions.md`: beantwortete Fragen geschlossen?

### Schritt 5: Konsistenz prüfen
- Keine widersprüchlichen Angaben zwischen Dateien?
- Versionsnummern konsistent?
- CRA-Deadlines konsistent (11.09.2026 / 11.12.2027)?

---

## Qualitätskriterien
- Kein Leser soll veraltete Informationen finden
- Alle Dateien untereinander konsistent
- Keine `[MISSING: ...]` Platzhalter ohne Begründung
