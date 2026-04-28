# Documentation Rules - TIAExporter

**Letzte Aktualisierung:** 2026-04-28

---

## Dokumentation ist Teil der Implementierung

Jede Aufgabe ist erst fertig, wenn die Dokumentation aktuell ist. Kein "Doku spaeter".

---

## README.md

Muss enthalten:

- Was ist TIAExporter? (1 Satz)
- Voraussetzungen (TIA Portal V20, .NET 4.8, Windows x64)
- Build-Anleitung
- CLI-Optionen und Beispielaufruf
- GUI-Modus Beschreibung
- Output-Struktur (welche Dateien werden erzeugt?)
- Bekannte Einschraenkungen (Verweis auf `docs/EXPORT_LIMITATIONS.md`)

Bei Aenderungen an CLI-Optionen, neuen Features oder geaenderten Output-Dateien -> README sofort aktualisieren.

---

## docs/ - Technische Spezifikation

| Datei | Inhalt | Aktualisieren wenn |
|-------|--------|-------------------|
| `CRA_EXPORT_SCOPE.md` | Was wird exportiert (Assets, Firmware, Software, Security, Safety, HMI, Drives) | Neuer Exporter oder neue Kategorie |
| `EXPORT_LIMITATIONS.md` | Bekannte Einschraenkungen (Compile, WinCC, Bibliotheken) | Neue Limitation entdeckt oder behoben |
| `TROUBLESHOOTING_BLOCK_EXPORT.md` | Fehlerdiagnose fuer Block-Export | Neue Fehlerquelle identifiziert |
| `SIEMENS_EXPORTER_STATUS.md` | Vollstaendiger Ist-Stand fuer Wiki, Management und Technik: Faehigkeiten, Luecken, Blocker, Verifikation und To-dos fuer CMDB, SBOM und CRA | Jede substanzielle Aenderung an Exportumfang, Diagnostik, Verifikation, Blockern oder CRA-Reifegrad |

`docs/SIEMENS_EXPORTER_STATUS.md` ist ein Pflichtartefakt. Wenn sich der reale Stand des Exporters aendert, muss diese Datei im gleichen Arbeitspaket mit aktualisiert werden. Sie ist die zentrale Langform-Dokumentation fuer Wiki-Uebernahme.

---

## .ai-framework/context/ - Laufende Projektdokumentation

| Datei | Aktualisieren wenn |
|-------|-------------------|
| `current-state.md` | Nach Abschluss jeder Aufgabe |
| `architecture-summary.md` | Bei Architekturaenderungen, neuen Modulen |
| `decisions.md` | Bei neuen ADRs; neue Entscheidung immer dokumentieren |
| `open-questions.md` | Neue Fragen ergaenzen; beantwortete Fragen als beantwortet markieren |
| `repo-assessment.md` | Nur bei substantieller Restrukturierung (sonst stabil) |

---

## Architekturentscheidungen (ADRs)

Jede nicht-triviale Designentscheidung bekommt einen ADR-Eintrag in `decisions.md`:

```text
### ADR-NNN: Titel
**Datum:** YYYY-MM-DD
**Entscheidung:** Was wurde entschieden?
**Begruendung:** Warum? Alternativen verworfen?
**Status:** Aktiv
```

Schwellenwert fuer ADR: Wenn jemand in 6 Monaten fragt "warum ist das so?" und die Antwort nicht aus dem Code hervorgeht.

---

## Code-Kommentare

- Standard: keine Kommentare.
- Erlaubt: nicht-offensichtliche Openness API-Quirks, CRA-fachliche Heuristiken, Workarounds fuer Siemens-Bugs.
- Nicht erlaubt: Kommentare, die nur den Code wiederholen.
- Ausnahme fuer TODOs: `// TODO: [OQ-NNN oder Ticket] Begruendung` mit Referenz.

---

## Neue Features

Bei neuen Features:

1. README Abschnitt ergaenzen oder aktualisieren.
2. Wenn CRA-relevant: `docs/CRA_EXPORT_SCOPE.md` aktualisieren.
3. Wenn neue Einschraenkung: `docs/EXPORT_LIMITATIONS.md` ergaenzen.
4. ADR schreiben, wenn es eine relevante Designentscheidung gab.
5. `current-state.md` unter "Aktive Funktionen" aktualisieren.
6. `docs/SIEMENS_EXPORTER_STATUS.md` aktualisieren, wenn sich Ist-Stand, Verifikation oder offene Restarbeit aendern.
