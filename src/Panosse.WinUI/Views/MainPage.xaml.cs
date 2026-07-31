using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Panosse.Core.ViewModels;
using Panosse.Services;
using Panosse.WinUI.Services;

namespace Panosse.WinUI.Views
{
    public partial class MainPage : Page
    {
        public ShellViewModel ViewModel { get; }
        public string AppVersionText { get; } = $"v{Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0"}";
        private readonly IBrowserProcessService browserProcessService;
        private List<string> navigateursEnCours = new();
        private bool isClosingBrowsers;
        private CancellationTokenSource? browserCloseCts;

        public MainPage()
        {
            ViewModel = App.GetRequiredService<ShellViewModel>();
            browserProcessService = App.GetRequiredService<IBrowserProcessService>();
            ViewModel.UiMarshal = action =>
            {
                _ = DispatcherQueue.TryEnqueue(() => action());
            };
            ViewModel.ConfirmCleanupAsync = ConfirmCleanupAsync;
            InitializeComponent();
            this.Loaded += MainPage_Loaded;
            this.KeyDown += MainPage_KeyDown;
            this.Unloaded += MainPage_Unloaded;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            DisplayLayoutHelper.ApplyWindowSize(App.MainWindow);
            NavList.SelectedIndex = 0;
            UpdateMopAnimation();
            await CheckBrowsersAsync();

            if (ViewModel.CheckUpdatesOnStartup)
            {
                _ = CheckForUpdatesAsync(showDialog: false);
            }
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            browserCloseCts?.Cancel();
            browserCloseCts?.Dispose();
            browserCloseCts = null;
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ShellViewModel.IsBusy))
            {
                UpdateMopAnimation();
                if (!ViewModel.IsBusy && ViewModel.IsSuccessStatus)
                {
                    NotifyCleanupCompleted();
                }
            }
        }

        private void NotifyCleanupCompleted()
        {
            if (ViewModel.PlaySuccessSound)
            {
                _ = MessageBeep(MbIconAsterisk);
            }

            if (ViewModel.ShowTrayNotifications)
            {
                try
                {
                    App.GetRequiredService<ISystemTrayService>().ShowNotification(
                        "Panosse",
                        ViewModel.LastRunSummary);
                }
                catch
                {
                    // Notification optionnelle.
                }
            }
        }

        // Lance/stoppe le balancement de la serpillère selon l'état de nettoyage.
        private void UpdateMopAnimation()
        {
            if (ViewModel.IsBusy)
            {
                MopCleaningStoryboard.Begin();
            }
            else
            {
                MopCleaningStoryboard.Stop();
                if (MopTransform is not null)
                {
                    MopTransform.Rotation = 0;
                    MopTransform.TranslateY = 0;
                }
            }
        }

        // ==================== Navigation sidebar ====================

        private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowPanel(NavList.SelectedIndex);
        }

        private void ShowPanel(int index)
        {
            if (HomePanel is null)
            {
                return;
            }

            HomePanel.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
            SettingsPanel.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
            HistoryPanel.Visibility = index == 2 ? Visibility.Visible : Visibility.Collapsed;
            AboutPanel.Visibility = index == 3 ? Visibility.Visible : Visibility.Collapsed;

            if (index == 1 || index == 2)
            {
                ViewModel.RefreshHistoryCommand.Execute(null);
            }
        }

        private void MainPage_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape && NavList.SelectedIndex != 0)
            {
                NavList.SelectedIndex = 0;
                e.Handled = true;
            }
        }

        // ==================== Navigateurs ====================

        private async Task CheckBrowsersAsync()
        {
            try
            {
                IReadOnlyList<string> browsers = await browserProcessService.GetRunningBrowsersAsync();
                navigateursEnCours = browsers.ToList();
            }
            catch
            {
                navigateursEnCours = new List<string>();
            }

            if (navigateursEnCours.Count > 0)
            {
                string browsers = string.Join(" et ", navigateursEnCours);
                if (BrowserWarningLink.Content is TextBlock warningText)
                {
                    warningText.Text = $"⚠️ Veuillez fermer {browsers} pour un nettoyage complet (cliquez ici pour fermer automatiquement)";
                }
                BrowserWarningLink.Visibility = Visibility.Visible;
                BrowserWarningLink.IsEnabled = !isClosingBrowsers;
            }
            else
            {
                BrowserWarningLink.Visibility = Visibility.Collapsed;
            }
        }

        private async Task<bool> ConfirmCleanupAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Confirmer le nettoyage",
                Content = new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.WrapWholeWords,
                    MaxWidth = 420
                },
                PrimaryButtonText = "Nettoyer",
                CloseButtonText = "Annuler",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };

            ContentDialogResult result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        private async void BrowserWarningLink_Click(object sender, RoutedEventArgs e)
        {
            if (isClosingBrowsers || navigateursEnCours.Count == 0)
            {
                return;
            }

            isClosingBrowsers = true;
            BrowserWarningLink.IsEnabled = false;
            ViewModel.StatusText = "Fermeture des navigateurs en cours...";

            browserCloseCts?.Cancel();
            browserCloseCts?.Dispose();
            browserCloseCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            try
            {
                BrowserCloseResult result = await browserProcessService.CloseBrowsersAsync(
                    navigateursEnCours,
                    browserCloseCts.Token);

                await CheckBrowsersAsync();

                if (result.AllClosed || navigateursEnCours.Count == 0)
                {
                    ViewModel.StatusText = "Navigateurs fermés. Nettoyage complet possible.";
                }
                else
                {
                    string remaining = string.Join(" et ", result.RemainingBrowsers);
                    ViewModel.StatusText = $"{remaining} reste ouvert. Fermez-le manuellement puis réessayez.";
                }
            }
            catch (OperationCanceledException)
            {
                await CheckBrowsersAsync();
                ViewModel.StatusText = navigateursEnCours.Count == 0
                    ? "Navigateurs fermés. Nettoyage complet possible."
                    : "Fermeture interrompue. Fermez les navigateurs manuellement.";
            }
            catch
            {
                await CheckBrowsersAsync();
                ViewModel.StatusText = "Erreur lors de la fermeture automatique des navigateurs.";
            }
            finally
            {
                isClosingBrowsers = false;
                BrowserWarningLink.IsEnabled = navigateursEnCours.Count > 0;
            }
        }

        public void TriggerCleanupFromTray()
        {
            if (!ViewModel.IsBusy)
            {
                _ = ViewModel.RunCleanupFromTrayAsync();
            }
        }

        private void MenuQuit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        // ==================== Mises à jour ====================

        private async Task CheckForUpdatesAsync(bool showDialog)
        {
            if (!ViewModel.CheckUpdatesCommand.CanExecute(null))
            {
                ViewModel.StatusText = "Vérification déjà en cours...";
                return;
            }

            ViewModel.StatusText = "Vérification des mises à jour en cours...";
            ViewModel.CheckUpdatesCommand.Execute(null);

            int guard = 0;
            while (ViewModel.IsCheckingUpdate && guard < 200)
            {
                await Task.Delay(50);
                guard++;
            }

            string result = string.IsNullOrWhiteSpace(ViewModel.UpdateStatusText)
                ? "Vérification terminée."
                : ViewModel.UpdateStatusText;
            ViewModel.StatusText = result;

            if (!showDialog)
            {
                return;
            }

            var dialog = new ContentDialog
            {
                Title = "Mises à jour",
                Content = result,
                CloseButtonText = ViewModel.IsUpdateAvailable ? "Plus tard" : "OK",
                XamlRoot = this.XamlRoot
            };

            if (ViewModel.IsUpdateAvailable)
            {
                dialog.PrimaryButtonText = "Télécharger";
            }

            ContentDialogResult dialogResult = await dialog.ShowAsync();
            if (dialogResult == ContentDialogResult.Primary && ViewModel.IsUpdateAvailable)
            {
                await RunUpdateInstallAsync();
            }
        }

        private async Task RunUpdateInstallAsync()
        {
            if (!ViewModel.PrepareUpdateCommand.CanExecute(null))
            {
                return;
            }

            ViewModel.PrepareUpdateCommand.Execute(null);

            int guard = 0;
            while (ViewModel.IsPreparingUpdate && guard < 1200)
            {
                await Task.Delay(100);
                guard++;
            }

            if (!ViewModel.InstallPreparedUpdateCommand.CanExecute(null))
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Mise à jour",
                    Content = ViewModel.UpdateStatusText,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                _ = await errorDialog.ShowAsync();
                return;
            }

            ViewModel.InstallPreparedUpdateCommand.Execute(null);
        }

        private async void AboutCheckUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            AboutCheckUpdatesButton.IsEnabled = false;
            AboutCheckUpdatesButton.Content = "Vérification...";
            AboutLastCheckText.Text = $"Dernière vérification : {DateTime.Now:HH:mm:ss} (en cours)";

            try
            {
                await CheckForUpdatesAsync(showDialog: false);
                AboutLastCheckText.Text = $"Dernière vérification : {DateTime.Now:HH:mm:ss}";
            }
            finally
            {
                AboutCheckUpdatesButton.IsEnabled = true;
                AboutCheckUpdatesButton.Content = "🔍 Vérifier les mises à jour";
            }
        }

        private void AboutGithubButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _ = Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/barbarom84-ai/panosse",
                    UseShellExecute = true
                });
            }
            catch
            {
                ViewModel.StatusText = "Impossible d'ouvrir GitHub.";
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool MessageBeep(uint uType);

        private const uint MbIconAsterisk = 0x00000040;
    }
}
