# Security Rules — TIAExporter

**Letzte Aktualisierung:** 2026-04-28

---

## Kritische Grenzen (absolut)

1. **Keine Secrets im Repository.**  
   Keine Passwörter, API-Keys, Tokens, Zertifikat-Passwörter. Openness nutzt Windows Auth — so belassen.

2. **Das Tool ist read-only gegenüber TIA-Projektdaten.**  
   Kein Write, kein Compile, kein Save über Openness API. Diese Grenze ist absolut.

3. **Keine Netzwerkkommunikation.**  
   Kein HTTP-Client, kein Remote-Zugriff. Das Tool arbeitet offline. Diese Grenze ist absolut.

---

## Eingabe-Validierung

4. **Alle Dateipfade validieren und sanitisieren.**  
   `FileNameSanitizer.cs` für alle Ausgabe-Dateipfade verwenden. Eingabe-Pfade prüfen auf Existenz, Zugreifbarkeit, kein Path-Traversal.

5. **CLI-Parameter validieren.**  
   `CliOptions.cs` validiert Eingaben — bei neuen Parametern Validierung ergänzen. Ungültige Kombinationen mit klarer Fehlermeldung ablehnen.

6. **Alle Pfad-Inputs gegen Path-Traversal absichern.**  
   Kein `Path.Combine(userInput, ...)` ohne vorherige Normalisierung. Bekannte Angriffsvektoren: `../../`, `%2e%2e/`, absolute Pfade in relativen Parametern.

---

## Output-Sicherheit

7. **Fehlerausgaben dürfen keine sensiblen Informationen leaken.**  
   Exception-Messages im Log können Stack-Traces enthalten — kein ungefiltrerter User-Output von internen Exceptions.  
   Logging Level `TRACE` für technische Details, `ERROR`/`WARN` für User-sichtbare Meldungen.

8. **Logging darf keine Secrets oder personenbezogenen Daten enthalten.**  
   TIA-Projektinhalte (Block-Code, Tag-Werte, Netzwerkadressen) im Log nur wo für Debugging nötig und nur auf `TRACE`-Level.

9. **SHA256-Evidence-Hashing erhalten und korrekt implementieren.**  
   `EvidenceHasher.cs` muss alle Output-Dateien korrekt hashen. Keine Umgehung des Hashings. Audit-Trail ist CRA-Anforderung.

---

## Abhängigkeits-Sicherheit

10. **Keine neuen Abhängigkeiten ohne Prüfung.**  
    Bei ADR-Anfrage für neue NuGet-Pakete: Paket-Provenienz prüfen, bekannte CVEs prüfen, Siemens-Kompatibilität sicherstellen.

11. **Siemens DLLs lokal referenziert — keine NuGet-Siemens-Pakete.**  
    Siemens Openness DLLs kommen von der lokalen TIA-Installation. Kein `Siemens.Engineering` von nuget.org.

---

## Zugriffsrechte

12. **`asInvoker` erhalten (app.manifest).**  
    Kein `requireAdministrator` hinzufügen. Das Tool läuft im User-Kontext — Openness erfordert nur Windows-Gruppenmitgliedschaft.

13. **Windows-Gruppen-Prüfung erhalten.**  
    `CraPostProcessor.TryCheckOpennessGroup()` prüft ob User in "Openness"/"Siemens TIA" Gruppe — Security-Diagnostik erhalten.

---

## CRA-Security-Findings (fachlich)

14. **Heuristics in `CraPostProcessor.cs` konservativ halten.**  
    Neue Security-Findings (SNMP, PUT/GET, Syslog, NTP) nur mit fachlicher Begründung und CRA-Bezug hinzufügen.  
    Keine False-Positive-Flut — jedes Finding muss handlungsrelevant sein.

15. **Security-Finding-Severity korrekt verwenden:**  
    - `HIGH`: Direktes Angriffspotential (z.B. SNMP community="public")
    - `MEDIUM`: Konfigurationsrisiko mit bekanntem Exploit-Pfad
    - `LOW`: Best-Practice-Abweichung ohne direkten Exploit

---

## Sicherheitsrelevante Tests

16. **Path-Traversal-Tests für `FileNameSanitizer`.**  
    Eingaben wie `../../secret`, `/etc/passwd`, `CON`, `NUL` (Windows-reservierte Namen) testen.

17. **SHA256-Korrektheit testen.**  
    `EvidenceHasher` mit bekanntem Input → bekanntem Hash testen (Regressionssicherheit).
