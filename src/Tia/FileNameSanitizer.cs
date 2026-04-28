namespace TIAExporter.Tia;

internal static class FileNameSanitizer
{
    private static readonly HashSet<char> InvalidChars = Path.GetInvalidFileNameChars().ToHashSet();

    public static string Sanitize(string? value)
    {
        var text = string.IsNullOrWhiteSpace(value) ? "unnamed" : value!.Trim();
        var chars = text.Select(ch => InvalidChars.Contains(ch) ? '_' : ch).ToArray();
        var sanitized = new string(chars).Trim('.', ' ');
        return string.IsNullOrWhiteSpace(sanitized) ? "unnamed" : sanitized;
    }

    public static string UniquePath(string directory, string fileName)
    {
        Directory.CreateDirectory(directory);
        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var path = Path.Combine(directory, fileName);
        var index = 2;
        while (File.Exists(path))
        {
            path = Path.Combine(directory, $"{name}_{index++}{extension}");
        }

        return path;
    }
}
