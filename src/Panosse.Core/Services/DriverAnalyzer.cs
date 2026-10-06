using System;
using System.Collections.Generic;
using System.Linq;

namespace Panosse.Services;

internal static class DriverAnalyzer
{
    /// <summary>Pilotes d'impression : liés aux files d'impression, pas aux périphériques PnP.</summary>
    private static readonly HashSet<string> NeverTouchClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Printer", "PrintQueue"
    };

    /// <summary>
    /// Classes appliquées en complément d'un autre pilote, ou chargées à la demande par une application :
    /// l'absence de périphérique ne prouve pas qu'elles sont inutiles.
    /// </summary>
    private static readonly HashSet<string> NoUnusedVerdictClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Extension", "SoftwareComponent", "AudioProcessingObject", "SoftwareDevice", "HSM", "LegacyDriver"
    };

    /// <summary>Périphériques amovibles : Windows les réinstalle simplement au prochain branchement.</summary>
    private static readonly HashSet<string> RemovableDeviceClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "USB", "USBDevice", "HIDClass", "Keyboard", "Mouse", "WPD", "Volume", "VolumeSnapshot",
        "DiskDrive", "CDROM", "Image", "Camera", "MEDIA", "AudioEndpoint", "Monitor",
        "AndroidUsbDeviceClass", "XboxComposite", "XnaComposite", "Modem", "Ports", "Biometric", "SmartCardReader"
    };

    public static IReadOnlyList<DriverIssue> Analyze(DriverInventory inventory, int minimumAgeDays, DateTime now)
    {
        var issues = new List<DriverIssue>();
        issues.AddRange(FindPackageIssues(inventory));
        issues.AddRange(FindPhantomDevices(inventory, minimumAgeDays, now));
        return issues
            .OrderBy(i => i.Kind)
            .ThenBy(i => i.RiskLevel == "Low" ? 0 : 1)
            .ThenBy(i => i.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    internal static int CompareVersions(DriverPackageInfo left, DriverPackageInfo right)
    {
        int byVersion = Comparer<Version?>.Default.Compare(left.Version, right.Version);
        return byVersion != 0 ? byVersion : Nullable.Compare(left.Date, right.Date);
    }

    private static IEnumerable<DriverIssue> FindPackageIssues(DriverInventory inventory)
    {
        HashSet<string> inUse = new(StringComparer.OrdinalIgnoreCase);
        foreach (PnpDeviceInfo device in inventory.Devices)
        {
            if (device.DriverName.Length > 0)
            {
                inUse.Add(device.DriverName);
            }

            inUse.UnionWith(device.InstalledMatchingDrivers);
        }

        foreach (DriverPackageInfo package in inventory.Packages)
        {
            if (package.DeviceInstanceIds.Count > 0 || IsLoadedByService(package, inventory.ServiceStoreFolders))
            {
                inUse.Add(package.PublishedName);
            }
        }

        ILookup<string, DriverPackageInfo> byOriginalName = inventory.Packages
            .ToLookup(p => p.OriginalName, StringComparer.OrdinalIgnoreCase);

        foreach (DriverPackageInfo package in inventory.Packages)
        {
            if (!IsThirdPartyPackage(package.PublishedName) ||
                inUse.Contains(package.PublishedName) ||
                NeverTouchClasses.Contains(package.ClassName) ||
                string.IsNullOrWhiteSpace(package.OriginalName))
            {
                continue;
            }

            DriverPackageInfo? newest = byOriginalName[package.OriginalName]
                .Where(other => !other.PublishedName.Equals(package.PublishedName, StringComparison.OrdinalIgnoreCase))
                .Where(other => CompareVersions(other, package) > 0)
                .OrderByDescending(other => other, Comparer<DriverPackageInfo>.Create(CompareVersions))
                .FirstOrDefault();

            if (newest is not null)
            {
                yield return new DriverIssue
                {
                    Kind = DriverIssueKind.ObsoletePackage,
                    Target = package.PublishedName,
                    OriginalName = package.OriginalName,
                    Title = $"{package.OriginalName} — {FormatVersion(package)}",
                    Details = $"{package.Provider} · {package.ClassName} · remplacé par la version {FormatVersion(newest)} ({newest.PublishedName})",
                    RiskLevel = "Low"
                };
            }
            else if (!NoUnusedVerdictClasses.Contains(package.ClassName))
            {
                yield return new DriverIssue
                {
                    Kind = DriverIssueKind.UnusedPackage,
                    Target = package.PublishedName,
                    OriginalName = package.OriginalName,
                    Title = $"{package.OriginalName} — {FormatVersion(package)}",
                    Details = $"{package.Provider} · {package.ClassName} · aucun périphérique ni service ne l'utilise ({package.PublishedName})",
                    RiskLevel = "Medium"
                };
            }
        }
    }

    private static IEnumerable<DriverIssue> FindPhantomDevices(DriverInventory inventory, int minimumAgeDays, DateTime now)
    {
        foreach (PnpDeviceInfo device in inventory.Devices.Where(d => d.IsDisconnected))
        {
            if (!inventory.LastSeenByInstanceId.TryGetValue(device.InstanceId, out DateTime lastSeen))
            {
                continue;
            }

            int ageDays = (int)Math.Floor((now - lastSeen).TotalDays);
            if (ageDays < minimumAgeDays)
            {
                continue;
            }

            string title = string.IsNullOrWhiteSpace(device.Description) ? device.InstanceId : device.Description;
            yield return new DriverIssue
            {
                Kind = DriverIssueKind.PhantomDevice,
                Target = device.InstanceId,
                Title = title,
                Details = $"{device.ClassName} · absent depuis {ageDays} jours (vu le {lastSeen:dd/MM/yyyy})",
                LastSeen = lastSeen,
                RiskLevel = RemovableDeviceClasses.Contains(device.ClassName) ? "Low" : "Medium"
            };
        }
    }

    /// <summary>Sans dossier résolu, tout service chargé depuis un paquet de même nom d'origine le protège.</summary>
    private static bool IsLoadedByService(DriverPackageInfo package, IReadOnlySet<string> serviceFolders)
    {
        if (serviceFolders.Count == 0)
        {
            return false;
        }

        if (package.StoreFolder is not null)
        {
            return serviceFolders.Contains(package.StoreFolder);
        }

        string prefix = package.OriginalName + "_";
        return package.OriginalName.Length > 0 &&
            serviceFolders.Any(folder => folder.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsThirdPartyPackage(string publishedName) =>
        publishedName.StartsWith("oem", StringComparison.OrdinalIgnoreCase) &&
        publishedName.EndsWith(".inf", StringComparison.OrdinalIgnoreCase);

    private static string FormatVersion(DriverPackageInfo package)
    {
        string version = package.Version?.ToString() ?? "version inconnue";
        return package.Date is DateTime date ? $"{version} du {date:dd/MM/yyyy}" : version;
    }
}
