using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Panosse.Services;

/// <summary>
/// Detects and closes supported browser processes without touching WebView2 hosts.
/// </summary>
public sealed class BrowserProcessService : IBrowserProcessService
{
    private static readonly (string DisplayName, string[] ProcessNames)[] KnownBrowsers =
    [
        ("Chrome", ["chrome"]),
        ("Edge", ["msedge"]),
        ("Firefox", ["firefox"]),
        ("Opera", ["opera", "opera_gx"]),
        ("Brave", ["brave"]),
        ("Vivaldi", ["vivaldi"])
    ];

    public Task<IReadOnlyList<string>> GetRunningBrowsersAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var running = new List<string>();
            foreach ((string displayName, string[] processNames) in KnownBrowsers)
            {
                if (processNames.Any(HasRunningProcess))
                {
                    running.Add(displayName);
                }
            }

            return (IReadOnlyList<string>)running;
        }, cancellationToken);
    }

    public Task<BrowserCloseResult> CloseBrowsersAsync(
        IEnumerable<string> browserNames,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(browserNames);

        var targets = browserNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.Run(() =>
        {
            var closed = new List<string>();

            foreach (string displayName in targets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                IReadOnlyList<string>? processNames = ResolveProcessNames(displayName);
                if (processNames is null)
                {
                    continue;
                }

                bool allClosed = true;
                foreach (string processName in processNames)
                {
                    if (!TryCloseBrowser(processName, cancellationToken))
                    {
                        allClosed = false;
                    }
                }

                if (allClosed)
                {
                    closed.Add(displayName);
                }
            }

            // Give Windows a moment to tear down child processes.
            Thread.Sleep(750);

            var remaining = new List<string>();
            foreach ((string displayName, string[] processNames) in KnownBrowsers)
            {
                if (targets.Contains(displayName, StringComparer.OrdinalIgnoreCase) &&
                    processNames.Any(HasRunningProcess))
                {
                    remaining.Add(displayName);
                }
            }

            return new BrowserCloseResult
            {
                ClosedBrowsers = closed,
                RemainingBrowsers = remaining
            };
        }, cancellationToken);
    }

    private static IReadOnlyList<string>? ResolveProcessNames(string displayName)
    {
        foreach ((string knownDisplay, string[] processNames) in KnownBrowsers)
        {
            if (string.Equals(knownDisplay, displayName, StringComparison.OrdinalIgnoreCase))
            {
                return processNames;
            }
        }

        return null;
    }

    private static bool HasRunningProcess(string processName)
    {
        Process[] processes = Array.Empty<Process>();
        try
        {
            processes = Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }
        catch
        {
            return false;
        }
        finally
        {
            DisposeAll(processes);
        }
    }

    private static bool TryCloseBrowser(string processName, CancellationToken cancellationToken)
    {
        // 1) Graceful close for processes that own a main window.
        Process[] processes = Array.Empty<Process>();
        try
        {
            processes = Process.GetProcessesByName(processName);
            foreach (Process process in processes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (process.HasExited)
                    {
                        continue;
                    }

                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        _ = process.CloseMainWindow();
                    }
                }
                catch
                {
                    // Access denied / already exited — continue with other processes.
                }
            }
        }
        finally
        {
            DisposeAll(processes);
        }

        if (!WaitUntilGone(processName, TimeSpan.FromSeconds(3), cancellationToken))
        {
            // 2) Force-kill remaining browser processes (not WebView2).
            ForceKillByName(processName);
            _ = WaitUntilGone(processName, TimeSpan.FromSeconds(2), cancellationToken);
        }

        return !HasRunningProcess(processName);
    }

    private static bool WaitUntilGone(string processName, TimeSpan timeout, CancellationToken cancellationToken)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!HasRunningProcess(processName))
            {
                return true;
            }

            Thread.Sleep(200);
        }

        return !HasRunningProcess(processName);
    }

    private static void ForceKillByName(string processName)
    {
        // Prefer taskkill tree kill: browsers spawn many helper processes.
        try
        {
            using var killer = Process.Start(new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = $"/IM {processName}.exe /F /T",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            if (killer is not null)
            {
                _ = killer.WaitForExit(5000);
            }
        }
        catch
        {
            // Fallback below.
        }

        Process[] remaining = Array.Empty<Process>();
        try
        {
            remaining = Process.GetProcessesByName(processName);
            foreach (Process process in remaining)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Ignore access / race conditions.
                }
            }
        }
        finally
        {
            DisposeAll(remaining);
        }
    }

    private static void DisposeAll(IEnumerable<Process> processes)
    {
        foreach (Process process in processes)
        {
            try
            {
                process.Dispose();
            }
            catch
            {
                // Ignore disposal failures.
            }
        }
    }
}
