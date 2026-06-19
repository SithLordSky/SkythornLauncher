using System.Diagnostics;

namespace SkythornLauncher.Services;

internal static class GameProcessTracker
{
    private static Process? _trackedProcess;

    public static void Track(Process? process)
    {
        _trackedProcess = process;
    }

    public static void Clear()
    {
        _trackedProcess = null;
    }

    public static bool IsGameRunning()
    {
        try
        {
            if (_trackedProcess != null && !_trackedProcess.HasExited)
            {
                return true;
            }

            foreach (var process in Process.GetProcessesByName("cuo"))
            {
                using (process)
                {
                    if (!process.HasExited)
                    {
                        return true;
                    }
                }
            }

            foreach (var process in Process.GetProcessesByName("dotnet"))
            {
                using (process)
                {
                    if (process.HasExited)
                    {
                        continue;
                    }

                    if (ProcessHostsClient(process))
                    {
                        return true;
                    }
                }
            }
        }
        catch
        {
            // If process enumeration fails, do not block updates.
        }

        return false;
    }

    private static bool ProcessHostsClient(Process process)
    {
        try
        {
            foreach (ProcessModule module in process.Modules)
            {
                if (string.Equals(module.ModuleName, "cuo.dll", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        catch
        {
            // Access denied or 32/64-bit mismatch — ignore.
        }

        return false;
    }
}
