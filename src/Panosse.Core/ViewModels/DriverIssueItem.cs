using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Panosse.Services;

namespace Panosse.Core.ViewModels;

public sealed class DriverIssueItem : INotifyPropertyChanged
{
    private bool isSelected;

    public DriverIssueItem(DriverIssue issue)
    {
        Issue = issue;
        isSelected = issue.RiskLevel == "Low";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public DriverIssue Issue { get; }
    public string Title => Issue.Title;
    public string Details => Issue.Details;
    public string KindLabel => Issue.KindLabel;
    public bool IsLowRisk => Issue.RiskLevel == "Low";
    public string RiskLabel => IsLowRisk ? "Risque faible" : "Risque moyen";

    /// <summary>Suppression de cet élément seul, fournie par <see cref="DriversViewModel"/>.</summary>
    public ICommand? DeleteCommand { get; init; }

    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if (isSelected == value)
            {
                return;
            }

            isSelected = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
