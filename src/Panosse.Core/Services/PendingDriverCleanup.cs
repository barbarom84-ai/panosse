using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Panosse.Services;

/// <summary>
/// Sélection de pilotes à supprimer conservée le temps de la relance en administrateur.
/// </summary>
public sealed class PendingDriverCleanup
{
    private static readonly TimeSpan MaximumAge = TimeSpan.FromMinutes(10);
    private readonly string filePath;

    public PendingDriverCleanup()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Panosse",
            "pending-driver-cleanup.txt"))
    {
    }

    internal PendingDriverCleanup(string filePath)
    {
        this.filePath = filePath;
    }

    public bool HasPending => File.Exists(filePath);

    public void Save(IEnumerable<string> targets)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllLines(filePath, targets.Where(t => !string.IsNullOrWhiteSpace(t)));
    }

    public void Clear()
    {
        try
        {
            File.Delete(filePath);
        }
        catch
        {
            // Fichier déjà absent ou verrouillé : sans conséquence.
        }
    }

    /// <summary>Lit puis supprime la sélection ; ignore une sélection trop ancienne.</summary>
    public IReadOnlySet<string> Take()
    {
        var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (File.Exists(filePath) && DateTime.Now - File.GetLastWriteTime(filePath) <= MaximumAge)
            {
                targets.UnionWith(File.ReadAllLines(filePath).Where(line => line.Length > 0));
            }
        }
        catch
        {
            targets.Clear();
        }
        finally
        {
            Clear();
        }

        return targets;
    }
}
