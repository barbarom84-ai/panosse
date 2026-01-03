using System.Windows;
using System.IO;
using System;
using System.Threading;

namespace Panosse
{
    public partial class App : Application
    {
        // Mutex pour empêcher plusieurs instances de Panosse
        private Mutex? instanceMutex;
        private const string MUTEX_NAME = "Panosse_Unique_Mutex_99";
        
        protected override void OnStartup(StartupEventArgs e)
        {
            // Vérifier si une instance de Panosse est déjà en cours d'exécution
            bool isNewInstance;
            instanceMutex = new Mutex(true, MUTEX_NAME, out isNewInstance);
            
            if (!isNewInstance)
            {
                // Une instance de Panosse est déjà active
                // Afficher le message AVANT d'appeler base.OnStartup pour éviter de créer la fenêtre
                MessageBox.Show(
                    "Panosse est déjà active dans la barre des tâches.\n\n" +
                    "Astuce : Double-cliquez sur l'icône 🧹 dans la barre des tâches pour afficher la fenêtre.",
                    "Panosse - Déjà active",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                
                // Fermer cette nouvelle instance immédiatement
                Environment.Exit(0);
                return;
            }
            
            base.OnStartup(e);
            
            // Capturer TOUTES les exceptions non gérées
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                LogError("UnhandledException", ex);
                MessageBox.Show(
                    $"Erreur fatale au démarrage de Panosse:\n\n{ex?.Message}\n\nVoir panosse_crash.log pour plus de détails.",
                    "Panosse - Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            };
            
            this.DispatcherUnhandledException += (sender, args) =>
            {
                LogError("DispatcherUnhandledException", args.Exception);
                MessageBox.Show(
                    $"Erreur UI de Panosse:\n\n{args.Exception.Message}\n\nVoir panosse_crash.log pour plus de détails.",
                    "Panosse - Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                args.Handled = true; // Empêcher le crash complet
            };
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            // Libérer le Mutex quand l'application se ferme
            if (instanceMutex != null)
            {
                instanceMutex.ReleaseMutex();
                instanceMutex.Dispose();
            }
            
            base.OnExit(e);
        }
        
        private void LogError(string type, Exception? ex)
        {
            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "panosse_crash.log"
                );
                
                string log = $@"
===========================================
PANOSSE CRASH LOG
===========================================
Date/Heure : {DateTime.Now:yyyy-MM-dd HH:mm:ss}
Type : {type}
Message : {ex?.Message}
StackTrace :
{ex?.StackTrace}

InnerException : {ex?.InnerException?.Message}
InnerStackTrace :
{ex?.InnerException?.StackTrace}
===========================================

";
                File.AppendAllText(logPath, log);
            }
            catch
            {
                // Si on ne peut même pas logger, on ne peut rien faire
            }
        }
    }
}

