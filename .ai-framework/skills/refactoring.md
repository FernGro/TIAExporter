# Skill: Refactoring

## Zweck
Code-Qualität verbessern ohne Verhaltensänderung.

## Wann verwenden
- Wenn Methode/Klasse klar zu groß ist und es einen konkreten Nutzen gibt
- Bei identifizierter Code-Duplizierung
- Auf explizite Anfrage (nicht als Nebenprodukt eines anderen Tasks)

## Kandidaten in TIAExporter
- `CraPostProcessor.cs` (506 Zeilen) → `BuildGapAnalysis()`, `BuildQualityScore()` in Sub-Klassen
- Duplizierte Openness-Extraktion in Exportern → `TiaReflection`-Methoden
- Magic Strings (z.B. Attributnamen) → benannte Konstanten

## Absolut verboten
- Output-Format ändern
- SHA256-ID-Algorithmus ändern
- Ohne Test-Absicherung refaktorieren

## Vorgehen
1. Refactoring-Ziel klar beschreiben (warum? was wird besser?)
2. Tests sicherstellen BEVOR die Änderung gemacht wird
3. Kleine, atomare Änderungen
4. Nach jeder Änderung: Build + Tests grün?
5. Verhalten identisch prüfen (Output-Vergleich)

## Output
- Refaktorierter Code
- Unveränderte Funktionalität und Output
- `agent-handoff.md`: Begründung für Refactoring
- `architecture-summary.md` aktualisiert wenn Struktur sich ändert

## Qualitätskriterien
- Build erfolgreich
- Alle Tests grün
- Output-Dateien identisch zu vor dem Refactoring (wenn testbar)
- Kein Verhalten geändert

## Abbruchkriterien
- Kein klarer Nutzen → nicht refaktorieren
- Output-Format würde sich ändern → abbrechen, ADR schreiben
