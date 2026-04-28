# Architecture Rules — TIAExporter

**Letzte Aktualisierung:** 2026-04-28

---

## Layer-Grenzen einhalten

```
Presentation → Application → Domain (Tia / Normalization) → Infrastructure
```

- **Presentation** (`Program.cs`, `MainForm.cs`, `CliOptions.cs`): Darf nur `App`-Layer aufrufen. Keine direkte Openness API.
- **Application** (`ExportApplication.cs`): Orchestriert Exporter. Keine Ausgabe direkt — ruft JsonWriterService auf.
- **Domain/Tia** (`TiaPortalService`, Exporter): Isoliert alle Openness-Interaktionen. Kein direktes JSON-Schreiben.
- **Domain/Normalization** (`CraPostProcessor`, `NormalizedModels`): Kein TIA-API-Zugriff — arbeitet auf In-Memory-Daten.
- **Infrastructure** (`ExportLogger`, `JsonWriterService`, `FileNameSanitizer`): Keine Business-Logik.

---

## Modulverantwortungen (nicht vermischen)

| Modul | Verantwortung | Nicht erlaubt |
|-------|--------------|---------------|
| `TiaPortalService` | Portal-Lebenszyklus | Datenverarbeitung |
| `TiaReflection` | API-Abstraktion + Fallbacks | Logik über Daten |
| Exporter (`Hardware*`, `PlcSoftware*`, etc.) | Daten extrahieren, in Models überführen | Direkt JSON schreiben |
| `CraPostProcessor` | CRA-Analyse, Scoring | TIA API aufrufen |
| `JsonWriterService` | JSON serialisieren + Dateien schreiben | Daten analysieren |
| `ExportLogger` | Logging | Datenverarbeitung |

---

## Zero Dependency Regel (ADR-001)

- Keine neuen NuGet-Pakete ohne explizite ADR-Entscheidung.
- Neue Funktionen zuerst mit `System.*` BCL-Mitteln implementieren.
- Bei absolutem Bedarf: ADR schreiben, Begründung dokumentieren, alternatives Vorgehen prüfen.

---

## Read-Only gegenüber TIA Portal (absolute Grenze)

- Kein Aufruf von Openness-Methoden, die Projektdaten verändern (`Compile`, `Save`, `Write*`, `Set*`).
- Diese Grenze darf nicht durch neue Features verletzt werden — auch nicht optional oder hinter Flags.

---

## Output-Format-Stabilität

- Änderungen an JSON-Property-Namen → breaking change für CMDB-Importer.
- Änderungen am ID-Algorithmus (`SHA256(...)` in `CraPostProcessor`) → bestehende CMDB-Einträge werden zu Duplikaten.
- Solche Änderungen erfordern: CMDB-Koordination + Versionsmarkierung im `manifest.json`.

---

## Openness API: Resilient Programming

- Jeder Openness-Aufruf muss mit `try/catch` + Fallback umgeben sein (Vorbild: `TiaReflection.cs`).
- Keine Annahme über Verfügbarkeit bestimmter Attribute über TIA-Versionen hinweg.
- Fehlende Attribute = `null`/default, nie Exception propagieren aus Extraction-Methoden.

---

## Keine neuen globalen Zustände

- `ExportLogger` ist der einzige akzeptierte "Singleton"-ähnliche Service.
- Neuer globaler State → immer hinterfragen. Warum global? Kann es als Parameter übergeben werden?

---

## Konfiguration von Logik trennen

- `ExportSettings` (immutable Record) ist die einzige legitime Konfigurationsquelle.
- Keine Konfigurationswerte aus Environment Variables oder Registry lesen (außer für TIA-Produkterkennung, die bereits in `CraPostProcessor` implementiert ist).

---

## Neue Exporter-Klassen

Wenn ein neuer Exporter (z.B. für neue TIA-Komponenten) gebraucht wird:
1. Neues `src/Tia/XxxExporter.cs` anlegen — `internal sealed class`.
2. Datenmodell in `NormalizedModels.cs` ergänzen.
3. In `ExportApplication.cs` einbinden.
4. In `CraPostProcessor.cs` CRA-Relevanz prüfen und ggf. ergänzen.
5. In `JsonWriterService.cs` Output-Pfad definieren.
6. `architecture-summary.md` aktualisieren.
