using TIAExporter.Normalization;

namespace TIAExporter.Tia;

internal sealed class ProjectScanner
{
    public static string GetProjectName(object project, string fallbackPath)
    {
        return TiaReflection.GetString(project, "Name") ?? Path.GetFileNameWithoutExtension(fallbackPath);
    }

    public static void MarkWarning(ExportState state, string warning)
    {
        state.Warnings.Add(warning);
    }
}
