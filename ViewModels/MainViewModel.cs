using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

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

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<string> TaskMessages { get; } = new();

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
