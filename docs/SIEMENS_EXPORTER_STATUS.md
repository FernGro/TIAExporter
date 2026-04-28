# Siemens Exporter Status

**Letzte Aktualisierung:** 2026-04-28  
**Repository:** `TIAExporter`  
**Zielsystem:** Siemens TIA Portal Openness, CRA-/CMDB-tauglicher Engineering-Export  
**Ziel dieser Datei:** Technischer Statusbericht fuer Wiki-Uebernahme. Diese Datei beschreibt den Ist-Zustand des Exporters, seine verifizierten Faehigkeiten, aktuelle Luecken, bekannte Blocker und den noch offenen Arbeitsumfang fuer CMDB, SBOM-nahe Auswertungen und CRA-Readiness.

---

## 1. Executive Summary

Der Exporter ist funktional deutlich ueber einen reinen Rohdatenexport hinaus erweitert worden. Er erzeugt heute nicht nur technische JSON-Fragmente, sondern auch Diagnosedateien, normalisierte Inventare, CRA-Gap-Analysen, Hash-Evidenz und einen Readiness-Report.

Der Schwerpunkt der letzten Ausbaustufe lag auf:

- Transparenz und Diagnose beim PLC-Blockexport
- Normalisierung fuer Asset-, Firmware-, Software-, Security- und Safety-Inventare
- Nachvollziehbare Evidenzdateien mit Hashes
- CRA-orientierter Bewertung des Exportumfangs

Der wichtigste aktuelle operative Blocker liegt nicht mehr primaer in der Datenmodellierung, sondern in der Laufzeitumgebung: In der derzeit verifizierten Testumgebung bricht der Start von TIA Openness bereits beim Aufbau der `TiaPortal`-Session mit `NonRecoverableException: Connection to TiaPortal failed.` ab. Dadurch koennen reale Exporte in dieser Umgebung derzeit nicht vollstaendig bis in den SPS-Bausteinexport durchlaufen.

Das bedeutet:

- Der Code fuer Diagnose, Normalisierung und Fehlersichtbarkeit ist implementiert.
- Die Build- und Teile-der-Testkette laufen.
- Die End-to-End-Validierung gegen ein laufendes TIA-Openness-Environment ist aktuell noch unvollstaendig blockiert.

Fuer CRA/CMDB ist der Exporter damit fachlich weit fortgeschritten, aber noch nicht als vollstaendig verifiziertes Produktionsartefakt einzustufen.

---

## 2. Architektur und Exportmodell

Der Exporter arbeitet in mehreren Schichten:

1. **TIA-Zugriff**
   - Oeffnet ein TIA-Projekt ueber Siemens Openness
   - Traversiert Hardware-, Netzwerk-, Software-, Bibliotheks- und weitere Engineering-Objekte

2. **Raw Export**
   - Schreibt technische JSON-Dateien wie `devices.json`, `software_blocks.json`, `hardware_attributes.json`
   - Legt, soweit moeglich, XML- oder Dokument-Evidenzen fuer exportierbare Objekte ab

3. **Diagnose**
   - Schreibt systematische Fehler- und Umfeldinformationen unter `diagnostics/`
   - Macht Exportausfaelle pro Objekt nachvollziehbar

4. **Normalisierung**
   - Verdichtet technische Rohdaten in CMDB-/CRA-orientierte Sichten unter `normalized/`
   - Erzeugt Inventare, Security-Findings, Safety-Sichten, Firmware-Schluessel und Gap-Analysen

5. **Reporting**
   - Erzeugt CRA-Readiness-Berichte und Scoring
   - Erzeugt Manifest und Evidence-Index

Die Grundausrichtung ist read-only gegenueber dem TIA-Projekt. Ein automatisches Compile ist standardmaessig deaktiviert.

---

## 3. Aktuell erzeugte Dateien und Artefaktklassen

### 3.1 Rohdaten

Der Exporter erzeugt oder soll erzeugen:

- `project.json`
- `devices.json`
- `controllers.json`
- `hardware_modules.json`
- `hardware_attributes.json`
- `network_interfaces.json`
- `network_links.json`
- `subnets.json`
- `tag_tables.json`
- `software_blocks.json`
- `libraries.json`
- `evidence_files.json`
- `export_report.json`
- `manifest.json`

