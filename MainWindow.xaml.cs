using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            TaskList.ItemsSource = taskMessages;
            
            // Définir la version dynamiquement depuis l'assembly
            VersionText.Text = $"v{VERSION_ACTUELLE}";
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Vérifier si Chrome ou Edge sont ouverts
            navigateursEnCours = CheckRunningBrowsers();
            if (navigateursEnCours.Count > 0)
            {
                string browsers = string.Join(" et ", navigateursEnCours);
                StatusText.Text = $"⚠️ Veuillez fermer {browsers} pour un nettoyage complet (cliquez ici pour fermer automatiquement)";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                StatusText.Cursor = System.Windows.Input.Cursors.Hand; // Cursor main pour indiquer que c'est cliquable
                StatusText.TextDecorations = TextDecorations.Underline; // Souligner pour indiquer que c'est cliquable
            }
            
            // Vérifier les mises à jour en arrière-plan
            _ = VerifierMiseAJour();
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
                    StatusText.Text = "✅ Navigateurs fermés ! Vous pouvez maintenant nettoyer en toute sécurité.";
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Vert
                    StatusText.Cursor = System.Windows.Input.Cursors.Arrow;
                    StatusText.TextDecorations = null;
                    
                    // Cacher le message après 5 secondes
                    await Task.Delay(5000);
                    StatusText.Text = "";
                }
                else
                {
                    // Certains navigateurs sont encore ouverts
                    string browsers = string.Join(" et ", navigateursEnCours);
                    StatusText.Text = $"⚠️ {browsers} n'a pas pu être fermé. Fermez-le manuellement.";
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Rouge
                    StatusText.Cursor = System.Windows.Input.Cursors.Arrow;
                    StatusText.TextDecorations = null;
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"❌ Erreur lors de la fermeture : {ex.Message}";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Rouge
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
                        DependencyObject current = element;
                        while (current != null && current != this)
                        {
                            if (current is Button || 
                                current is MenuItem || 
                                current is Menu ||
                                current is System.Windows.Controls.Primitives.Popup)
                            {
                                return; // Ne pas déplacer la fenêtre
                            }
                            current = VisualTreeHelper.GetParent(current);
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
                StatusText.Text = $"⚠️ Veuillez fermer {browsers} pour un nettoyage complet (cliquez ici pour fermer automatiquement)";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                StatusText.Cursor = System.Windows.Input.Cursors.Hand;
                StatusText.TextDecorations = TextDecorations.Underline;
            }
            else
            {
                StatusText.Text = "✅ Aucun navigateur ouvert. Vous pouvez nettoyer en toute sécurité !";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Vert
                StatusText.Cursor = System.Windows.Input.Cursors.Arrow;
                StatusText.TextDecorations = null;
                
                // Cacher le message après 3 secondes
                Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    await Dispatcher.InvokeAsync(() => StatusText.Text = "");
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
            
            try
            {
                using (var client = new HttpClient())
                {
                    // Timeout de 10 secondes pour éviter de bloquer trop longtemps
                    client.Timeout = TimeSpan.FromSeconds(10);
                    
                    // Ajouter un User-Agent (requis par l'API GitHub)
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Panosse-App/1.0");
                    
                    // URL de l'API GitHub pour la dernière release
                    string apiUrl = $"https://api.github.com/repos/{GITHUB_REPO}/releases/latest";
                    
                    // Récupérer les informations de la dernière release
                    var response = await client.GetStringAsync(apiUrl);
                    
                    // Parser la réponse JSON
                    using (JsonDocument doc = JsonDocument.Parse(response))
                    {
                        var root = doc.RootElement;
                        
                        // Récupérer le tag_name (ex: "v1.0.1")
                        string tagName = root.GetProperty("tag_name").GetString() ?? "";
                        
                        // Récupérer l'URL de la release
                        string htmlUrl = root.GetProperty("html_url").GetString() ?? "";
                        
                        // Récupérer l'URL de téléchargement du .exe
                        string exeDownloadUrl = "";
                        if (root.TryGetProperty("assets", out JsonElement assets) && assets.GetArrayLength() > 0)
                        {
                            foreach (JsonElement asset in assets.EnumerateArray())
                            {
                                string assetName = asset.GetProperty("name").GetString() ?? "";
                                // Chercher le fichier .exe (ex: Panosse-v1.0.1.exe)
                                if (assetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                                {
                                    exeDownloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                                    break;
                                }
                            }
                        }
                        
                        // Enlever le 'v' du début si présent
                        string versionDistante = tagName.TrimStart('v');
                        
                        // Comparer les versions
                        if (EstVersionPlusRecente(versionDistante, VERSION_ACTUELLE))
                        {
                            // Sauvegarder les URLs pour le bouton "Mettre à jour"
                            derniereVersionUrl = htmlUrl;
                            derniereVersionTag = tagName;
                            downloadUrl = exeDownloadUrl;
                            estAJour = false;
                            verificationEchouee = false;
                            
                            // Afficher la barre de notification
                            await Dispatcher.InvokeAsync(() =>
                            {
                                UpdateMessage.Text = $"Une nouvelle version ({tagName}) est disponible !";
                                AfficherBarreMiseAJour();
                            });
                        }
                        else
                        {
                            // L'application est à jour
                            estAJour = true;
                            verificationEchouee = false;
                        }
                    }
                }
            }
            catch (HttpRequestException)
            {
                // Erreur réseau (pas de connexion Internet, DNS échoue, etc.)
                GererErreurVerification();
            }
            catch (TaskCanceledException)
            {
                // Timeout de la requête (connexion trop lente)
                GererErreurVerification();
            }
            catch (JsonException)
            {
                // Erreur de parsing JSON (réponse invalide de GitHub)
                GererErreurVerification();
            }
            catch (Exception)
            {
                // Toute autre erreur imprévue
                GererErreurVerification();
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
        /// Compare deux versions au format X.Y.Z
        /// </summary>
        private bool EstVersionPlusRecente(string versionDistante, string versionLocale)
        {
            try
            {
                // Enlever les suffixes comme "-beta", "-alpha" pour la comparaison
                versionDistante = versionDistante.Split('-')[0];
                versionLocale = versionLocale.Split('-')[0];
                
                var partsDistante = versionDistante.Split('.').Select(int.Parse).ToArray();
                var partsLocale = versionLocale.Split('.').Select(int.Parse).ToArray();
                
                // Comparer MAJOR
                if (partsDistante[0] > partsLocale[0]) return true;
                if (partsDistante[0] < partsLocale[0]) return false;
                
                // Comparer MINOR
                if (partsDistante.Length > 1 && partsLocale.Length > 1)
                {
                    if (partsDistante[1] > partsLocale[1]) return true;
                    if (partsDistante[1] < partsLocale[1]) return false;
                }
                
                // Comparer PATCH
                if (partsDistante.Length > 2 && partsLocale.Length > 2)
                {
                    if (partsDistante[2] > partsLocale[2]) return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Affiche la barre de notification avec animation
        /// </summary>
        private void AfficherBarreMiseAJour()
        {
            UpdateBar.Visibility = Visibility.Visible;
            
            // Animation de slide-in + fade-in
            var slideAnimation = new ThicknessAnimation
            {
                From = new Thickness(0, -40, 0, 0),
                To = new Thickness(0, 0, 0, 0),
                Duration = TimeSpan.FromSeconds(0.4),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            
            var fadeAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.4)
            };
            
            UpdateBar.BeginAnimation(MarginProperty, slideAnimation);
            UpdateBar.BeginAnimation(OpacityProperty, fadeAnimation);
        }

        /// <summary>
        /// Masque la barre de notification avec animation
        /// </summary>
        private void MasquerBarreMiseAJour()
        {
            var slideAnimation = new ThicknessAnimation
            {
                To = new Thickness(0, -40, 0, 0),
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
            
            UpdateBar.BeginAnimation(MarginProperty, slideAnimation);
            UpdateBar.BeginAnimation(OpacityProperty, fadeAnimation);
        }

        /// <summary>
        /// Gestionnaire pour le bouton "Mettre à jour"
        /// </summary>
        private async void BtnMettreAJour_Click(object sender, RoutedEventArgs e)
        {
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
                    catch { }
                }
                return;
            }

            try
            {
                // Désactiver le bouton et fermer pendant le téléchargement
                BtnMettreAJour.IsEnabled = false;
                BtnFermerUpdate.IsEnabled = false;
                
                // Changer le message et afficher la barre de progression
                UpdateMessage.Text = "Téléchargement de la mise à jour...";
                DownloadProgressBar.Visibility = Visibility.Visible;
                DownloadProgressBar.Value = 0;

                // Télécharger la nouvelle version avec progression
                await TelechargerEtInstallerMiseAJour();
            }
            catch (Exception ex)
            {
                // Masquer la barre de progression
                DownloadProgressBar.Visibility = Visibility.Collapsed;
                
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
                    catch { }
                }

                // Réactiver les boutons
                BtnMettreAJour.IsEnabled = true;
                BtnFermerUpdate.IsEnabled = true;
                UpdateMessage.Text = $"Une nouvelle version ({derniereVersionTag}) est disponible !";
            }
        }

        /// <summary>
        /// Télécharge et installe la mise à jour automatiquement avec progression
        /// </summary>
        private async Task TelechargerEtInstallerMiseAJour()
        {
            // Chemin de l'exécutable actuel
            string cheminActuel = Process.GetCurrentProcess().MainModule?.FileName ?? "";
            if (string.IsNullOrEmpty(cheminActuel))
            {
                throw new Exception("Impossible de déterminer le chemin de l'exécutable actuel.");
            }

            // Dossier temporaire
            string dossierTemp = Path.GetTempPath();
            string cheminNouvelExe = Path.Combine(dossierTemp, $"Panosse-{derniereVersionTag}.exe");
            string cheminScriptBatch = Path.Combine(dossierTemp, "PanosseUpdate.bat");

            // Vérifier que downloadUrl n'est pas null
            if (string.IsNullOrEmpty(downloadUrl))
            {
                throw new InvalidOperationException("L'URL de téléchargement n'est pas disponible.");
            }

            // Télécharger le nouvel exécutable avec HttpClient pour avoir la progression
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Panosse-App");
                httpClient.Timeout = TimeSpan.FromMinutes(10); // Timeout de 10 minutes pour les gros fichiers

                // Obtenir la taille totale du fichier
                var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                
                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                
                // Télécharger avec progression
                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(cheminNouvelExe, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192];
                    long totalBytesRead = 0;
                    int bytesRead;
                    
                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;
                        
                        if (totalBytes > 0)
                        {
                            var progressPercentage = (int)((totalBytesRead * 100) / totalBytes);
                            
                            // Mettre à jour la barre de progression sur le thread UI
                            await Dispatcher.InvokeAsync(() =>
                            {
                                DownloadProgressBar.Value = progressPercentage;
                                UpdateMessage.Text = $"Téléchargement de la mise à jour... {progressPercentage}%";
                            });
                        }
                    }
                }
            }
            
            // Masquer la barre de progression et changer le message
            await Dispatcher.InvokeAsync(() =>
            {
                DownloadProgressBar.Visibility = Visibility.Collapsed;
                UpdateMessage.Text = "Installation en cours...";
            });

            // Créer le script batch de mise à jour
            string scriptBatch = $@"@echo off
chcp 65001 >nul
echo Mise a jour de Panosse en cours...
echo.

REM Attendre que Panosse se ferme (max 10 secondes)
set /a compteur=0
:attendre
timeout /t 1 /nobreak >nul
tasklist /FI ""IMAGENAME eq Panosse.exe"" 2>NUL | find /I /N ""Panosse.exe"">NUL
if ""%ERRORLEVEL%""==""0"" (
    set /a compteur+=1
    if !compteur! lss 10 goto attendre
)

echo Remplacement de l'ancien executable...

REM Sauvegarder l'ancien exe (au cas où)
if exist ""{cheminActuel}.old"" del ""{cheminActuel}.old""
move /Y ""{cheminActuel}"" ""{cheminActuel}.old"" >nul 2>&1

REM Copier le nouveau exe
move /Y ""{cheminNouvelExe}"" ""{cheminActuel}"" >nul 2>&1

if errorlevel 1 (
    echo ERREUR: Impossible de remplacer l'executable.
    echo Restauration de l'ancienne version...
    move /Y ""{cheminActuel}.old"" ""{cheminActuel}"" >nul 2>&1
    pause
    exit /b 1
)

echo Mise a jour terminee avec succes !
echo Redemarrage de Panosse...
timeout /t 2 /nobreak >nul

REM Relancer Panosse
start """" ""{cheminActuel}""

REM Supprimer l'ancienne version
if exist ""{cheminActuel}.old"" del ""{cheminActuel}.old""

REM Supprimer le script lui-même
(goto) 2>nul & del ""%~f0""
";

            // Écrire le script batch
            await File.WriteAllTextAsync(cheminScriptBatch, scriptBatch, System.Text.Encoding.UTF8);

            // Informer l'utilisateur
            MessageBox.Show(
                "La mise à jour a été téléchargée avec succès !\n\n" +
                "Panosse va maintenant se fermer et se mettre à jour automatiquement.\n\n" +
                "L'application redémarrera dans quelques secondes.",
                "Mise à jour prête",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            // Lancer le script batch
            var processInfo = new ProcessStartInfo
            {
                FileName = cheminScriptBatch,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(processInfo);

            // Fermer l'application actuelle
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Gestionnaire pour le bouton "Rechercher des mises à jour" dans le panneau À propos
        /// </summary>
        private async void BtnRechercherMAJ_Click(object sender, RoutedEventArgs e)
        {
            // Désactiver le bouton pendant la vérification
            BtnRechercherMAJ.IsEnabled = false;
            BtnRechercherMAJ.Content = "Vérification...";

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
                    BtnRechercherMAJ.Content = "⚠️ Vérification impossible\n(vérifiez votre connexion)";
                    BtnRechercherMAJ.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                    BtnRechercherMAJ.IsEnabled = true; // Permettre de réessayer
                    
                    // Pas de MessageBox - L'utilisateur peut continuer normalement
                    // Il peut réessayer plus tard en cliquant à nouveau sur le bouton
                }
                else if (estAJour)
                {
                    // Aucune mise à jour disponible
                    BtnRechercherMAJ.Content = "✅ Version à jour";
                    BtnRechercherMAJ.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Vert
                    
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
                        BtnRechercherMAJ.Content = "Téléchargement...";
                        await TelechargerEtInstallerMiseAJour();
                    }
                    else
                    {
                        // L'utilisateur a refusé
                        BtnRechercherMAJ.Content = "🔍 Vérifier les mises à jour";
                        BtnRechercherMAJ.IsEnabled = true;
                    }
                }
                // Note : Le cas "verificationEchouee" est déjà géré plus haut
                // Plus besoin de ce else final car on gère l'erreur silencieusement
            }
            catch (Exception)
            {
                // Erreur inattendue lors du clic sur le bouton
                // Afficher le bouton avec un message d'erreur
                BtnRechercherMAJ.Content = "⚠️ Vérification impossible\n(vérifiez votre connexion)";
                BtnRechercherMAJ.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                BtnRechercherMAJ.IsEnabled = true;
                
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
