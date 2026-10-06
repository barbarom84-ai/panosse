using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Panosse.Core.ViewModels;
using Panosse.Services;
using Panosse.WinUI.Services;
using Panosse.WinUI.Views;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace Panosse.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private SingleInstanceCoordinator? singleInstance;
        private Window? window;
        private ISystemTrayService? systemTrayService;
        private IGlobalHotkeyService? globalHotkeyService;
        private bool resourcesDisposed;
        private bool isExiting;

        public static Window MainWindow { get; private set; } = null!;

        public IServiceProvider Services { get; }

        public App()
        {
            if (!SingleInstanceCoordinator.TryClaimPrimary(out singleInstance))
            {
                Environment.Exit(0);
            }

            Services = ConfigureServices();
            InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            window ??= new Window();
            MainWindow = window;
            window.Title = "Panosse";

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            ApplyWindowIcon(appWindow);
            
            // Taille initiale ; affinée après le premier rendu (DPI + zone de travail).
            appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 1060, Height = 820 });
            
            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                presenter.IsResizable = true;
                presenter.IsMaximizable = true;
            }

            appWindow.Closing += (_, args) =>
            {
                if (isExiting)
                {
                    return;
                }

                // Fermer la fenêtre = masquer dans le tray (Quitter via le menu tray).
                args.Cancel = true;
                HideMainWindow();
            };
            
            window.ExtendsContentIntoTitleBar = false;

            if (window.Content is not Frame rootFrame)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                window.Content = rootFrame;
            }

            _ = rootFrame.Navigate(typeof(MainPage), e.Arguments);
            window.Activate();

            InitializeSystemTrayAndHotkey();
            singleInstance?.StartActivationWatcher(() =>
            {
                _ = window?.DispatcherQueue.TryEnqueue(BringWindowToFront);
            });
        }

        /// <summary>
        /// Affiche l'icône Panosse dans la barre de titre (app unpackagée).
        /// Cherche Assets\panosse.ico sous AppContext.BaseDirectory (Debug + single-file extrait).
        /// </summary>
        private static void ApplyWindowIcon(Microsoft.UI.Windowing.AppWindow appWindow)
        {
            try
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "panosse.ico");
                if (!File.Exists(iconPath))
                {
                    iconPath = Path.Combine(AppContext.BaseDirectory, "panosse.ico");
                }

                if (File.Exists(iconPath))
                {
                    appWindow.SetIcon(iconPath);
                }
            }
            catch
            {
                // Icône décorative : ne pas bloquer le démarrage.
            }
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerService, LoggingService>();
            services.AddSingleton<ITelemetryService, TelemetryService>();
            services.AddSingleton<IOperationHistoryService, OperationHistoryService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<ICleanupService, CleanupService>();
            services.AddSingleton<IRegistryCleanerService, RegistryCleanerService>();
            services.AddSingleton<IDriverCleanerService, DriverCleanerService>();
            services.AddSingleton<PendingDriverCleanup>();
            services.AddSingleton<DriversViewModel>();
            services.AddSingleton<ICleanupOrchestrator, CleanupOrchestrator>();
            services.AddSingleton<IBrowserProcessService, BrowserProcessService>();
            services.AddSingleton<IUpdateService, UpdateService>();
            services.AddSingleton<IUpdateDownloadService, UpdateDownloadService>();
            services.AddSingleton<IUpdateInstallService, UpdateInstallService>();
            services.AddSingleton<IUpdateOrchestrator, UpdateOrchestrator>();
            services.AddSingleton<IDiagnosticsService, DiagnosticsService>();
            services.AddSingleton<ShellViewModel>();
            services.AddSingleton<ISystemTrayService, SystemTrayService>();
            services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        public static T GetRequiredService<T>() where T : notnull
        {
            App app = (App)Current;
            return app.Services.GetRequiredService<T>();
        }

        private void InitializeSystemTrayAndHotkey()
        {
            if (window == null)
            {
                return;
            }

            systemTrayService ??= Services.GetRequiredService<ISystemTrayService>();
            globalHotkeyService ??= Services.GetRequiredService<IGlobalHotkeyService>();

            systemTrayService.Initialize(
                onShowRequested: BringWindowToFront,
                onCleanupRequested: TriggerCleanupFromTray,
                onExitRequested: ExitApplication);

            IntPtr hWnd = WindowNative.GetWindowHandle(window);
            bool hotkeyRegistered = globalHotkeyService.Register(hWnd);
            if (!hotkeyRegistered)
            {
                systemTrayService.ShowNotification(
                    "Panosse",
                    "Impossible d'enregistrer Ctrl+Alt+P (raccourci déjà pris).");
            }

            globalHotkeyService.HotkeyPressed += (_, _) => TriggerCleanupFromTray();

            // Closed ne doit plus disposer tray/hotkey : la fenêtre se masque via Closing.
        }

        private const int SwHide = 0;
        private const int SwShow = 5;
        private const int SwRestore = 9;

        private void HideMainWindow()
        {
            if (window == null)
            {
                return;
            }

            IntPtr hWnd = WindowNative.GetWindowHandle(window);
            _ = ShowWindow(hWnd, SwHide);
        }

        private void BringWindowToFront()
        {
            if (window == null)
            {
                return;
            }

            IntPtr hWnd = WindowNative.GetWindowHandle(window);
            _ = ShowWindow(hWnd, SwRestore);
            _ = ShowWindow(hWnd, SwShow);
            _ = SetForegroundWindow(hWnd);
            window.Activate();
        }

        private void TriggerCleanupFromTray()
        {
            if (window?.Content is Frame frame && frame.Content is MainPage mainPage)
            {
                mainPage.TriggerCleanupFromTray();
                BringWindowToFront();
            }
        }

        /// <summary>
        /// Relance Panosse avec élévation UAC puis quitte l'instance courante.
        /// Renvoie false si l'utilisateur refuse l'élévation.
        /// </summary>
        public bool TryRestartElevated()
        {
            string? executable = Environment.ProcessPath;
            if (string.IsNullOrEmpty(executable))
            {
                return false;
            }

            try
            {
                _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(executable)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    Arguments = SingleInstanceCoordinator.ElevatedRestartArgument
                });
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return false;
            }

            ExitApplication();
            return true;
        }

        private void ExitApplication()
        {
            isExiting = true;
            DisposeResources();
            window?.Close();
            Environment.Exit(0);
        }

        private void DisposeResources()
        {
            if (resourcesDisposed)
            {
                return;
            }

            resourcesDisposed = true;
            globalHotkeyService?.Dispose();
            systemTrayService?.Dispose();
            singleInstance?.Dispose();
            singleInstance = null;
            if (Services is IDisposable disposableProvider)
            {
                disposableProvider.Dispose();
            }
        }

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
