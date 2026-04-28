# Refactoring Pipeline — TIAExporter

## Zweck
Strukturierter Ablauf für Refactorings ohne Verhaltensänderung.

---

## Voraussetzungen (vor Start)
- Klarer Grund dokumentiert: Was wird konkret besser?
- Kein Breaking Change an Output-Format oder ID-Algorithmus
- Kein aktiver Feature-Branch (Refactoring separat)

---

## Pipeline-Schritte

### Schritt 1: Architekturproblem identifizieren (Architect Agent)
- Was ist das konkrete Problem? (Zu große Klasse? Duplizierter Code?)
- Welchen messbaren Nutzen bringt das Refactoring?
- ADR schreiben wenn Strukturänderung

### Schritt 2: Zielstruktur definieren
- Wie sieht die Zielstruktur aus?
- Welche neuen Klassen/Methoden entstehen?
- Welche Dateien werden geändert?
- Wird `architecture-summary.md` aktualisiert?

### Schritt 3: Verhalten absichern (Test Engineer Agent)
- Bestehende Tests identifizieren
- Neue Tests für zu refaktorierende Logik schreiben (wenn möglich)
- **Keine Refactoring-Änderung ohne Test-Sicherheitsnetz**

### Schritt 4: Schrittweise refaktorieren (Refactoring Engineer / Coder Agent)
- Kleine, atomare Änderungen
- Nach jeder Änderung: Build + Tests grün
- Output-Format-Stabilität prüfen

### Schritt 5: Regressionstest
- Alle Tests grün?
- Output mit Referenz-Export verglichen? (wenn TIA Portal verfügbar)
- Kein unerwartetes Verhalten?

### Schritt 6: Dokumentation (Documentation Engineer Agent)
- `architecture-summary.md` aktualisieren
- ADR-Entscheidung in `decisions.md` abschließen
- `current-state.md` aktualisieren

---

## Klare Abbruchkriterien
- Output-Format würde sich ändern → STOPP
- SHA256-ID-Algorithmus würde sich ändern → STOPP
- Kein messbarer Nutzen → STOPP (nicht refaktorieren)
- Tests können nicht geschrieben werden → Risiko dokumentieren, Entscheidung mit Nutzer
