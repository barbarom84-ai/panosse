using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Panosse.Services;

public interface IDriverCleanerService : IDisposable
{
    bool IsElevated { get; }
    string BackupDirectory { get; }

    /// <summary>Inventaire en lecture seule (aucun droit administrateur requis).</summary>
    Task<DriverInventory> LoadInventoryAsync(CancellationToken cancellationToken = default);

    IReadOnlyList<DriverIssue> Analyze(DriverInventory inventory, int minimumAgeDays);

    /// <summary>
    /// Point de restauration, puis suppression des périphériques cachés, puis export et suppression
    /// des paquets (sans /force : Windows refuse si le pilote sert encore). Administrateur requis.
    /// </summary>
    Task<DriverCleanResult> CleanAsync(
        IReadOnlyList<DriverIssue> issues,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    string? GetLatestBackupFolder();

    Task<(bool Success, string Message)> RestoreBackupAsync(string backupFolder, CancellationToken cancellationToken = default);
}
