using TIAExporter.Logging;
using TIAExporter.Normalization;

namespace TIAExporter.Tia;

internal sealed class LibraryExporter
{
    private readonly ExportLogger logger;
    private readonly EvidenceHasher hasher;

    public LibraryExporter(ExportLogger logger, EvidenceHasher hasher)
    {
        this.logger = logger;
        this.hasher = hasher;
    }

    public void Export(object project, ExportState state)
    {
        if (!state.Settings.IncludeLibraries)
        {
            state.Warnings.Add("Library export disabled by CLI/GUI option.");
            return;
        }

        var library = TiaReflection.GetValue(project, "ProjectLibrary");
        if (library == null)
        {
            state.Warnings.Add("No ProjectLibrary object exposed by Openness.");
            return;
        }

        logger.Info("Scanning project library.");
        ScanLibraryGroup(TiaReflection.GetValue(library, "TypeFolder", "Types"), state, "Type", "");
        ScanLibraryGroup(TiaReflection.GetValue(library, "MasterCopyFolder", "MasterCopies"), state, "MasterCopy", "");
    }

    private void ScanLibraryGroup(object? group, ExportState state, string kind, string path)
    {
        if (group == null)
        {
            return;
        }

        foreach (var item in TiaReflection.Enumerate(TiaReflection.GetValue(group, "Types", "MasterCopies", "Items")))
        {
            ExportLibraryItem(item, state, kind, path);
        }

        foreach (var child in TiaReflection.Enumerate(TiaReflection.GetValue(group, "Folders", "Groups")))
        {
            var childName = TiaReflection.GetString(child, "Name") ?? "Folder";
            var childPath = string.IsNullOrWhiteSpace(path) ? childName : $"{path}/{childName}";
            ScanLibraryGroup(child, state, kind, childPath);
        }
    }

    private void ExportLibraryItem(object item, ExportState state, string kind, string path)
    {
        var name = TiaReflection.GetString(item, "Name") ?? "library_item";
        var version = TiaReflection.GetString(item, "Version", "VersionNumber");
        var folder = string.IsNullOrWhiteSpace(path)
            ? Path.Combine(state.ExportRoot, "libraries")
            : Path.Combine(state.ExportRoot, "libraries", FileNameSanitizer.Sanitize(path).Replace('/', Path.DirectorySeparatorChar));
        var filePath = FileNameSanitizer.UniquePath(folder, $"{FileNameSanitizer.Sanitize(name)}.xml");
        var normalized = new NormalizedLibraryItem
        {
            Name = name,
            Version = version,
            Kind = kind,
            Path = path,
            Author = TiaReflection.GetString(item, "Author"),
            Comment = TiaReflection.GetString(item, "Comment"),
            LastModified = TiaReflection.GetString(item, "ModifiedDate", "LastModified", "LastChange"),
            ExportAttempted = true
        };

        try
        {
            if (TiaReflection.TryExportWithOptions(item, filePath, out var xmlError, out _))
            {
                var evidence = hasher.HashFile(state.ExportRoot, filePath, $"{kind}/{path}/{name}", true, "library", "raw_export",
                    ["Library inventory", "Evidence / hashes", "SBOM readiness"]);
                state.EvidenceFiles.Add(evidence);
                normalized.ExportFile = evidence.RelativePath;
                normalized.ExportSuccess = true;
                normalized.ExportStatus = "full_xml";
            }
            else
            {
                Exception? documentError = null;
                if (state.Settings.IncludeDocuments && TiaReflection.TryExportAsDocument(item, Path.ChangeExtension(filePath, ".document"), out documentError, out _))
                {
                    var actualFile = Directory.EnumerateFiles(Path.GetDirectoryName(filePath)!, "*", SearchOption.TopDirectoryOnly)
                        .OrderByDescending(File.GetLastWriteTimeUtc)
                        .FirstOrDefault() ?? Path.ChangeExtension(filePath, ".document");
                    var evidence = hasher.HashFile(state.ExportRoot, actualFile, $"{kind}/{path}/{name}", true, "library", "raw_export",
                        ["Library inventory", "Evidence / hashes"]);
                    state.EvidenceFiles.Add(evidence);
                    normalized.DocumentFile = evidence.RelativePath;
                    normalized.ExportSuccess = true;
                    normalized.FallbackExportUsed = true;
                    normalized.ExportStatus = "document_only";
                }
                else
                {
                    throw xmlError ?? documentError ?? new NotSupportedException($"Library item type {item.GetType().FullName} has no supported export method.");
                }
            }
        }
        catch (Exception ex)
        {
            var message = $"Failed to export library item {kind}/{path}/{name}";
            logger.Error(message, ex);
            state.Warnings.Add($"{message}: {ex.GetType().Name}: {ex.Message}");
            normalized.ExportSuccess = false;
            normalized.ExportStatus = "metadata_only";
            normalized.ExportErrorType = ex.GetType().Name;
            normalized.ExportErrorMessage = ex.Message;
            normalized.SuspectedReason = "Library item does not expose a supported export method or requires a TIA module/license not available in this Openness environment.";
            normalized.RequiredAction = "Verify project/global library access in TIA and rerun on an engineering station with required library support.";
            var failure = new LibraryExportFailure
            {
                Category = "LIBRARY_EXPORT",
                Context = $"{kind}/{path}/{name}",
                LibraryItemName = name,
                Version = version,
                Kind = kind,
                Path = path,
                ErrorType = ex.GetType().Name,
                ErrorMessage = ex.Message,
                StackShort = ShortStack(ex),
                SuspectedReason = normalized.SuspectedReason,
                RequiredAction = normalized.RequiredAction
            };
            state.LibraryExportFailures.Add(failure);
            state.ExportErrors.Add(failure);
        }

        state.Libraries.Add(normalized);
    }

    private static string? ShortStack(Exception ex)
    {
        if (string.IsNullOrWhiteSpace(ex.StackTrace))
        {
            return null;
        }

        return string.Join(Environment.NewLine, ex.StackTrace.Split([Environment.NewLine], StringSplitOptions.None).Take(5));
    }
}
