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
        private static Mutex? instanceMutex;
        private const string MutexName = "Panosse_Unique_Mutex_99";

        private Window? window;
        private ISystemTrayService? systemTrayService;
        private IGlobalHotkeyService? globalHotkeyService;
        private bool resourcesDisposed;

        public static Window MainWindow { get; private set; } = null!;

        public IServiceProvider Services { get; }

        public App()
        {
            instanceMutex = new Mutex(true, MutexName, out bool isNewInstance);
            if (!isNewInstance)
            {
                ShowMessageBox(
                    "Panosse est déjà active dans la barre des tâches.\n\n" +
                    "Astuce : double-cliquez sur l'icône dans la zone de notification pour afficher la fenêtre.",
                    "Panosse - Déjà active");
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
            
            // Taille initiale ; affinée après le premier rendu (DPI + zone de travail).
            appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 540, Height = 880 });
            
            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
            }
            
            // On retire ExtendsContentIntoTitleBar pour que la barre de titre standard s'affiche
            // et ne cache pas le menu en haut.
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
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerService, LoggingService>();
            services.AddSingleton<ITelemetryService, TelemetryService>();
            services.AddSingleton<IOperationHistoryService, OperationHistoryService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<ICleanupService, CleanupService>();
            services.AddSingleton<ICleanupOrchestrator, CleanupOrchestrator>();
            services.AddSingleton<IUpdateService, UpdateService>();
            services.AddSingleton<IUpdateOrchestrator, UpdateOrchestrator>();
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
            _ = globalHotkeyService.Register(hWnd);
            globalHotkeyService.HotkeyPressed += (_, _) => TriggerCleanupFromTray();

            window.Closed += (_, _) =>
            {
                DisposeResources();
            };
        }

        private void BringWindowToFront()
        {
            if (window == null)
            {
                return;
            }

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

        private void ExitApplication()
        {
            DisposeResources();
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
            if (instanceMutex is not null)
            {
                instanceMutex.ReleaseMutex();
                instanceMutex.Dispose();
                instanceMutex = null;
            }
            if (Services is IDisposable disposableProvider)
            {
                disposableProvider.Dispose();
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        private static void ShowMessageBox(string text, string caption)
        {
            _ = MessageBox(IntPtr.Zero, text, caption, 0x40);
        }
    }
}
