using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Gml.Web.Api.Domains.System;

namespace Gml.Client.Helpers;

public class LauncherUpdater
{
    private const string FinishUpdateArgument = "-finish-update";
    private const int MaxCopyAttempts = 20;
    private static readonly TimeSpan CopyRetryDelay = TimeSpan.FromMilliseconds(250);

    public static void FileReplaceAndRestart(OsType osType, string newFileName, string originalFileName)
    {
        Start(osType, newFileName, true, originalFileName);
    }

    /// <summary>
    /// Called once at process startup. If this process was launched to finish an update
    /// (see <see cref="FileReplaceAndRestart"/>), copies itself over the originally installed
    /// file and relaunches from there, so the app keeps running from the location the user
    /// installed it in rather than the throwaway temp copy the update was downloaded to.
    /// Returns true if it handled a pending update (the process exits before returning).
    /// </summary>
    public static bool TryFinishPendingUpdate(string[] args, OsType osType)
    {
        var argIndex = Array.IndexOf(args, FinishUpdateArgument);
        if (argIndex < 0 || argIndex + 1 >= args.Length) return false;

        var targetPath = args[argIndex + 1];
        var currentExePath = Process.GetCurrentProcess().MainModule?.FileName;

        if (string.IsNullOrEmpty(currentExePath) ||
            string.Equals(Path.GetFullPath(currentExePath), Path.GetFullPath(targetPath),
                StringComparison.OrdinalIgnoreCase))
            return false;

        var targetDirectory = Path.GetDirectoryName(Path.GetFullPath(targetPath));
        if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
            Directory.CreateDirectory(targetDirectory);

        for (var attempt = 1; attempt <= MaxCopyAttempts; attempt++)
        {
            try
            {
                File.Copy(currentExePath, targetPath, true);
                break;
            }
            catch (IOException) when (attempt < MaxCopyAttempts)
            {
                // The previous process (the one we were launched to replace) may still be
                // releasing its lock on the target file as it exits.
                Thread.Sleep(CopyRetryDelay);
            }
        }

        Start(osType, targetPath, true);
        return true;
    }

    private static void ExecuteCommand(string command, OsType osType, string? workingDirectory = null)
    {
        ProcessStartInfo processInfo;
        if (osType == OsType.Windows)
            processInfo = new ProcessStartInfo("cmd.exe", $"/C {command}")
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        else
            processInfo = new ProcessStartInfo("bash", $"-c \"{command}\"")
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        if (workingDirectory is not null) processInfo.WorkingDirectory = workingDirectory;
        using (var process = Process.Start(processInfo))
        {
            process.WaitForExit();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            if (process.ExitCode != 0) throw new Exception($"Command '{command}' failed with error: {error}");
        }
    }

    public static void Start(OsType osType, string fileName, bool skipUpdate = false,
        string? finishUpdateTargetPath = null)
    {
        var argumentParts = new System.Collections.Generic.List<string>();
        if (skipUpdate) argumentParts.Add("-skip-update");
        if (!string.IsNullOrEmpty(finishUpdateTargetPath))
            argumentParts.Add(osType == OsType.Windows
                ? $"{FinishUpdateArgument} \"{finishUpdateTargetPath}\""
                : $"{FinishUpdateArgument} '{finishUpdateTargetPath}'");

        var arguments = string.Join(' ', argumentParts);

        switch (osType)
        {
            case OsType.Linux:
            case OsType.OsX:
                ExecuteCommand($"chmod +x \"{fileName}\"", osType);
                ExecuteCommand($"./{fileName} {arguments}", osType);
                break;
            case OsType.Windows:
                Process.Start(new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = true
                });
                break;
            case OsType.Undefined:
            default:
                throw new ArgumentOutOfRangeException(nameof(osType), osType, null);
        }

        Environment.Exit(0);
    }
}
