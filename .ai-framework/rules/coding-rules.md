# Coding Rules — TIAExporter

**Letzte Aktualisierung:** 2026-04-28

---

## Namensgebung

- **Klassen:** PascalCase, beschreibend. `HardwareExporter`, `CraPostProcessor` — klar und sprechend.
- **Methoden:** PascalCase, Verb + Substantiv. `BuildGapAnalysis()`, `TryExportWithOptions()`.
- **Properties/Felder:** PascalCase für Properties, `_camelCase` für private Felder (wo vorhanden).
- **Lokale Variablen:** camelCase, sprechend. Kein `x`, `tmp`, `data` ohne Kontext.
- **Konstanten:** PascalCase oder SCREAMING_SNAKE_CASE — im bestehenden Stil.
- **Namespaces:** `TIAExporter.{Modul}` — bestehende Struktur einhalten.
- **Keine Magic Numbers:** Werte mit Bedeutung als benannte Konstante oder Property ausdrücken.

---

## Funktionen / Methoden

- Kleine Methoden bevorzugen — eine Methode, eine Verantwortung.
- Methoden über 60 Zeilen aufteilen oder begründen.
- Keine tiefen Verschachtelungen (max. 3 Ebenen) — frühe Returns bevorzugen (guard clauses).
- Parameter: max. 5; bei mehr → Parameter-Objekt oder Builder.

---

## Klassen / Module

- Klassen haben eine klar benannte Verantwortung.
- `internal sealed class` für alle Implementierungsklassen (bestehender Stil).
- Keine unnötigen öffentlichen APIs — alles `internal` wo möglich.
- Dependency Injection ist nicht verwendet — direkte Instanziierung OK (Repo ist klein genug).

---

## Fehlerbehandlung

- Bestehender Stil: `try { ... } catch (Exception ex) { logger.Error(...); state.Errors.Add(...); }`
- Kein leerer Catch-Block.
- Kein generisches `catch (Exception)` ohne Logging.
- Openness API-Calls: immer mit Fallback — API-Verfügbarkeit nicht garantiert.

---

## Konfiguration vs. Logik

- Konfiguration (Pfade, Optionen) gehört in `ExportSettings.cs` oder CLI-Parameter.
- Keine Pfade oder Konfigurationswerte direkt in Business-Logik hardkodieren.
- CRA-Kategorien und Scoring-Gewichte: in `CraPostProcessor.cs` als benannte Konstanten/Properties, nicht als Inline-Literale.

---

## Seiteneffekte minimieren

- Methoden die Dateien schreiben: klar aus Name erkennbar (`Write...`, `Export...`).
- Methoden die nur lesen: klar aus Name erkennbar (`Build...`, `Get...`, `Scan...`).
- Keine versteckten Schreiboperationen in Leselogik.

---

## Kommentare

- Standard: **keine Kommentare** — Code ist selbstdokumentierend durch sprechende Namen.
- Kommentare erlaubt wenn: nicht-offensichtliche Openness API-Quirks, bekannte Siemens-Bugs, CRA-fachliche Heuristiken mit unklarem Ursprung.
- Kein "was macht der Code" kommentieren — nur "warum tut er es auf diese seltsame Weise".

---

## Copy-Paste-Logik

- Keine duplizierte Logik. Wenn dieselbe Extraktion an 3+ Stellen erscheint → gemeinsame Methode in `TiaReflection.cs` oder Utility-Klasse.
- Bestehende Hilfsmethoden (`TiaReflection.GetString()`, `FileNameSanitizer`, `EvidenceHasher`) zuerst prüfen.

---

## Tote Code-Blöcke

- Kein auskommentierter Code committen.
- Kein `#if false`-Block ohne Begründung.
- Alte Debug-Ausgaben entfernen.

---

## Bestehenden Stil respektieren

- Nullable Enable: `?` und Null-Checks konsequent verwenden.
- Implizite Usings: bestehende Using-Direktiven als Muster nehmen.
- Record für immutable DTOs (`sealed record`).
- String-Interpolation: `$"..."` — kein String.Format ohne Grund.
- Bestehende Fehlerbehandlungs-Muster in `ExportApplication.cs` und Exportern als Vorlage.

---

## Neue Struktur nur mit nachweisbarem Nutzen

- Keine neuen Abstraktions-Layer nur weil sie "sauber" wirken.
- Kein Interface für eine einzelne Implementierung (Openness COM-Interfaces ausgenommen).
- Kein Generic wenn ein konkreter Typ ausreicht.
