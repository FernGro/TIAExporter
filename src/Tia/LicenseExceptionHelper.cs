namespace TIAExporter.Tia;

internal static class LicenseExceptionHelper
{
    internal static bool IsLicenseMissingException(Exception? ex)
    {
        if (ex == null)
        {
            return false;
        }

        var typeName = ex.GetType().Name;
        var message = ex.Message;
        return typeName.Equals("LicenseNotFoundException", StringComparison.OrdinalIgnoreCase)
            || message.IndexOf("Necessary license", StringComparison.OrdinalIgnoreCase) >= 0
            || (message.IndexOf("license", StringComparison.OrdinalIgnoreCase) >= 0
                && (message.IndexOf("missing", StringComparison.OrdinalIgnoreCase) >= 0
                    || message.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0
                    || message.IndexOf("nicht gefunden", StringComparison.OrdinalIgnoreCase) >= 0));
    }

    internal static string ExtractMissingLicenseName(Exception ex)
    {
        var message = ex.Message;
        const string marker = "Necessary license '";
        var start = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start >= 0)
        {
            start += marker.Length;
            var end = message.IndexOf('\'', start);
            if (end > start)
            {
                return message.Substring(start, end - start);
            }
        }

        return "STEP 7 Professional";
    }

    internal static string LicenseSuspectReason(string? licenseName) =>
        $"Siemens Openness reported that license '{licenseName ?? "STEP 7 Professional"}' is not usable for this export process. " +
        "This does not prove the license is not installed; possible causes include ALM/license-server reachability, occupied/floating license, version mismatch, Openness process context, or project feature requirements.";

    internal static string LicenseRequiredAction(string? licenseName) =>
        $"Verify in Automation License Manager that a TIA Portal '{licenseName ?? "STEP 7 Professional"}' license is available to the same Windows user/session used by the exporter. " +
        "Check floating license availability, license server connection, TIA/ALM version match, and whether another TIA process currently occupies the license.";

    internal static string TruncateMessage(string message, int maxLength) =>
        message.Length <= maxLength ? message : message.Substring(0, maxLength) + "...";
}
