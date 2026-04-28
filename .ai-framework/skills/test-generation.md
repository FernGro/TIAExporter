# Skill: Test Generation

## Zweck
Unit Tests für testbare TIAExporter-Komponenten erstellen.

## Wann verwenden
- Bei Änderungen an testbaren Klassen (`CraMetadataParser`, `FileNameSanitizer`, `EvidenceHasher`, `JsonWriterService`, `CraPostProcessor`)
- Bei Bug-Fixes (Regressionstest)
- Auf Anfrage für bestehende Logik ohne Tests

## Testbare Klassen (ohne TIA Portal)
- `CraMetadataParser` — Regex-Input-Tests
- `FileNameSanitizer` — Pfad-Normalisierungs-Tests inkl. Security
- `EvidenceHasher` — SHA256-Verifikation
- `JsonWriterService` — Serialisierungs-Korrektheit
- `CraPostProcessor` — mit Mock-`ExportState`

## Vorgehen
1. Betroffene Klasse lesen
2. Testfälle identifizieren: Happy Path, Edge Cases, Negativtests
3. Testprojekt `TIAExporter.Tests` anlegen (wenn nicht vorhanden)
4. Tests schreiben (NUnit oder xUnit — bestehenden Stil übernehmen)
5. Assertions: Verhalten prüfen, nicht Implementierungsdetails

## Output
- Neue/aktualisierte Testdateien in `TIAExporter.Tests/`
- `test-report.md`

## Qualitätskriterien
- Tests deterministisch
- Edge Cases abgedeckt
- Security-Negativtests für `FileNameSanitizer`
- Tests kompilieren und laufen grün

## Abbruchkriterien
- Klasse ist nicht testbar ohne TIA Portal → dokumentieren in `test-report.md`
