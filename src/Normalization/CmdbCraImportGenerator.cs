using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace TIAExporter.Normalization;

internal sealed class CmdbCraImportGenerator
{
    public const string SchemaVersion = "1.0.0";

    public CmdbCraImport Build(ExportState state)
    {
        var result = new CmdbCraImport
        {
            SchemaVersion = SchemaVersion,
            ExportMetadata = BuildMetadata(state),
            CmdbScope = BuildScope(state),
            Assets = BuildAssets(state),
            SoftwareComponents = BuildSoftwareComponents(state),
            Network = BuildNetwork(state),
            Security = BuildSecurity(state),
            Safety = BuildSafety(state),
            Libraries = BuildLibraries(state),
            Evidence = BuildEvidence(state),
            CraReadiness = BuildCraReadiness(state)
        };
        return result;
    }

    public void WriteAll(ExportState state, Action<string, object> jsonWriter)
    {
        var normalizedDir = Path.Combine(state.ExportRoot, "normalized");
        var reportsDir = Path.Combine(state.ExportRoot, "reports");
        var diagnosticsDir = Path.Combine(state.ExportRoot, "diagnostics");
        Directory.CreateDirectory(normalizedDir);
        Directory.CreateDirectory(reportsDir);
        Directory.CreateDirectory(diagnosticsDir);

        var import = Build(state);
        jsonWriter(Path.Combine(normalizedDir, "cmdb_cra_import.json"), import);
        File.WriteAllText(Path.Combine(normalizedDir, "cmdb_cra_import.schema.json"), BuildSchemaJson(), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(normalizedDir, "cmdb_cra_import.csv"), BuildAssetCsv(import), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(reportsDir, "CMDB_CRA_IMPORT_README.md"), BuildReadme(), new UTF8Encoding(false));

        var validation = Validate(import);
        jsonWriter(Path.Combine(diagnosticsDir, "cmdb_cra_import_validation.json"), validation);
    }

