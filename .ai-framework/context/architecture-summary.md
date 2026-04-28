# Architecture Summary — TIAExporter

**Letzte Aktualisierung:** 2026-04-28

---

## Systemkontext

TIAExporter ist ein Windows-Desktop-Tool für Siemens-TIA-Portal-Umgebungen. Es agiert als **Brücke zwischen TIA Portal (Openness API) und CMDB/CRA-Compliance-Systemen**. Ausgabe ist ein strukturiertes JSON-basiertes Datenpaket mit SHA256-Hashes für Audit-Zwecke.

```
┌─────────────────┐       ┌──────────────────┐       ┌──────────────────────┐
│  TIA Portal V20 │  ──►  │  TIAExporter.exe │  ──►  │  Export-Verzeichnis  │
│  (Openness API) │       │  (this tool)     │       │  JSON + XML + MD     │
└─────────────────┘       └──────────────────┘       └──────────────────────┘
```

---

## Layer-Architektur

```
┌────────────────────────────────────────────────────────┐
│  Presentation Layer                                     │
│  ├─ Program.cs            (CLI/GUI Entry Point Router)  │
│  ├─ MainForm.cs           (WinForms: GUI-Modus)         │
│  └─ CliOptions.cs         (CLI-Argument-Parser)         │
├────────────────────────────────────────────────────────┤
│  Application Layer                                      │
│  └─ ExportApplication.cs  (Pipeline-Orchestrierung)    │
│     └─ ExportSettings.cs  (Immutable Config Record)    │
├────────────────────────────────────────────────────────┤
│  Domain Layer: TIA-Daten-Extraktion                    │
│  ├─ TiaPortalService.cs   (Portal Lifecycle)            │
│  ├─ TiaReflection.cs      (API-Abstraktion, Fallbacks) │
│  ├─ HardwareExporter.cs   (Devices, Module, Netzwerk)  │
│  ├─ PlcSoftwareExporter.cs (Blöcke, XML, Metadata)     │
│  ├─ TagTableExporter.cs   (Tag-Tabellen)               │
│  ├─ LibraryExporter.cs    (Projektbibliotheken)        │
│  ├─ HmiExporter.cs        (HMI-Detektion)              │
│  └─ ProjectScanner.cs     (Hilfsfunktionen)            │
├────────────────────────────────────────────────────────┤
│  Domain Layer: Normalisierung & CRA-Analyse            │
│  ├─ NormalizedModels.cs   (19+ Datenklassen)           │
│  ├─ CraPostProcessor.cs   (CRA-Kategorien, Gap, Score) │
│  ├─ CraMetadataParser.cs  (Regex-Metadaten-Tags)       │
│  └─ EvidenceHasher.cs     (SHA256 Audit Trail)         │
├────────────────────────────────────────────────────────┤
│  Infrastructure Layer                                   │
│  ├─ ExportLogger.cs       (Thread-sicherer Logger)     │
│  ├─ JsonWriterService.cs  (Custom JSON-Serializer)     │
│  └─ FileNameSanitizer.cs  (Pfad-Normalisierung)        │
├────────────────────────────────────────────────────────┤
│  Compatibility                                          │
│  └─ IsExternalInit.cs     (NET48 Record-Polyfill)      │
└────────────────────────────────────────────────────────┘
```

---

## Datenfluss (vereinfacht)

```
TIA Portal Projekt (.ap20)
    │
    ▼ TiaPortalService.OpenProject()
    │
    ├──► HardwareExporter ──────────┐
    ├──► PlcSoftwareExporter ───────┤
    ├──► TagTableExporter ──────────┤──► ExportState (In-Memory)
    ├──► LibraryExporter ───────────┤
    └──► HmiExporter ───────────────┘
                                    │
                                    ▼ CraPostProcessor.Process()
                                    │
                                    ├── BuildAssetInventory()
                                    ├── BuildSecurityConfiguration()
                                    ├── BuildGapAnalysis()        (13 CRA-Kategorien)
                                    └── BuildQualityScore()       (9 gewichtete Kategorien)
                                    │
                                    ▼ JsonWriterService.WriteAll()
                                    │
                                    └── Output-Verzeichnis:
                                        ├── manifest.json
                                        ├── normalized/*.json
                                        ├── diagnostics/*.json
                                        ├── reports/CRA_EXPORT_READINESS.md
                                        ├── software/*/blocks/
                                        └── logs/export.log
```

---

## Schlüsselkomponenten im Detail

### TiaReflection.cs — API-Abstraktion
- Problem: Openness API-Attribute variieren zwischen TIA-Versionen (V15–V20)
- Lösung: Reflection-basierter Zugriff mit Fallbacks bei fehlenden Properties
- Methoden: `GetValue()`, `GetString()`, `GetBool()`, `TryExportWithOptions()`
- **Kritisch:** Jede Änderung hier kann alle Exporter betreffen

### CraPostProcessor.cs — Kern der CRA-Logik
- Baut 13 CRA-Readiness-Kategorien
- Stabile ID-Generierung: `SHA256(ProjectName|ObjectPath|OrderNumber|TypeIdentifier)` → 16 Hex
- Security-Findings: SNMP "public" (HIGH), fehlende NTP (LOW), PUT/GET aktiviert
- Gap-Analysis: HIGH/MEDIUM/LOW Severities mit Evidence-Referenzen
- **Achtung:** 506 Zeilen — größte einzelne Logik-Datei im Projekt

### JsonWriterService.cs — Custom Serializer
- Keine externen JSON-Bibliotheken (Zero-Dependency)
- PropertyName → snake_case Konvertierung
- Rekursive Serialisierung von Objects, Dictionaries, Arrays
- **Achtung:** Änderungen am Output-Format betreffen CMDB-Import-Kompatibilität

### NormalizedModels.cs — Datenmodell
- 19+ POCO-Klassen und Records
- Sehr große Datei (~19.000 Zeilen) — bewusst als Single-File für Übersicht
- Enthält alle DTOs für Hardware, Software, Netzwerk, CRA, Safety, HMI

---

## Architekturgrenzen

| Grenze | Beschreibung |
|--------|-------------|
| **TIA Portal API** | Nur read-only; kein Compile, kein Schreiben in Projekt |
| **Windows only** | WinForms + Siemens Openness = kein Cross-Platform |
| **x64 only** | Siemens DLLs sind x64 |
| **NET48** | Siemens Openness V20 hat keine .NET 5+ Unterstützung |
| **Lokal only** | Kein Netzwerk, kein Remote-Zugriff |

---

## Bekannte Architekturentscheidungen (ADRs)

1. **Zero NuGet Dependencies:** Robustheit in Produktionsumgebungen; keine transitive Dependency-Probleme.
2. **Custom JSON Serializer:** Vermeidet Json.NET / System.Text.Json Versionskonflikt mit NET48.
3. **Reflection-basierte API-Abstraktion:** Toleriert TIA Portal V15–V20 API-Unterschiede.
4. **SHA256-basierte stabile IDs:** CMDB-Deduplication über Projekt-Rebuilds hinweg.
5. **Single-File NormalizedModels.cs:** Alle Datenklassen in einer Datei für schnelle Navigation (bewusste Entscheidung gegen Splittung).
