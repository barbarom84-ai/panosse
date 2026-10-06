using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;

namespace Panosse.Services;

public sealed class DiagnosticsService : IDiagnosticsService
{
    public IReadOnlyList<DiagnosticItem> RunChecks()
    {
        var items = new List<DiagnosticItem>
        {
            CheckAdmin(),
            CheckPathAccess("Dossier Temp utilisateur", Path.GetTempPath()),
            CheckPathAccess("Temp Windows", @"C:\Windows\Temp"),
            CheckPathAccess(
                "Téléchargements",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")),
            CheckPathAccess(
                "Cache Chrome",
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Google\Chrome\User Data\Default\Cache"),
                optionalMissing: true),
            CheckPathAccess(
                "Cache Edge",
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Microsoft\Edge\User Data\Default\Cache"),
                optionalMissing: true),
            CheckPathAccess(
                "INetCache",
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Microsoft\Windows\INetCache"),
                optionalMissing: true),
            CheckPathAccess(
                "Delivery Optimization",
                @"C:\Windows\SoftwareDistribution\DeliveryOptimization\Cache",
                optionalMissing: true),
            CheckPathAccess(
                "WER ReportQueue",
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    @"Microsoft\Windows\WER\ReportQueue"),
                optionalMissing: true),
            CheckAppData()
        };

        return items;
    }

    private static DiagnosticItem CheckAdmin()
    {
        try
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            return new DiagnosticItem
            {
                Name = "Droits administrateur",
                Status = isAdmin ? "ok" : "warning",
                Detail = isAdmin
                    ? "Session élevée (UAC admin)."
                    : "Session standard — certains nettoyages système peuvent être partiels."
            };
        }
        catch (Exception ex)
        {
            return new DiagnosticItem
            {
                Name = "Droits administrateur",
                Status = "error",
                Detail = ex.Message
            };
        }
    }

    private static DiagnosticItem CheckAppData()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Panosse");
        return CheckPathAccess("Données Panosse (%AppData%)", folder, createIfMissing: true);
    }

    private static DiagnosticItem CheckPathAccess(
        string name,
        string path,
        bool optionalMissing = false,
        bool createIfMissing = false)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                if (createIfMissing)
                {
                    Directory.CreateDirectory(path);
                }
                else if (optionalMissing)
                {
                    return new DiagnosticItem
                    {
                        Name = name,
                        Status = "warning",
                        Detail = "Dossier introuvable (navigateur peut-être non installé)."
                    };
                }
                else
                {
                    return new DiagnosticItem
                    {
                        Name = name,
                        Status = "error",
                        Detail = $"Introuvable : {path}"
                    };
                }
            }

            string probe = Path.Combine(path, $".panosse-probe-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);

            return new DiagnosticItem
            {
                Name = name,
                Status = "ok",
                Detail = "Lecture/écriture OK."
            };
        }
        catch (UnauthorizedAccessException)
        {
            return new DiagnosticItem
            {
                Name = name,
                Status = "error",
                Detail = "Accès refusé."
            };
        }
        catch (Exception ex)
        {
            return new DiagnosticItem
            {
                Name = name,
                Status = "error",
                Detail = ex.Message
            };
        }
    }
}
