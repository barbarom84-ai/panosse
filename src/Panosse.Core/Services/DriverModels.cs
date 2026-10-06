using System;
using System.Collections.Generic;

namespace Panosse.Services;

public sealed class DriverPackageInfo
{
    public string PublishedName { get; init; } = string.Empty;
    public string OriginalName { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public string VersionText { get; init; } = string.Empty;
    public Version? Version { get; init; }
    public DateTime? Date { get; init; }
    public IReadOnlyList<string> DeviceInstanceIds { get; init; } = [];

    /// <summary>Dossier FileRepository du paquet, null s'il n'a pas pu être déterminé.</summary>
    public string? StoreFolder { get; set; }
}

public sealed class PnpDeviceInfo
{
    public string InstanceId { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string DriverName { get; init; } = string.Empty;
    public IReadOnlyList<string> InstalledMatchingDrivers { get; init; } = [];

    public bool IsDisconnected => Status.Equals("Disconnected", StringComparison.OrdinalIgnoreCase);
}

public sealed class DriverInventory
{
    public IReadOnlyList<DriverPackageInfo> Packages { get; init; } = [];
    public IReadOnlyList<PnpDeviceInfo> Devices { get; init; } = [];

    /// <summary>Dernière connexion ou déconnexion connue, par identifiant d'instance (périphériques absents uniquement).</summary>
    public IReadOnlyDictionary<string, DateTime> LastSeenByInstanceId { get; init; } = new Dictionary<string, DateTime>();

    /// <summary>Dossiers FileRepository dont un service Windows charge un fichier.</summary>
    public IReadOnlySet<string> ServiceStoreFolders { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}

public enum DriverIssueKind
{
    ObsoletePackage,
    UnusedPackage,
    PhantomDevice
}

public sealed class DriverIssue
{
    public DriverIssueKind Kind { get; init; }

    /// <summary>oemN.inf pour un paquet, identifiant d'instance pour un périphérique.</summary>
    public string Target { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public string RiskLevel { get; init; } = "Low";
    public string? OriginalName { get; init; }
    public DateTime? LastSeen { get; init; }

    public string KindLabel => Kind switch
    {
        DriverIssueKind.ObsoletePackage => "Pilote obsolète",
        DriverIssueKind.UnusedPackage => "Pilote inutilisé",
        _ => "Périphérique caché"
    };
}

public sealed class DriverCleanResult
{
    public int DevicesRemoved { get; init; }
    public int PackagesRemoved { get; init; }
    public int SkippedCount { get; init; }
    public bool RebootRequired { get; init; }
    public bool RestorePointRequested { get; init; }
    public string? BackupFolder { get; init; }
    public IReadOnlyList<string> Failures { get; init; } = [];
}
