using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Panosse.Core.ViewModels;
using Panosse.WinUI.Services;

namespace Panosse.WinUI.Views
{
    public partial class MainPage : Page
    {
        public ShellViewModel ViewModel { get; }
        public string AppVersionText { get; } = $"v{Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0"}";
        private List<string> navigateursEnCours = new();
        private bool isClosingBrowsers;

        public MainPage()
        {
            ViewModel = App.GetRequiredService<ShellViewModel>();
            InitializeComponent();
            this.Loaded += MainPage_Loaded;
            this.KeyDown += MainPage_KeyDown;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            DisplayLayoutHelper.ApplyWindowSize(App.MainWindow);
            await CheckBrowsersAsync();
        }

        private async Task CheckBrowsersAsync()
        {
            navigateursEnCours = await Task.Run(() =>
            {
                var browsers = new List<string>();
                try
                {
                    var processes = Process.GetProcesses();
                    if (processes.Any(p => p.ProcessName.ToLower().Contains("chrome"))) browsers.Add("Chrome");
                    if (processes.Any(p => p.ProcessName.ToLower().Contains("msedge"))) browsers.Add("Edge");
                }
                catch { }
                return browsers;
            });

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

        private async void BrowserWarningLink_Click(object sender, RoutedEventArgs e)
        {
            if (isClosingBrowsers || navigateursEnCours.Count == 0)
            {
                return;
            }

            isClosingBrowsers = true;
            BrowserWarningLink.IsEnabled = false;
            ViewModel.StatusText = "Fermeture des navigateurs en cours...";

            try
            {
                foreach (var browser in navigateursEnCours)
                {
                    string processName = browser == "Chrome" ? "chrome" : "msedge";
                    var processes = Process.GetProcesses().Where(p => p.ProcessName.ToLower().Contains(processName));
                    foreach (var process in processes)
                    {
                        try
                        {
                            process.CloseMainWindow();
                            if (!process.WaitForExit(1000))
                            {
                                process.Kill();
                            }
                        }
                        catch { }
                    }
                }

                await Task.Delay(1000);
                await CheckBrowsersAsync();
                ViewModel.StatusText = navigateursEnCours.Count == 0
                    ? "Navigateurs fermes. Nettoyage complet possible."
                    : "Certains navigateurs restent ouverts. Fermez-les manuellement.";
            }
            catch
            {
                ViewModel.StatusText = "Erreur lors de la fermeture automatique des navigateurs.";
            }
            finally
            {
                isClosingBrowsers = false;
                BrowserWarningLink.IsEnabled = true;
            }
        }

        public void TriggerCleanupFromTray()
        {
            if (ViewModel.RunCleanupCommand.CanExecute(null))
            {
                ViewModel.RunCleanupCommand.Execute(null);
            }
        }

        private void MenuQuit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RefreshHistoryCommand.Execute(null);
            ShowOnlyOverlay(OverlaySettings);
        }

        private void CloseSettings_Click(object sender, RoutedEventArgs e)
        {
            OverlaySettings.Visibility = Visibility.Collapsed;
        }

        private void MenuHistory_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RefreshHistoryCommand.Execute(null);
            ShowOnlyOverlay(OverlayHistory);
        }

        private void CloseHistory_Click(object sender, RoutedEventArgs e)
        {
            OverlayHistory.Visibility = Visibility.Collapsed;
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            ShowOnlyOverlay(OverlayAbout);
        }

        private async void MenuCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            await CheckForUpdatesAsync(showDialog: sender is not Button);
        }

        private async Task CheckForUpdatesAsync(bool showDialog)
        {
            if (!ViewModel.CheckUpdatesCommand.CanExecute(null))
            {
                ViewModel.StatusText = "Verification deja en cours...";
                return;
            }

            ViewModel.StatusText = "Verification des mises a jour en cours...";
            ViewModel.CheckUpdatesCommand.Execute(null);

            int guard = 0;
            while (ViewModel.IsCheckingUpdate && guard < 200)
            {
                await Task.Delay(50);
                guard++;
            }

            string result = string.IsNullOrWhiteSpace(ViewModel.UpdateStatusText)
                ? "Verification terminee."
                : ViewModel.UpdateStatusText;
            ViewModel.StatusText = result;

            if (!showDialog)
            {
                return;
            }

            var dialog = new ContentDialog
            {
                Title = "Mises a jour",
                Content = result,
                CloseButtonText = ViewModel.IsUpdateAvailable ? "Plus tard" : "OK",
                XamlRoot = this.XamlRoot
            };

            if (ViewModel.IsUpdateAvailable)
            {
                dialog.PrimaryButtonText = "Telecharger";
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
                    Title = "Mise a jour",
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

        private void CloseAbout_Click(object sender, RoutedEventArgs e)
        {
            OverlayAbout.Visibility = Visibility.Collapsed;
        }

        private void MainPage_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape && CloseAnyOverlay())
            {
                e.Handled = true;
            }
        }

        private void ShowOnlyOverlay(UIElement targetOverlay)
        {
            OverlaySettings.Visibility = Visibility.Collapsed;
            OverlayHistory.Visibility = Visibility.Collapsed;
            OverlayAbout.Visibility = Visibility.Collapsed;
            targetOverlay.Visibility = Visibility.Visible;
        }

        private bool CloseAnyOverlay()
        {
            if (OverlaySettings.Visibility == Visibility.Visible)
            {
                OverlaySettings.Visibility = Visibility.Collapsed;
                return true;
            }

            if (OverlayHistory.Visibility == Visibility.Visible)
            {
                OverlayHistory.Visibility = Visibility.Collapsed;
                return true;
            }

            if (OverlayAbout.Visibility == Visibility.Visible)
            {
                OverlayAbout.Visibility = Visibility.Collapsed;
                return true;
            }

            return false;
        }
    }
}