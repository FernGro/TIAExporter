using TIAExporter.Logging;
using TIAExporter.Normalization;

namespace TIAExporter.Tia;

internal sealed class PlcSoftwareExporter
{
    private readonly ExportLogger logger;
    private readonly EvidenceHasher hasher;

    public PlcSoftwareExporter(ExportLogger logger, EvidenceHasher hasher)
    {
        this.logger = logger;
        this.hasher = hasher;
    }

    public void Export(PlcCandidate plc, ExportState state)
    {
        logger.Info($"Exporting PLC software: {plc.PlcName}");
        var plcRoot = Path.Combine(state.ExportRoot, "software", FileNameSanitizer.Sanitize(plc.PlcName));
        Directory.CreateDirectory(plcRoot);

        ExportBlockGroup(TiaReflection.GetValue(plc.PlcSoftware, "BlockGroup"), state, plc.PlcName, plcRoot, "", "blocks");
        ExportBlockGroup(TiaReflection.GetValue(plc.PlcSoftware, "TypeGroup"), state, plc.PlcName, plcRoot, "", "udts");

        var unitGroup = TiaReflection.GetValue(plc.PlcSoftware, "SoftwareUnitGroup", "SoftwareUnits");
        if (unitGroup != null)
        {
            logger.Info($"Software units detected for PLC {plc.PlcName}; exporting reachable contents.");
            ExportBlockGroup(unitGroup, state, plc.PlcName, plcRoot, "SoftwareUnits", "blocks");
        }
    }

    private void ExportBlockGroup(object? group, ExportState state, string plcName, string plcRoot, string groupPath, string folderName)
    {
        if (group == null)
        {
            return;
        }

        var blocks = TiaReflection.GetValue(group, "Blocks", "Types");
        foreach (var block in TiaReflection.Enumerate(blocks))
        {
            ExportBlock(block, state, plcName, plcRoot, groupPath, folderName);
        }

        var groups = TiaReflection.GetValue(group, "Groups");
        foreach (var child in TiaReflection.Enumerate(groups))
        {
            var childName = TiaReflection.GetString(child, "Name") ?? "Group";
            var childPath = string.IsNullOrWhiteSpace(groupPath) ? childName : $"{groupPath}/{childName}";
            ExportBlockGroup(child, state, plcName, plcRoot, childPath, folderName);
        }
    }

