using TIAExporter.Logging;
using TIAExporter.Normalization;

namespace TIAExporter.Tia;

internal sealed class TagTableExporter
{
    private readonly ExportLogger logger;
    private readonly EvidenceHasher hasher;

    public TagTableExporter(ExportLogger logger, EvidenceHasher hasher)
    {
        this.logger = logger;
        this.hasher = hasher;
    }

    public void Export(PlcCandidate plc, ExportState state)
    {
        var group = TiaReflection.GetValue(plc.PlcSoftware, "TagTableGroup");
        ExportGroup(group, state, plc.PlcName, "", Path.Combine(state.ExportRoot, "software", FileNameSanitizer.Sanitize(plc.PlcName), "tag_tables"));
    }

    private void ExportGroup(object? group, ExportState state, string plcName, string groupPath, string root)
    {
        if (group == null)
        {
            return;
        }

        var tables = TiaReflection.GetValue(group, "TagTables");
        foreach (var table in TiaReflection.Enumerate(tables))
        {
            ExportTable(table, state, plcName, groupPath, root);
        }

        var groups = TiaReflection.GetValue(group, "Groups");
        foreach (var child in TiaReflection.Enumerate(groups))
        {
            var childName = TiaReflection.GetString(child, "Name") ?? "Group";
            var childPath = string.IsNullOrWhiteSpace(groupPath) ? childName : $"{groupPath}/{childName}";
            ExportGroup(child, state, plcName, childPath, root);
        }
    }

    private void ExportTable(object table, ExportState state, string plcName, string groupPath, string root)
    {
        var name = TiaReflection.GetString(table, "Name") ?? "unnamed_tag_table";
        var folder = string.IsNullOrWhiteSpace(groupPath)
            ? root
            : Path.Combine(root, FileNameSanitizer.Sanitize(groupPath).Replace('/', Path.DirectorySeparatorChar));
        var filePath = FileNameSanitizer.UniquePath(folder, $"{FileNameSanitizer.Sanitize(name)}.xml");
        int? tagCount = null;
        var tags = TiaReflection.GetValue(table, "Tags");
        if (tags != null)
        {
            tagCount = TiaReflection.Enumerate(tags).Count();
        }

        try
        {
            if (!TiaReflection.TryExport(table, filePath))
            {
                logger.Warn($"Tag table {plcName}/{groupPath}/{name} cannot be exported; writing metadata only.");
                state.Warnings.Add($"Tag table {plcName}/{groupPath}/{name} cannot be exported; metadata only.");
                state.TagTables.Add(new NormalizedTagTable(plcName, name, groupPath, tagCount, null, false));
                return;
            }

            var evidence = hasher.HashFile(state.ExportRoot, filePath, $"{plcName}/{groupPath}/{name}");
            state.EvidenceFiles.Add(evidence);
            state.TagTables.Add(new NormalizedTagTable(plcName, name, groupPath, tagCount, evidence.RelativePath, true));
        }
        catch (Exception ex)
        {
            var message = $"Failed to export tag table {plcName}/{groupPath}/{name}";
            logger.Error(message, ex);
            state.Errors.Add($"{message}: {ex.Message}");
            state.TagTables.Add(new NormalizedTagTable(plcName, name, groupPath, tagCount, null, false));
        }
    }
}
