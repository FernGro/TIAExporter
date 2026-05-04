using TIAExporter.Normalization;

namespace TIAExporter.Tests;

internal static class CmdbCraImportTests
{
    internal static void Run()
    {
        TestSchemaVersion();
        TestDeterministicAssetIds();
        TestCsvEscaping();
        TestSchemaJsonValid();
        TestHighGapsBecomeBlocking();
        TestEmptyStateProducesValidStructure();
        TestProtectedSafetyBlockSurfaces();
        TestEmptyLibrariesNoCrash();
        TestDataQualityRulesForHardware();
        TestSoftwareComponentMissingHash();
        TestEvidenceCoverageSummary();
        TestNotALegalComplianceStatement();
        Console.WriteLine("[PASS] All CmdbCraImport tests passed.");
    }

    private static ExportState MakeBasicState()
    {
        var state = new ExportState
        {
            ProjectName = "TestProject",
            ProjectPath = @"C:\proj\TestProject.ap20",
            ExportTimestamp = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero)
        };
        return state;
    }

    private static void TestSchemaVersion()
    {
        var state = MakeBasicState();
        var import = new CmdbCraImportGenerator().Build(state);
        AssertEqual("1.0.0", import.SchemaVersion, nameof(TestSchemaVersion));
        Assert(!string.IsNullOrEmpty(import.ExportMetadata.ExportId), nameof(TestSchemaVersion) + "_export_id");
    }

    private static void TestDeterministicAssetIds()
    {
        var state = MakeBasicState();
        state.AssetInventory.Add(new AssetInventoryItem
        {
            AssetId = "asset-abc",
            DeviceItemName = "PLC_1",
            Path = "Devices/PLC_1",
            OrderNumber = "6ES7 511-1AK02-0AB0",
            FirmwareVersion = "V2.9",
            IsController = true,
            TypeIdentifier = "CPU 1511-1 PN"
        });

        var a = new CmdbCraImportGenerator().Build(state);
        var b = new CmdbCraImportGenerator().Build(state);
        AssertEqual(a.Assets[0].AssetId, b.Assets[0].AssetId, nameof(TestDeterministicAssetIds));
        AssertEqual("asset-abc", a.Assets[0].AssetId, nameof(TestDeterministicAssetIds) + "_passthrough");
        Assert(a.Assets[0].VulnerabilityLookupKeys.Count > 0, nameof(TestDeterministicAssetIds) + "_lookup_keys");
    }

    private static void TestCsvEscaping()
    {
        var import = new CmdbCraImport
        {
            Assets = new List<CmdbAsset>
            {
                new()
                {
                    AssetId = "id1",
                    AssetType = "controller",
                    AssetName = "Name, with comma and \"quote\"",
                    Vendor = "Siemens",
                    IpAddresses = new List<string> { "10.0.0.1", "10.0.0.2" },
                    DataQuality = new CmdbDataQuality { Completeness = "complete", Confidence = "high" }
                }
            }
        };
        var csv = CmdbCraImportGenerator.BuildAssetCsv(import);
        Assert(csv.Contains("\"Name, with comma and \"\"quote\"\"\""), nameof(TestCsvEscaping) + "_escape");
        Assert(csv.Contains("10.0.0.1;10.0.0.2"), nameof(TestCsvEscaping) + "_ip_join");
        Assert(csv.StartsWith("asset_id,asset_type,asset_name"), nameof(TestCsvEscaping) + "_header");
    }

    private static void TestSchemaJsonValid()
    {
        var schema = CmdbCraImportGenerator.BuildSchemaJson();
        Assert(schema.Contains("\"schema_version\""), nameof(TestSchemaJsonValid) + "_version");
        Assert(schema.Contains("\"data_quality\""), nameof(TestSchemaJsonValid) + "_data_quality");
        Assert(schema.Contains("\"stable_fields\""), nameof(TestSchemaJsonValid) + "_compat");
        // Roughly balanced braces
        var open = schema.Count(c => c == '{');
        var close = schema.Count(c => c == '}');
        AssertEqual(open.ToString(), close.ToString(), nameof(TestSchemaJsonValid) + "_braces");
    }

    private static void TestHighGapsBecomeBlocking()
    {
        var state = MakeBasicState();
        state.CraGapAnalysis.Add(new CraGapAnalysisItem { Category = "Library inventory", Status = "MISSING", Severity = "HIGH", Impact = "x", RequiredAction = "fix lib export" });
        state.CraGapAnalysis.Add(new CraGapAnalysisItem { Category = "Network", Status = "PARTIAL", Severity = "MEDIUM", Impact = "y", RequiredAction = "review" });
        state.CraGapAnalysis.Add(new CraGapAnalysisItem { Category = "Asset", Status = "COMPLETE", Severity = "INFO", Impact = "z", RequiredAction = "" });

        var import = new CmdbCraImportGenerator().Build(state);
        AssertEqual("1", import.CraReadiness.BlockingGaps.Count.ToString(), nameof(TestHighGapsBecomeBlocking) + "_count");
        AssertEqual("Library inventory", import.CraReadiness.BlockingGaps[0].Category, nameof(TestHighGapsBecomeBlocking) + "_cat");
        AssertEqual("insufficient", import.CraReadiness.OverallStatus, nameof(TestHighGapsBecomeBlocking) + "_overall");
        Assert(import.CraReadiness.RecommendedManualActions.Contains("fix lib export"), nameof(TestHighGapsBecomeBlocking) + "_action_high");
        Assert(import.CraReadiness.RecommendedManualActions.Contains("review"), nameof(TestHighGapsBecomeBlocking) + "_action_medium");
    }

    private static void TestEmptyStateProducesValidStructure()
    {
        var state = MakeBasicState();
        var import = new CmdbCraImportGenerator().Build(state);
        Assert(import.Assets != null, nameof(TestEmptyStateProducesValidStructure) + "_assets");
        Assert(import.SoftwareComponents != null, nameof(TestEmptyStateProducesValidStructure) + "_sw");
        Assert(import.Network != null, nameof(TestEmptyStateProducesValidStructure) + "_net");
        Assert(import.Evidence != null, nameof(TestEmptyStateProducesValidStructure) + "_ev");
        AssertEqual("complete", import.CraReadiness.OverallStatus, nameof(TestEmptyStateProducesValidStructure) + "_overall");
    }

    private static void TestProtectedSafetyBlockSurfaces()
    {
        var state = MakeBasicState();
        state.SoftwareBlocks.Add(new NormalizedSoftwareBlock
        {
            PlcName = "PLC_1",
            BlockName = "F_Block_1",
            BlockType = "FB",
            IsProtected = true,
            IsSafetyRelated = true,
            ExportSuccess = false,
            EvidenceStatus = "metadata_only"
        });
        state.SoftwareInventory.Add(new SoftwareInventoryItem
        {
            ComponentId = "comp-1",
            PlcName = "PLC_1",
            ComponentName = "F_Block_1",
            IsSafetyRelated = true,
            ExportStatus = "metadata_only"
        });
        state.BlockExportFailures.Add(new BlockExportFailure
        {
            PlcName = "PLC_1",
            BlockName = "F_Block_1",
            IsProtected = true,
            IsSafetyRelated = true,
            ErrorType = "Protected",
            ErrorMessage = "block is know-how protected",
            SuspectedReason = "block is know-how protected"
        });

        var import = new CmdbCraImportGenerator().Build(state);
        var sw = import.SoftwareComponents.First(x => x.Name == "F_Block_1");
        Assert(sw.IsProtected, nameof(TestProtectedSafetyBlockSurfaces) + "_protected");
        Assert(sw.DataQuality.ManualReviewRequired, nameof(TestProtectedSafetyBlockSurfaces) + "_review");
        AssertEqual("1", import.Safety.BlockedExports.Count.ToString(), nameof(TestProtectedSafetyBlockSurfaces) + "_blocked");
    }

    private static void TestEmptyLibrariesNoCrash()
    {
        var state = MakeBasicState();
        var import = new CmdbCraImportGenerator().Build(state);
        AssertEqual("0", import.Libraries.LibraryItems.Count.ToString(), nameof(TestEmptyLibrariesNoCrash));
        AssertEqual("0", import.Libraries.LibraryExportFailures.Count.ToString(), nameof(TestEmptyLibrariesNoCrash) + "_fail");
    }

    private static void TestDataQualityRulesForHardware()
    {
        var state = MakeBasicState();
        state.AssetInventory.Add(new AssetInventoryItem
        {
            AssetId = "asset-1",
            DeviceItemName = "Module1",
            Path = "Devices/Module1"
            // no order_number, no firmware
        });
        var import = new CmdbCraImportGenerator().Build(state);
        var dq = import.Assets[0].DataQuality;
        Assert(dq.MissingFields.Contains("order_number"), nameof(TestDataQualityRulesForHardware) + "_order");
        Assert(dq.MissingFields.Contains("firmware_version"), nameof(TestDataQualityRulesForHardware) + "_fw");
        AssertEqual("partial", dq.Completeness, nameof(TestDataQualityRulesForHardware) + "_completeness");
    }

    private static void TestSoftwareComponentMissingHash()
    {
        var state = MakeBasicState();
        state.SoftwareInventory.Add(new SoftwareInventoryItem
        {
            ComponentId = "c1",
            PlcName = "PLC_1",
            ComponentName = "FB1",
            ExportStatus = "metadata_only"
        });
        var import = new CmdbCraImportGenerator().Build(state);
        var sw = import.SoftwareComponents[0];
        Assert(sw.DataQuality.MissingFields.Contains("hash_sha256"), nameof(TestSoftwareComponentMissingHash) + "_hash");
        Assert(sw.DataQuality.MissingFields.Contains("export_file"), nameof(TestSoftwareComponentMissingHash) + "_xml");
        Assert(!sw.Exported, nameof(TestSoftwareComponentMissingHash) + "_exported");
    }

    private static void TestEvidenceCoverageSummary()
    {
        var state = MakeBasicState();
        state.AssetInventory.Add(new AssetInventoryItem { AssetId = "a1", DeviceItemName = "X", Path = "p" });
        state.EvidenceFiles.Add(new EvidenceFile { RelativePath = "normalized/devices.json", Sha256 = "abcd" });
        var import = new CmdbCraImportGenerator().Build(state);
        Assert(import.Evidence.CoverageSummary.TotalExpected >= 1, nameof(TestEvidenceCoverageSummary) + "_expected");
        Assert(import.Evidence.CoverageSummary.TotalHashed >= 1, nameof(TestEvidenceCoverageSummary) + "_hashed");
        AssertEqual("SHA-256", import.Evidence.HashAlgorithm, nameof(TestEvidenceCoverageSummary) + "_alg");
    }

    private static void TestNotALegalComplianceStatement()
    {
        var state = MakeBasicState();
        var import = new CmdbCraImportGenerator().Build(state);
        Assert(import.CraReadiness.NotALegalComplianceStatement, nameof(TestNotALegalComplianceStatement));
    }

    private static void Assert(bool condition, string testName)
    {
        if (!condition) throw new Exception($"[FAIL] {testName}");
    }

    private static void AssertEqual(string expected, string actual, string testName)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new Exception($"[FAIL] {testName}: expected '{expected}', got '{actual}'");
        }
    }
}
