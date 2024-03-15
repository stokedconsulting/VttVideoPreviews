using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VttVideoPreviews.Lib;

public static class Executor
{
    private static readonly StaticClassLogger StaticLogger;
    private static ILogger Logger => StaticLogger.Logger;

    static Executor()
    {
        StaticLogger = new StaticClassLogger();
    }
    
    public static async Task<(string output, string errors)> ExecAsync(string command)
    {
        try
        {
            var (cmd, args) = ExtractCommandAndArguments(command);
            var (process, output, errors )= ExecBase(cmd, args);
            await process.WaitForExitAsync();
            return (output, errors);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex.Message);
            Logger.LogError(ex.StackTrace);
            return ("", ex.Message);
        }
    }
    public static async Task<(string output, string errors)> ExecAsync(string command, string arguments)
    {
        try
        {
            var (process, output, errors) = ExecBase(command, arguments);
            await process.WaitForExitAsync();
            return (output, errors);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex.Message);
            Logger.LogError(ex.StackTrace);
            return ("", ex.Message);
        }
    }
    
    public static (string output, string errors) Exec(string command)
    {
        try
        {
            var (cmd, args) = ExtractCommandAndArguments(command);
            var (process, output, errors)= ExecBase(cmd, args);
            process.WaitForExit();
            return (output, errors);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex.Message);
            Logger.LogError(ex.StackTrace);
            return ("", ex.Message);
        }
    }
    
    public static (string output, string errors) Cli(string command)
    {
        try{
            var (process, output, errors) = CliBase(command);
            process.WaitForExit();
            return (output, errors);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex.Message);
            Logger.LogError(ex.StackTrace);
            return ("", ex.Message);
        }
    }
    
    public static async Task<(string output, string errors)> CliAsync(string command)
    {
        try 
        {
            var (process, output, errors) = CliBase(command);
            await process.WaitForExitAsync();
            var std = await process.StandardOutput.ReadToEndAsync();

            return (output, errors);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex.Message);
            Logger.LogError(ex.StackTrace);
            return ("", ex.Message);
        }
    }
    
    private static (Process process, string output, string errors) CliBase(string command)
    {
        var cmd = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash";
        var arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"/c {command}" : $"-c \"{command}\"";
        return ExecBase(cmd, arguments);
    }

    private static (Process process, string output, string errors) ExecBase(string command, string? arguments)
    {
        var process = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true
        };

        var output = string.Empty;
        var errors = string.Empty;

        process.OutputDataReceived += (sender, args) => output += args.Data + "\n";
        process.ErrorDataReceived += (sender, args) => errors += args.Data + "\n";

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return (process, output, errors);
    }
    
    private static (string cmd, string? args) ExtractCommandAndArguments(string command)
    {
        if (string.IsNullOrEmpty(command))
        {
            throw new Exception("The input command was null or empty. Can't extract command from it.");
        }

        int firstSpaceIndex = command.IndexOf(' ');

        if (firstSpaceIndex == -1) // No spaces found, input is a single word or empty
        {
            return (command, string.Empty);
        }
        else
        {
            var cmd = command.Substring(0, firstSpaceIndex);
            var args = command.Substring(firstSpaceIndex + 1);
            return (cmd, args);
        }
    }
}