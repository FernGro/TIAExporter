# TIAExporter

TIAExporter creates Siemens TIA Portal Openness exports for CRA/CMDB engineering review. It does not certify legal CRA conformity; it improves the technical evidence base: assets, firmware, software blocks, libraries, security-relevant settings, diagnostics, hashes, limitations and gap analysis.

## Build

```powershell
dotnet build
```

The project targets `net48`, WinForms and x64 because Siemens TIA Portal Openness V20 is referenced from the local installation.

## CLI

```powershell
bin\Debug\net48\TIAExporter.exe --project "C:\Path\Project.ap20" --out "C:\TIA_Export" --headless false
```

`--project` accepts TIA Portal project files (`.ap15` ... `.ap20`) and project archives (`.zap15` ... `.zap20`). Archives are retrieved into a short temporary work folder to avoid Windows path-length failures; older archives such as `.zap17` are retrieved with upgrade when required by the installed TIA Openness version.

Supported options:

- `--diagnostics-only true|false`
- `--compile-check true|false`
- `--compile-before-export true|false` (default false; automatic compile is not performed)
- `--include-hmi true|false`
- `--include-drives true|false`
- `--include-libraries true|false`
- `--include-documents true|false`
- `--strict true|false` (non-zero exit if HIGH CRA gaps remain)

## GUI

Run without arguments:

```powershell
bin\Debug\net48\TIAExporter.exe
```

The GUI supports HMI, Drive, Library, Document and Strict toggles, a diagnostics-only run, export quality score display and opening the export folder.

## Output

Key outputs:

- `diagnostics/openness_environment.json`
- `diagnostics/block_export_failures.json`
- `diagnostics/library_export_failures.json`
- `diagnostics/capability_matrix.json`
- `normalized/asset_inventory.json`
- `normalized/firmware_inventory.json`
- `normalized/software_inventory.json`
- `normalized/security_configuration.json`
- `normalized/security_findings.json`
- `normalized/safety_inventory.json`
- `normalized/hmi_inventory.json`
- `normalized/drive_inventory.json`
- `normalized/cra_gap_analysis.json`
- `normalized/export_quality_score.json`
- `normalized/evidence_files.json`
- `reports/CRA_EXPORT_READINESS.md`

Each generated file is hashed and listed in `normalized/evidence_files.json`.

## More Documentation

- `docs/CRA_EXPORT_SCOPE.md`
- `docs/EXPORT_LIMITATIONS.md`
- `docs/TROUBLESHOOTING_BLOCK_EXPORT.md`
