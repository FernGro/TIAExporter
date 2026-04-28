# Offene Fragen — TIAExporter

**Letzte Aktualisierung:** 2026-04-28

---

## Technisch

### OQ-001: TIA Portal V17/V18/V19 Kompatibilität
**Frage:** Welche TIA Portal Versionen werden aktiv in Dorst-Projekten eingesetzt?  
**Relevanz:** `TiaReflection.cs` hat Fallbacks für API-Variabilität, aber nicht alle Versionen getestet.  
**Status:** Offen

### OQ-002: Multi-CPU-Projekte
**Frage:** Wie verhält sich der Exporter bei Projekten mit mehreren PLCs / CPUs?  
**Relevanz:** `ExportApplication.cs` iteriert über alle Controller — unklar ob parallele Verarbeitung möglich/nötig.  
**Status:** Offen

### OQ-003: Große Projekte (>100 Geräte)
**Frage:** Gibt es Performance-Limits bei sehr großen TIA-Projekten?  
**Relevanz:** Kein Async, kein Progress-Callback — UI könnte bei sehr großen Projekten einfrieren.  
**Status:** Offen

### OQ-004: Test-Strategie ohne TIA Portal
**Frage:** Welche Teile können ohne TIA Portal Installation getestet werden?  
**Relevanz:** `CraMetadataParser`, `FileNameSanitizer`, `EvidenceHasher`, `CraPostProcessor` (mit Mock-Daten) könnten testbar sein.  
**Status:** Offen — nächster Schritt wäre Mockability-Analyse

### OQ-005: .gitignore vollständig?
**Frage:** Sind alle sensiblen / lokalen Dateien in .gitignore?  
**Relevanz:** test_export_*/ enthält reale Projektdaten. Sollen diese commitet werden?  
**Status:** Offen

---

## Fachlich / CRA

### OQ-006: SBOM-Format
**Frage:** Soll der JSON-Output in Richtung CycloneDX oder SPDX-Format entwickelt werden?  
**Relevanz:** Aktuelle JSON-Struktur ist proprietär. CMDB-Integration unklar.  
**Status:** Offen

### OQ-007: Welche CMDB empfängt die Exports?
**Frage:** Welches System importiert die erzeugten JSON-Pakete?  
**Relevanz:** Output-Format und ID-Stabilität sind auf CMDB-Import ausgelegt — aber CMDB unbekannt.  
**Status:** Unbekannt / noch zu prüfen

### OQ-008: Aktualisierungsintervall für CRA-Gap-Kategorien
**Frage:** Wann und von wem werden die 13 CRA-Kategorien in `CraPostProcessor.cs` aktualisiert, wenn neue CRA-Anforderungen kommen?  
**Relevanz:** CRA Anhang I vollständig ab 11.12.2027. Kategorien müssen möglicherweise erweitert werden.  
**Status:** Offen

---

## Deployment / Betrieb

### OQ-009: Wie wird das Tool an Kunden verteilt?
**Frage:** Single-EXE, Installer, Zip-Paket?  
**Relevanz:** Siemens DLLs sind lokal abhängig — Tool kann nicht ohne TIA Portal Installation verwendet werden.  
**Status:** Unbekannt / noch zu prüfen

### OQ-010: Lizenz und IP-Fragen
**Frage:** Ist die Verwendung der Siemens Openness API (lizenzpflichtig?) für kommerzielle Tools abgeklärt?  
**Relevanz:** `Siemens.Engineering.dll` ist Teil von TIA Portal — Nutzungsrechte für Tool-Verteilung unklar.  
**Status:** Unbekannt / noch zu prüfen

---

## Neue offene Fragen hier eintragen

Format:
```
### OQ-NNN: Titel
**Frage:** Konkrete Frage
**Relevanz:** Warum ist diese Frage wichtig?
**Status:** Offen / In Klärung / Beantwortet (Antwort: ...)
```
