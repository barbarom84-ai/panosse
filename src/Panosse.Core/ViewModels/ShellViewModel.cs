using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Windows.Input;
using Panosse.Core.Commands;
using Panosse.Services;

namespace Panosse.Core.ViewModels;

public sealed partial class ShellViewModel : INotifyPropertyChanged, IDisposable
{
    private string statusText = "Prêt";
    private string buttonText = "Passer la panosse";
    private double progressValue;
    private bool isBusy;
    private bool previewModeEnabled;
    private string exclusionPatterns = string.Empty;
    private string lastRunSummary = "Aucun nettoyage exécuté.";
    private string historySummary = "Aucun historique disponible.";
    private string updateStatusText = "Mise à jour non vérifiée.";
    private double updateProgressValue;
    private bool isUpdateAvailable;
    private bool isCheckingUpdate;
    private bool isPreparingUpdate;
    private bool suppressSettingsAutoSave;
    private CancellationTokenSource? settingsAutoSaveDebounceCts;
    private string? updateDownloadUrl;
    private string? updateTagName;
    private string? updateExpectedSha256;
    private string? preparedScriptPath;
    private string? downloadedUpdatePath;
    private UpdatePhase updatePhase = UpdatePhase.Idle;
    private bool checkUpdatesOnStartup = true;
    private bool playSuccessSound = true;
    private bool showTrayNotifications = true;
    private bool enableScheduledCleanup;
    private int scheduledCleanupIntervalHours = 24;
    private string schedulerStatusText = "Planification inactive.";
    private string cleanupProfile = CleanupProfiles.Standard;
    private int uiScalePercent = 100;
    private const int MaxVisibleTaskMessages = 6;
    private bool isSuccessStatus;
    private bool hasPreviewResults;
    private string previewRiskSummary = string.Empty;
    private bool hasStorageReport;
    private CancellationTokenSource? cleanupCts;
    private System.Timers.Timer? scheduledCleanupTimer;
    private readonly SemaphoreSlim scheduledCleanupLock = new(1, 1);
    private bool disposed;
    private readonly ICleanupOrchestrator cleanupOrchestrator;
    private readonly ILoggerService loggerService;
    private readonly ISettingsService settingsService;
    private readonly IOperationHistoryService historyService;
    private readonly IUpdateService updateService;
    private readonly IUpdateOrchestrator updateOrchestrator;
    private readonly IDiagnosticsService diagnosticsService;
    private readonly IRegistryCleanerService registryCleaner;
    private string registryBackupStatus = string.Empty;
    private const string GithubRepo = "barbarom84-ai/panosse";
    private static readonly TimeSpan MinimumCleanupDuration = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// Optional UI-thread marshaler (set by WinUI host for timer/property updates).
    /// </summary>
    public Action<Action>? UiMarshal { get; set; }

    /// <summary>
    /// Optional confirmation before a destructive cleanup. Return false to cancel.
    /// </summary>
    public Func<string, Task<bool>>? ConfirmCleanupAsync { get; set; }

    /// <summary>
    /// Optional generic confirmation (title, message, primary button text). Return false to cancel.
    /// </summary>
    public Func<string, string, string, Task<bool>>? ConfirmActionAsync { get; set; }

