using Siemens.Engineering;
using TIAExporter.Logging;

namespace TIAExporter.Tia;

internal sealed class TiaPortalService : IDisposable
{
    private readonly ExportLogger logger;
    private TiaPortal? tiaPortal;

    public TiaPortalService(ExportLogger logger)
    {
        this.logger = logger;
    }

    public TiaPortal Portal => tiaPortal ?? throw new InvalidOperationException("TIA Portal is not started.");

    public Project OpenProject(string projectPath, bool headless)
    {
        var attached = TryAttachToOpenProject(projectPath);
        if (attached != null)
        {
            return attached;
        }

        attached = TryAttachToAnyPortalAndOpen(projectPath);
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
            attached = TryAttachToAnyPortalAndOpen(projectPath);
            if (attached != null)
            {
                return attached;
            }

            throw;
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

    private Project? TryAttachToAnyPortalAndOpen(string projectPath)
    {
        foreach (var process in GetTiaProcesses())
        {
            try
            {
                logger.Info($"Trying to attach to running TIA Portal process {process.Id}.");
                tiaPortal = process.Attach();
                foreach (var project in tiaPortal.Projects)
                {
                    var path = TiaReflection.GetString(project, "Path");
                    if (path != null && PathsEqual(path, projectPath))
                    {
                        logger.Info($"Project is already open in attached TIA Portal process {process.Id}.");
                        return project;
                    }
                }

                logger.Info($"Opening project in attached TIA Portal process {process.Id}: {projectPath}");
                return tiaPortal.Projects.Open(new FileInfo(projectPath));
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

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }
}