### 3.2 Diagnostik

Der Exporter erzeugt oder soll erzeugen:

- `diagnostics/export_errors.json`
- `diagnostics/openness_environment.json`
- `diagnostics/block_export_failures.json`
- `diagnostics/library_export_failures.json`
- `diagnostics/capability_matrix.json`

### 3.3 Normalisierte Ausgaben

Der Exporter erzeugt oder soll erzeugen:

- `normalized/asset_inventory.json`
- `normalized/firmware_inventory.json`
- `normalized/software_inventory.json`
- `normalized/security_configuration.json`
- `normalized/security_findings.json`
- `normalized/safety_inventory.json`
- `normalized/hmi_inventory.json`
- `normalized/drive_inventory.json`
- `normalized/library_inventory.json`
- `normalized/library_dependency_gaps.json`
- `normalized/cra_gap_analysis.json`
- `normalized/export_quality_score.json`

### 3.4 Reports

Der Exporter erzeugt oder soll erzeugen:

- `reports/CRA_EXPORT_READINESS.md`

---

## 4. Was der Exporter heute bereits kann

### 4.1 Hardware- und Netzwerkexport

Implementiert:

- Rekursive Erfassung von Geraeten und DeviceItems
- Export von Hardwaremodulen
- Export von Hardwareattributen
- Export von Netzwerkschnittstellen
- Export von Netzwerkverbindungen
- Export von Subnetzen
- Controller-Erkennung

Nutzen fuer CMDB/CRA:

- Grundlegendes Asset-Inventar ist ableitbar
- Netzwerkkontext fuer SPS, HMI und weitere Komponenten ist vorhanden
- Firmware-, Order-Number- und Hardwareattribute koennen spaeter normalisiert werden

Einschraenkung:

- Qualitaet und Vollstaendigkeit haengen stark davon ab, welche Attribute Openness fuer das konkrete Geraet freigibt.

### 4.2 PLC-Softwareexport

Implementiert:

- Rekursive Traversierung von `PlcSoftware`
- Traversierung von BlockGroups, Untergruppen, Software Units und Typgruppen
- Exportversuche fuer SPS-Bloecke ueber mehrere Strategien
- Speicherung ausfuehrlicher Exportdiagnose je Block

Unterstuetzte Exportstrategie:

1. XML-Export ueber `Export(...)`
2. Mehrere `ExportOptions` per Reflection, nur wenn in installierter API vorhanden
3. Dokument-Fallback ueber `ExportAsDocument`, falls verfuegbar
4. Metadata-only-Fallback, wenn kein Dateiexport gelingt

Nutzen fuer CMDB/CRA:

- Vollstaendige Komponenteninventarisierung ist fachlich angelegt
- Auch bei fehlgeschlagenem Dateiexport bleibt Objektinventar nicht stumm leer
- Fehlerursachen koennen pro Block nachvollzogen werden

### 4.3 Tag Tables

Implementiert:

- Tag-Table-Erkennung und Export

Nutzen:

- Hilfreich fuer Import in CMDB, Signalbezug und Engineering-Kontext

### 4.4 Bibliotheken

Implementiert:

- Erkennung von Projektbibliotheken
- Metadatenorientierte Bibliotheksanalyse
- Exportstatus und Fehlerdetails
- Normalisierte Bibliothekssicht und Dependency-Gap-Datei

Nutzen:

- Bessere Sicht auf wiederverwendete Engineering-Komponenten
- Grundlage fuer manuelle Bibliothekszuordnung im CRA-/SBOM-Kontext

### 4.5 Security-Normalisierung

Implementiert:

- Extraktion relevanter Sicherheitskonfigurationen aus Hardware-/Netzwerkdaten
- Normalisierung in `security_configuration.json`
- Regelbasierte Ableitung von `security_findings.json`

Beispiele:

- PUT/GET aktiv
- OPC UA aktiv
- SNMP aktiv
- Router-/Forwarding-Konfiguration
- fehlende Syslog-/NTP-Konfiguration

Nutzen:

- Security Review Findings koennen ohne manuelles Durchsehen aller Rohattribute vorbereitet werden

### 4.6 Safety-Sicht

Implementiert:

