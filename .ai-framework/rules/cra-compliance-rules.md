# CRA Compliance Rules — TIAExporter

**Letzte Aktualisierung:** 2026-04-28  
**Hinweis:** Diese Regeln betreffen die technische CRA-Relevanz von Codeänderungen. Keine juristische Beratung. Offene rechtliche Bewertungen werden explizit als offen markiert.

---

## Kontext: TIAExporter als CRA-Werkzeug

TIAExporter ist selbst ein CRA-Compliance-Werkzeug für Dorst. Änderungen an diesem Tool können daher:
- Die Qualität der CRA-Analyse für Dorst-Kunden beeinflussen
- Selbst CRA-Anforderungen unterliegen (wenn als Produktkomponente eingesetzt)

---

## Pflichtprüfungen bei jeder Änderung

### 1. Softwarekomponenten und SBOM
- Neue externe Abhängigkeiten (NuGet, DLLs) → in `open-questions.md` als SBOM-relevant markieren.
- Geänderte Siemens-DLL-Versionen → SBOM-Eintrag aktualisieren.
- Neue eigene Module → für SBOM-Dokumentation in `decisions.md` vermerken.

### 2. Schwachstellenmanagement
- Neue externe Abhängigkeiten auf bekannte CVEs prüfen (NVD, vendor bulletins).
- Sicherheitskritische Fehler in `CraPostProcessor` oder `EvidenceHasher` → als HIGH-Priority behandeln.
- Bekannte Einschränkungen, die Sicherheitslücken erzeugen könnten → in `EXPORT_LIMITATIONS.md` dokumentieren.

### 3. Updatefähigkeit
- Das Tool muss durch einfaches Ersetzen der EXE updatebar bleiben.
- Keine Installationsroutinen einführen, die Updates verkomplizieren.
- Bei Breaking Changes im Output-Format → Versionsmarkierung in `manifest.json` implementieren.

### 4. Logging und Nachvollziehbarkeit (CRA Art. 13)
- Der `export.log` und `manifest.json` sicherstellen: Jede Export-Session vollständig protokolliert.
- `EvidenceHasher` muss alle Output-Dateien abdecken — keine ungehashten Output-Files.
- SHA256-Evidence-Chain darf nicht rückwirkend manipulierbar sein.

### 5. Sichere Standardkonfiguration (Security-by-Default)
- Default-CLI-Optionen müssen sicher sein (kein `--compile-check true` als Default).
- Neue CLI-Optionen: sicherer Default dokumentieren.
- Keine Funktionen, die standardmäßig unsicher sind.

### 6. Technische Dokumentation (CRA Anhang II)
- `README.md`, `docs/`, `.ai-framework/context/` müssen Zweck, Funktion und Einschränkungen des Tools dokumentieren.
- Bei relevantem Feature-Update: Dokumentation sofort aktualisieren.

### 7. Security-by-Design
- Neue Funktionen: Sicherheitsauswirkung zuerst bedenken.
- CRA-Findings in `CraPostProcessor` nur mit fachlicher Begründung ändern.
- Keine neuen Netzwerk-Features (absolut verboten — OQ-010 offen).

### 8. CRA-Kategorien und Gap-Analysis
- Die 13 CRA-Readiness-Kategorien in `CraPostProcessor.BuildGapAnalysis()` sind fachliche Heuristiken.
- Änderungen hier haben direkte Auswirkung auf Kundendokumentation.
- Jede Änderung an CRA-Kategorien: CRA Expert Agent Review erforderlich + ADR.

---

## Kritische CRA-Deadlines im Kontext

| Datum | Anforderung | Relevanz für TIAExporter |
|-------|-------------|--------------------------|
| 11.09.2026 | CRA Art. 14 Meldepflichten | Tool-Outputs als Evidenz für Meldungen verwenden |
| 20.01.2027 | MVO Cybersicherheitsanforderungen | Dorst-Maschinen mit TIA Portal → CRA-Export-Scope |
| 11.12.2027 | CRA Anhang I vollständig | 13 Kategorien möglicherweise erweiterungsbedürftig |
| 11.12.2027 | CRA Art. 32 Konformitätsbewertung | Tool-Outputs als CMDB-Evidenz |

---

## Was zu markieren ist

- Neue Softwarekomponenten → `[CRA-SBOM]` in `decisions.md`
- Offene rechtliche Bewertungen → `[CRA-OFFEN: Beschreibung]` in `open-questions.md`
- Technische CRA-Risiken → in `security-report.md` oder `cra-impact-report.md`

---

## Was niemals zu tun ist

- Keine CRA-Compliance behaupten, wenn sie nicht nachgewiesen ist.
- Keine Scoring-Werte ohne fachliche Basis erfinden.
- Keine Gap-Kategorien entfernen, die real existierende CRA-Anforderungen abbilden.
