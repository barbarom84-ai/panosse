using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Panosse.Services;

public sealed class DriverCleanerService : IDriverCleanerService
{
    private const int ErrorSuccessRebootRequired = 3010;
    private static readonly TimeSpan EnumerationTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ActionTimeout = TimeSpan.FromMinutes(3);
    private static readonly Encoding PnpUtilEncoding = CreateEncoding(ansi: true);
    private static readonly Encoding ConsoleEncoding = CreateEncoding(ansi: false);

    private readonly ILoggerService logger;
    private readonly HashSet<Process> runningProcesses = new();
    private readonly object processLock = new();
    private bool disposed;

    public DriverCleanerService(ILoggerService logger)
    {
        this.logger = logger;
        BackupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Panosse",
            "DriverBackups");
    }

    public bool IsElevated
    {
        get
        {
            try
            {
                using WindowsIdentity identity = WindowsIdentity.GetCurrent();
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }

    public string BackupDirectory { get; }

    // Un processus 32 bits sur Windows 64 bits ne trouve pas pnputil dans SysWOW64.
    private static string PnpUtilPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Windows),
        Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess ? "Sysnative" : "System32",
        "pnputil.exe");

    public async Task<DriverInventory> LoadInventoryAsync(CancellationToken cancellationToken = default)
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "panosse-drivers-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        try
        {
            string driversXml = Path.Combine(tempDirectory, "drivers.xml");
            string devicesXml = Path.Combine(tempDirectory, "devices.xml");

            Task<(int ExitCode, string Output)> driversTask = RunToolAsync(
                PnpUtilPath,
                ["/enum-drivers", "/devices", "/format", "xml", "/output-file", driversXml],
                EnumerationTimeout,
                cancellationToken);
            Task<(int ExitCode, string Output)> devicesTask = RunToolAsync(
                PnpUtilPath,
                ["/enum-devices", "/drivers", "/format", "xml", "/output-file", devicesXml],
                EnumerationTimeout,
                cancellationToken);
            await Task.WhenAll(driversTask, devicesTask);

            if (!File.Exists(driversXml) || !File.Exists(devicesXml))
            {
                throw new InvalidOperationException(
                    $"pnputil n'a pas pu lister les pilotes (codes {driversTask.Result.ExitCode}/{devicesTask.Result.ExitCode}).");
            }

            return await Task.Run(() =>
            {
                IReadOnlyList<DriverPackageInfo> packages = PnpUtilXmlParser.ParseDrivers(File.ReadAllText(driversXml, Encoding.UTF8));
                IReadOnlyList<PnpDeviceInfo> devices = PnpUtilXmlParser.ParseDevices(File.ReadAllText(devicesXml, Encoding.UTF8));

                var lastSeen = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
                foreach (PnpDeviceInfo device in devices.Where(d => d.IsDisconnected))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (DeviceLastSeenReader.GetLastSeen(device.InstanceId) is DateTime seen)
                    {
                        lastSeen[device.InstanceId] = seen;
                    }
                }

                foreach (DriverPackageInfo package in packages)
                {
                    package.StoreFolder = DriverStoreReferences.GetStoreFolderName(package.PublishedName);
                }

                HashSet<string> serviceFolders = DriverStoreReferences.GetServiceReferencedFolders();
                logger.LogInfo(
                    "drivers",
                    $"Inventaire : {packages.Count} paquets, {devices.Count} périphériques, {lastSeen.Count} absents datés, {serviceFolders.Count} pilotes de service.");
                return new DriverInventory
                {
                    Packages = packages,
                    Devices = devices,
                    LastSeenByInstanceId = lastSeen,
                    ServiceStoreFolders = serviceFolders
                };
            }, cancellationToken);
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    public IReadOnlyList<DriverIssue> Analyze(DriverInventory inventory, int minimumAgeDays) =>
        DriverAnalyzer.Analyze(inventory, minimumAgeDays, DateTime.Now);

    public async Task<DriverCleanResult> CleanAsync(
        IReadOnlyList<DriverIssue> issues,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsElevated)
        {
            throw new InvalidOperationException("Droits administrateur requis pour supprimer des pilotes.");
        }

        var failures = new List<string>();
        int devicesRemoved = 0;
        int packagesRemoved = 0;
        int skipped = 0;
        bool rebootRequired = false;

        progress?.Report("🛟 Création d'un point de restauration système...");
        bool restorePointRequested = await TryCreateRestorePointAsync(cancellationToken);

        foreach (DriverIssue device in issues.Where(i => i.Kind == DriverIssueKind.PhantomDevice))
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report($"🔌 Retrait du périphérique caché « {device.Title} »...");
            (int exitCode, string output) = await RunToolAsync(PnpUtilPath, ["/remove-device", device.Target], ActionTimeout, cancellationToken);
            if (IsSuccess(exitCode))
            {
                devicesRemoved++;
                rebootRequired |= exitCode == ErrorSuccessRebootRequired;
            }
            else
            {
                skipped++;
                failures.Add($"{device.Title} : {Summarize(output, exitCode)}");
            }
        }

        string backupRoot = Path.Combine(BackupDirectory, DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        foreach (DriverIssue package in issues.Where(i => i.Kind is DriverIssueKind.ObsoletePackage or DriverIssueKind.UnusedPackage))
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report($"💾 Sauvegarde puis suppression de {package.OriginalName} ({package.Target})...");

            string exportDirectory = Path.Combine(
                backupRoot,
                $"{Path.GetFileNameWithoutExtension(package.Target)}_{SanitizeFileName(package.OriginalName ?? "pilote")}");
            Directory.CreateDirectory(exportDirectory);

            (int exportCode, string exportOutput) = await RunToolAsync(
                PnpUtilPath, ["/export-driver", package.Target, exportDirectory], ActionTimeout, cancellationToken);
            if (!IsSuccess(exportCode) || !Directory.EnumerateFiles(exportDirectory, "*.inf", SearchOption.AllDirectories).Any())
            {
                skipped++;
                failures.Add($"{package.Title} : sauvegarde impossible, pilote conservé ({Summarize(exportOutput, exportCode)})");
                TryDeleteDirectory(exportDirectory);
                continue;
            }

            (int deleteCode, string deleteOutput) = await RunToolAsync(
                PnpUtilPath, ["/delete-driver", package.Target], ActionTimeout, cancellationToken);
            if (IsSuccess(deleteCode))
            {
                packagesRemoved++;
                rebootRequired |= deleteCode == ErrorSuccessRebootRequired;
            }
            else
            {
                skipped++;
                failures.Add($"{package.Title} : refusé par Windows ({Summarize(deleteOutput, deleteCode)})");
                TryDeleteDirectory(exportDirectory);
            }
        }

        string? backupFolder = Directory.Exists(backupRoot) && Directory.EnumerateFileSystemEntries(backupRoot).Any()
            ? backupRoot
            : null;
        if (backupFolder is null)
        {
            TryDeleteDirectory(backupRoot);
        }

        logger.LogInfo(
            "drivers",
            $"Nettoyage pilotes : {devicesRemoved} périphérique(s), {packagesRemoved} paquet(s), {skipped} ignoré(s), restauration={restorePointRequested}, sauvegarde={backupFolder ?? "aucune"}.");
        foreach (string failure in failures)
        {
            logger.LogWarning("drivers", failure);
        }

        return new DriverCleanResult
        {
            DevicesRemoved = devicesRemoved,
            PackagesRemoved = packagesRemoved,
            SkippedCount = skipped,
            RebootRequired = rebootRequired,
            RestorePointRequested = restorePointRequested,
            BackupFolder = backupFolder,
            Failures = failures
        };
    }

    public string? GetLatestBackupFolder()
    {
        try
        {
            if (!Directory.Exists(BackupDirectory))
            {
                return null;
            }

            return Directory.EnumerateDirectories(BackupDirectory)
                .OrderByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(folder => Directory.EnumerateFiles(folder, "*.inf", SearchOption.AllDirectories).Any());
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool Success, string Message)> RestoreBackupAsync(string backupFolder, CancellationToken cancellationToken = default)
    {
        if (!IsElevated)
        {
            return (false, "Droits administrateur requis pour réinstaller des pilotes.");
        }

        if (string.IsNullOrWhiteSpace(backupFolder) || !Directory.Exists(backupFolder))
        {
            return (false, "Sauvegarde de pilotes introuvable.");
        }

        (int exitCode, string output) = await RunToolAsync(
            PnpUtilPath,
            ["/add-driver", Path.Combine(backupFolder, "*.inf"), "/subdirs"],
            ActionTimeout,
            cancellationToken);
        if (IsSuccess(exitCode))
        {
            logger.LogInfo("drivers", $"Pilotes restaurés depuis {backupFolder}.");
            return (true, $"Pilotes de la sauvegarde {Path.GetFileName(backupFolder)} remis dans le magasin de pilotes.");
        }

        logger.LogWarning("drivers", $"Restauration des pilotes incomplète ({exitCode}) : {output}");
        return (false, $"Restauration incomplète ({Summarize(output, exitCode)}).");
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        lock (processLock)
        {
            foreach (Process process in runningProcesses)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
            }

            runningProcesses.Clear();
        }
    }

    internal static bool IsSuccess(int exitCode) => exitCode is 0 or ErrorSuccessRebootRequired;

    internal static string SanitizeFileName(string name)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        string sanitized = new(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "pilote" : sanitized;
    }

    private async Task<bool> TryCreateRestorePointAsync(CancellationToken cancellationToken)
    {
        try
        {
            string powershell = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");
            (int exitCode, string output) = await RunToolAsync(
                powershell,
                [
                    "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-Command",
                    "Checkpoint-Computer -Description 'Panosse - avant nettoyage des pilotes' -RestorePointType MODIFY_SETTINGS -ErrorAction Stop"
                ],
                ActionTimeout,
                cancellationToken);
            if (exitCode != 0)
            {
                logger.LogWarning("drivers", $"Point de restauration non créé ({exitCode}) : {output}");
            }

            return exitCode == 0;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning("drivers", $"Point de restauration impossible : {ex.Message}");
            return false;
        }
    }

    private async Task<(int ExitCode, string Output)> RunToolAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        Encoding encoding = fileName.Equals(PnpUtilPath, StringComparison.OrdinalIgnoreCase) ? PnpUtilEncoding : ConsoleEncoding;
        var startInfo = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = encoding,
            StandardErrorEncoding = encoding
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        lock (processLock)
        {
            runningProcesses.Add(process);
        }

        try
        {
            Task<string> stdout = process.StandardOutput.ReadToEndAsync();
            Task<string> stderr = process.StandardError.ReadToEndAsync();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);
            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                cancellationToken.ThrowIfCancellationRequested();
                return (-1, "délai dépassé");
            }

            return (process.ExitCode, $"{await stdout}{await stderr}".Trim());
        }
        finally
        {
            lock (processLock)
            {
                runningProcesses.Remove(process);
            }
        }
    }

    /// <summary>Garde le message d'erreur : la première ligne est l'en-tête de l'outil, les dernières des compteurs.</summary>
    internal static string Summarize(string output, int exitCode)
    {
        string[] lines = output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToArray();
        string? message = lines.Skip(1).FirstOrDefault() ?? lines.FirstOrDefault();
        return string.IsNullOrEmpty(message) ? $"code {exitCode}" : $"{message} — code {exitCode}";
    }

    /// <summary>pnputil écrit dans la page de code ANSI une fois redirigé, PowerShell dans la page OEM.</summary>
    private static Encoding CreateEncoding(bool ansi)
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            System.Globalization.TextInfo textInfo = System.Globalization.CultureInfo.CurrentCulture.TextInfo;
            return Encoding.GetEncoding(ansi ? textInfo.ANSICodePage : textInfo.OEMCodePage);
        }
        catch
        {
            return Encoding.Default;
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Nettoyage best effort.
        }
    }
}
