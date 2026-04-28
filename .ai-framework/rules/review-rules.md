# Review Rules — TIAExporter

**Letzte Aktualisierung:** 2026-04-28

---

## Was jeder Review prüft

1. **Funktionalität:** Erfüllt die Änderung die Anforderung aus `task-brief.md`?
2. **Keine Regression:** Bestehende Funktionen unverändert? Output-Dateien stabil?
3. **Architektur:** Layer-Grenzen eingehalten? Keine Vermischung von Verantwortungen?
4. **Code-Qualität:** Sprechende Namen, keine Magic Numbers, keine duplizierte Logik?
5. **Fehlerbehandlung:** Alle Openness-Calls in Try-Catch? Logging korrekt?
6. **Security:** Path-Traversal abgesichert? Keine Secrets? Read-Only-Grenze eingehalten?
7. **CRA:** Hat die Änderung CRA-Auswirkung? Wenn ja, ist `cra-impact-report.md` ausgefüllt?
8. **Tests:** Testbare Logik getestet? Neue Edge Cases abgedeckt?
9. **Dokumentation:** README, docs/, context/ aktuell?

---

## Review-Ergebnis

- **APPROVED:** Alle Checks grün, Dokumentation aktuell.
- **APPROVED WITH MINOR NOTES:** Kleinere Punkte dokumentiert, kein Blocker.
- **REVISION REQUIRED:** Blocker identifiziert — Änderung muss überarbeitet werden.

---

## Besondere Achtsamkeit bei

- Änderungen in `CraPostProcessor.cs` → CRA-fachliche Prüfung zwingend
- Änderungen in `JsonWriterService.cs` → Output-Format-Kompatibilität prüfen
- Änderungen in `EvidenceHasher.cs` → Audit-Trail-Integrität prüfen
- Änderungen am SHA256-ID-Algorithmus → CMDB-Stabilität prüfen (OQ-007)
- Neue Dependencies → ADR-001 Ausnahme erforderlich
- Neue CLI-Optionen → README + Security-Review
