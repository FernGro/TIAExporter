# Feature Request Template

> Kopieren und ausfüllen → in `exchange/task-brief.md` einfügen.

---

## Feature-Titel
[Prägnanter Name, max. 60 Zeichen]

## Motivation
**Als** [Wer nutzt das Feature?]  
**möchte ich** [Was soll das Feature tun?]  
**damit** [Welchen Nutzen bringt es?]

## Beschreibung
[Detaillierte Beschreibung: Was genau soll das Feature tun? Wo setzt es an (CLI, GUI, Output)?]

## Akzeptanzkriterien
- [ ] [Messbar: Was muss wahr sein wenn das Feature fertig ist?]
- [ ] [...]

## Beispiel
```
# Beispielaufruf (wenn CLI-Feature):
TIAExporter.exe --neues-flag "wert"

# Beispiel-Output (wenn Output-Feature):
{
  "neues_feld": "wert"
}
```

## Nicht in Scope
[Was soll das Feature explizit NICHT tun?]

## Bekannte Risiken
- [ ] Breaking Change am Output-Format?
- [ ] CMDB-Auswirkung?
- [ ] CRA-Impact?
- [ ] Erfordert TIA Portal V20 API?

## Priorität
- [ ] BLOCKING
- [ ] HIGH
- [ ] MEDIUM
- [ ] LOW
