using System.Security.Cryptography;

namespace TIAExporter.Normalization;

internal sealed class EvidenceHasher
{
    public EvidenceFile HashFile(string exportRoot, string filePath, string sourceObject, bool exportSuccess = true)
    {
        return HashFile(exportRoot, filePath, sourceObject, exportSuccess, "", "raw_export", []);
    }

    public EvidenceFile HashFile(string exportRoot, string filePath, string sourceObject, bool exportSuccess, string sourceCategory, string evidenceLevel, List<string> usedForCraCategories)
    {
        using var stream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(stream);
        var info = new FileInfo(filePath);
        return new EvidenceFile
        {
            RelativePath = GetRelativePath(exportRoot, filePath).Replace('\\', '/'),
            FileType = info.Extension.TrimStart('.').ToLowerInvariant(),
            Sha256 = string.Concat(hash.Select(x => x.ToString("x2"))),
            SizeBytes = info.Length,
            CreatedAt = info.CreationTimeUtc,
            SourceObject = sourceObject,
            SourceCategory = sourceCategory,
            ExportSuccess = exportSuccess,
            EvidenceLevel = evidenceLevel,
            UsedForCraCategories = usedForCraCategories
        };
    }

    private static string GetRelativePath(string root, string path)
    {
        var rootUri = new Uri(AppendDirectorySeparator(Path.GetFullPath(root)));
        var pathUri = new Uri(Path.GetFullPath(path));
        return Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
    }

    private static string AppendDirectorySeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            ? path
            : path + Path.DirectorySeparatorChar;
    }
}
