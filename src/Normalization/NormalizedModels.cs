namespace TIAExporter.Normalization;

internal sealed class ExportState
{
    public string ProjectName { get; set; } = "";
    public string ProjectPath { get; set; } = "";
    public string ExportRoot { get; set; } = "";
    public DateTimeOffset ExportTimestamp { get; set; } = DateTimeOffset.Now;
    public string ExporterVersion { get; set; } = "1.0.0";
    public string? TiaPortalVersion { get; set; }
    public bool ExportSuccess { get; set; }
    public ExportSettingsSnapshot Settings { get; set; } = new();
    public List<string> Warnings { get; } = [];
    public List<string> Errors { get; } = [];
    public List<NormalizedDevice> Devices { get; } = [];
    public List<NormalizedController> Controllers { get; } = [];
    public List<NormalizedHardwareModule> HardwareModules { get; } = [];
    public List<NormalizedNetworkInterface> NetworkInterfaces { get; } = [];
    public List<NormalizedNetworkLink> NetworkLinks { get; } = [];
    public List<NormalizedSubnet> Subnets { get; } = [];
    public List<HardwareAttributeSnapshot> HardwareAttributes { get; } = [];
    public List<NormalizedSoftwareBlock> SoftwareBlocks { get; } = [];
    public List<NormalizedTagTable> TagTables { get; } = [];
    public List<NormalizedLibraryItem> Libraries { get; } = [];
    public List<EvidenceFile> EvidenceFiles { get; } = [];
    public List<ExportErrorDiagnostic> ExportErrors { get; } = [];
    public List<BlockExportFailure> BlockExportFailures { get; } = [];
    public List<LibraryExportFailure> LibraryExportFailures { get; } = [];
    public List<CapabilityEntry> CapabilityMatrix { get; } = [];
    public OpennessEnvironmentDiagnostic OpennessEnvironment { get; set; } = new();
    public List<AssetInventoryItem> AssetInventory { get; } = [];
    public List<FirmwareInventoryItem> FirmwareInventory { get; } = [];
    public List<SoftwareInventoryItem> SoftwareInventory { get; } = [];
    public List<LibraryInventoryItem> LibraryInventory { get; } = [];
    public List<LibraryDependencyGap> LibraryDependencyGaps { get; } = [];
    public SafetyInventory SafetyInventory { get; set; } = new();
    public SecurityConfiguration SecurityConfiguration { get; set; } = new();
    public List<SecurityFinding> SecurityFindings { get; } = [];
    public List<HmiInventoryItem> HmiInventory { get; } = [];
    public List<DriveInventoryItem> DriveInventory { get; } = [];
    public List<CraGapAnalysisItem> CraGapAnalysis { get; } = [];
    public ExportQualityScore ExportQualityScore { get; set; } = new();
    public List<string> Limitations { get; } = [];
    public List<string> RequiredManualActions { get; } = [];
    public Dictionary<string, int> Counts { get; } = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class ExportSettingsSnapshot
{
    public bool Headless { get; set; }
    public bool DiagnosticsOnly { get; set; }
    public bool CompileCheck { get; set; }
    public bool CompileBeforeExport { get; set; }
    public bool IncludeHmi { get; set; } = true;
    public bool IncludeDrives { get; set; } = true;
    public bool IncludeLibraries { get; set; } = true;
    public bool IncludeDocuments { get; set; } = true;
    public bool Strict { get; set; }
}

internal sealed record NormalizedDevice(string Name, string? TypeIdentifier, string? OrderNumber, string? FirmwareVersion, string SourcePath);
internal sealed record NormalizedController(string PlcName, string DeviceName, string DeviceItemName, string SourcePath);
internal sealed record NormalizedHardwareModule(string DeviceName, string Name, string? TypeIdentifier, string? OrderNumber, string? FirmwareVersion, string Path);
internal sealed class NormalizedNetworkInterface
{
    public string DeviceName { get; set; } = "";
    public string ModuleName { get; set; } = "";
    public string? InterfaceName { get; set; }
    public string? InterfaceType { get; set; }
    public string? OperatingMode { get; set; }
    public string? IpAddress { get; set; }
    public string? SubnetMask { get; set; }
    public string? SubnetName { get; set; }
    public List<string> NodeNames { get; set; } = [];
    public List<string> PortNames { get; set; } = [];
    public List<string> IoSystemNames { get; set; } = [];
    public Dictionary<string, string?> Attributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string Path { get; set; } = "";
}

internal sealed class NormalizedNetworkLink
{
    public string LinkType { get; set; } = "";
    public string SourcePath { get; set; } = "";
    public string? SourceName { get; set; }
    public string? TargetPath { get; set; }
    public string? TargetName { get; set; }
    public string? SubnetName { get; set; }
    public string? IoSystemName { get; set; }
}

internal sealed class NormalizedSubnet
{
    public string Name { get; set; } = "";
    public string? NetType { get; set; }
    public List<string> IoSystemNames { get; set; } = [];
    public Dictionary<string, string?> Attributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class HardwareAttributeSnapshot
{
    public string ObjectPath { get; set; } = "";
    public string ObjectType { get; set; } = "";
    public Dictionary<string, string?> Attributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class NormalizedSoftwareBlock
{
    public string PlcName { get; set; } = "";
    public string BlockName { get; set; } = "";
    public string? BlockType { get; set; }
    public string? BlockNumber { get; set; }
    public string? Language { get; set; }
    public string GroupPath { get; set; } = "";
    public string? ExportFile { get; set; }
    public string? DocumentFile { get; set; }
    public string? HashSha256 { get; set; }
    public bool ExportSuccess { get; set; }
    public bool ExportAttempted { get; set; }
    public string? ExportErrorType { get; set; }
    public string? ExportErrorMessage { get; set; }
    public string? ExportErrorStackShort { get; set; }
    public string? SuspectedReason { get; set; }
    public string? RequiredAction { get; set; }
    public bool? IsConsistent { get; set; }
    public bool? IsCompiled { get; set; }
    public bool? IsProtected { get; set; }
    public bool IsSafetyRelated { get; set; }
    public bool IsSystemBlock { get; set; }
    public bool? CanExportXml { get; set; }
    public bool? CanExportDocument { get; set; }
    public bool FallbackExportUsed { get; set; }
    public string EvidenceStatus { get; set; } = "metadata_only";
    public string? FileType { get; set; }
    public Dictionary<string, string?> Attributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public CraMetadata CraMetadata { get; set; } = new();
}

internal sealed class CraMetadata
{
    public string? Module { get; set; }
    public string? Version { get; set; }
    public string? Library { get; set; }
    public string? Owner { get; set; }
    public bool? CraRelevant { get; set; }
    public bool? SafetyRelevant { get; set; }
    public bool? NetworkRelevant { get; set; }
    public string? Description { get; set; }
}

internal sealed record NormalizedTagTable(string PlcName, string Name, string GroupPath, int? TagCount, string? ExportFile, bool ExportSuccess);
internal sealed class NormalizedLibraryItem
{
    public NormalizedLibraryItem()
    {
    }

    public NormalizedLibraryItem(string name, string? version, string kind, string? exportFile, bool exportSuccess)
    {
        Name = name;
        Version = version;
        Kind = kind;
        ExportFile = exportFile;
        ExportSuccess = exportSuccess;
    }

    public string Name { get; set; } = "";
    public string? Version { get; set; }
    public string Kind { get; set; } = "";
    public string? ExportFile { get; set; }
    public string? DocumentFile { get; set; }
    public bool ExportSuccess { get; set; }
    public bool ExportAttempted { get; set; }
    public bool FallbackExportUsed { get; set; }
    public string ExportStatus { get; set; } = "metadata_only";
    public string? Path { get; set; }
    public string? Author { get; set; }
    public string? Comment { get; set; }
    public string? LastModified { get; set; }
    public string? ExportErrorType { get; set; }
    public string? ExportErrorMessage { get; set; }
    public string? SuspectedReason { get; set; }
    public string? RequiredAction { get; set; }
}

internal sealed class EvidenceFile
{
    public string RelativePath { get; set; } = "";
    public string FileType { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string SourceObject { get; set; } = "";
    public string SourceCategory { get; set; } = "";
    public bool ExportSuccess { get; set; }
    public string EvidenceLevel { get; set; } = "raw_export";
    public List<string> UsedForCraCategories { get; set; } = [];
}

internal class ExportErrorDiagnostic
{
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
    public string Category { get; set; } = "";
    public string Context { get; set; } = "";
    public string ErrorType { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    public string? StackShort { get; set; }
    public string? SuspectedReason { get; set; }
    public string? RequiredAction { get; set; }
}

internal sealed class BlockExportFailure : ExportErrorDiagnostic
{
    public string PlcName { get; set; } = "";
    public string BlockName { get; set; } = "";
    public string? BlockType { get; set; }
    public string GroupPath { get; set; } = "";
    public bool? IsProtected { get; set; }
    public bool IsSafetyRelated { get; set; }
}

internal sealed class LibraryExportFailure : ExportErrorDiagnostic
{
    public string LibraryItemName { get; set; } = "";
    public string? Version { get; set; }
    public string Kind { get; set; } = "";
    public string? Path { get; set; }
}

internal sealed class CapabilityEntry
{
    public string Capability { get; set; } = "";
    public bool Required { get; set; }
    public bool? Available { get; set; }
    public string Evidence { get; set; } = "";
    public string ImpactIfMissing { get; set; } = "";
}

internal sealed class OpennessEnvironmentDiagnostic
{
    public string? OsVersion { get; set; }
    public string? DotNetVersion { get; set; }
    public string? TiaPortalVersion { get; set; }
    public string? SiemensEngineeringDllPath { get; set; }
    public string? SiemensEngineeringDllVersion { get; set; }
    public List<string> SiemensEngineeringAssemblies { get; set; } = [];
    public List<string> DetectedTiaProducts { get; set; } = [];
    public bool? Step7Present { get; set; }
    public bool? WinCcPresent { get; set; }
    public bool? StartdrivePresent { get; set; }
    public bool? SafetyPresent { get; set; }
    public bool? UnifiedAssembliesPresent { get; set; }
    public string? WindowsUser { get; set; }
    public bool? UserInSiemensOpennessGroup { get; set; }
    public string? ProjectPath { get; set; }
    public string? ProjectVersion { get; set; }
    public string? ExporterVersion { get; set; }
    public List<string> Warnings { get; set; } = [];
}

internal sealed class AssetInventoryItem
{
    public string AssetId { get; set; } = "";
    public string ProjectName { get; set; } = "";
    public string DeviceName { get; set; } = "";
    public string DeviceItemName { get; set; } = "";
    public string AssetType { get; set; } = "";
    public string Vendor { get; set; } = "Siemens";
    public string? ProductFamily { get; set; }
    public string? OrderNumber { get; set; }
    public string? NormalizedOrderNumber { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? TypeIdentifier { get; set; }
    public string Path { get; set; } = "";
    public string? ParentPath { get; set; }
    public List<string> NetworkInterfaces { get; set; } = [];
    public bool IsController { get; set; }
    public bool IsDrive { get; set; }
    public bool IsHmi { get; set; }
    public bool IsNetworkInterface { get; set; }
    public bool IsSafetyRelated { get; set; }
    public string EvidenceSource { get; set; } = "hardware_modules.json";
}

internal sealed class FirmwareInventoryItem
{
    public string Vendor { get; set; } = "Siemens";
    public string Product { get; set; } = "";
    public string? OrderNumber { get; set; }
    public string? FirmwareVersion { get; set; }
    public string SourcePath { get; set; } = "";
    public string EvidenceFile { get; set; } = "normalized/hardware_modules.json";
    public string VulnerabilityLookupKey { get; set; } = "";
}

internal sealed class SoftwareInventoryItem
{
    public string ComponentId { get; set; } = "";
    public string PlcName { get; set; } = "";
    public string ComponentName { get; set; } = "";
    public string? ComponentType { get; set; }
    public string? TiaBlockType { get; set; }
    public string? Language { get; set; }
    public string? BlockNumber { get; set; }
    public string GroupPath { get; set; } = "";
    public bool IsSafetyRelated { get; set; }
    public bool IsNetworkRelatedGuess { get; set; }
    public bool IsHmiRelatedGuess { get; set; }
    public bool IsRecipeRelatedGuess { get; set; }
    public bool IsDiagnosticsRelatedGuess { get; set; }
    public bool IsStandardLibraryGuess { get; set; }
    public string? ExportFile { get; set; }
    public string? DocumentFile { get; set; }
    public string? HashSha256 { get; set; }
    public string ExportStatus { get; set; } = "metadata_only";
    public CraMetadata CraMetadata { get; set; } = new();
    public List<string> EvidenceFiles { get; set; } = [];
    public string RiskRelevanceGuess { get; set; } = "review";
    public bool ReviewRequired { get; set; } = true;
}

internal sealed class LibraryInventoryItem
{
    public string Name { get; set; } = "";
    public string? Version { get; set; }
    public string Kind { get; set; } = "";
    public string? Author { get; set; }
    public string? Comment { get; set; }
    public string? LastModified { get; set; }
    public string? Path { get; set; }
    public string ExportStatus { get; set; } = "metadata_only";
    public string? ExportFile { get; set; }
    public string? DocumentFile { get; set; }
    public string? ExportErrorType { get; set; }
    public string? ExportErrorMessage { get; set; }
}

internal sealed class LibraryDependencyGap
{
    public string BlockName { get; set; } = "";
    public string? SuspectedLibrarySource { get; set; }
    public string Confidence { get; set; } = "low";
    public string Basis { get; set; } = "";
    public bool ReviewRequired { get; set; } = true;
}

internal sealed class SafetyInventory
{
    public bool SafetyPresent { get; set; }
    public bool FCpu { get; set; }
    public bool? FCapabilityActivated { get; set; }
    public List<SoftwareInventoryItem> FBlocks { get; set; } = [];
    public Dictionary<string, string> FBlockExportStatus { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<HardwareAttributeSnapshot> FParametersFromHardwareAttributes { get; set; } = [];
    public List<string> Limitations { get; set; } = [];
}

internal sealed class SecurityConfiguration
{
    public List<Dictionary<string, string?>> Cpu { get; set; } = [];
    public List<Dictionary<string, string?>> Network { get; set; } = [];
    public List<string> Limitations { get; set; } = [];
}

internal sealed class SecurityFinding
{
    public string Severity { get; set; } = "INFO";
    public string Finding { get; set; } = "";
    public string Evidence { get; set; } = "";
    public string RequiredAction { get; set; } = "";
    public string Category { get; set; } = "CMDB/CRA review finding";
}

internal sealed class HmiInventoryItem
{
    public string Name { get; set; } = "";
    public string? RuntimeName { get; set; }
    public string Path { get; set; } = "";
    public List<string> NetworkInterfaces { get; set; } = [];
    public string ExportStatus { get; set; } = "metadata_only";
    public List<string> Limitations { get; set; } = [];
    public string? RequiredAction { get; set; }
}

internal sealed class DriveInventoryItem
{
    public string Name { get; set; } = "";
    public string? OrderNumber { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? TypeName { get; set; }
    public string? NetworkInterface { get; set; }
    public string? IpAddress { get; set; }
    public string? ProfinetDeviceName { get; set; }
    public string? IoSystem { get; set; }
    public string? TelegramParameterInfo { get; set; }
    public string ExportStatus { get; set; } = "metadata_only";
    public List<string> Limitations { get; set; } = [];
}

internal sealed class CraGapAnalysisItem
{
    public string Category { get; set; } = "";
    public string Status { get; set; } = "MISSING";
    public string Evidence { get; set; } = "";
    public List<string> MissingFields { get; set; } = [];
    public string Impact { get; set; } = "";
    public string RequiredAction { get; set; } = "";
    public string Severity { get; set; } = "INFO";
}

internal sealed class ExportQualityScore
{
    public int Score { get; set; }
    public Dictionary<string, int> CategoryScores { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> CategoryMaximums { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Rationale { get; set; } = [];
}

internal sealed class Manifest
{
    public string ProjectName { get; set; } = "";
    public string ProjectPath { get; set; } = "";
    public DateTimeOffset ExportTimestamp { get; set; }
    public string ExporterVersion { get; set; } = "";
    public string? TiaPortalVersion { get; set; }
    public string MachineName { get; set; } = Environment.MachineName;
    public string ProjectFolderName { get; set; } = "";
    public bool ExportSuccess { get; set; }
    public string ExportStatus { get; set; } = "";
    public int TotalErrors { get; set; }
    public int TotalWarnings { get; set; }
    public Dictionary<string, bool?> CapabilitySummary { get; set; } = [];
    public Dictionary<string, int> ExportCounts { get; set; } = [];
    public string CraReadinessSummary { get; set; } = "";
    public List<string> OutputFiles { get; set; } = [];
    public List<string> Limitations { get; set; } = [];
    public List<string> RequiredManualActions { get; set; } = [];
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public Dictionary<string, int> Counts { get; set; } = [];
}

internal sealed class ExportReport
{
    public string Status { get; set; } = "";
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public int MissingCraMetadata { get; set; }
    public Dictionary<string, int> Counts { get; set; } = [];
    public List<string> Limitations { get; set; } = [];
}
