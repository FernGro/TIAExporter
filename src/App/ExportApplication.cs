using TIAExporter.Logging;
using TIAExporter.Normalization;
using TIAExporter.Tia;

namespace TIAExporter.App;

internal sealed class ExportApplication
{
    private readonly ExportLogger logger;
    private readonly JsonWriterService jsonWriter = new();
    private readonly EvidenceHasher hasher = new();

    public ExportApplication(ExportLogger logger)
    {
        this.logger = logger;
    }

    public ExportResult Run(ExportSettings settings)
    {
        var state = CreateState(settings);
        Directory.CreateDirectory(state.ExportRoot);
        Directory.CreateDirectory(Path.Combine(state.ExportRoot, "logs"));
        logger.AttachFile(Path.Combine(state.ExportRoot, "logs", "export.log"));

        try
        {
            ValidateSettings(settings);
            logger.Info("Starting export.");
            logger.Info($"Project: {settings.ProjectPath}");
            logger.Info($"Output:  {state.ExportRoot}");

            using var tia = new TiaPortalService(logger);
            var project = tia.OpenProject(settings.ProjectPath, settings.Headless);
            state.TiaPortalVersion = tia.GetVersion();
            state.ProjectName = GetProjectName(project, settings.ProjectPath);

            if (settings.CompileBeforeExport)
            {
                state.Warnings.Add("compile-before-export was requested, but automatic project compilation is intentionally not implemented because it can modify project state. Compile in TIA manually and rerun.");
                state.RequiredManualActions.Add("Compile project in TIA manually before export if full XML block export is required.");
            }

            if (settings.CompileCheck)
            {
                state.Warnings.Add("compile-check is limited to readable block consistency/compiled attributes where exposed by Openness. No compile operation is executed.");
            }

            var hardwareExporter = new HardwareExporter(logger, jsonWriter);
            var plcCandidates = hardwareExporter.Export(project, state);

            if (!settings.DiagnosticsOnly)
            {
                var plcExporter = new PlcSoftwareExporter(logger, hasher);
                var tagExporter = new TagTableExporter(logger, hasher);
                foreach (var plc in plcCandidates)
                {
                    plcExporter.Export(plc, state);
                    tagExporter.Export(plc, state);
                }

                new LibraryExporter(logger, hasher).Export(project, state);
                if (settings.IncludeHmi)
                {
                    new HmiExporter(logger).Export(project, state);
                }
            }
            else
            {
                state.Warnings.Add("Diagnostics-only mode: software, tag, library and document exports were skipped.");
            }

            state.ExportSuccess = true;
            logger.Info("Export finished.");
        }
        catch (Exception ex)
        {
            state.ExportSuccess = false;
            logger.Error("Export failed", ex);
            state.Errors.Add($"{ex.GetType().Name}: {ex.Message}");
            state.ExportErrors.Add(new ExportErrorDiagnostic
            {
                Category = "EXPORT_RUNTIME",
                Context = settings.ProjectPath,
                ErrorType = ex.GetType().Name,
                ErrorMessage = ex.Message,
                StackShort = string.IsNullOrWhiteSpace(ex.StackTrace) ? null : string.Join(Environment.NewLine, ex.StackTrace.Split([Environment.NewLine], StringSplitOptions.None).Take(5)),
                SuspectedReason = "TIA Portal Openness runtime could not complete the export operation.",
                RequiredAction = "Verify TIA Portal can start with the selected UI/headless mode, project is accessible, and user has Openness permissions."
            });
        }
        finally
        {
            new CraPostProcessor().Process(state);
            RefreshCounts(state);
            WriteLimitations(state.ExportRoot);
            jsonWriter.WriteAll(state);
        }

        var strictFailed = settings.Strict && state.CraGapAnalysis.Any(x => x.Severity == "HIGH");
        var success = state.ExportSuccess && state.Errors.Count == 0 && !strictFailed;
        var partial = state.ExportSuccess && state.Errors.Count > 0;
        var status = success ? "Export erfolgreich." : strictFailed ? "Export abgeschlossen, aber strict wegen HIGH CRA gaps fehlgeschlagen." : partial ? "Export teilweise erfolgreich." : "Export fehlgeschlagen.";
        logger.Info(status);
        return new ExportResult((success || partial) && !strictFailed, partial, status);
    }