- Safety-Erkennung ueber CPU-/Block-/Hardwareindikatoren
- Separate `safety_inventory.json`
- Eskalation, wenn Safety vorhanden ist, aber nicht sicher exportiert werden kann

Nutzen:

- Safety darf nicht still aus dem Export herausfallen
- Hohe Relevanz fuer CRA-Review und manuelle Nachpruefung

### 4.7 HMI- und Drive-Erkennung

Implementiert:

- Grundlegende HMI-Erkennung
- Grundlegende Drive-/Startdrive-Heuristiken
- Separate Normalisierungsdateien

Nutzen:

- HMI- und Drive-Relevanz wird sichtbar statt still ignoriert

Einschraenkung:

- Detaillierte HMI-/Startdrive-Daten haengen stark von installierten Modulen und Openness-Faehigkeiten ab

### 4.8 Evidence, Manifest, CRA-Reporting

Implementiert:

- Evidence-Dateiindex mit SHA256
- CRA Gap Analysis
- Export Quality Score
- CRA Readiness Report als Markdown
- erweitertes Manifest mit Summary-Feldern

Nutzen:

- Export ist als Audit-/Review-Artefakt deutlich brauchbarer
- Nachvollziehbarkeit gegenueber CMDB-Import, CRA-Pruefung und manueller Nacharbeit steigt deutlich

---

## 5. Was im Code implementiert ist, aber operativ nur teilweise verifiziert wurde

Folgende Bereiche sind implementiert, konnten in der aktuellen Testumgebung aber noch nicht vollstaendig end-to-end bestaetigt werden:

- Vollstaendiger PLC-Blockexport gegen das Beispielprojekt
- Breiter XML-Export fuer Bibliotheken
- HMI-Tiefenexport ueber WinCC-Openness-spezifische APIs
- Startdrive-/SINAMICS-spezifischer Tiefenexport
- Safety-spezifische Spezialfaelle in realen Projekten

Grund:

- Der TIA-Openness-Start scheitert in der aktuellen Umgebung bereits vor dem eigentlichen Projektzugriff.

Diese Differenz ist wichtig:

- **Implementiert** bedeutet: Codepfad vorhanden.
- **Verifiziert** bedeutet: in der aktuellen TIA-Laufzeit gegen reales Projekt erfolgreich beobachtet.

Aktuell ist nicht alles, was implementiert ist, auch end-to-end verifiziert.

---

## 6. Was heute noch fehlt oder nur heuristisch vorliegt

### 6.1 Fuer CMDB

Noch offen oder nur teilweise abgedeckt:

- Breiter Realbetriebstest mit mehreren Projekten
- Absicherung stabiler Schluessel fuer alle Asset-/Softwaretypen
- Tiefergehende HMI-Verbindungen, HMI-Tags und gegebenenfalls Screen-Metadaten
- Vollstaendigere Zuordnung von Bibliotheksquellen zu Bloecken
- Konsolidierter Nachweis, welche Attributnamen je Geraetetyp wirklich geliefert werden

### 6.2 Fuer SBOM-nahe Auswertungen

Wichtig: TIA-Projekte liefern keine klassische Paketmanager-SBOM wie `npm`, `pip` oder `NuGet`.

Was moeglich ist:

- Inventar von Engineering-Komponenten
- Bibliotheks- und Typbezug, soweit technisch sichtbar
- Firmware- und Produktidentifikatoren
- Nachvollziehbare Evidence-Dateien

Was noch fehlt:

- Vollstaendige und verifizierte Dependency Chain fuer Libraries
- Sichere Zuordnung von Standardbibliothek vs. kundenspezifische Bibliothek in allen Faellen
- Breite Hash-Abdeckung fuer exportierte Softwareartefakte in Projekten, in denen XML-Export aktuell blockiert ist

Fazit:

Der Exporter ist SBOM-unterstuetzend, aber kein klassischer SBOM-Generator.

### 6.3 Fuer CRA-Readiness

Noch offen:

- End-to-End-Nachweis, dass die implementierten Diagnose- und Normalisierungsdateien in realen TIA-Laeufen konsistent erzeugt werden
- Robuste Handhabung der aktuellen TIA-Openness-Startblockade
- Bessere reale Abdeckung fuer HMI, Drives und Safety-Sonderfaelle
- Mehr reale Exportlaeufe zur Kalibrierung von Heuristiken und Scoring