    private void ExportBlock(object block, ExportState state, string plcName, string plcRoot, string groupPath, string folderName)
    {
        var name = TiaReflection.GetString(block, "Name") ?? "unnamed_block";
        var type = DetermineBlockType(block, name, folderName);
        var targetFolder = string.IsNullOrWhiteSpace(groupPath)
            ? Path.Combine(plcRoot, folderName)
            : Path.Combine(plcRoot, folderName, FileNameSanitizer.Sanitize(groupPath).Replace('/', Path.DirectorySeparatorChar));
        var filePath = FileNameSanitizer.UniquePath(targetFolder, $"{FileNameSanitizer.Sanitize(name)}.xml");
        var isSafety = IsSafetyRelated(block, name, groupPath, type);
        var normalized = new NormalizedSoftwareBlock
        {
            PlcName = plcName,
            BlockName = name,
            BlockType = type,
            BlockNumber = TiaReflection.GetString(block, "Number"),
            Language = TiaReflection.GetString(block, "ProgrammingLanguage", "Language"),
            GroupPath = groupPath,
            ExportAttempted = true,
            IsConsistent = TiaReflection.GetBool(block, "IsConsistent", "Consistent"),
            IsCompiled = TiaReflection.GetBool(block, "IsCompiled", "Compiled"),
            IsProtected = TiaReflection.GetBool(block, "IsKnowHowProtected", "IsProtected", "KnowHowProtected"),
            IsSafetyRelated = isSafety,
            IsSystemBlock = IsSystemBlock(block, name, groupPath),
            Attributes = TiaReflection.GetReadableAttributes(block)
        };

        try
        {
            if (!TiaReflection.TryExportWithOptions(block, filePath, out var xmlError, out var optionUsed))
            {
                normalized.CanExportXml = xmlError == null ? false : null;
                // Skip document fallback for this block when XML failed due to a license exception.
                if (state.Settings.IncludeDocuments && !IsLicenseMissingException(xmlError))
                {
                    TryDocumentFallback(block, state, plcName, plcRoot, groupPath, name, normalized, xmlError);
                }

                if (!normalized.ExportSuccess)
                {
                    throw xmlError ?? new NotSupportedException($"Object type {block.GetType().FullName} has no supported Export(FileInfo, ExportOptions) method.");
                }
            }
            else
            {
                normalized.CanExportXml = true;
                normalized.FileType = "xml";
                normalized.EvidenceStatus = "full_xml";
                logger.Info($"Block XML export succeeded: {plcName}/{groupPath}/{name} using {optionUsed}");
                var evidence = hasher.HashFile(state.ExportRoot, filePath, $"{plcName}/{groupPath}/{name}", true, "software_block", "raw_export",
                    ["Software block inventory", "Evidence / hashes", "SBOM readiness"]);
                state.EvidenceFiles.Add(evidence);
                normalized.ExportFile = evidence.RelativePath;
                normalized.HashSha256 = evidence.Sha256;
                normalized.ExportSuccess = true;
                normalized.CraMetadata = CraMetadataParser.ParseFile(filePath);
            }
        }
        catch (Exception ex)
        {
            if (IsLicenseMissingException(ex))
            {
                HandleLicenseException(ex, normalized, state, plcName, groupPath, name, type);
            }
            else
            {
                var message = $"Failed to export block {plcName}/{groupPath}/{name}";
                logger.Error(message, ex);
                var reason = SuspectReason(ex, normalized);
                var action = RequiredAction(normalized);
                state.Errors.Add($"{message}: {ex.GetType().Name}: {ex.Message}");
                normalized.ExportSuccess = false;
                normalized.ExportErrorType = ex.GetType().Name;
                normalized.ExportErrorMessage = ex.Message;
                normalized.ExportErrorStackShort = ShortStack(ex);
                normalized.SuspectedReason = reason;
                normalized.RequiredAction = action;
                normalized.EvidenceStatus = "metadata_only";
                var failure = new BlockExportFailure
                {
                    Category = "PLC_BLOCK_EXPORT",
                    Context = $"{plcName}/{groupPath}/{name}",
                    PlcName = plcName,
                    BlockName = name,
                    BlockType = type,
                    GroupPath = groupPath,
                    ErrorType = ex.GetType().Name,
                    ErrorMessage = ex.Message,
                    StackShort = ShortStack(ex),
                    SuspectedReason = reason,
                    RequiredAction = action,
                    IsProtected = normalized.IsProtected,
                    IsSafetyRelated = normalized.IsSafetyRelated
                };
                state.BlockExportFailures.Add(failure);
                state.ExportErrors.Add(failure);
            }
        }

        state.SoftwareBlocks.Add(normalized);
    }

