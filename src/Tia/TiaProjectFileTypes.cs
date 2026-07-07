namespace TIAExporter.Tia;

internal static class TiaProjectFileTypes
{
    private static readonly string[] ProjectExtensions = [".ap20", ".ap19", ".ap18", ".ap17", ".ap16", ".ap15"];
    private static readonly string[] ArchiveExtensions = [".zap20", ".zap19", ".zap18", ".zap17", ".zap16", ".zap15"];

    public const string ArchiveRetrieveFolderName = "_tia_archive_retrieve";
    public const string TempArchiveRetrieveFolderName = "TIAExp";
    public const string ProjectDialogFilter = "TIA Projekte und Archive (*.ap20;*.ap19;*.ap18;*.ap17;*.ap16;*.ap15;*.zap20;*.zap19;*.zap18;*.zap17;*.zap16;*.zap15)|*.ap20;*.ap19;*.ap18;*.ap17;*.ap16;*.ap15;*.zap20;*.zap19;*.zap18;*.zap17;*.zap16;*.zap15|Alle Dateien (*.*)|*.*";
    public const string HelpDescription = "TIA Portal project/archive file (*.ap20, *.ap19, *.ap18, *.ap17, *.ap16, *.ap15, *.zap20, *.zap19, *.zap18, *.zap17, *.zap16, *.zap15)";

    public static bool IsSupported(string path)
    {
        var extension = Path.GetExtension(path);
        return ProjectExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase) ||
               ArchiveExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsArchive(string path)
    {
        var extension = Path.GetExtension(path);
        return ArchiveExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    public static int? GetVersionFromExtension(string path)
    {
        var digits = new string(Path.GetExtension(path).Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var version) ? version : null;
    }

    public static string GetShortArchiveRetrieveRoot()
    {
        return Path.Combine(Path.GetTempPath(), TempArchiveRetrieveFolderName, "zap");
    }
}
