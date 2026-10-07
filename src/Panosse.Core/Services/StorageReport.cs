using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Panosse.Services;

public sealed class StorageReportItem
{
    public string Label { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public long Bytes { get; init; }
    public string Advice { get; init; } = string.Empty;

    public string Title => $"{Label} · {FormatSize(Bytes)}";

    internal static string FormatSize(long bytes) =>
        bytes >= 1L << 30
            ? $"{Math.Round(bytes / (double)(1L << 30), 1)} Go"
            : $"{Math.Round(bytes / (double)(1L << 20))} Mo";
}

/// <summary>
/// Espace occupé que Panosse ne supprime jamais (données utilisateur ou réglages système) :
/// seulement mesuré, avec la marche à suivre pour le récupérer.
/// </summary>
public static class StorageReport
{
    private const long MinimumReportedBytes = 500L * 1024 * 1024;

    public static IReadOnlyList<StorageReportItem> Build()
    {
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)) ?? @"C:\";

        string huggingFace = Environment.GetEnvironmentVariable("HF_HOME") is { Length: > 0 } hfHome
            ? hfHome
            : Path.Combine(userProfile, @".cache\huggingface");
        string ollamaModels = Environment.GetEnvironmentVariable("OLLAMA_MODELS") is { Length: > 0 } ollamaHome
            ? ollamaHome
            : Path.Combine(userProfile, @".ollama\models");

        var items = new List<StorageReportItem>
        {
            new()
            {
                Label = "Fichier de veille prolongée (hiberfil.sys)",
                Location = Path.Combine(systemDrive, "hiberfil.sys"),
                Bytes = CleanupPaths.GetFileLength(Path.Combine(systemDrive, "hiberfil.sys")),
                Advice = "« powercfg /h /type reduced » (admin) le réduit en gardant le démarrage rapide ; « powercfg /h off » le supprime mais désactive veille prolongée et démarrage rapide."
            },
            new()
            {
                Label = "Modèles Hugging Face",
                Location = huggingFace,
                Bytes = CleanupPaths.EstimateDirectoryBytes(huggingFace),
                Advice = "« hf cache scan » liste les modèles, puis « hf cache delete » pour retirer ceux qui ne servent plus (retéléchargés à la prochaine utilisation)."
            },
            new()
            {
                Label = "Modèles Ollama",
                Location = ollamaModels,
                Bytes = CleanupPaths.EstimateDirectoryBytes(ollamaModels),
                Advice = "« ollama list » puis « ollama rm <modèle> » pour les modèles inutilisés."
            },
            new()
            {
                Label = "Modèles LM Studio",
                Location = Path.Combine(userProfile, @".lmstudio\models"),
                Bytes = CleanupPaths.EstimateDirectories(
                [
                    Path.Combine(userProfile, @".lmstudio\models"),
                    Path.Combine(userProfile, @".cache\lm-studio\models")
                ]),
                Advice = "Supprimer les modèles inutilisés depuis l'onglet « Mes modèles » de LM Studio."
            },
            new()
            {
                Label = "Disques Docker Desktop",
                Location = Path.Combine(localAppData, "Docker"),
                Bytes = SumVirtualDisks(Path.Combine(localAppData, "Docker")),
                Advice = "« docker system prune -a » retire images et conteneurs inutilisés ; le disque virtuel ne rétrécit qu'après compactage (wsl --shutdown puis Optimize-VHD ou diskpart « compact vdisk »)."
            },
            new()
            {
                Label = "Disques des distributions WSL",
                Location = Path.Combine(localAppData, "wsl"),
                Bytes = SumWslDistributionDisks(localAppData),
                Advice = "Faire le ménage dans la distribution, puis « wsl --manage <distribution> --set-sparse true » ou compacter le .vhdx pour rendre l'espace à Windows."
            }
        };

        return items
            .Where(item => item.Bytes >= MinimumReportedBytes)
            .OrderByDescending(item => item.Bytes)
            .ToList();
    }

    private static long SumVirtualDisks(string root)
    {
        try
        {
            if (!Directory.Exists(root))
            {
                return 0;
            }

            var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };
            return new DirectoryInfo(root).EnumerateFiles("*.vhdx", options).Sum(file => file.Length);
        }
        catch
        {
            return 0;
        }
    }

    private static long SumWslDistributionDisks(string localAppData)
    {
        long total = SumVirtualDisks(Path.Combine(localAppData, "wsl"));

        try
        {
            string packages = Path.Combine(localAppData, "Packages");
            if (Directory.Exists(packages))
            {
                foreach (string package in Directory.EnumerateDirectories(packages))
                {
                    total += CleanupPaths.GetFileLength(Path.Combine(package, "LocalState", "ext4.vhdx"));
                }
            }
        }
        catch
        {
            // Dossier Packages inaccessible.
        }

        return total;
    }
}
