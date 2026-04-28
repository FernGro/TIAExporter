# Skill: Repo Analysis

## Zweck
Vollständige Analyse des TIAExporter-Repositories für Kontext-Aufbau oder nach größeren Änderungen.

## Wann verwenden
- Zu Beginn einer neuen Session ohne bekannten Kontext
- Nach größeren Refactorings
- Wenn `repo-assessment.md` veraltet erscheint

## Input
- Repository-Verzeichnis

## Vorgehen
1. `src/` Verzeichnisstruktur erfassen
2. Alle `.cs` Dateien lesen (Klassen, Namespaces, Abhängigkeiten)
3. `TIAExporter.csproj` auf Dependencies prüfen
4. `README.md` und `docs/` lesen
5. Bestehende `.ai-framework/context/` Dateien prüfen
6. Delta zur bestehenden `repo-assessment.md` identifizieren

## Output
- Aktualisierte `repo-assessment.md` (wenn veraltet)
- Zusammenfassung: was hat sich seit letzter Analyse geändert?

## Qualitätskriterien
- Alle Module (`src/App`, `src/Gui`, `src/Tia`, `src/Normalization`) erfasst
- Abhängigkeiten (Siemens DLLs) dokumentiert
- Bekannte Schwächen aktuell

## Abbruchkriterien
- `repo-assessment.md` ist aktuell (< 1 Session alt, keine strukturellen Änderungen)
