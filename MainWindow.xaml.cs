using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Panosse.Services;
using Panosse.ViewModels;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace Panosse
{
    public partial class MainWindow : Window
    {
        // Imports pour RegisterHotKey (raccourci clavier global)
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        
        // Constantes pour le HotKey
        private const int HOTKEY_ID = 9000;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_ALT = 0x0001;
        private const uint VK_P = 0x50; // Touche 'P'
        private const int WM_HOTKEY = 0x0312;
        
        // Handle pour la fenêtre (nécessaire pour RegisterHotKey)
        private IntPtr windowHandle;
        private System.Windows.Interop.HwndSource? hwndSource;
        
        /// <summary>
        /// Log de debug pour tracer le démarrage de l'application
        /// Actif uniquement en mode DEBUG (supprimé en Release)
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        private void LogDebug(string message)
        {
#if DEBUG
            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "panosse_debug.log"
                );
                
                string log = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
                File.AppendAllText(logPath, log + "\n");
                
                // Aussi en console pour debug
                System.Diagnostics.Debug.WriteLine(log);
            }
            catch
            {
                // Si on ne peut pas logger, on continue quand même
            }
#endif
        }

        private Storyboard? pulseStoryboard;
        private readonly MainViewModel viewModel;
        private int etapesCourantes = 0;
        private int etapesTotales = 8;
        
        // Statistiques de nettoyage
        private long espaceLibereMo = 0;
        
        // Version actuelle de l'application (lue automatiquement depuis le .csproj)
        private static readonly string VERSION_ACTUELLE = 
            Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
        private const string GITHUB_REPO = "barbarom84-ai/panosse";
        
        // URLs de la dernière release
        private string? derniereVersionUrl = null;
        private string? derniereVersionTag = null;
        private string? downloadUrl = null;
        private bool estAJour = false;  // Indique si l'application est à jour
        private bool verificationEchouee = false;  // Indique si la vérification a échoué (pas de connexion)
        
        // Navigateurs en cours d'exécution
        private System.Collections.Generic.List<string> navigateursEnCours = new System.Collections.Generic.List<string>();
        
        // System Tray Icon
        private Forms.NotifyIcon? notifyIcon;
        private Forms.ContextMenuStrip? contextMenu;
        
        // Mémoire Sélective (v2.0) - Surveillance du dossier Téléchargements
        private System.Timers.Timer? surveillanceTimer;
        private bool dossierTelechargementsEncombre = false;
        private double tailleTelechargementsGo = 0;
        private int nombreFichiersAnciens = 0;
        private Drawing.Icon? iconeNormale;
        private Drawing.Icon? iconeAlerte;
        private const double SEUIL_TAILLE_GO = 5.0;
        private const long SEUIL_FICHIER_GROS_MO = 200;
        private const int SEUIL_JOURS_ANCIEN = 30;
        private readonly ICleanupService cleanupService;
        private readonly IUpdateService updateService;
        private readonly ISettingsService settingsService;
        private readonly ITelemetryService telemetryService;
        private readonly ILoggerService loggerService;
        private readonly IOperationHistoryService historyService;
        private readonly ICleanupOrchestrator cleanupOrchestrator;
        private readonly IUpdateOrchestrator updateOrchestrator;
        private readonly SemaphoreSlim surveillanceLock = new(1, 1);
        private System.Timers.Timer? scheduledCleanupTimer;
        private readonly Stopwatch startupStopwatch = Stopwatch.StartNew();

        public MainWindow()
            : this(
                new CleanupService(),
                new UpdateService(),
                new SettingsService(),
                new TelemetryService(),
                new LoggingService(),
                new OperationHistoryService())
        {
        }

        public MainWindow(
            ICleanupService cleanupService,
            IUpdateService updateService,
            ISettingsService settingsService,
            ITelemetryService telemetryService,
            ILoggerService loggerService,
            IOperationHistoryService historyService)
        {
            try
            {
                LogDebug("Constructeur - Début");
                
                InitializeComponent();
                LogDebug("Constructeur - InitializeComponent OK");
                viewModel = new MainViewModel();
                DataContext = viewModel;
                this.cleanupService = cleanupService;
                this.updateService = updateService;
                this.settingsService = settingsService;
                this.telemetryService = telemetryService;
                this.loggerService = loggerService;
                this.historyService = historyService;
                cleanupOrchestrator = new CleanupOrchestrator(this.cleanupService, this.telemetryService, this.historyService, this.loggerService);
                updateOrchestrator = new UpdateOrchestrator(this.telemetryService, this.historyService, this.loggerService);
                ChargerParametres();
                ConfigurerCommandes();
                telemetryService.Increment("app_launch_count");
                
                Loaded += MainWindow_Loaded;
                LogDebug("Constructeur - Loaded event ajouté");

                // Définir la version dynamiquement depuis l'assembly
                viewModel.VersionText = $"v{VERSION_ACTUELLE}";
                RafraichirHistorique();
                LogDebug($"Constructeur - Version définie: {VERSION_ACTUELLE}");
                
                LogDebug("Constructeur - Fin (succès)");
            }
            catch (Exception ex)
            {
                LogDebug($"Constructeur - ERREUR: {ex.Message}");
                throw;
            }
        }

        private void ConfigurerCommandes()
        {
            viewModel.CleanCommand = new AsyncRelayCommand(ExecuteCleaningAsync, onException: GererExceptionCommandeAsync);
            viewModel.MinimizeToTrayCommand = new RelayCommand(() => BtnQuitter_Click(this, new RoutedEventArgs()));
            viewModel.QuitCommand = new RelayCommand(() => MenuItem_QuitterDefinitivement_Click(this, new RoutedEventArgs()));
            viewModel.OpenAboutCommand = new RelayCommand(() => BtnAPropos_Click(this, new RoutedEventArgs()));
            viewModel.CloseAboutCommand = new RelayCommand(() => BtnRetourAPropos_Click(this, new RoutedEventArgs()));
            viewModel.RefreshDetectionCommand = new RelayCommand(() => MenuItem_Actualiser_Click(this, new RoutedEventArgs()));
            viewModel.OpenGitHubCommand = new RelayCommand(() => MenuItem_GitHub_Click(this, new RoutedEventArgs()));
            viewModel.CheckUpdatesCommand = new AsyncRelayCommand(ExecuteCheckUpdatesAsync, onException: GererExceptionCommandeAsync);
            viewModel.InstallUpdateCommand = new AsyncRelayCommand(ExecuteInstallUpdateAsync, onException: GererExceptionCommandeAsync);
            viewModel.CloseUpdateBarCommand = new RelayCommand(() => BtnFermerUpdate_Click(this, new RoutedEventArgs()));
            viewModel.OpenSettingsCommand = new RelayCommand(() => OuvrirParametres());
            viewModel.CloseSettingsCommand = new RelayCommand(() => FermerParametres());
            viewModel.SaveSettingsCommand = new RelayCommand(() => SauvegarderParametres());
            viewModel.ShowHistoryCommand = new RelayCommand(() => AfficherHistoriqueOperations());
        }

        private void ChargerParametres()
        {
            AppSettings settings = settingsService.Load();
            viewModel.CheckUpdatesOnStartup = settings.CheckUpdatesOnStartup;
            viewModel.PlaySuccessSound = settings.PlaySuccessSound;
            viewModel.ShowTrayNotifications = settings.ShowTrayNotifications;
            viewModel.PreviewModeEnabled = settings.PreviewModeEnabled;
            viewModel.ExclusionPatterns = settings.ExclusionPatterns;
            viewModel.EnableScheduledCleanup = settings.EnableScheduledCleanup;
            viewModel.ScheduledCleanupIntervalHours = Math.Max(1, settings.ScheduledCleanupIntervalHours);
            ConfigurerPlanificationNettoyage();
        }

        private void SauvegarderParametres()
        {
            settingsService.Save(new AppSettings
            {
                CheckUpdatesOnStartup = viewModel.CheckUpdatesOnStartup,
                PlaySuccessSound = viewModel.PlaySuccessSound,
                ShowTrayNotifications = viewModel.ShowTrayNotifications,
                PreviewModeEnabled = viewModel.PreviewModeEnabled,
                ExclusionPatterns = viewModel.ExclusionPatterns,
                EnableScheduledCleanup = viewModel.EnableScheduledCleanup,
                ScheduledCleanupIntervalHours = Math.Max(1, viewModel.ScheduledCleanupIntervalHours)
            });
            telemetryService.Increment("settings_save_count");
            ConfigurerPlanificationNettoyage();

            FermerParametres();
            viewModel.StatusText = "⚙️ Paramètres sauvegardés";
            viewModel.StatusForeground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
        }

        private void GererExceptionCommandeAsync(Exception ex)
        {
            loggerService.LogError("commands", "Commande asynchrone en échec.", ex);
            telemetryService.Increment("async_command_failed_count");
            viewModel.StatusText = "⚠️ Une opération a échoué. Vérifiez les logs.";
            viewModel.StatusForeground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
        }

        private void OuvrirParametres()
        {
            telemetryService.Increment("settings_open_count");
            OverlaySettings.Visibility = Visibility.Visible;
        }

        private void FermerParametres()
        {
            OverlaySettings.Visibility = Visibility.Collapsed;
        }

        private void ConfigurerPlanificationNettoyage()
        {
            try
            {
                if (scheduledCleanupTimer != null)
                {
                    scheduledCleanupTimer.Stop();
                    scheduledCleanupTimer.Dispose();
                    scheduledCleanupTimer = null;
                }

                if (!viewModel.EnableScheduledCleanup)
                {
                    return;
                }

                int intervalHours = Math.Max(1, viewModel.ScheduledCleanupIntervalHours);
                scheduledCleanupTimer = new System.Timers.Timer(TimeSpan.FromHours(intervalHours).TotalMilliseconds);
                scheduledCleanupTimer.AutoReset = true;
                scheduledCleanupTimer.Elapsed += async (_, _) =>
                {
                    telemetryService.Increment("cleanup_scheduled_trigger_count");
                    await Dispatcher.InvokeAsync(() => LancerNettoyageArrierePlan());
                };
                scheduledCleanupTimer.Start();
            }
            catch (Exception ex)
            {
                loggerService.LogError("scheduler", "Impossible de configurer la planification.", ex);
            }
        }

        private List<string> GetExclusionPatterns()
        {
            return (viewModel.ExclusionPatterns ?? string.Empty)
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(pattern => pattern.Trim())
                .Where(pattern => !string.IsNullOrWhiteSpace(pattern))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void RafraichirHistorique()
        {
            var lastEntries = historyService.GetRecentEntries(3);
            if (lastEntries.Count == 0)
            {
                viewModel.HistorySummary = "Aucune opération enregistrée.";
                return;
            }

            viewModel.HistorySummary = string.Join(
                " | ",
                lastEntries.Select(entry =>
                    $"{entry.TimestampUtc.ToLocalTime():dd/MM HH:mm} {entry.OperationType}: {entry.Outcome}"));
        }

        private void AfficherHistoriqueOperations()
        {
            var entries = historyService.GetRecentEntries(12);
            if (entries.Count == 0)
            {
                MessageBox.Show("Aucun historique disponible.", "Historique", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string content = string.Join(
                Environment.NewLine,
                entries.Select(entry =>
                    $"{entry.TimestampUtc.ToLocalTime():dd/MM/yyyy HH:mm} | {entry.OperationType} | {entry.Outcome} | {(entry.FreedBytes / 1024.0 / 1024.0):F2} Mo | {entry.DurationMs} ms"));

            MessageBox.Show(content, "Historique des opérations", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Initialise l'icône dans le System Tray (barre des tâches)
        /// </summary>
        private void InitialiserSystemTray()
        {
            // Créer le menu contextuel
            contextMenu = new Forms.ContextMenuStrip();
            
            // Menu "Ouvrir Panosse"
            var menuOuvrir = new Forms.ToolStripMenuItem("🪟 Ouvrir Panosse");
            menuOuvrir.Click += (s, e) => AfficherFenetre();
            contextMenu.Items.Add(menuOuvrir);
            
            // Menu "Passer la panosse maintenant"
            var menuNettoyer = new Forms.ToolStripMenuItem("🧹 Passer la panosse maintenant");
            menuNettoyer.Click += (s, e) => 
            {
                AfficherFenetre();
                Dispatcher.Invoke(() => _ = ExecuteCleaningAsync());
            };
            contextMenu.Items.Add(menuNettoyer);
            
            // Séparateur
            contextMenu.Items.Add(new Forms.ToolStripSeparator());
            
            // Menu "Pourquoi l'icône est rouge?" (Mémoire Sélective - v2.0)
            var menuPourquoi = new Forms.ToolStripMenuItem("❓ Pourquoi l'icône est rouge ?");
            menuPourquoi.Name = "MenuPourquoi";
            menuPourquoi.Visible = false; // Visible uniquement si encombré
            menuPourquoi.Click += (s, e) => AfficherExplicationEncombrement();
            contextMenu.Items.Add(menuPourquoi);
            
            // Séparateur (visible uniquement si menu "Pourquoi" visible)
            var separatorPourquoi = new Forms.ToolStripSeparator();
            separatorPourquoi.Name = "SeparatorPourquoi";
            separatorPourquoi.Visible = false;
            contextMenu.Items.Add(separatorPourquoi);
            
            // Menu "Quitter"
            var menuQuitter = new Forms.ToolStripMenuItem("❌ Quitter");
            menuQuitter.Click += (s, e) => QuitterApplication();
            contextMenu.Items.Add(menuQuitter);
            
            // Créer l'icône dans le System Tray
            notifyIcon = new Forms.NotifyIcon
            {
                Text = "Panosse - La serpillère numérique",
                Visible = true,
                ContextMenuStrip = contextMenu
            };
            
            // Charger les icônes depuis les ressources embarquées
            ChargerIcones();
            
            // Double-clic pour afficher la fenêtre
            notifyIcon.DoubleClick += (s, e) => AfficherFenetre();
            
            // Stocker l'icône normale pour pouvoir basculer
            // (déjà fait dans ChargerIcones)
            
            // Démarrer la surveillance du dossier Téléchargements
            DemarrerSurveillanceTelechi();
            
            // Gérer la fermeture de la fenêtre (masquer au lieu de fermer)
            this.Closing += MainWindow_Closing;
        }
        
        /// <summary>
        /// Charge les deux icônes (propre et sale) depuis les ressources
        /// </summary>
        private void ChargerIcones()
        {
            try
            {
                // Icône PROPRE (normale)
                var iconPropreUri = new Uri("pack://application:,,,/assets/panosse.ico");
                var streamPropre = System.Windows.Application.GetResourceStream(iconPropreUri);
                
                if (streamPropre != null)
                {
                    using (var stream = streamPropre.Stream)
                    {
                        // Créer une copie du stream pour éviter qu'il soit fermé
                        using (var ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            ms.Position = 0;
                            iconeNormale = new Drawing.Icon(ms);
                        }
                    }
                    if (notifyIcon != null && iconeNormale != null)
                    {
                        notifyIcon.Icon = iconeNormale;
                    }
                    LogDebug("✅ Icône propre chargée depuis les ressources");
                }
                else
                {
                    // Fallback : fichier physique
                    string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "panosse.ico");
                    if (File.Exists(iconPath))
                    {
                        iconeNormale = new Drawing.Icon(iconPath);
                        if (notifyIcon != null && iconeNormale != null)
                        {
                            notifyIcon.Icon = iconeNormale;
                        }
                        LogDebug("✅ Icône propre chargée depuis fichier");
                    }
                }
                
                // Icône SALE (alerte)
                var iconSaleUri = new Uri("pack://application:,,,/assets/panosse_sale.ico");
                var streamSale = System.Windows.Application.GetResourceStream(iconSaleUri);
                
                if (streamSale != null)
                {
                    using (var stream = streamSale.Stream)
                    {
                        // Créer une copie du stream
                        using (var ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            ms.Position = 0;
                            iconeAlerte = new Drawing.Icon(ms);
                        }
                    }
                    LogDebug("✅ Icône sale chargée depuis les ressources");
                }
                else
                {
                    // Fallback : fichier physique
                    string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "panosse_sale.ico");
                    if (File.Exists(iconPath))
                    {
                        iconeAlerte = new Drawing.Icon(iconPath);
                        LogDebug("✅ Icône sale chargée depuis fichier");
                    }
                    else
                    {
                        // Fallback final : créer une icône rouge dynamiquement
                        LogDebug("⚠️ panosse_sale.ico introuvable, création dynamique");
                        CreerIconeAlerteDynamique();
                    }
                }
                
                // Si on n'a aucune icône, utiliser l'icône système
                if (iconeNormale == null)
                {
                    iconeNormale = Drawing.SystemIcons.Application;
                    if (notifyIcon != null)
                    {
                        notifyIcon.Icon = iconeNormale;
                    }
                    LogDebug("⚠️ Utilisation icône système par défaut");
                }
                
                if (iconeAlerte == null)
                {
                    iconeAlerte = Drawing.SystemIcons.Warning;
                    LogDebug("⚠️ Utilisation icône Warning système par défaut");
                }
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Erreur chargement icônes: {ex.Message}");
                // Fallback complet
                iconeNormale = Drawing.SystemIcons.Application;
                iconeAlerte = Drawing.SystemIcons.Warning;
                if (notifyIcon != null && iconeNormale != null)
                {
                    notifyIcon.Icon = iconeNormale;
                }
            }
        }
        
        /// <summary>
        /// Affiche la fenêtre principale et la met au premier plan
        /// </summary>
        private void AfficherFenetre()
        {
            Dispatcher.Invoke(() =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
                this.Focus();
            });
        }
        
        /// <summary>
        /// Quitte vraiment l'application (pas seulement masquer)
        /// </summary>
        private void QuitterApplication()
        {
            // Désenregistrer le HotKey
            DesenregistrerHotKey();
            
            // Arrêter la surveillance
            ArreterSurveillanceTelechi();
            settingsService.Save(new AppSettings
            {
                CheckUpdatesOnStartup = viewModel.CheckUpdatesOnStartup,
                PlaySuccessSound = viewModel.PlaySuccessSound,
                ShowTrayNotifications = viewModel.ShowTrayNotifications,
                PreviewModeEnabled = viewModel.PreviewModeEnabled,
                ExclusionPatterns = viewModel.ExclusionPatterns,
                EnableScheduledCleanup = viewModel.EnableScheduledCleanup,
                ScheduledCleanupIntervalHours = Math.Max(1, viewModel.ScheduledCleanupIntervalHours)
            });

            if (scheduledCleanupTimer != null)
            {
                scheduledCleanupTimer.Stop();
                scheduledCleanupTimer.Dispose();
                scheduledCleanupTimer = null;
            }
            
            // Nettoyer l'icône du System Tray
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
                notifyIcon = null;
            }
            
            // Fermer l'application
            if (updateOrchestrator is IDisposable disposableUpdater)
            {
                disposableUpdater.Dispose();
            }

            Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown());
        }
        
        /// <summary>
        /// Intercepte la fermeture de la fenêtre pour la masquer au lieu de la fermer
        /// </summary>
        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            // Annuler la fermeture
            e.Cancel = true;
            
            // Masquer la fenêtre au lieu de la fermer
            this.Hide();
            
            // Afficher une notification (optionnel)
            if (notifyIcon != null && viewModel.ShowTrayNotifications)
            {
                notifyIcon.ShowBalloonTip(
                    2000,
                    "Panosse",
                    "Panosse est toujours actif dans la barre des tâches. Double-cliquez sur l'icône pour le réouvrir.",
                    Forms.ToolTipIcon.Info
                );
            }
        }
        
        #region Mémoire Sélective - Surveillance du dossier Téléchargements (v2.0)
        
        /// <summary>
        /// Démarre la surveillance périodique du dossier Téléchargements
        /// </summary>
        private void DemarrerSurveillanceTelechi()
        {
            try
            {
                // Créer un timer qui se déclenche toutes les heures
                surveillanceTimer = new System.Timers.Timer(3600000); // 1 heure = 3600000 ms
                surveillanceTimer.Elapsed += async (sender, e) => await VerifierEncombrementTelechi();
                surveillanceTimer.AutoReset = true;
                surveillanceTimer.Start();
                
                // Faire une première vérification immédiatement (après 30 secondes pour ne pas ralentir le démarrage)
                Task.Run(async () =>
                {
                    await Task.Delay(30000); // 30 secondes
                    await VerifierEncombrementTelechi();
                });
                
                LogDebug("✅ Surveillance du dossier Téléchargements démarrée (vérification toutes les heures)");
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Erreur démarrage surveillance: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Arrête la surveillance du dossier Téléchargements
        /// </summary>
        private void ArreterSurveillanceTelechi()
        {
            try
            {
                if (surveillanceTimer != null)
                {
                    surveillanceTimer.Stop();
                    surveillanceTimer.Dispose();
                    surveillanceTimer = null;
                }
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Erreur arrêt surveillance: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Vérifie l'encombrement du dossier Téléchargements de manière asynchrone
        /// </summary>
        private async Task VerifierEncombrementTelechi()
        {
            if (!await surveillanceLock.WaitAsync(0))
            {
                telemetryService.Increment("downloads_scan_overlap_skipped_count");
                return;
            }

            var sw = Stopwatch.StartNew();
            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        string downloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                        
                        if (!Directory.Exists(downloadPath))
                        {
                            return;
                        }
                        
                        long tailleTotal = 0;
                        int fichiersAnciens = 0;
                        DateTime seuil30Jours = DateTime.Now.AddDays(-SEUIL_JOURS_ANCIEN);
                        
                        // Parcourir tous les fichiers
                        var fichiers = Directory.GetFiles(downloadPath, "*", SearchOption.AllDirectories);
                        
                        foreach (var fichier in fichiers)
                        {
                            try
                            {
                                var info = new FileInfo(fichier);
                                tailleTotal += info.Length;
                                
                                // Vérifier les gros fichiers anciens
                                long tailleMo = info.Length / (1024 * 1024);
                                if (tailleMo >= SEUIL_FICHIER_GROS_MO && info.LastWriteTime < seuil30Jours)
                                {
                                    fichiersAnciens++;
                                }
                            }
                            catch
                            {
                                // Ignorer les fichiers inaccessibles
                            }
                        }
                        
                        // Convertir en Go
                        double tailleGo = tailleTotal / (1024.0 * 1024.0 * 1024.0);
                        
                        // Déterminer si encombré
                        bool etaitEncombre = dossierTelechargementsEncombre;
                        dossierTelechargementsEncombre = tailleGo > SEUIL_TAILLE_GO || fichiersAnciens > 0;
                        tailleTelechargementsGo = tailleGo;
                        nombreFichiersAnciens = fichiersAnciens;
                        
                        LogDebug($"📊 Téléchargements: {tailleGo:F2} Go, {fichiersAnciens} gros fichiers anciens");
                        
                        // Mettre à jour l'icône si l'état a changé
                        if (etaitEncombre != dossierTelechargementsEncombre)
                        {
                            Dispatcher.InvokeAsync(() => MettreAJourIconeSystemTray());
                        }
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"❌ Erreur vérification encombrement: {ex.Message}");
                        loggerService.LogError("downloads", "Erreur pendant la vérification d'encombrement.", ex);
                    }
                });

                sw.Stop();
                telemetryService.RecordDuration("downloads_scan_duration_ms", sw.Elapsed);
            }
            finally
            {
                surveillanceLock.Release();
            }
        }
        
        /// <summary>
        /// Crée dynamiquement une icône d'alerte avec un point rouge (fallback)
        /// </summary>
        private void CreerIconeAlerteDynamique()
        {
            try
            {
                // Créer une icône rouge à partir de l'icône normale
                if (iconeNormale != null)
                {
                    // Créer un bitmap de 16x16 (taille standard System Tray)
                    using (var bitmap = new Drawing.Bitmap(16, 16))
                    using (var graphics = Drawing.Graphics.FromImage(bitmap))
                    {
                        // Dessiner l'icône normale
                        graphics.DrawIcon(iconeNormale, 0, 0);
                        
                        // Ajouter un point d'exclamation rouge en haut à droite
                        using (var brush = new Drawing.SolidBrush(Drawing.Color.Red))
                        {
                            graphics.FillEllipse(brush, 10, 0, 6, 6);
                        }
                        
                        // Convertir en icône
                        IntPtr hIcon = bitmap.GetHicon();
                        iconeAlerte = Drawing.Icon.FromHandle(hIcon);
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Erreur création icône alerte: {ex.Message}");
                // En cas d'erreur, utiliser l'icône Warning de Windows
                iconeAlerte = Drawing.SystemIcons.Warning;
            }
        }
        
        /// <summary>
        /// Met à jour l'icône du System Tray en fonction de l'état d'encombrement
        /// </summary>
        private void MettreAJourIconeSystemTray()
        {
            if (notifyIcon == null || contextMenu == null)
                return;
            
            try
            {
                if (dossierTelechargementsEncombre)
                {
                    // État encombré : icône rouge
                    if (iconeAlerte != null)
                    {
                        notifyIcon.Icon = iconeAlerte;
                        notifyIcon.Text = $"⚠️ Panosse - Téléchargements encombré ({tailleTelechargementsGo:F1} Go)";
                    }
                    
                    // Afficher le menu "Pourquoi l'icône est rouge?"
                    var menuPourquoi = contextMenu.Items.Find("MenuPourquoi", false).FirstOrDefault();
                    var separatorPourquoi = contextMenu.Items.Find("SeparatorPourquoi", false).FirstOrDefault();
                    
                    if (menuPourquoi != null)
                        menuPourquoi.Visible = true;
                    if (separatorPourquoi != null)
                        separatorPourquoi.Visible = true;
                    
                    LogDebug("🔴 Icône System Tray passée en mode ALERTE");
                }
                else
                {
                    // État propre : icône normale
                    if (iconeNormale != null)
                    {
                        notifyIcon.Icon = iconeNormale;
                        notifyIcon.Text = "Panosse - La serpillère numérique";
                    }
                    
                    // Masquer le menu "Pourquoi l'icône est rouge?"
                    var menuPourquoi = contextMenu.Items.Find("MenuPourquoi", false).FirstOrDefault();
                    var separatorPourquoi = contextMenu.Items.Find("SeparatorPourquoi", false).FirstOrDefault();
                    
                    if (menuPourquoi != null)
                        menuPourquoi.Visible = false;
                    if (separatorPourquoi != null)
                        separatorPourquoi.Visible = false;
                    
                    LogDebug("🟢 Icône System Tray passée en mode NORMAL");
                }
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Erreur mise à jour icône: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Affiche l'explication de l'encombrement du dossier Téléchargements
        /// </summary>
        private void AfficherExplicationEncombrement()
        {
            if (notifyIcon == null)
                return;
            
            try
            {
                string message;
                
                if (tailleTelechargementsGo > SEUIL_TAILLE_GO && nombreFichiersAnciens > 0)
                {
                    message = $"Votre dossier Téléchargements commence à être encombré:\n\n" +
                             $"📦 Taille totale: {tailleTelechargementsGo:F2} Go\n" +
                             $"📂 {nombreFichiersAnciens} gros fichier(s) ancien(s) (>200 Mo, >30 jours)\n\n" +
                             $"💡 Appuyez sur Ctrl+Alt+P pour faire de la place !";
                }
                else if (tailleTelechargementsGo > SEUIL_TAILLE_GO)
                {
                    message = $"Votre dossier Téléchargements commence à être encombré:\n\n" +
                             $"📦 Taille totale: {tailleTelechargementsGo:F2} Go\n\n" +
                             $"💡 Appuyez sur Ctrl+Alt+P pour faire de la place !";
                }
                else
                {
                    message = $"Votre dossier Téléchargements contient:\n\n" +
                             $"📂 {nombreFichiersAnciens} gros fichier(s) ancien(s) (>200 Mo, >30 jours)\n\n" +
                             $"💡 Appuyez sur Ctrl+Alt+P pour faire de la place !";
                }
                
                notifyIcon.ShowBalloonTip(
                    8000, // 8 secondes pour avoir le temps de lire
                    "⚠️ Dossier Téléchargements encombré",
                    message,
                    Forms.ToolTipIcon.Warning
                );
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Erreur affichage explication: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Remet l'icône propre après un nettoyage manuel (Reset Manuel v2.0)
        /// </summary>
        private void ResetIconePropre()
        {
            if (notifyIcon == null || iconeNormale == null)
                return;
            
            try
            {
                // Forcer le retour à l'icône propre
                dossierTelechargementsEncombre = false;
                notifyIcon.Icon = iconeNormale;
                notifyIcon.Text = "Panosse - La serpillère numérique";
                
                // Masquer le menu "Pourquoi rouge?"
                if (contextMenu != null)
                {
                    var menuPourquoi = contextMenu.Items.Find("MenuPourquoi", false).FirstOrDefault();
                    var separatorPourquoi = contextMenu.Items.Find("SeparatorPourquoi", false).FirstOrDefault();
                    
                    if (menuPourquoi != null)
                        menuPourquoi.Visible = false;
                    if (separatorPourquoi != null)
                        separatorPourquoi.Visible = false;
                }
                
                LogDebug("🟢 Icône remise sur PROPRE après nettoyage");
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Erreur reset icône: {ex.Message}");
            }
        }
        
        #endregion
        
        /// <summary>
        /// Enregistre le raccourci clavier global Ctrl+Alt+P
        /// </summary>
        private void EnregistrerHotKey()
        {
            try
            {
                // Obtenir le handle de la fenêtre
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                windowHandle = helper.Handle;
                
                // Créer le HwndSource pour intercepter les messages Windows
                hwndSource = System.Windows.Interop.HwndSource.FromHwnd(windowHandle);
                if (hwndSource != null)
                {
                    hwndSource.AddHook(WndProc);
                }
                
                // Enregistrer le HotKey : Ctrl+Alt+P
                bool success = RegisterHotKey(windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_P);
                
                if (success)
                {
                    LogDebug("✅ Raccourci Ctrl+Alt+P enregistré avec succès");
                }
                else
                {
                    LogDebug("❌ Échec de l'enregistrement du raccourci");
                }
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Erreur lors de l'enregistrement du HotKey: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Désenregistre le raccourci clavier global
        /// </summary>
        private void DesenregistrerHotKey()
        {
            try
            {
                if (windowHandle != IntPtr.Zero)
                {
                    UnregisterHotKey(windowHandle, HOTKEY_ID);
                }
                
                if (hwndSource != null)
                {
                    hwndSource.RemoveHook(WndProc);
                }
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Erreur lors du désenregistrement du HotKey: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gestionnaire de messages Windows pour intercepter le HotKey
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Vérifier si c'est un message WM_HOTKEY
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                
                // Vérifier si c'est notre HotKey (Ctrl+Alt+P)
                if (id == HOTKEY_ID)
                {
                    handled = true;
                    
                    // Lancer le nettoyage en arrière-plan
                    LogDebug("🔥 Ctrl+Alt+P détecté ! Lancement du nettoyage en arrière-plan...");
                    LancerNettoyageArrierePlan();
                }
            }
            
            return IntPtr.Zero;
        }
        
        /// <summary>
        /// Lance le nettoyage complet en arrière-plan sans afficher la fenêtre
        /// </summary>
        private async void LancerNettoyageArrierePlan()
        {
            telemetryService.Increment("cleanup_background_start_count");
            await Task.Run(async () =>
            {
                try
                {
                    // Réinitialiser l'espace libéré
                    espaceLibereMo = 0;
                    
                    // Exécuter toutes les tâches de nettoyage
                    var result = await cleanupOrchestrator.RunBackgroundCleanupAsync(new CleanupExecutionOptions
                    {
                        PreviewOnly = false,
                        ExclusionPatterns = GetExclusionPatterns()
                    });
                    espaceLibereMo = result.TotalFreedBytes / (1024 * 1024);
                    
                    // Remettre l'icône propre après le nettoyage (Reset Manuel - Mémoire Sélective v2.0)
                    await Dispatcher.InvokeAsync(() => ResetIconePropre());
                    
                    // Jouer le son de réussite
                    await Dispatcher.InvokeAsync(() => JouerSonReussite());
                    
                    // Afficher la notification Toast
                    await Dispatcher.InvokeAsync(() => AfficherNotificationToast());
                    telemetryService.Increment("cleanup_background_success_count");
                }
                catch (Exception ex)
                {
                    LogDebug($"❌ Erreur pendant le nettoyage en arrière-plan: {ex.Message}");
                    loggerService.LogError("cleanup", "Erreur pendant le nettoyage arrière-plan.", ex);
                    telemetryService.Increment("cleanup_background_failed_count");
                }
            });
            RafraichirHistorique();
        }
        
        /// <summary>
        /// Joue le son de réussite système
        /// </summary>
        private void JouerSonReussite()
        {
            if (!viewModel.PlaySuccessSound)
            {
                return;
            }

            try
            {
                // Jouer le son "Asterisk" de Windows (son de succès)
                System.Media.SystemSounds.Asterisk.Play();
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Impossible de jouer le son: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Affiche une notification Toast Windows
        /// </summary>
        private void AfficherNotificationToast()
        {
            if (!viewModel.ShowTrayNotifications)
            {
                return;
            }

            try
            {
                if (notifyIcon != null)
                {
                    // Utiliser BalloonTip pour simuler une notification Toast
                    string message = espaceLibereMo > 0
                        ? $"Panosse a fini son travail : {espaceLibereMo} Mo libérés ! 🧹✨"
                        : "Panosse a fini son travail : PC nettoyé ! 🧹✨";
                    
                    notifyIcon.ShowBalloonTip(
                        5000, // 5 secondes
                        "✅ Nettoyage terminé",
                        message,
                        Forms.ToolTipIcon.Info
                    );
                    telemetryService.Increment("tray_notification_shown_count");
                }
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Impossible d'afficher la notification: {ex.Message}");
            }
        }
        
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LogDebug("MainWindow_Loaded - Début");

                // Laisser le premier rendu UI se faire avant les initialisations plus lourdes.
                await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);

                await Task.Run(() =>
                {
                    LogDebug("MainWindow_Loaded - Initialisation System Tray...");
                    Dispatcher.Invoke(InitialiserSystemTray);
                    LogDebug("MainWindow_Loaded - Enregistrement HotKey...");
                    Dispatcher.Invoke(EnregistrerHotKey);
                });

                // Vérifier les navigateurs hors chemin critique du rendu.
                navigateursEnCours = await Task.Run(CheckRunningBrowsers);
                LogDebug($"MainWindow_Loaded - Navigateurs trouvés: {navigateursEnCours.Count}");
                
                if (navigateursEnCours.Count > 0)
                {
                    string browsers = string.Join(" et ", navigateursEnCours);
                    viewModel.StatusText = $"⚠️ Veuillez fermer {browsers} pour un nettoyage complet (cliquez ici pour fermer automatiquement)";
                    viewModel.StatusForeground = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                    StatusText.Cursor = System.Windows.Input.Cursors.Hand; // Cursor main pour indiquer que c'est cliquable
                    StatusText.TextDecorations = TextDecorations.Underline; // Souligner pour indiquer que c'est cliquable
            }
            
            // Vérifier les mises à jour en arrière-plan
            if (viewModel.CheckUpdatesOnStartup)
            {
                LogDebug("MainWindow_Loaded - Vérification mises à jour...");
                _ = VerifierMiseAJour();
            }
            else
            {
                LogDebug("MainWindow_Loaded - Vérification mises à jour désactivée");
            }
            
            LogDebug("MainWindow_Loaded - Fin (succès)");
            startupStopwatch.Stop();
            telemetryService.RecordDuration("startup_duration_ms", startupStopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            LogDebug($"MainWindow_Loaded - ERREUR: {ex.Message}");
            LogDebug($"MainWindow_Loaded - StackTrace: {ex.StackTrace}");
            loggerService.LogError("startup", "Erreur pendant MainWindow_Loaded.", ex);
            MessageBox.Show(
                $"Erreur lors du chargement de Panosse:\n\n{ex.Message}\n\nVoir panosse_crash.log sur le Bureau.",
                "Panosse - Erreur",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

        private System.Collections.Generic.List<string> CheckRunningBrowsers()
        {
            var browsers = new System.Collections.Generic.List<string>();
            
            try
            {
                var processes = Process.GetProcesses();
                bool chromeRunning = processes.Any(p => p.ProcessName.ToLower().Contains("chrome"));
                bool edgeRunning = processes.Any(p => p.ProcessName.ToLower().Contains("msedge"));

                if (chromeRunning) browsers.Add("Chrome");
                if (edgeRunning) browsers.Add("Edge");
            }
            catch (Exception ex)
            {
                loggerService.LogWarning("process", $"Impossible de vérifier les navigateurs: {ex.Message}");
            }

            return browsers;
        }
        
        /// <summary>
        /// Gestionnaire de clic sur le message d'alerte navigateur
        /// </summary>
        private void StatusText_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Vérifier si c'est l'alerte navigateur
            if (navigateursEnCours.Count == 0)
                return;
            
            // Demander confirmation
            string browsers = string.Join(" et ", navigateursEnCours);
            var result = MessageBox.Show(
                $"Voulez-vous fermer {browsers} automatiquement ?\n\n" +
                $"⚠️ Assurez-vous de sauvegarder votre travail avant de continuer.\n\n" +
                $"Les navigateurs seront fermés et Panosse attendra 2 secondes avant de commencer le nettoyage.",
                "Fermer les navigateurs",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );
            
            if (result == MessageBoxResult.Yes)
            {
                FermerNavigateurs();
            }
        }
        
        /// <summary>
        /// Ferme les navigateurs en cours d'exécution
        /// </summary>
        private async void FermerNavigateurs()
        {
            try
            {
                int browsersTermines = 0;
                
                foreach (var browser in navigateursEnCours)
                {
                    try
                    {
                        string processName = browser == "Chrome" ? "chrome" : "msedge";
                        var processes = Process.GetProcesses().Where(p => p.ProcessName.ToLower().Contains(processName));
                        
                        foreach (var process in processes)
                        {
                            try
                            {
                                process.CloseMainWindow(); // Essayer de fermer proprement
                                await Task.Delay(500); // Attendre un peu
                                
                                if (!process.HasExited)
                                {
                                    process.Kill(); // Forcer si nécessaire
                                }
                                browsersTermines++;
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
                
                // Attendre 2 secondes que tout se ferme
                await Task.Delay(2000);
                
                // Revérifier les navigateurs
                navigateursEnCours = CheckRunningBrowsers();
                
                if (navigateursEnCours.Count == 0)
                {
                    // Tous les navigateurs sont fermés
                    viewModel.StatusText = "✅ Navigateurs fermés ! Vous pouvez maintenant nettoyer en toute sécurité.";
                    viewModel.StatusForeground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Vert
                    StatusText.Cursor = System.Windows.Input.Cursors.Arrow;
                    StatusText.TextDecorations = null;
                    
                    // Cacher le message après 5 secondes
                    await Task.Delay(5000);
                    viewModel.StatusText = "";
                }
                else
                {
                    // Certains navigateurs sont encore ouverts
                    string browsers = string.Join(" et ", navigateursEnCours);
                    viewModel.StatusText = $"⚠️ {browsers} n'a pas pu être fermé. Fermez-le manuellement.";
                    viewModel.StatusForeground = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Rouge
                    StatusText.Cursor = System.Windows.Input.Cursors.Arrow;
                    StatusText.TextDecorations = null;
                }
            }
            catch (Exception ex)
            {
                viewModel.StatusText = $"❌ Erreur lors de la fermeture : {ex.Message}";
                viewModel.StatusForeground = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Rouge
                StatusText.Cursor = System.Windows.Input.Cursors.Arrow;
                StatusText.TextDecorations = null;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Permet de déplacer la fenêtre sans bordure en cliquant n'importe où sur le fond
            // SAUF sur les éléments interactifs (Menu, Boutons, etc.)
            try
            {
                if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
                {
                    // Vérifier si le clic est sur un élément interactif
                    var element = e.OriginalSource as FrameworkElement;
                    
                    // Ne pas déplacer si on clique sur :
                    // - Le menu
                    // - Un bouton
                    // - Un MenuItem
                    // - Un TextBlock dans le menu
                    if (element != null)
                    {
                        // Rechercher si l'élément ou un parent est un contrôle interactif
                        DependencyObject? current = element;
                        while (current != null && current != this)
                        {
                            if (current is Button || 
                                current is MenuItem || 
                                current is System.Windows.Controls.Primitives.Popup)
                            {
                                return; // Ne pas déplacer la fenêtre
                            }
                            current = GetParentSafe(current);
                        }
                    }
                    
                    this.DragMove();
                }
            }
            catch
            {
                // Ignore les erreurs si DragMove est appelé dans un contexte invalide
            }
        }

        private void MainMenu_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != System.Windows.Input.MouseButton.Left)
            {
                return;
            }

            // Permet de déplacer la fenêtre depuis la barre de menu,
            // sauf lorsqu'on clique sur un item menu ou un bouton interactif.
            DependencyObject? current = e.OriginalSource as DependencyObject;
            while (current != null)
            {
                if (current is MenuItem || current is Button)
                {
                    return;
                }
                current = GetParentSafe(current);
            }

            try
            {
                DragMove();
                e.Handled = true;
            }
            catch
            {
                // Ignorer les cas où DragMove ne peut pas démarrer.
            }
        }

        private static DependencyObject? GetParentSafe(DependencyObject current)
        {
            // VisualTreeHelper only works for Visual/Visual3D.
            if (current is Visual || current is System.Windows.Media.Media3D.Visual3D)
            {
                return VisualTreeHelper.GetParent(current);
            }

            if (current is FrameworkContentElement contentElement)
            {
                return contentElement.Parent;
            }

            return LogicalTreeHelper.GetParent(current);
        }

        private void BtnQuitter_Click(object sender, RoutedEventArgs e)
        {
            // Masquer la fenêtre au lieu de fermer l'application
            this.Hide();
            
            // Afficher une notification
            if (notifyIcon != null && viewModel.ShowTrayNotifications)
            {
                notifyIcon.ShowBalloonTip(
                    2000,
                    "Panosse",
                    "Panosse est toujours actif dans la barre des tâches. Double-cliquez sur l'icône pour le réouvrir.",
                    Forms.ToolTipIcon.Info
                );
            }
        }

        private void BtnAPropos_Click(object sender, RoutedEventArgs e)
        {
            telemetryService.Increment("about_open_count");
            // Afficher l'overlay "À propos" avec animation
            OverlayAPropos.Visibility = Visibility.Visible;
            AnimerApparitionOverlay();
        }
        
        /// <summary>
        /// Gestionnaire pour le menu "Quitter définitivement"
        /// </summary>
        private void MenuItem_QuitterDefinitivement_Click(object sender, RoutedEventArgs e)
        {
            QuitterApplication();
        }
        
        /// <summary>
        /// Gestionnaire pour le menu "Actualiser la détection"
        /// </summary>
        private void MenuItem_Actualiser_Click(object sender, RoutedEventArgs e)
        {
            // Revérifier les navigateurs en cours d'exécution
            navigateursEnCours = CheckRunningBrowsers();
            
            if (navigateursEnCours.Count > 0)
            {
                string browsers = string.Join(" et ", navigateursEnCours);
                viewModel.StatusText = $"⚠️ Veuillez fermer {browsers} pour un nettoyage complet (cliquez ici pour fermer automatiquement)";
                viewModel.StatusForeground = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                StatusText.Cursor = System.Windows.Input.Cursors.Hand;
                StatusText.TextDecorations = TextDecorations.Underline;
            }
            else
            {
                viewModel.StatusText = "✅ Aucun navigateur ouvert. Vous pouvez nettoyer en toute sécurité !";
                viewModel.StatusForeground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Vert
                StatusText.Cursor = System.Windows.Input.Cursors.Arrow;
                StatusText.TextDecorations = null;
                
                // Cacher le message après 3 secondes
                Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    await Dispatcher.InvokeAsync(() => viewModel.StatusText = "");
                });
            }
        }
        
        /// <summary>
        /// Gestionnaire pour le menu "Ouvrir le dépôt GitHub"
        /// </summary>
        private void MenuItem_GitHub_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://github.com/barbarom84-ai/panosse",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Impossible d'ouvrir le navigateur.\n\nURL : https://github.com/barbarom84-ai/panosse\n\nErreur : {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void BtnRetourAPropos_Click(object sender, RoutedEventArgs e)
        {
            // Masquer l'overlay "À propos" avec animation
            AnimerDisparitionOverlay();
        }

        private void OverlayAPropos_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Fermer l'overlay si on clique sur le fond sombre
            if (e.Source == OverlayAPropos)
            {
                AnimerDisparitionOverlay();
            }
        }

        private void OverlaySettings_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.Source == OverlaySettings)
            {
                FermerParametres();
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            // Ouvrir le lien dans le navigateur par défaut
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
                e.Handled = true;
            }
            catch
            {
                // Ignorer les erreurs d'ouverture de lien
            }
        }

        private async void BtnNettoyer_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteCleaningAsync();
        }

        private async Task ExecuteCleaningAsync()
        {
            // Désactiver le bouton pendant le nettoyage
            BtnNettoyer.IsEnabled = false;
            bool isPreview = viewModel.PreviewModeEnabled;
            viewModel.ButtonText = isPreview ? "Prévisualisation en cours..." : "Nettoyage en cours...";
            viewModel.StatusText = "Préparation...";
            viewModel.StatusForeground = new SolidColorBrush(Color.FromRgb(117, 117, 117)); // Gris
            
            // Réinitialiser et afficher la barre de progression
            viewModel.ProgressVisibility = Visibility.Visible;
            viewModel.IsProgressIndeterminate = false;
            viewModel.ProgressValue = 0;
            viewModel.ProgressForeground = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Bleu (couleur par défaut)
            
            // Afficher et réinitialiser la liste des tâches
            TaskScrollViewer.Visibility = Visibility.Visible;
            viewModel.TaskMessages.Clear();
            etapesCourantes = 0;

            // Animer l'apparition de la liste des tâches avec un fondu fluide
            AnimerApparitionListeTaches();

            // Démarrer l'animation de pulsation
            StartPulseAnimation();
            try
            {
                var options = new CleanupExecutionOptions
                {
                    PreviewOnly = isPreview,
                    ExclusionPatterns = GetExclusionPatterns()
                };

                long octetsLiberes = 0;
                await foreach (CleanupStepUpdate update in cleanupOrchestrator.StreamCleanupAsync(options))
                {
                    if (!update.IsCompleted)
                    {
                        await AjouterMessageTache(update.Message);
                        await MettreAJourStatut(update.Message);
                        continue;
                    }

                    octetsLiberes += update.StepFreedBytes;
                    etapesCourantes = update.StepIndex;
                    await MettreAJourProgression();
                    await MettreAJourDernierMessage(update.Message);
                }

                StopPulseAnimation();
                double moLiberes = Math.Round(octetsLiberes / 1024.0 / 1024.0, 2);
                viewModel.ProgressForeground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                viewModel.StatusText = isPreview
                    ? $"✓ Prévisualisation terminée : {moLiberes} Mo estimés."
                    : $"✓ Votre PC est tout propre ! {moLiberes} Mo ont été libérés";
                viewModel.StatusForeground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                AnimerMessageSucces();
                espaceLibereMo = octetsLiberes / (1024 * 1024);
                ResetIconePropre();
            }
            catch (Exception ex)
            {
                StopPulseAnimation();
                viewModel.StatusText = $"⚠️ Nettoyage interrompu : {ex.Message}";
                viewModel.StatusForeground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                telemetryService.Increment("cleanup_manual_failed_count");
                loggerService.LogError("cleanup", "Erreur pendant le nettoyage manuel.", ex);
            }
            finally
            {
                RafraichirHistorique();
                viewModel.ButtonText = "Passer la panosse";
                BtnNettoyer.IsEnabled = true;
            }
        }

        // Méthodes utilitaires pour l'interface utilisateur
        private async Task AjouterMessageTache(string message)
        {
            await Dispatcher.InvokeAsync(() => viewModel.TaskMessages.Add(message));
        }

        private async Task MettreAJourDernierMessage(string message)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                if (viewModel.TaskMessages.Count > 0)
                {
                    viewModel.TaskMessages[viewModel.TaskMessages.Count - 1] = message;
                }
            });
        }

        private async Task MettreAJourStatut(string message)
        {
            await Dispatcher.InvokeAsync(() => viewModel.StatusText = message);
        }

        private async Task MettreAJourProgression()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                viewModel.ProgressValue = (etapesCourantes * 100.0) / etapesTotales;
            });
        }

        private void StartPulseAnimation()
        {
            pulseStoryboard = (Storyboard)this.Resources["PulseAnimation"];
            if (pulseStoryboard != null)
            {
                Storyboard.SetTarget(pulseStoryboard, BtnNettoyer);
                pulseStoryboard.Begin();
            }
        }

        private void StopPulseAnimation()
        {
            if (pulseStoryboard != null)
            {
                pulseStoryboard.Stop();
                // Réinitialiser la transformation
                var transform = (ScaleTransform)BtnNettoyer.RenderTransform;
                transform.ScaleX = 1.0;
                transform.ScaleY = 1.0;
            }
        }

        private void AnimerApparitionListeTaches()
        {
            // Créer une animation de fondu pour l'opacité
            DoubleAnimation fadeInAnimation = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase 
                { 
                    EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut 
                }
            };

            // Appliquer l'animation à la propriété Opacity du TaskScrollViewer
            TaskScrollViewer.BeginAnimation(System.Windows.UIElement.OpacityProperty, fadeInAnimation);
        }

        private void AnimerMessageSucces()
        {
            // S'assurer que le StatusText a une transformation pour l'animation
            if (StatusText.RenderTransform == null || !(StatusText.RenderTransform is ScaleTransform))
            {
                StatusText.RenderTransform = new ScaleTransform(1.0, 1.0);
                StatusText.RenderTransformOrigin = new Point(0.5, 0.5);
            }

            // Créer une animation de rebond sur l'échelle X avec KeyFrames
            DoubleAnimationUsingKeyFrames bounceX = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromSeconds(0.8)
            };
            bounceX.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
            bounceX.KeyFrames.Add(new EasingDoubleKeyFrame(1.3, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.2)), 
                new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }));
            bounceX.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.8)),
                new System.Windows.Media.Animation.BounceEase { Bounces = 2, Bounciness = 3, EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }));

            // Créer une animation de rebond sur l'échelle Y avec KeyFrames
            DoubleAnimationUsingKeyFrames bounceY = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromSeconds(0.8)
            };
            bounceY.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
            bounceY.KeyFrames.Add(new EasingDoubleKeyFrame(1.3, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.2)),
                new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }));
            bounceY.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.8)),
                new System.Windows.Media.Animation.BounceEase { Bounces = 2, Bounciness = 3, EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }));

            // Appliquer les animations
            var transform = (ScaleTransform)StatusText.RenderTransform;
            transform.BeginAnimation(ScaleTransform.ScaleXProperty, bounceX);
            transform.BeginAnimation(ScaleTransform.ScaleYProperty, bounceY);
        }

        private void AnimerApparitionOverlay()
        {
            // Animation de fondu pour l'overlay "À propos"
            DoubleAnimation fadeIn = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase
                {
                    EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut
                }
            };

            OverlayAPropos.BeginAnimation(System.Windows.UIElement.OpacityProperty, fadeIn);
        }

        private void AnimerDisparitionOverlay()
        {
            // Animation de fondu pour masquer l'overlay
            DoubleAnimation fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromSeconds(0.2),
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase
                {
                    EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn
                }
            };

            fadeOut.Completed += (s, e) =>
            {
                OverlayAPropos.Visibility = Visibility.Collapsed;
            };

            OverlayAPropos.BeginAnimation(System.Windows.UIElement.OpacityProperty, fadeOut);
        }

        // ==========================================
        // VÉRIFICATION DES MISES À JOUR
        // ==========================================

        /// <summary>
        /// Vérifie si une nouvelle version est disponible sur GitHub
        /// </summary>
        private async Task VerifierMiseAJour()
        {
            // Réinitialiser l'état d'erreur
            verificationEchouee = false;
            var sw = Stopwatch.StartNew();
            
            try
            {
                UpdateCheckResult result = await updateService.CheckForUpdateAsync(GITHUB_REPO, VERSION_ACTUELLE);
                if (result.VerificationFailed)
                {
                    GererErreurVerification();
                    telemetryService.Increment("update_check_failed_count");
                    return;
                }

                if (result.HasUpdate && result.ReleaseInfo != null)
                {
                    // Sauvegarder les URLs pour le bouton "Mettre à jour"
                    derniereVersionUrl = result.ReleaseInfo.HtmlUrl;
                    derniereVersionTag = result.ReleaseInfo.TagName;
                    downloadUrl = result.ReleaseInfo.DownloadUrl;
                    estAJour = false;
                    verificationEchouee = false;

                    // Afficher la barre de notification
                    await Dispatcher.InvokeAsync(() =>
                    {
                        viewModel.UpdateMessage = $"Une nouvelle version ({result.ReleaseInfo.TagName}) est disponible !";
                        AfficherBarreMiseAJour();
                    });
                    telemetryService.Increment("update_check_update_available_count");
                    return;
                }

                estAJour = result.IsUpToDate;
                verificationEchouee = false;
                if (estAJour)
                {
                    telemetryService.Increment("update_check_up_to_date_count");
                }
                sw.Stop();
                telemetryService.RecordDuration("update_check_duration_ms", sw.Elapsed);
            }
            catch (Exception ex)
            {
                GererErreurVerification();
                telemetryService.Increment("update_check_error_count");
                loggerService.LogError("update", "Erreur pendant la vérification des mises à jour.", ex);
                sw.Stop();
                telemetryService.RecordDuration("update_check_duration_ms", sw.Elapsed);
            }
        }

        /// <summary>
        /// Gère les erreurs de vérification de mise à jour de manière silencieuse
        /// </summary>
        private void GererErreurVerification()
        {
            // Marquer que la vérification a échoué
            verificationEchouee = true;
            estAJour = false;
            
            // Ne pas afficher de MessageBox ou de fenêtre d'erreur
            // L'utilisateur peut continuer à utiliser l'application normalement
            // Le bouton dans "À propos" affichera un message approprié
        }

        /// <summary>
        /// Affiche la barre de notification avec animation
        /// </summary>
        private void AfficherBarreMiseAJour()
        {
            UpdateBar.Visibility = Visibility.Visible;
            
            // Animation de slide-in + fade-in
            var slideAnimation = new DoubleAnimation
            {
                From = -40,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.4),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            
            var fadeAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.4)
            };
            
            UpdateBar.BeginAnimation(RenderTransformProperty, null);
            if (UpdateBar.RenderTransform is not TranslateTransform)
            {
                UpdateBar.RenderTransform = new TranslateTransform();
            }
            ((TranslateTransform)UpdateBar.RenderTransform).BeginAnimation(TranslateTransform.YProperty, slideAnimation);
            UpdateBar.BeginAnimation(OpacityProperty, fadeAnimation);
        }

        /// <summary>
        /// Masque la barre de notification avec animation
        /// </summary>
        private void MasquerBarreMiseAJour()
        {
            var slideAnimation = new DoubleAnimation
            {
                To = -40,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            
            var fadeAnimation = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3)
            };
            
            slideAnimation.Completed += (s, e) =>
            {
                UpdateBar.Visibility = Visibility.Collapsed;
            };
            
            if (UpdateBar.RenderTransform is not TranslateTransform)
            {
                UpdateBar.RenderTransform = new TranslateTransform();
            }
            ((TranslateTransform)UpdateBar.RenderTransform).BeginAnimation(TranslateTransform.YProperty, slideAnimation);
            UpdateBar.BeginAnimation(OpacityProperty, fadeAnimation);
        }

        /// <summary>
        /// Gestionnaire pour le bouton "Mettre à jour"
        /// </summary>
        private async void BtnMettreAJour_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteInstallUpdateAsync();
        }

        private async Task ExecuteInstallUpdateAsync()
        {
            telemetryService.Increment("update_install_start_count");
            if (string.IsNullOrEmpty(downloadUrl))
            {
                // Fallback : ouvrir la page GitHub si pas d'URL de téléchargement
                if (!string.IsNullOrEmpty(derniereVersionUrl))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = derniereVersionUrl,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception openEx)
                    {
                        loggerService.LogWarning("update", $"Impossible d'ouvrir la page release: {openEx.Message}");
                    }
                }
                return;
            }

            try
            {
                // Désactiver le bouton et fermer pendant le téléchargement
                BtnMettreAJour.IsEnabled = false;
                BtnFermerUpdate.IsEnabled = false;
                
                // Changer le message et afficher la barre de progression
                viewModel.UpdateMessage = "Téléchargement de la mise à jour...";
                viewModel.DownloadProgressVisibility = Visibility.Visible;
                viewModel.DownloadProgressValue = 0;

                // Télécharger la nouvelle version avec progression
                await TelechargerEtInstallerMiseAJour();
            }
            catch (Exception ex)
            {
                telemetryService.Increment("update_install_failed_count");
                // Masquer la barre de progression
                viewModel.DownloadProgressVisibility = Visibility.Collapsed;
                
                // En cas d'erreur, afficher un message et proposer le téléchargement manuel
                var result = MessageBox.Show(
                    $"Impossible de télécharger automatiquement la mise à jour.\n\n" +
                    $"Erreur : {ex.Message}\n\n" +
                    $"Voulez-vous ouvrir la page de téléchargement dans votre navigateur ?",
                    "Erreur de mise à jour",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes && !string.IsNullOrEmpty(derniereVersionUrl))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = derniereVersionUrl,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception openEx)
                    {
                        loggerService.LogWarning("update", $"Impossible d'ouvrir la page release: {openEx.Message}");
                    }
                }

                // Réactiver les boutons
                BtnMettreAJour.IsEnabled = true;
                BtnFermerUpdate.IsEnabled = true;
                viewModel.UpdateMessage = $"Une nouvelle version ({derniereVersionTag}) est disponible !";
            }
        }

        /// <summary>
        /// Télécharge et installe la mise à jour automatiquement avec progression
        /// </summary>
        private async Task TelechargerEtInstallerMiseAJour()
        {
            string cheminActuel = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(cheminActuel))
            {
                throw new InvalidOperationException("Impossible de déterminer le chemin de l'exécutable actuel.");
            }

            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                throw new InvalidOperationException("L'URL de téléchargement n'est pas disponible.");
            }

            string targetVersion = string.IsNullOrWhiteSpace(derniereVersionTag) ? "latest" : derniereVersionTag.Trim();
            string downloadedPath = Path.Combine(Path.GetTempPath(), $"Panosse-{targetVersion}.exe");
            await foreach (UpdateDownloadProgress progress in updateOrchestrator.DownloadAndPrepareInstallAsync(downloadUrl, cheminActuel, derniereVersionTag))
            {
                viewModel.DownloadProgressValue = progress.ProgressPercent;
                viewModel.UpdateMessage = progress.Message;
            }

            var scriptResult = await updateOrchestrator.BuildInstallScriptAsync(downloadedPath, cheminActuel, derniereVersionTag);
            if (!scriptResult.Success || string.IsNullOrWhiteSpace(scriptResult.ScriptPath))
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(scriptResult.ErrorMessage)
                    ? "La préparation de la mise à jour a échoué."
                    : scriptResult.ErrorMessage);
            }

            viewModel.DownloadProgressVisibility = Visibility.Collapsed;
            viewModel.UpdateMessage = "Installation en cours...";

            MessageBox.Show(
                "La mise à jour a été téléchargée avec succès !\n\n" +
                "Panosse va maintenant se fermer et se mettre à jour automatiquement.\n\n" +
                "L'application redémarrera dans quelques secondes.",
                "Mise à jour prête",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            updateOrchestrator.LaunchInstallerAndShutdown(scriptResult.ScriptPath);
        }

        /// <summary>
        /// Gestionnaire pour le bouton "Rechercher des mises à jour" dans le panneau À propos
        /// </summary>
        private async void BtnRechercherMAJ_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteCheckUpdatesAsync();
        }

        private async Task ExecuteCheckUpdatesAsync()
        {
            telemetryService.Increment("update_check_start_count");
            // Désactiver le bouton pendant la vérification
            viewModel.IsCheckUpdatesButtonEnabled = false;
            viewModel.CheckUpdatesButtonText = "Vérification...";
            viewModel.CheckUpdatesButtonBackground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            viewModel.LastUpdateCheckText = $"Dernière vérification : {DateTime.Now:HH:mm:ss} (en cours)";

            try
            {
                // Réinitialiser l'état
                estAJour = false;
                verificationEchouee = false;
                derniereVersionUrl = null;
                derniereVersionTag = null;
                downloadUrl = null;

                // Vérifier les mises à jour
                await VerifierMiseAJour();

                // Attendre un court instant pour l'animation
                await Task.Delay(500);

                if (verificationEchouee)
                {
                    // La vérification a échoué (pas de connexion, GitHub inaccessible, etc.)
                    viewModel.CheckUpdatesButtonText = "⚠️ Vérification impossible\n(vérifiez votre connexion)";
                    viewModel.CheckUpdatesButtonBackground = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                    viewModel.IsCheckUpdatesButtonEnabled = true; // Permettre de réessayer
                    viewModel.LastUpdateCheckText = $"Dernière vérification : {DateTime.Now:HH:mm:ss} (échec)";
                    
                    // Pas de MessageBox - L'utilisateur peut continuer normalement
                    // Il peut réessayer plus tard en cliquant à nouveau sur le bouton
                }
                else if (estAJour)
                {
                    // Aucune mise à jour disponible
                    viewModel.CheckUpdatesButtonText = "✅ Version à jour";
                    viewModel.CheckUpdatesButtonBackground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Vert
                    viewModel.IsCheckUpdatesButtonEnabled = true;
                    viewModel.LastUpdateCheckText = $"Dernière vérification : {DateTime.Now:HH:mm:ss} (à jour)";
                    
                    // Afficher un message de confirmation
                    await Task.Delay(100);
                    MessageBox.Show(
                        $"Vous utilisez déjà la dernière version de Panosse !\n\n" +
                        $"Version actuelle : {VERSION_ACTUELLE}\n\n" +
                        $"Aucune mise à jour nécessaire.",
                        "À jour",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else if (!string.IsNullOrEmpty(downloadUrl))
                {
                    // Une mise à jour est disponible
                    var result = MessageBox.Show(
                        $"Une nouvelle version est disponible !\n\n" +
                        $"Version actuelle : {VERSION_ACTUELLE}\n" +
                        $"Nouvelle version : {derniereVersionTag}\n\n" +
                        $"Voulez-vous télécharger et installer la mise à jour maintenant ?",
                        "Mise à jour disponible",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        // Fermer le panneau À propos
                        AnimerDisparitionOverlay();
                        
                        // Attendre la fin de l'animation
                        await Task.Delay(300);
                        
                        // Lancer le téléchargement et l'installation
                        viewModel.CheckUpdatesButtonText = "Téléchargement...";
                        await TelechargerEtInstallerMiseAJour();
                    }
                    else
                    {
                        // L'utilisateur a refusé
                        viewModel.CheckUpdatesButtonText = "🔍 Vérifier les mises à jour";
                        viewModel.IsCheckUpdatesButtonEnabled = true;
                        viewModel.LastUpdateCheckText = $"Dernière vérification : {DateTime.Now:HH:mm:ss} (mise à jour disponible)";
                    }
                }
                else
                {
                    // Mise à jour détectée mais sans lien .exe direct
                    viewModel.CheckUpdatesButtonText = "🌐 Ouvrir la page release";
                    viewModel.CheckUpdatesButtonBackground = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Bleu
                    viewModel.IsCheckUpdatesButtonEnabled = true;
                    viewModel.LastUpdateCheckText = $"Dernière vérification : {DateTime.Now:HH:mm:ss} (mise à jour disponible)";
                }
                // Note : Le cas "verificationEchouee" est déjà géré plus haut
                // Plus besoin de ce else final car on gère l'erreur silencieusement
            }
            catch (Exception)
            {
                // Erreur inattendue lors du clic sur le bouton
                // Afficher le bouton avec un message d'erreur
                viewModel.CheckUpdatesButtonText = "⚠️ Vérification impossible\n(vérifiez votre connexion)";
                viewModel.CheckUpdatesButtonBackground = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                viewModel.IsCheckUpdatesButtonEnabled = true;
                viewModel.LastUpdateCheckText = $"Dernière vérification : {DateTime.Now:HH:mm:ss} (erreur)";
                
                // Ne pas afficher de MessageBox - rester silencieux
                // L'utilisateur peut réessayer en recliquant
            }
        }

        /// <summary>
        /// Gestionnaire pour fermer la barre de notification
        /// </summary>
        private void BtnFermerUpdate_Click(object sender, RoutedEventArgs e)
        {
            MasquerBarreMiseAJour();
        }
    }
}
