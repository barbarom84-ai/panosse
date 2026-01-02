using System.Windows;
using System.IO;
using System;

namespace Panosse
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
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

