using System;
using System.Diagnostics;
using Gml.Web.Api.Domains.System;

namespace Gml.Client.Helpers;

public class LauncherUpdater
{
    public static void FileReplaceAndRestart(OsType osType, string newFileName, string originalFileName)
    {
        Start(osType, newFileName, true);
    }

    // Runs fileName directly via the process API (no cmd.exe/bash -c involved), so a
    // filename or argument containing shell metacharacters can't inject extra commands —
    // fileName comes from the update package name, which round-trips through the update
    // server.
    private static void ExecuteProcess(string fileName, string[] arguments, string? workingDirectory = null)
    {
        var processInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments) processInfo.ArgumentList.Add(argument);
        if (workingDirectory is not null) processInfo.WorkingDirectory = workingDirectory;

        using var process = Process.Start(processInfo);
        process.WaitForExit();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        if (process.ExitCode != 0) throw new Exception($"Command '{fileName}' failed with error: {error}");
    }

    public static void Start(OsType osType, string fileName, bool skipUpdate = false)
    {
        var arguments = skipUpdate ? new[] { "-skip-update" } : Array.Empty<string>();

        switch (osType)
        {
            case OsType.Linux:
            case OsType.OsX:
                ExecuteProcess("chmod", new[] { "+x", fileName });
                ExecuteProcess($"./{fileName}", arguments);
                break;
            case OsType.Windows:
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    UseShellExecute = true
                };
                foreach (var argument in arguments) processInfo.ArgumentList.Add(argument);
                Process.Start(processInfo);
                break;
            }
            case OsType.Undefined:
            default:
                throw new ArgumentOutOfRangeException(nameof(osType), osType, null);
        }

        Environment.Exit(0);
    }
}
