# Troubleshooting PLC Block Export

If PLC blocks show `export_success=false`:

1. Open the project in TIA Portal.
2. Compile/check the project and PLC software consistency.
3. Check whether the PLC/project is open or locked by another TIA instance.
4. Check whether blocks are know-how protected.
5. Check whether Safety, STEP 7 Professional, WinCC or Startdrive modules required by the project are installed.
6. Check whether the Windows user is in the Siemens Openness group.
7. Run the exporter with `--diagnostics-only true`.
8. Inspect `diagnostics/block_export_failures.json`.

Relevant fields:

- `export_error_type`
- `export_error_message`
- `export_error_stack_short`
- `suspected_reason`
- `required_action`
- `is_consistent`
- `is_compiled`
- `is_protected`
- `is_safety_related`
- `can_export_xml`
- `can_export_document`
- `fallback_export_used`

If XML export fails but document export succeeds, the block appears in `normalized/software_inventory.json` with `export_status=document_only`. If both fail, the block remains visible as `metadata_only`, `license_unavailable` or `failed` with diagnostic details.

## TIA Shows A License But Openness Cannot Use It

Symptom: `diagnostics/openness_environment.json` shows `step7_professional_software_installed=true` but `step7_professional_license_available_for_openness=false`. The export log may show `LicenseNotFoundException: Necessary license 'STEP 7 Professional' is missing.` for block export calls. Affected blocks in `normalized/software_blocks.json` have `evidence_status=license_unavailable`.

Meaning: the exporter does not prove that the license is not installed. It records that Siemens Openness reported the license as not usable by the exporter process/session.

Common causes:

- License exists but is already occupied by another TIA process.
- Floating license server is not reachable from this user/session.
- ALM sees a license, but it does not match the TIA Portal/Openness version used by the exporter.
- Exporter runs in a different Windows session/context than the TIA GUI check.
- Stale hidden `Siemens.Automation.Portal` processes hold or block the license.
- Project features require an additional module/license.

Classic STEP 7 V5.x licenses are separate from TIA Portal licenses. They may be visible in ALM but do not necessarily satisfy TIA Portal V17+ Openness block export.

Checks:

1. Open Siemens Automation License Manager.
2. Confirm the license is available, not only installed.
3. Check floating license server reachability.
4. Close stale hidden `Siemens.Automation.Portal` processes if no visible TIA session needs them.
5. Run the exporter from the same Windows user/session used to verify the license in TIA.
6. Re-run the exporter.

Exporter behavior when Openness reports a license issue:

- The first block that triggers `LicenseNotFoundException` is logged as `ERROR` with full stack trace.
- Later blocks are still attempted because license failures can be object-type or feature specific.
- No document fallback is attempted for that block when XML export failed due to a license exception.
- `normalized/export_report.json` contains `license_blocking_failure_detected=true`, `missing_license_name`, `license_diagnostic_summary`, `affected_export_categories`, and `manual_actions`.
- `diagnostics/openness_environment.json` contains `step7_professional_license_available_for_openness=false` and diagnostic notes explaining the installation-vs-process-availability distinction.