    public static CmdbValidationResult Validate(CmdbCraImport import)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrEmpty(import.SchemaVersion)) errors.Add("schema_version is required");
        if (import.ExportMetadata == null) errors.Add("export_metadata is required");
        else
        {
            if (string.IsNullOrEmpty(import.ExportMetadata.ExportId)) errors.Add("export_metadata.export_id is required");
            if (string.IsNullOrEmpty(import.ExportMetadata.ProjectName)) errors.Add("export_metadata.project_name is required");
            if (string.IsNullOrEmpty(import.ExportMetadata.ExporterVersion)) errors.Add("export_metadata.exporter_version is required");
        }

        var assetIds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < import.Assets.Count; i++)
        {
            var a = import.Assets[i];
            if (string.IsNullOrEmpty(a.AssetId)) errors.Add($"assets[{i}].asset_id is required");
            else if (!assetIds.Add(a.AssetId)) errors.Add($"assets[{i}].asset_id '{a.AssetId}' is not unique");
            if (string.IsNullOrEmpty(a.AssetName)) errors.Add($"assets[{i}].asset_name is required");
            if (string.IsNullOrEmpty(a.AssetType)) errors.Add($"assets[{i}].asset_type is required");
            if (a.DataQuality == null) errors.Add($"assets[{i}].data_quality is required");
            else if (!IsValidCompleteness(a.DataQuality.Completeness)) errors.Add($"assets[{i}].data_quality.completeness invalid: {a.DataQuality.Completeness}");
        }

        var componentIds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < import.SoftwareComponents.Count; i++)
        {
            var s = import.SoftwareComponents[i];
            if (string.IsNullOrEmpty(s.ComponentId)) errors.Add($"software_components[{i}].component_id is required");
            else if (!componentIds.Add(s.ComponentId)) warnings.Add($"software_components[{i}].component_id '{s.ComponentId}' is not unique (may be acceptable for library duplicates)");
            if (string.IsNullOrEmpty(s.Name)) errors.Add($"software_components[{i}].name is required");
            if (s.DataQuality != null && !IsValidCompleteness(s.DataQuality.Completeness)) errors.Add($"software_components[{i}].data_quality.completeness invalid");
        }

        if (import.CraReadiness == null) errors.Add("cra_readiness is required");
        else
        {
            if (!import.CraReadiness.NotALegalComplianceStatement) errors.Add("cra_readiness.not_a_legal_compliance_statement must be true");
            if (import.CraReadiness.OverallStatus is not ("complete" or "partial" or "insufficient")) errors.Add($"cra_readiness.overall_status invalid: {import.CraReadiness.OverallStatus}");
        }

        if (import.Evidence?.CoverageSummary != null && import.Evidence.CoverageSummary.MissingOrUnhashed > 0)
        {
            warnings.Add($"evidence.coverage_summary reports {import.Evidence.CoverageSummary.MissingOrUnhashed} missing or unhashed expected files.");
        }

        return new CmdbValidationResult
        {
            SchemaVersion = SchemaVersion,
            Valid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings,
            CheckedAt = DateTimeOffset.Now
        };
    }

    private static bool IsValidCompleteness(string value) => value is "complete" or "partial" or "missing";

    private static CmdbExportMetadata BuildMetadata(ExportState state) => new()
    {
        ExportId = StableHash($"{state.ProjectName}|{state.ProjectPath}|{state.ExportTimestamp:O}"),
        ExportTimestamp = state.ExportTimestamp,
        ExporterVersion = state.ExporterVersion,
        ProjectName = state.ProjectName,
        ProjectPath = state.ProjectPath,
        ProjectVersion = string.IsNullOrWhiteSpace(state.ProjectPath) ? null : Path.GetExtension(state.ProjectPath).TrimStart('.').ToUpperInvariant(),
        TiaPortalVersion = state.TiaPortalVersion,
        WindowsUser = state.OpennessEnvironment.WindowsUser,
        MachineName = Environment.MachineName,
        ExportStatus = state.ExportSuccess ? (state.Errors.Count == 0 ? "successful" : "partially_successful") : "failed",
        ExportQualityScore = state.ExportQualityScore.Score,
        Limitations = new List<string>(state.Limitations)
    };

    private static CmdbScope BuildScope(ExportState state) => new()
    {
        MachineOrProjectIdentifier = state.ProjectName,
        SafetyRelevance = state.SafetyInventory.SafetyPresent ? "yes" : "unknown",
        NetworkRelevance = state.NetworkInterfaces.Count > 0 ? "yes" : "unknown",
        CraRelevance = state.AssetInventory.Count > 0 ? "yes" : "unknown",
        SupportRelevance = "unknown"
    };

    private static List<CmdbAsset> BuildAssets(ExportState state)
    {
        var evidenceLookup = BuildEvidenceLookup(state);
        var result = new List<CmdbAsset>();

        foreach (var asset in state.AssetInventory)
        {
            var network = state.NetworkInterfaces.Where(x =>
                    x.Path.StartsWith(asset.Path, StringComparison.OrdinalIgnoreCase) ||
                    asset.Path.StartsWith(x.Path, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var ipAddresses = network.Select(x => x.IpAddress).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Cast<string>().ToList();
            var profinet = network.SelectMany(x => x.NodeNames).FirstOrDefault();
            var subnet = network.Select(x => x.SubnetName).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

            var lookupKeys = BuildLookupKeys(asset.Vendor, asset.ProductFamily, asset.DeviceItemName, asset.OrderNumber, asset.NormalizedOrderNumber, asset.FirmwareVersion);

            var missingFields = new List<string>();
            if (string.IsNullOrWhiteSpace(asset.OrderNumber)) missingFields.Add("order_number");
            if (string.IsNullOrWhiteSpace(asset.FirmwareVersion)) missingFields.Add("firmware_version");
            if (lookupKeys.Count == 0) missingFields.Add("vulnerability_lookup_keys");

            var completeness = missingFields.Count == 0 ? "complete" : missingFields.Count >= 2 ? "partial" : "partial";
            var confidence = missingFields.Count == 0 ? "high" : missingFields.Count >= 2 ? "low" : "medium";
            var manualReview = asset.IsHmi || asset.IsDrive || asset.IsSafetyRelated || missingFields.Count > 0;

            var notes = new List<string>();
            if (asset.IsHmi) notes.Add("HMI asset: tags/screens not exported via Openness in current run.");
            if (asset.IsDrive) notes.Add("Drive asset: parameter export depends on Startdrive Openness support.");
            if (asset.IsSafetyRelated) notes.Add("Safety-related asset: requires manual safety review.");

            result.Add(new CmdbAsset
            {
                AssetId = asset.AssetId,
                AssetType = MapAssetType(asset),
                AssetName = asset.DeviceItemName,
                ParentAssetId = ResolveParentAssetId(state.AssetInventory, asset),
                SourceFile = "normalized/hardware_modules.json",
                SourceSystem = "TIA Openness",
                Vendor = asset.Vendor,
                ProductFamily = asset.ProductFamily,
                ProductName = asset.TypeIdentifier ?? asset.DeviceItemName,
                OrderNumber = asset.OrderNumber,
                HardwareIdentifier = asset.TypeIdentifier,
                FirmwareVersion = asset.FirmwareVersion,
                IpAddresses = ipAddresses,
                MacAddresses = asset.MacAddresses,
                SerialNumber = asset.SerialNumber,
                ProfinetName = profinet,
                NetworkInterfaces = asset.NetworkInterfaces,
                Subnet = subnet,
                SafetyRelated = asset.IsSafetyRelated,
                SecurityRelevant = true,
                InternetExposed = false,
                RemoteAccessRelevant = "unknown",
                VulnerabilityLookupKeys = lookupKeys,
                EvidenceRefs = ResolveEvidenceRefs(evidenceLookup, "normalized/hardware_modules.json"),
                DataQuality = new CmdbDataQuality
                {
                    Completeness = completeness,
                    Confidence = confidence,
                    MissingFields = missingFields,
                    ManualReviewRequired = manualReview,
                    Notes = notes
                }
            });
        }

        return result;
    }

    private static List<CmdbSoftwareComponent> BuildSoftwareComponents(ExportState state)
    {
        var evidenceLookup = BuildEvidenceLookup(state);
        var result = new List<CmdbSoftwareComponent>();

        foreach (var sw in state.SoftwareInventory)
        {
            var hasXml = !string.IsNullOrWhiteSpace(sw.ExportFile);
            var hasHash = !string.IsNullOrWhiteSpace(sw.HashSha256);
            var protectedBlock = state.SoftwareBlocks.FirstOrDefault(x => x.BlockName == sw.ComponentName && x.PlcName == sw.PlcName)?.IsProtected == true;

            var missing = new List<string>();
            if (!hasXml) missing.Add("export_file");
            if (!hasHash) missing.Add("hash_sha256");

            var completeness = sw.ExportStatus == "full_xml" ? "complete" : sw.ExportStatus == "failed" ? "missing" : "partial";
            var confidence = sw.ExportStatus == "full_xml" ? "high" : "medium";
            var manualReview = !hasXml || protectedBlock || sw.IsSafetyRelated;

            var notes = new List<string>();
            if (protectedBlock) notes.Add("Block is protected and cannot be exported as XML.");
            if (sw.IsSafetyRelated) notes.Add("Safety-related software component: manual review required.");
            if (!string.IsNullOrWhiteSpace(sw.MissingLicense)) notes.Add($"Affected by missing license: {sw.MissingLicense}.");

            var evidenceRefs = new List<string>();
            if (hasXml)
            {
                evidenceRefs.AddRange(ResolveEvidenceRefs(evidenceLookup, sw.ExportFile!));
            }
            if (!string.IsNullOrWhiteSpace(sw.DocumentFile))
            {
                evidenceRefs.AddRange(ResolveEvidenceRefs(evidenceLookup, sw.DocumentFile!));
            }

            result.Add(new CmdbSoftwareComponent
            {
                ComponentId = sw.ComponentId,
                ComponentType = MapComponentType(sw.ComponentType),
                Name = sw.ComponentName,
                Path = sw.GroupPath,
                ParentAssetId = ResolveControllerAssetId(state, sw.PlcName),
                Version = sw.BlockNumber,
                Language = sw.Language,
                BlockType = sw.TiaBlockType,
                IsProtected = protectedBlock,
                IsSafetyRelated = sw.IsSafetyRelated,
                Exported = hasXml,
                ExportFormat = sw.ExportStatus == "full_xml" ? "xml" : sw.ExportStatus == "failed" ? "failed" : "metadata_only",
                HashSha256 = sw.HashSha256,
                EvidenceRefs = evidenceRefs.Distinct().ToList(),
                DataQuality = new CmdbDataQuality
                {
                    Completeness = completeness,
                    Confidence = confidence,
                    MissingFields = missing,
                    ManualReviewRequired = manualReview,
                    Notes = notes
                }
            });
        }

        foreach (var lib in state.LibraryInventory)
        {
            var exported = lib.ExportStatus == "full_xml" || lib.ExportStatus == "document_only";
            var missing = new List<string>();
            if (!exported) missing.Add("library_export");

            var notes = new List<string>();
            if (!exported) notes.Add($"Library item not exported (status={lib.ExportStatus}). Manual export required.");
            if (!string.IsNullOrWhiteSpace(lib.ExportErrorType)) notes.Add($"Error: {lib.ExportErrorType}: {lib.ExportErrorMessage}");

            result.Add(new CmdbSoftwareComponent
            {
                ComponentId = StableHash($"library|{lib.Kind}|{lib.Name}|{lib.Version}|{lib.Path}"),
                ComponentType = lib.Kind?.IndexOf("Type", StringComparison.OrdinalIgnoreCase) >= 0 ? "library_type" : "library_instance",
                Name = lib.Name,
                Path = lib.Path ?? "",
                Version = lib.Version,
                IsProtected = false,
                IsSafetyRelated = false,
                Exported = exported,
                ExportFormat = exported ? "xml" : "metadata_only",
                HashSha256 = null,
                EvidenceRefs = string.IsNullOrWhiteSpace(lib.ExportFile) ? new List<string>() : new List<string> { lib.ExportFile! },
                DataQuality = new CmdbDataQuality
                {
                    Completeness = exported ? "complete" : "partial",
                    Confidence = exported ? "medium" : "low",
                    MissingFields = missing,
                    ManualReviewRequired = !exported,
                    Notes = notes
                }
            });
        }

        return result;
    }

    private static CmdbNetwork BuildNetwork(ExportState state)
    {
        var net = new CmdbNetwork();
        foreach (var iface in state.NetworkInterfaces)
        {
            net.Interfaces.Add(new CmdbNetworkInterface
            {
                DeviceName = iface.DeviceName,
                ModuleName = iface.ModuleName,
                InterfaceName = iface.InterfaceName,
                IpAddress = iface.IpAddress,
                SubnetMask = iface.SubnetMask,
                SubnetName = iface.SubnetName,
                ProfinetName = iface.NodeNames.FirstOrDefault(),
                Path = iface.Path
            });
        }
        foreach (var link in state.NetworkLinks)
        {
            net.Links.Add(new CmdbNetworkLink
            {
                LinkType = link.LinkType,
                SourceName = link.SourceName,
                TargetName = link.TargetName,
                SubnetName = link.SubnetName
            });
        }
        foreach (var sub in state.Subnets)
        {
            net.Subnets.Add(new CmdbSubnet { Name = sub.Name, NetType = sub.NetType });
        }

        if (state.NetworkInterfaces.Count == 0)
        {
            net.OpenQuestions.Add("No network interfaces detected. Verify whether project actually has no PROFINET / Ethernet interfaces.");
        }
        return net;
    }

    private static CmdbSecurity BuildSecurity(ExportState state)
    {
        var sec = new CmdbSecurity();
        sec.SecurityConfigurationCpu = state.SecurityConfiguration.Cpu;
        sec.SecurityConfigurationNetwork = state.SecurityConfiguration.Network;
        foreach (var f in state.SecurityFindings)
        {
            sec.SecurityFindings.Add(f);
            if (f.Severity == "HIGH" || f.Severity == "MEDIUM")
            {
                sec.CraRelevantFindings.Add(f);
            }
            if (f.Finding.IndexOf("SNMP", StringComparison.OrdinalIgnoreCase) >= 0 ||
                f.Finding.IndexOf("PUT/GET", StringComparison.OrdinalIgnoreCase) >= 0 ||
                f.Finding.IndexOf("Webserver", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                sec.Iec62443RelevantFindings.Add(f);
            }
        }

        if (state.SecurityFindings.Any(x => x.Severity == "HIGH"))
        {
            sec.ManualReviewItems.Add("Resolve HIGH severity security findings before treating CMDB record as authoritative.");
        }
        return sec;
    }

    private static CmdbSafety BuildSafety(ExportState state)
    {
        var safety = new CmdbSafety();
        foreach (var asset in state.AssetInventory.Where(x => x.IsSafetyRelated))
        {
            safety.SafetyAssets.Add(asset.AssetId);
        }
        foreach (var fb in state.SafetyInventory.FBlocks)
        {
            safety.SafetyBlocks.Add(new CmdbSafetyBlock
            {
                Name = fb.ComponentName,
                PlcName = fb.PlcName,
                ExportStatus = fb.ExportStatus,
                IsProtected = state.SoftwareBlocks.FirstOrDefault(x => x.BlockName == fb.ComponentName)?.IsProtected == true
            });
        }
        foreach (var bf in state.BlockExportFailures.Where(x => x.IsSafetyRelated || x.IsProtected == true))
        {
            safety.BlockedExports.Add(new CmdbBlockedExport
            {
                BlockName = bf.BlockName,
                Reason = bf.SuspectedReason ?? bf.ErrorMessage,
                IsProtected = bf.IsProtected,
                IsSafetyRelated = bf.IsSafetyRelated,
                RequiredAction = bf.RequiredAction ?? "Manual review by safety engineer required."
            });
        }
        if (state.SafetyInventory.SafetyPresent)
        {
            safety.ManualReviewItems.Add("Safety project requires manual review by certified safety engineer.");
        }
        return safety;
    }

    private static CmdbLibraries BuildLibraries(ExportState state)
    {
        var libs = new CmdbLibraries();
        foreach (var lib in state.LibraryInventory)
        {
            libs.LibraryItems.Add(new CmdbLibraryItem
            {
                Name = lib.Name,
                Version = lib.Version,
                Kind = lib.Kind,
                ExportStatus = lib.ExportStatus,
                Path = lib.Path
            });
        }
        foreach (var fail in state.LibraryExportFailures)
        {
            libs.LibraryExportFailures.Add(new CmdbLibraryExportFailure
            {
                LibraryItemName = fail.LibraryItemName,
                Kind = fail.Kind,
                ErrorType = fail.ErrorType,
                ErrorMessage = fail.ErrorMessage,
                Classification = ClassifyLibraryFailure(fail),
                SuspectedReason = fail.SuspectedReason,
                RequiredAction = fail.RequiredAction
            });
        }
        foreach (var gap in state.LibraryDependencyGaps)
        {
            libs.DependencyGaps.Add(new CmdbDependencyGap
            {
                BlockName = gap.BlockName,
                SuspectedLibrarySource = gap.SuspectedLibrarySource,
                Confidence = gap.Confidence,
                Basis = gap.Basis
            });
        }
        if (state.LibraryInventory.Count > 0 && state.LibraryInventory.All(x => x.ExportStatus == "metadata_only" || x.ExportStatus == "failed"))
        {
            libs.ManualReviewItems.Add($"All {state.LibraryInventory.Count} library items lack a real export. Verify library API access and re-run export, or accept metadata-only inventory.");
        }
        return libs;
    }

    private static CmdbEvidence BuildEvidence(ExportState state)
    {
        var ev = new CmdbEvidence();
        foreach (var f in state.EvidenceFiles)
        {
            ev.EvidenceFiles.Add(new CmdbEvidenceFile
            {
                RelativePath = f.RelativePath,
                Sha256 = f.Sha256,
                SizeBytes = f.SizeBytes,
                FileType = f.FileType,
                SourceCategory = f.SourceCategory
            });
        }
        var expected = state.SoftwareInventory.Count(x => !string.IsNullOrWhiteSpace(x.ExportFile))
                       + state.LibraryInventory.Count(x => !string.IsNullOrWhiteSpace(x.ExportFile))
                       + state.AssetInventory.Count;
        var hashed = state.EvidenceFiles.Count(x => !string.IsNullOrWhiteSpace(x.Sha256));
        ev.CoverageSummary = new CmdbEvidenceCoverage
        {
            TotalExpected = expected,
            TotalHashed = hashed,
            MissingOrUnhashed = Math.Max(0, expected - hashed)
        };
        return ev;
    }

    private static CmdbCraReadiness BuildCraReadiness(ExportState state)
    {
        var readiness = new CmdbCraReadiness
        {
            QualityScore = state.ExportQualityScore.Score,
            NotALegalComplianceStatement = true
        };

        foreach (var gap in state.CraGapAnalysis)
        {
            readiness.GapSummary.Add(new CmdbGap
            {
                Category = gap.Category,
                Status = gap.Status,
                Severity = gap.Severity,
                Impact = gap.Impact,
                RequiredAction = gap.RequiredAction
            });
            if (gap.Severity == "HIGH")
            {
                readiness.BlockingGaps.Add(new CmdbGap
                {
                    Category = gap.Category,
                    Status = gap.Status,
                    Severity = gap.Severity,
                    Impact = gap.Impact,
                    RequiredAction = gap.RequiredAction
                });
                if (!readiness.RecommendedManualActions.Contains(gap.RequiredAction))
                {
                    readiness.RecommendedManualActions.Add(gap.RequiredAction);
                }
            }
            else if (gap.Severity == "MEDIUM" && !readiness.RecommendedManualActions.Contains(gap.RequiredAction))
            {
                readiness.RecommendedManualActions.Add(gap.RequiredAction);
            }
        }

        readiness.OverallStatus = readiness.BlockingGaps.Count > 0
            ? "insufficient"
            : readiness.GapSummary.Any(x => x.Severity == "MEDIUM") ? "partial" : "complete";

        return readiness;
    }

    public static string BuildAssetCsv(CmdbCraImport import)
    {
        var sb = new StringBuilder();
        sb.AppendLine("asset_id,asset_type,asset_name,parent_asset_id,vendor,product_family,product_name,order_number,firmware_version,software_version,ip_addresses,profinet_name,safety_related,security_relevant,vulnerability_lookup_keys,completeness,confidence,missing_fields,manual_review_required,evidence_refs");
        foreach (var a in import.Assets)
        {
            sb.Append(Csv(a.AssetId)).Append(',');
            sb.Append(Csv(a.AssetType)).Append(',');
            sb.Append(Csv(a.AssetName)).Append(',');
            sb.Append(Csv(a.ParentAssetId)).Append(',');
            sb.Append(Csv(a.Vendor)).Append(',');
            sb.Append(Csv(a.ProductFamily)).Append(',');
            sb.Append(Csv(a.ProductName)).Append(',');
            sb.Append(Csv(a.OrderNumber)).Append(',');
            sb.Append(Csv(a.FirmwareVersion)).Append(',');
            sb.Append(Csv(a.SoftwareVersion)).Append(',');
            sb.Append(Csv(string.Join(";", a.IpAddresses))).Append(',');
            sb.Append(Csv(a.ProfinetName)).Append(',');
            sb.Append(Csv(a.SafetyRelated.ToString().ToLowerInvariant())).Append(',');
            sb.Append(Csv(a.SecurityRelevant.ToString().ToLowerInvariant())).Append(',');
            sb.Append(Csv(string.Join(";", a.VulnerabilityLookupKeys))).Append(',');
            sb.Append(Csv(a.DataQuality.Completeness)).Append(',');
            sb.Append(Csv(a.DataQuality.Confidence)).Append(',');
            sb.Append(Csv(string.Join(";", a.DataQuality.MissingFields))).Append(',');
            sb.Append(Csv(a.DataQuality.ManualReviewRequired.ToString().ToLowerInvariant())).Append(',');
            sb.Append(Csv(string.Join(";", a.EvidenceRefs)));
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string Csv(string? value)
    {
        if (value == null) return "";
        var needsQuote = value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
        var escaped = value.Replace("\"", "\"\"");
        return needsQuote ? $"\"{escaped}\"" : escaped;
    }

    public static string BuildSchemaJson()
    {
        return """
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "TIA Exporter CMDB/CRA Import",
  "description": "Consolidated machine-readable export for CMDB, CRA readiness review and vulnerability lookup. This is an engineering data artifact, not a legal compliance statement.",
  "type": "object",
  "schema_version": "1.0.0",
  "compatibility": {
    "stable_fields": [
      "schema_version",
      "export_metadata.export_id",
      "export_metadata.export_timestamp",
      "export_metadata.project_name",
      "assets[].asset_id",
      "assets[].asset_type",
      "assets[].asset_name",
      "software_components[].component_id",
      "software_components[].name",
      "evidence.evidence_files[].sha256",
      "cra_readiness.blocking_gaps"
    ],
    "experimental_fields": [
      "assets[].remote_access_relevant",
      "assets[].internet_exposed",
      "security.iec62443_relevant_findings",
      "libraries.library_export_failures[].classification"
    ],
    "rules": [
      "Minor schema_version increments preserve stable fields and add only optional fields.",
      "Major schema_version increments may rename or remove fields. Consumers must check schema_version before parsing."
    ]
  },
  "required": ["schema_version", "export_metadata", "assets", "software_components", "network", "security", "safety", "libraries", "evidence", "cra_readiness"],
  "properties": {
    "schema_version": { "type": "string" },
    "export_metadata": {
      "type": "object",
      "required": ["export_id", "export_timestamp", "project_name", "exporter_version"],
      "properties": {
        "export_id": { "type": "string" },
        "export_timestamp": { "type": "string", "format": "date-time" },
        "exporter_version": { "type": "string" },
        "project_name": { "type": "string" },
        "project_path": { "type": "string" },
        "export_status": { "type": "string", "enum": ["successful", "partially_successful", "failed"] },
        "export_quality_score": { "type": "integer", "minimum": 0, "maximum": 100 }
      }
    },
    "assets": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["asset_id", "asset_type", "asset_name", "data_quality"],
        "properties": {
          "asset_id": { "type": "string" },
          "asset_type": { "type": "string", "enum": ["controller", "hmi", "drive", "io_module", "network_device", "software_component", "library", "safety_component", "module", "unknown"] },
          "asset_name": { "type": "string" },
          "data_quality": { "$ref": "#/definitions/data_quality" }
        }
      }
    },
    "software_components": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["component_id", "component_type", "name", "data_quality"],
        "properties": {
          "component_id": { "type": "string" },
          "component_type": { "type": "string" },
          "name": { "type": "string" },
          "data_quality": { "$ref": "#/definitions/data_quality" }
        }
      }
    },
    "cra_readiness": {
      "type": "object",
      "required": ["overall_status", "not_a_legal_compliance_statement"],
      "properties": {
        "overall_status": { "type": "string", "enum": ["complete", "partial", "insufficient"] },
        "not_a_legal_compliance_statement": { "type": "boolean", "const": true }
      }
    }
  },
  "definitions": {
    "data_quality": {
      "type": "object",
      "required": ["completeness", "confidence"],
      "properties": {
        "completeness": { "type": "string", "enum": ["complete", "partial", "missing"] },
        "confidence": { "type": "string", "enum": ["high", "medium", "low"] },
        "missing_fields": { "type": "array", "items": { "type": "string" } },
        "manual_review_required": { "type": "boolean" },
        "notes": { "type": "array", "items": { "type": "string" } }
      }
    }
  }
}
""";
    }

    public static string BuildReadme()
    {
        return """
# CMDB / CRA Import Artifact

This artifact consolidates the TIA Openness export into a single machine-readable file (`normalized/cmdb_cra_import.json`) for downstream CMDB ingestion, CRA readiness review and vulnerability lookup workflows.

## Disclaimer

This file is an **engineering export quality artifact**. It does not certify legal CRA conformity, IEC 62443 conformity or safety conformity. The field `cra_readiness.not_a_legal_compliance_statement` is permanently `true`.

## Files

| File | Purpose |
| --- | --- |
| `normalized/cmdb_cra_import.json` | Canonical, machine-readable consolidated export. |
| `normalized/cmdb_cra_import.schema.json` | JSON Schema for the canonical file (Draft-07). |
| `normalized/cmdb_cra_import.csv` | Flat per-asset view for spreadsheet review. |
| `reports/CMDB_CRA_IMPORT_README.md` | This documentation. |

## CMDB Import Logic (suggested)

1. Read `schema_version` and verify it is supported by the importer.
2. Use `export_metadata.export_id` as a unique batch identifier.
3. For each entry in `assets`:
   - Use `asset_id` as the stable CMDB CI primary key (deterministic SHA-256 prefix derived from project name, asset path, order number and type identifier).
   - Use `parent_asset_id` for hierarchical CI relationships.
   - Map `asset_type` to your CMDB CI class.
   - Use `vulnerability_lookup_keys` for CVE lookup automation (vendor + order number, vendor + product name, etc.).
4. For each entry in `software_components`:
   - Use `component_id` as a stable software-CI key.
   - Link it to a parent asset via `parent_asset_id`.
   - Use `hash_sha256` as the integrity reference.
5. Use `evidence.evidence_files[].sha256` to verify file integrity at import time.
6. Treat `cra_readiness.blocking_gaps` as items that must be reviewed before the CMDB record is considered authoritative.

## Field stability

Stable fields are listed in `cmdb_cra_import.schema.json` under `compatibility.stable_fields`. Experimental fields may change between minor versions.

## Manual review items

The following are intentionally **not** automated and must be reviewed by an engineer:

- HMI tags and screens (require WinCC Openness in current Openness configuration).
- Drive parameter sets (require Startdrive Openness).
- Protected / safety-locked blocks.
- Library items that could not be exported as XML.
- All HIGH severity security findings.

## Recommended import process

1. Validate `cmdb_cra_import.json` against `cmdb_cra_import.schema.json`.
2. Verify `evidence.coverage_summary.missing_or_unhashed == 0` or document the gap.
3. Resolve all `cra_readiness.blocking_gaps` or accept them with documented justification.
4. Import assets, software_components, network, security and safety sections into CMDB.
5. Hand HIGH-severity findings to security and safety reviewers before treating the CI as authoritative.
6. Re-export and re-import after any project change; `asset_id` and `component_id` stay stable for unchanged objects.
""";
    }

    private static string MapAssetType(AssetInventoryItem asset)
    {
        if (asset.IsController) return "controller";
        if (asset.IsHmi) return "hmi";
        if (asset.IsDrive) return "drive";
        if (asset.IsNetworkInterface) return "network_device";
        if (asset.IsSafetyRelated) return "safety_component";
        return string.IsNullOrWhiteSpace(asset.AssetType) ? "unknown" : asset.AssetType;
    }

    private static string MapComponentType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return "unknown";
        var lower = type!.ToLowerInvariant();
        if (lower.Contains("udt")) return "udt";
        if (lower.Contains("db")) return "db";
        if (lower.Contains("tag")) return "tag_table";
        return "plc_block";
    }

    private static string? ResolveParentAssetId(IEnumerable<AssetInventoryItem> all, AssetInventoryItem asset)
    {
        if (string.IsNullOrWhiteSpace(asset.ParentPath)) return null;
        var parent = all.FirstOrDefault(x => string.Equals(x.Path, asset.ParentPath, StringComparison.OrdinalIgnoreCase));
        return parent?.AssetId;
    }

    private static string? ResolveControllerAssetId(ExportState state, string plcName)
    {
        var controller = state.AssetInventory.FirstOrDefault(x => x.IsController && string.Equals(x.DeviceItemName, plcName, StringComparison.OrdinalIgnoreCase));
        return controller?.AssetId;
    }

    private static List<string> BuildLookupKeys(string? vendor, string? family, string? product, string? order, string? normalizedOrder, string? firmware)
    {
        var keys = new List<string>();
        if (!string.IsNullOrWhiteSpace(vendor) && !string.IsNullOrWhiteSpace(product)) keys.Add($"{vendor} {product}".Trim());
        if (!string.IsNullOrWhiteSpace(vendor) && !string.IsNullOrWhiteSpace(normalizedOrder)) keys.Add($"{vendor} {normalizedOrder}".Trim());
        if (!string.IsNullOrWhiteSpace(normalizedOrder) && !string.IsNullOrWhiteSpace(firmware)) keys.Add($"{normalizedOrder} {firmware}".Trim());
        if (!string.IsNullOrWhiteSpace(family) && !string.IsNullOrWhiteSpace(firmware)) keys.Add($"{family} {firmware}".Trim());
        return keys.Distinct().ToList();
    }

    private static Dictionary<string, EvidenceFile> BuildEvidenceLookup(ExportState state)
    {
        var dict = new Dictionary<string, EvidenceFile>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in state.EvidenceFiles)
        {
            dict[f.RelativePath.Replace('\\', '/')] = f;
        }
        return dict;
    }

    private static List<string> ResolveEvidenceRefs(Dictionary<string, EvidenceFile> lookup, string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/');
        return lookup.TryGetValue(normalized, out var f) ? new List<string> { $"{f.RelativePath}#sha256:{f.Sha256}" } : new List<string> { normalized };
    }

    private static string ClassifyLibraryFailure(LibraryExportFailure fail)
    {
        var msg = $"{fail.ErrorType} {fail.ErrorMessage}".ToLowerInvariant();
        if (msg.Contains("license")) return "missing_license_or_module";
        if (msg.Contains("protect")) return "protected_library";
        if (msg.Contains("not support") || msg.Contains("unsupported")) return "unsupported_export_type";
        if (msg.Contains("path") || msg.Contains("name") || msg.Contains("invalid")) return "path_or_name_issue";
        if (msg.Contains("not implement") || msg.Contains("api")) return "api_limitation";
        return "unknown_exception";
    }

    public static string StableHash(string value)
    {
        using var sha = SHA256.Create();
        return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(value)).Take(16).Select(x => x.ToString("x2")));
    }
}

