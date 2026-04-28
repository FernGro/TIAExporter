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

If XML export fails but document export succeeds, the block appears in `normalized/software_inventory.json` with `export_status=document_only`. If both fail, the block remains visible as `metadata_only` or `failed` with diagnostic details.
