using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;

namespace Panosse
{
    public partial class MainWindow : Window
    {
        // Import pour vider la corbeille nativement
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHEmptyRecycleBin(IntPtr hwnd, string rootPath, uint flags);

        private Storyboard? pulseStoryboard;
        private ObservableCollection<string> taskMessages = new ObservableCollection<string>();
        private int etapesCourantes = 0;
        private int etapesTotales = 8;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            TaskList.ItemsSource = taskMessages;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Vérifier si Chrome ou Edge sont ouverts
            var runningBrowsers = CheckRunningBrowsers();
            if (runningBrowsers.Count > 0)
            {
                string browsers = string.Join(" et ", runningBrowsers);
                StatusText.Text = $"⚠️ Veuillez fermer {browsers} pour un nettoyage complet";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
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
            catch { }

            return browsers;
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Permet de déplacer la fenêtre sans bordure en cliquant n'importe où sur le fond
            try
            {
                if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
                {
                    this.DragMove();
                }
            }
            catch
            {
                // Ignore les erreurs si DragMove est appelé dans un contexte invalide
            }
        }

        private void BtnQuitter_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnAPropos_Click(object sender, RoutedEventArgs e)
        {
            // Afficher l'overlay "À propos" avec animation
            OverlayAPropos.Visibility = Visibility.Visible;
            AnimerApparitionOverlay();
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
            // Désactiver le bouton pendant le nettoyage
            BtnNettoyer.IsEnabled = false;
            BtnText.Text = "Nettoyage en cours...";
            StatusText.Text = "Préparation...";
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)); // Gris
            
            // Réinitialiser et afficher la barre de progression
            PrgBar.Visibility = Visibility.Visible;
            PrgBar.IsIndeterminate = false;
            PrgBar.Value = 0;
            PrgBar.Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Bleu (couleur par défaut)
            
            // Afficher et réinitialiser la liste des tâches
            TaskScrollViewer.Visibility = Visibility.Visible;
            taskMessages.Clear();
            etapesCourantes = 0;

            // Animer l'apparition de la liste des tâches avec un fondu fluide
            AnimerApparitionListeTaches();

            // Démarrer l'animation de pulsation
            StartPulseAnimation();

            // Exécuter le nettoyage avec suivi des étapes
            long octetsLiberes = await ExecuterNettoyageAvecProgression();

            // Arrêter l'animation
            StopPulseAnimation();

            double moLiberes = Math.Round(octetsLiberes / 1024.0 / 1024.0, 2);
            
            // Changer la couleur de la barre de progression en vert
            PrgBar.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Vert
            
            // Message de succès en VERT
            StatusText.Text = $"✓ Votre PC est tout propre ! {moLiberes} Mo ont été libérés";
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Vert
            
            // Animer le message de succès avec un rebond
            AnimerMessageSucces();
            