    public ShellViewModel(
        ICleanupOrchestrator cleanupOrchestrator,
        ILoggerService loggerService,
        ISettingsService settingsService,
        IOperationHistoryService historyService,
        IUpdateService updateService,
        IUpdateOrchestrator updateOrchestrator,
        IDiagnosticsService diagnosticsService,
        IRegistryCleanerService registryCleaner)
    {
        this.cleanupOrchestrator = cleanupOrchestrator;
        this.registryCleaner = registryCleaner;
        this.loggerService = loggerService;
        this.settingsService = settingsService;
        this.historyService = historyService;
        this.updateService = updateService;
        this.updateOrchestrator = updateOrchestrator;
        this.diagnosticsService = diagnosticsService;

        RunCleanupCommand = new AsyncRelayCommand(
            executeAsync: () => ExecuteCleanupAsync(previewOnly: false),
            canExecute: () => !IsBusy,
            onException: ex => loggerService.LogError("winui-cleanup", "Cleanup command failed.", ex));
        RunPreviewCommand = new AsyncRelayCommand(
            executeAsync: () => ExecuteCleanupAsync(previewOnly: true),
            canExecute: () => !IsBusy,
            onException: ex => loggerService.LogError("winui-cleanup", "Preview command failed.", ex));
        ConfirmCleanupFromPreviewCommand = new AsyncRelayCommand(
            executeAsync: () => ExecuteCleanupAsync(previewOnly: false, fromPreview: true),
            canExecute: () => !IsBusy && HasPreviewResults,
            onException: ex => loggerService.LogError("winui-cleanup", "Confirm cleanup failed.", ex));
        CancelCleanupCommand = new RelayCommand(
            execute: CancelCleanup,
            canExecute: () => IsBusy);

        SaveSettingsCommand = new RelayCommand(SaveSettings);
        RefreshHistoryCommand = new RelayCommand(RefreshHistory);
        ClearHistoryCommand = new RelayCommand(ClearHistory);
        ExportHistoryCommand = new RelayCommand(ExportHistory);
        RunDiagnosticsCommand = new RelayCommand(RunDiagnostics);
        CheckUpdatesCommand = new AsyncRelayCommand(
            executeAsync: ExecuteCheckUpdatesAsync,
            canExecute: () => !IsCheckingUpdate && !IsPreparingUpdate,
            onException: ex => loggerService.LogError("winui-update", "Update check failed.", ex));
        PrepareUpdateCommand = new AsyncRelayCommand(
            executeAsync: ExecutePrepareUpdateAsync,
            canExecute: () => isUpdateAvailable && !IsPreparingUpdate,
            onException: ex => loggerService.LogError("winui-update", "Update preparation failed.", ex));
        InstallPreparedUpdateCommand = new RelayCommand(
            execute: ExecuteInstallPreparedUpdate,
            canExecute: () => !string.IsNullOrWhiteSpace(preparedScriptPath) && !IsPreparingUpdate);
        OpenLogsCommand = new RelayCommand(OpenLogsFolder);
        RestoreRegistryBackupCommand = new AsyncRelayCommand(
            executeAsync: RestoreLatestRegistryBackupAsync,
            canExecute: () => !IsBusy,
            onException: ex => loggerService.LogError("winui-registry", "Registry restore failed.", ex));
        OpenRegistryBackupsCommand = new RelayCommand(OpenRegistryBackupsFolder);

        TaskMessages.CollectionChanged += OnTaskMessagesCollectionChanged;
        HistoryItems.CollectionChanged += OnHistoryItemsCollectionChanged;

        LoadSettings();
        RefreshHistory();
        RefreshRegistryBackupStatus();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<string> TaskMessages { get; } = new();
    public ObservableCollection<string> HistoryItems { get; } = new();
    public ObservableCollection<string> PreviewItems { get; } = new();
    public ObservableCollection<StorageReportItem> StorageReportItems { get; } = new();
    public ObservableCollection<string> DiagnosticItems { get; } = new();

    public ICommand RunCleanupCommand { get; }
    public ICommand RunPreviewCommand { get; }
    public ICommand ConfirmCleanupFromPreviewCommand { get; }
    public ICommand CancelCleanupCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand RefreshHistoryCommand { get; }
    public ICommand ClearHistoryCommand { get; }
    public ICommand ExportHistoryCommand { get; }
    public ICommand RunDiagnosticsCommand { get; }
    public ICommand CheckUpdatesCommand { get; }
    public ICommand PrepareUpdateCommand { get; }
    public ICommand InstallPreparedUpdateCommand { get; }
    public ICommand OpenLogsCommand { get; }
    public ICommand RestoreRegistryBackupCommand { get; }
    public ICommand OpenRegistryBackupsCommand { get; }

    public string RegistryBackupStatus
    {
        get => registryBackupStatus;
        private set => SetField(ref registryBackupStatus, value);
    }

    public string StatusText
    {
        get => statusText;
        set => SetField(ref statusText, value);
    }

    public string ButtonText
    {
        get => buttonText;
        set => SetField(ref buttonText, value);
    }

    public double ProgressValue
    {
        get => progressValue;
        set
        {
            if (SetField(ref progressValue, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsProgressVisible)));
            }
        }
    }

    public bool IsBusy
    {
        get => isBusy;
        set
        {
            if (SetField(ref isBusy, value))
            {
                (RunCleanupCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (RunPreviewCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (ConfirmCleanupFromPreviewCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (CancelCleanupCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (RestoreRegistryBackupCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsProgressVisible)));
            }
        }
    }

    public bool IsProgressVisible => IsBusy || ProgressValue > 0;

    public bool HasTaskMessages => TaskMessages.Count > 0;

    public bool HasPreviewResults
    {
        get => hasPreviewResults;
        private set
        {
            if (SetField(ref hasPreviewResults, value))
            {
                (ConfirmCleanupFromPreviewCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string PreviewRiskSummary
    {
        get => previewRiskSummary;
        private set => SetField(ref previewRiskSummary, value);
    }

    public bool HasStorageReport
    {
        get => hasStorageReport;
        private set => SetField(ref hasStorageReport, value);
    }

    public bool IsSuccessStatus
    {
        get => isSuccessStatus;
        private set => SetField(ref isSuccessStatus, value);
    }

    public bool CheckUpdatesOnStartup
    {
        get => checkUpdatesOnStartup;
        set
        {
            if (SetField(ref checkUpdatesOnStartup, value))
            {
                ScheduleAutoSaveSettings();
            }
        }
    }

    public bool PlaySuccessSound
    {
        get => playSuccessSound;
        set
        {
            if (SetField(ref playSuccessSound, value))
            {
                ScheduleAutoSaveSettings();
            }
        }
    }

    public bool ShowTrayNotifications
    {
        get => showTrayNotifications;
        set
        {
            if (SetField(ref showTrayNotifications, value))
            {
                ScheduleAutoSaveSettings();
            }
        }
    }

    public bool PreviewModeEnabled
    {
        get => previewModeEnabled;
        set
        {
            if (SetField(ref previewModeEnabled, value))
            {
                ScheduleAutoSaveSettings();
            }
        }
    }

    public string ExclusionPatterns
    {
        get => exclusionPatterns;
        set
        {
            if (SetField(ref exclusionPatterns, value))
            {
                ScheduleAutoSaveSettings();
            }
        }
    }

    public string LastRunSummary
    {
        get => lastRunSummary;
        set => SetField(ref lastRunSummary, value);
    }

    public string HistorySummary
    {
        get => historySummary;
        set => SetField(ref historySummary, value);
    }

    public string UpdateStatusText
    {
        get => updateStatusText;
        set => SetField(ref updateStatusText, value);
    }

    public UpdatePhase CurrentUpdatePhase => updatePhase;

    public string UpdatePhaseText => updatePhase switch
    {
        UpdatePhase.Checking => "Vérification en cours…",
        UpdatePhase.Downloading => "Téléchargement / préparation…",
        UpdatePhase.Ready => "Mise à jour prête à installer.",
        UpdatePhase.Available => "Mise à jour disponible.",
        UpdatePhase.UpToDate => "Application à jour.",
        UpdatePhase.Error => "Erreur de mise à jour.",
        _ => UpdateStatusText
    };

    public double UpdateProgressValue
    {
        get => updateProgressValue;
        set => SetField(ref updateProgressValue, value);
    }

    public bool IsUpdateAvailable
    {
        get => isUpdateAvailable;
        set
        {
            if (SetField(ref isUpdateAvailable, value))
            {
                (PrepareUpdateCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UpdatePhaseText)));
            }
        }
    }

    public bool IsInstallReady => !string.IsNullOrWhiteSpace(preparedScriptPath);

    public bool IsUpdateProgressVisible => IsPreparingUpdate;

    public string CleanupProfile
    {
        get => cleanupProfile;
        set
        {
            string normalized = CleanupProfiles.Normalize(value);
            if (SetField(ref cleanupProfile, normalized))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CleanupProfileIndex)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CleanupProfileDescription)));
                ScheduleAutoSaveSettings();
            }
        }
    }

    public int CleanupProfileIndex
    {
        get => CleanupProfiles.ToIndex(CleanupProfile);
        set => CleanupProfile = CleanupProfiles.FromIndex(value);
    }

    public string CleanupProfileDescription => CleanupProfiles.GetDescription(CleanupProfile);

    public int UiScalePercent
    {
        get => uiScalePercent;
        set
        {
            int normalized = value is 110 or 125 ? value : 100;
            if (SetField(ref uiScalePercent, normalized))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UiScaleIndex)));
                ScheduleAutoSaveSettings();
                UiScaleChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public int UiScaleIndex
    {
        get => UiScalePercent switch
        {
            110 => 1,
            125 => 2,
            _ => 0
        };
        set => UiScalePercent = value switch
        {
            1 => 110,
            2 => 125,
            _ => 100
        };
    }

    public event EventHandler? UiScaleChanged;

    public bool IsCheckingUpdate
    {
        get => isCheckingUpdate;
        private set
        {
            if (SetField(ref isCheckingUpdate, value))
            {
                (CheckUpdatesCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UpdatePhaseText)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanCheckUpdates)));
            }
        }
    }

    public bool IsPreparingUpdate
    {
        get => isPreparingUpdate;
        private set
        {
            if (SetField(ref isPreparingUpdate, value))
            {
                (CheckUpdatesCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (PrepareUpdateCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (InstallPreparedUpdateCommand as RelayCommand)?.RaiseCanExecuteChanged();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsUpdateProgressVisible)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UpdatePhaseText)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanCheckUpdates)));
            }
        }
    }

    public bool CanCheckUpdates => !IsCheckingUpdate && !IsPreparingUpdate;

    public bool EnableScheduledCleanup
    {
        get => enableScheduledCleanup;
        set
        {
            if (SetField(ref enableScheduledCleanup, value))
            {
                if (!suppressSettingsAutoSave)
                {
                    ConfigureScheduledCleanup();
                }

                ScheduleAutoSaveSettings();
            }
        }
    }

    public int ScheduledCleanupIntervalHours
    {
        get => scheduledCleanupIntervalHours;
        set
        {
            if (SetField(ref scheduledCleanupIntervalHours, Math.Max(1, value)))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ScheduledCleanupIntervalHoursText)));
                if (!suppressSettingsAutoSave)
                {
                    ConfigureScheduledCleanup();
                }

                ScheduleAutoSaveSettings();
            }
        }
    }

    public string ScheduledCleanupIntervalHoursText
    {
        get => scheduledCleanupIntervalHours.ToString();
        set
        {
            if (int.TryParse(value, out int hours))
            {
                ScheduledCleanupIntervalHours = hours;
            }
        }
    }

    public string SchedulerStatusText
    {
        get => schedulerStatusText;
        set => SetField(ref schedulerStatusText, value);
    }

    public Task RunCleanupFromTrayAsync() =>
        ExecuteCleanupAsync(previewOnly: false, requireConfirmation: false);

    private async Task ExecuteCleanupAsync(bool previewOnly, bool fromPreview = false, bool requireConfirmation = true)
    {
        bool preview = fromPreview
            ? false
            : previewOnly || PreviewModeEnabled;
        if (!preview && requireConfirmation && ConfirmCleanupAsync is not null)
        {
            string confirmMessage = fromPreview && HasPreviewResults
                ? BuildCleanupConfirmMessage()
                : BuildDefaultCleanupConfirmMessage();

            if (!await ConfirmCleanupAsync(confirmMessage))
            {
                StatusText = "Nettoyage annulé.";
                return;
            }
        }

        IsBusy = true;
        cleanupCts?.Dispose();
        cleanupCts = new CancellationTokenSource();
        CancellationToken token = cleanupCts.Token;
        Stopwatch cleanupStopwatch = Stopwatch.StartNew();
        IsSuccessStatus = false;
        ButtonText = preview ? "Prévisualisation..." : "Nettoyage...";
        StatusText = "Préparation...";
        ProgressValue = 0;
        TaskMessages.Clear();
        if (preview)
        {
            PreviewItems.Clear();
            HasPreviewResults = false;
            PreviewRiskSummary = string.Empty;
        }

        var options = new CleanupExecutionOptions
        {
            PreviewOnly = preview,
            ExclusionPatterns = GetExclusions(),
            CleanupProfile = CleanupProfile
        };

        long totalFreedBytes = 0;
        try
        {
            await foreach (CleanupStepUpdate update in cleanupOrchestrator.StreamCleanupAsync(options, token))
            {
                StatusText = update.Message;
                if (update.IsCompleted)
                {
                    totalFreedBytes += update.StepFreedBytes;
                    AddTaskMessage(update.Message);
                }

                double ratio = update.TotalSteps > 0
                    ? (double)update.StepIndex / update.TotalSteps
                    : 0;
                ProgressValue = Math.Round(ratio * 100, 2);
            }

            await EnsureMinimumCleanupDurationAsync(cleanupStopwatch);

            double totalMb = Math.Round(totalFreedBytes / 1024.0 / 1024.0, 2);
            LastRunSummary = preview
                ? $"Prévisualisation terminée : {totalMb} Mo estimés."
                : $"Nettoyage terminé : {totalMb} Mo libérés.";
            StatusText = LastRunSummary;
            IsSuccessStatus = true;

            if (preview)
            {
                PopulatePreviewResults(totalMb);
                await RefreshStorageReportAsync();
            }
            else
            {
                PreviewItems.Clear();
                HasPreviewResults = false;
                PreviewRiskSummary = string.Empty;
                RefreshRegistryBackupStatus();
            }

            RefreshHistory();
        }
        catch (OperationCanceledException)
        {
            IsSuccessStatus = false;
            StatusText = preview ? "Aperçu annulé." : "Nettoyage annulé.";
            LastRunSummary = StatusText;
            AddTaskMessage(StatusText);
            RefreshHistory();
        }
        catch (Exception ex)
        {
            await EnsureMinimumCleanupDurationAsync(cleanupStopwatch);

            IsSuccessStatus = false;
            StatusText = $"Erreur : {ex.Message}";
            AddTaskMessage(StatusText);
            loggerService.LogError("winui-cleanup", "Cleanup execution failed.", ex);
        }
        finally
        {
            cleanupCts?.Dispose();
            cleanupCts = null;
            IsBusy = false;
            ButtonText = "Passer la panosse";
        }
    }

    private void CancelCleanup()
    {
        try
        {
            cleanupCts?.Cancel();
            StatusText = "Annulation en cours...";
        }
        catch (Exception ex)
        {
            loggerService.LogError("winui-cleanup", "Failed to cancel cleanup.", ex);
        }
    }

    private void RunDiagnostics()
    {
        try
        {
            DiagnosticItems.Clear();
            foreach (DiagnosticItem item in diagnosticsService.RunChecks())
            {
                DiagnosticItems.Add(item.DisplayLine);
            }

            int errors = DiagnosticItems.Count(line => line.StartsWith('❌'));
            int warnings = DiagnosticItems.Count(line => line.StartsWith('⚠'));
            StatusText = errors > 0
                ? $"Diagnostics : {errors} problème(s), {warnings} avertissement(s)."
                : warnings > 0
                    ? $"Diagnostics : {warnings} avertissement(s)."
                    : "Diagnostics : tout est OK.";
        }
        catch (Exception ex)
        {
            loggerService.LogError("winui-diagnostics", "Diagnostics failed.", ex);
            StatusText = "Impossible d'exécuter les diagnostics.";
        }
    }

    private string BuildDefaultCleanupConfirmMessage()
    {
        string profileName = CleanupProfiles.GetDisplayName(CleanupProfile);
        string message = $"Lancer le nettoyage ({profileName}) ?\n{CleanupProfiles.GetDescription(CleanupProfile)}";
        if (CleanupProfiles.Normalize(CleanupProfile) == CleanupProfiles.Deep)
        {
            message += "\n\nAttention : le mode Profond inclut les entrées registre système (désinstallations orphelines, DLL partagées), les téléchargements anciens, les logs, l'ancienne installation de Windows (plus de retour arrière possible) et les caches développeur (retéléchargés au prochain build). Le registre est sauvegardé automatiquement avant toute modification ; les fichiers supprimés ne sont pas récupérables.";
        }

        return message;
    }

    private string BuildCleanupConfirmMessage()
    {
        var lines = new List<string>
        {
            $"Confirmer le nettoyage ({CleanupProfiles.GetDisplayName(CleanupProfile)}) avec les estimations actuelles ?"
        };

        if (!string.IsNullOrWhiteSpace(PreviewRiskSummary))
        {
            lines.Add(PreviewRiskSummary);
        }

        foreach (string item in PreviewItems.Take(5))
        {
            lines.Add("• " + item);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private void PopulatePreviewResults(double totalMb)
    {
        PreviewItems.Clear();
        IReadOnlyList<CleanupPreviewItem> items = cleanupOrchestrator.GetPreviewBreakdown(GetExclusions(), CleanupProfile);
        bool hasMediumRisk = false;
        bool hasHighRisk = false;

        foreach (CleanupPreviewItem item in items
                     .Where(i => i.EstimatedBytes > 0 || i.ItemCount > 0 || string.Equals(i.RiskLevel, "High", StringComparison.OrdinalIgnoreCase))
                     .OrderByDescending(i => i.EstimatedBytes)
                     .ThenByDescending(i => i.ItemCount))
        {
            string risk = FormatRiskLevel(item.RiskLevel);
            if (string.Equals(item.RiskLevel, "High", StringComparison.OrdinalIgnoreCase))
            {
                hasHighRisk = true;
            }
            else if (string.Equals(item.RiskLevel, "Medium", StringComparison.OrdinalIgnoreCase))
            {
                hasMediumRisk = true;
            }

            PreviewItems.Add(FormatPreviewLine(item, risk));
        }

        if (PreviewItems.Count == 0)
        {
            PreviewItems.Add($"Total estimé : {totalMb} Mo");
        }

        HasPreviewResults = PreviewItems.Count > 0;
        PreviewRiskSummary = hasHighRisk
            ? "Attention (Profond) : téléchargements, logs, pilotes ou Windows.old à risque élevé. Le registre est sauvegardé avant modification."
            : hasMediumRisk
                ? "Attention : certaines catégories (téléchargements, registre système…) ont un risque moyen. Le registre est sauvegardé avant modification."
                : $"Profil {CleanupProfiles.GetDisplayName(CleanupProfile)} — risque faible pour les catégories listées.";
    }

    private async Task RefreshStorageReportAsync()
    {
        try
        {
            IReadOnlyList<StorageReportItem> report = await Task.Run(StorageReport.Build);
            StorageReportItems.Clear();
            foreach (StorageReportItem item in report)
            {
                StorageReportItems.Add(item);
            }

            HasStorageReport = StorageReportItems.Count > 0;
        }
        catch (Exception ex)
        {
            loggerService.LogError("winui-cleanup", "Storage report failed.", ex);
        }
    }

    internal static string FormatPreviewLine(CleanupPreviewItem item, string risk)
    {
        if (item.ItemCount > 0 && item.EstimatedBytes == 0)
        {
            string details = string.IsNullOrWhiteSpace(item.Location) ? string.Empty : $" ({item.Location})";
            return $"{item.Category} : {item.ItemCount} entrée(s){details} · risque {risk}";
        }

        double mb = Math.Round(item.EstimatedBytes / 1024.0 / 1024.0, 2);
        return $"{item.Category} : {mb} Mo · risque {risk}";
    }

    private void RefreshRegistryBackupStatus()
    {
        string? latest = registryCleaner.GetLatestBackupPath();
        RegistryBackupStatus = latest is null
            ? "Aucune sauvegarde du registre pour l'instant. Panosse en crée une automatiquement avant chaque nettoyage du registre."
            : $"Dernière sauvegarde : {Path.GetFileName(latest)} ({File.GetLastWriteTime(latest):dd/MM/yyyy HH:mm})";
    }

    private async Task RestoreLatestRegistryBackupAsync()
    {
        string? latest = registryCleaner.GetLatestBackupPath();
        if (latest is null)
        {
            StatusText = "Aucune sauvegarde du registre à restaurer.";
            return;
        }

        if (ConfirmActionAsync is not null &&
            !await ConfirmActionAsync(
                "Restaurer le registre",
                $"Restaurer le registre depuis la sauvegarde du {File.GetLastWriteTime(latest):dd/MM/yyyy à HH:mm} ?\nLes entrées supprimées lors de ce nettoyage seront recréées.",
                "Restaurer"))
        {
            StatusText = "Restauration annulée.";
            return;
        }

        IsBusy = true;
        try
        {
            (bool restored, string message) = await Task.Run(() =>
            {
                bool ok = registryCleaner.TryRestoreBackup(latest, out string result);
                return (ok, result);
            });

            IsSuccessStatus = restored;
            StatusText = message;
            AddTaskMessage(restored ? $"✅ {message}" : $"❌ {message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OpenRegistryBackupsFolder()
    {
        try
        {
            Directory.CreateDirectory(registryCleaner.BackupDirectory);
            Process.Start(new ProcessStartInfo
            {
                FileName = registryCleaner.BackupDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            loggerService.LogError("winui-registry", "Failed to open registry backups folder.", ex);
            StatusText = "Impossible d'ouvrir le dossier des sauvegardes du registre.";
        }
    }

    private static string FormatRiskLevel(string riskLevel) =>
        riskLevel?.Trim().ToLowerInvariant() switch
        {
            "high" => "élevé",
            "medium" => "moyen",
            _ => "faible"
        };

    private static async Task EnsureMinimumCleanupDurationAsync(Stopwatch cleanupStopwatch)
    {
        TimeSpan remainingDuration = MinimumCleanupDuration - cleanupStopwatch.Elapsed;
        if (remainingDuration > TimeSpan.Zero)
        {
            await Task.Delay(remainingDuration);
        }
    }

    private void AddTaskMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        TaskMessages.Add(message);
        while (TaskMessages.Count > MaxVisibleTaskMessages)
        {
            TaskMessages.RemoveAt(0);
        }
    }

    private List<string> GetExclusions()
    {
        return ExclusionPatterns
            .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void LoadSettings()
    {
        try
        {
            suppressSettingsAutoSave = true;
            AppSettings settings = settingsService.Load();
            CheckUpdatesOnStartup = settings.CheckUpdatesOnStartup;
            PlaySuccessSound = settings.PlaySuccessSound;
            ShowTrayNotifications = settings.ShowTrayNotifications;
            PreviewModeEnabled = settings.PreviewModeEnabled;
            ExclusionPatterns = settings.ExclusionPatterns ?? string.Empty;
            EnableScheduledCleanup = settings.EnableScheduledCleanup;
            ScheduledCleanupIntervalHours = Math.Max(1, settings.ScheduledCleanupIntervalHours);
            CleanupProfile = CleanupProfiles.Normalize(settings.CleanupProfile);
            UiScalePercent = settings.UiScalePercent is 110 or 125 ? settings.UiScalePercent : 100;

            ConfigureScheduledCleanup();
            suppressSettingsAutoSave = false;
        }
        catch (Exception ex)
        {
            suppressSettingsAutoSave = false;
            loggerService.LogError("winui-settings", "Failed to load settings.", ex);
        }
    }

    private void SaveSettings()
    {
        SaveSettings(showStatus: true);
    }

    private void SaveSettings(bool showStatus)
    {
        try
        {
            settingsService.Save(BuildCurrentSettings());
            ConfigureScheduledCleanup();
            if (showStatus)
            {
                StatusText = "Paramètres sauvegardés.";
            }
        }
        catch (Exception ex)
        {
            loggerService.LogError("winui-settings", "Failed to save settings.", ex);
            if (showStatus)
            {
                StatusText = "Erreur de sauvegarde des paramètres.";
            }
        }
    }

    private void RefreshHistory()
    {
        try
        {
            IReadOnlyList<OperationHistoryEntry> entries = historyService.GetRecentEntries(12);
            HistoryItems.Clear();
            foreach (OperationHistoryEntry entry in entries)
            {
                HistoryItems.Add(FormatHistoryLine(entry));
            }

            HistorySummary = HistoryItems.Count == 0
                ? "Aucun historique disponible."
                : $"{HistoryItems.Count} entrée(s) récente(s).";
        }
        catch (Exception ex)
        {
            loggerService.LogError("winui-history", "Failed to refresh history.", ex);
            HistorySummary = "Historique indisponible.";
        }
    }

    internal static string FormatHistoryLine(OperationHistoryEntry entry)
    {
        double mb = Math.Round(entry.FreedBytes / 1024.0 / 1024.0, 2);
        string type = FormatOperationType(entry.OperationType);
        string outcome = FormatOutcome(entry.Outcome);
        string line = $"{entry.TimestampUtc.ToLocalTime():dd/MM HH:mm} · {type} · {outcome} · {mb} Mo";
        if (!string.IsNullOrWhiteSpace(entry.Details) &&
            !string.Equals(entry.Outcome, "success", StringComparison.OrdinalIgnoreCase))
        {
            line += $" — {entry.Details}";
        }

        return line;
    }

    private static string FormatOperationType(string operationType) =>
        operationType?.Trim().ToLowerInvariant() switch
        {
            "cleanup_preview" => "Aperçu",
            "cleanup_manual" => "Nettoyage",
            "cleanup_scheduled" => "Planifié",
            "update" => "Mise à jour",
            _ => string.IsNullOrWhiteSpace(operationType) ? "Opération" : operationType
        };

    private static string FormatOutcome(string outcome) =>
        outcome?.Trim().ToLowerInvariant() switch
        {
            "success" => "Succès",
            "failure" => "Échec",
            "cancelled" => "Annulé",
            _ => string.IsNullOrWhiteSpace(outcome) ? "—" : outcome
        };

    private void ClearHistory()
    {
        try
        {
            historyService.Clear();
            RefreshHistory();
            StatusText = "Historique effacé.";
        }
        catch (Exception ex)
        {
            loggerService.LogError("winui-history", "Failed to clear history.", ex);
            StatusText = "Impossible d'effacer l'historique.";
        }
    }

    private void ExportHistory()
    {
        try
        {
            string path = historyService.ExportToCsv();
            StatusText = $"Historique exporté : {path}";
            try
            {
                _ = Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Opening the file is optional.
            }
        }
        catch (Exception ex)
        {
            loggerService.LogError("winui-history", "Failed to export history.", ex);
            StatusText = "Impossible d'exporter l'historique.";
        }
    }

    private void OpenLogsFolder()
    {
        try
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "Panosse");
            Directory.CreateDirectory(appFolder);
            Process.Start(new ProcessStartInfo
            {
                FileName = appFolder,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            loggerService.LogError("winui-logs", "Failed to open logs folder.", ex);
            StatusText = "Impossible d'ouvrir le dossier de logs.";
        }
    }

    private void ConfigureScheduledCleanup()
    {
        try
        {
            if (scheduledCleanupTimer != null)
            {
                scheduledCleanupTimer.Stop();
                scheduledCleanupTimer.Dispose();
                scheduledCleanupTimer = null;
            }

            if (!EnableScheduledCleanup)
            {
                SchedulerStatusText = "Planification inactive.";
                return;
            }

            double intervalMs = TimeSpan.FromHours(ScheduledCleanupIntervalHours).TotalMilliseconds;
            scheduledCleanupTimer = new System.Timers.Timer(intervalMs);
            scheduledCleanupTimer.AutoReset = true;
            scheduledCleanupTimer.Elapsed += (_, _) =>
            {
                RunOnUiThread(() => _ = ExecuteScheduledCleanupAsync());
            };
            scheduledCleanupTimer.Start();
            SchedulerStatusText = $"Planifié toutes les {ScheduledCleanupIntervalHours}h.";
        }
        catch (Exception ex)
        {
            loggerService.LogError("winui-scheduler", "Failed to configure scheduled cleanup.", ex);
            SchedulerStatusText = "Erreur de planification.";
        }
    }

    private void RunOnUiThread(Action action)
    {
        if (UiMarshal is not null)
        {
            UiMarshal(action);
            return;
        }

        action();
    }

    private async Task ExecuteScheduledCleanupAsync()
    {
        if (!await scheduledCleanupLock.WaitAsync(0))
        {
            return;
        }

        try
        {
            if (IsBusy)
            {
                return;
            }

            StatusText = "Nettoyage planifié en cours...";
            await ExecuteCleanupAsync(previewOnly: false, requireConfirmation: false);
            SchedulerStatusText = $"Dernier run planifié : {DateTime.Now:dd/MM HH:mm}";
        }
        catch (Exception ex)
        {
            loggerService.LogError("winui-scheduler", "Scheduled cleanup failed.", ex);
            SchedulerStatusText = "Échec du run planifié.";
        }
        finally
        {
            scheduledCleanupLock.Release();
        }
    }

    private void OnTaskMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasTaskMessages)));
    }

    private AppSettings BuildCurrentSettings()
    {
        return new AppSettings
        {
            CheckUpdatesOnStartup = CheckUpdatesOnStartup,
            PlaySuccessSound = PlaySuccessSound,
            ShowTrayNotifications = ShowTrayNotifications,
            PreviewModeEnabled = PreviewModeEnabled,
            ExclusionPatterns = ExclusionPatterns,
            EnableScheduledCleanup = EnableScheduledCleanup,
            ScheduledCleanupIntervalHours = ScheduledCleanupIntervalHours,
            CleanupProfile = CleanupProfile,
            UiScalePercent = UiScalePercent,
            SchemaVersion = AppSettings.CurrentSchemaVersion
        };
    }

    private void OnHistoryItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Kept for future history UI hooks.
    }

    private void ScheduleAutoSaveSettings()
    {
        if (suppressSettingsAutoSave)
        {
            return;
        }

        settingsAutoSaveDebounceCts?.Cancel();
        settingsAutoSaveDebounceCts?.Dispose();

        var cts = new CancellationTokenSource();
        settingsAutoSaveDebounceCts = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(550, cts.Token);
                PersistCurrentSettingsBestEffort();
            }
            catch (OperationCanceledException)
            {
                // Intentionally ignored: a newer user edit superseded this save.
            }
        });
    }

    private void PersistCurrentSettingsBestEffort()
    {
        try
        {
            settingsService.Save(BuildCurrentSettings());
        }
        catch (Exception ex)
        {
            loggerService.LogError("winui-settings", "Failed to auto-save settings.", ex);
        }
    }

    private void NotifyInstallReadyChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsInstallReady)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UpdatePhaseText)));
        (InstallPreparedUpdateCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (scheduledCleanupTimer != null)
        {
            scheduledCleanupTimer.Stop();
            scheduledCleanupTimer.Dispose();
            scheduledCleanupTimer = null;
        }

        TaskMessages.CollectionChanged -= OnTaskMessagesCollectionChanged;
        HistoryItems.CollectionChanged -= OnHistoryItemsCollectionChanged;
        cleanupCts?.Cancel();
        cleanupCts?.Dispose();
        cleanupCts = null;
        settingsAutoSaveDebounceCts?.Cancel();
        settingsAutoSaveDebounceCts?.Dispose();
        settingsAutoSaveDebounceCts = null;
        scheduledCleanupLock.Dispose();
    }
}
