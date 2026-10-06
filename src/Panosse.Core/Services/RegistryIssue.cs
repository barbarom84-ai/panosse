using Microsoft.Win32;

namespace Panosse.Services;

public enum RegistryIssueKind
{
    /// <summary>Supprime une seule valeur de la clé.</summary>
    Value,

    /// <summary>Supprime la clé et toute son arborescence.</summary>
    Key,

    /// <summary>Vide toutes les valeurs de la clé et de ses sous-clés, sans supprimer les clés.</summary>
    ClearValues
}

public sealed class RegistryIssue
{
    public string Category { get; init; } = string.Empty;
    public RegistryHive Hive { get; init; }
    public string KeyPath { get; init; } = string.Empty;
    public string? ValueName { get; init; }
    public RegistryIssueKind Kind { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? TargetPath { get; init; }
    public string RiskLevel { get; init; } = "Low";

    public string HiveName => Hive switch
    {
        RegistryHive.LocalMachine => "HKEY_LOCAL_MACHINE",
        RegistryHive.CurrentUser => "HKEY_CURRENT_USER",
        RegistryHive.ClassesRoot => "HKEY_CLASSES_ROOT",
        RegistryHive.Users => "HKEY_USERS",
        _ => Hive.ToString()
    };

    public string FullKeyPath => $"{HiveName}\\{KeyPath}";
}

public sealed class RegistryCleanResult
{
    public int RemovedCount { get; init; }
    public int SkippedCount { get; init; }
    public string? BackupPath { get; init; }
}