    private void HandleLicenseException(Exception ex, NormalizedSoftwareBlock normalized, ExportState state, string plcName, string groupPath, string name, string? type)
    {
        var missingLicense = ExtractMissingLicenseName(ex);
        if (!state.LicenseBlockingFailureDetected)
        {
            state.LicenseBlockingFailureDetected = true;
            state.MissingLicenseName = missingLicense;
            state.FirstLicenseErrorMessage = TruncateMessage(ex.Message, 300);
            logger.Error($"Siemens Openness reports license '{missingLicense}' is not usable during PLC block export. Block: {plcName}/{groupPath}/{name}", ex);
            logger.Info("Note: TIA may show a license as installed while Openness cannot use it in this exporter process/session.");
            logger.Info("The exporter will continue with later blocks because license failures can be object-type or feature specific.");
            state.Errors.Add($"License-related block export failure: Siemens Openness reports license '{missingLicense}' is not usable by this exporter process. Verify ALM availability for the same Windows user/session.");
            state.BlockExportFailures.Add(new BlockExportFailure
            {
                Category = "PLC_BLOCK_EXPORT_LICENSE",
                Context = $"{plcName}/{groupPath}/{name}",
                PlcName = plcName,
                BlockName = normalized.BlockName,
                BlockType = type,
                GroupPath = groupPath,
                ErrorType = ex.GetType().Name,
                ErrorMessage = TruncateMessage(ex.Message, 300),
                StackShort = ShortStack(ex),
                SuspectedReason = LicenseSuspectReason(missingLicense),
                RequiredAction = LicenseRequiredAction(missingLicense),
                IsProtected = normalized.IsProtected,
                IsSafetyRelated = normalized.IsSafetyRelated
            });
            state.ExportErrors.Add(state.BlockExportFailures[state.BlockExportFailures.Count - 1]);
        }
        else
        {
            logger.Warn($"License-related block export failure ('{missingLicense}'): {plcName}/{groupPath}/{name}");
        }
        ApplyLicenseSkippedState(normalized, missingLicense, TruncateMessage(ex.Message, 300));
        state.LicenseBlockingAffectedBlockCount++;
    }

    private void TryDocumentFallback(object block, ExportState state, string plcName, string plcRoot, string groupPath, string name, NormalizedSoftwareBlock normalized, Exception? xmlError)
    {
        var documentFolder = string.IsNullOrWhiteSpace(groupPath)
            ? Path.Combine(plcRoot, "documents")
            : Path.Combine(plcRoot, "documents", FileNameSanitizer.Sanitize(groupPath).Replace('/', Path.DirectorySeparatorChar));
        var documentPath = FileNameSanitizer.UniquePath(documentFolder, $"{FileNameSanitizer.Sanitize(name)}.document");
        if (!TiaReflection.TryExportAsDocument(block, documentPath, out var documentError, out _))
        {
            normalized.CanExportDocument = documentError == null ? false : null;
            if (xmlError != null)
            {
                throw xmlError;
            }

            if (documentError != null)
            {
                throw documentError;
            }

            return;
        }

        var actualFile = ResolveDocumentFile(documentFolder, documentPath);
        var evidence = hasher.HashFile(state.ExportRoot, actualFile, $"{plcName}/{groupPath}/{name}", true, "software_block", "raw_export",
            ["Software block inventory", "Evidence / hashes"]);
        state.EvidenceFiles.Add(evidence);
        normalized.CanExportDocument = true;
        normalized.FallbackExportUsed = true;
        normalized.ExportSuccess = true;
        normalized.EvidenceStatus = "document_only";
        normalized.DocumentFile = evidence.RelativePath;
        normalized.FileType = Path.GetExtension(actualFile).TrimStart('.').ToLowerInvariant();
        normalized.HashSha256 = evidence.Sha256;
    }

    private static string ResolveDocumentFile(string documentFolder, string requestedPath)
    {
        if (File.Exists(requestedPath))
        {
            return requestedPath;
        }

        var newest = Directory.Exists(documentFolder)
            ? Directory.EnumerateFiles(documentFolder, "*", SearchOption.TopDirectoryOnly)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault()
            : null;
        return newest ?? requestedPath;
    }

