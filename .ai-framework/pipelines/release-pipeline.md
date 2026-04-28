# Release Pipeline — TIAExporter

## Zweck
Sicherstellen dass ein Release-Build korrekt, dokumentiert und sicher ist.

---

## Voraussetzungen
- Alle offenen Tasks abgeschlossen
- Kein BLOCKING Finding in Security- oder CRA-Report
- Alle `agent-handoff.md` der letzten Änderungen vollständig

---

## Pipeline-Schritte

### Schritt 1: Code-Freeze vorbereiten
- Kein offener Feature-Branch
- Git-Status sauber (alle Änderungen committed)
- Version/Release-Tag definieren

### Schritt 2: Release Check (Skill: release-check)
- Build: `dotnet build -c Release` → grün?
- Keine Debug-Code-Reste
- Keine Secrets im Code
- `app.manifest`: `asInvoker` korrekt
- Siemens DLL-Referenzpfade korrekt (TIA Portal V20)

### Schritt 3: Security Final Check
- `security-report.md` aus letzten Tasks: alle Findings adressiert?
- Keine neuen Vulnerabilities seit letztem Security-Review?

### Schritt 4: CRA Final Check
- `cra-impact-report.md` aus letzten Tasks aktuell?
- Alle `[CRA-OFFEN]` Punkte: bewusst offen oder blockierend?

### Schritt 5: Dokumentation final
- README korrekt und vollständig?
- `docs/` aktuell?
- `current-state.md` korrekt?

### Schritt 6: Release Build erstellen
- `dotnet publish -c Release`
- Output: `TIAExporter.exe` (Single Executable)

### Schritt 7: Smoke Test
- Manueller Test mit `--help` und `--diagnostics-only` (ohne TIA Portal)
- Wenn TIA Portal verfügbar: Export-Run auf Testprojekt

### Schritt 8: Release-Dokumentation
- `agent-handoff.md`: Release-Notizen
- Was ist neu / geändert / bekannt

---

## GO-Kriterien
- Build grün
- Keine BLOCKING Findings (Security + CRA)
- Dokumentation aktuell
- Smoke Test bestanden
