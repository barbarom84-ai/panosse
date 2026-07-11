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

public sealed class ShellViewModel : INotifyPropertyChanged, IDisposable
{
    private string statusText = "Pret";
    private string buttonText = "Passer la panosse";
    private double progressValue;
    private bool isBusy;
    private bool previewModeEnabled;
    private string exclusionPatterns = string.Empty;
    private string lastRunSummary = "Aucun nettoyage execute.";
    private string historySummary = "Aucun historique disponible.";
    private string updateStatusText = "Mise a jour non verifiee.";
    private double updateProgressValue;
    private bool isUpdateAvailable;
    private bool isCheckingUpdate;
    private bool isPreparingUpdate;
    private bool isUpdatesExpanded;
    private bool userChangedUpdatesExpanded;
    private bool suppressUpdatesExpandedTracking;
    private bool isTaskMessagesExpanded;
    private bool isHistoryItemsExpanded;
    private bool userChangedTaskMessagesExpanded;
    private bool userChangedHistoryItemsExpanded;
    private bool suppressTaskMessagesExpandedTracking;
    private bool suppressHistoryItemsExpandedTracking;
    private bool suppressUiLayoutPersistence;
    private bool suppressSettingsAutoSave;
    private CancellationTokenSource? uiLayoutPersistDebounceCts;
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
            executeAsync: ExecuteCleanupAsync,
            canExecute: () => !IsBusy,
            onException: ex => loggerService.LogError("winui-cleanup", "Cleanup command failed.", ex));

        SaveSettingsCommand = new RelayCommand(SaveSettings);
        RefreshHistoryCommand = new RelayCommand(RefreshHistory);
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

    public ICommand RunCleanupCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand RefreshHistoryCommand { get; }
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsProgressVisible)));
            }
        }
    }

    public bool IsProgressVisible => IsBusy || ProgressValue > 0;

    public bool HasTaskMessages => TaskMessages.Count > 0;

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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UpdateHeaderText)));
                (PrepareUpdateCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string UpdateHeaderText => IsUpdateAvailable ? "Mises a jour - Nouveau" : "Mises a jour";

    public bool IsUpdatesExpanded
    {
        get => isUpdatesExpanded;
        set
        {
            if (SetField(ref isUpdatesExpanded, value) && !suppressUpdatesExpandedTracking)
            {
                userChangedUpdatesExpanded = true;
                SchedulePersistUiLayoutPreferences();
            }
        }
    }

    public string TaskMessagesHeaderText => $"Messages de tache ({TaskMessages.Count})";

    public string HistoryItemsHeaderText => $"Historique recent ({HistoryItems.Count})";

    public string TaskMessagesEmptyText => TaskMessages.Count == 0 ? "Aucun message de tache." : string.Empty;

    public string HistoryItemsEmptyText => HistoryItems.Count == 0 ? "Aucun element d'historique." : string.Empty;

    public bool IsTaskMessagesExpanded
    {
        get => isTaskMessagesExpanded;
        set
        {
            if (SetField(ref isTaskMessagesExpanded, value) && !suppressTaskMessagesExpandedTracking)
            {
                userChangedTaskMessagesExpanded = true;
                SchedulePersistUiLayoutPreferences();
            }
        }
    }

    public bool IsHistoryItemsExpanded
    {
        get => isHistoryItemsExpanded;
        set
        {
            if (SetField(ref isHistoryItemsExpanded, value) && !suppressHistoryItemsExpandedTracking)
            {
                userChangedHistoryItemsExpanded = true;
                SchedulePersistUiLayoutPreferences();
            }
        }
    }

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

    private async Task ExecuteCleanupAsync()
    {
        IsBusy = true;
        ButtonText = PreviewModeEnabled ? "Previsualisation..." : "Nettoyage...";
        StatusText = "Preparation...";
        ProgressValue = 0;
        TaskMessages.Clear();

        var options = new CleanupExecutionOptions
        {
            PreviewOnly = PreviewModeEnabled,
            ExclusionPatterns = GetExclusions()
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
                }

                double ratio = update.TotalSteps > 0
                    ? (double)update.StepIndex / update.TotalSteps
                    : 0;
                ProgressValue = Math.Round(ratio * 100, 2);
                TaskMessages.Add(update.Message);
            }

            double totalMb = Math.Round(totalFreedBytes / 1024.0 / 1024.0, 2);
            LastRunSummary = PreviewModeEnabled
                ? $"Previsualisation terminee: {totalMb} Mo estimes."
                : $"Nettoyage termine: {totalMb} Mo liberes.";
            StatusText = LastRunSummary;
            RefreshHistory();
        }
        catch (Exception ex)
        {
            StatusText = $"Erreur: {ex.Message}";
            TaskMessages.Add(StatusText);
            loggerService.LogError("winui-cleanup", "Cleanup execution failed.", ex);
        }
        finally
        {
            IsBusy = false;
            ButtonText = "Passer la panosse";
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

            if (settings.HasSavedUiLayoutPreferences)
            {
                suppressUiLayoutPersistence = true;
                suppressTaskMessagesExpandedTracking = true;
                suppressHistoryItemsExpandedTracking = true;
                suppressUpdatesExpandedTracking = true;

                IsTaskMessagesExpanded = settings.TaskMessagesExpanded;
                IsHistoryItemsExpanded = settings.HistoryItemsExpanded;
                IsUpdatesExpanded = settings.UpdatesExpanded;

                suppressTaskMessagesExpandedTracking = false;
                suppressHistoryItemsExpandedTracking = false;
                suppressUpdatesExpandedTracking = false;
                suppressUiLayoutPersistence = false;

                userChangedTaskMessagesExpanded = true;
                userChangedHistoryItemsExpanded = true;
                userChangedUpdatesExpanded = true;
            }

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
                StatusText = "Parametres sauvegardes.";
            }
        }
        catch (Exception ex)
        {
            loggerService.LogError("winui-settings", "Failed to save settings.", ex);
            if (showStatus)
            {
                StatusText = "Erreur de sauvegarde des parametres.";
            }
        }
    }

    private void RefreshHistory()
    {
        try
        {
            IReadOnlyList<OperationHistoryEntry> entries = historyService.GetRecentEntries(8);
            HistoryItems.Clear();
            foreach (OperationHistoryEntry entry in entries)
            {
                double mb = Math.Round(entry.FreedBytes / 1024.0 / 1024.0, 2);
                HistoryItems.Add($"{entry.TimestampUtc.ToLocalTime():dd/MM HH:mm} | {entry.OperationType} | {entry.Outcome} | {mb} Mo");
            }

            HistorySummary = entries.Count == 0
                ? "Aucun historique disponible."
                : $"Derniere operation: {entries[0].TimestampUtc.ToLocalTime():dd/MM/yyyy HH:mm}";
        }
        catch (Exception ex)
        {
            loggerService.LogError("winui-history", "Failed to refresh history.", ex);
            HistorySummary = "Historique indisponible.";
        }
    }

    private async Task ExecuteCheckUpdatesAsync()
    {
        IsCheckingUpdate = true;
        IsUpdateAvailable = false;
        updateDownloadUrl = null;
        updateTagName = null;
        updateExpectedSha256 = null;
        UpdateStatusText = "Verification des mises a jour...";
        UpdateProgressValue = 0;

        try
        {
            string currentVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0";
            UpdateCheckResult result = await updateService.CheckForUpdateAsync(GithubRepo, currentVersion);
            if (result.VerificationFailed)
            {
                UpdateStatusText = "Verification impossible. Verifiez votre connexion.";
                return;
            }

            if (!result.HasUpdate || result.ReleaseInfo == null)
            {
                UpdateStatusText = "Application deja a jour.";
                return;
            }

            updateTagName = result.ReleaseInfo.TagName;
            updateDownloadUrl = result.ReleaseInfo.DownloadUrl;
            updateExpectedSha256 = result.ReleaseInfo.ExpectedSha256;
            IsUpdateAvailable = !string.IsNullOrWhiteSpace(updateDownloadUrl)
                && !string.IsNullOrWhiteSpace(updateExpectedSha256);
            if (IsUpdateAvailable && !userChangedUpdatesExpanded && !IsUpdatesExpanded)
            {
                suppressUpdatesExpandedTracking = true;
                IsUpdatesExpanded = true;
                suppressUpdatesExpandedTracking = false;
            }

            UpdateStatusText = IsUpdateAvailable
                ? $"Mise a jour disponible: {updateTagName}"
                : string.IsNullOrWhiteSpace(updateDownloadUrl)
                    ? $"Mise a jour detectee ({updateTagName}) sans binaire .exe."
                    : $"Mise a jour detectee ({updateTagName}) sans checksum SHA256.";
        }
        catch (Exception ex)
        {
            UpdateStatusText = $"Erreur update: {ex.Message}";
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
            UpdateStatusText = "Aucun lien de telechargement disponible.";
            return;
        }

        if (string.IsNullOrWhiteSpace(updateExpectedSha256))
        {
            UpdateStatusText = "Checksum SHA256 indisponible pour cette mise a jour.";
            return;
        }

        string currentExePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
        if (string.IsNullOrWhiteSpace(currentExePath))
        {
            UpdateStatusText = "Impossible de determiner le chemin de l'application.";
            return;
        }

        IsPreparingUpdate = true;
        UpdateProgressValue = 0;
        UpdateStatusText = "Preparation de la mise a jour...";
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
                    ? "Preparation de la mise a jour echouee."
                    : result.ErrorMessage;
                return;
            }

            preparedScriptPath = result.ScriptPath;
            UpdateStatusText = "Mise a jour preparee. Script pret.";
            (InstallPreparedUpdateCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
        catch (Exception ex)
        {
            UpdateStatusText = $"Erreur preparation update: {ex.Message}";
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
            UpdateStatusText = "Aucun script de mise a jour prepare.";
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
            scheduledCleanupTimer.Elapsed += (_, _) => _ = ExecuteScheduledCleanupAsync();
            scheduledCleanupTimer.Start();
            SchedulerStatusText = $"Planifie toutes les {ScheduledCleanupIntervalHours}h.";
        }
        catch (Exception ex)
        {
            loggerService.LogError("winui-scheduler", "Failed to configure scheduled cleanup.", ex);
            SchedulerStatusText = "Erreur de planification.";
        }
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

            StatusText = "Nettoyage planifie en cours...";
            await ExecuteCleanupAsync();
            SchedulerStatusText = $"Dernier run planifie: {DateTime.Now:dd/MM HH:mm}";
        }
        catch (Exception ex)
        {
            loggerService.LogError("winui-scheduler", "Scheduled cleanup failed.", ex);
            SchedulerStatusText = "Echec du run planifie.";
        }
        finally
        {
            scheduledCleanupLock.Release();
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
            loggerService.LogError("winui-diagnostics", "Failed to open logs folder.", ex);
            StatusText = "Impossible d'ouvrir le dossier de logs.";
        }
    }

    private void OnTaskMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (TaskMessages.Count > 0 && !userChangedTaskMessagesExpanded && !IsTaskMessagesExpanded)
        {
            suppressTaskMessagesExpandedTracking = true;
            IsTaskMessagesExpanded = true;
            suppressTaskMessagesExpandedTracking = false;
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TaskMessagesHeaderText)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TaskMessagesEmptyText)));
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
            HasSavedUiLayoutPreferences = true,
            TaskMessagesExpanded = IsTaskMessagesExpanded,
            HistoryItemsExpanded = IsHistoryItemsExpanded,
            UpdatesExpanded = IsUpdatesExpanded
        };
    }

    private void OnHistoryItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (HistoryItems.Count > 0 && !userChangedHistoryItemsExpanded && !IsHistoryItemsExpanded)
        {
            suppressHistoryItemsExpandedTracking = true;
            IsHistoryItemsExpanded = true;
            suppressHistoryItemsExpandedTracking = false;
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HistoryItemsHeaderText)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HistoryItemsEmptyText)));
    }

    private void PersistUiLayoutPreferencesBestEffort()
    {
        if (suppressUiLayoutPersistence)
        {
            return;
        }

        try
        {
            AppSettings settings = settingsService.Load();
            settings.HasSavedUiLayoutPreferences = true;
            settings.TaskMessagesExpanded = IsTaskMessagesExpanded;
            settings.HistoryItemsExpanded = IsHistoryItemsExpanded;
            settings.UpdatesExpanded = IsUpdatesExpanded;
            settingsService.Save(settings);
        }
        catch (Exception ex)
        {
            loggerService.LogError("winui-settings", "Failed to persist UI layout preferences.", ex);
        }
    }

    private void SchedulePersistUiLayoutPreferences()
    {
        uiLayoutPersistDebounceCts?.Cancel();
        uiLayoutPersistDebounceCts?.Dispose();

        var cts = new CancellationTokenSource();
        uiLayoutPersistDebounceCts = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(350, cts.Token);
                PersistUiLayoutPreferencesBestEffort();
            }
            catch (OperationCanceledException)
            {
                // Intentionally ignored: a newer UI change superseded this save.
            }
        });
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
        uiLayoutPersistDebounceCts?.Cancel();
        uiLayoutPersistDebounceCts?.Dispose();
        uiLayoutPersistDebounceCts = null;
        settingsAutoSaveDebounceCts?.Cancel();
        settingsAutoSaveDebounceCts?.Dispose();
        settingsAutoSaveDebounceCts = null;
        scheduledCleanupLock.Dispose();
    }
}
