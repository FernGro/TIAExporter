# Repository Assessment — TIAExporter

**Erstellt:** 2026-04-28  
**Analysiert von:** AI Framework Initialisierung

---

## Projektidentität

| Merkmal | Wert |
|---------|------|
| **Projektname** | TIAExporter |
| **Typ** | Windows Desktop Tool (CLI + WinForms GUI) |
| **Zweck** | CRA/CMDB-Export aus Siemens TIA Portal Projekten |
| **Auftraggeber** | Dorst Technologies (intern) |
| **Sprache** | C# (.NET Framework 4.8) |
| **Plattform** | Windows x64 only |
| **Codeumfang** | ~3.571 Zeilen C# (exkl. NormalizedModels.cs) |
| **NuGet-Abhängigkeiten** | Keine (Zero-Dependency Design) |
| **Tests** | Keine Unit-Tests vorhanden |
| **CI/CD** | Keine |

---

## Verzeichnisstruktur

```
TIAExporter/
├── Program.cs                    # Entry Point: CLI/GUI-Router
├── TIAExporter.csproj            # .NET Framework 4.8, x64
├── TIAExporter.sln
├── app.manifest                  # asInvoker, keine UAC-Elevation
├── README.md
├── src/
│   ├── App/
│   │   ├── CliOptions.cs         # CLI-Argument-Parser
│   │   ├── ExportApplication.cs  # Orchestrierung aller Exporter
│   │   └── ExportSettings.cs     # Immutable Record: Export-Optionen
│   ├── Gui/
│   │   └── MainForm.cs           # WinForms: GUI-Modus
│   ├── Logging/
│   │   └── ExportLogger.cs       # Thread-sicherer Logger (Memory + File)
│   ├── Tia/
│   │   ├── TiaPortalService.cs   # TIA Portal Lifecycle (Open/Close)
│   │   ├── TiaReflection.cs      # Reflection-Wrapper für Openness API
│   │   ├── HardwareExporter.cs   # Hardware + Netzwerk-Scan
│   │   ├── PlcSoftwareExporter.cs # PLC-Block-XML-Export
│   │   ├── TagTableExporter.cs   # Tag-Tabellen-Export
│   │   ├── LibraryExporter.cs    # Projekt-Bibliothek-Export
│   │   ├── HmiExporter.cs        # HMI-Detektion
│   │   ├── ProjectScanner.cs     # Projekt-Hilfsfunktionen
│   │   └── FileNameSanitizer.cs  # Pfad-Normalisierung
│   ├── Normalization/
│   │   ├── NormalizedModels.cs   # 19+ POCO-Datenklassen (~19.000 Zeilen)
│   │   ├── CraPostProcessor.cs   # CRA-Analyse + Scoring (506 Zeilen)
│   │   ├── JsonWriterService.cs  # Custom JSON-Serializer (452 Zeilen)
│   │   ├── EvidenceHasher.cs     # SHA256-Hashing für Audit Trail
│   │   └── CraMetadataParser.cs  # Regex-Parser: @cra/@safety Block-Tags
│   └── Compatibility/
│       └── IsExternalInit.cs     # Compiler-Polyfill für NET48
├── docs/
│   ├── CRA_EXPORT_SCOPE.md
│   ├── EXPORT_LIMITATIONS.md
│   └── TROUBLESHOOTING_BLOCK_EXPORT.md
└── test_export_*/                # Beispiel-Export-Ausgaben (kein Testcode)
```

---

## Externe Abhängigkeiten

| Bibliothek | Version | Typ | Beschaffung |
|-----------|---------|-----|-------------|
| `Siemens.Engineering.dll` | V20 | Lokal (TIA Portal) | `C:\Program Files\Siemens\Automation\Portal V20\PublicAPI\V20\` |
| `Siemens.Engineering.Hmi.dll` | V20 | Lokal | Gleicher Pfad |
| `Siemens.Engineering.Contract.dll` | V20 | Lokal | `Portal V20\Bin\PublicAPI\` |
| `Siemens.Engineering.ClientAdapter.Interfaces.dll` | V20 | Lokal | Gleicher Pfad |

**Kritische Abhängigkeit:** TIA Portal V20 muss auf der Entwicklungsmaschine installiert sein. Build schlägt ohne lokale TIA-Installation fehl.

---

## Architektur-Überblick

```
Program.cs (CLI/GUI Router)
    └─ ExportApplication.Run()
           ├─ TiaPortalService  (Portal Start/Attach/Close)
           ├─ HardwareExporter  (Devices, Module, Netzwerk)
           ├─ PlcSoftwareExporter (Block-XML, Metadata)
           ├─ TagTableExporter
           ├─ LibraryExporter
           ├─ HmiExporter
           └─ CraPostProcessor  (CRA-Analyse, Scoring, Gap-Analysis)
                  └─ JsonWriterService (Output: JSON + MD Reports)
```

**Design Patterns:** Facade (TiaPortalService, TiaReflection), Strategy (Exporter), Pipeline (ExportApplication.Run), Builder (CraPostProcessor).

---

## Wichtige Sicherheitsaspekte

1. **Read-Only:** Das Tool modifiziert niemals TIA-Projektdaten.
2. **Keine Credentials:** Openness nutzt Windows Auth; keine Passwörter gespeichert.
3. **Keine Netzwerkkommunikation:** Ausschließlich lokaler Zugriff auf TIA Portal.
4. **Audit Trail:** SHA256-Hashing aller Output-Dateien via `EvidenceHasher.cs`.
5. **Security Findings:** Detektiert SNMP "public", fehlende NTP/Syslog, PUT/GET.
6. **UAC:** `asInvoker` — keine Elevation; läuft im User-Kontext.
7. **Windows-Gruppe:** Prüft ob User in "Openness"/"Siemens TIA" Gruppe ist.

---

## CRA-Relevanz

Das Tool **ist selbst ein CRA-Werkzeug** — es erzeugt SBOM-Daten, Asset-Inventare und Gap-Analysen für CRA-Compliance. Änderungen an folgenden Komponenten haben direkte CRA-Auswirkung:

- `CraPostProcessor.cs` — CRA-Kategorien, Scoring, Gap-Definitionen
- `NormalizedModels.cs` — SBOM-Datenstrukturen
- `EvidenceHasher.cs` — Audit Trail Integrität
- `JsonWriterService.cs` — Output-Format (CMDB-Import-Kompatibilität)

---

## Bekannte Schwächen / Offene Punkte

1. **Keine Unit-Tests** — Openness COM-Interfaces schwer mockbar; Integration Tests erfordern echte TIA-Installation.
2. **`NormalizedModels.cs` sehr groß** (~19.000 Zeilen) — viele POCO-Klassen in einer Datei; Refactoring möglich aber nicht dringend.
3. **`CraPostProcessor.cs` 506 Zeilen** — kann in Sub-Klassen aufgeteilt werden.
4. **Kein CI/CD** — Build nur lokal möglich (TIA Portal Abhängigkeit).
5. **Kein Async** — Openness API blockierend; für aktuelle Nutzungsszenarien akzeptabel.
6. **Kein Progress-Callback** — bei großen Projekten keine Fortschrittsanzeige.
7. **Teilweise Deutsch hardcoded** — GUI-Texte, Log-Meldungen nicht lokalisierbar.
