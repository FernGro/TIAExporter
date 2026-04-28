# Export Limitations

This export is generated through Siemens TIA Portal Openness. The exact reachable object model depends on the installed TIA version, project configuration, licenses and Openness permissions.

Known limitations:

- Hardware is always normalized from API metadata; full HWCN/AML export is not guaranteed.
- HMI devices are detected, but screen and detailed WinCC tag export can require project-specific API handling.
- Project library dependency chains may be incomplete because TIA Openness does not always expose all usage relationships.
- Objects that do not provide Export(FileInfo, ExportOptions) are recorded as metadata only.
- Individual object export failures are logged and do not stop the full export.