# Global Engineering Rules — TIAExporter

**Gültig für:** Alle Änderungen im Repository  
**Letzte Aktualisierung:** 2026-04-28

---

## Verbindliche Grundregeln

1. **Code muss modular, wartbar, lesbar und testbar sein.**  
   Neue Klassen folgen dem bestehenden Namespace-Schema `TIAExporter.{Modul}`.

2. **Keine unnötige Komplexität.**  
   Neue Abstraktionen nur einführen, wenn sie einen nachweisbaren Nutzen haben. Drei ähnliche Zeilen sind besser als eine voreilige Abstraktion.

3. **Keine großen monolithischen Methoden.**  
   Methoden über 80 Zeilen begründen oder aufteilen. Klassen über 300 Zeilen begründen oder aufteilen. Ausnahme: `NormalizedModels.cs` (bewusste ADR-Entscheidung).

4. **Keine stillen Fehler.**  
   Exceptions entweder behandeln (Try-Catch + Logging + State-Update) oder explizit propagieren. Kein leerer Catch-Block.

5. **Keine unvalidierten Eingaben an Systemgrenzen.**  
   Alle Pfade, Dateinamen, CLI-Parameter müssen validiert und sanitisiert werden (→ `FileNameSanitizer.cs` nutzen).

6. **Keine hart kodierten Geheimnisse.**  
   Keine Passwörter, API-Keys, Tokens im Code oder in Konfigurationsdateien. Openness nutzt Windows Auth — so lassen.

7. **Keine Änderungen ohne nachvollziehbaren Grund.**  
   Jede relevante Änderung wird in `agent-handoff.md` dokumentiert. Keine "Cleanup"-Änderungen ohne Aufgaben-Kontext.

8. **Keine Architekturänderung ohne Dokumentation.**  
   Neue Designentscheidungen → `decisions.md` (ADR). Geänderte Architektur → `architecture-summary.md`.

9. **Jede relevante Änderung aktualisiert laufende Dokumentation.**  
   `current-state.md` bei jedem Abschluss einer Aufgabe aktualisieren.

10. **Bestehende Funktionalität darf nicht unbeabsichtigt verändert werden.**  
    Vor Änderungen: Welche anderen Module nutzen die geänderte Klasse? Welche Output-Dateien werden beeinflusst?

11. **Bei Unsicherheit: Annahme explizit dokumentieren.**  
    In Code-Kommentar oder in `open-questions.md`. Keine Annahme still machen.

12. **Keine spekulativen Implementierungen.**  
    Keine Features für hypothetische zukünftige Anforderungen. Implementiere nur, was die aktuelle Aufgabe erfordert.

13. **Keine Dummy-Implementierungen ohne klare Kennzeichnung.**  
    Wenn Platzhalter nötig: `// TODO: [REASON] — nicht produktiv verwenden` mit Begründung.

14. **Keine Tests entfernen, nur weil sie fehlschlagen.**  
    Fehlschlagende Tests → Ursache verstehen und beheben oder in `open-questions.md` dokumentieren.

15. **Jede Änderung muss prüfbar, rückverfolgbar und erklärbar sein.**  
    Kein Magic — jede nicht-offensichtliche Entscheidung hat eine Erklärung.

---

## TIAExporter-spezifische Zusatzregeln

16. **Das Tool ist read-only gegenüber TIA-Projektdaten.**  
    Keine Methoden hinzufügen, die TIA-Projektinhalte schreiben, kompilieren oder verändern.

17. **Zero-Dependency-Prinzip einhalten.**  
    Keine neuen NuGet-Pakete ohne explizite ADR-Entscheidung (ADR-001). Neue Funktionen zuerst mit BCL-Mitteln versuchen.

18. **Output-Format-Stabilität.**  
    Änderungen an JSON-Output-Struktur oder SHA256-ID-Algorithmus (ADR-002, ADR-004) erst nach CMDB-Koordination. Solche Änderungen sind breaking changes.

19. **CRA-Kategorien und Scoring nur nach fachlicher Prüfung ändern.**  
    `CraPostProcessor.cs` enthält fachliche Heuristiken mit direkter CRA-Auswirkung. Änderungen erfordern CRA-Agent-Review.

20. **TiaReflection.cs: Fallbacks erhalten.**  
    Beim Hinzufügen von API-Zugriffen immer graceful Fallback implementieren — TIA Portal V15–V20 haben unterschiedliche APIs.