---

## 7. Wichtigste aktuelle Blocker

### 7.1 TIA Openness startet in der Testumgebung nicht stabil

Beobachteter Fehler:

- `NonRecoverableException: Connection to TiaPortal failed.`

Einordnung:

- Der Fehler tritt bereits beim Erzeugen der `TiaPortal`-Session auf.
- Das ist vor dem eigentlichen Export.
- Damit ist das nicht primaer ein Block-XML-Fehler, sondern ein Session-/IPC-/Umgebungsproblem.

Wahrscheinliche Ursachen:

- Haengende oder unsauber beendete `Siemens.Automation.Portal`-Prozesse
- Session-/Desktop-Kontextproblem
- Lizenz im TIA sichtbar, aber fuer den Openness-Prozess in dieser Session nicht nutzbar
- Openness-Gruppen- oder Rechteproblem
- TIA-/Openness-Installations- oder Modulinkonsistenz

Auswirkung:

- Reale Integrationstests koennen nicht verlaesslich voll durchlaufen
- Einige implementierte Exportpfade bleiben praktisch unbestaetigt

### 7.2 Historische Fehlinterpretation von Lizenzfehlern

Vorheriges Problem:

- Der Exporter hat frueher zu aggressiv behauptet, STEP 7 Professional sei nicht vorhanden, wenn Exception-Typen oder Meldungen nur unscharf auf Lizenz hindeuteten.

Behoben:

- Lizenzklassifizierung ist enger gefasst
- Reports sprechen nun von "Lizenz fuer diesen Prozess / diese Session nicht nutzbar", nicht pauschal von "Lizenz fehlt"
- Ein einzelner Lizenzfehler stoppt nicht mehr pauschal alle weiteren Blockversuche

Restproblem:

- Die reale Ursache, warum TIA in der aktuellen Umgebung nicht startet oder warum Lizenznutzung scheitert, ist damit sichtbar gemacht, aber nicht technisch beseitigt.

### 7.3 Solution-Build verhaelt sich noch inkonsistent

Beobachtet:

- `dotnet build TIAExporter.csproj --no-restore` erfolgreich
- `dotnet build tests\\TIAExporter.Tests.csproj --no-restore` erfolgreich
- Test-EXE erfolgreich
- Solution-Build zeigte zeitweise Exit Code `1` trotz fehlender fachlicher Buildfehler

Einordnung:

- Kein unmittelbarer Exportfachfehler
- Aber stoert robuste Automatisierung

### 7.4 Zugriff auf generierte Testordner / Artefakte

Beobachtet:

- Einzelne Testartefakte oder Diagnoseordner liessen sich wegen `Access denied` nicht immer bereinigen

Einordnung:

- Kein Kernproblem des Exporters
- Hinweis auf offene Dateihandles, Rechte oder parallel laufende Prozesse

---

## 8. Detaillierter Status nach Exportkategorie

### 8.1 Asset Inventory

**Status:** Teilweise implementiert, fachlich stark, operativ weiter zu verifizieren

Vorhanden:

- Geraete, DeviceItems, Pfade, Elternbezug
- Controller-Erkennung
- Asset-Normalisierung mit stabilen IDs

Offen:

- Breite Validierung ueber verschiedene Hardwarefamilien
- Feinschliff fuer HMI-/Drive-/Sondermodule

### 8.2 Firmware Inventory

**Status:** Implementiert, qualitaetsabhaengig von Rohattributen

Vorhanden:

- Firmwareversionen, Order Numbers, Suchschluessel fuer Schwachstellenabgleich

Offen:

- Reale Validierung an unterschiedlichen Siemens-Geraetefamilien
- Konsistente Feldbelegung fuer alle MLFB-/Firmware-Varianten

### 8.3 Network Inventory

**Status:** Bereits vergleichsweise stark

Vorhanden:

- Interfaces, Links, Subnetze, teils Rollenbezug

Offen:

- Tiefere Port-/Redundanz-/Monitoringwerte, soweit Openness diese liefert

### 8.4 Software Inventory

**Status:** Strukturell stark verbessert, aber realer Tiefenexport noch Hauptthema

Vorhanden:

- Blockinventar
- Detaillierte Exportdiagnose
- Exportstatus `full_xml`, `document_only`, `metadata_only`, `failed`
- Heuristiken fuer Safety/Network/HMI/Recipe/Diagnostics

