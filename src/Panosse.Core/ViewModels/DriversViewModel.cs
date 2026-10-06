using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Panosse.Core.Commands;
using Panosse.Services;

namespace Panosse.Core.ViewModels;

public sealed class DriversViewModel : INotifyPropertyChanged
{
    private static readonly int[] AgeOptionsDays = [30, 90, 180, 365];
    private static readonly string[] AgeOptionLabels = ["30 jours", "90 jours", "6 mois", "1 an"];

    private readonly IDriverCleanerService driverCleaner;
    private readonly PendingDriverCleanup pendingCleanup;
    private readonly ILoggerService logger;
    private DriverInventory? inventory;
    private bool isBusy;
    private int minimumAgeIndex = 1;
    private string statusText = "Lancez une analyse pour repérer les pilotes obsolètes et les périphériques cachés.";
    private string summaryText = string.Empty;
    private string selectionText = string.Empty;
    private string backupStatus = string.Empty;

    public DriversViewModel(IDriverCleanerService driverCleaner, PendingDriverCleanup pendingCleanup, ILoggerService logger)
    {
        this.driverCleaner = driverCleaner;
        this.pendingCleanup = pendingCleanup;
        this.logger = logger;
        IsElevated = driverCleaner.IsElevated;

        ScanCommand = new AsyncRelayCommand(
            executeAsync: ScanAsync,
            canExecute: () => !IsBusy,
            onException: ex => ReportError("Analyse des pilotes impossible", ex));
        CleanSelectedCommand = new AsyncRelayCommand(
            executeAsync: () => DeleteAsync(Items.Where(i => i.IsSelected).ToList()),
            canExecute: () => !IsBusy && Items.Any(i => i.IsSelected),
            onException: ex => ReportError("Suppression des pilotes interrompue", ex));
        SelectRecommendedCommand = new RelayCommand(() => SetSelection(item => item.IsLowRisk), () => !IsBusy);
        SelectNoneCommand = new RelayCommand(() => SetSelection(_ => false), () => !IsBusy);
        RestartAsAdminCommand = new RelayCommand(RestartAsAdmin, () => !IsElevated);
        RestoreLatestBackupCommand = new AsyncRelayCommand(
            executeAsync: RestoreLatestBackupAsync,
            canExecute: () => !IsBusy && IsElevated,
            onException: ex => ReportError("Restauration des pilotes impossible", ex));
        OpenBackupsCommand = new RelayCommand(OpenBackupsFolder);

        RefreshBackupStatus();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DriverIssueItem> Items { get; } = new();

    public ICommand ScanCommand { get; }
    public ICommand CleanSelectedCommand { get; }
    public ICommand SelectRecommendedCommand { get; }
    public ICommand SelectNoneCommand { get; }
    public ICommand RestartAsAdminCommand { get; }
    public ICommand RestoreLatestBackupCommand { get; }
    public ICommand OpenBackupsCommand { get; }

    /// <summary>Confirmation (titre, message, bouton principal) fournie par l'hôte WinUI.</summary>
    public Func<string, string, string, Task<bool>>? ConfirmActionAsync { get; set; }

    /// <summary>Relance élevée fournie par l'hôte ; renvoie false si l'utilisateur refuse l'UAC.</summary>
    public Func<bool>? RequestElevatedRestart { get; set; }

    public bool IsElevated { get; }
    public bool ShowElevationNotice => !IsElevated;
    public bool HasItems => Items.Count > 0;

    /// <summary>Une suppression attend d'être reprise après la relance en administrateur.</summary>
    public bool HasPendingCleanup => IsElevated && pendingCleanup.HasPending;

    public string DeleteSelectedText => $"🗑️ Supprimer la sélection ({Items.Count(i => i.IsSelected)})";

    public string DeleteHintText => IsElevated
        ? "Chaque pilote est sauvegardé avant suppression et un point de restauration est demandé."
        : "Windows demandera l'autorisation administrateur : Panosse redémarre puis termine la suppression tout seul.";

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetField(ref isBusy, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public int MinimumAgeIndex
    {
        get => minimumAgeIndex;
        set
        {
            int clamped = Math.Clamp(value, 0, AgeOptionsDays.Length - 1);
            if (SetField(ref minimumAgeIndex, clamped) && inventory is not null && !IsBusy)
            {
                ApplyAnalysis();
            }
        }
    }

    public string StatusText
    {
        get => statusText;
        private set => SetField(ref statusText, value);
    }

    public string SummaryText
    {
        get => summaryText;
        private set => SetField(ref summaryText, value);
    }

    public string SelectionText
    {
        get => selectionText;
        private set => SetField(ref selectionText, value);
    }

    public string BackupStatus
    {
        get => backupStatus;
        private set => SetField(ref backupStatus, value);
    }

    internal static string BuildSummary(IReadOnlyCollection<DriverIssue> issues, string ageLabel)
    {
        if (issues.Count == 0)
        {
            return $"Rien à nettoyer : aucun pilote obsolète ni périphérique absent depuis plus de {ageLabel}.";
        }

        int obsolete = issues.Count(i => i.Kind == DriverIssueKind.ObsoletePackage);
        int unused = issues.Count(i => i.Kind == DriverIssueKind.UnusedPackage);
        int phantom = issues.Count(i => i.Kind == DriverIssueKind.PhantomDevice);
        return $"{obsolete} pilote(s) obsolète(s), {unused} pilote(s) inutilisé(s), {phantom} périphérique(s) caché(s) absent(s) depuis plus de {ageLabel}.";
    }

    internal static string BuildConfirmMessage(IReadOnlyCollection<DriverIssue> issues, bool isElevated)
    {
        int packages = issues.Count(i => i.Kind != DriverIssueKind.PhantomDevice);
        int devices = issues.Count - packages;
        string question = issues.Count == 1
            ? $"Supprimer « {issues.First().Title} » ?"
            : $"Supprimer {packages} pilote(s) et {devices} périphérique(s) caché(s) ?";

        string message =
            question + "\n\n" +
            "• Un point de restauration système est demandé avant de commencer.\n" +
            "• Chaque pilote est exporté avant suppression (restaurable depuis cette page).\n" +
            "• Windows refuse de supprimer un pilote encore utilisé.\n" +
            "• Un périphérique caché réapparaît automatiquement s'il est rebranché.";
        if (!isElevated)
        {
            message += "\n\nWindows va demander l'autorisation administrateur : Panosse redémarre puis supprime automatiquement cette sélection.";
        }

        return message;
    }

    internal static string BuildResultMessage(DriverCleanResult result)
    {
        string message = $"✅ {result.PackagesRemoved} pilote(s) et {result.DevicesRemoved} périphérique(s) caché(s) supprimés";
        if (result.SkippedCount > 0)
        {
            message += $", {result.SkippedCount} conservé(s) par sécurité";
        }

        message += ".";
        if (result.RebootRequired)
        {
            message += " Redémarrez le PC pour terminer.";
        }

        return message;
    }

    private async Task ScanAsync()
    {
        IsBusy = true;
        try
        {
            StatusText = "🔎 Analyse des pilotes et des périphériques...";
            inventory = await driverCleaner.LoadInventoryAsync();
            ApplyAnalysis();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyAnalysis()
    {
        if (inventory is null)
        {
            return;
        }

        foreach (DriverIssueItem item in Items)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
        }

        Items.Clear();
        IReadOnlyList<DriverIssue> issues = driverCleaner.Analyze(inventory, AgeOptionsDays[MinimumAgeIndex]);
        foreach (DriverIssue issue in issues)
        {
            DriverIssueItem? item = null;
            item = new DriverIssueItem(issue)
            {
                DeleteCommand = new AsyncRelayCommand(
                    executeAsync: () => DeleteAsync([item!]),
                    canExecute: () => !IsBusy,
                    onException: ex => ReportError("Suppression interrompue", ex))
            };
            item.PropertyChanged += OnItemPropertyChanged;
            Items.Add(item);
        }

        SummaryText = BuildSummary(issues, AgeOptionLabels[MinimumAgeIndex]);
        StatusText = issues.Count == 0
            ? string.Empty
            : "Les éléments à faible risque sont déjà cochés. Cliquez sur « Supprimer la sélection », ou sur « Supprimer » à côté d'un élément.";
        OnPropertyChanged(nameof(HasItems));
        UpdateSelection();
    }

    /// <summary>Reprend, dans l'instance élevée, la suppression demandée avant la relance.</summary>
    public async Task ResumePendingCleanupAsync()
    {
        if (!HasPendingCleanup || IsBusy)
        {
            return;
        }

        try
        {
            IReadOnlySet<string> targets = pendingCleanup.Take();
            IsBusy = true;
            try
            {
                StatusText = "🔎 Vérification de la sélection avant suppression...";
                inventory = await driverCleaner.LoadInventoryAsync();
                ApplyAnalysis();
            }
            finally
            {
                IsBusy = false;
            }

            SetSelection(item => targets.Contains(item.Issue.Target));
            List<DriverIssue> selected = Items.Where(i => i.IsSelected).Select(i => i.Issue).ToList();
            if (selected.Count == 0)
            {
                StatusText = "Les éléments choisis ne sont plus détectés : rien à supprimer.";
                return;
            }

            await RunCleanupAsync(selected);
        }
        catch (Exception ex)
        {
            ReportError("Suppression des pilotes interrompue", ex);
        }
    }

    private async Task DeleteAsync(IReadOnlyList<DriverIssueItem> items)
    {
        List<DriverIssue> selected = items.Select(i => i.Issue).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        string message = BuildConfirmMessage(selected, IsElevated);
        if (ConfirmActionAsync is not null &&
            !await ConfirmActionAsync("Supprimer les pilotes", message, IsElevated ? "Supprimer" : "Autoriser et supprimer"))
        {
            StatusText = "Suppression annulée.";
            return;
        }

        if (!IsElevated)
        {
            pendingCleanup.Save(selected.Select(i => i.Target));
            StatusText = "🛡️ Relance de Panosse en administrateur...";
            if (RequestElevatedRestart?.Invoke() != true)
            {
                pendingCleanup.Clear();
                StatusText = "Autorisation administrateur refusée : aucun pilote supprimé.";
            }

            return;
        }

        await RunCleanupAsync(selected);
    }

    private async Task RunCleanupAsync(IReadOnlyList<DriverIssue> selected)
    {
        IsBusy = true;
        try
        {
            var progress = new Progress<string>(update => StatusText = update);
            DriverCleanResult result = await driverCleaner.CleanAsync(selected, progress);

            string resultSummary = result.Failures.Count == 0
                ? BuildResultMessage(result)
                : $"{BuildResultMessage(result)}\n" + string.Join("\n", result.Failures.Take(4).Select(f => "• " + f));
            string restoreNote = result.RestorePointRequested
                ? "Point de restauration système créé avant le nettoyage."
                : "Point de restauration non créé (protection système désactivée ou limite de 24 h) ; les pilotes supprimés restent sauvegardés.";
            RefreshBackupStatus();

            StatusText = "🔎 Nouvelle analyse...";
            inventory = await driverCleaner.LoadInventoryAsync();
            ApplyAnalysis();
            SummaryText = resultSummary;
            StatusText = restoreNote;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RestoreLatestBackupAsync()
    {
        string? latest = driverCleaner.GetLatestBackupFolder();
        if (latest is null)
        {
            StatusText = "Aucune sauvegarde de pilotes à restaurer.";
            return;
        }

        if (ConfirmActionAsync is not null &&
            !await ConfirmActionAsync(
                "Restaurer des pilotes",
                $"Remettre dans Windows les pilotes sauvegardés le {Directory.GetCreationTime(latest):dd/MM/yyyy à HH:mm} ?",
                "Restaurer"))
        {
            return;
        }

        IsBusy = true;
        try
        {
            StatusText = "♻️ Restauration des pilotes...";
            (bool success, string message) = await driverCleaner.RestoreBackupAsync(latest);
            StatusText = success ? $"✅ {message}" : $"❌ {message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RestartAsAdmin()
    {
        if (RequestElevatedRestart?.Invoke() != true)
        {
            StatusText = "Relance en administrateur annulée.";
        }
    }

    private void SetSelection(Func<DriverIssueItem, bool> predicate)
    {
        foreach (DriverIssueItem item in Items)
        {
            item.IsSelected = predicate(item);
        }
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DriverIssueItem.IsSelected))
        {
            UpdateSelection();
        }
    }

    private void UpdateSelection()
    {
        int count = Items.Count(i => i.IsSelected);
        SelectionText = Items.Count == 0 ? string.Empty : $"{count} élément(s) sélectionné(s) sur {Items.Count}";
        OnPropertyChanged(nameof(DeleteSelectedText));
        (CleanSelectedCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }

    private void RefreshBackupStatus()
    {
        string? latest = driverCleaner.GetLatestBackupFolder();
        BackupStatus = latest is null
            ? "Aucune sauvegarde de pilotes pour l'instant."
            : $"Dernière sauvegarde : {Path.GetFileName(latest)}";
    }

    private void OpenBackupsFolder()
    {
        try
        {
            Directory.CreateDirectory(driverCleaner.BackupDirectory);
            Process.Start(new ProcessStartInfo
            {
                FileName = driverCleaner.BackupDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ReportError("Impossible d'ouvrir le dossier des sauvegardes de pilotes", ex);
        }
    }

    private void ReportError(string message, Exception ex)
    {
        logger.LogError("winui-drivers", message, ex);
        StatusText = $"❌ {message} : {ex.Message}";
        IsBusy = false;
    }

    private void RaiseCommandStates()
    {
        (ScanCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (CleanSelectedCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (SelectRecommendedCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (SelectNoneCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (RestoreLatestBackupCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        foreach (DriverIssueItem item in Items)
        {
            (item.DeleteCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged(string? propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
