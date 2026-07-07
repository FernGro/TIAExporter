using Siemens.Engineering;
using TIAExporter.Logging;

namespace TIAExporter.Tia;

internal sealed class TiaPortalService : IDisposable
{
    private readonly ExportLogger logger;
    private readonly List<string> archiveRetrieveDirectories = [];
    private TiaPortal? tiaPortal;

    public TiaPortalService(ExportLogger logger)
    {
        this.logger = logger;
    }

    public TiaPortal Portal => tiaPortal ?? throw new InvalidOperationException("TIA Portal is not started.");

    public Project OpenProject(string projectPath, bool headless, string archiveRetrieveRoot)
    {
        var isArchive = TiaProjectFileTypes.IsArchive(projectPath);
        if (!isArchive)
        {
            var attachedProject = TryAttachToOpenProject(projectPath);
            if (attachedProject != null)
            {
                return attachedProject;
            }
        }

        var attached = TryAttachToAnyPortalAndOpen(projectPath, archiveRetrieveRoot);
        if (attached != null)
        {
            return attached;
        }

        var mode = headless ? TiaPortalMode.WithoutUserInterface : TiaPortalMode.WithUserInterface;
        try
        {
            StartPortal(mode);
        }
        catch (Exception ex)
        {
            logger.Warn($"Starting TIA Portal failed: {ex.GetType().Name}: {ex.Message}");
            attached = TryAttachToAnyPortalAndOpen(projectPath, archiveRetrieveRoot);
            if (attached != null)
            {
                return attached;
            }

            throw;
        }

        return OpenOrRetrieveProject(projectPath, archiveRetrieveRoot);
    }

    private Project OpenOrRetrieveProject(string projectPath, string archiveRetrieveRoot)
    {
        if (TiaProjectFileTypes.IsArchive(projectPath))
        {
            return RetrieveArchivedProject(projectPath, archiveRetrieveRoot);
        }

        logger.Info($"Opening project: {projectPath}");
        try
        {
            return Portal.Projects.Open(new FileInfo(projectPath));
        }
        catch (Exception ex) when (IsAlreadyOpenError(ex))
        {
            logger.Warn("Project appears to be open already. Trying to attach to the running TIA Portal process.");
            tiaPortal?.Dispose();
            tiaPortal = null;
            return AttachToOpenProject(projectPath);
        }
    }

    public string GetVersion()
    {
        return typeof(TiaPortal).Assembly.GetName().Version?.ToString() ?? "unknown";
    }

    public void Dispose()
    {
        try
        {
            tiaPortal?.Dispose();
        }
        catch (Exception ex)
        {
            logger.Warn($"Failed to dispose TIA Portal cleanly: {ex.Message}");
        }

        CleanupArchiveRetrieveDirectories();
    }

    private Project AttachToOpenProject(string projectPath)
    {
        var project = TryAttachToOpenProject(projectPath);
        if (project != null)
        {
            return project;
        }

        throw new InvalidOperationException("The project is reported as already open, but no attachable TIA Portal process with this project path was found.");
    }

    private Project? TryAttachToOpenProject(string projectPath)
    {
        var fullPath = Path.GetFullPath(projectPath);
        foreach (var process in GetTiaProcesses())
        {
            var processProjectPath = process.ProjectPath?.FullName;
            if (string.IsNullOrWhiteSpace(processProjectPath))
            {
                continue;
            }

            if (!PathsEqual(processProjectPath!, fullPath))
            {
                continue;
            }

            logger.Info($"Attaching to TIA Portal process {process.Id}.");
            tiaPortal = process.Attach();
            foreach (var project in tiaPortal.Projects)
            {
                var path = TiaReflection.GetString(project, "Path");
                if (path == null || PathsEqual(path, fullPath))
                {
                    return project;
                }
            }
        }

        return null;
    }

    private Project? TryAttachToAnyPortalAndOpen(string projectPath, string archiveRetrieveRoot)
    {
        var isArchive = TiaProjectFileTypes.IsArchive(projectPath);
        foreach (var process in GetTiaProcesses())
        {
            try
            {
                logger.Info($"Trying to attach to running TIA Portal process {process.Id}.");
                tiaPortal = process.Attach();
                if (!isArchive)
                {
                    foreach (var project in tiaPortal.Projects)
                    {
                        var path = TiaReflection.GetString(project, "Path");
                        if (path != null && PathsEqual(path, projectPath))
                        {
                            logger.Info($"Project is already open in attached TIA Portal process {process.Id}.");
                            return project;
                        }
                    }
                }

                logger.Info($"{(isArchive ? "Retrieving archive" : "Opening project")} in attached TIA Portal process {process.Id}: {projectPath}");
                return OpenOrRetrieveProject(projectPath, archiveRetrieveRoot);
            }
            catch (Exception ex)
            {
                logger.Warn($"Attach/open via TIA Portal process {process.Id} failed: {ex.GetType().Name}: {ex.Message}");
                try
                {
                    tiaPortal?.Dispose();
                }
                catch
                {
                    // Continue with next process.
                }
                tiaPortal = null;
            }
        }

        return null;
    }

