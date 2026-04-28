# Skill: CRA Impact Check

## Zweck
Technische CRA-Auswirkungen einer Änderung bewerten.

## Wann verwenden
- Bei Änderungen an `CraPostProcessor.cs` (Kategorien, Scoring, Gap-Analysis)
- Bei Änderungen an `NormalizedModels.cs` (SBOM-Datenstrukturen)
- Bei Änderungen an `JsonWriterService.cs` (Output-Format)
- Bei neuen externen Abhängigkeiten
- Bei neuen Export-Funktionen

## Input
- Geänderte Quelldateien
- `cra-compliance-rules.md`
- `open-questions.md` (offene CRA-Fragen)

## Vorgehen
1. Änderung klassifizieren: CRA-relevant? (Ja/Nein/Unklar)
2. SBOM-Relevanz prüfen (neue Komponenten?)
3. CRA-Kategorien in `CraPostProcessor` betroffen?
4. Output-Format-Änderung → CMDB-Import-Kompatibilität?
5. Evidence-Chain vollständig?
6. Sichere Standardkonfiguration erhalten?
7. Technische Dokumentation aktuell?
8. Offene rechtliche Punkte identifizieren → `[CRA-OFFEN]`

## Output
- `cra-impact-report.md`

## Qualitätskriterien
- CRA-Relevanz klar bewertet
- Offene rechtliche Fragen als `[CRA-OFFEN]` markiert
- Keine Compliance ohne Nachweis behauptet

## Abbruchkriterien
- Änderung ist klar nicht CRA-relevant → kurzer CLEAR-Vermerk in `cra-impact-report.md`
