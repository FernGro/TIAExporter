# Documentation Rules — TIAExporter

**Letzte Aktualisierung:** 2026-04-28

---

## Dokumentation ist Teil der Implementierung

Jede Aufgabe ist erst fertig, wenn die Dokumentation aktuell ist. Kein "Doku später".

---

## README.md

Muss enthalten:
- Was ist TIAExporter? (1 Satz)
- Voraussetzungen (TIA Portal V20, .NET 4.8, Windows x64)
- Build-Anleitung
- CLI-Optionen und Beispielaufruf
- GUI-Modus Beschreibung
- Output-Struktur (welche Dateien werden erzeugt?)
- Bekannte Einschränkungen (Verweis auf `docs/EXPORT_LIMITATIONS.md`)

Bei Änderungen an CLI-Optionen, neuen Features oder geänderten Output-Dateien → README sofort aktualisieren.

---

## docs/ — Technische Spezifikation

| Datei | Inhalt | Aktualisieren wenn |
|-------|--------|-------------------|
| `CRA_EXPORT_SCOPE.md` | Was wird exportiert (Assets, Firmware, Software, Security, Safety, HMI, Drives) | Neuer Exporter / neue Kategorie |
| `EXPORT_LIMITATIONS.md` | Bekannte Einschränkungen (Compile, WinCC, Bibliotheken) | Neue Limitation entdeckt oder behoben |
| `TROUBLESHOOTING_BLOCK_EXPORT.md` | Fehlerdiagnose für Block-Export | Neue Fehlerquelle identifiziert |

---

## .ai-framework/context/ — Laufende Projektdokumentation

| Datei | Aktualisieren wenn |
|-------|-------------------|
| `current-state.md` | Nach Abschluss jeder Aufgabe |
| `architecture-summary.md` | Bei Architekturänderungen, neuen Modulen |
| `decisions.md` | Bei neuen ADRs; neue Entscheidung immer dokumentieren |
| `open-questions.md` | Neue Fragen ergänzen; beantwortete Fragen als beantwortet markieren |
| `repo-assessment.md` | Nur bei substantieller Restrukturierung (sonst stabil) |

---

## Architekturentscheidungen (ADRs)

Jede nicht-triviale Designentscheidung bekommt einen ADR-Eintrag in `decisions.md`:

```
### ADR-NNN: Titel
**Datum:** YYYY-MM-DD
**Entscheidung:** Was wurde entschieden?
**Begründung:** Warum? Alternativen verworfen?
**Status:** Aktiv
```

Schwellenwert für ADR: Wenn jemand in 6 Monaten fragt "warum ist das so?" und die Antwort nicht aus dem Code hervorgeht.

---

## Code-Kommentare

- Standard: keine Kommentare.
- Erlaubt: nicht-offensichtliche Openness API-Quirks, CRA-fachliche Heuristiken, workarounds für Siemens-Bugs.
- Nicht erlaubt: "// was der Code tut", "// added for XYZ task", "// TODO: refactor".
- Ausnahme für TODOs: `// TODO: [OQ-NNN oder Jira-Ticket] Begründung` mit Ticket-Referenz.

---

## Neue Features

Bei neuen Features:
1. README Abschnitt ergänzen oder aktualisieren.
2. Wenn CRA-relevant: `docs/CRA_EXPORT_SCOPE.md` aktualisieren.
3. Wenn neue Einschränkung: `docs/EXPORT_LIMITATIONS.md` ergänzen.
4. ADR schreiben wenn Designentscheidung.
5. `current-state.md` "Aktive Funktionen" aktualisieren.