internal sealed class CmdbCraImport
{
    public string SchemaVersion { get; set; } = "1.0.0";
    public CmdbExportMetadata ExportMetadata { get; set; } = new();
    public CmdbScope CmdbScope { get; set; } = new();
    public List<CmdbAsset> Assets { get; set; } = new();
    public List<CmdbSoftwareComponent> SoftwareComponents { get; set; } = new();
    public CmdbNetwork Network { get; set; } = new();
    public CmdbSecurity Security { get; set; } = new();
    public CmdbSafety Safety { get; set; } = new();
    public CmdbLibraries Libraries { get; set; } = new();
    public CmdbEvidence Evidence { get; set; } = new();
    public CmdbCraReadiness CraReadiness { get; set; } = new();
}

internal sealed class CmdbExportMetadata
{
    public string ExportId { get; set; } = "";
    public DateTimeOffset ExportTimestamp { get; set; }
    public string ExporterVersion { get; set; } = "";
    public string ProjectName { get; set; } = "";
    public string ProjectPath { get; set; } = "";
    public string? ProjectVersion { get; set; }
    public string? TiaPortalVersion { get; set; }
    public string? WindowsUser { get; set; }
    public string MachineName { get; set; } = "";
    public string ExportStatus { get; set; } = "";
    public int ExportQualityScore { get; set; }
    public List<string> Limitations { get; set; } = new();
}

