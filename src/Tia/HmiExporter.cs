using TIAExporter.Logging;
using TIAExporter.Normalization;

namespace TIAExporter.Tia;

internal sealed class HmiExporter
{
    private readonly ExportLogger logger;

    public HmiExporter(ExportLogger logger)
    {
        this.logger = logger;
    }

    public void Export(object project, ExportState state)
    {
        logger.Info("Scanning HMI devices for metadata.");
        foreach (var device in TiaReflection.Enumerate(TiaReflection.GetValue(project, "Devices")))
        {
            var deviceName = TiaReflection.GetString(device, "Name") ?? "unnamed_device";
            var typeName = device.GetType().FullName ?? "";
            var looksHmi = deviceName.IndexOf("HMI", StringComparison.OrdinalIgnoreCase) >= 0 || typeName.IndexOf("Hmi", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!looksHmi)
            {
                continue;
            }

            ScanHmiDevice(device, deviceName, state);
        }
    }

    private void ScanHmiDevice(object device, string deviceName, ExportState state)
    {
        var inventory = state.HmiInventory.FirstOrDefault(x => string.Equals(x.Name, deviceName, StringComparison.OrdinalIgnoreCase));
        var created = false;
        if (inventory == null)
        {
            inventory = new HmiInventoryItem
            {
                Name = deviceName,
                Path = deviceName,
                ExportStatus = "metadata_only"
            };
            created = true;
        }

        foreach (var item in TiaReflection.Enumerate(TiaReflection.GetValue(device, "DeviceItems")))
        {
            CollectHmiTarget(item, inventory);
        }

        if (inventory.ScreenCount == null && inventory.TagCount == null && inventory.ConnectionCount == null)
        {
            inventory.Limitations.Add("No HmiTarget service was reachable. Detailed HMI tag/screen export typically requires WinCC Openness support and matching TIA project access.");
            inventory.RequiredAction ??= "Install/enable WinCC Openness module on the engineering station and re-run the exporter.";
        }
        else
        {
            inventory.ExportStatus = "metadata_only_with_counts";
        }

        if (created)
        {
            state.HmiInventory.Add(inventory);
        }
    }

    private static void CollectHmiTarget(object deviceItem, HmiInventoryItem inventory)
    {
        var hmiTarget = TiaReflection.GetServiceByFullName(deviceItem, "Siemens.Engineering.Hmi.HmiTarget");
        var softwareContainer = TiaReflection.GetServiceByFullName(deviceItem, "Siemens.Engineering.HW.Features.SoftwareContainer");
        var software = hmiTarget ?? TiaReflection.GetValue(softwareContainer, "Software");
        if (software == null)
        {
            foreach (var child in TiaReflection.Enumerate(TiaReflection.GetValue(deviceItem, "DeviceItems")))
            {
                CollectHmiTarget(child, inventory);
            }
            return;
        }

        inventory.RuntimeName ??= TiaReflection.GetString(software, "Name");

        var screens = CountFolderRecursive(TiaReflection.GetValue(software, "ScreenFolder"), "Screens", "Folders", "ScreenFolders");
        if (screens.count > 0)
        {
            inventory.ScreenCount = (inventory.ScreenCount ?? 0) + screens.count;
            foreach (var n in screens.names.Take(64))
            {
                if (!inventory.ScreenNames.Contains(n)) inventory.ScreenNames.Add(n);
            }
        }

        var tags = CountFolderRecursive(TiaReflection.GetValue(software, "TagFolder"), "Tags", "Folders", "TagFolders");
        if (tags.count > 0)
        {
            inventory.TagCount = (inventory.TagCount ?? 0) + tags.count;
        }

        var connections = CountFolderRecursive(TiaReflection.GetValue(software, "Connections"), "Connections", "Folders");
        if (connections.count > 0)
        {
            inventory.ConnectionCount = (inventory.ConnectionCount ?? 0) + connections.count;
            foreach (var n in connections.names.Take(64))
            {
                if (!inventory.ConnectionNames.Contains(n)) inventory.ConnectionNames.Add(n);
            }
        }

        var alarms = CountFolderRecursive(TiaReflection.GetValue(software, "AlarmClasses", "Alarms"), "Alarms", "Folders");
        if (alarms.count > 0)
        {
            inventory.AlarmCount = (inventory.AlarmCount ?? 0) + alarms.count;
        }
    }

    private static (int count, List<string> names) CountFolderRecursive(object? folder, params string[] itemPropertyNames)
    {
        var names = new List<string>();
        var count = 0;
        if (folder == null) return (0, names);

        foreach (var prop in itemPropertyNames)
        {
            foreach (var item in TiaReflection.Enumerate(TiaReflection.GetValue(folder, prop)))
            {
                count++;
                var n = TiaReflection.GetString(item, "Name");
                if (!string.IsNullOrWhiteSpace(n)) names.Add(n!);
            }
        }
        foreach (var sub in TiaReflection.Enumerate(TiaReflection.GetValue(folder, "Folders", "Groups")))
        {
            var nested = CountFolderRecursive(sub, itemPropertyNames);
            count += nested.count;
            names.AddRange(nested.names);
        }
        return (count, names);
    }
}
