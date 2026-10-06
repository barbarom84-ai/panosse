using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace Panosse.Services;

/// <summary>
/// Lecture de la sortie <c>pnputil /format xml</c> : les noms de balises ne dépendent pas de la langue de Windows.
/// </summary>
internal static class PnpUtilXmlParser
{
    public static IReadOnlyList<DriverPackageInfo> ParseDrivers(string xml)
    {
        XElement? root = XDocument.Parse(xml).Root;
        if (root is null)
        {
            return [];
        }

        return root.Elements("Driver")
            .Select(driver =>
            {
                string versionText = Text(driver, "DriverVersion");
                (DateTime? date, Version? version) = ParseDriverVersion(versionText);
                return new DriverPackageInfo
                {
                    PublishedName = (string?)driver.Attribute("DriverName") ?? string.Empty,
                    OriginalName = Text(driver, "OriginalName"),
                    Provider = Text(driver, "ProviderName"),
                    ClassName = Text(driver, "ClassName"),
                    VersionText = versionText,
                    Version = version,
                    Date = date,
                    DeviceInstanceIds = driver.Element("Devices")?.Elements("Device")
                        .Select(d => (string?)d.Attribute("InstanceId") ?? string.Empty)
                        .Where(id => id.Length > 0)
                        .ToList() ?? []
                };
            })
            .Where(p => p.PublishedName.Length > 0)
            .ToList();
    }

    public static IReadOnlyList<PnpDeviceInfo> ParseDevices(string xml)
    {
        XElement? root = XDocument.Parse(xml).Root;
        if (root is null)
        {
            return [];
        }

        return root.Elements("Device")
            .Select(device => new PnpDeviceInfo
            {
                InstanceId = (string?)device.Attribute("InstanceId") ?? string.Empty,
                Description = Text(device, "DeviceDescription"),
                ClassName = Text(device, "ClassName"),
                Status = Text(device, "Status"),
                DriverName = Text(device, "DriverName"),
                InstalledMatchingDrivers = device.Element("MatchingDrivers")?.Elements("DriverName")
                    .Where(d => Text(d, "Status").Contains("Installed", StringComparison.OrdinalIgnoreCase))
                    .Select(d => (string?)d.Attribute("DriverName") ?? string.Empty)
                    .Where(name => name.Length > 0)
                    .ToList() ?? []
            })
            .Where(d => d.InstanceId.Length > 0)
            .ToList();
    }

    /// <summary>Format pnputil : « MM/dd/yyyy a.b.c.d ».</summary>
    internal static (DateTime? Date, Version? Version) ParseDriverVersion(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return (null, null);
        }

        string[] parts = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        DateTime? date = parts.Length > 0 &&
            DateTime.TryParseExact(parts[0], "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate)
                ? parsedDate
                : null;
        Version? version = parts.Length > 1 && Version.TryParse(parts[1], out Version? parsedVersion)
            ? parsedVersion
            : null;
        return (date, version);
    }

    private static string Text(XElement parent, string name) => parent.Element(name)?.Value.Trim() ?? string.Empty;
}