internal sealed class CmdbScope
{
    public string MachineOrProjectIdentifier { get; set; } = "";
    public string? Customer { get; set; }
    public string? OrderNumber { get; set; }
    public string? PlantArea { get; set; }
    public string SupportRelevance { get; set; } = "unknown";
    public string CraRelevance { get; set; } = "unknown";
    public string SafetyRelevance { get; set; } = "unknown";
    public string NetworkRelevance { get; set; } = "unknown";
}

internal sealed class CmdbAsset
{
    public string AssetId { get; set; } = "";
    public string AssetType { get; set; } = "";
    public string AssetName { get; set; } = "";
    public string? ParentAssetId { get; set; }
    public string SourceFile { get; set; } = "";
    public string SourceSystem { get; set; } = "TIA Openness";
    public string Vendor { get; set; } = "Siemens";
    public string? ProductFamily { get; set; }
    public string? ProductName { get; set; }
    public string? OrderNumber { get; set; }
    public string? HardwareIdentifier { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? SoftwareVersion { get; set; }
    public string? SerialNumber { get; set; }
    public List<string> IpAddresses { get; set; } = new();
    public List<string> MacAddresses { get; set; } = new();
    public string? ProfinetName { get; set; }
    public List<string> NetworkInterfaces { get; set; } = new();
    public string? Subnet { get; set; }
    public bool SafetyRelated { get; set; }
    public bool SecurityRelevant { get; set; } = true;
    public bool InternetExposed { get; set; }
    public string RemoteAccessRelevant { get; set; } = "unknown";
    public List<string> VulnerabilityLookupKeys { get; set; } = new();
    public List<string> EvidenceRefs { get; set; } = new();
    public CmdbDataQuality DataQuality { get; set; } = new();
}

internal sealed class CmdbSoftwareComponent
{
    public string ComponentId { get; set; } = "";
    public string ComponentType { get; set; } = "unknown";
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string? ParentAssetId { get; set; }
    public string? Version { get; set; }
    public string? Language { get; set; }
    public string? BlockType { get; set; }
    public bool IsProtected { get; set; }
    public bool IsSafetyRelated { get; set; }
    public bool Exported { get; set; }
    public string ExportFormat { get; set; } = "metadata_only";
    public string? HashSha256 { get; set; }
    public List<string> EvidenceRefs { get; set; } = new();
    public List<string> DependencyRefs { get; set; } = new();
    public CmdbDataQuality DataQuality { get; set; } = new();
}

internal sealed class CmdbDataQuality
{
    public string Completeness { get; set; } = "partial";
    public string Confidence { get; set; } = "medium";
    public List<string> MissingFields { get; set; } = new();
    public bool ManualReviewRequired { get; set; }
    public List<string> Notes { get; set; } = new();
}

internal sealed class CmdbNetwork
{
    public List<CmdbNetworkInterface> Interfaces { get; set; } = new();
    public List<CmdbNetworkLink> Links { get; set; } = new();
    public List<CmdbSubnet> Subnets { get; set; } = new();
    public List<string> OpenQuestions { get; set; } = new();
}

internal sealed class CmdbNetworkInterface
{
    public string DeviceName { get; set; } = "";
    public string ModuleName { get; set; } = "";
    public string? InterfaceName { get; set; }
    public string? IpAddress { get; set; }
    public string? SubnetMask { get; set; }
    public string? SubnetName { get; set; }
    public string? ProfinetName { get; set; }
    public string Path { get; set; } = "";
}

internal sealed class CmdbNetworkLink
{
    public string LinkType { get; set; } = "";
    public string? SourceName { get; set; }
    public string? TargetName { get; set; }
    public string? SubnetName { get; set; }
}

internal sealed class CmdbSubnet
{
    public string Name { get; set; } = "";
    public string? NetType { get; set; }
}

internal sealed class CmdbSecurity
{
    public List<Dictionary<string, string?>> SecurityConfigurationCpu { get; set; } = new();
    public List<Dictionary<string, string?>> SecurityConfigurationNetwork { get; set; } = new();
    public List<SecurityFinding> SecurityFindings { get; set; } = new();
    public List<SecurityFinding> CraRelevantFindings { get; set; } = new();
    public List<SecurityFinding> Iec62443RelevantFindings { get; set; } = new();
    public List<string> ManualReviewItems { get; set; } = new();
}

internal sealed class CmdbSafety
{
    public List<string> SafetyAssets { get; set; } = new();
    public List<CmdbSafetyBlock> SafetyBlocks { get; set; } = new();
    public List<CmdbBlockedExport> BlockedExports { get; set; } = new();
    public List<string> ManualReviewItems { get; set; } = new();
}

internal sealed class CmdbSafetyBlock
{
    public string Name { get; set; } = "";
    public string PlcName { get; set; } = "";
    public string ExportStatus { get; set; } = "";
    public bool IsProtected { get; set; }
}

internal sealed class CmdbBlockedExport
{
    public string BlockName { get; set; } = "";
    public string Reason { get; set; } = "";
    public bool? IsProtected { get; set; }
    public bool IsSafetyRelated { get; set; }
    public string RequiredAction { get; set; } = "";
}

internal sealed class CmdbLibraries
{
    public List<CmdbLibraryItem> LibraryItems { get; set; } = new();
    public List<CmdbLibraryExportFailure> LibraryExportFailures { get; set; } = new();
    public List<CmdbDependencyGap> DependencyGaps { get; set; } = new();
    public List<string> ManualReviewItems { get; set; } = new();
}

internal sealed class CmdbLibraryItem
{
    public string Name { get; set; } = "";
    public string? Version { get; set; }
    public string Kind { get; set; } = "";
    public string ExportStatus { get; set; } = "";
    public string? Path { get; set; }
}

internal sealed class CmdbLibraryExportFailure
{
    public string LibraryItemName { get; set; } = "";
    public string Kind { get; set; } = "";
    public string ErrorType { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    public string Classification { get; set; } = "unknown_exception";
    public string? SuspectedReason { get; set; }
    public string? RequiredAction { get; set; }
}

internal sealed class CmdbDependencyGap
{
    public string BlockName { get; set; } = "";
    public string? SuspectedLibrarySource { get; set; }
    public string Confidence { get; set; } = "low";
    public string Basis { get; set; } = "";
}

internal sealed class CmdbEvidence
{
    public List<CmdbEvidenceFile> EvidenceFiles { get; set; } = new();
    public string HashAlgorithm { get; set; } = "SHA-256";
    public CmdbEvidenceCoverage CoverageSummary { get; set; } = new();
}

internal sealed class CmdbEvidenceFile
{
    public string RelativePath { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public long SizeBytes { get; set; }
    public string FileType { get; set; } = "";
    public string SourceCategory { get; set; } = "";
}

internal sealed class CmdbEvidenceCoverage
{
    public int TotalExpected { get; set; }
    public int TotalHashed { get; set; }
    public int MissingOrUnhashed { get; set; }
}

internal sealed class CmdbCraReadiness
{
    public string OverallStatus { get; set; } = "partial";
    public int QualityScore { get; set; }
    public List<CmdbGap> GapSummary { get; set; } = new();
    public List<CmdbGap> BlockingGaps { get; set; } = new();
    public List<string> RecommendedManualActions { get; set; } = new();
    public bool NotALegalComplianceStatement { get; set; } = true;
}

internal sealed class CmdbGap
{
    public string Category { get; set; } = "";
    public string Status { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Impact { get; set; } = "";
    public string RequiredAction { get; set; } = "";
}

internal sealed class CmdbValidationResult
{
    public string SchemaVersion { get; set; } = "";
    public bool Valid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public DateTimeOffset CheckedAt { get; set; }
}
