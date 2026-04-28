# Skill: Security Review

## Zweck
Sicherheitsrelevante Aspekte von Änderungen prüfen.

## Wann verwenden
- Bei jeder Feature-Implementierung (als Teil der Pipeline)
- Bei Änderungen an `FileNameSanitizer`, `CliOptions`, `EvidenceHasher`, `TiaPortalService`
- Auf explizite Anfrage

## Input
- Geänderte Quelldateien
- `security-rules.md`

## Vorgehen
1. Neue Eingaben identifizieren (CLI, Dateipfade, API-Returns)
2. Path-Traversal-Risiken prüfen (`FileNameSanitizer` verwendet?)
3. Secrets-Prüfung (keine Passwörter/Keys im Code?)
4. Openness-Aufrufe: nur Read? Kein Write, kein Compile?
5. Neue Dependencies: CVE-Check?
6. Logging: keine sensiblen Daten?
7. `EvidenceHasher`: deckt alle Output-Dateien ab?
8. `app.manifest`: noch `asInvoker`?

## Output
- `security-report.md`:
  - Findings mit Severity (HIGH/MEDIUM/LOW/INFO)
  - Empfehlungen
  - Gesamtbewertung: CLEAR / MINOR FINDINGS / BLOCKING FINDINGS

## Qualitätskriterien
- Alle 8 Prüfpunkte adressiert
- Findings mit konkreter Datei und Zeile

## Abbruchkriterien
- BLOCKING FINDINGS → Implementierung stoppen, korrigieren
