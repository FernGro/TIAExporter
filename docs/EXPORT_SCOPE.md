# Export Scope

TIAExporter exports reachable engineering data through Siemens TIA Portal Openness.

Included:

- Project metadata and manifest
- Device and DeviceItem metadata
- Controller candidates found via `PlcSoftware`
- PLC blocks and types where `Export(FileInfo, ExportOptions)` is supported
- PLC tag tables where export is supported
- Project library items where export is supported
- HMI device detection
- Normalized JSON for CMDB/CRA import preparation
- SHA256 evidence inventory for exported files

Individual object failures are logged and do not abort the complete export.
