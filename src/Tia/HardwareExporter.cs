using TIAExporter.Logging;
using TIAExporter.Normalization;

namespace TIAExporter.Tia;

internal sealed class HardwareExporter
{
    private readonly ExportLogger logger;
    private readonly JsonWriterService jsonWriter;

    public HardwareExporter(ExportLogger logger, JsonWriterService jsonWriter)
    {
        this.logger = logger;
        this.jsonWriter = jsonWriter;
    }

    public IReadOnlyList<PlcCandidate> Export(object project, ExportState state)
    {
        var candidates = new List<PlcCandidate>();

        ScanSubnets(project, state);

        var devices = TiaReflection.GetValue(project, "Devices");
        foreach (var device in TiaReflection.Enumerate(devices))
        {
            var deviceName = TiaReflection.GetString(device, "Name") ?? "unnamed_device";
            var devicePath = deviceName;
            logger.Info($"Scanning device: {deviceName}");

            state.Devices.Add(new NormalizedDevice(
                deviceName,
                TiaReflection.GetString(device, "TypeIdentifier", "TypeName"),
                TiaReflection.GetString(device, "OrderNumber", "ArticleNumber"),
                TiaReflection.GetString(device, "FirmwareVersion"),
                devicePath));

            var items = TiaReflection.GetValue(device, "DeviceItems");
            foreach (var item in TiaReflection.Enumerate(items))
            {
                ScanDeviceItem(item, state, candidates, deviceName, devicePath);
            }
        }

        var summaryPath = Path.Combine(state.ExportRoot, "hardware", "hardware_summary.json");
        jsonWriter.Write(summaryPath, new
        {
            devices = state.Devices,
            hardware_modules = state.HardwareModules,
            network_interfaces = state.NetworkInterfaces,
            network_links = state.NetworkLinks,
            subnets = state.Subnets,
            hardware_attributes = state.HardwareAttributes,
            controllers = state.Controllers
        });

        return candidates;
    }

    private void ScanDeviceItem(object item, ExportState state, List<PlcCandidate> candidates, string deviceName, string parentPath)
    {
        var name = TiaReflection.GetString(item, "Name") ?? "unnamed_item";
        var path = $"{parentPath}/{name}";

        state.HardwareModules.Add(new NormalizedHardwareModule(
            deviceName,
            name,
            TiaReflection.GetString(item, "TypeIdentifier", "TypeName"),
            TiaReflection.GetString(item, "OrderNumber", "ArticleNumber"),
            TiaReflection.GetString(item, "FirmwareVersion"),
            path));

        var attributes = TiaReflection.GetReadableAttributes(item);
        if (attributes.Count > 0)
        {
            state.HardwareAttributes.Add(new HardwareAttributeSnapshot
            {
                ObjectPath = path,
                ObjectType = item.GetType().FullName ?? item.GetType().Name,
                Attributes = attributes
            });
        }

        CaptureNetwork(item, state, deviceName, name, path);

        var plcSoftware = TiaReflection.GetServiceByFullName(item, "Siemens.Engineering.SW.PlcSoftware");
        if (plcSoftware == null)
        {
            var softwareContainer = TiaReflection.GetServiceByFullName(item, "Siemens.Engineering.HW.Features.SoftwareContainer");
            plcSoftware = TiaReflection.GetValue(softwareContainer, "Software");
        }

        if (plcSoftware != null)
        {
            var plcName = TiaReflection.GetString(plcSoftware, "Name") ?? name;
            logger.Info($"PLC software found: {plcName} at {path}");
            candidates.Add(new PlcCandidate(plcName, deviceName, name, path, plcSoftware));
            state.Controllers.Add(new NormalizedController(plcName, deviceName, name, path));
        }

        var children = TiaReflection.GetValue(item, "DeviceItems");
        foreach (var child in TiaReflection.Enumerate(children))
        {
            ScanDeviceItem(child, state, candidates, deviceName, path);
        }
    }

    private void CaptureNetwork(object item, ExportState state, string deviceName, string moduleName, string path)
    {
        var networkInterface = TiaReflection.GetServiceByFullName(item, "Siemens.Engineering.HW.Features.NetworkInterface");
        var ip = TiaReflection.GetString(item, "Address", "IPAddress", "IpAddress");
        var subnetMask = TiaReflection.GetString(item, "SubnetMask");

        if (networkInterface == null && ip == null && subnetMask == null)
        {
            return;
        }

        var normalized = new NormalizedNetworkInterface
        {
            DeviceName = deviceName,
            ModuleName = moduleName,
            InterfaceName = TiaReflection.GetString(item, "InterfaceName", "Name"),
            InterfaceType = TiaReflection.GetString(networkInterface, "InterfaceType"),
            OperatingMode = TiaReflection.GetString(networkInterface, "InterfaceOperatingMode"),
            IpAddress = ip,
            SubnetMask = subnetMask,
            Path = path,
            Attributes = TiaReflection.GetReadableAttributes(networkInterface)
        };

        CaptureAddresses(item, normalized);
        CaptureNodes(networkInterface, normalized);
        CapturePorts(networkInterface, state, normalized, path);
        CaptureIoControllers(networkInterface, state, normalized, path);
        CaptureIoConnectors(networkInterface, state, normalized, path);

        state.NetworkInterfaces.Add(normalized);
    }

