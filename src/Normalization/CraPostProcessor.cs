using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;
using Siemens.Engineering;

namespace TIAExporter.Normalization;

internal sealed class CraPostProcessor
{
    public void Process(ExportState state)
    {
        BuildOpennessEnvironment(state);
        BuildCapabilityMatrix(state);
        BuildAssetAndFirmwareInventory(state);
        BuildSoftwareInventory(state);
        BuildLibraryInventory(state);
        BuildSecurityConfiguration(state);
        BuildSafetyInventory(state);
        BuildHmiAndDriveInventory(state);
        BuildGapAnalysis(state);
        BuildQualityScore(state);
        BuildManualActionsAndLimitations(state);
    }

    private static void BuildOpennessEnvironment(ExportState state)
    {
        var assembly = typeof(TiaPortal).Assembly;
        var dllPath = Try(() => assembly.Location);
        var engineeringDir = string.IsNullOrWhiteSpace(dllPath) ? null : Path.GetDirectoryName(dllPath);
        var assemblies = engineeringDir != null && Directory.Exists(engineeringDir)
            ? Directory.EnumerateFiles(engineeringDir, "Siemens.Engineering*.dll").Select(Path.GetFileName).Where(x => x != null).Cast<string>().OrderBy(x => x).ToList()
            : [];

        var detectedProducts = DetectTiaProducts();
        var windowsUser = $"{Environment.UserDomainName}\\{Environment.UserName}";

        var tiaInstalled = DetectTiaPortalInstalled(detectedProducts);
        var tiaInstallPath = DetectTiaPortalInstallPath();
        var step7ProfInstalled = DetectStep7ProfessionalInstalled(detectedProducts);
        var step7SafetyInstalled = DetectStep7SafetyInstalled(detectedProducts, assemblies);
        var winCcInstalled = DetectWinCcInstalled(detectedProducts, assemblies);
        var winCcUnifiedInstalled = DetectWinCcUnifiedInstalled(detectedProducts, assemblies);
        var startdriveInstalled = DetectStartdriveInstalled(detectedProducts, assemblies);
        var almInstallPath = DetectAlmInstallPath();

        // License status comes from the actual export attempt; ALM COM is not invoked
        bool? licenseAvailable = state.LicenseBlockingFailureDetected
            ? false
            : state.SoftwareBlocks.Any(x => x.ExportSuccess == true)
                ? true
                : null;

        var env = new OpennessEnvironmentDiagnostic
        {
            OsVersion = Environment.OSVersion.VersionString,
            DotNetVersion = Environment.Version.ToString(),
            TiaPortalVersion = state.TiaPortalVersion,
            TiaPortalInstalled = tiaInstalled,
            TiaPortalInstallPath = tiaInstallPath,
            OpennessApiAvailable = !string.IsNullOrWhiteSpace(dllPath),
            SiemensEngineeringDllPath = dllPath,
            SiemensEngineeringDllVersion = assembly.GetName().Version?.ToString(),
            SiemensEngineeringAssemblies = assemblies,
            Step7ProfessionalSoftwareInstalled = step7ProfInstalled,
            Step7SafetySoftwareInstalled = step7SafetyInstalled,
            WinCcSoftwareInstalled = winCcInstalled,
            WinCcUnifiedSoftwareInstalled = winCcUnifiedInstalled,
            StartdriveSoftwareInstalled = startdriveInstalled,
            AlmInstallPath = almInstallPath,
            AlmLicenseCheckSupported = false,
            Step7ProfessionalLicenseAvailableForOpenness = licenseAvailable,
            DetectedTiaProducts = detectedProducts,
            WindowsUser = windowsUser,
            UserInSiemensOpennessGroup = TryCheckOpennessGroup(),
            ProjectPath = state.ProjectPath,
            ProjectVersion = Path.GetExtension(state.ProjectPath).TrimStart('.').ToUpperInvariant(),
            ExporterVersion = state.ExporterVersion
        };

        env.DiagnosticNotes.Add("TIA Portal software installation and ALM license availability are checked independently.");
        env.DiagnosticNotes.Add("Step7ProfessionalSoftwareInstalled=true means TIA Portal V17+ was found in the registry (STEP 7 Professional is bundled). It does NOT guarantee ALM license availability.");
        env.DiagnosticNotes.Add("Step7ProfessionalLicenseAvailableForOpenness is derived from the Openness block export result, not from an ALM query.");
        env.DiagnosticNotes.Add("AlmLicenseCheckSupported=false: ALM COM API is not invoked; license availability is inferred from export attempt outcome.");

        if (state.LicenseBlockingFailureDetected)
        {
            env.DiagnosticNotes.Add(
                $"Openness block export reported license '{state.MissingLicenseName}' as not usable for this exporter process/session. " +
                "This can happen even when TIA shows a license as installed. Check ALM availability for the same Windows user, floating-license occupancy, license server reachability, and version match.");
        }

        if (string.IsNullOrWhiteSpace(dllPath))
        {
            env.Warnings.Add("Siemens.Engineering.dll path could not be resolved.");
        }

        if (env.UserInSiemensOpennessGroup == null)
        {
            env.Warnings.Add("Windows group membership for Siemens Openness could not be verified.");
        }

        state.OpennessEnvironment = env;
    }

