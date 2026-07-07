using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using TIAExporter.Tia;

namespace TIAExporter.Normalization;

internal sealed class JsonWriterService
{
    public void WriteAll(ExportState state)
    {
        var normalized = Path.Combine(state.ExportRoot, "normalized");
        Directory.CreateDirectory(normalized);

        Write(Path.Combine(state.ExportRoot, "manifest.json"), BuildManifest(state));
        Write(Path.Combine(normalized, "project.json"), new
        {
            state.ProjectName,
            state.ProjectPath,
            state.ExportTimestamp,
            state.ExporterVersion,
            state.TiaPortalVersion
        });
        Write(Path.Combine(normalized, "devices.json"), state.Devices);
        Write(Path.Combine(normalized, "controllers.json"), state.Controllers);
        Write(Path.Combine(normalized, "hardware_modules.json"), state.HardwareModules);
        Write(Path.Combine(normalized, "network_interfaces.json"), state.NetworkInterfaces);
        Write(Path.Combine(normalized, "network_links.json"), state.NetworkLinks);
        Write(Path.Combine(normalized, "subnets.json"), state.Subnets);
        Write(Path.Combine(normalized, "hardware_attributes.json"), state.HardwareAttributes);
        Write(Path.Combine(normalized, "software_blocks.json"), state.SoftwareBlocks);
        Write(Path.Combine(normalized, "tag_tables.json"), state.TagTables);
        Write(Path.Combine(normalized, "libraries.json"), state.Libraries);
        Write(Path.Combine(normalized, "asset_inventory.json"), state.AssetInventory);
        Write(Path.Combine(normalized, "firmware_inventory.json"), state.FirmwareInventory);
        Write(Path.Combine(normalized, "software_inventory.json"), state.SoftwareInventory);
        Write(Path.Combine(normalized, "library_inventory.json"), state.LibraryInventory);
        Write(Path.Combine(normalized, "library_dependency_gaps.json"), state.LibraryDependencyGaps);
        Write(Path.Combine(normalized, "safety_inventory.json"), state.SafetyInventory);
        Write(Path.Combine(normalized, "security_configuration.json"), state.SecurityConfiguration);
        Write(Path.Combine(normalized, "security_findings.json"), state.SecurityFindings);
        Write(Path.Combine(normalized, "hmi_inventory.json"), state.HmiInventory);
        Write(Path.Combine(normalized, "drive_inventory.json"), state.DriveInventory);
        Write(Path.Combine(normalized, "cra_gap_analysis.json"), state.CraGapAnalysis);
        Write(Path.Combine(normalized, "export_quality_score.json"), state.ExportQualityScore);
        Write(Path.Combine(normalized, "export_report.json"), BuildReport(state));
        Write(Path.Combine(state.ExportRoot, "diagnostics", "export_errors.json"), state.ExportErrors);
        Write(Path.Combine(state.ExportRoot, "diagnostics", "openness_environment.json"), state.OpennessEnvironment);
        Write(Path.Combine(state.ExportRoot, "diagnostics", "block_export_failures.json"), state.BlockExportFailures);
        Write(Path.Combine(state.ExportRoot, "diagnostics", "library_export_failures.json"), state.LibraryExportFailures);
        Write(Path.Combine(state.ExportRoot, "diagnostics", "capability_matrix.json"), state.CapabilityMatrix);
        // Write CMDB before evidence refresh so the validation JSON is picked up by the scan below.
        try
        {
            new CmdbCraImportGenerator().WriteAll(state, (p, v) => Write(p, v));
        }
        catch (Exception ex)
        {
            state.Warnings.Add($"CMDB/CRA consolidated import generation failed: {ex.GetType().Name}: {ex.Message}");
        }
        RefreshEvidenceIndex(state);
        // Sync the evidence gap entry so ReadinessReport shows the post-scan count.
        var evGap = state.CraGapAnalysis.FirstOrDefault(x => x.Category == "Evidence / hashes");
        if (evGap != null) evGap.Evidence = $"{state.EvidenceFiles.Count} evidence files hashed";
        WriteReadinessReport(Path.Combine(state.ExportRoot, "reports", "CRA_EXPORT_READINESS.md"), state);
        Write(Path.Combine(normalized, "evidence_files.json"), state.EvidenceFiles);
        Write(Path.Combine(normalized, "export_report.json"), BuildReport(state));
        Write(Path.Combine(state.ExportRoot, "manifest.json"), BuildManifest(state));
    }