    private static string DetermineBlockType(object block, string name, string folderName)
    {
        var raw = TiaReflection.GetString(block, "BlockType", "Type", "ProgrammingLanguage") ?? block.GetType().Name;
        var text = $"{raw} {block.GetType().FullName} {name}";
        if (folderName.Equals("udts", StringComparison.OrdinalIgnoreCase) || text.IndexOf("Struct", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "UDT";
        }

        foreach (var known in new[] { "OB", "FB", "FC", "DB" })
        {
            if (text.IndexOf(known, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return IsSafetyName(name) && known is "FB" or "FC" or "DB" ? $"F-{known}" : known;
            }
        }

        if (text.IndexOf("Technology", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Technology object";
        }

        return raw;
    }

    private static bool IsSafetyRelated(object block, string name, string groupPath, string? blockType)
    {
        if (TiaReflection.GetBool(block, "IsFailsafe", "Failsafe", "IsSafety") == true)
        {
            return true;
        }

        return IsSafetyName($"{name} {groupPath} {blockType}");
    }

    private static bool IsSafetyName(string text)
    {
        return text.IndexOf("Safety", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("Failsafe", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("F-LAD", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("F_", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("NotHalt", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("Emergency", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("Schutz", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsSystemBlock(object block, string name, string groupPath)
    {
        if (TiaReflection.GetBool(block, "IsSystemBlock", "SystemBlock") == true)
        {
            return true;
        }

        var text = $"{block.GetType().FullName} {name} {groupPath}";
        return text.IndexOf("System", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string SuspectReason(Exception ex, NormalizedSoftwareBlock block)
    {
        if (IsLicenseMissingException(ex))
        {
            return LicenseSuspectReason(ExtractMissingLicenseName(ex));
        }

        var text = ex.ToString();
        if (block.IsProtected == true || text.IndexOf("protect", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("know", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Block may be protected or locked for Openness export.";
        }

        if (block.IsSafetyRelated)
        {
            return "Safety block export may require STEP 7 Safety and unlocked safety project access.";
        }

        if (block.IsConsistent == false || block.IsCompiled == false || text.IndexOf("consistent", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("compile", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Block appears inconsistent, not compiled, or the export method is not supported for this object/language.";
        }

        if (ex is NotSupportedException || text.IndexOf("no supported Export", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Object does not expose a supported XML/document export method in this installed Openness API.";
        }

        return "Block export failed in TIA Openness; inspect error type/message and verify compile state, protection and installed TIA modules.";
    }

    private static string RequiredAction(NormalizedSoftwareBlock block)
    {
        if (block.IsSafetyRelated)
        {
            return "Export requires TIA Safety/STEP7 Safety module or unlocked safety project access; verify manually.";
        }

        if (block.IsProtected == true)
        {
            return "Unlock or document protected block manually; rerun exporter if XML export is required.";
        }

        return "Compile project in TIA and retry; if still failing use ExportAsDocument fallback or review metadata-only evidence.";
    }

    private static string? ShortStack(Exception ex)
    {
        if (string.IsNullOrWhiteSpace(ex.StackTrace))
        {
            return null;
        }

        return string.Join(Environment.NewLine, ex.StackTrace.Split([Environment.NewLine], StringSplitOptions.None).Take(5));
    }

    private static bool IsLicenseMissingException(Exception? ex) => LicenseExceptionHelper.IsLicenseMissingException(ex);

    private static string ExtractMissingLicenseName(Exception ex) => LicenseExceptionHelper.ExtractMissingLicenseName(ex);

    private static void ApplyLicenseSkippedState(NormalizedSoftwareBlock normalized, string? missingLicense, string? errorMessage)
    {
        normalized.ExportSuccess = false;
        normalized.EvidenceStatus = "license_unavailable";
        normalized.MissingLicense = missingLicense;
        normalized.ExportErrorType = "LicenseNotFoundException";
        normalized.ExportErrorMessage = errorMessage;
        normalized.SuspectedReason = LicenseExceptionHelper.LicenseSuspectReason(missingLicense);
        normalized.RequiredAction = LicenseExceptionHelper.LicenseRequiredAction(missingLicense);
    }

    private static string LicenseSuspectReason(string? licenseName) => LicenseExceptionHelper.LicenseSuspectReason(licenseName);

    private static string LicenseRequiredAction(string? licenseName) => LicenseExceptionHelper.LicenseRequiredAction(licenseName);

    private static string TruncateMessage(string message, int maxLength) => LicenseExceptionHelper.TruncateMessage(message, maxLength);
}