    private static bool? DetectTiaPortalInstalled(List<string> products)
    {
        return products.Any(p => System.Text.RegularExpressions.Regex.IsMatch(p, @"_InstalledSW/TIAP\d+", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            ? true : null;
    }

    private static string? DetectTiaPortalInstallPath()
    {
        foreach (var basePath in new[] { @"SOFTWARE\Siemens\Automation\_InstalledSW", @"SOFTWARE\WOW6432Node\Siemens\Automation\_InstalledSW" })
        {
            using var baseKey = Try(() => Registry.LocalMachine.OpenSubKey(basePath));
            if (baseKey == null) continue;
            foreach (var subName in baseKey.GetSubKeyNames().Where(n => System.Text.RegularExpressions.Regex.IsMatch(n, @"^TIAP\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)).OrderByDescending(x => x))
            {
                using var key = Try(() => baseKey.OpenSubKey(subName));
                if (key == null) continue;
                foreach (var valueName in new[] { "InstallPath", "Location", "Path", "InstallDir" })
                {
                    var path = Try(() => key.GetValue(valueName) as string);
                    if (!string.IsNullOrWhiteSpace(path)) return path;
                }
            }
        }

        return null;
    }

    private static bool? DetectStep7ProfessionalInstalled(List<string> products)
    {
        // STEP 7 Professional is bundled with TIA Portal V17+; TIAP17–TIAP25+ means it is installed
        return products.Any(p => System.Text.RegularExpressions.Regex.IsMatch(p, @"_InstalledSW/TIAP(1[7-9]|2\d)", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            ? true : null;
    }

    private static bool? DetectStep7SafetyInstalled(List<string> products, List<string> assemblies)
    {
        return ContainsAny(products, "Safety", "S7Safety", "Failsafe") || assemblies.Any(x => x.IndexOf("Safety", StringComparison.OrdinalIgnoreCase) >= 0)
            ? true : null;
    }

    private static bool? DetectWinCcInstalled(List<string> products, List<string> assemblies)
    {
        return ContainsAny(products, "WinCC", "HMIRTM", "HVWebApp") || assemblies.Any(x => x.Equals("Siemens.Engineering.Hmi.dll", StringComparison.OrdinalIgnoreCase) || x.IndexOf(".Hmi.", StringComparison.OrdinalIgnoreCase) >= 0)
            ? true : null;
    }

    private static bool? DetectWinCcUnifiedInstalled(List<string> products, List<string> assemblies)
    {
        return ContainsAny(products, "Unified", "WinCC_Unified") || assemblies.Any(x => x.IndexOf("Unified", StringComparison.OrdinalIgnoreCase) >= 0)
            ? true : null;
    }

    private static bool? DetectStartdriveInstalled(List<string> products, List<string> assemblies)
    {
        return ContainsAny(products, "Startdrive", "Start Drive") || assemblies.Any(x => x.IndexOf("Startdrive", StringComparison.OrdinalIgnoreCase) >= 0)
            ? true : null;
    }

    private static string? DetectAlmInstallPath()
    {
        foreach (var regPath in new[] {
            @"SOFTWARE\Siemens\Automation License Manager",
            @"SOFTWARE\WOW6432Node\Siemens\Automation License Manager",
            @"SOFTWARE\Siemens\ALM" })
        {
            using var key = Try(() => Registry.LocalMachine.OpenSubKey(regPath));
            if (key == null) continue;
            foreach (var valueName in new[] { "InstallPath", "Location", "Path", "InstallDir" })
            {
                var path = Try(() => key.GetValue(valueName) as string);
                if (!string.IsNullOrWhiteSpace(path)) return path;
            }
        }

        return null;
    }

    private static void BuildCapabilityMatrix(ExportState state)
    {
        var env = state.OpennessEnvironment;
        state.CapabilityMatrix.Clear();
        AddCapability(state, "TIA_PORTAL_INSTALLED", true, env.TiaPortalInstalled,
            "TIA product registry scan (_InstalledSW/TIAPxx)",
            "TIA Portal not found; Openness API cannot operate.",
            "TIA Portal V17+ detected in Windows registry.");
        AddCapability(state, "OPENNESS_API_AVAILABLE", true, env.OpennessApiAvailable ? true : null,
            "Siemens.Engineering.dll presence and load",
            "Openness API DLL missing; no export possible.",
            "Siemens.Engineering.dll loaded successfully.");
        AddCapability(state, "STEP7_PROFESSIONAL_SOFTWARE_INSTALLED", true, env.Step7ProfessionalSoftwareInstalled,
            "TIA Portal V17+ registry presence (STEP 7 Professional is bundled)",
            "STEP 7 Professional software not detected; block export will fail.",
            "STEP 7 Professional is bundled with TIA Portal V17+; registry presence is sufficient evidence.");
        AddCapability(state, "STEP7_PROFESSIONAL_LICENSE_AVAILABLE_FOR_OPENNESS", true, env.Step7ProfessionalLicenseAvailableForOpenness,
            "Openness block export attempt result (LicenseNotFoundException detection)",
            "ALM license not usable by this exporter process: block XML export may be blocked.",
            "Inferred from export outcome. null=not yet tested. false=LicenseNotFoundException thrown. true=at least one block exported.");
        AddCapability(state, "PLC_BLOCK_XML_EXPORT", true, state.SoftwareBlocks.Any(x => x.CanExportXml == true),
            "software_blocks.json export status",
            "Software inventory incomplete.");
        AddCapability(state, "PLC_BLOCK_DOCUMENT_EXPORT", false, state.SoftwareBlocks.Any(x => x.FallbackExportUsed == true),
            "software_blocks.json document fallback status",
            "Document fallback not available; some blocks may lack exported content.");
        AddCapability(state, "PLC_TAG_TABLE_EXPORT", true, state.TagTables.Any(x => x.ExportSuccess),
            "tag_tables.json export status",
            "PLC tags incomplete.");
        AddCapability(state, "UDT_EXPORT", true, state.SoftwareBlocks.Any(x => string.Equals(x.BlockType, "UDT", StringComparison.OrdinalIgnoreCase) && x.ExportSuccess),
            "UDT entries in software_blocks.json",
            "Data type inventory incomplete.");
        AddCapability(state, "HMI_EXPORT", false, state.HmiInventory.Count > 0 ? true : env.WinCcSoftwareInstalled,
            "WinCC assemblies and HMI inventory",
            "HMI inventory incomplete.");
        AddCapability(state, "STARTDRIVE_EXPORT", false, env.StartdriveSoftwareInstalled,
            "TIA product registry scan",
            "Drive parameter inventory incomplete.");
        AddCapability(state, "LIBRARY_TYPE_EXPORT", true, state.Libraries.Any(x => x.ExportStatus == "full_xml" || x.ExportStatus == "document_only"),
            "libraries.json export status",
            "Library inventory incomplete.");
        AddCapability(state, "SAFETY_BLOCK_EXPORT", false, state.SoftwareBlocks.Any(x => x.IsSafetyRelated && x.ExportSuccess) ? true : env.Step7SafetySoftwareInstalled,
            "Safety assemblies/products and block export status",
            "Safety software inventory incomplete.");
    }

    private static void BuildAssetAndFirmwareInventory(ExportState state)
    {
        state.AssetInventory.Clear();
        state.FirmwareInventory.Clear();

        foreach (var module in state.HardwareModules)
        {
            var text = $"{module.Name} {module.TypeIdentifier} {module.OrderNumber}";
            var networkInterfaces = state.NetworkInterfaces.Where(x => x.Path.StartsWith(module.Path, StringComparison.OrdinalIgnoreCase) || module.Path.StartsWith(x.Path, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.InterfaceName ?? x.ModuleName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var macs = state.NetworkInterfaces.Where(x => x.Path.StartsWith(module.Path, StringComparison.OrdinalIgnoreCase) || module.Path.StartsWith(x.Path, StringComparison.OrdinalIgnoreCase))
                .SelectMany(x => x.MacAddresses).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (!string.IsNullOrWhiteSpace(module.MacAddress) && !macs.Contains(module.MacAddress!, StringComparer.OrdinalIgnoreCase))
            {
                macs.Add(module.MacAddress!);
            }
            var item = new AssetInventoryItem
            {
                AssetId = StableHash($"{state.ProjectName}|{module.Path}|{module.OrderNumber}|{module.TypeIdentifier}"),
                ProjectName = state.ProjectName,
                DeviceName = module.DeviceName,
                DeviceItemName = module.Name,
                AssetType = GuessAssetType(text),
                ProductFamily = GuessProductFamily(text),
                OrderNumber = module.OrderNumber,
                NormalizedOrderNumber = NormalizeOrderNumber(module.OrderNumber),
                FirmwareVersion = module.FirmwareVersion,
                TypeIdentifier = module.TypeIdentifier,
                Path = module.Path,
                ParentPath = ParentPath(module.Path),
                NetworkInterfaces = networkInterfaces,
                MacAddresses = macs,
                SerialNumber = module.SerialNumber,
                IsController = state.Controllers.Any(x => string.Equals(x.SourcePath, module.Path, StringComparison.OrdinalIgnoreCase)),
                IsDrive = IsDrive(text),
                IsHmi = IsHmi(text),
                IsNetworkInterface = networkInterfaces.Count > 0,
                IsSafetyRelated = IsSafetyText(text)
            };
            state.AssetInventory.Add(item);

            if (!string.IsNullOrWhiteSpace(module.FirmwareVersion) || !string.IsNullOrWhiteSpace(module.OrderNumber))
            {
                var normalizedOrder = NormalizeOrderNumber(module.OrderNumber);
                var lookup = string.Join(" ", new[] { normalizedOrder, module.FirmwareVersion }.Where(x => !string.IsNullOrWhiteSpace(x)));
                state.FirmwareInventory.Add(new FirmwareInventoryItem
                {
                    Product = module.Name,
                    OrderNumber = module.OrderNumber,
                    FirmwareVersion = module.FirmwareVersion,
                    SourcePath = module.Path,
                    VulnerabilityLookupKey = lookup
                });
            }
        }
    }

    private static void BuildSoftwareInventory(ExportState state)
    {
        state.SoftwareInventory.Clear();
        foreach (var block in state.SoftwareBlocks)
        {
            var text = $"{block.BlockName} {block.GroupPath} {block.BlockType}";
            var evidence = new List<string>();
            if (!string.IsNullOrWhiteSpace(block.ExportFile)) evidence.Add(block.ExportFile!);
            if (!string.IsNullOrWhiteSpace(block.DocumentFile)) evidence.Add(block.DocumentFile!);

            state.SoftwareInventory.Add(new SoftwareInventoryItem
            {
                ComponentId = StableHash($"{state.ProjectName}|{block.PlcName}|{block.GroupPath}|{block.BlockName}|{block.BlockNumber}"),
                PlcName = block.PlcName,
                ComponentName = block.BlockName,
                ComponentType = block.BlockType,
                TiaBlockType = block.BlockType,
                Language = block.Language,
                BlockNumber = block.BlockNumber,
                GroupPath = block.GroupPath,
                IsSafetyRelated = block.IsSafetyRelated || Guess(text, "Safety", "F_", "F-LAD", "NotHalt", "Schutz", "Emergency"),
                IsNetworkRelatedGuess = Guess(text, "OPC", "TCP", "UDP", "PN", "Web", "UA"),
                IsHmiRelatedGuess = Guess(text, "HMI", "Visu"),
                IsRecipeRelatedGuess = Guess(text, "Recipe", "Rezept", "Daten", "Param"),
                IsDiagnosticsRelatedGuess = Guess(text, "Diag", "Error", "Failure", "Stoerung", "Störung"),
                IsStandardLibraryGuess = Guess(text, "Library", "Lib", "LGF", "LCom", "Siemens"),
                ExportFile = block.ExportFile,
                DocumentFile = block.DocumentFile,
                HashSha256 = block.HashSha256,
                ExportStatus = block.EvidenceStatus == "full_xml" ? "full_xml"
                    : block.EvidenceStatus == "document_only" ? "document_only"
                    : block.EvidenceStatus == "license_unavailable" ? "license_unavailable"
                    : block.EvidenceStatus == "skipped_license_missing" ? "license_unavailable"
                    : block.ExportSuccess ? "metadata_only" : "failed",
                MissingLicense = block.MissingLicense,
                CraMetadata = block.CraMetadata,
                EvidenceFiles = evidence,
                RiskRelevanceGuess = Guess(text, "Safety", "OPC", "TCP", "UDP", "HMI", "Recipe", "Rezept", "Diag") ? "elevated_review" : "standard_review",
                ReviewRequired = !block.ExportSuccess || Guess(text, "Safety", "OPC", "TCP", "UDP", "HMI", "Recipe", "Rezept", "Diag")
            });
        }
    }

    private static void BuildLibraryInventory(ExportState state)
    {
        state.LibraryInventory.Clear();
        foreach (var library in state.Libraries)
        {
            state.LibraryInventory.Add(new LibraryInventoryItem
            {
                Name = library.Name,
                Version = library.Version,
                Kind = library.Kind,
                Author = library.Author,
                Comment = library.Comment,
                LastModified = library.LastModified,
                Path = library.Path,
                ExportStatus = library.ExportStatus,
                ExportFile = library.ExportFile,
                DocumentFile = library.DocumentFile,
                ExportErrorType = library.ExportErrorType,
                ExportErrorMessage = library.ExportErrorMessage
            });
        }

        state.LibraryDependencyGaps.Clear();
        foreach (var block in state.SoftwareBlocks)
        {
            var match = state.Libraries.FirstOrDefault(x =>
                (!string.IsNullOrWhiteSpace(x.Name) && block.BlockName.IndexOf(x.Name, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrWhiteSpace(block.GroupPath) && block.GroupPath.IndexOf(x.Name, StringComparison.OrdinalIgnoreCase) >= 0));
            if (match == null)
            {
                continue;
            }

            state.LibraryDependencyGaps.Add(new LibraryDependencyGap
            {
                BlockName = block.BlockName,
                SuspectedLibrarySource = match.Name,
                Confidence = block.BlockName.IndexOf(match.Name, StringComparison.OrdinalIgnoreCase) >= 0 ? "medium" : "low",
                Basis = block.BlockName.IndexOf(match.Name, StringComparison.OrdinalIgnoreCase) >= 0 ? "name match" : "group path"
            });
        }
    }

    private static void BuildSecurityConfiguration(ExportState state)
    {
        state.SecurityConfiguration = new SecurityConfiguration();
        state.SecurityFindings.Clear();
        var interesting = new[]
        {
            "webserver", "put_get", "putget", "access", "opc", "ua", "snmp", "syslog", "ntp", "time", "router",
            "forward", "protection", "security", "failsafe", "f_", "mrp", "lldp", "monitor", "port", "community"
        };

        foreach (var snapshot in state.HardwareAttributes)
        {
            var filtered = snapshot.Attributes
                .Where(x => interesting.Any(i => x.Key.IndexOf(i, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
            if (filtered.Count == 0)
            {
                continue;
            }

            filtered["object_path"] = snapshot.ObjectPath;
            filtered["object_type"] = snapshot.ObjectType;
            if (state.Controllers.Any(x => snapshot.ObjectPath.StartsWith(x.SourcePath, StringComparison.OrdinalIgnoreCase)))
            {
                state.SecurityConfiguration.Cpu.Add(filtered);
            }
            else
            {
                state.SecurityConfiguration.Network.Add(filtered);
            }
        }

        foreach (var entry in state.SecurityConfiguration.Cpu.Concat(state.SecurityConfiguration.Network))
        {
            var objectPath = entry.TryGetValue("object_path", out var path) ? path ?? "" : "";
            foreach (var pair in entry)
            {
                var key = pair.Key;
                var value = pair.Value ?? "";
                if (key.IndexOf("snmp", StringComparison.OrdinalIgnoreCase) >= 0 && IsTrue(value))
                {
                    AddFinding(state, "MEDIUM", "SNMP active", $"{objectPath}: {key}={value}", "Review SNMP requirement and community configuration.");
                }
                if (key.IndexOf("community", StringComparison.OrdinalIgnoreCase) >= 0 && value.Equals("public", StringComparison.OrdinalIgnoreCase))
                {
                    AddFinding(state, "HIGH", "SNMP default community public detected", $"{objectPath}: {key}=public", "Change SNMP community or disable SNMP if not required.");
                }
                if (key.IndexOf("put", StringComparison.OrdinalIgnoreCase) >= 0 && key.IndexOf("get", StringComparison.OrdinalIgnoreCase) >= 0 && IsTrue(value))
                {
                    AddFinding(state, "HIGH", "PUT/GET communication enabled", $"{objectPath}: {key}={value}", "Review PLC access policy and disable PUT/GET if not required.");
                }
                if (key.IndexOf("webserver", StringComparison.OrdinalIgnoreCase) >= 0 && IsTrue(value))
                {
                    AddFinding(state, "LOW", "Webserver enabled", $"{objectPath}: {key}={value}", "Review webserver hardening, users, TLS and logging.");
                }
                if (key.IndexOf("router", StringComparison.OrdinalIgnoreCase) >= 0 && IsTrue(value))
                {
                    AddFinding(state, "MEDIUM", "Routing/IP forwarding enabled", $"{objectPath}: {key}={value}", "Verify routing is intended and documented in network zoning.");
                }
            }
        }

        if (!ContainsKeyLike(state.SecurityConfiguration.Cpu, "syslog"))
        {
            AddFinding(state, "MEDIUM", "No syslog setting found", "No syslog-related CPU attribute was exported.", "Verify logging configuration manually.");
        }
        if (!ContainsKeyLike(state.SecurityConfiguration.Cpu, "ntp") && !ContainsKeyLike(state.SecurityConfiguration.Cpu, "time"))
        {
            AddFinding(state, "LOW", "No NTP/time synchronization setting found", "No time-sync CPU attribute was exported.", "Verify time synchronization manually.");
        }
    }

    private static void BuildSafetyInventory(ExportState state)
    {
        var fHardware = state.HardwareAttributes
            .Where(x => IsSafetyText($"{x.ObjectPath} {x.ObjectType} {string.Join(" ", x.Attributes.Keys)} {string.Join(" ", x.Attributes.Values)}"))
            .ToList();
        var fBlocks = state.SoftwareInventory.Where(x => x.IsSafetyRelated).ToList();
        state.SafetyInventory = new SafetyInventory
        {
            SafetyPresent = fHardware.Count > 0 || fBlocks.Count > 0,
            FCpu = state.AssetInventory.Any(x => x.IsController && x.IsSafetyRelated),
            FCapabilityActivated = fHardware.Count > 0 ? true : null,
            FBlocks = fBlocks,
            FParametersFromHardwareAttributes = fHardware
        };

        foreach (var block in fBlocks)
        {
            state.SafetyInventory.FBlockExportStatus[block.ComponentName] = block.ExportStatus;
        }

        if (state.SafetyInventory.SafetyPresent && fBlocks.Any(x => x.ExportStatus is "failed" or "metadata_only"))
        {
            state.SafetyInventory.Limitations.Add("Some safety blocks are not fully exported. Manual safety review is required.");
            state.RequiredManualActions.Add("Export requires TIA Safety/STEP7 Safety module or unlocked safety project access; verify manually.");
        }
    }

    private static void BuildHmiAndDriveInventory(ExportState state)
    {
        state.HmiInventory.Clear();
        state.DriveInventory.Clear();
        foreach (var asset in state.AssetInventory.Where(x => x.IsHmi && state.Settings.IncludeHmi))
        {
            state.HmiInventory.Add(new HmiInventoryItem
            {
                Name = asset.DeviceItemName,
                Path = asset.Path,
                NetworkInterfaces = asset.NetworkInterfaces,
                RequiredAction = "Install/enable WinCC Openness module or run exporter on engineering station with WinCC support.",
                Limitations = ["Detailed HMI connections, tags and screens are best-effort and may require WinCC Openness support."]
            });
        }

        foreach (var asset in state.AssetInventory.Where(x => x.IsDrive && state.Settings.IncludeDrives))
        {
            var network = state.NetworkInterfaces.FirstOrDefault(x => asset.Path.StartsWith(x.Path, StringComparison.OrdinalIgnoreCase) || x.Path.StartsWith(asset.Path, StringComparison.OrdinalIgnoreCase));
            var drive = new DriveInventoryItem
            {
                Name = asset.DeviceItemName,
                OrderNumber = asset.OrderNumber,
                FirmwareVersion = asset.FirmwareVersion,
                TypeName = asset.TypeIdentifier,
                NetworkInterface = network?.InterfaceName,
                IpAddress = network?.IpAddress,
                ProfinetDeviceName = network?.NodeNames.FirstOrDefault(),
                IoSystem = network?.IoSystemNames.FirstOrDefault(),
                Limitations = ["SINAMICS/Startdrive parameter export depends on installed Startdrive Openness support."]
            };

            var driveAttrs = state.HardwareAttributes.Where(x => x.ObjectPath.StartsWith(asset.Path, StringComparison.OrdinalIgnoreCase) || asset.Path.StartsWith(x.ObjectPath, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var snapshot in driveAttrs)
            {
                foreach (var pair in snapshot.Attributes)
                {
                    if (IsDriveParameterKey(pair.Key))
                    {
                        drive.KnownParameters[pair.Key] = pair.Value;
                    }
                    if (pair.Key.IndexOf("Telegram", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        drive.TelegramParameterInfo ??= pair.Value;
                    }
                    if (pair.Key.IndexOf("ParameterSet", StringComparison.OrdinalIgnoreCase) >= 0 && !string.IsNullOrWhiteSpace(pair.Value))
                    {
                        if (!drive.ParameterSetNames.Contains(pair.Value!)) drive.ParameterSetNames.Add(pair.Value!);
                    }
                }
            }

            if (drive.ParameterSetNames.Count > 0)
            {
                drive.ParameterSetCount = drive.ParameterSetNames.Count;
            }
            if (drive.KnownParameters.Count > 0 || drive.ParameterSetNames.Count > 0)
            {
                drive.ExportStatus = "metadata_with_parameters";
            }

            state.DriveInventory.Add(drive);
        }
    }

    private static bool IsDriveParameterKey(string key)
    {
        return key.StartsWith("p", StringComparison.OrdinalIgnoreCase) && key.Length > 1 && char.IsDigit(key[1])
               || key.StartsWith("r", StringComparison.OrdinalIgnoreCase) && key.Length > 1 && char.IsDigit(key[1])
               || key.IndexOf("Telegram", StringComparison.OrdinalIgnoreCase) >= 0
               || key.IndexOf("MotorType", StringComparison.OrdinalIgnoreCase) >= 0
               || key.IndexOf("RatedCurrent", StringComparison.OrdinalIgnoreCase) >= 0
               || key.IndexOf("RatedPower", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void BuildGapAnalysis(ExportState state)
    {
        state.CraGapAnalysis.Clear();
        AddGap(state, "Asset inventory", state.AssetInventory.Count > 0 ? "COMPLETE" : "MISSING", $"{state.AssetInventory.Count} assets detected", [], "Missing assets reduce CMDB coverage.", "Run hardware export on engineering station.", state.AssetInventory.Count > 0 ? "INFO" : "HIGH");
        AddGap(state, "Firmware inventory", state.FirmwareInventory.Count > 0 ? "PARTIAL" : "MISSING", $"{state.FirmwareInventory.Count} firmware/order entries detected", state.FirmwareInventory.Count > 0 ? [] : ["firmware_version", "order_number"], "Vulnerability lookup readiness is reduced.", "Verify MLFB and firmware manually for modules with missing values.", state.FirmwareInventory.Count > 0 ? "LOW" : "HIGH");
        AddGap(state, "Network inventory", state.NetworkInterfaces.Count > 0 ? "PARTIAL" : "MISSING", $"{state.NetworkInterfaces.Count} network interfaces, {state.NetworkLinks.Count} links", [], "Network zoning/import coverage may be incomplete.", "Review network_interfaces.json and topology manually.", state.NetworkInterfaces.Count > 0 ? "LOW" : "MEDIUM");
        var fullBlocks = state.SoftwareInventory.Count(x => x.ExportStatus == "full_xml");
        AddGap(state, "Software block inventory", fullBlocks == state.SoftwareInventory.Count && fullBlocks > 0 ? "COMPLETE" : state.SoftwareInventory.Count > 0 ? "PARTIAL" : "MISSING", $"{state.SoftwareInventory.Count} blocks detected, {fullBlocks} full XML exports", fullBlocks > 0 ? [] : ["full_xml_export"], "Software components cannot be fully analyzed or hashed.", "Fix block XML export or compile/unprotect project.", fullBlocks > 0 ? "MEDIUM" : "HIGH");
        var libraryFull = state.LibraryInventory.Count(x => x.ExportStatus == "full_xml" || x.ExportStatus == "document_only");
        AddGap(state, "Library inventory", libraryFull > 0 ? "PARTIAL" : state.LibraryInventory.Count > 0 ? "PARTIAL" : "MISSING", $"{state.LibraryInventory.Count} library items, {libraryFull} exported", libraryFull > 0 ? [] : ["library_exports"], "Library provenance/dependency evidence is incomplete.", "Review library_export_failures.json and export libraries manually if required.", libraryFull > 0 ? "MEDIUM" : "HIGH");
        AddGap(state, "HMI inventory", state.HmiInventory.Count > 0 ? "PARTIAL" : "MISSING", $"{state.HmiInventory.Count} HMI assets detected", ["hmi_tags", "hmi_screens"], "HMI attack surface may be underrepresented.", "Install/enable WinCC Openness module or verify manually.", state.HmiInventory.Count > 0 ? "MEDIUM" : "HIGH");
        AddGap(state, "Drive inventory", state.DriveInventory.Count > 0 ? "PARTIAL" : "MISSING", $"{state.DriveInventory.Count} drive assets detected", ["drive_parameters"], "Drive firmware/parameter review may be incomplete.", "Install/enable Startdrive support or verify manually.", state.DriveInventory.Count > 0 ? "MEDIUM" : "LOW");
        AddGap(state, "Safety inventory", state.SafetyInventory.SafetyPresent ? (state.SafetyInventory.FBlocks.All(x => x.ExportStatus == "full_xml") ? "COMPLETE" : "PARTIAL") : "NOT_APPLICABLE", $"{state.SafetyInventory.FBlocks.Count} safety blocks detected", state.SafetyInventory.SafetyPresent ? ["manual_safety_review"] : [], "Safety program evidence may be incomplete.", "Verify safety project access and exported F-blocks.", state.SafetyInventory.SafetyPresent ? "HIGH" : "INFO");
        AddGap(state, "Security configuration", state.SecurityConfiguration.Cpu.Count + state.SecurityConfiguration.Network.Count > 0 ? "PARTIAL" : "MISSING", $"{state.SecurityFindings.Count} review findings", [], "Security posture cannot be fully reviewed from export.", "Review normalized/security_configuration.json and findings.", state.SecurityConfiguration.Cpu.Count > 0 ? "MEDIUM" : "HIGH");
        AddGap(state, "Evidence / hashes", state.EvidenceFiles.Count > 0 ? "PARTIAL" : "MISSING", $"{state.EvidenceFiles.Count} evidence files hashed", [], "Audit evidence chain is incomplete.", "Ensure all generated files are listed in evidence_files.json.", state.EvidenceFiles.Count > 0 ? "LOW" : "HIGH");
        AddGap(state, "Vulnerability lookup readiness", state.FirmwareInventory.Any(x => !string.IsNullOrWhiteSpace(x.VulnerabilityLookupKey)) ? "PARTIAL" : "MISSING", $"{state.FirmwareInventory.Count(x => !string.IsNullOrWhiteSpace(x.VulnerabilityLookupKey))} lookup keys", [], "Automated vulnerability matching may miss assets.", "Complete order number and firmware fields.", "MEDIUM");
        AddGap(state, "SBOM readiness", fullBlocks > 0 || libraryFull > 0 ? "PARTIAL" : "MISSING", $"{fullBlocks} block XML exports, {libraryFull} library exports", ["dependency_chain"], "TIA does not expose complete npm/pip-style dependencies.", "Use library_dependency_gaps.json for manual review.", "MEDIUM");
        AddGap(state, "Manual review required", state.CraGapAnalysis.Any(x => x.Severity == "HIGH") ? "PARTIAL" : "COMPLETE", $"{state.CraGapAnalysis.Count(x => x.Severity == "HIGH")} HIGH gaps", [], "Manual engineering review remains required.", "Resolve HIGH gaps or document accepted limitations.", state.CraGapAnalysis.Any(x => x.Severity == "HIGH") ? "HIGH" : "INFO");
    }

    private static void BuildQualityScore(ExportState state)
    {
        var max = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Asset inventory"] = 15,
            ["Firmware inventory"] = 10,
            ["Network inventory"] = 10,
            ["Software inventory"] = 20,
            ["Library inventory"] = 10,
            ["Security configuration"] = 10,
            ["Safety inventory"] = 10,
            ["Evidence/hashing"] = 10,
            ["HMI/Drive coverage"] = 5
        };
        var scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Asset inventory"] = state.AssetInventory.Count > 0 ? 15 : 0,
            ["Firmware inventory"] = state.FirmwareInventory.Count > 0 ? 7 : 0,
            ["Network inventory"] = state.NetworkInterfaces.Count > 0 ? 8 : 0,
            ["Software inventory"] = ScoreSoftware(state),
            ["Library inventory"] = ScoreLibraries(state),
            ["Security configuration"] = state.SecurityConfiguration.Cpu.Count + state.SecurityConfiguration.Network.Count > 0 ? 7 : 0,
            ["Safety inventory"] = !state.SafetyInventory.SafetyPresent ? 10 : state.SafetyInventory.FBlocks.Any(x => x.ExportStatus == "full_xml") ? 7 : 4,
            ["Evidence/hashing"] = state.EvidenceFiles.Count > 10 ? 10 : state.EvidenceFiles.Count > 0 ? 5 : 0,
            ["HMI/Drive coverage"] = (state.HmiInventory.Count > 0 || state.DriveInventory.Count > 0) ? 3 : 1
        };
        state.ExportQualityScore = new ExportQualityScore
        {
            Score = scores.Values.Sum(),
            CategoryScores = scores,
            CategoryMaximums = max,
            Rationale = state.CraGapAnalysis.Where(x => x.Severity is "HIGH" or "MEDIUM").Select(x => $"{x.Category}: {x.Status} - {x.Impact}").ToList()
        };
    }

    private static int ScoreSoftware(ExportState state)
    {
        if (state.SoftwareInventory.Count == 0) return 0;
        if (state.SoftwareInventory.All(x => x.ExportStatus is "metadata_only" or "failed")) return 8;
        var fullRatio = state.SoftwareInventory.Count(x => x.ExportStatus == "full_xml") / (double)state.SoftwareInventory.Count;
        return Math.Min(20, 8 + (int)Math.Round(fullRatio * 12));
    }

    private static int ScoreLibraries(ExportState state)
    {
        if (state.LibraryInventory.Count == 0) return 0;
        if (state.LibraryInventory.All(x => x.ExportStatus == "metadata_only")) return 4;
        return state.LibraryInventory.Any(x => x.ExportStatus == "full_xml") ? 8 : 6;
    }

    private static void BuildManualActionsAndLimitations(ExportState state)
    {
        state.Limitations.Add("CRA readiness is an engineering-data quality assessment; it is not a legal compliance certification.");
        state.Limitations.Add("TIA Openness coverage depends on installed TIA modules, project licenses, protection state and object model availability.");

        if (state.LicenseBlockingFailureDetected)
        {
            var licenseName = state.MissingLicenseName ?? "STEP 7 Professional";
            state.Limitations.Add(
                $"PLC block XML export was blocked because Openness reported license '{licenseName}' as not usable. " +
                $"{state.LicenseBlockingAffectedBlockCount} block(s) could not be exported. " +
                "Software inventory is metadata-only for affected blocks; SHA-256 hashes and full SBOM readiness require re-export when Openness can use the license.");
            var action = $"Verify Automation License Manager (ALM) makes a TIA Portal '{licenseName}' license available to the same Windows user/session used by the exporter. " +
                "Check floating-license occupancy, license server connection, version match, and stale TIA Portal processes; then re-run the exporter.";
            if (!state.RequiredManualActions.Contains(action))
            {
                state.RequiredManualActions.Add(action);
            }
        }

        foreach (var gap in state.CraGapAnalysis.Where(x => x.Severity == "HIGH"))
        {
            if (!state.RequiredManualActions.Contains(gap.RequiredAction))
            {
                state.RequiredManualActions.Add(gap.RequiredAction);
            }
        }
    }

    private static void AddCapability(ExportState state, string capability, bool required, bool? available, string evidence, string impact, string? description = null)
    {
        state.CapabilityMatrix.Add(new CapabilityEntry { Capability = capability, Required = required, Available = available, Evidence = evidence, ImpactIfMissing = impact, Description = description });
    }

    private static void AddGap(ExportState state, string category, string status, string evidence, List<string> missing, string impact, string requiredAction, string severity)
    {
        state.CraGapAnalysis.Add(new CraGapAnalysisItem { Category = category, Status = status, Evidence = evidence, MissingFields = missing, Impact = impact, RequiredAction = requiredAction, Severity = severity });
    }

    private static void AddFinding(ExportState state, string severity, string finding, string evidence, string action)
    {
        state.SecurityFindings.Add(new SecurityFinding { Severity = severity, Finding = finding, Evidence = evidence, RequiredAction = action });
    }

    private static List<string> DetectTiaProducts()
    {
        var result = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in new[] { Registry.LocalMachine, Registry.CurrentUser })
        {
            foreach (var path in new[] { @"SOFTWARE\Siemens\Automation", @"SOFTWARE\WOW6432Node\Siemens\Automation" })
            {
                using var key = Try(() => root.OpenSubKey(path));
                if (key == null) continue;
                foreach (var name in key.GetSubKeyNames())
                {
                    result.Add(name);
                    using var child = Try(() => key.OpenSubKey(name));
                    if (child == null) continue;
                    foreach (var sub in child.GetSubKeyNames()) result.Add($"{name}/{sub}");
                }
            }
        }
        return result.ToList();
    }

    private static bool? TryCheckOpennessGroup()
    {
        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return identity.Groups?.Select(x => Try(() => x.Translate(typeof(System.Security.Principal.NTAccount)).Value))
                .Where(x => x != null)
                .Any(x => x!.IndexOf("Openness", StringComparison.OrdinalIgnoreCase) >= 0 || x.IndexOf("Siemens TIA", StringComparison.OrdinalIgnoreCase) >= 0);
        }
        catch
        {
            return null;
        }
    }

    private static T? Try<T>(Func<T> action)
    {
        try { return action(); }
        catch { return default; }
    }

    private static bool ContainsAny(IEnumerable<string> values, params string[] needles) => values.Any(x => needles.Any(n => x.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0));
    private static bool ContainsKeyLike(IEnumerable<Dictionary<string, string?>> values, string needle) => values.Any(x => x.Keys.Any(k => k.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0));
    private static bool Guess(string text, params string[] needles) => needles.Any(x => text.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0);
    private static bool IsTrue(string value) => value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("1", StringComparison.OrdinalIgnoreCase) || value.Equals("yes", StringComparison.OrdinalIgnoreCase) || value.Equals("enabled", StringComparison.OrdinalIgnoreCase) || value.Equals("active", StringComparison.OrdinalIgnoreCase);
    private static bool IsDrive(string text) => Guess(text, "SINAMICS", "G120", "S120", "Drive", "Antrieb", "6SL");
    // HMI IE_CP modules have TypeIdentifier containing "HMI IE" — exclude them from the HMI heuristic.
    private static bool IsHmi(string text) =>
        Guess(text, "HMI", "WinCC", "Unified", "Panel") &&
        !Guess(text, "HMI IE", "IE_CP", "IECP");
    // HMI_RT_N is the WinCC Runtime software container inside an HMI device — distinct from the hardware panel.
    private static bool IsHmiRuntime(string text) => Guess(text, "HMI_RT", "WinCC RT", "WinCC_RT", "HMI RT");
    private static bool IsSafetyText(string text) => Guess(text, "Safety", "Failsafe", "F-CPU", "F_CPU", "F-LAD", "F_", "NotHalt", "Emergency", "Schutz");
    private static string GuessAssetType(string text) =>
        IsDrive(text) ? "drive" :
        IsHmiRuntime(text) ? "hmi_runtime" :
        IsHmi(text) ? "hmi" :
        Guess(text, "CPU", "PLC") ? "controller" :
        Guess(text, "PN", "PROFINET", "Interface", "Port") ? "network_device" :
        Guess(text, "CP 1", "CP 3", "CM 1", "IE Switch", "SCALANCE", " Switch", "HMI IE") ? "communication_module" :
        Guess(text, " PS ", " PM ", "Power Supply") ? "power_supply" :
        Guess(text, "DI ", "DO ", "AI ", "AO ", "SM 1", "SM 3", " IM ", " BM ") ? "io_module" :
        "unknown";
    private static string? GuessProductFamily(string text) => Guess(text, "S7-1500", "151", "CPU") ? "SIMATIC S7" : IsDrive(text) ? "SINAMICS" : IsHmi(text) ? "SIMATIC HMI" : null;
    private static string? NormalizeOrderNumber(string? orderNumber) => string.IsNullOrWhiteSpace(orderNumber) ? null : new string(orderNumber.Where(ch => !char.IsWhiteSpace(ch)).ToArray()).ToUpperInvariant();
    private static string? ParentPath(string path) => path.Contains("/") ? path.Substring(0, path.LastIndexOf("/", StringComparison.Ordinal)) : null;

    private static string StableHash(string value)
    {
        using var sha = SHA256.Create();
        return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(value)).Take(16).Select(x => x.ToString("x2")));
    }
}