Offen:

- Reale Bestätigung hoher XML-Exportquote im Beispielprojekt
- Klaerung, welche Bloecke wegen Inconsistency, Protection, API-Limitation oder Session-/Lizenzproblem scheitern

### 8.5 Library Inventory

**Status:** Vorhanden, aber im industriellen Detail noch limitiert

Vorhanden:

- Bibliotheksobjekte, Exportstatus, Fehlerdatei, Gap-Datei

Offen:

- Bessere reale Exportabdeckung
- Robustere Typ-/Versionszuordnung
- Grenzen der Openness-API sauber gegen echte Datenluecken abgrenzen

### 8.6 HMI Inventory

**Status:** Grundabdeckung vorhanden, Tiefenabdeckung offen

Vorhanden:

- HMI-Erkennung
- Normalisierungsdatei und Limitationen

Offen:

- HMI-Tags
- HMI-Verbindungen
- tiefergehende Runtime-/Screen-Daten, sofern API verfuegbar

### 8.7 Drive Inventory

**Status:** Heuristische Basis vorhanden

Vorhanden:

- Drive-Erkennung
- Startdrive-Capability-Betrachtung

Offen:

- Telegramm-/Parameter-/IO-System-Tiefenexport
- robuste Validierung mit Startdrive-Modul

### 8.8 Safety Inventory

**Status:** Fachlich korrekt separat behandelt, technisch weiter zu haerten

Vorhanden:

- Safety-Praesenz, F-CPU/F-Block-Indikatoren, Limitationen, Exportstatus

Offen:

- reale Verifikation gegen Safety-Projekte
- bessere Abdeckung von F-Parametern, soweit API zugaenglich

### 8.9 Security Configuration

**Status:** Gut fuer Review-Vorbereitung

Vorhanden:

- Normalisierte Security-Werte
- Finding-Ableitung mit Schweregraden

Offen:

- Feinjustierung der Regeln nach realen Projekten
- Projekt- oder Betreiberrichtlinien fuer "enabled vs disabled" sauberer abbilden

### 8.10 Evidence / Hashing

**Status:** Grundsaetzlich gut umgesetzt

Vorhanden:

- Evidence-Index fuer erzeugte Dateien
- SHA256
- Evidence-Level
- Nutzung fuer CRA-Kategorien

Offen:

- Sicherstellen, dass in jedem realen Lauf wirklich alle erzeugten Dateien indexiert werden

---

## 9. Relevanz fuer CMDB, SBOM und CRA

### 9.1 CMDB

Der Exporter ist fuer CMDB-Import deutlich besser geeignet als der urspruengliche Rohdatenexport, weil er:

- stabile Inventarobjekte normalisiert
- Asset-, Firmware-, Software- und Sicherheitsdaten trennt
- Objektpfade und Evidenz referenzierbar macht
- Hashes und Exportstatus liefert

Die Hauptluecke fuer eine belastbare CMDB-Produktivnutzung ist derzeit nicht das Datenmodell, sondern die noch unvollstaendig verifizierte Laufzeitstabilitaet gegen TIA Openness.

### 9.2 SBOM-nahe Nutzung

Der Exporter ist geeignet fuer:

- Firmware-Inventarisierung
- Zuordnung technischer Komponenten
- teilweise Bibliotheks- und Typbezug
- Exporttechnische Evidenz

Der Exporter ist nicht gleichbedeutend mit:

- einer vollstaendigen semantischen Software-Stueckliste im Sinne moderner Paketmanager
- einer beweisbar vollstaendigen Dependency-Aufloesung aller PLC-/HMI-/Library-Komponenten

### 9.3 CRA

Der Exporter unterstuetzt CRA-nahe Aufgaben durch:

- Inventarisierung
- Exportstatus je Objekt
- Evidence-Hashing
- Security-Findings
- Safety-Sichtbarkeit
- Gap Analysis
- explizite Limitations

Der Exporter garantiert nicht:

- rechtliche CRA-Konformitaet
- vollstaendige technische Exportierbarkeit jedes Siemens-Projekts
- automatische Beseitigung von Openness-, Lizenz- oder Projektkonsistenzproblemen

---

