# .ai-framework — TIAExporter

**Version:** 1.0  
**Initialisiert:** 2026-04-28  
**Repo:** TIAExporter (C# .NET 4.8, Siemens TIA Portal CRA-Export-Tool)

---

## Zweck

Dieses Framework definiert den Entwicklungsprozess für TIAExporter. Es ist unabhängig von einem bestimmten KI-Tool nutzbar (Claude Code, Codex, Copilot Agent Mode oder manuell).

Die zentrale Wahrheit liegt in `.ai-framework/`. Produktivcode liegt in `src/`.

---

## Standardablauf für jede Aufgabe

```
1.  Aufgabe in exchange/task-brief.md erfassen
2.  Planner Agent erstellt exchange/implementation-plan.md
3.  Architect Agent prüft Architekturauswirkung → agents/architect.md
4.  Cybersecurity Agent prüft Sicherheitsauswirkung → agents/cybersec-engineer.md
5.  CRA Expert prüft CRA-Auswirkung → agents/cra-compliance-expert.md
6.  Coder Agent implementiert → agents/coder.md
7.  Test Engineer ergänzt / prüft Tests → agents/test-engineer.md
8.  Validator Agent prüft Ergebnis → agents/validator.md
9.  Documentation Engineer aktualisiert Dokumentation → agents/documentation-engineer.md
10. Ergebnis und offene Punkte in exchange/agent-handoff.md zusammenführen
```

**Bei kleinen Änderungen (< 20 Zeilen, kein Architektur-Impact):**  
Schritte 3, 5 als "N/A" dokumentieren wenn kein Impact. Schritt 9 (Dokumentation) ist immer Pflicht.

---

## Verzeichnisstruktur

```
.ai-framework/
├── README.md                   ← Diese Datei
├── context/
│   ├── repo-assessment.md      ← Technische Bestandsaufnahme
│   ├── architecture-summary.md ← Layer-Architektur + Datenfluss
│   ├── current-state.md        ← Aktueller Projektstatus
│   ├── decisions.md            ← ADRs (Architekturentscheidungen)
│   └── open-questions.md       ← Offene technische + fachliche Fragen
├── rules/
│   ├── global-rules.md         ← Verbindliche Grundregeln
│   ├── coding-rules.md         ← C#-Coding-Standards
│   ├── architecture-rules.md   ← Layer-Grenzen, Modulverantwortungen
│   ├── testing-rules.md        ← Teststrategie + Testpflichten
│   ├── documentation-rules.md  ← Dokumentationspflichten
│   ├── security-rules.md       ← Security-Grenzen + Prüfpflichten
│   ├── cra-compliance-rules.md ← CRA-Prüfpflichten
│   └── review-rules.md         ← Review-Checkliste
├── agents/
│   ├── architect.md            ← Architekturprüfung + ADRs
│   ├── planner.md              ← Aufgabenzerlegung + Plan
│   ├── coder.md                ← C#-Implementierung
│   ├── validator.md            ← QA + Review
│   ├── test-engineer.md        ← Unit Tests + Edge Cases
│   ├── documentation-engineer.md ← Doku aktuell halten
│   ├── cybersec-engineer.md    ← Security Review
│   ├── cra-compliance-expert.md ← CRA-Impact-Bewertung
│   └── refactoring-engineer.md ← Refactoring ohne Verhaltensänderung
├── skills/
│   ├── repo-analysis.md
│   ├── feature-planning.md
│   ├── implementation.md
│   ├── code-review.md
│   ├── test-generation.md
│   ├── refactoring.md
│   ├── security-review.md
│   ├── cra-impact-check.md
│   ├── documentation-update.md
│   └── release-check.md
├── pipelines/
│   ├── feature-pipeline.md
│   ├── bugfix-pipeline.md
│   ├── refactoring-pipeline.md
│   ├── security-pipeline.md
│   ├── documentation-pipeline.md
│   └── release-pipeline.md
├── exchange/                   ← Austauschdateien (werden pro Task befüllt)
│   ├── task-brief.md
│   ├── agent-handoff.md        ← ZENTRALE Übergabedatei
│   ├── implementation-plan.md
│   ├── validation-report.md
│   ├── test-report.md
│   ├── security-report.md
│   ├── cra-impact-report.md
│   └── decision-log.md
└── templates/
    ├── feature-request-template.md
    ├── bug-report-template.md
    ├── review-template.md
    ├── test-plan-template.md
    └── architecture-decision-record-template.md
```

---

## Kritische Regeln (Zusammenfassung)

| Regel | Grund |
|-------|-------|
| Read-Only gegenüber TIA-Projektdaten | Absolute Grenze — Tool darf nie TIA-Daten schreiben |
| Zero NuGet Dependencies (ADR-001) | Robustheit in Produktionsumgebungen |
| Output-Format stabil (ADR-002) | CMDB-Import-Kompatibilität |
| SHA256-ID stabil (ADR-004) | CMDB-Deduplication |
| Keine Netzwerkkommunikation | Offline-only Design |
| asInvoker (app.manifest) | Least Privilege |

---

## TIAExporter-Kontext

- **Sprache:** C# .NET Framework 4.8 (Windows x64)
- **Abhängigkeiten:** Siemens TIA Portal V20 (lokal, keine NuGet)
- **Modi:** CLI (headless) + WinForms GUI
- **Output:** JSON (normalized/) + XML (raw) + Markdown (reports/)
- **CRA-Relevanz:** Tool erzeugt CRA-Compliance-Evidenz für Dorst
- **Tests:** Keine automatischen Tests vorhanden (OQ-004)
- **CI/CD:** Nicht vorhanden

---

## Kompatibilität mit bestehenden Control-Dateien

Das bestehende `C:\ClaudeDev\CyberSec\.claude\CLAUDE.md` und übergeordnete Governance-Regeln bleiben unverändert gültig. Dieses Framework ergänzt sie für das TIAExporter-Repo.  
Konflikte: `.ai-framework/` gewinnt für repo-spezifische Implementierungsentscheidungen.
