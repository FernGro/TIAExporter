# Current State — TIAExporter

**Letzte Aktualisierung:** 2026-04-28

---

## Projektstatus

| Dimension | Status |
|-----------|--------|
| **Funktional** | Produktionsbereit (CLI + GUI) |
| **Tests** | Keine Unit-Tests; manuelle Integration-Tests mit test_export_*/ |
| **Dokumentation** | README + docs/ vorhanden; AI-Framework neu initialisiert |
| **CI/CD** | Nicht vorhanden |
| **Git** | Initialisiert, noch keine Commits |
| **Build** | Lokal (erfordert TIA Portal V20) |

---

## Aktive Funktionen

- [x] Hardware-Export (Devices, Module, Netzwerk-Topologie)
- [x] PLC-Block-Export (XML, Metadaten-Tags)
- [x] Tag-Tabellen-Export
- [x] Bibliotheks-Export
- [x] HMI-Detektion
- [x] CRA Gap-Analysis (13 Kategorien)
- [x] Quality Score (9 gewichtete Kategorien)
- [x] SHA256 Evidence Hashing
- [x] CLI-Modus (headless-kompatibel)
- [x] GUI-Modus (WinForms)
- [x] CRA Readiness Report (Markdown)
- [x] Openness Environment Diagnostics

---

## Bekannte Einschränkungen (aus EXPORT_LIMITATIONS.md)

- Compile-Check nicht möglich (würde Projektstate ändern)
- WinCC / Startdrive: optional, heuristisch detektiert
- Library-Dependencies: nur heuristisch, keine vollständige Auflösung
- HMI-Daten: nur Gerätedetektion, kein Screen-Export
- Safety-Erkennung: heuristisch über Block-Namen und Module

---

## Nächste mögliche Aufgaben (nicht priorisiert)

1. Erste Unit-Tests für testbare Logik (CraMetadataParser, FileNameSanitizer, EvidenceHasher)
2. CraPostProcessor aufteilen in Sub-Klassen
3. Progress-Callbacks für lange Exports
4. .gitignore ergänzen / ersten Commit anlegen
5. CI/CD Setup (soweit ohne TIA Portal möglich)

---

## Framework-Initialisierung

**Datum:** 2026-04-28  
**Was wurde angelegt:** `.ai-framework/` Verzeichnis mit Regeln, Agenten, Skills, Pipelines, Austausch-Templates und Kontextdokumentation.  
**Produktivcode:** Nicht verändert.
