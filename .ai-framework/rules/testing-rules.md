# Testing Rules — TIAExporter

**Letzte Aktualisierung:** 2026-04-28

---

## Kontext: Testbarkeit im TIAExporter

Das Repo hat keine Unit-Tests. Die Openness COM-Interfaces sind schwer mockbar.  
**Strategie:** Testbare Logik isolieren und gezielt testen; Openness-Abhängigkeiten akzeptieren.

---

## Was ist testbar (ohne TIA Portal)

| Klasse | Testbar | Methode |
|--------|---------|---------|
| `CraMetadataParser` | Ja | Unit-Test: Regex gegen string-Input |
| `FileNameSanitizer` | Ja | Unit-Test: Eingabe/Ausgabe-Paare |
| `EvidenceHasher` | Ja | Unit-Test: bekannter Input → bekannter SHA256 |
| `CraPostProcessor` | Teilweise | Unit-Test mit Mock-`ExportState` (POCO-Daten) |
| `JsonWriterService` | Ja | Unit-Test: serialisiere bekanntes Objekt, prüfe JSON-String |
| Exporter (Tia/) | Nein | Erfordern TIA Portal V20 |
| `TiaPortalService` | Nein | Erfordert TIA Portal |
| `MainForm` | Nein | WinForms UI |

---

## Pflichtregeln

1. **Neue Logik in testbaren Klassen braucht Tests.**  
   Wenn `CraMetadataParser` erweitert wird → Testfall ergänzen.

2. **Bugs in testbaren Klassen brauchen Regressionstests.**  
   Kein Fix ohne Test, der den Bug reproduziert und dann grün ist.

3. **Edge Cases müssen explizit geprüft werden:**
   - `FileNameSanitizer`: Leerzeichen, Sonderzeichen, leerer String, zu langer Pfad
   - `CraMetadataParser`: fehlende Tags, falsch formatierte Tags, mehrfache Tags
   - `EvidenceHasher`: leere Datei, nicht vorhandene Datei
   - `CraPostProcessor`: leere ExportState, ExportState mit nur einem Gerät

4. **Tests müssen deterministisch sein.**  
   Kein Zufalls-Input, keine Zeit-Abhängigkeit, keine Reihenfolge-Abhängigkeit.

5. **Keine Tests schreiben, die nur Implementierungsdetails prüfen.**  
   Prüfe Verhalten (Input → Output), nicht Methodennamen oder interne Variablen.

6. **Keine Tests entfernen ohne Begründung in `agent-handoff.md`.**

7. **Testdaten müssen nachvollziehbar sein.**  
   Keine magic Input-Strings — Kommentar warum dieser Input den Edge Case abdeckt.

8. **Kritische Pfade haben Priorität:**
   - SHA256 ID-Generierung (CMDB-Stabilität)
   - CRA-Scoring-Algorithmus (fachliche Korrektheit)
   - Metadaten-Tag-Parsing (Kunden-Inputs)
   - Pfad-Sanitisierung (Security)

9. **Security-relevante Funktionen brauchen Negativtests:**
   - `FileNameSanitizer`: Path-Traversal-Eingaben (`../../etc/passwd`)
   - `CraMetadataParser`: Regex-Injection-Versuch
   - `EvidenceHasher`: manipulierte Datei erkennbar

---

## Testprojekt-Struktur (noch zu erstellen)

```
TIAExporter.Tests/
  ├── TIAExporter.Tests.csproj    (NUnit oder xUnit, net48)
  ├── CraMetadataParserTests.cs
  ├── FileNameSanitizerTests.cs
  ├── EvidenceHasherTests.cs
  ├── JsonWriterServiceTests.cs
  └── CraPostProcessorTests.cs    (mit Mock-ExportState)
```

**Hinweis:** Testprojekt noch nicht angelegt — OQ-004 offen.

---

## Integrationstests (manuell, mit TIA Portal)

Bis automatisierte Tests möglich sind, gelten die `test_export_*/` Verzeichnisse als Referenz-Outputs.  
Bei Änderungen an Exporter-Logik: manuell prüfen dass Output-Struktur unverändert.