    private static void CaptureAddresses(object item, NormalizedNetworkInterface normalized)
    {
        foreach (var address in TiaReflection.Enumerate(TiaReflection.GetValue(item, "Addresses")))
        {
            normalized.IpAddress ??= TiaReflection.GetString(address, "Address", "IPAddress", "IpAddress", "StartAddress");
            normalized.SubnetMask ??= TiaReflection.GetString(address, "SubnetMask");
            foreach (var pair in TiaReflection.GetReadableAttributes(address))
            {
                normalized.Attributes[$"address.{pair.Key}"] = pair.Value;
            }
        }
    }

    private static void CaptureNodes(object? networkInterface, NormalizedNetworkInterface normalized)
    {
        foreach (var node in TiaReflection.Enumerate(TiaReflection.GetValue(networkInterface, "Nodes")))
        {
            var nodeName = TiaReflection.GetString(node, "Name") ?? TiaReflection.ToStableString(node) ?? "node";
            normalized.NodeNames.Add(nodeName);
            normalized.IpAddress ??= TiaReflection.GetString(node, "Address", "IPAddress", "IpAddress");
            normalized.SubnetMask ??= TiaReflection.GetString(node, "SubnetMask");
            foreach (var pair in TiaReflection.GetReadableAttributes(node))
            {
                normalized.Attributes[$"node.{nodeName}.{pair.Key}"] = pair.Value;
            }
        }
    }

    private static void CapturePorts(object? networkInterface, ExportState state, NormalizedNetworkInterface normalized, string path)
    {
        foreach (var port in TiaReflection.Enumerate(TiaReflection.GetValue(networkInterface, "Ports")))
        {
            var portName = TiaReflection.GetString(port, "Name") ?? TiaReflection.ToStableString(port) ?? "port";
            normalized.PortNames.Add(portName);

            foreach (var connectedPort in TiaReflection.Enumerate(TiaReflection.GetValue(port, "ConnectedPorts")))
            {
                state.NetworkLinks.Add(new NormalizedNetworkLink
                {
                    LinkType = "port_connection",
                    SourcePath = $"{path}/{portName}",
                    SourceName = portName,
                    TargetName = TiaReflection.GetString(connectedPort, "Name") ?? TiaReflection.ToStableString(connectedPort)
                });
            }
        }
    }

    private void CaptureIoControllers(object? networkInterface, ExportState state, NormalizedNetworkInterface normalized, string path)
    {
        foreach (var controller in TiaReflection.Enumerate(TiaReflection.GetValue(networkInterface, "IoControllers")))
        {
            foreach (var ioSystem in TiaReflection.Enumerate(TiaReflection.GetValue(controller, "IoSystem")))
            {
                CaptureIoSystem(ioSystem, state, normalized, path, "io_controller");
            }
        }
    }

    private void CaptureIoConnectors(object? networkInterface, ExportState state, NormalizedNetworkInterface normalized, string path)
    {
        foreach (var connector in TiaReflection.Enumerate(TiaReflection.GetValue(networkInterface, "IoConnectors")))
        {
            var ioSystem = TiaReflection.GetValue(connector, "ConnectedToIoSystem");
            if (ioSystem != null)
            {
                CaptureIoSystem(ioSystem, state, normalized, path, "io_connector");
            }
        }
    }

    private static void CaptureIoSystem(object ioSystem, ExportState state, NormalizedNetworkInterface normalized, string path, string linkType)
    {
        var ioSystemName = TiaReflection.GetString(ioSystem, "Name") ?? TiaReflection.ToStableString(ioSystem);
        if (!string.IsNullOrWhiteSpace(ioSystemName) && !normalized.IoSystemNames.Contains(ioSystemName!))
        {
            normalized.IoSystemNames.Add(ioSystemName!);
        }

        var subnet = TiaReflection.GetValue(ioSystem, "Subnet");
        var subnetName = TiaReflection.GetString(subnet, "Name");
        normalized.SubnetName ??= subnetName;

        state.NetworkLinks.Add(new NormalizedNetworkLink
        {
            LinkType = linkType,
            SourcePath = path,
            SourceName = normalized.InterfaceName,
            TargetName = ioSystemName,
            IoSystemName = ioSystemName,
            SubnetName = subnetName
        });

        foreach (var connectedDevice in TiaReflection.Enumerate(TiaReflection.GetValue(ioSystem, "ConnectedIoDevices")))
        {
            state.NetworkLinks.Add(new NormalizedNetworkLink
            {
                LinkType = "io_device",
                SourcePath = path,
                SourceName = normalized.InterfaceName,
                TargetName = TiaReflection.GetString(connectedDevice, "Name") ?? TiaReflection.ToStableString(connectedDevice),
                IoSystemName = ioSystemName,
                SubnetName = subnetName
            });
        }
    }

    private static void ScanSubnets(object project, ExportState state)
    {
        foreach (var subnet in TiaReflection.Enumerate(TiaReflection.GetValue(project, "Subnets")))
        {
            var normalized = new NormalizedSubnet
            {
                Name = TiaReflection.GetString(subnet, "Name") ?? "unnamed_subnet",
                NetType = TiaReflection.GetString(subnet, "NetType"),
                Attributes = TiaReflection.GetReadableAttributes(subnet)
            };

            foreach (var ioSystem in TiaReflection.Enumerate(TiaReflection.GetValue(subnet, "IoSystems")))
            {
                var name = TiaReflection.GetString(ioSystem, "Name") ?? TiaReflection.ToStableString(ioSystem);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    normalized.IoSystemNames.Add(name!);
                }
            }

            state.Subnets.Add(normalized);
        }
    }
}

internal sealed record PlcCandidate(string PlcName, string DeviceName, string DeviceItemName, string SourcePath, object PlcSoftware);
