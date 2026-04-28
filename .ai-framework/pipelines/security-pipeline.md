# Security Pipeline — TIAExporter

## Zweck
Dedizierter Security-Review-Ablauf (für geplante Security-Audits oder nach Security-Findings).

---

## Pipeline-Schritte

### Schritt 1: Angriffsfläche bestimmen
- Welche Eingaben akzeptiert das Tool? (CLI-Parameter, Dateipfade)
- Welche externen Daten werden verarbeitet? (TIA Portal API-Rückgaben)
- Welche Dateien werden geschrieben?

### Schritt 2: Datenflüsse prüfen
- Alle Pfade von Eingabe bis Ausgabe verfolgen
- Wo werden Daten validiert? Wo nicht?
- Sind Openness API-Rückgaben direkt in Dateipfade geflossen?

### Schritt 3: Secrets prüfen
- Repository auf Credentials, Tokens, Passwörter scannen
- `git log` auf frühere versehentliche Commits prüfen
- `appsettings`, Konfigurationsdateien prüfen (keine vorhanden — OK)

### Schritt 4: Dependency-Risiken prüfen
- Siemens DLL-Versionen: bekannte CVEs?
- .NET Framework 4.8: Microsoft Security Advisories für diese Version?
- Keine NuGet-Pakete → niedrigeres Risiko

### Schritt 5: Auth/AuthZ prüfen
- Windows-Gruppen-Prüfung (`TryCheckOpennessGroup`) korrekt?
- `asInvoker` (keine Elevation) korrekt?
- Keine eigenen Auth-Mechanismen eingeführt?

### Schritt 6: Logging prüfen
- `ExportLogger`: werden sensible Daten geloggt?
- Welche Log-Level welche Inhalte?
- Ist Log-Datei read-only zugänglich für Dritte?

### Schritt 7: Security-Report schreiben
- `security-report.md` mit allen Findings
- Severity-Bewertung
- Gesamtbewertung

### Schritt 8: Fixes priorisieren
- HIGH-Findings sofort in `task-brief.md` als Bugfix
- MEDIUM-Findings in `open-questions.md` mit Priorität
- LOW-Findings als Hinweise dokumentieren
