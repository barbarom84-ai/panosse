using System.Collections.Generic;
using System.Threading;

namespace Panosse.Services;

public interface IRegistryCleanerService
{
    string BackupDirectory { get; }

    /// <summary>Analyse en lecture seule ; ne modifie jamais le registre.</summary>
    IReadOnlyList<RegistryIssue> Scan(
        string? cleanupProfile,
        IReadOnlyCollection<string>? exclusionPatterns = null,
        CancellationToken cancellationToken = default);

    /// <summary>Sauvegarde les clés concernées dans un fichier .reg, puis supprime uniquement les entrées sauvegardées.</summary>
    RegistryCleanResult Clean(IReadOnlyList<RegistryIssue> issues, CancellationToken cancellationToken = default);

    string? GetLatestBackupPath();

    bool TryRestoreBackup(string backupPath, out string message);
}
