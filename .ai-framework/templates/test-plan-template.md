# Test Plan Template

> Für neue Tests. Kopieren und ausfüllen → `TIAExporter.Tests/XxxTests.cs`.

---

## Zu testende Klasse
`TIAExporter.Normalization.XxxClass` (oder anderer Namespace)

## Testdatei
`TIAExporter.Tests/XxxClassTests.cs`

## Testfälle

### Happy Path
| Test-ID | Input | Erwarteter Output | Beschreibung |
|---------|-------|------------------|-------------|
| TC-001 | `"valid_input"` | `"expected_output"` | Normalfall |

### Edge Cases
| Test-ID | Input | Erwarteter Output | Beschreibung |
|---------|-------|------------------|-------------|
| TC-010 | `""` | Exception / leer / default | Leerer String |
| TC-011 | `null` | Exception / null-safe | Null-Input |
| TC-012 | `"a".PadRight(300)` | Truncated/Error | Zu langer Input |

### Negativtests (Security)
| Test-ID | Input | Erwarteter Output | Beschreibung |
|---------|-------|------------------|-------------|
| TC-020 | `"../../evil"` | Sanitisierter Pfad | Path-Traversal |
| TC-021 | `"CON"` | Sanitisierter Name | Windows-reservierter Name |

---

## Testprojekt-Konfiguration

```xml
<!-- TIAExporter.Tests/TIAExporter.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="NUnit" Version="3.14.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.5.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <ProjectReference Include="..\TIAExporter.csproj" />
  </ItemGroup>
</Project>
```

## Ausführung
```bash
dotnet test TIAExporter.Tests/TIAExporter.Tests.csproj
```
