using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Panosse.Services;

/// <summary>
/// Pilotes de type service (filtres, pilotes logiciels) chargés directement depuis le magasin de pilotes,
/// sans aucun périphérique PnP pour les relier à leur paquet.
/// </summary>
internal static partial class DriverStoreReferences
{
    private const string ServicesKey = @"SYSTEM\CurrentControlSet\Services";

    [GeneratedRegex(@"\\FileRepository\\([^\\""]+)\\", RegexOptions.IgnoreCase)]
    private static partial Regex FileRepositoryFolderRegex();

    public static HashSet<string> GetServiceReferencedFolders()
    {
        var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using RegistryKey? services = baseKey.OpenSubKey(ServicesKey);
            if (services is null)
            {
                return folders;
            }

            foreach (string serviceName in services.GetSubKeyNames())
            {
                try
                {
                    using RegistryKey? service = services.OpenSubKey(serviceName);
                    AddFolder(folders, service?.GetValue("ImagePath", null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string);
                    using RegistryKey? parameters = service?.OpenSubKey("Parameters");
                    AddFolder(folders, parameters?.GetValue("ServiceDll", null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string);
                }
                catch
                {
                    // Clé de service protégée : ignorée.
                }
            }
        }
        catch
        {
            // Lecture best effort.
        }

        return folders;
    }

    internal static void AddFolder(ISet<string> folders, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        Match match = FileRepositoryFolderRegex().Match(path);
        if (match.Success)
        {
            folders.Add(match.Groups[1].Value);
        }
    }

    /// <summary>Nom du dossier FileRepository d'un paquet publié (ex. oem12.inf → gameflt.inf_amd64_c03f…).</summary>
    public static string? GetStoreFolderName(string publishedName)
    {
        try
        {
            var buffer = new StringBuilder(520);
            if (!SetupGetInfDriverStoreLocationW(publishedName, IntPtr.Zero, null, buffer, buffer.Capacity, out _))
            {
                return null;
            }

            string? directory = Path.GetDirectoryName(buffer.ToString());
            return string.IsNullOrEmpty(directory) ? null : Path.GetFileName(directory);
        }
        catch
        {
            return null;
        }
    }

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupGetInfDriverStoreLocationW(
        string fileName,
        IntPtr alternatePlatformInfo,
        string? localeName,
        StringBuilder returnBuffer,
        int returnBufferSize,
        out int requiredSize);
}
