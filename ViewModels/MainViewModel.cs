using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;

namespace Panosse.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private string buttonText = "Passer la panosse";
    private string statusText = string.Empty;
    private Brush statusForeground = new SolidColorBrush(Color.FromRgb(117, 117, 117));
    private Visibility progressVisibility = Visibility.Collapsed;
    private bool isProgressIndeterminate;
    private double progressValue;
    private Brush progressForeground = new SolidColorBrush(Color.FromRgb(33, 150, 243));
    private string versionText = "v1.0.0";
    private string updateMessage = "Une nouvelle version est disponible !";
    private Visibility downloadProgressVisibility = Visibility.Collapsed;
    private double downloadProgressValue;
    private string checkUpdatesButtonText = "🔍 Vérifier les mises à jour";
    private Brush checkUpdatesButtonBackground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
    private bool isCheckUpdatesButtonEnabled = true;
    private string lastUpdateCheckText = "Dernière vérification : jamais";
    private bool checkUpdatesOnStartup = true;
    private bool playSuccessSound = true;
    private bool showTrayNotifications = true;
    private bool previewModeEnabled;
    private string exclusionPatterns = string.Empty;
    private bool enableScheduledCleanup;
    private int scheduledCleanupIntervalHours = 24;
    private string historySummary = "Aucun historique disponible.";

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<string> TaskMessages { get; } = new();

    public ICommand? CleanCommand { get; set; }
    public ICommand? MinimizeToTrayCommand { get; set; }
    public ICommand? QuitCommand { get; set; }
    public ICommand? OpenAboutCommand { get; set; }
    public ICommand? CloseAboutCommand { get; set; }
    public ICommand? RefreshDetectionCommand { get; set; }
    public ICommand? OpenGitHubCommand { get; set; }
    public ICommand? CheckUpdatesCommand { get; set; }
    public ICommand? InstallUpdateCommand { get; set; }
    public ICommand? CloseUpdateBarCommand { get; set; }
    public ICommand? OpenSettingsCommand { get; set; }
    public ICommand? CloseSettingsCommand { get; set; }
    public ICommand? SaveSettingsCommand { get; set; }
    public ICommand? ShowHistoryCommand { get; set; }

    public string ButtonText
    {
        get => buttonText;
        set => SetField(ref buttonText, value);
    }

    public string StatusText
    {
        get => statusText;
        set => SetField(ref statusText, value);
    }

    public Brush StatusForeground
    {
        get => statusForeground;
        set => SetField(ref statusForeground, value);
    }

    public Visibility ProgressVisibility
    {
        get => progressVisibility;
        set => SetField(ref progressVisibility, value);
    }

    public bool IsProgressIndeterminate
    {
        get => isProgressIndeterminate;
        set => SetField(ref isProgressIndeterminate, value);
    }

    public double ProgressValue
    {
        get => progressValue;
        set => SetField(ref progressValue, value);
    }

    public Brush ProgressForeground
    {
        get => progressForeground;
        set => SetField(ref progressForeground, value);
    }

    public string VersionText
    {
        get => versionText;
        set => SetField(ref versionText, value);
    }

    public string UpdateMessage
    {
        get => updateMessage;
        set => SetField(ref updateMessage, value);
    }

    public Visibility DownloadProgressVisibility
    {
        get => downloadProgressVisibility;
        set => SetField(ref downloadProgressVisibility, value);
    }

    public double DownloadProgressValue
    {
        get => downloadProgressValue;
        set => SetField(ref downloadProgressValue, value);
    }

    public string CheckUpdatesButtonText
    {
        get => checkUpdatesButtonText;
        set => SetField(ref checkUpdatesButtonText, value);
    }

    public Brush CheckUpdatesButtonBackground
    {
        get => checkUpdatesButtonBackground;
        set => SetField(ref checkUpdatesButtonBackground, value);
    }

    public bool IsCheckUpdatesButtonEnabled
    {
        get => isCheckUpdatesButtonEnabled;
        set => SetField(ref isCheckUpdatesButtonEnabled, value);
    }

    public string LastUpdateCheckText
    {
        get => lastUpdateCheckText;
        set => SetField(ref lastUpdateCheckText, value);
    }

    public bool CheckUpdatesOnStartup
    {
        get => checkUpdatesOnStartup;
        set => SetField(ref checkUpdatesOnStartup, value);
    }

    public bool PlaySuccessSound
    {
        get => playSuccessSound;
        set => SetField(ref playSuccessSound, value);
    }

    public bool ShowTrayNotifications
    {
        get => showTrayNotifications;
        set => SetField(ref showTrayNotifications, value);
    }

    public bool PreviewModeEnabled
    {
        get => previewModeEnabled;
        set => SetField(ref previewModeEnabled, value);
    }

    public string ExclusionPatterns
    {
        get => exclusionPatterns;
        set => SetField(ref exclusionPatterns, value);
    }

    public bool EnableScheduledCleanup
    {
        get => enableScheduledCleanup;
        set => SetField(ref enableScheduledCleanup, value);
    }

    public int ScheduledCleanupIntervalHours
    {
        get => scheduledCleanupIntervalHours;
        set => SetField(ref scheduledCleanupIntervalHours, value);
    }

    public string HistorySummary
    {
        get => historySummary;
        set => SetField(ref historySummary, value);
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
