using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Panosse.WinUI.Services;

/// <summary>
/// Ensures a single Panosse process: a second launch activates the existing window then exits.
/// </summary>
internal sealed class SingleInstanceCoordinator : IDisposable
{
    private const string MutexName = "Panosse_Unique_Mutex_99";
    private const string ActivateEventName = "Local\\Panosse_ActivateWindow";
    public const string ElevatedRestartArgument = "--elevated-restart";

    private readonly Mutex mutex;
    private readonly EventWaitHandle activateEvent;
    private readonly CancellationTokenSource cts = new();
    private bool disposed;

    private SingleInstanceCoordinator(Mutex mutex, EventWaitHandle activateEvent)
    {
        this.mutex = mutex;
        this.activateEvent = activateEvent;
    }

    /// <summary>
    /// Claims the primary instance. If another instance is already running, signals it to show
    /// and returns <c>false</c>.
    /// </summary>
    public static bool TryClaimPrimary(out SingleInstanceCoordinator? coordinator)
    {
        coordinator = null;
        bool isElevatedRestart = Environment.GetCommandLineArgs()
            .Any(arg => arg.Equals(ElevatedRestartArgument, StringComparison.OrdinalIgnoreCase));

        Mutex mutex;
        try
        {
            mutex = new Mutex(false, MutexName);
        }
        catch (UnauthorizedAccessException)
        {
            // Mutex créé par une instance administrateur : inaccessible depuis une instance standard.
            SignalExistingInstance();
            return false;
        }

        bool acquired;
        try
        {
            // Après une relance élevée, l'instance précédente libère le mutex en quittant.
            acquired = mutex.WaitOne(isElevatedRestart ? TimeSpan.FromSeconds(15) : TimeSpan.Zero);
        }
        catch (AbandonedMutexException)
        {
            acquired = true;
        }

        if (!acquired)
        {
            mutex.Dispose();
            SignalExistingInstance();
            return false;
        }

        EventWaitHandle activateEvent;
        try
        {
            activateEvent = new EventWaitHandle(false, EventResetMode.ManualReset, ActivateEventName);
        }
        catch (UnauthorizedAccessException)
        {
            // Événement laissé par une ancienne instance d'un autre niveau de droits : activation inter-instances indisponible.
            activateEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
        }

        coordinator = new SingleInstanceCoordinator(mutex, activateEvent);
        return true;
    }

    /// <summary>
    /// Starts listening for activation requests from secondary launches.
    /// </summary>
    public void StartActivationWatcher(Action onActivateRequested)
    {
        _ = Task.Run(() =>
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    if (activateEvent.WaitOne(500))
                    {
                        _ = activateEvent.Reset();
                        onActivateRequested();
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Shutting down.
            }
        }, cts.Token);
    }

    private static void SignalExistingInstance()
    {
        try
        {
            AllowForegroundToPrimaryProcess();

            if (EventWaitHandle.TryOpenExisting(ActivateEventName, out EventWaitHandle? existing))
            {
                using (existing)
                {
                    _ = existing.Set();
                }
            }
        }
        catch
        {
            // Best effort: primary may be shutting down.
        }
    }

    private static void AllowForegroundToPrimaryProcess()
    {
        try
        {
            Process current = Process.GetCurrentProcess();
            foreach (Process process in Process.GetProcessesByName(current.ProcessName))
            {
                try
                {
                    if (process.Id == current.Id)
                    {
                        continue;
                    }

                    _ = AllowSetForegroundWindow(process.Id);
                }
                finally
                {
                    process.Dispose();
                }
            }
        }
        catch
        {
            // Optional: SetForegroundWindow may still succeed without this.
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        cts.Cancel();
        cts.Dispose();
        activateEvent.Dispose();

        try
        {
            mutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // Not owned.
        }

        mutex.Dispose();
    }

    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int dwProcessId);
}
