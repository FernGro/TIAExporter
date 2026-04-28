# CRA Export Readiness

Project: SPT_V1_0_20251210
Export timestamp: 2026-04-28T13:23:32.5079764+02:00
Quality score: 11/100

This report assesses engineering export completeness for CRA/CMDB review. It does not certify legal CRA conformity.

## Category Status

| Category | Status | Severity | Evidence | Required action |
| --- | --- | --- | --- | --- |
| Asset inventory | MISSING | HIGH | 0 assets detected | Run hardware export on engineering station. |
| Firmware inventory | MISSING | HIGH | 0 firmware/order entries detected | Verify MLFB and firmware manually for modules with missing values. |
| Network inventory | MISSING | MEDIUM | 0 network interfaces, 0 links | Review network_interfaces.json and topology manually. |
| Software block inventory | MISSING | HIGH | 0 blocks detected, 0 full XML exports | Fix block XML export or compile/unprotect project. |
| Library inventory | MISSING | HIGH | 0 library items, 0 exported | Review library_export_failures.json and export libraries manually if required. |
| HMI inventory | MISSING | HIGH | 0 HMI assets detected | Install/enable WinCC Openness module or verify manually. |
| Drive inventory | MISSING | LOW | 0 drive assets detected | Install/enable Startdrive support or verify manually. |
| Safety inventory | NOT_APPLICABLE | INFO | 0 safety blocks detected | Verify safety project access and exported F-blocks. |
| Security configuration | MISSING | HIGH | 2 review findings | Review normalized/security_configuration.json and findings. |
| Evidence / hashes | MISSING | HIGH | 0 evidence files hashed | Ensure all generated files are listed in evidence_files.json. |
| Vulnerability lookup readiness | MISSING | MEDIUM | 0 lookup keys | Complete order number and firmware fields. |
| SBOM readiness | MISSING | MEDIUM | 0 block XML exports, 0 library exports | Use library_dependency_gaps.json for manual review. |
| Manual review required | PARTIAL | HIGH | 7 HIGH gaps | Resolve HIGH gaps or document accepted limitations. |

## HIGH Gaps

- Asset inventory: Missing assets reduce CMDB coverage. Required action: Run hardware export on engineering station.
- Firmware inventory: Vulnerability lookup readiness is reduced. Required action: Verify MLFB and firmware manually for modules with missing values.
- Software block inventory: Software components cannot be fully analyzed or hashed. Required action: Fix block XML export or compile/unprotect project.
- Library inventory: Library provenance/dependency evidence is incomplete. Required action: Review library_export_failures.json and export libraries manually if required.
- HMI inventory: HMI attack surface may be underrepresented. Required action: Install/enable WinCC Openness module or verify manually.
- Security configuration: Security posture cannot be fully reviewed from export. Required action: Review normalized/security_configuration.json and findings.
- Evidence / hashes: Audit evidence chain is incomplete. Required action: Ensure all generated files are listed in evidence_files.json.
- Manual review required: Manual engineering review remains required. Required action: Resolve HIGH gaps or document accepted limitations.

## Limitations

- CRA readiness is an engineering-data quality assessment; it is not a legal compliance certification.
- TIA Openness coverage depends on installed TIA modules, project licenses, protection state and object model availability.