    public void Write<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, SimpleJson.Serialize(value), new UTF8Encoding(false));
    }

    private static Manifest BuildManifest(ExportState state) => new()
    {
        ProjectName = state.ProjectName,
        ProjectPath = state.ProjectPath,
        ExportTimestamp = state.ExportTimestamp,
        ExporterVersion = state.ExporterVersion,
        TiaPortalVersion = state.TiaPortalVersion,
        ProjectFolderName = Path.GetFileName(Path.GetDirectoryName(state.ProjectPath) ?? ""),
        ExportSuccess = state.ExportSuccess,
        ExportStatus = state.ExportSuccess ? (state.Errors.Count == 0 ? "successful" : "partially_successful") : "failed",
        TotalErrors = state.Errors.Count,
        TotalWarnings = state.Warnings.Count,
        CapabilitySummary = state.CapabilityMatrix.ToDictionary(x => x.Capability, x => x.Available, StringComparer.OrdinalIgnoreCase),
        ExportCounts = state.Counts,
        CraReadinessSummary = $"Score {state.ExportQualityScore.Score}/100; HIGH gaps: {state.CraGapAnalysis.Count(x => x.Severity == "HIGH")}",
        OutputFiles = Directory.Exists(state.ExportRoot)
            ? Directory.EnumerateFiles(state.ExportRoot, "*", SearchOption.AllDirectories)
                .Where(x => !IsInternalWorkFile(state.ExportRoot, x))
                .Select(x => GetRelativePath(state.ExportRoot, x).Replace('\\', '/'))
                .OrderBy(x => x)
                .ToList()
            : [],
        Limitations = state.Limitations,
        RequiredManualActions = state.RequiredManualActions,
        Errors = state.Errors,
        Warnings = state.Warnings,
        Counts = state.Counts
    };

    private static ExportReport BuildReport(ExportState state)
    {
        var missing = state.SoftwareBlocks.Count(x =>
            x.ExportSuccess &&
            x.CraMetadata.Module == null &&
            x.CraMetadata.Version == null &&
            x.CraMetadata.CraRelevant == null &&
            x.CraMetadata.SafetyRelevant == null);

        var affectedCategories = new List<string>();
        if (state.LicenseBlockingFailureDetected)
        {
            affectedCategories.Add("PLC_BLOCK_XML_EXPORT");
            affectedCategories.Add("SBOM_READINESS");
            if (state.SoftwareBlocks.Any(x => x.IsSafetyRelated && !x.ExportSuccess)) affectedCategories.Add("SAFETY_BLOCK_EXPORT");
        }

        var manualActions = new List<string>(state.RequiredManualActions);
        if (state.LicenseBlockingFailureDetected)
        {
            var licenseAction = $"Verify that Automation License Manager makes TIA Portal '{state.MissingLicenseName ?? "STEP 7 Professional"}' available to the exporter user/session, then re-run the exporter.";
            if (!manualActions.Contains(licenseAction)) manualActions.Insert(0, licenseAction);
        }

        string? licenseSummary = null;
        if (state.LicenseBlockingFailureDetected)
        {
            licenseSummary =
                $"Siemens Openness reported license '{state.MissingLicenseName}' as not usable during PLC block export. " +
                $"{state.LicenseBlockingAffectedBlockCount} block export attempt(s) were affected. " +
                "Software inventory is metadata-only for affected blocks. " +
                "Note: TIA Portal software installation, TIA GUI license display, and ALM availability to this exporter process are separate checks.";
        }

        return new ExportReport
        {
            Status = state.ExportSuccess ? (state.Errors.Count == 0 ? "successful" : "partially_successful") : "failed",
            ErrorCount = state.Errors.Count,
            WarningCount = state.Warnings.Count,
            MissingCraMetadata = missing,
            Counts = state.Counts,
            LicenseBlockingFailureDetected = state.LicenseBlockingFailureDetected,
            MissingLicenseName = state.MissingLicenseName,
            LicenseDiagnosticSummary = licenseSummary,
            AffectedExportCategories = affectedCategories,
            ManualActions = manualActions,
            Limitations =
            [
                "CRA readiness is an engineering export quality assessment and not a legal compliance statement.",
                "Hardware, HMI and library details depend on the installed TIA Openness API and project object model.",
                "Complete dependency chains for project libraries are not guaranteed by TIA Openness.",
                "Some objects may only be represented as normalized metadata if XML export is unsupported."
            ]
        };
    }

    private static void WriteReadinessReport(string path, ExportState state)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# CRA Export Readiness");
        builder.AppendLine();
        builder.AppendLine($"Project: {state.ProjectName}");
        builder.AppendLine($"Export timestamp: {state.ExportTimestamp:O}");
        builder.AppendLine($"Quality score: {state.ExportQualityScore.Score}/100");
        builder.AppendLine();
        builder.AppendLine("This report assesses engineering export completeness for CRA/CMDB review. It does not certify legal CRA conformity.");
        builder.AppendLine();
        builder.AppendLine("## Category Status");
        builder.AppendLine();
        builder.AppendLine("| Category | Status | Severity | Evidence | Required action |");
        builder.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (var gap in state.CraGapAnalysis)
        {
            builder.AppendLine($"| {Escape(gap.Category)} | {Escape(gap.Status)} | {Escape(gap.Severity)} | {Escape(gap.Evidence)} | {Escape(gap.RequiredAction)} |");
        }
        builder.AppendLine();
        builder.AppendLine("## HIGH Gaps");
        builder.AppendLine();
        var high = state.CraGapAnalysis.Where(x => x.Severity == "HIGH").ToList();
        if (high.Count == 0)
        {
            builder.AppendLine("No HIGH gaps detected from exported data.");
        }
        else
        {
            foreach (var gap in high)
            {
                builder.AppendLine($"- {gap.Category}: {gap.Impact} Required action: {gap.RequiredAction}");
            }
        }
        builder.AppendLine();
        builder.AppendLine("## Limitations");
        builder.AppendLine();
        foreach (var limitation in state.Limitations)
        {
            builder.AppendLine($"- {limitation}");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
    }

    private static void RefreshEvidenceIndex(ExportState state)
    {
        state.EvidenceFiles.Clear();
        if (!Directory.Exists(state.ExportRoot))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(state.ExportRoot, "*", SearchOption.AllDirectories)
                     .Where(x => !x.EndsWith(Path.Combine("logs", "export.log"), StringComparison.OrdinalIgnoreCase))
                     .Where(x => !IsInternalWorkFile(state.ExportRoot, x))
                     .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            var relative = GetRelativePath(state.ExportRoot, file).Replace('\\', '/');
            using var stream = File.OpenRead(file);
            using var sha = SHA256.Create();
            var info = new FileInfo(file);
            state.EvidenceFiles.Add(new EvidenceFile
            {
                RelativePath = relative,
                FileType = info.Extension.TrimStart('.').ToLowerInvariant(),
                Sha256 = string.Concat(sha.ComputeHash(stream).Select(x => x.ToString("x2"))),
                SizeBytes = info.Length,
                CreatedAt = info.CreationTimeUtc,
                SourceObject = relative,
                SourceCategory = SourceCategory(relative),
                ExportSuccess = true,
                EvidenceLevel = EvidenceLevel(relative),
                UsedForCraCategories = CraCategories(relative)
            });
        }

        state.Counts["evidence_files"] = state.EvidenceFiles.Count;
    }

    private static string SourceCategory(string relative)
    {
        if (relative.StartsWith("diagnostics/", StringComparison.OrdinalIgnoreCase)) return "diagnostic";
        if (relative.StartsWith("normalized/", StringComparison.OrdinalIgnoreCase)) return "normalized";
        if (relative.StartsWith("reports/", StringComparison.OrdinalIgnoreCase)) return "report";
        if (relative.StartsWith("software/", StringComparison.OrdinalIgnoreCase)) return "software";
        if (relative.StartsWith("libraries/", StringComparison.OrdinalIgnoreCase)) return "library";
        if (relative.StartsWith("hardware/", StringComparison.OrdinalIgnoreCase)) return "hardware";
        return "export";
    }

    private static string EvidenceLevel(string relative)
    {
        if (relative.StartsWith("diagnostics/", StringComparison.OrdinalIgnoreCase)) return "diagnostic";
        if (relative.StartsWith("normalized/", StringComparison.OrdinalIgnoreCase)) return "normalized_json";
        if (relative.StartsWith("reports/", StringComparison.OrdinalIgnoreCase) || relative.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) return "report";
        return "raw_export";
    }

    private static List<string> CraCategories(string relative)
    {
        var result = new List<string> { "Evidence / hashes" };
        if (Has(relative, "software")) result.Add("Software block inventory");
        if (Has(relative, "hardware") || Has(relative, "asset")) result.Add("Asset inventory");
        if (Has(relative, "firmware")) result.Add("Firmware inventory");
        if (Has(relative, "security")) result.Add("Security configuration");
        if (Has(relative, "safety")) result.Add("Safety inventory");
        if (Has(relative, "library")) result.Add("Library inventory");
        if (Has(relative, "hmi")) result.Add("HMI inventory");
        if (Has(relative, "drive")) result.Add("Drive inventory");
        return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool Has(string value, string needle) => value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool IsInternalWorkFile(string exportRoot, string path)
    {
        var relative = GetRelativePath(exportRoot, path).Replace('\\', '/');
        return relative.StartsWith(TiaProjectFileTypes.ArchiveRetrieveFolderName + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetRelativePath(string root, string path)
    {
        var rootUri = new Uri(AppendDirectorySeparator(Path.GetFullPath(root)));
        var pathUri = new Uri(Path.GetFullPath(path));
        return Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
    }

    private static string AppendDirectorySeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            ? path
            : path + Path.DirectorySeparatorChar;
    }

    private static string Escape(string value) => value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");

    private static class SimpleJson
    {
        public static string Serialize(object? value)
        {
            var builder = new StringBuilder();
            WriteValue(builder, value, 0);
            builder.AppendLine();
            return builder.ToString();
        }

        private static void WriteValue(StringBuilder builder, object? value, int indent)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            switch (value)
            {
                case string text:
                    WriteString(builder, text);
                    return;
                case bool boolean:
                    builder.Append(boolean ? "true" : "false");
                    return;
                case DateTimeOffset dto:
                    WriteString(builder, dto.ToString("O", CultureInfo.InvariantCulture));
                    return;
                case DateTime dt:
                    WriteString(builder, dt.ToString("O", CultureInfo.InvariantCulture));
                    return;
                case Enum:
                    WriteString(builder, value.ToString() ?? "");
                    return;
            }

            var type = value.GetType();
            if (IsNumber(type))
            {
                builder.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                return;
            }

            if (value is IDictionary dictionary)
            {
                WriteDictionary(builder, dictionary, indent);
                return;
            }

            if (value is IEnumerable enumerable)
            {
                WriteArray(builder, enumerable, indent);
                return;
            }

            WriteObject(builder, value, indent);
        }

        private static void WriteDictionary(StringBuilder builder, IDictionary dictionary, int indent)
        {
            builder.Append('{');
            var first = true;
            foreach (DictionaryEntry entry in dictionary)
            {
                if (!first)
                {
                    builder.Append(',');
                }

                NewLine(builder, indent + 1);
                WriteString(builder, Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? "");
                builder.Append(": ");
                WriteValue(builder, entry.Value, indent + 1);
                first = false;
            }

            if (!first)
            {
                NewLine(builder, indent);
            }

            builder.Append('}');
        }

        private static void WriteArray(StringBuilder builder, IEnumerable enumerable, int indent)
        {
            builder.Append('[');
            var first = true;
            foreach (var item in enumerable)
            {
                if (!first)
                {
                    builder.Append(',');
                }

                NewLine(builder, indent + 1);
                WriteValue(builder, item, indent + 1);
                first = false;
            }

            if (!first)
            {
                NewLine(builder, indent);
            }

            builder.Append(']');
        }

        private static void WriteObject(StringBuilder builder, object value, int indent)
        {
            builder.Append('{');
            var properties = value.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.GetIndexParameters().Length == 0)
                .ToArray();
            var first = true;
            foreach (var property in properties)
            {
                var propertyValue = property.GetValue(value);
                if (propertyValue == null)
                {
                    continue;
                }

                if (!first)
                {
                    builder.Append(',');
                }

                NewLine(builder, indent + 1);
                WriteString(builder, ToSnakeCase(property.Name));
                builder.Append(": ");
                WriteValue(builder, propertyValue, indent + 1);
                first = false;
            }

            if (!first)
            {
                NewLine(builder, indent);
            }

            builder.Append('}');
        }

        private static void WriteString(StringBuilder builder, string value)
        {
            builder.Append('"');
            foreach (var ch in value)
            {
                builder.Append(ch switch
                {
                    '"' => "\\\"",
                    '\\' => "\\\\",
                    '\b' => "\\b",
                    '\f' => "\\f",
                    '\n' => "\\n",
                    '\r' => "\\r",
                    '\t' => "\\t",
                    _ when char.IsControl(ch) => "\\u" + ((int)ch).ToString("x4"),
                    _ => ch.ToString()
                });
            }
            builder.Append('"');
        }

        private static void NewLine(StringBuilder builder, int indent)
        {
            builder.AppendLine();
            builder.Append(' ', indent * 2);
        }

        private static bool IsNumber(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal);
        }

        private static string ToSnakeCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var builder = new StringBuilder();
            for (var i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                if (char.IsUpper(ch) && i > 0)
                {
                    builder.Append('_');
                }
                builder.Append(char.ToLowerInvariant(ch));
            }

            return builder.ToString();
        }
    }
}
