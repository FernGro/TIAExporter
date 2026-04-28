# Architecture Decision Record Template

> Kopieren und in `context/decisions.md` einfügen.

---

### ADR-NNN: [Titel]

**Datum:** YYYY-MM-DD  
**Status:** Aktiv / Abgelöst durch ADR-XXX / Experimentell  
**Entscheider:** [Agent / Person]  
**Task-Kontext:** `[TASK-ID]` (falls aus konkreter Aufgabe entstanden)

---

**Kontext**  
[Was ist der Hintergrund? Warum muss diese Entscheidung getroffen werden?]

**Entscheidung**  
[Was genau wurde entschieden? Klar und präzise.]

**Begründung**  
[Warum wurde diese Option gewählt? Was spricht dafür?]

**Verworfene Alternativen**  
- **Alternative A:** [Beschreibung] — Verworfen wegen: [Grund]
- **Alternative B:** [Beschreibung] — Verworfen wegen: [Grund]

**Konsequenzen**  
- [Was ändert sich durch diese Entscheidung?]
- [Welche Risiken entstehen?]
- [Welche Einschränkungen entstehen?]

**Gilt für**  
[Welche Dateien / Module sind betroffen?]

---

## Beispiel

### ADR-008: Async/Await nicht einführen

**Datum:** 2026-05-01  
**Status:** Aktiv

**Kontext**  
Openness API-Aufrufe sind blockierend. GUI friert bei langen Exports ein.

**Entscheidung**  
Kein Async/Await einführen. Stattdessen: GUI läuft auf Background Thread via `Task.Run()`.

**Begründung**  
Openness API ist nicht thread-safe. Async würde COM-Thread-Affinity-Probleme erzeugen.  
`Task.Run()` auf GUI-Seite reicht aus um UI reaktiv zu halten.

**Verworfene Alternativen**  
- **Vollständiges Async:** Openness COM-Interop nicht async-kompatibel — verworfen.

**Konsequenzen**  
- GUI bleibt reaktiv via Background-Task
- Openness-Code bleibt synchron (kein Refactoring nötig)
- Kein CancellationToken möglich (Openness-Limitation)
