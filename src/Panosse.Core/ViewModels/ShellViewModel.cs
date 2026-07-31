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
    private bool checkUpdatesOnStartup = true;
    private bool playSuccessSound = true;
    private bool showTrayNotifications = true;
    private bool enableScheduledCleanup;
    private int scheduledCleanupIntervalHours = 24;
    private string schedulerStatusText = "Planification inactive.";
    private string cleanupProfile = CleanupProfiles.Standard;
    private const int MaxVisibleTaskMessages = 6;
    private bool isSuccessStatus;
    private bool hasPreviewResults;
    private string previewRiskSummary = string.Empty;
    private System.Timers.Timer? scheduledCleanupTimer;
    private readonly SemaphoreSlim scheduledCleanupLock = new(1, 1);
    private bool disposed;
    private readonly ICleanupOrchestrator cleanupOrchestrator;
    private readonly ILoggerService loggerService;
    private readonly ISettingsService settingsService;
    private readonly IOperationHistoryService historyService;
    private readonly IUpdateService updateService;
    private readonly IUpdateOrchestrator updateOrchestrator;
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

    public ShellViewModel(
        ICleanupOrchestrator cleanupOrchestrator,
        ILoggerService loggerService,
        ISettingsService settingsService,
        IOperationHistoryService historyService,
        IUpdateService updateService,
        IUpdateOrchestrator updateOrchestrator)
    {
        this.cleanupOrchestrator = cleanupOrchestrator;
        this.loggerService = loggerService;
        this.settingsService = settingsService;
        this.historyService = historyService;
        this.updateService = updateService;
        this.updateOrchestrator = updateOrchestrator;

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

        SaveSettingsCommand = new RelayCommand(SaveSettings);
        RefreshHistoryCommand = new RelayCommand(RefreshHistory);
        ClearHistoryCommand = new RelayCommand(ClearHistory);
        ExportHistoryCommand = new RelayCommand(ExportHistory);
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

        TaskMessages.CollectionChanged += OnTaskMessagesCollectionChanged;
        HistoryItems.CollectionChanged += OnHistoryItemsCollectionChanged;

        LoadSettings();
        RefreshHistory();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<string> TaskMessages { get; } = new();
    public ObservableCollection<string> HistoryItems { get; } = new();
    public ObservableCollection<string> PreviewItems { get; } = new();

    public ICommand RunCleanupCommand { get; }
    public ICommand RunPreviewCommand { get; }
    public ICommand ConfirmCleanupFromPreviewCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand RefreshHistoryCommand { get; }
    public ICommand ClearHistoryCommand { get; }
    public ICommand ExportHistoryCommand { get; }
    public ICommand CheckUpdatesCommand { get; }
    public ICommand PrepareUpdateCommand { get; }
    public ICommand InstallPreparedUpdateCommand { get; }
    public ICommand OpenLogsCommand { get; }

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

    public bool IsCheckingUpdate
    {
        get => isCheckingUpdate;
        private set
        {
            if (SetField(ref isCheckingUpdate, value))
            {
                (CheckUpdatesCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
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
            }
        }
    }

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
            await foreach (CleanupStepUpdate update in cleanupOrchestrator.StreamCleanupAsync(options))
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
            }
            else
            {
                PreviewItems.Clear();
                HasPreviewResults = false;
                PreviewRiskSummary = string.Empty;
            }

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
            IsBusy = false;
            ButtonText = "Passer la panosse";
        }
    }

    private string BuildDefaultCleanupConfirmMessage()
    {
        string profileName = CleanupProfiles.GetDisplayName(CleanupProfile);
        string message = $"Lancer le nettoyage ({profileName}) ?\n{CleanupProfiles.GetDescription(CleanupProfile)}";
        if (CleanupProfiles.Normalize(CleanupProfile) == CleanupProfiles.Deep)
        {
            message += "\n\nAttention : le mode Profond inclut registre, téléchargements anciens et logs — certaines actions sont difficilement réversibles.";
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
                     .Where(i => i.EstimatedBytes > 0 || string.Equals(i.RiskLevel, "High", StringComparison.OrdinalIgnoreCase))
                     .OrderByDescending(i => i.EstimatedBytes))
        {
            double mb = Math.Round(item.EstimatedBytes / 1024.0 / 1024.0, 2);
            string risk = FormatRiskLevel(item.RiskLevel);
            if (string.Equals(item.RiskLevel, "High", StringComparison.OrdinalIgnoreCase))
            {
                hasHighRisk = true;
            }
            else if (string.Equals(item.RiskLevel, "Medium", StringComparison.OrdinalIgnoreCase))
            {
                hasMediumRisk = true;
            }

            PreviewItems.Add($"{item.Category} : {mb} Mo · risque {risk}");
        }

        if (PreviewItems.Count == 0)
        {
            PreviewItems.Add($"Total estimé : {totalMb} Mo");
        }

        HasPreviewResults = PreviewItems.Count > 0;
        PreviewRiskSummary = hasHighRisk
            ? "Attention (Profond) : registre, téléchargements ou logs à risque élevé."
            : hasMediumRisk
                ? "Attention : certaines catégories (ex. téléchargements) ont un risque moyen."
                : $"Profil {CleanupProfiles.GetDisplayName(CleanupProfile)} — risque faible pour les catégories listées.";
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

    private async Task ExecuteCheckUpdatesAsync()
    {
        IsCheckingUpdate = true;
        IsUpdateAvailable = false;
        updateDownloadUrl = null;
        updateTagName = null;
        updateExpectedSha256 = null;
        preparedScriptPath = null;
        NotifyInstallReadyChanged();
        UpdateStatusText = "Vérification des mises à jour...";
        UpdateProgressValue = 0;

        try
        {
            string currentVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0";
            UpdateCheckResult result = await updateService.CheckForUpdateAsync(GithubRepo, currentVersion);
            if (result.VerificationFailed)
            {
                UpdateStatusText = "Vérification impossible. Vérifiez votre connexion.";
                return;
            }

            if (!result.HasUpdate || result.ReleaseInfo == null)
            {
                UpdateStatusText = "Application déjà à jour.";
                return;
            }

            updateTagName = result.ReleaseInfo.TagName;
            updateDownloadUrl = result.ReleaseInfo.DownloadUrl;
            updateExpectedSha256 = result.ReleaseInfo.ExpectedSha256;
            IsUpdateAvailable = !string.IsNullOrWhiteSpace(updateDownloadUrl)
                && !string.IsNullOrWhiteSpace(updateExpectedSha256);

            UpdateStatusText = IsUpdateAvailable
                ? $"Mise à jour disponible : {updateTagName}"
                : string.IsNullOrWhiteSpace(updateDownloadUrl)
                    ? $"Mise à jour détectée ({updateTagName}) sans binaire .exe."
                    : $"Mise à jour détectée ({updateTagName}) sans checksum SHA256.";
        }
        catch (Exception ex)
        {
            UpdateStatusText = $"Erreur mise à jour : {ex.Message}";
            loggerService.LogError("winui-update", "Update check exception.", ex);
        }
        finally
        {
            IsCheckingUpdate = false;
        }
    }

    private async Task ExecutePrepareUpdateAsync()
    {
        if (string.IsNullOrWhiteSpace(updateDownloadUrl))
        {
            UpdateStatusText = "Aucun lien de téléchargement disponible.";
            return;
        }

        if (string.IsNullOrWhiteSpace(updateExpectedSha256))
        {
            UpdateStatusText = "Checksum SHA256 indisponible pour cette mise à jour.";
            return;
        }

        string currentExePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
        if (string.IsNullOrWhiteSpace(currentExePath))
        {
            UpdateStatusText = "Impossible de déterminer le chemin de l'application.";
            return;
        }

        IsPreparingUpdate = true;
        UpdateProgressValue = 0;
        UpdateStatusText = "Préparation de la mise à jour...";
        preparedScriptPath = null;

        try
        {
            string targetVersion = string.IsNullOrWhiteSpace(updateTagName) ? "latest" : updateTagName.Trim();
            string downloadedPath = Path.Combine(Path.GetTempPath(), $"Panosse-{targetVersion}.exe");

            await foreach (UpdateDownloadProgress progress in updateOrchestrator.DownloadAndPrepareInstallAsync(
                               updateDownloadUrl,
                               currentExePath,
                               updateTagName,
                               updateExpectedSha256))
            {
                UpdateProgressValue = progress.ProgressPercent;
                UpdateStatusText = progress.Message;
            }

            UpdateInstallResult result = await updateOrchestrator.BuildInstallScriptAsync(downloadedPath, currentExePath, updateTagName);
            if (!result.Success || string.IsNullOrWhiteSpace(result.ScriptPath))
            {
                UpdateStatusText = string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? "Préparation de la mise à jour échouée."
                    : result.ErrorMessage;
                return;
            }

            preparedScriptPath = result.ScriptPath;
            UpdateStatusText = "Mise à jour préparée. Cliquez sur Installer.";
            NotifyInstallReadyChanged();
        }
        catch (Exception ex)
        {
            UpdateStatusText = $"Erreur préparation mise à jour : {ex.Message}";
            loggerService.LogError("winui-update", "Update preparation exception.", ex);
        }
        finally
        {
            IsPreparingUpdate = false;
        }
    }

    private void ExecuteInstallPreparedUpdate()
    {
        if (string.IsNullOrWhiteSpace(preparedScriptPath))
        {
            UpdateStatusText = "Aucun script de mise à jour préparé.";
            return;
        }

        UpdateStatusText = "Installation en cours...";
        updateOrchestrator.LaunchInstallerAndShutdown(preparedScriptPath);
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
            CleanupProfile = CleanupProfile
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
        settingsAutoSaveDebounceCts?.Cancel();
        settingsAutoSaveDebounceCts?.Dispose();
        settingsAutoSaveDebounceCts = null;
        scheduledCleanupLock.Dispose();
    }
}
