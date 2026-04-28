# Export Limitations

TIA Openness coverage depends on installed TIA Portal products, project version, licenses, object protection, compile state and Windows Openness permissions.

Important limitations:

- The tool does not automatically compile or modify the project.
- Compile-check is limited to readable attributes such as consistency, compiled or protection flags when exposed by the installed API.
- Protected, inconsistent or safety-related blocks may only be exported as metadata.
- HMI tags, screens and connections may require WinCC Openness support.
- SINAMICS parameters may require Startdrive Openness support.
- TIA does not expose complete dependency chains like package managers do; library dependency gaps are heuristic and require review.
- Security findings are CMDB/CRA review findings, not legal compliance statements.