    private Project RetrieveArchivedProject(string archivePath, string archiveRetrieveRoot)
    {
        var archiveVersion = TiaProjectFileTypes.GetVersionFromExtension(archivePath);
        var tiaVersion = GetTiaMajorVersion();
        var retrieveWithUpgrade = archiveVersion.HasValue && tiaVersion.HasValue && archiveVersion.Value < tiaVersion.Value;
        var targetDirectory = CreateArchiveRetrieveDirectory(archiveRetrieveRoot, archivePath, retrieveWithUpgrade ? "upgrade" : null);

        logger.Info($"Retrieving archived project: {archivePath}");
        logger.Info($"Retrieve target: {targetDirectory.FullName}");
        if (retrieveWithUpgrade)
        {
            logger.Warn($"Archive version V{archiveVersion} is older than TIA Openness V{tiaVersion}. Retrieving with upgrade into a short temporary work folder.");
        }

        try
        {
            return retrieveWithUpgrade
                ? Portal.Projects.RetrieveWithUpgrade(new FileInfo(archivePath), targetDirectory)
                : Portal.Projects.Retrieve(new FileInfo(archivePath), targetDirectory);
        }
        catch (Exception ex) when (!retrieveWithUpgrade && IsUpgradeRequiredError(ex))
        {
            logger.Warn($"Archive retrieve requires an upgrade: {ex.GetType().Name}: {ex.Message}");
            var upgradeTarget = CreateArchiveRetrieveDirectory(archiveRetrieveRoot, archivePath, "upgrade_retry");
            logger.Info($"Retrying archive retrieve with upgrade. Retrieve target: {upgradeTarget.FullName}");
            return Portal.Projects.RetrieveWithUpgrade(new FileInfo(archivePath), upgradeTarget);
        }
    }

    private DirectoryInfo CreateArchiveRetrieveDirectory(string archiveRetrieveRoot, string archivePath, string? suffix)
    {
        Directory.CreateDirectory(archiveRetrieveRoot);
        var mode = string.IsNullOrWhiteSpace(suffix) ? "n" : FileNameSanitizer.Sanitize(suffix).Substring(0, 1).ToLowerInvariant();
        var folderName = $"r_{mode}_{Guid.NewGuid():N}".Substring(0, 14);
        var candidate = Path.Combine(archiveRetrieveRoot, folderName);
        if (Directory.Exists(candidate) && Directory.EnumerateFileSystemEntries(candidate).Any())
        {
            candidate = Path.Combine(archiveRetrieveRoot, $"r_{Guid.NewGuid():N}".Substring(0, 14));
        }

        Directory.CreateDirectory(candidate);
        archiveRetrieveDirectories.Add(candidate);
        return new DirectoryInfo(candidate);
    }

    private void CleanupArchiveRetrieveDirectories()
    {
        foreach (var directory in archiveRetrieveDirectories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
            catch (Exception ex)
            {
                logger.Warn($"Could not remove temporary archive retrieve folder '{directory}': {ex.Message}");
            }
        }
    }

    private void StartPortal(TiaPortalMode mode)
    {
        logger.Info($"Starting TIA Portal ({mode}).");
        tiaPortal = new TiaPortal(mode);
    }

    private IEnumerable<TiaPortalProcess> GetTiaProcesses()
    {
        try
        {
            return TiaPortal.GetProcesses().ToArray();
        }
        catch (Exception ex)
        {
            logger.Warn($"Could not enumerate running TIA Portal processes: {ex.GetType().Name}: {ex.Message}");
            return [];
        }
    }

    private static bool IsAlreadyOpenError(Exception ex)
    {
        var text = ex.ToString();
        return text.IndexOf("bereits", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("already", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsUpgradeRequiredError(Exception ex)
    {
        var text = ex.ToString();
        return text.IndexOf("upgrade", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("update", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("hochruest", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("hochrüst", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("aktualis", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static int? GetTiaMajorVersion()
    {
        var major = typeof(TiaPortal).Assembly.GetName().Version?.Major;
        return major > 0 ? major : null;
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }
}
