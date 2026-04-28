# Current State - TIAExporter

**Letzte Aktualisierung:** 2026-04-28

---

## Projektstatus

| Dimension | Status |
|-----------|--------|
| **Funktional** | Erweitert implementiert, aber End-to-End derzeit durch TIA-Openness-Startproblem blockiert |
| **Tests** | Projektbuild und zielgerichtete Unit-Tests vorhanden; reale TIA-Integration aktuell nur teilweise verifizierbar |
| **Dokumentation** | README + docs/ vorhanden; Wiki-Statusdatei `docs/SIEMENS_EXPORTER_STATUS.md` ist Pflichtartefakt |
| **CI/CD** | Nicht vorhanden |
| **Git** | Initialisiert, noch keine Commits |
| **Build** | `TIAExporter.csproj` und Testprojekt bauen lokal; Solution-Build verhaelt sich noch inkonsistent |

---

## Aktive Funktionen

- [x] Hardware-Export (Devices, Module, Netzwerk-Topologie)
- [x] PLC-Block-Export mit Mehrfachstrategie (XML, alternative ExportOptions, Dokument-Fallback, Metadata-only)
- [x] Tag-Tabellen-Export
- [x] Bibliotheks-Export mit Fehlerdiagnose
- [x] HMI-Detektion
- [x] Drive-Detektion / Startdrive-Capability-Betrachtung
- [x] Safety-Inventarisierung
- [x] Security-Konfigurationsnormalisierung und Findings
- [x] CRA Gap-Analysis (13 Kategorien)
- [x] Quality Score (9 gewichtete Kategorien)
- [x] SHA256 Evidence Hashing
- [x] CLI-Modus (headless-kompatibel)
- [x] GUI-Modus (WinForms)
- [x] CRA Readiness Report (Markdown)
- [x] Openness Environment Diagnostics
- [x] Block- und Library-Failure-Diagnostics
- [x] Manifest mit CRA-/Capability-Summary

---

## Bekannte Einschraenkungen

- TIA-Openness-Start kann in der aktuellen Umgebung mit `Connection to TiaPortal failed` abbrechen
- Compile-Check nicht automatisch oder standardmaessig moeglich, ohne Projektzustand zu beruehren
- WinCC / Startdrive: Abdeckung stark von installierten Modulen und verfuegbaren APIs abhaengig
- Library-Dependencies: nur heuristisch, keine vollstaendige semantische Aufloesung
- HMI-Daten: Grunddetektion vorhanden, Tiefenexport nur eingeschraenkt
- Safety-Erkennung: teilweise heuristisch ueber Block-Namen, Module und Attribute
- SBOM ist nur engineering-nah ableitbar, keine klassische Paketmanager-SBOM

---

## Verifizierter Teststand

- `dotnet build TIAExporter.csproj --no-restore` erfolgreich
- `dotnet build tests\\TIAExporter.Tests.csproj --no-restore` erfolgreich
- `tests\\bin\\Debug\\net48\\TIAExporter.Tests.exe` erfolgreich
- Realer Exportlauf in aktueller Umgebung scheitert derzeit vor Exportbeginn am `TiaPortal`-Start

---

## Naechste Aufgaben

1. TIA-Openness-Startproblem reproduzierbar isolieren und beheben
2. Danach End-to-End-Export gegen Beispielprojekt erneut validieren
3. Blockexportfehler nach realen Fehlerklassen auswerten
4. Bibliotheks-, HMI-, Drive- und Safety-Abdeckung gegen reale Projekte weiter haerten
5. Solution-Build-Verhalten und Artefakt-Bereinigung bereinigen
6. Mehr Unit-Tests fuer nicht-TIA-abhaengige Logik

---

## Juengste relevante Aenderungen

- Lizenzdiagnose enger gefasst; "Lizenz fehlt" wird nicht mehr pauschal behauptet
- PLC-Blockexport stoppt nicht mehr global nach erstem Lizenzfehler
- Dokument-Fallback und Metadata-only-Pfade sind in der Diagnose sauber sichtbar
- Neue Wiki-Statusdatei `docs/SIEMENS_EXPORTER_STATUS.md` als Pflichtdokument eingefuehrt