            BtnText.Text = "Passer la panosse";
            BtnNettoyer.IsEnabled = true;
        }

        private async Task<long> ExecuterNettoyageAvecProgression()
        {
            long tailleInitiale = 0;

            // Étape 1: Nettoyage Corbeille
            tailleInitiale += await ExecuterEtapeNettoyage(
                iconeDebut: "🗑️",
                messageDebut: "Vidage de la corbeille...",
                action: async () =>
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            SHEmptyRecycleBin(IntPtr.Zero, string.Empty, 1 | 2 | 4);
                        }
                        catch { }
                    });
                    return 0; // Pas de taille mesurable pour la corbeille
                },
                messageFin: "✅ Corbeille vidée"
            );

            // Étape 2: Nettoyage Dossiers Temp
            long tempSize = await ExecuterEtapeNettoyage(
                iconeDebut: "🧹",
                messageDebut: "Nettoyage des fichiers temporaires...",
                action: async () =>
                {
                    return await Task.Run(() =>
                    {
                        long size = 0;
                        size += NettoyerDossier(Path.GetTempPath());
                        size += NettoyerDossier(@"C:\Windows\Temp");
                        return size;
                    });
                },
                messageFin: taille =>
                {
                    double moTemp = Math.Round(taille / 1024.0 / 1024.0, 2);
                    return $"✅ Fichiers temporaires nettoyés ({moTemp} Mo)";
                }
            );
            tailleInitiale += tempSize;

            // Étape 3: Cache Chrome
            long chromeSize = await ExecuterEtapeNettoyage(
                iconeDebut: "🌐",
                messageDebut: "Nettoyage du cache Chrome...",
                action: async () =>
                {
                    return await Task.Run(() =>
                    {
                        long size = 0;
                        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        
                        size += NettoyerDossier(Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache"));
                        size += NettoyerDossier(Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache\Cache_Data"));
                        size += NettoyerDossier(Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Code Cache"));
                        
                        return size;
                    });
                },
                messageFin: taille =>
                {
                    double moChrome = Math.Round(taille / 1024.0 / 1024.0, 2);
                    return $"✅ Cache Chrome nettoyé ({moChrome} Mo)";
                }
            );
            tailleInitiale += chromeSize;

            // Étape 4: Cache Microsoft Edge
            long edgeSize = await ExecuterEtapeNettoyage(
                iconeDebut: "🌐",
                messageDebut: "Nettoyage du cache Edge...",
                action: async () =>
                {
                    return await Task.Run(() =>
                    {
                        long size = 0;
                        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        
                        size += NettoyerDossier(Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache"));
                        size += NettoyerDossier(Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache\Cache_Data"));
                        size += NettoyerDossier(Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Code Cache"));
                        
                        return size;
                    });
                },
                messageFin: taille =>
                {
                    double moEdge = Math.Round(taille / 1024.0 / 1024.0, 2);
                    return $"✅ Cache Edge nettoyé ({moEdge} Mo)";
                }
            );
            tailleInitiale += edgeSize;

            // Étape 5: Nettoyage du registre
            await ExecuterEtapeNettoyage(
                iconeDebut: "📋",
                messageDebut: "Nettoyage du registre...",
                action: async () =>
                {
                    await Task.Run(() => NettoyerRegistre());
                    return 0;
                },
                messageFin: "✅ Registre nettoyé"
            );

            // Étape 6: Nettoyage des téléchargements anciens
            long downloadsSize = await ExecuterEtapeNettoyage(
                iconeDebut: "📥",
                messageDebut: "Nettoyage des téléchargements anciens...",
                action: async () =>
                {
                    return await Task.Run(() => NettoyerTelechargements());
                },
                messageFin: taille =>
                {
                    double moDownloads = Math.Round(taille / 1024.0 / 1024.0, 2);
                    return $"✅ Téléchargements nettoyés ({moDownloads} Mo)";
                }
            );
            tailleInitiale += downloadsSize;

            // Étape 7: Nettoyage des logs Windows
            long logsSize = await ExecuterEtapeNettoyage(
                iconeDebut: "📄",
                messageDebut: "Nettoyage des logs Windows...",
                action: async () =>
                {
                    return await Task.Run(() => NettoyerLogsWindows());
                },
                messageFin: taille =>
                {
                    double moLogs = Math.Round(taille / 1024.0 / 1024.0, 2);
                    return $"✅ Logs Windows nettoyés ({moLogs} Mo)";
                }
            );
            tailleInitiale += logsSize;

            // Étape 8: Nettoyage du cache des miniatures
            long thumbnailsSize = await ExecuterEtapeNettoyage(
                iconeDebut: "🖼️",
                messageDebut: "Nettoyage du cache des miniatures...",
                action: async () =>
                {
                    return await Task.Run(() => NettoyerCacheMiniatures());
                },
                messageFin: taille =>
                {
                    double moThumbnails = Math.Round(taille / 1024.0 / 1024.0, 2);
                    return $"✅ Cache miniatures nettoyé ({moThumbnails} Mo)";
                }
            );
            tailleInitiale += thumbnailsSize;

            return tailleInitiale;
        }

        // Méthode refactorisée pour exécuter une étape de nettoyage avec mise à jour de la progression
        private async Task<long> ExecuterEtapeNettoyage(
            string iconeDebut,
            string messageDebut,
            Func<Task<long>> action,
            Func<long, string>? messageFin = null,
            string? messageFinSimple = null)
        {
            // Afficher le message de début
            await AjouterMessageTache($"{iconeDebut} {messageDebut}");
            await MettreAJourStatut(messageDebut);

            // Exécuter l'action de nettoyage
            long taille = await action();

            // Incrémenter l'étape et mettre à jour la barre de progression
            etapesCourantes++;
            await MettreAJourProgression();

            // Afficher le message de fin
            string messageFinal = messageFin != null ? messageFin(taille) : messageFinSimple ?? "✅ Terminé";
            await MettreAJourDernierMessage(messageFinal);

            return taille;
        }

        // Surcharge pour les étapes avec message simple
        private async Task<long> ExecuterEtapeNettoyage(
            string iconeDebut,
            string messageDebut,
            Func<Task<long>> action,
            string messageFin)
        {
            return await ExecuterEtapeNettoyage(iconeDebut, messageDebut, action, null, messageFin);
        }

        // Méthodes utilitaires pour l'interface utilisateur
        private async Task AjouterMessageTache(string message)
        {
            await Dispatcher.InvokeAsync(() => taskMessages.Add(message));
        }

        private async Task MettreAJourDernierMessage(string message)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                if (taskMessages.Count > 0)
                {
                    taskMessages[taskMessages.Count - 1] = message;
                }
            });
        }

        private async Task MettreAJourStatut(string message)
        {
            await Dispatcher.InvokeAsync(() => StatusText.Text = message);
        }

        private async Task MettreAJourProgression()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                PrgBar.Value = (etapesCourantes * 100.0) / etapesTotales;
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

        private long ExecuterNettoyage()
        {
            // Méthode conservée pour compatibilité mais non utilisée
            return 0;
        }

        private void NettoyerRegistre()
        {
            // Nettoyer l'historique des commandes exécutées (RunMRU)
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU", true))
                {
                    if (key != null)
                    {
                        // Récupérer tous les noms de valeurs
                        string[] valueNames = key.GetValueNames();
                        
                        // Supprimer toutes les valeurs sauf la valeur par défaut
                        foreach (string valueName in valueNames)
                        {
                            if (!string.IsNullOrEmpty(valueName))
                            {
                                try
                                {
                                    key.DeleteValue(valueName, false);
                                }
                                catch { /* Ignore les erreurs individuelles */ }
                            }
                        }
                    }
                }
            }
            catch { /* Ignore les erreurs d'accès au registre */ }

            // Nettoyer la liste des documents récents
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\RecentDocs", true))
                {
                    if (key != null)
                    {
                        // Récupérer tous les noms de valeurs
                        string[] valueNames = key.GetValueNames();
                        
                        // Supprimer toutes les valeurs sauf la valeur par défaut
                        foreach (string valueName in valueNames)
                        {
                            if (!string.IsNullOrEmpty(valueName))
                            {
                                try
                                {
                                    key.DeleteValue(valueName, false);
                                }
                                catch { /* Ignore les erreurs individuelles */ }
                            }
                        }

                        // Nettoyer également les sous-clés (documents récents par type de fichier)
                        string[] subKeyNames = key.GetSubKeyNames();
                        foreach (string subKeyName in subKeyNames)
                        {
                            try
                            {
                                using (RegistryKey? subKey = key.OpenSubKey(subKeyName, true))
                                {
                                    if (subKey != null)
                                    {
                                        string[] subValueNames = subKey.GetValueNames();
                                        foreach (string valueName in subValueNames)
                                        {
                                            if (!string.IsNullOrEmpty(valueName))
                                            {
                                                try
                                                {
                                                    subKey.DeleteValue(valueName, false);
                                                }
                                                catch { /* Ignore les erreurs individuelles */ }
                                            }
                                        }
                                    }
                                }
                            }
                            catch { /* Ignore les erreurs de sous-clés */ }
                        }
                    }
                }
            }
            catch { /* Ignore les erreurs d'accès au registre */ }
        }

        private long NettoyerTelechargements()
        {
            long tailleSupprimee = 0;
            
            try
            {
                // Obtenir le chemin du dossier Downloads
                string downloadsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads"
                );

                if (!Directory.Exists(downloadsPath))
                {
                    return 0;
                }

                DirectoryInfo downloadsDir = new DirectoryInfo(downloadsPath);
                DateTime dateSeuilSuppression = DateTime.Now.AddDays(-14);

                // Parcourir les fichiers .exe et .msi
                var fichiersASupprimer = downloadsDir.GetFiles()
                    .Where(f => 
                        (f.Extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
                         f.Extension.Equals(".msi", StringComparison.OrdinalIgnoreCase)) &&
                        f.LastWriteTime < dateSeuilSuppression
                    );

                foreach (FileInfo fichier in fichiersASupprimer)
                {
                    try
                    {
                        tailleSupprimee += fichier.Length;
                        fichier.Delete();
                    }
                    catch
                    {
                        // Fichier en cours d'utilisation ou protégé, on ignore
                    }
                }
            }
            catch
            {
                // Erreur d'accès au dossier Downloads, on ignore
            }

            return tailleSupprimee;
        }

        private long NettoyerLogsWindows()
        {
            long tailleSupprimee = 0;
            
            try
            {
                string logsPath = @"C:\Windows\Logs";
                
                if (!Directory.Exists(logsPath))
                {
                    return 0;
                }

                DirectoryInfo logsDir = new DirectoryInfo(logsPath);
                
                // Nettoyer les fichiers de logs dans le dossier principal
                foreach (FileInfo file in logsDir.GetFiles("*.log", SearchOption.AllDirectories))
                {
                    try
                    {
                        // Vérifier que le fichier n'est pas trop récent (garder logs des 7 derniers jours)
                        if (file.LastWriteTime < DateTime.Now.AddDays(-7))
                        {
                            tailleSupprimee += file.Length;
                            file.Delete();
                        }
                    }
                    catch
                    {
                        // Fichier verrouillé par le système ou en cours d'utilisation, on ignore
                    }
                }

                // Nettoyer aussi les fichiers .etl (Event Trace Logs) et .old
                foreach (FileInfo file in logsDir.GetFiles("*.etl", SearchOption.AllDirectories))
                {
                    try
                    {
                        if (file.LastWriteTime < DateTime.Now.AddDays(-7))
                        {
                            tailleSupprimee += file.Length;
                            file.Delete();
                        }
                    }
                    catch { /* Fichier système verrouillé */ }
                }

                foreach (FileInfo file in logsDir.GetFiles("*.old", SearchOption.AllDirectories))
                {
                    try
                    {
                        tailleSupprimee += file.Length;
                        file.Delete();
                    }
                    catch { /* Fichier système verrouillé */ }
                }
            }
            catch
            {
                // Erreur d'accès au dossier Logs (permissions insuffisantes), on ignore
            }

            return tailleSupprimee;
        }

        private long NettoyerCacheMiniatures()
        {
            long tailleSupprimee = 0;
            
            try
            {
                // Chemin du cache des miniatures
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string thumbnailsPath = Path.Combine(localAppData, @"Microsoft\Windows\Explorer");
                
                if (!Directory.Exists(thumbnailsPath))
                {
                    return 0;
                }

                DirectoryInfo thumbnailsDir = new DirectoryInfo(thumbnailsPath);
                
                // Nettoyer tous les fichiers thumbcache*.db
                foreach (FileInfo file in thumbnailsDir.GetFiles("thumbcache*.db"))
                {
                    try
                    {
                        tailleSupprimee += file.Length;
                        file.Delete();
                    }
                    catch
                    {
                        // Fichier en cours d'utilisation par l'explorateur, on ignore
                    }
                }

                // Nettoyer aussi les fichiers iconcache*.db
                foreach (FileInfo file in thumbnailsDir.GetFiles("iconcache*.db"))
                {
                    try
                    {
                        tailleSupprimee += file.Length;
                        file.Delete();
                    }
                    catch
                    {
                        // Fichier en cours d'utilisation, on ignore
                    }
                }
            }
            catch
            {
                // Erreur d'accès au dossier, on ignore
            }

            return tailleSupprimee;
        }

        private long NettoyerDossier(string chemin)
        {
            long tailleSupprimee = 0;
            if (!Directory.Exists(chemin)) return 0;

            try
            {
                DirectoryInfo di = new DirectoryInfo(chemin);
                
                // Nettoyer les fichiers
                foreach (FileInfo file in di.GetFiles())
                {
                    try 
                    { 
                        tailleSupprimee += file.Length;
                        file.Delete(); 
                    } 
                    catch { /* Fichier utilisé, on ignore */ }
                }

                // Nettoyer les sous-dossiers récursivement
                foreach (DirectoryInfo subDir in di.GetDirectories())
                {
                    try
                    {
                        tailleSupprimee += NettoyerDossierRecursif(subDir);
                    }
                    catch { /* Dossier protégé, on ignore */ }
                }
            }
            catch { }

            return tailleSupprimee;
        }

        private long NettoyerDossierRecursif(DirectoryInfo dir)
        {
            long taille = 0;
            
            try
            {
                foreach (FileInfo file in dir.GetFiles())
                {
                    try
                    {
                        taille += file.Length;
                        file.Delete();
                    }
                    catch { }
                }

                foreach (DirectoryInfo subDir in dir.GetDirectories())
                {
                    try
                    {
                        taille += NettoyerDossierRecursif(subDir);
                        subDir.Delete();
                    }
                    catch { }
                }
            }
            catch { }

            return taille;
        }
    }
}
