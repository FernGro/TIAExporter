# Entscheidungslog — TIAExporter

**Format:** Datum | Entscheidung | Begründung | Status

---

## Architekturentscheidungen (aus Code abgeleitet)

### ADR-001: Zero NuGet Dependencies
**Datum:** Unbekannt (aus Codebase abgeleitet)  
**Entscheidung:** Keine externen NuGet-Pakete. Alle Funktionen über .NET Framework 4.8 BCL implementiert.  
**Begründung:** Produktionsumgebungen bei Kunden (Dorst) sind oft air-gapped oder restriktiv bezüglich externer Pakete. Transitive Dependency-Probleme vermeiden. Kleine Exe ohne Package-Manager-Overhead.  
**Status:** Aktiv — bei neuen Funktionen einhalten.

---

### ADR-002: Custom JSON Serializer statt Json.NET / System.Text.Json
**Datum:** Unbekannt  
**Entscheidung:** `JsonWriterService.cs` implementiert eigene snake_case JSON-Serialisierung via Reflection.  
**Begründung:** NET48 + Json.NET Versionskonflikt mit Siemens Openness DLLs historisch problematisch. System.Text.Json nicht auf NET48 verfügbar.  
**Status:** Aktiv — Output-Format stabil halten (CMDB-Import-Kompatibilität).

---

### ADR-003: Reflection-basierte Openness API-Abstraktion
**Datum:** Unbekannt  
**Entscheidung:** `TiaReflection.cs` greift auf Openness-Objekte via .NET Reflection zu statt direkter API-Calls.  
**Begründung:** TIA Portal V15–V20 haben unterschiedliche API-Attribute. Reflection mit Fallbacks ermöglicht Multi-Version-Kompatibilität ohne Conditional Compilation.  
**Status:** Aktiv — bei API-Updates sorgfältig testen.

---

### ADR-004: SHA256-basierte stabile Asset-IDs
**Datum:** Unbekannt  
**Entscheidung:** Asset-IDs = `SHA256(ProjectName|ObjectPath|OrderNumber|TypeIdentifier)` → 16 Hex.  
**Begründung:** Stabile IDs über Projekt-Rebuilds hinweg ermöglichen CMDB-Deduplication. Verhindert Phantomeinträge bei wiederholten Exporten.  
**Status:** Aktiv — ID-Algorithmus nicht ohne CMDB-Koordination ändern.

---

### ADR-005: NormalizedModels.cs als Single-File
**Datum:** Unbekannt  
**Entscheidung:** Alle 19+ POCO-Datenklassen in einer Datei (~19.000 Zeilen).  
**Begründung:** Schnelle Navigation aller Datenstrukturen an einem Ort. Ändert sich selten.  
**Status:** Aktiv — Datei ist groß aber stabil. Kein Refactoring ohne klaren Nutzen.

---

### ADR-006: asInvoker (keine UAC-Elevation)
**Datum:** Unbekannt  
**Entscheidung:** `app.manifest` setzt `asInvoker` — Tool läuft ohne Admin-Rechte.  
**Begründung:** Openness API erfordert nur Windows-Gruppenmitgliedschaft (nicht Admin). Least-Privilege-Prinzip.  
**Status:** Aktiv.

---

### ADR-007: .ai-framework Framework-Initialisierung
**Datum:** 2026-04-28  
**Entscheidung:** Universelles AI-Engineering-Framework unter `.ai-framework/` angelegt.  
**Begründung:** Strukturierte Entwicklung mit Agenten-Rollen, Sicherheits- und CRA-Prüfung, nachvollziehbare Änderungsprozesse.  
**Status:** Aktiv — Framework ist Leitstruktur für alle zukünftigen Änderungen.

---

## Neue Entscheidungen hier dokumentieren

Format:
```
### ADR-NNN: Titel
**Datum:** YYYY-MM-DD
**Entscheidung:** Was wurde entschieden?
**Begründung:** Warum? Welche Alternativen wurden verworfen?
**Status:** Aktiv / Abgelöst durch ADR-XXX / Experimentell
```
