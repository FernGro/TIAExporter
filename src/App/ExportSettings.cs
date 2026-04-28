using TIAExporter.Normalization;

namespace TIAExporter.App;

internal sealed record ExportSettings(
    string ProjectPath,
    string OutputPath,
    bool Headless,
    bool DiagnosticsOnly = false,
    bool CompileCheck = false,
    bool CompileBeforeExport = false,
    bool IncludeHmi = true,
    bool IncludeDrives = true,
    bool IncludeLibraries = true,
    bool IncludeDocuments = true,
    bool Strict = false)
{
    public ExportSettingsSnapshot ToSnapshot() => new()
    {
        Headless = Headless,
        DiagnosticsOnly = DiagnosticsOnly,
        CompileCheck = CompileCheck,
        CompileBeforeExport = CompileBeforeExport,
        IncludeHmi = IncludeHmi,
        IncludeDrives = IncludeDrives,
        IncludeLibraries = IncludeLibraries,
        IncludeDocuments = IncludeDocuments,
        Strict = Strict
    };
}

internal sealed record ExportResult(bool Success, bool PartialSuccess, string StatusText);
