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
            if (deviceName.IndexOf("HMI", StringComparison.OrdinalIgnoreCase) < 0 &&
                (device.GetType().FullName ?? "").IndexOf("Hmi", StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            state.Warnings.Add($"HMI device {deviceName} detected. Detailed HMI tag/screen export is documented as limitation for this MVP.");
        }
    }
}
