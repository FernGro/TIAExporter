using System.Text.RegularExpressions;

namespace TIAExporter.Normalization;

internal static class CraMetadataParser
{
    private static readonly Regex TagRegex = new(@"@(?<key>module|version|library|owner|cra|safety|network|description)\s*:\s*(?<value>[^\r\n<]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static CraMetadata ParseFile(string filePath)
    {
        var metadata = new CraMetadata();
        if (!File.Exists(filePath))
        {
            return metadata;
        }

        var text = File.ReadAllText(filePath);
        foreach (Match match in TagRegex.Matches(text))
        {
            var key = match.Groups["key"].Value.ToLowerInvariant();
            var value = System.Net.WebUtility.HtmlDecode(match.Groups["value"].Value).Trim();
            switch (key)
            {
                case "module": metadata.Module = EmptyToNull(value); break;
                case "version": metadata.Version = EmptyToNull(value); break;
                case "library": metadata.Library = EmptyToNull(value); break;
                case "owner": metadata.Owner = EmptyToNull(value); break;
                case "cra": metadata.CraRelevant = ParseBool(value); break;
                case "safety": metadata.SafetyRelevant = ParseBool(value); break;
                case "network": metadata.NetworkRelevant = ParseBool(value); break;
                case "description": metadata.Description = EmptyToNull(value); break;
            }
        }

        return metadata;
    }

    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static bool? ParseBool(string value)
    {
        if (bool.TryParse(value, out var result))
        {
            return result;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "yes" or "ja" or "1" => true,
            "no" or "nein" or "0" => false,
            _ => null
        };
    }
}