    private static ExportState CreateState(ExportSettings settings)
    {
        var exportRoot = ResolveExportRoot(settings.OutputPath);
        return new ExportState
        {
            ProjectPath = Path.GetFullPath(settings.ProjectPath),
            ExportRoot = exportRoot,
            ProjectName = Path.GetFileNameWithoutExtension(settings.ProjectPath),
            ExportTimestamp = DateTimeOffset.Now,
            Settings = settings.ToSnapshot()
        };
    }

    private static string ResolveExportRoot(string outputPath)
    {
        var full = Path.GetFullPath(outputPath);
        if (!Directory.Exists(full))
        {
            return full;
        }

        var hasFiles = Directory.EnumerateFileSystemEntries(full).Any();
        if (!hasFiles)
        {
            return full;
        }

        return Path.Combine(full, $"export_{DateTime.Now:yyyyMMdd_HHmmss}");
    }

    private static void ValidateSettings(ExportSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ProjectPath) || !File.Exists(settings.ProjectPath))
        {
            throw new FileNotFoundException("TIA project file not found.", settings.ProjectPath);
        }

        var extension = Path.GetExtension(settings.ProjectPath);
        var supported = new[] { ".ap20", ".ap19", ".ap18", ".ap17", ".ap16", ".ap15" };
        if (!supported.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Unsupported project extension: {extension}");
        }
    }

    private static string GetProjectName(object project, string projectPath)
    {
        return TiaReflection.GetString(project, "Name") ?? Path.GetFileNameWithoutExtension(projectPath);
    }

    private static void RefreshCounts(ExportState state)
    {
        state.Counts["devices"] = state.Devices.Count;
        state.Counts["device_items"] = state.HardwareModules.Count;
        state.Counts["plc_blocks"] = state.SoftwareBlocks.Count(x => !string.Equals(x.BlockType, "DB", StringComparison.OrdinalIgnoreCase));
        state.Counts["tag_tables"] = state.TagTables.Count;
        state.Counts["udts"] = state.SoftwareBlocks.Count(x => x.ExportFile?.IndexOf("/udts/", StringComparison.OrdinalIgnoreCase) >= 0);
        state.Counts["dbs"] = state.SoftwareBlocks.Count(x => string.Equals(x.BlockType, "DB", StringComparison.OrdinalIgnoreCase) || x.BlockName.StartsWith("DB", StringComparison.OrdinalIgnoreCase));
        state.Counts["hardware_exports"] = state.HardwareModules.Count;
        state.Counts["network_links"] = state.NetworkLinks.Count;
        state.Counts["subnets"] = state.Subnets.Count;
        state.Counts["library_items"] = state.Libraries.Count;
        state.Counts["evidence_files"] = state.EvidenceFiles.Count;
    }

    private static void WriteLimitations(string exportRoot)
    {
        var path = Path.Combine(exportRoot, "README_EXPORT_LIMITATIONS.md");
        File.WriteAllText(path,
            """
            # Export Limitations

            This export is generated through Siemens TIA Portal Openness. The exact reachable object model depends on the installed TIA version, project configuration, licenses and Openness permissions.

            Known limitations:

            - Hardware is always normalized from API metadata; full HWCN/AML export is not guaranteed.
            - HMI devices are detected, but screen and detailed WinCC tag export can require project-specific API handling.
            - Project library dependency chains may be incomplete because TIA Openness does not always expose all usage relationships.
            - Objects that do not provide Export(FileInfo, ExportOptions) are recorded as metadata only.
            - Individual object export failures are logged and do not stop the full export.
            """,
            System.Text.Encoding.UTF8);
    }
}
