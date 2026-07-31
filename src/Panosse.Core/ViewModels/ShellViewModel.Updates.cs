using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Panosse.Services;

namespace Panosse.Core.ViewModels;

public sealed partial class ShellViewModel
{
    private async Task ExecuteCheckUpdatesAsync()
    {
        IsCheckingUpdate = true;
        IsUpdateAvailable = false;
        updateDownloadUrl = null;
        updateTagName = null;
        updateExpectedSha256 = null;
        preparedScriptPath = null;
        downloadedUpdatePath = null;
        NotifyInstallReadyChanged();
        UpdateStatusText = "Vérification des mises à jour...";
        UpdateProgressValue = 0;
        SetUpdatePhase(UpdatePhase.Checking);

        try
        {
            string currentVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0";
            UpdateCheckResult result = await updateService.CheckForUpdateAsync(GithubRepo, currentVersion);
            if (result.VerificationFailed)
            {
                UpdateStatusText = "Vérification impossible. Vérifiez votre connexion.";
                SetUpdatePhase(UpdatePhase.Error);
                return;
            }

            if (!result.HasUpdate || result.ReleaseInfo == null)
            {
                UpdateStatusText = "Application déjà à jour.";
                SetUpdatePhase(UpdatePhase.UpToDate);
                return;
            }

            updateTagName = result.ReleaseInfo.TagName;
            updateDownloadUrl = result.ReleaseInfo.DownloadUrl;
            updateExpectedSha256 = result.ReleaseInfo.ExpectedSha256;
            IsUpdateAvailable = !string.IsNullOrWhiteSpace(updateDownloadUrl)
                && !string.IsNullOrWhiteSpace(updateExpectedSha256);

            if (IsUpdateAvailable)
            {
                UpdateStatusText = $"Mise à jour disponible : {updateTagName}";
                SetUpdatePhase(UpdatePhase.Available);
            }
            else
            {
                UpdateStatusText = string.IsNullOrWhiteSpace(updateDownloadUrl)
                    ? $"Mise à jour détectée ({updateTagName}) sans binaire .exe."
                    : $"Mise à jour détectée ({updateTagName}) sans checksum SHA256.";
                SetUpdatePhase(UpdatePhase.Error);
            }
        }
        catch (Exception ex)
        {
            UpdateStatusText = $"Erreur mise à jour : {ex.Message}";
            SetUpdatePhase(UpdatePhase.Error);
            loggerService.LogError("winui-update", "Update check exception.", ex);
        }
        finally
        {
            IsCheckingUpdate = false;
        }
    }

    private async Task ExecutePrepareUpdateAsync()
    {
        if (string.IsNullOrWhiteSpace(updateDownloadUrl))
        {
            UpdateStatusText = "Aucun lien de téléchargement disponible.";
            SetUpdatePhase(UpdatePhase.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(updateExpectedSha256))
        {
            UpdateStatusText = "Checksum SHA256 indisponible pour cette mise à jour.";
            SetUpdatePhase(UpdatePhase.Error);
            return;
        }

        string currentExePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
        if (string.IsNullOrWhiteSpace(currentExePath))
        {
            UpdateStatusText = "Impossible de déterminer le chemin de l'application.";
            SetUpdatePhase(UpdatePhase.Error);
            return;
        }

        IsPreparingUpdate = true;
        UpdateProgressValue = 0;
        UpdateStatusText = "Préparation de la mise à jour...";
        preparedScriptPath = null;
        downloadedUpdatePath = null;
        SetUpdatePhase(UpdatePhase.Downloading);

        try
        {
            await foreach (UpdateDownloadProgress progress in updateOrchestrator.DownloadAndPrepareInstallAsync(
                               updateDownloadUrl,
                               currentExePath,
                               updateTagName,
                               updateExpectedSha256))
            {
                UpdateProgressValue = progress.ProgressPercent;
                UpdateStatusText = progress.Message;
                if (!string.IsNullOrWhiteSpace(progress.LocalFilePath))
                {
                    downloadedUpdatePath = progress.LocalFilePath;
                }
            }

            if (string.IsNullOrWhiteSpace(downloadedUpdatePath))
            {
                UpdateStatusText = "Fichier téléchargé introuvable.";
                SetUpdatePhase(UpdatePhase.Error);
                return;
            }

            UpdateInstallResult result = await updateOrchestrator.BuildInstallScriptAsync(
                downloadedUpdatePath,
                currentExePath,
                updateTagName);
            if (!result.Success || string.IsNullOrWhiteSpace(result.ScriptPath))
            {
                UpdateStatusText = string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? "Préparation de la mise à jour échouée."
                    : result.ErrorMessage;
                SetUpdatePhase(UpdatePhase.Error);
                return;
            }

            preparedScriptPath = result.ScriptPath;
            UpdateStatusText = "Mise à jour préparée. Cliquez sur Installer.";
            SetUpdatePhase(UpdatePhase.Ready);
            NotifyInstallReadyChanged();
        }
        catch (Exception ex)
        {
            UpdateStatusText = $"Erreur préparation mise à jour : {ex.Message}";
            SetUpdatePhase(UpdatePhase.Error);
            loggerService.LogError("winui-update", "Update preparation exception.", ex);
        }
        finally
        {
            IsPreparingUpdate = false;
        }
    }

    private void ExecuteInstallPreparedUpdate()
    {
        if (string.IsNullOrWhiteSpace(preparedScriptPath))
        {
            UpdateStatusText = "Aucun script de mise à jour préparé.";
            SetUpdatePhase(UpdatePhase.Error);
            return;
        }

        UpdateStatusText = "Installation en cours...";
        updateOrchestrator.LaunchInstallerAndShutdown(preparedScriptPath);
    }

    private void SetUpdatePhase(UpdatePhase phase)
    {
        if (SetField(ref updatePhase, phase, nameof(CurrentUpdatePhase)))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UpdatePhaseText)));
        }
    }
}