## 10. Verifikation und Teststand

### 10.1 Erfolgreich verifiziert

- `dotnet build TIAExporter.csproj --no-restore`
- `dotnet build tests\\TIAExporter.Tests.csproj --no-restore`
- `tests\\bin\\Debug\\net48\\TIAExporter.Tests.exe`

### 10.2 Teilweise verifiziert

- Dokumentations- und Normalisierungspfad
- Lizenzdiagnose-Logik
- Fehlerklassifizierung im PLC-Exportpfad

### 10.3 Nicht vollstaendig verifiziert

- End-to-End-Export mit aktiver TIA-Openness-Session in der aktuellen Umgebung
- Vollstaendige reale Generierung aller vorgesehenen Exportdateien aus dem Beispielprojekt

### 10.4 Warum die Verifikation aktuell haengt

Die Hauptblockade ist derzeit kein C#-Compilefehler mehr, sondern der TIA-Session-Start. Solange `TiaPortal` nicht stabil initialisiert werden kann, bleiben alle nachgelagerten Exportpfade nur teilweise beweisbar.

---

## 11. Offene technische Arbeit

### 11.1 Prioritaet 1

- TIA-Openness-Startproblem reproduzierbar isolieren und beseitigen
- Session-, IPC-, Prozess- und Lizenzkontext sauber diagnostizieren
- Beispielprojekt erneut end-to-end gegen reale TIA-Session laufen lassen

### 11.2 Prioritaet 2

- PLC-Blockexport real gegen das Beispielprojekt validieren
- Blockfehler nach realen Fehlerklassen auswerten:
  - inkonsistent
  - geschuetzt
  - Safety-Modul fehlt
  - API unterstuetzt Exporttyp nicht
  - Lizenz / Session nicht nutzbar

### 11.3 Prioritaet 3

- Bibliotheks-, HMI- und Drive-Abdeckung gegen reale Projekte haerten
- Heuristiken anhand realer Ausgaben nachschaerfen

### 11.4 Prioritaet 4

- Solution-Build-Verhalten bereinigen
- Testartefakt-Bereinigung robuster machen
- Mehr Unit-Tests fuer nicht-TIA-abhaengige Komponenten

---

## 12. Empfohlene naechste konkrete Schritte

1. TIA-Prozesslandschaft vor Exportstart systematisch pruefen und haengende `Siemens.Automation.Portal`-Prozesse ausschliessen.
2. Verifizieren, ob der Exporter im gleichen Windows-User-, Desktop- und Lizenzkontext wie ein erfolgreich gestartetes TIA laeuft.
3. Beispielprojekt mit `--diagnostics-only true` gegen eine nachweislich saubere TIA-Session laufen lassen.
4. Danach Voll-Export starten und `diagnostics/block_export_failures.json` auswerten.
5. Reale Exportdaten aus `software_inventory.json`, `cra_gap_analysis.json` und `export_quality_score.json` gegen Erwartung abgleichen.
6. HMI-/Drive-/Safety-Tiefenabdeckung erst nach erfolgreicher Basissession weiter ausbauen.

---

## 13. Kurzfazit

Der Siemens Exporter ist heute kein einfacher JSON-Dumper mehr, sondern ein deutlich erweiterter Engineering-Export mit Diagnose, Normalisierung, Evidenz und CRA-orientierter Bewertung. Die Datenstruktur fuer CMDB, Firmware-Inventar, Software-Inventar, Security-Findings und Gap-Analysen ist im Code im Wesentlichen angelegt.

Der aktuelle Engpass liegt nicht mehr primaer in fehlenden JSON-Dateien, sondern in der operativen Verifikation gegen eine funktionierende TIA-Openness-Session. Solange der Start von `TiaPortal` in der Zielumgebung mit `Connection to TiaPortal failed` abbrechen kann, bleibt der Exporter fachlich weit entwickelt, aber betrieblich noch nicht abschliessend abgesichert.

Fuer das Wiki ist daher die praezise Aussage:

- **Datenmodell und Diagnose deutlich erweitert**
- **CRA-/CMDB-Nutzen substanziell verbessert**
- **SBOM-nahe Sicht moeglich, aber keine klassische Voll-SBOM**
- **Hauptrestarbeit: stabile End-to-End-Verifikation gegen TIA Openness**
