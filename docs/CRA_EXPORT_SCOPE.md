# CRA Export Scope

TIAExporter produces engineering evidence for CRA/CMDB review. It does not claim legal conformity.

Included data areas:

- Asset and firmware inventory from TIA hardware metadata
- Network interfaces, subnets and topology where exposed by Openness
- PLC blocks, UDTs and tag tables with XML/document/metadata-only status
- Library type and master copy metadata with export status
- HMI and drive detection with limitations when WinCC/Startdrive APIs are unavailable
- Safety inventory and explicit HIGH gaps for missing F-block evidence
- Security-relevant CPU/network settings normalized from hardware attributes
- SHA256 hashes and evidence index for generated files
- CRA gap analysis and export quality score

The output is designed for PostgreSQL CMDB import because records use stable IDs, normalized fields, explicit status values and evidence file references.
